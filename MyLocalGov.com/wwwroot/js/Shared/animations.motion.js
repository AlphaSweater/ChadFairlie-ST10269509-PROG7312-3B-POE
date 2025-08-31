// animations.motion.js
// Simple, unified animation helpers (Anime.js required)

(function () {
	if (typeof window === "undefined" || !window.anime) {
		console.warn("Anime.js not found. animations.motion.js requires Anime.js.");
		return;
	}

	const defaults = {
		duration: 700,
		easing: "cubicBezier(.25,.8,.25,1)",
		distance: 100
	};

	// Helper: resolve element
	const resolve = el =>
		typeof el === "string" ? document.querySelector(el) : el instanceof Element ? el : null;

	// Unified animate function
	function animate(type, target, opts = {}) {
		const el = resolve(target);
		if (!el) return Promise.resolve();

		const cfg = { ...defaults, ...opts };
		window.anime.remove(el);

		switch (type) {
			case "fadeIn":
				el.style.opacity = 0;
				return window.anime({
					targets: el,
					opacity: 1,
					duration: cfg.duration,
					easing: cfg.easing
				}).finished;

			case "fadeOut":
				el.style.opacity = 1;
				return window.anime({
					targets: el,
					opacity: 0,
					duration: cfg.duration,
					easing: cfg.easing
				}).finished;

			case "slideIn":
				const { direction = "left", distance = cfg.distance } = cfg;
				let x = 0, y = 0;
				if (direction === "left") x = -distance;
				if (direction === "right") x = distance;
				if (direction === "up") y = -distance;
				if (direction === "down") y = distance;
				window.anime.set(el, { translateX: x, translateY: y, opacity: 0 });
				return window.anime({
					targets: el,
					translateX: 0,
					translateY: 0,
					opacity: 1,
					duration: cfg.duration,
					easing: cfg.easing
				}).finished;

			case "slideOut":
				const slideOpts = cfg;
				const { direction: slideDirection = "left", distance: slideDistance = cfg.distance } = slideOpts;
				let targetX = 0, targetY = 0;
				if (slideDirection === "left") targetX = -slideDistance;
				if (slideDirection === "right") targetX = slideDistance;
				if (slideDirection === "up") targetY = -slideDistance;
				if (slideDirection === "down") targetY = slideDistance;
				window.anime.set(el, { translateX: 0, translateY: 0, opacity: 1 });
				return window.anime({
					targets: el,
					translateX: targetX,
					translateY: targetY,
					opacity: 0,
					duration: cfg.duration,
					easing: cfg.easing
				}).finished;

			case "moveFLIP":
				const first = el.getBoundingClientRect();
				const clone = el.cloneNode(true);
				Object.assign(clone.style, {
					position: "absolute",
					top: `${first.top}px`,
					left: `${first.left}px`,
					width: `${first.width}px`,
					height: `${first.height}px`,
					margin: "0",
					pointerEvents: "none",
					zIndex: "9999",
					transformOrigin: "top left",
					boxSizing: "border-box",
					display: getComputedStyle(el).display === "inline" ? "inline-block" : getComputedStyle(el).display,
					transform: "none"
				});
				document.body.appendChild(clone);
				el.style.visibility = "hidden";
				if (cfg.layoutChangeFn) cfg.layoutChangeFn();
				const last = el.getBoundingClientRect();
				const dx = last.left - first.left;
				const dy = last.top - first.top;
				const scaleX = first.width === 0 ? 1 : last.width / first.width;
				const scaleY = first.height === 0 ? 1 : last.height / first.height;
				return window.anime({
					targets: clone,
					translateX: dx,
					translateY: dy,
					scaleX,
					scaleY,
					duration: cfg.duration,
					easing: cfg.easing,
					complete: () => {
						clone.remove();
						el.style.visibility = "";
					}
				}).finished;

			default:
				return Promise.resolve();
		}
	}

	// Expose API
	window.Animations = Object.assign({}, window.Animations, {
		animate
	});
})();