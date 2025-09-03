/*!
 * DropzoneHelper - Lightweight drag & drop file picker (vanilla JS)
 * - Works with ASP.NET Core Razor Pages and MVC (.NET 8)
 * - No dependencies; optional integration with ValidationHelper
 * - Manages: drag-over UI, browse button, merging dropped files, list rendering, removal
 *
 * Usage:
 *   const dz = DropzoneHelper.init({
 *     dropzone: "#dropzone",
 *     input: "#fileInput",
 *     list: "#fileList",
 *     browse: "#browseBtn",
 *     emptyListText: "No files selected",
 *     dragOverClass: "is-dragover",
 *     renderItem: (file, idx) => { ...return HTMLElement... },
 *     onChange: (files, ctx) => { ... },
 *     maxFiles: 10,
 *     maxSizeBytes: 10 * 1024 * 1024, // 10MB
 *     accept: ".png,.jpg,.jpeg,image/*", // fallback; defaults to input.accept
 *     deduplicate: true
 *   });
 */
(function (root, factory) {
	if (typeof define === "function" && define.amd) {
		define([], factory);
	} else if (typeof exports === "object") {
		module.exports = factory();
	} else {
		root.DropzoneHelper = factory();
	}
}(typeof self !== "undefined" ? self : this, function () {
	"use strict";

	const isEl = (x) => x instanceof Element;
	const toEl = (x) => isEl(x) ? x : document.querySelector(x);
	const toArray = (x) => Array.isArray(x) ? x : (x ? Array.from(x) : []);
	const noop = () => { };

	function parseAccept(accept) {
		return String(accept || "")
			.split(",")
			.map(s => s.trim())
			.filter(Boolean);
	}
	function fileMatchesAccept(file, acceptList) {
		if (!acceptList || acceptList.length === 0) return true;
		const name = (file.name || "").toLowerCase();
		const type = (file.type || "").toLowerCase();
		return acceptList.some(token => {
			token = token.toLowerCase();
			if (token === "*/*") return true;
			if (token.endsWith("/*")) {
				const prefix = token.slice(0, -1);
				return type.startsWith(prefix);
			}
			if (token.startsWith(".")) return name.endsWith(token);
			return type === token;
		});
	}
	function makeDataTransfer() {
		try { return new DataTransfer(); } catch { /* older Safari */ }
		try {
			const evt = new ClipboardEvent("paste");
			return evt.clipboardData || new DataTransfer();
		} catch {
			// Very old browsers won't support programmatic FileList replacement.
			// In such cases, we degrade gracefully by not mutating input.files.
			return null;
		}
	}

	function defaultRenderItem(file, idx) {
		const li = document.createElement("li");
		li.className = "dz-file";
		li.innerHTML = `
			<span class="dz-file-name">${file.name}</span>
			<button type="button" class="btn btn-outline-danger btn-sm" data-dz-remove="${idx}" aria-label="Remove ${file.name}">
				<i class="bi bi-x"></i>
			</button>`;
		return li;
	}

	function Dropzone(options) {
		const cfg = Object.assign({
			dropzone: null,     // required
			input: null,        // required
			list: null,         // required
			browse: null,       // optional
			clickToBrowse: false,
			dragOverClass: "is-dragover",
			emptyListText: "No files selected",
			renderItem: defaultRenderItem,
			onChange: noop,
			onFilesRejected: noop, // (rejectedFiles, reason) => {}
			maxFiles: null,
			maxSizeBytes: null,
			accept: null,       // if null, uses input.accept
			deduplicate: true,
			validateInputOnChange: true // calls ValidationHelper.validateFields on input
		}, options || {});

		if (!cfg.dropzone || !cfg.input || !cfg.list) {
			throw new Error("[DropzoneHelper] dropzone, input and list are required.");
		}

		const dz = toEl(cfg.dropzone);
		const input = toEl(cfg.input);
		const list = toEl(cfg.list);
		const browseBtn = cfg.browse ? toEl(cfg.browse) : null;
		if (!dz || !input || !list) throw new Error("[DropzoneHelper] One or more elements not found.");

		const acceptList = parseAccept(cfg.accept || input.getAttribute("accept") || "");
		const allowMultiple = input.hasAttribute("multiple");

		let destroyed = false;

		// Keep an internal snapshot so we can merge across browse selections.
		let modelFiles = Array.from(input.files || []);

		// Guard to avoid opening the picker twice (e.g., button + container click)
		let openingPicker = false;
		function openFilePicker() {
			if (openingPicker) return;
			openingPicker = true;
			try { input.click(); } finally {
				// Release the guard after change or a short timeout fallback
				const release = () => { openingPicker = false; input.removeEventListener("change", release); window.removeEventListener("focus", release); };
				input.addEventListener("change", release, { once: true });
				// Fallback in case focus doesn't return (some browsers)
				window.addEventListener("focus", release, { once: true });
				setTimeout(() => { openingPicker = false; }, 1500);
			}
		}

		function currentFiles() {
			return Array.from(modelFiles);
		}
		function setFiles(newFiles) {
			const dt = makeDataTransfer();
			const next = toArray(newFiles);
			if (!dt) {
				// Cannot mutate input.files programmatically; best-effort update of our model/UI.
				modelFiles = next;
				render();
				notifyChange();
				return;
			}
			next.forEach(f => dt.items.add(f));
			input.files = dt.files;
			modelFiles = Array.from(dt.files);
			render();
			notifyChange();
		}
		function addFiles(files) {
			const existing = currentFiles();
			const incoming = toArray(files);

			const rejected = [];
			const accepted = [];

			for (const f of incoming) {
				// reject by accept types
				if (acceptList.length && !fileMatchesAccept(f, acceptList)) {
					rejected.push({ file: f, reason: "type" }); continue;
				}
				// reject by size
				if (cfg.maxSizeBytes && f.size > cfg.maxSizeBytes) {
					rejected.push({ file: f, reason: "size" }); continue;
				}
				// dedupe by name+size+lastModified
				if (cfg.deduplicate) {
					const dup = existing.concat(accepted).some(x =>
						x.name === f.name && x.size === f.size && x.lastModified === f.lastModified);
					if (dup) continue;
				}
				accepted.push(f);
			}

			// enforce maxFiles
			let merged = existing.concat(accepted);
			if (cfg.maxFiles != null) {
				if (!allowMultiple) {
					merged = merged.slice(0, 1);
				} else if (merged.length > cfg.maxFiles) {
					const over = merged.length - cfg.maxFiles;
					rejected.push(...merged.slice(merged.length - over).map(f => ({ file: f, reason: "maxFiles" })));
					merged = merged.slice(0, cfg.maxFiles);
				}
			} else if (!allowMultiple) {
				merged = merged.slice(0, 1);
			}

			setFiles(merged);
			if (rejected.length && typeof cfg.onFilesRejected === "function") {
				try { cfg.onFilesRejected(rejected, "rejected"); } catch { /* no-op */ }
			}
		}
		function removeAt(index) {
			const files = currentFiles();
			if (index < 0 || index >= files.length) return;
			const dt = makeDataTransfer();
			if (!dt) return; // cannot set programmatically; no-op
			files.forEach((f, i) => { if (i !== index) dt.items.add(f); });
			input.files = dt.files;
			modelFiles = Array.from(dt.files);
			render();
			notifyChange();
		}
		function clear() {
			setFiles([]);
		}

		function render() {
			list.innerHTML = "";
			const files = currentFiles();
			if (files.length === 0) {
				const li = document.createElement("li");
				li.className = "dz-empty";
				li.textContent = cfg.emptyListText || "No files selected";
				list.appendChild(li);
				return;
			}
			files.forEach((f, idx) => {
				let node = null;
				try { node = cfg.renderItem(f, idx); } catch { /* ignore */ }
				if (!(node instanceof Element)) node = defaultRenderItem(f, idx);
				// Ensure index attribute for delegated removal
				if (!node.hasAttribute("data-dz-index")) node.setAttribute("data-dz-index", String(idx));
				list.appendChild(node);
			});
		}

		function notifyChange() {
			if (cfg.validateInputOnChange && window.ValidationHelper) {
				try { ValidationHelper.init(input.form || input).validateFields(input); } catch { /* ignore */ }
			}
			try { cfg.onChange(currentFiles(), api); } catch { /* ignore */ }
		}

		function preventDefaults(e) { e.preventDefault(); e.stopPropagation(); }

		function bind() {
			["dragenter", "dragover", "dragleave", "drop"].forEach(evt =>
				dz.addEventListener(evt, preventDefaults));
			["dragenter", "dragover"].forEach(evt =>
				dz.addEventListener(evt, () => dz.classList.add(cfg.dragOverClass)));
			["dragleave", "drop"].forEach(evt =>
				dz.addEventListener(evt, () => dz.classList.remove(cfg.dragOverClass)));

			dz.addEventListener("drop", (e) => {
				const dt = e.dataTransfer;
				if (!dt?.files?.length) return;
				addFiles(dt.files);
			});

			if (cfg.clickToBrowse) {
				dz.addEventListener("click", (e) => {
					// Avoid triggering when clicking remove buttons inside the list
					if (list.contains(e.target)) return;
					// Avoid double opening when clicking the browse button (event bubbles)
					if (browseBtn && (e.target === browseBtn || browseBtn.contains(e.target))) return;
					openFilePicker();
				});
			}
			if (browseBtn) {
				browseBtn.addEventListener("click", (e) => {
					e.preventDefault();
					e.stopPropagation(); // prevent dz click handler from firing too
					openFilePicker();
				});
			}
			input.addEventListener("change", () => {
				// Use what the browser set; apply filters/dedupe/max rules if needed
				if (acceptList.length || cfg.maxSizeBytes || cfg.deduplicate || (cfg.maxFiles != null) || !allowMultiple) {
					if (input.files && input.files.length) {
						addFiles(input.files);
					}
				} else {
					// No processing: just mirror what the browser set
					modelFiles = Array.from(input.files || []);
					render();
					notifyChange();
				}
			});

			// delegated removal
			list.addEventListener("click", (e) => {
				const btn = e.target.closest("[data-dz-remove]");
				if (!btn) return;
				// prefer explicit index attribute; fallback to parent data
				let idx = btn.getAttribute("data-dz-remove");
				if (idx == null) idx = btn.closest("[data-dz-index]")?.getAttribute("data-dz-index");
				const index = Number(idx);
				if (!Number.isNaN(index)) removeAt(index);
			});

			// Initial paint
			render();
		}

		function destroy() {
			if (destroyed) return;
			destroyed = true;
			try {
				dz.replaceWith(dz.cloneNode(true)); // quick unbind for drag events
			} catch { /* no-op */ }
			try { input.replaceWith(input.cloneNode(true)); } catch { /* no-op */ }
			try { list.replaceWith(list.cloneNode(false)); } catch { /* no-op */ }
		}

		const api = {
			addFiles,
			removeAt,
			clear,
			setFiles,
			getFiles: currentFiles,
			render,
			destroy,
			elements: { dropzone: dz, input, list, browse: browseBtn }
		};

		bind();
		return api;
	}

	return {
		init(opts) { return new Dropzone(opts); },
		initMany(selectorOrList, baseOptions) {
			const nodes = typeof selectorOrList === "string"
				? Array.from(document.querySelectorAll(selectorOrList))
				: toArray(selectorOrList);
			return nodes.map(node => new Dropzone(Object.assign({}, baseOptions, { dropzone: node })));
		}
	};
}));