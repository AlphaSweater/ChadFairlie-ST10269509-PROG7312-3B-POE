(function (global) {
	"use strict";

	function debounce(fn, wait = 200) {
		let t = 0;
		return function (...args) {
			clearTimeout(t);
			t = setTimeout(() => fn.apply(this, args), wait);
		};
	}

	function resolve(elOrSelector) {
		return typeof elOrSelector === "string" ? document.querySelector(elOrSelector) : elOrSelector;
	}

	function ensureBaseStyles() {
		const STYLE_ID = "ac-helper-base-styles";
		if (document.getElementById(STYLE_ID)) return;

		const css = `
/* === AutocompleteHelper functional/base styles (injected by JS) === */
.ac-helper-menu {
	position: absolute;         /* ensure dropdown anchors to input container */
	z-index: 2000;              /* floats above page chrome */
	display: none;              /* JS toggles visibility */
	max-height: 320px;          /* scroll when long */
	overflow-y: auto;           /* allow scrolling in the menu */
	overscroll-behavior: contain;
	box-sizing: border-box;     /* width/position computed from input rect */
}

.ac-helper-menu.is-open {
	display: block;
}

.ac-helper-item {
	cursor: pointer;
	user-select: none;
	white-space: normal;        /* allow wrapping if needed */
}
		`.trim();

		const style = document.createElement("style");
		style.id = STYLE_ID;
		style.type = "text/css";
		style.appendChild(document.createTextNode(css));
		document.head.appendChild(style);
	}

	class AutocompleteBox {
		constructor(options) {
			this.opts = Object.assign({
				inputEl: null,
				minLength: 3,
				autoFetchDetails: false,
				onSelect: null,
				fetch: (q) => MapService.AddressApi.autocomplete(q),
				getDetails: (placeId) => MapService.AddressApi.placeDetails(placeId),
				renderItem: null
			}, options || {});

			this.inputEl = resolve(this.opts.inputEl);
			if (!this.inputEl) throw new Error("AutocompleteHelper: inputEl is required.");

			ensureBaseStyles();

			this._host = null;
			this._items = [];
			this._index = -1;

			this._buildHost();
			this._wireInput();
		}

		_buildHost() {
			const host = document.createElement("div");
			host.className = "ac-helper-menu";
			host.setAttribute("role", "listbox");
			this._host = host;

			const parent = this.inputEl.parentElement || this.inputEl;
			if (getComputedStyle(parent).position === "static") {
				parent.style.position = "relative"; // functional: anchor dropdown positioning
			}
			parent.appendChild(host);

			const reposition = () => this._position();
			window.addEventListener("resize", reposition);
			window.addEventListener("scroll", reposition, true);
		}

		_position() {
			const r = this.inputEl.getBoundingClientRect();
			const pr = (this.inputEl.parentElement || document.body).getBoundingClientRect();
			this._host.style.left = `${r.left - pr.left}px`;
			this._host.style.top = `${r.bottom - pr.top + 4}px`;
			this._host.style.width = `${r.width}px`;
		}

		_wireInput() {
			this.inputEl.addEventListener("input", debounce(() => {
				const q = (this.inputEl.value || "").trim();
				if (q.length < this.opts.minLength) { this._hide(); return; }
				this.opts.fetch(q)
					.then(items => {
						if (!Array.isArray(items) || items.length === 0) { this._hide(); return; }
						this._render(items);
					})
					.catch(() => this._hide());
			}, 200));

			this.inputEl.addEventListener("keydown", (e) => {
				if (e.key === "ArrowDown") { e.preventDefault(); this._move(1); }
				else if (e.key === "ArrowUp") { e.preventDefault(); this._move(-1); }
				else if (e.key === "Enter") {
					if (this._index >= 0) { e.preventDefault(); this._choose(this._index); }
				} else if (e.key === "Escape") {
					this._hide();
				}
			});

			this.inputEl.addEventListener("blur", () => setTimeout(() => this._hide(), 150));
		}

		_render(items) {
			this._items = items;
			this._index = -1;
			this._host.innerHTML = "";
			this._position();

			items.forEach((it, idx) => {
				const el = document.createElement("div");
				el.className = "ac-helper-item";
				el.setAttribute("role", "option");
				el.dataset.index = String(idx);

				if (typeof this.opts.renderItem === "function") {
					const custom = this.opts.renderItem(it);
					if (custom instanceof HTMLElement) el.appendChild(custom);
					else el.innerHTML = custom;
				} else {
					const main = document.createElement("div");
					main.className = "ac-helper-item-main";
					main.textContent = it.mainText || it.description || "";

					const secondary = document.createElement("small");
					secondary.className = "ac-helper-item-secondary";
					secondary.textContent = it.secondaryText || "";

					el.appendChild(main);
					if (secondary.textContent) el.appendChild(secondary);
				}

				el.addEventListener("mouseenter", () => this._highlight(idx));
				el.addEventListener("mouseleave", () => this._highlight(-1));
				el.addEventListener("mousedown", (e) => e.preventDefault());
				el.addEventListener("click", () => this._choose(idx));
				this._host.appendChild(el);
			});

			this._host.classList.add("is-open");
			this._host.style.display = "block"; // functional: ensure visibility even if theme overrides
		}

		_move(delta) {
			if (!this._items.length) return;
			let idx = this._index + delta;
			if (idx < 0) idx = this._items.length - 1;
			if (idx >= this._items.length) idx = 0;
			this._highlight(idx);
		}

		_highlight(index) {
			const children = Array.from(this._host.children);
			children.forEach((el, i) => {
				el.classList.toggle("is-active", i === index);
			});
			this._index = index;
		}

		_hide() {
			this._host.classList.remove("is-open");
			this._host.style.display = "none"; // functional: hide via inline style
			this._host.innerHTML = "";
			this._items = [];
			this._index = -1;
		}

		_choose(index) {
			const suggestion = this._items[index];
			this._hide();
			if (!suggestion) return;

			this.inputEl.value = suggestion.description || suggestion.mainText || "";

			if (this.opts.autoFetchDetails && suggestion.placeId) {
				this.opts.getDetails(suggestion.placeId)
					.then(details => this._emitSelect(suggestion, details))
					.catch(() => this._emitSelect(suggestion, null));
			} else {
				this._emitSelect(suggestion, null);
			}
		}

		_emitSelect(suggestion, details) {
			if (typeof this.opts.onSelect === "function") {
				this.opts.onSelect({ suggestion, details });
			}
		}
	}

	function create(options) {
		return new AutocompleteBox(options);
	}

	global.AutocompleteHelper = { create };
})(window);