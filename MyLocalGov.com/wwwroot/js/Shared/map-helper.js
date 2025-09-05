(function (global) {
	"use strict";

	let mapsPromise = null;

	function discoverApiKey(explicitKey) {
		if (explicitKey) return explicitKey;
		if (global.__gmapsKey) return global.__gmapsKey;
		const meta = document.querySelector('meta[name="google-maps-api-key"]');
		return meta?.content || "";
	}

	function loadGoogleMaps(apiKey, options = {}) {
		const resolvedKey = discoverApiKey(apiKey);
		if (global.google && global.google.maps) return Promise.resolve(global.google.maps);
		if (mapsPromise) return mapsPromise;

		const params = new URLSearchParams({
			key: resolvedKey || "",
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

	const Dom = {
		resolve(elOrSelector) {
			if (!elOrSelector) return null;
			return typeof elOrSelector === "string" ? document.querySelector(elOrSelector) : elOrSelector;
		},
		setValue(el, val) {
			if (!el) return;
			el.value = val ?? "";
		},
		toFixedOr(val, digits = 6) {
			const n = Number(val);
			return Number.isFinite(n) ? n.toFixed(digits) : "";
		}
	};

	class MapPicker {
		constructor(options) {
			if (!global.google || !global.google.maps) {
				throw new Error("MapHelper.init: Google Maps JS API is not loaded. Call MapHelper.loadGoogleMaps() first.");
			}

			this.mapEl = Dom.resolve(options.map);
			this.inputEl = /** @type {HTMLInputElement} */(Dom.resolve(options.input));
			this.latEl = /** @type {HTMLInputElement} */(Dom.resolve(options.latInput));
			this.lngEl = /** @type {HTMLInputElement} */(Dom.resolve(options.lngInput));
			this.addressRefs = {
				street: Dom.resolve(options.addressFields?.street),
				suburb: Dom.resolve(options.addressFields?.suburb),
				city: Dom.resolve(options.addressFields?.city),
				postalCode: Dom.resolve(options.addressFields?.postalCode)
			};

			if (!this.mapEl) throw new Error("MapHelper.init: 'map' element is required.");
			if (!this.inputEl) throw new Error("MapHelper.init: 'input' element is required.");

			this.opts = {
				zoom: options.zoom ?? 13,
				pickZoom: options.pickZoom ?? 15,
				draggable: options.draggable !== false,
				onChange: typeof options.onChange === "function" ? options.onChange : null
			};

			const maps = global.google.maps;
			const center = options.defaultCenter || { lat: -33.9249, lng: 18.4241 };

			this.map = new maps.Map(this.mapEl, {
				center,
				zoom: this.opts.zoom,
				mapTypeControl: false, streetViewControl: false, fullscreenControl: false
			});
			this.marker = new maps.Marker({ map: this.map, position: center, draggable: this.opts.draggable });

			this.lastPlace = null;

			if (this.latEl) this.latEl.value = Dom.toFixedOr(center.lat);
			if (this.lngEl) this.lngEl.value = Dom.toFixedOr(center.lng);
			this._emitChange(center.lat, center.lng, null);

			this._wireMapInteractions();
			this._wireManualGeocode();
		}

		_emitChange(lat, lng, place, extra = {}) {
			if (this.latEl) this.latEl.value = Dom.toFixedOr(lat);
			if (this.lngEl) this.lngEl.value = Dom.toFixedOr(lng);
			if (this.opts.onChange) {
				this.opts.onChange({
					lat, lng, place: place || this.lastPlace || null,
					map: this.map, marker: this.marker, input: this.inputEl,
					...extra
				});
			}
		}

		_applyServerResult(data, { setInput = true } = {}) {
			const lat = Number(data?.lat);
			const lng = Number(data?.lng);

			const p = data?.parts || {};
			Dom.setValue(this.addressRefs.street, p.street || "");
			Dom.setValue(this.addressRefs.suburb, p.suburb || "");
			Dom.setValue(this.addressRefs.city, p.city || "");
			Dom.setValue(this.addressRefs.postalCode, p.postalCode || "");

			if (setInput && this.inputEl) {
				const formatted = data?.formattedAddress || p.formatted || "";
				this.inputEl.value = formatted?.trim()?.length ? formatted : `${Dom.toFixedOr(lat)}, ${Dom.toFixedOr(lng)}`;
			}

			this._emitChange(lat, lng, this.lastPlace);
		}

		_wireMapInteractions() {
			this.map.addListener("click", (e) => {
				const lat = e.latLng.lat();
				const lng = e.latLng.lng();
				this.marker.setPosition(e.latLng);
				this.map.setCenter(e.latLng);
				this.map.setZoom(this.opts.pickZoom || 15);
				this._serverReverseGeocode(lat, lng);
			});

			this.marker.addListener("dragend", () => {
				const pos = this.marker.getPosition();
				const lat = pos.lat();
				const lng = pos.lng();
				this.map.setCenter(pos);
				this.map.setZoom(this.opts.pickZoom || 15);
				this._serverReverseGeocode(lat, lng);
			});
		}

		_wireManualGeocode() {
			this.inputEl.addEventListener("keydown", (e) => {
				if (e.key === "Enter") {
					e.preventDefault();
					const q = (this.inputEl.value || "").trim();
					if (q.length === 0) return;
					this._geocodeText(q);
				}
			});
			this.inputEl.addEventListener("blur", () => {
				const q = (this.inputEl.value || "").trim();
				if (q.length === 0) return;
				setTimeout(() => {
					this._geocodeText(q);
				}, 150);
			});
		}

		_serverReverseGeocode(lat, lng) {
			return fetch("/api/address/reverse-geocode", {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify({ lat, lng })
			})
				.then(r => r.ok ? r.json() : Promise.reject(r))
				.then(data => this._applyServerResult(data, { setInput: true }))
				.catch(err => {
					console.warn("reverse-geocode failed:", err);
					if (this.inputEl) this.inputEl.value = `${Dom.toFixedOr(lat)}, ${Dom.toFixedOr(lng)}`;
					this._emitChange(lat, lng, this.lastPlace);
				});
		}

		_geocodeText(query) {
			return fetch("/api/address/geocode-text", {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify({ query })
			})
				.then(r => r.ok ? r.json() : Promise.reject(r))
				.then(data => {
					const lat = Number(data?.lat);
					const lng = Number(data?.lng);
					// Only center if we have a real match (avoid (0,0) no-result centering)
					if (Number.isFinite(lat) && Number.isFinite(lng) && !(lat === 0 && lng === 0)) {
						const maps = global.google.maps;
						const pos = new maps.LatLng(lat, lng);
						this.marker.setPosition(pos);
						this.map.setCenter(pos);
						this.map.setZoom(this.opts.pickZoom || 15);
					}
					this._applyServerResult(data, { setInput: true });
				})
				.catch(err => {
					console.warn("geocode-text failed:", err);
				});
		}

		setPosition(lat, lng, { pan = true, zoom = this.opts.pickZoom || 15, reverseGeocodeOnPick = true } = {}) {
			const maps = global.google.maps;
			const pos = new maps.LatLng(lat, lng);
			this.marker.setPosition(pos);
			if (pan) this.map.setCenter(pos);
			if (zoom) this.map.setZoom(zoom);

			if (reverseGeocodeOnPick) {
				this._serverReverseGeocode(lat, lng);
			} else {
				this._emitChange(lat, lng, this.lastPlace);
			}
		}

		getPosition() {
			const p = this.marker.getPosition();
			return { lat: p.lat(), lng: p.lng() };
		}

		validateNow() {
			const p = this.marker.getPosition();
			return this._serverReverseGeocode(p.lat(), p.lng());
		}

		destroy() {
			const g = global.google.maps;
			g.event.clearInstanceListeners(this.marker);
			g.event.clearInstanceListeners(this.map);
			this.marker.setMap(null);
		}
	}

	function init(options) {
		if (!global.google || !global.google.maps) {
			throw new Error("MapHelper.init: Google Maps JS API is not loaded. Call MapHelper.loadGoogleMaps() first.");
		}
		const picker = new MapPicker(options);
		return {
			getPosition: () => picker.getPosition(),
			setPosition: (lat, lng, opts) => picker.setPosition(lat, lng, opts || {}),
			validateNow: () => picker.validateNow(),
			destroy: () => picker.destroy()
		};
	}

	global.MapHelper = {
		loadGoogleMaps,
		init
	};
})(window);