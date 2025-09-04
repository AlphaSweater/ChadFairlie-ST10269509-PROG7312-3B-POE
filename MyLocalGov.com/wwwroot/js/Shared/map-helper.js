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

	function clamp(n, min, max) {
		return Math.max(min, Math.min(max, n));
	}

	function debounce(fn, wait = 250) {
		let t = null;
		return function (...args) {
			clearTimeout(t);
			t = setTimeout(() => fn.apply(this, args), wait);
		};
	}

	function haversineMeters(a, b) {
		if (!a || !b) return NaN;
		const R = 6371000;
		const toRad = (x) => (x * Math.PI) / 180;
		const dLat = toRad(b.lat - a.lat);
		const dLon = toRad(b.lng - a.lng);
		const lat1 = toRad(a.lat);
		const lat2 = toRad(b.lat);
		const s =
			Math.sin(dLat / 2) * Math.sin(dLat / 2) +
			Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
		const c = 2 * Math.atan2(Math.sqrt(s), Math.sqrt(1 - s));
		return R * c;
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

	function discoverApiKey(explicitKey) {
		if (explicitKey) return explicitKey;
		if (global.__gmapsKey) return global.__gmapsKey;
		const meta = document.querySelector('meta[name="google-maps-api-key"]');
		if (meta && meta.content) return meta.content;
		return "";
	}

	function loadGoogleMaps(apiKey, options = {}) {
		const resolvedKey = discoverApiKey(apiKey);
		if (global.google && global.google.maps) return Promise.resolve(global.google.maps);
		if (mapsPromise) return mapsPromise;

		const params = new URLSearchParams({
			key: resolvedKey || "",
			libraries: (options.libraries || ["places"]).join(","),
			v: options.version || "quarterly",
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

	function buildValidationPayload(parts, opts = {}) {
		// Address Validation API payload
		// https://developers.google.com/maps/documentation/address-validation/reference/rest/v1/top-level/validateAddress
		const { regionCode, languageCode } = opts;
		const addressLines = [];
		const line1 = [parts.streetNumber, parts.route].filter(Boolean).join(" ").trim() || parts.street || "";
		if (line1) addressLines.push(line1);

		return {
			address: {
				regionCode: regionCode || "ZA", // default to South Africa (adjust as needed or pass via options)
				languageCode: languageCode || undefined,
				addressLines,
				locality: parts.city || undefined,
				postalCode: parts.postalCode || undefined
			},
			previousResponseId: undefined,
			enableUspsCass: false
		};
	}

	async function callAddressValidationProxy(payload, endpoint) {
		if (!endpoint) return null;
		try {
			const res = await fetch(endpoint, {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify(payload)
			});
			if (!res.ok) throw new Error(`Validation proxy failed: ${res.status}`);
			return await res.json();
		} catch (e) {
			console.warn("Address validation failed:", e);
			return null;
		}
	}

	function scoreFromValidation(verdict) {
		// Coarse weights based on Google validation verdict details
		// addressGranularity: SUB_PREMISE > PREMISE > PREMISE_PROXIMITY > ROUTE > OTHER/LOCALITY...
		const gran = (verdict?.addressGranularity || "").toUpperCase();
		const complete = !!verdict?.addressComplete;
		const hasUnconfirmed = !!verdict?.hasUnconfirmedComponents;

		let s = 0;

		if (complete) s += 40;
		switch (gran) {
			case "SUB_PREMISE": s += 35; break;
			case "PREMISE": s += 30; break;
			case "PREMISE_PROXIMITY": s += 24; break;
			case "ROUTE": s += 18; break;
			case "LOCALITY": s += 10; break;
			case "ADMINISTRATIVE_AREA": s += 6; break;
			default: s += 4; break;
		}
		if (hasUnconfirmed) s -= 10;

		return s;
	}

	function scoreFromGeocodePrecision(locationType) {
		switch ((locationType || "").toUpperCase()) {
			case "ROOFTOP": return 20;
			case "RANGE_INTERPOLATED": return 15;
			case "GEOMETRIC_CENTER": return 8;
			case "APPROXIMATE": return 2;
			default: return 0;
		}
	}

	function scoreFromDistance(meters) {
		if (!Number.isFinite(meters)) return 0;
		if (meters <= 15) return 10;
		if (meters <= 50) return 5;
		if (meters <= 200) return 0;
		if (meters <= 1000) return -10;
		return -20;
	}

	function completenessBonus(parts) {
		let s = 0;
		if (parts.street || (parts.streetNumber && parts.route)) s += 2;
		if (parts.suburb) s += 2;
		if (parts.city) s += 2;
		if (parts.postalCode) s += 2;
		return s;
	}

	function scoreToLabel(score) {
		if (score >= 85) return "Exact";
		if (score >= 65) return "Near";
		if (score >= 40) return "Good Area Match";
		return "Vague";
	}

	function computeScore({ validation, geocodeLocationType, pin, validatedPoint, parts }) {
		const base = scoreFromValidation(validation?.verdict);
		const precision = scoreFromGeocodePrecision(geocodeLocationType);
		const d = (pin && validatedPoint) ? haversineMeters(pin, validatedPoint) : NaN;
		const dist = scoreFromDistance(d);
		const comp = completenessBonus(parts || {});
		const raw = base + precision + dist + comp;
		const score = clamp(Math.round(raw), 0, 100);
		return {
			score,
			label: scoreToLabel(score),
			details: {
				validationVerdict: validation?.verdict || null,
				geocodeLocationType: geocodeLocationType || null,
				distanceMeters: Number.isFinite(d) ? Math.round(d) : null,
				completenessBonus: comp
			}
		};
	}

	function setInputToCoords(inputEl, lat, lng) {
		if (!inputEl) return;
		inputEl.value = `${toFixedOr(lat)}, ${toFixedOr(lng)}`;
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
		const sessionToken = new maps.places.AutocompleteSessionToken();

		const addressFields = options.addressFields || {
			street: null, suburb: null, city: null, postalCode: null
		};
		// Normalize DOM refs
		for (const k of Object.keys(addressFields)) {
			addressFields[k] = normalizeEl(addressFields[k]);
		}

		const ctx = {
			mapEl,
			inputEl,
			latEl,
			lngEl,
			map,
			marker,
			autocomplete,
			geocoder,
			sessionToken,
			lastPlace: null,
			lastGeocode: null,
			lastValidation: null,
			lastScore: null,
			options
		};

		function emitChange(lat, lng, place, extra = {}) {
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
					input: inputEl,
					validation: ctx.lastValidation || null,
					score: ctx.lastScore?.score ?? null,
					scoreLabel: ctx.lastScore?.label ?? null,
					scoreDetails: ctx.lastScore?.details ?? null,
					...extra
				});
			}
		}

		function writeAddressFields(parts) {
			if (!parts) return;
			const setIfEl = (el, val) => { if (el && val != null) el.value = val; };
			const streetValue = parts.street || [parts.streetNumber, parts.route].filter(Boolean).join(" ").trim();
			setIfEl(addressFields.street, streetValue || "");
			setIfEl(addressFields.suburb, parts.suburb || "");
			setIfEl(addressFields.city, parts.city || "");
			setIfEl(addressFields.postalCode, parts.postalCode || "");
		}

		async function validateAndScore(lat, lng, placeOrResult, { addressText } = {}) {
			try {
				const place = placeOrResult || ctx.lastPlace || null;
				// Prefer components from place/result; fall back to fields
				let parts = extractAddressComponents(place);
				if (!parts.formatted && addressText) parts.formatted = addressText;

				// Try to use last geocode result for geometry and location_type
				let validatedPoint = null;
				let locationType = null;

				if (ctx.lastGeocode && ctx.lastGeocode.results && ctx.lastGeocode.results[0]) {
					const r0 = ctx.lastGeocode.results[0];
					if (r0.geometry && r0.geometry.location) {
						validatedPoint = {
							lat: typeof r0.geometry.location.lat === "function" ? r0.geometry.location.lat() : r0.geometry.location.lat,
							lng: typeof r0.geometry.location.lng === "function" ? r0.geometry.location.lng() : r0.geometry.location.lng
						};
					}
					locationType = r0.geometry?.location_type || null;
				} else if (place?.geometry?.location) {
					validatedPoint = {
						lat: place.geometry.location.lat(),
						lng: place.geometry.location.lng()
					};
				}

				// Build Address Validation payload and call proxy endpoint if configured
				let validation = null;
				if (options.validation?.endpoint) {
					const payload = buildValidationPayload(parts, {
						regionCode: options.validation.regionCode,
						languageCode: options.validation.languageCode
					});
					validation = await callAddressValidationProxy(payload, options.validation.endpoint);
				}

				// Compute score
				const pin = { lat, lng };
				const score = computeScore({
					validation,
					geocodeLocationType: locationType,
					pin,
					validatedPoint,
					parts
				});

				ctx.lastValidation = validation;
				ctx.lastScore = score;

				emitChange(lat, lng, place, { validation, score: score.score, scoreLabel: score.label, scoreDetails: score.details });
			} catch (err) {
				console.warn("validateAndScore failed:", err);
			}
		}

		const debouncedValidate = debounce((lat, lng, place, extra) => {
			validateAndScore(lat, lng, place, extra);
		}, options.validation?.debounceMs ?? 350);

		function reverseGeocode(lat, lng, { setInput = true } = {}) {
			return new Promise((resolve) => {
				ctx.geocoder.geocode({ location: { lat, lng } }, (results, status) => {
					ctx.lastGeocode = { results: results || [], status };
					if (status === "OK" && results && results[0]) {
						ctx.lastPlace = results[0];
						const parts = extractAddressComponents(results[0]);
						writeAddressFields(parts);
						if (setInput && ctx.inputEl) {
							if (options.inputReflectsCoordinatesOnPick) {
								setInputToCoords(ctx.inputEl, lat, lng);
							} else {
								ctx.inputEl.value = results[0].formatted_address || "";
							}
						}
						resolve(results[0]);
					} else {
						resolve(null);
					}
				});
			});
		}

		function setPosition(lat, lng, { pan = true, zoom = options.pickZoom ?? 15, reverseGeocodeOnPick = false, reflectCoords = options.inputReflectsCoordinatesOnPick !== false } = {}) {
			const pos = new maps.LatLng(lat, lng);
			marker.setPosition(pos);
			if (pan) map.setCenter(pos);
			if (zoom) map.setZoom(zoom);

			if (reflectCoords && inputEl) {
				setInputToCoords(inputEl, lat, lng);
			}

			emitChange(lat, lng, ctx.lastPlace);

			if (reverseGeocodeOnPick || options.reverseGeocodeOnPick) {
				reverseGeocode(lat, lng, { setInput: !reflectCoords }).then((place) => {
					debouncedValidate(lat, lng, place, { addressText: place?.formatted_address });
				});
			} else {
				debouncedValidate(lat, lng, ctx.lastPlace, { addressText: inputEl?.value });
			}
		}

		// Autocomplete flow: suggestions provided by the widget; selection updates map/pin/fields
		autocomplete.setOptions({ sessionToken });
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

			const parts = extractAddressComponents(place);
			writeAddressFields(parts);

			// Geocode once to capture location_type for precision scoring
			geocoder.geocode({ placeId: place.place_id }, (results, status) => {
				ctx.lastGeocode = { results: results || [], status };
				debouncedValidate(lat, lng, place, { addressText: place.formatted_address });
			});

			emitChange(lat, lng, place);
		});

		// Click to move marker
		map.addListener("click", (e) => {
			const lat = e.latLng.lat();
			const lng = e.latLng.lng();
			marker.setPosition(e.latLng);

			setPosition(lat, lng, { reverseGeocodeOnPick: true, reflectCoords: options.inputReflectsCoordinatesOnPick !== false });
		});

		// Drag end to update
		marker.addListener("dragend", () => {
			const pos = marker.getPosition();
			const lat = pos.lat();
			const lng = pos.lng();
			setPosition(lat, lng, { reverseGeocodeOnPick: true, reflectCoords: options.inputReflectsCoordinatesOnPick !== false });
		});

		// Initialize hidden fields and score on load
		if (latEl) latEl.value = toFixedOr(center.lat);
		if (lngEl) lngEl.value = toFixedOr(center.lng);
		emitChange(center.lat, center.lng, null);
		debouncedValidate(center.lat, center.lng, null, { addressText: inputEl?.value });

		state.set(mapEl, ctx);

		return {
			getPosition: () => {
				const p = marker.getPosition();
				return { lat: p.lat(), lng: p.lng() };
			},
			setPosition: (lat, lng, opts) => setPosition(lat, lng, opts || {}),
			validateNow: () => {
				const p = marker.getPosition();
				const lat = p.lat();
				const lng = p.lng();
				return validateAndScore(lat, lng, ctx.lastPlace, { addressText: inputEl?.value });
			},
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