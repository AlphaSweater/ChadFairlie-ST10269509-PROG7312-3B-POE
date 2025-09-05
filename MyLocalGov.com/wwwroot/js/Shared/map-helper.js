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

	function debounce(fn, wait = 250) {
		let t = 0;
		return function (...args) {
			clearTimeout(t);
			t = setTimeout(() => fn.apply(this, args), wait);
		};
	}

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

			// Resolve elements
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

			this._buildSuggestionsUI();
			this._wireMapInteractions();
			this._wireSearchBox();
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

		_wireSearchBox() {
			// Keyboard navigation + Enter handling
			this.inputEl.addEventListener("keydown", (e) => {
				if (e.key === "ArrowDown") { e.preventDefault(); this._moveSelection(1); return; }
				if (e.key === "ArrowUp") { e.preventDefault(); this._moveSelection(-1); return; }
				if (e.key === "Escape") { this._hideSuggestions(); return; }
				if (e.key === "Enter") {
					e.preventDefault();
					if (this._selectHighlighted()) return;
					const q = (this.inputEl.value || "").trim();
					if (!q) return;
					MapService.AddressApi.geocodeText(q)
						.then(d => this._applyResultToUI(d, { setInput: true }))
						.catch(() => { /* ignore */ });
				}
			});

			// Debounced autocomplete
			this.inputEl.addEventListener("input", debounce(() => {
				const q = (this.inputEl.value || "").trim();
				if (q.length < 3) { this._hideSuggestions(); return; }
				MapService.AddressApi.autocomplete(q)
					.then(items => {
						if (!Array.isArray(items) || items.length === 0) { this._hideSuggestions(); return; }
						this._renderSuggestions(items);
					})
					.catch(() => this._hideSuggestions());
			}, 200));

			// Hide after blur (allow click to register)
			this.inputEl.addEventListener("blur", () => setTimeout(() => this._hideSuggestions(), 200));
		}

		// Suggestions UI
		_buildSuggestionsUI() {
			const container = document.createElement("div");
			container.className = "mapui-suggest";
			container.style.position = "absolute";
			container.style.zIndex = "1000";
			container.style.background = "#fff";
			container.style.border = "1px solid rgba(0,0,0,0.15)";
			container.style.borderRadius = "0.25rem";
			container.style.boxShadow = "0 4px 12px rgba(0,0,0,0.08)";
			container.style.padding = "4px 0";
			container.style.display = "none";
			container.setAttribute("role", "listbox");

			this._sugHost = container;
			this._sugItems = [];
			this._sugIndex = -1;

			const parent = this.inputEl.parentElement || this.inputEl;
			parent.style.position = "relative";
			parent.appendChild(container);

			const reposition = () => this._positionSuggestions();
			window.addEventListener("resize", reposition);
			window.addEventListener("scroll", reposition, true);
		}

		_positionSuggestions() {
			const r = this.inputEl.getBoundingClientRect();
			const pr = (this.inputEl.parentElement || document.body).getBoundingClientRect();
			const left = r.left - pr.left;
			const top = r.bottom - pr.top + 4;
			this._sugHost.style.left = `${left}px`;
			this._sugHost.style.top = `${top}px`;
			this._sugHost.style.width = `${r.width}px`;
		}

		_renderSuggestions(items) {
			this._sugItems = items;
			this._sugIndex = -1;
			this._sugHost.innerHTML = "";
			this._positionSuggestions();

			items.forEach((it, idx) => {
				const div = document.createElement("div");
				div.className = "mapui-suggest-item";
				div.style.padding = "6px 10px";
				div.style.cursor = "pointer";
				div.style.display = "flex";
				div.style.flexDirection = "column";
				div.setAttribute("role", "option");
				div.dataset.index = String(idx);

				const main = document.createElement("span");
				main.textContent = it.mainText || it.description || "";
				main.style.fontWeight = "500";

				const secondary = document.createElement("small");
				secondary.textContent = it.secondaryText || "";
				secondary.style.color = "#6c757d";

				div.appendChild(main);
				if (secondary.textContent) div.appendChild(secondary);

				div.addEventListener("mouseenter", () => { this._highlight(idx); });
				div.addEventListener("mouseleave", () => { this._highlight(-1); });
				div.addEventListener("mousedown", (e) => e.preventDefault());
				div.addEventListener("click", () => this._choose(idx));

				this._sugHost.appendChild(div);
			});

			this._sugHost.style.display = "block";
		}

		_hideSuggestions() {
			this._sugHost.style.display = "none";
			this._sugHost.innerHTML = "";
			this._sugItems = [];
			this._sugIndex = -1;
		}

		_highlight(index) {
			const children = Array.from(this._sugHost.children);
			children.forEach((el, i) => {
				el.style.background = i === index ? "rgba(25,135,84,0.08)" : "transparent";
			});
			this._sugIndex = index;
		}

		_moveSelection(delta) {
			if (!this._sugItems.length) return;
			let idx = this._sugIndex + delta;
			if (idx < 0) idx = this._sugItems.length - 1;
			if (idx >= this._sugItems.length) idx = 0;
			this._highlight(idx);
		}

		_selectHighlighted() {
			if (this._sugIndex < 0 || this._sugIndex >= this._sugItems.length) return false;
			this._choose(this._sugIndex);
			return true;
		}

		_choose(index) {
			const it = this._sugItems[index];
			this._hideSuggestions();
			if (!it || !it.placeId) return;

			MapService.AddressApi.placeDetails(it.placeId)
				.then(d => this._applyResultToUI(d, { setInput: true }))
				.catch(() => { /* ignore */ });
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

		destroy() {
			const g = global.google.maps;
			g.event.clearInstanceListeners(this.marker);
			g.event.clearInstanceListeners(this.map);
			this.marker.setMap(null);
		}
	}

	function createWidget(options) {
		return new MapWidget(options);
	}

	global.MapUI = {
		loadGoogleMaps,
		createWidget
	};
})(window);