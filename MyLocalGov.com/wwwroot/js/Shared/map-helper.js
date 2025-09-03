(function (global) {
    "use strict";

    const state = new WeakMap();
    let mapsPromise = null;

    function normalizeEl(elOrSelector) {
        if (!elOrSelector) return null;
        return typeof elOrSelector === "string" ? document.querySelector(elOrSelector) : elOrSelector;
    }

    function toFixedOr(val, digits = 6) {
        const n = Number(val);
        return Number.isFinite(n) ? n.toFixed(digits) : "";
    }

    function extractAddressComponents(place) {
        const out = {
            streetNumber: "",
            route: "",
            street: "",
            suburb: "",
            city: "",
            postalCode: "",
            formatted: place?.formatted_address || ""
        };
        const comps = place?.address_components || [];
        for (const c of comps) {
            const t = c.types || [];
            if (t.includes("street_number")) out.streetNumber = c.long_name;
            if (t.includes("route")) out.route = c.long_name;
            if (
                t.includes("sublocality_level_1") ||
                t.includes("sublocality") ||
                t.includes("neighborhood")
            ) out.suburb = out.suburb || c.long_name;
            if (t.includes("locality")) out.city = c.long_name;
            // Fallback for areas where locality is not populated
            if (!out.city && t.includes("administrative_area_level_2")) out.city = c.long_name;
            if (t.includes("postal_code")) out.postalCode = c.long_name;
        }
        out.street = [out.streetNumber, out.route].filter(Boolean).join(" ").trim();
        return out;
    }

    function loadGoogleMaps(apiKey, options = {}) {
        if (global.google && global.google.maps) return Promise.resolve(global.google.maps);
        if (mapsPromise) return mapsPromise;

        const params = new URLSearchParams({
            key: apiKey || "",
            libraries: (options.libraries || ["places"]).join(","),
            v: options.version || "quarterly"
        });
        if (options.language) params.set("language", options.language);
        if (options.region) params.set("region", options.region);

        mapsPromise = new Promise((resolve, reject) => {
            const script = document.createElement("script");
            script.src = `https://maps.googleapis.com/maps/api/js?${params.toString()}`;
            script.async = true;
            script.defer = true;
            script.onerror = () => reject(new Error("Google Maps failed to load."));
            script.onload = () => resolve(global.google.maps);
            document.head.appendChild(script);
        });

        return mapsPromise;
    }

    function init(options) {
        if (!global.google || !global.google.maps) {
            throw new Error("MapHelper.init: Google Maps JS API is not loaded. Call MapHelper.loadGoogleMaps(apiKey) first.");
        }

        const maps = global.google.maps;

        const mapEl = normalizeEl(options.map);
        const inputEl = normalizeEl(options.input);
        const latEl = normalizeEl(options.latInput);
        const lngEl = normalizeEl(options.lngInput);

        if (!mapEl) throw new Error("MapHelper.init: 'map' element is required.");
        if (!inputEl) throw new Error("MapHelper.init: 'input' element is required.");

        const center = options.defaultCenter || { lat: -33.9249, lng: 18.4241 };
        const map = new maps.Map(mapEl, {
            center,
            zoom: options.zoom ?? 13,
            mapTypeControl: false,
            streetViewControl: false,
            fullscreenControl: false
        });

        const marker = new maps.Marker({
            map,
            position: center,
            draggable: options.draggable !== false
        });

        const autocomplete = new maps.places.Autocomplete(inputEl, options.autocompleteOptions || {
            fields: ["formatted_address", "geometry", "address_components"]
        });
        autocomplete.bindTo("bounds", map);

        const geocoder = new maps.Geocoder();

        const ctx = {
            mapEl,
            inputEl,
            latEl,
            lngEl,
            map,
            marker,
            autocomplete,
            geocoder,
            lastPlace: null,
            options
        };

        function emitChange(lat, lng, place) {
            if (latEl) latEl.value = toFixedOr(lat);
            if (lngEl) lngEl.value = toFixedOr(lng);
            if (typeof options.onChange === "function") {
                options.onChange({
                    lat,
                    lng,
                    place: place || ctx.lastPlace || null,
                    address: place?.formatted_address || null,
                    map,
                    marker,
                    input: inputEl
                });
            }
        }

        function setPosition(lat, lng, { pan = true, zoom = options.pickZoom ?? 15, reverseGeocode = false } = {}) {
            const pos = new maps.LatLng(lat, lng);
            marker.setPosition(pos);
            if (pan) map.setCenter(pos);
            if (zoom) map.setZoom(zoom);
            emitChange(lat, lng, ctx.lastPlace);

            if (reverseGeocode) {
                geocoder.geocode({ location: { lat, lng } }, (results, status) => {
                    if (status === "OK" && results && results[0]) {
                        ctx.lastPlace = results[0];
                        if (inputEl) inputEl.value = results[0].formatted_address || "";
                        emitChange(lat, lng, results[0]);
                    }
                });
            }
        }

        // When user picks a place from autocomplete
        autocomplete.addListener("place_changed", () => {
            const place = autocomplete.getPlace();
            ctx.lastPlace = place || null;
            if (!place || !place.geometry) return;

            const loc = place.geometry.location;
            const lat = loc.lat();
            const lng = loc.lng();

            map.setCenter(loc);
            map.setZoom(options.pickZoom ?? 15);
            marker.setPosition(loc);

            emitChange(lat, lng, place);
        });

        // Click to move marker
        map.addListener("click", (e) => {
            const lat = e.latLng.lat();
            const lng = e.latLng.lng();
            marker.setPosition(e.latLng);
            emitChange(lat, lng, null);

            if (options.reverseGeocodeOnPick) {
                geocoder.geocode({ location: { lat, lng } }, (results, status) => {
                    if (status === "OK" && results && results[0]) {
                        ctx.lastPlace = results[0];
                        if (inputEl) inputEl.value = results[0].formatted_address || "";
                        emitChange(lat, lng, results[0]);
                    }
                });
            }
        });

        // Drag end to update
        marker.addListener("dragend", () => {
            const pos = marker.getPosition();
            const lat = pos.lat();
            const lng = pos.lng();
            emitChange(lat, lng, null);

            if (options.reverseGeocodeOnPick) {
                geocoder.geocode({ location: { lat, lng } }, (results, status) => {
                    if (status === "OK" && results && results[0]) {
                        ctx.lastPlace = results[0];
                        if (inputEl) inputEl.value = results[0].formatted_address || "";
                        emitChange(lat, lng, results[0]);
                    }
                });
            }
        });

        // Initialize hidden fields
        emitChange(center.lat, center.lng, null);

        state.set(mapEl, ctx);

        return {
            getPosition: () => {
                const p = marker.getPosition();
                return { lat: p.lat(), lng: p.lng() };
            },
            setPosition: (lat, lng, opts) => setPosition(lat, lng, opts || {}),
            destroy: () => {
                global.google.maps.event.clearInstanceListeners(marker);
                global.google.maps.event.clearInstanceListeners(map);
                global.google.maps.event.clearInstanceListeners(autocomplete);
                marker.setMap(null);
                state.delete(mapEl);
            }
        };
    }

    global.MapHelper = {
        loadGoogleMaps,
        init,
        parseAddressComponents: extractAddressComponents
    };
})(window);