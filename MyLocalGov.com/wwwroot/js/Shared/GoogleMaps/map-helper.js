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

	class MapWidget {
		constructor(opts) {
			if (!global.google || !global.google.maps) {
				throw new Error("MapUI: Google Maps JS API not loaded. Call MapUI.loadGoogleMaps() first.");
			}
			this.opts = Object.assign({
				pickZoom: 15,
				draggable: true,
				onChange: null
			}, opts || {});

			this.mapEl = Dom.resolve(this.opts.mapEl);
			this.inputEl = Dom.resolve(this.opts.inputEl);
			this.coordsLabelEl = Dom.resolve(this.opts.coordsLabelEl);
			this.latInputEl = Dom.resolve(this.opts.latInputEl);
			this.lngInputEl = Dom.resolve(this.opts.lngInputEl);
			if (!this.mapEl || !this.inputEl) throw new Error("MapUI: mapEl and inputEl are required.");

			const maps = global.google.maps;
			const center = this.opts.defaultCenter || { lat: -33.9249, lng: 18.4241 };

			this.map = new maps.Map(this.mapEl, {
				center,
				zoom: this.opts.zoom || 13,
				mapTypeControl: false, streetViewControl: false, fullscreenControl: false
			});
			this.marker = new maps.Marker({ map: this.map, position: center, draggable: this.opts.draggable });

			if (this.latInputEl) this.latInputEl.value = Dom.toFixedOr(center.lat);
			if (this.lngInputEl) this.lngInputEl.value = Dom.toFixedOr(center.lng);
			this._emitChange(center.lat, center.lng);

			this._wireMapInteractions();
		}

		_emitChange(lat, lng) {
			if (this.latInputEl) this.latInputEl.value = Dom.toFixedOr(lat);
			if (this.lngInputEl) this.lngInputEl.value = Dom.toFixedOr(lng);

			if (this.coordsLabelEl) {
				const latFixed = Dom.toFixedOr(lat);
				const lngFixed = Dom.toFixedOr(lng);
				if (latFixed && lngFixed) {
					this.coordsLabelEl.textContent = `Coordinates: ${latFixed}, ${lngFixed}`;
					this.coordsLabelEl.classList.remove("text-danger");
					this.coordsLabelEl.classList.add("text-muted");
				} else {
					this.coordsLabelEl.textContent = "Coordinates: UNKNOWN";
					this.coordsLabelEl.classList.add("text-danger");
				}
			}

			if (typeof this.opts.onChange === "function") {
				this.opts.onChange({ lat, lng, map: this.map, marker: this.marker, input: this.inputEl });
			}
		}

		_applyResultToUI(data, { setInput = true } = {}) {
			const lat = Number(data?.lat);
			const lng = Number(data?.lng);

			if (Number.isFinite(lat) && Number.isFinite(lng)) {
				const maps = global.google.maps;
				const pos = new maps.LatLng(lat, lng);
				this.marker.setPosition(pos);
				this.map.setCenter(pos);
				this.map.setZoom(this.opts.pickZoom || 15);
			}

			if (setInput && this.inputEl) {
				const formatted = (data?.formattedAddress || data?.parts?.formatted || "").trim();
				this.inputEl.value = formatted.length ? formatted : `${Dom.toFixedOr(lat)}, ${Dom.toFixedOr(lng)}`;
			}

			this._emitChange(lat, lng);
		}

		_wireMapInteractions() {
			this.map.addListener("click", (e) => {
				const lat = e.latLng.lat();
				const lng = e.latLng.lng();
				this.marker.setPosition(e.latLng);
				this.map.setCenter(e.latLng);
				this.map.setZoom(this.opts.pickZoom || 15);

				MapService.AddressApi.reverseGeocode(lat, lng)
					.then(data => this._applyResultToUI(data, { setInput: true }))
					.catch(() => {
						if (this.inputEl) this.inputEl.value = `${Dom.toFixedOr(lat)}, ${Dom.toFixedOr(lng)}`;
						this._emitChange(lat, lng);
					});
			});

			this.marker.addListener("dragend", () => {
				const pos = this.marker.getPosition();
				const lat = pos.lat();
				const lng = pos.lng();
				this.map.setCenter(pos);
				this.map.setZoom(this.opts.pickZoom || 15);

				MapService.AddressApi.reverseGeocode(lat, lng)
					.then(data => this._applyResultToUI(data, { setInput: true }))
					.catch(() => {
						if (this.inputEl) this.inputEl.value = `${Dom.toFixedOr(lat)}, ${Dom.toFixedOr(lng)}`;
						this._emitChange(lat, lng);
					});
			});
		}

		// Public API
		setPosition(lat, lng, { pan = true, zoom = this.opts.pickZoom || 15, reverseGeocodeOnPick = true } = {}) {
			const maps = global.google.maps;
			const pos = new maps.LatLng(lat, lng);
			this.marker.setPosition(pos);
			if (pan) this.map.setCenter(pos);
			if (zoom) this.map.setZoom(zoom);
			if (reverseGeocodeOnPick) {
				MapService.AddressApi.reverseGeocode(lat, lng)
					.then(d => this._applyResultToUI(d, { setInput: true }))
					.catch(() => this._emitChange(lat, lng));
			} else {
				this._emitChange(lat, lng);
			}
		}

		getPosition() {
			const p = this.marker.getPosition();
			return { lat: p.lat(), lng: p.lng() };
		}

		validateNow() {
			const p = this.marker.getPosition();
			return MapService.AddressApi.reverseGeocode(p.lat(), p.lng()).then(d => this._applyResultToUI(d, { setInput: true }));
		}

		destroy() {
			const g = global.google.maps;
			g.event.clearInstanceListeners(this.marker);
			g.event.clearInstanceListeners(this.map);
			this.marker.setMap(null);
		}
	}

	global.MapUI = {
		loadGoogleMaps,
		createWidget: (options) => new MapWidget(options)
	};
})(window);