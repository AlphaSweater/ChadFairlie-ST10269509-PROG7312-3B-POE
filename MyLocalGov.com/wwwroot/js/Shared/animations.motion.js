// animations.motion.js
// Movement + layout-based animations (Anime.js powered)

(function () {
	if (typeof window === "undefined") return;
	if (!window.anime) {
		console.warn("Anime.js not found. animations.motion.js requires Anime.js.");
		return;
	}

	var defaults = {
		duration: 700,
		easing: "cubicBezier(.25,.8,.25,1)"
	};

	function _resolve(el) {
		if (!el) return null;
		if (typeof el === "string") return document.querySelector(el);
		return el instanceof Element ? el : null;
	}

	function cancelOn(el) {
		var e = _resolve(el);
		if (e) anime.remove(e);
	}

	function fadeIn(target, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		cancelOn(el);
		anime.set(el, { opacity: 0 });
		return anime({
			targets: el,
			opacity: 1,
			duration: cfg.duration,
			easing: cfg.easing
		}).finished;
	}

	function fadeOut(target, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		cancelOn(el);
		anime.set(el, { opacity: 1 });
		return anime({
			targets: el,
			opacity: 0,
			duration: cfg.duration,
			easing: cfg.easing
		}).finished;
	}

	function slideInFrom(target, direction, opts) {
		var el = _resolve(target);
		if (!el) return Promise.resolve();
		var cfg = Object.assign({}, defaults, opts);
		cancelOn(el);
		var distance = cfg.distance || 100;
		var from = { x: 0, y: 0 };
		if (direction === "left") from.x = -distance;
		if (direction === "right") from.x = distance;
		if (direction === "up") from.y = -distance;
		if (direction === "down") from.y = distance;
		anime.set(el, { translateX: from.x, translateY: from.y, opacity: 0 });
		return anime({
			targets: el,
			translateX: 0,
			translateY: 0,
			opacity: 1,
			duration: cfg.duration,
			easing: cfg.easing
		}).finished;
	}

	// Expose
	window.Animations = Object.assign({}, window.Animations, {
		fadeIn,
		fadeOut,
		slideInFrom,
		cancelOn
	});
})();
