// animations-helper-global.js
// Production-ready animation helpers for Anime.js (global usage)
// Usage: <script src="anime.min.js"></script>
//        <script src="animations-helper-global.js"></script>
//        window.Animations.moveWithClone(...)

(function () {

	if (typeof window === "undefined") throw new Error("Animations requires a browser environment");
	if (!window.anime) {
		console.warn("Anime.js not detected (window.anime). Animations will not work.");
		return;
	}

	// Default config
	var defaults = {
		duration: 700,
		easing: "cubicBezier(.25,.8,.25,1)",
		cloneZIndex: 9999,
		clonePointerEvents: "none",
		clonePosition: "absolute",
		willChange: true,
		disableTransformsOnParentsDepth: 2
	};

	// Helper: resolve element or selector
	function _resolve(el) {
		if (!el) return null;
		if (typeof el === "string") return document.querySelector(el);
		return el instanceof Element ? el : null;
	}

	// Helper: set will-change if configured
	function _setWillChange(el, val) {
		if (!defaults.willChange || !el) return;
		el.style.willChange = val ? "transform, opacity" : "";
	}

	// Helper: cancel anime.js animations for an element
	function cancelOn(el) {
		var e = _resolve(el);
		if (!e) return;
		anime.remove(e);
	}

	// Clone-based FLIP
	function moveWithClone(target, layoutChangeFn, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		var first = el.getBoundingClientRect();
		var clone = el.cloneNode(true);
		clone.style.position = cfg.clonePosition;
		clone.style.top = first.top + "px";
		clone.style.left = first.left + "px";
		clone.style.width = first.width + "px";
		clone.style.height = first.height + "px";
		clone.style.margin = "0";
		clone.style.pointerEvents = cfg.clonePointerEvents;
		clone.style.zIndex = String(cfg.cloneZIndex);
		clone.style.transformOrigin = "top left";
		clone.style.boxSizing = "border-box";
		clone.style.display = getComputedStyle(el).display === "inline" ? "inline-block" : getComputedStyle(el).display;
		clone.style.transform = "none";
		document.body.appendChild(clone);
		var prevVisibility = el.style.visibility;
		el.style.visibility = "hidden";
		layoutChangeFn && layoutChangeFn();
		var last = el.getBoundingClientRect();
		var dx = last.left - first.left;
		var dy = last.top - first.top;
		var scaleX = first.width === 0 ? 1 : last.width / first.width;
		var scaleY = first.height === 0 ? 1 : last.height / first.height;
		return new Promise(function (resolve) {
			_setWillChange(clone, true);
			anime({
				targets: clone,
				translateX: dx,
				translateY: dy,
				scaleX: scaleX,
				scaleY: scaleY,
				duration: cfg.duration,
				easing: cfg.easing,
				complete: function () {
					clone.remove();
					el.style.visibility = prevVisibility || "";
					_setWillChange(null, false);
					resolve();
				}
			});
		});
	}

	// In-place FLIP
	function moveInPlace(target, layoutChangeFn, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		cancelOn(el);
		var first = el.getBoundingClientRect();
		var parents = [];
		var p = el.parentElement;
		for (var i = 0; i < cfg.disableTransformsOnParentsDepth && p; i++, p = p.parentElement) {
			parents.push(p);
		}
		var originalTransitions = new Map();
		[el].concat(parents).forEach(function (node) {
			originalTransitions.set(node, node.style.transition || "");
			node.style.transition = "none";
		});
		layoutChangeFn && layoutChangeFn();
		var last = el.getBoundingClientRect();
		var dx = first.left - last.left;
		var dy = first.top - last.top;
		if (dx === 0 && dy === 0) {
			[el].concat(parents).forEach(function (node) {
				node.style.transition = originalTransitions.get(node) || "";
			});
			return Promise.resolve();
		}
		anime.set(el, { translateX: dx, translateY: dy });
		el.offsetHeight;
		return new Promise(function (resolve) {
			requestAnimationFrame(function () {
				_setWillChange(el, true);
				anime({
					targets: el,
					translateX: 0,
					translateY: 0,
					duration: cfg.duration,
					easing: cfg.easing,
					complete: function () {
						el.style.transform = "";
						_setWillChange(el, false);
						[el].concat(parents).forEach(function (node) {
							node.style.transition = originalTransitions.get(node) || "";
						});
						resolve();
					}
				});
			});
		});
	}

	// Grouped move with clone
	function groupMoveWithClone(containerSelectorOrEl, layoutChangeFn, opts) {
		var container = _resolve(containerSelectorOrEl);
		if (!container) return Promise.resolve();
		return moveWithClone(container, layoutChangeFn, opts);
	}

	// Fade helpers
	function fadeIn(target, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		cancelOn(el);
		if (cfg.layoutChangeFn) cfg.layoutChangeFn();
		_setWillChange(el, true);
		anime.set(el, { opacity: 0 });
		return new Promise(function (resolve) {
			anime({
				targets: el,
				opacity: 1,
				duration: cfg.duration,
				easing: cfg.easing,
				complete: function () {
					_setWillChange(el, false);
					resolve();
				}
			});
		});
	}

	function fadeOut(target, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		cancelOn(el);
		if (cfg.layoutChangeFn) cfg.layoutChangeFn();
		_setWillChange(el, true);
		anime.set(el, { opacity: 1 });
		return new Promise(function (resolve) {
			anime({
				targets: el,
				opacity: 0,
				duration: cfg.duration,
				easing: cfg.easing,
				complete: function () {
					_setWillChange(el, false);
					resolve();
				}
			});
		});
	}

	// Simple slide helpers
	function slideInFrom(target, direction, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		cancelOn(el);
		if (cfg.layoutChangeFn) cfg.layoutChangeFn();
		_setWillChange(el, true);
		var from = { x: 0, y: 0 };
		var distance = opts && opts.distance || 100;
		if (direction === "left") from.x = -distance;
		if (direction === "right") from.x = distance;
		if (direction === "up") from.y = -distance;
		if (direction === "down") from.y = distance;
		anime.set(el, { translateX: from.x, translateY: from.y, opacity: 0 });
		return new Promise(function (resolve) {
			anime({
				targets: el,
				translateX: 0,
				translateY: 0,
				opacity: 1,
				duration: cfg.duration,
				easing: cfg.easing,
				complete: function () { _setWillChange(el, false); resolve(); }
			});
		});
	}

	// Sequence runner
	function runSequence(tasks) {
		tasks = tasks || [];
		return tasks.reduce(function (p, task) { return p.then(function () { return task(); }); }, Promise.resolve());
	}

	// Config helpers
	function setDefaults(newDefaults) {
		defaults = Object.assign({}, defaults, newDefaults);
	}

	// Expose API globally
	window.Animations = {
		moveWithClone: moveWithClone,
		moveInPlace: moveInPlace,
		groupMoveWithClone: groupMoveWithClone,
		fadeIn: fadeIn,
		fadeOut: fadeOut,
		slideInFrom: slideInFrom,
		cancelOn: cancelOn,
		runSequence: runSequence,
		setDefaults: setDefaults
	};
})();

/*
 ============================
   Animations3.js Usage Examples & API Explanation
   ============================
   This file exposes a global `window.Animations` object with advanced animation helpers.
   You must include Anime.js first:
	 <script src="https://cdn.jsdelivr.net/npm/animejs@3.2.1/lib/anime.min.js"></script>
	 <script src="animations-helper-global.js"></script>

   --- API Overview ---
   All animation helpers accept an optional `layoutChangeFn` in their options object. This function is called before the animation starts, allowing you to trigger visibility/layout changes for the target element.

   Animations.moveWithClone(target, layoutChangeFn, opts)
	 // FLIP animation using a visual clone. Animates an element from its old position/size to new after a layout change.
	 // Example:
	 Animations.moveWithClone("#logo", function() {
	   document.getElementById("logo").classList.add("small");
	 }, { duration: 900 });

   Animations.moveInPlace(target, layoutChangeFn, opts)
	 // FLIP animation without a clone. Animates the element itself from old to new position.
	 // Example:
	 Animations.moveInPlace("#panel", function() {
	   document.getElementById("panel").style.left = "200px";
	 }, { duration: 600 });

   Animations.groupMoveWithClone(container, layoutChangeFn, opts)
	 // Like moveWithClone, but animates a container and its children as a group.
	 // Example:
	 Animations.groupMoveWithClone("#header", function() {
	   document.getElementById("header").classList.toggle("collapsed");
	 }, { duration: 800 });

   Animations.fadeIn(target, opts)
	 // Fades in an element (opacity 0 → 1).
	 // Example:
	 Animations.fadeIn("#loginForm", { duration: 400, layoutChangeFn: () => showForm("login") });

   Animations.fadeOut(target, opts)
	 // Fades out an element (opacity 1 → 0).
	 // Example:
	 Animations.fadeOut("#modal", { duration: 400, layoutChangeFn: () => hideModal() });

   Animations.slideInFrom(target, direction, opts)
	 // Slides an element in from a direction ("left", "right", "up", "down").
	 // Example:
	 Animations.slideInFrom("#sidebar", "left", { distance: 200, duration: 500, layoutChangeFn: () => showSidebar() });

   Animations.runSequence([fn1, fn2, ...])
	 // Runs an array of animation functions sequentially (each returns a Promise).
	 // Example:
	 Animations.runSequence([
	   function() { return Animations.moveWithClone("#logo", changeFn, { duration: 700 }); },
	   function() { return Animations.fadeIn("#loginForm", { duration: 400 }); }
	 ]);

   Animations.cancelOn(target)
	 // Cancels any running Anime.js animations on the target element.
	 // Example:
	 Animations.cancelOn("#panel");

   Animations.setDefaults({ ... })
	 // Sets global default options for all animations.
	 // Example:
	 Animations.setDefaults({ duration: 1000, easing: "easeInOutQuad" });

   --- Notes ---
   - All selectors can be CSS strings ("#id", ".class") or DOM elements.
   - All animation methods return Promises for easy chaining.
   - You can combine these helpers for complex UI transitions.
   - For best results, use with Anime.js v3+.
  */