// animations.css.js
// Reusable CSS-based animations triggered via JS

(function () {
	if (typeof window === "undefined") return;

	function _resolve(el) {
		if (!el) return null;
		if (typeof el === "string") return document.querySelector(el);
		return el instanceof Element ? el : null;
	}

	function animate(type, target, opts = {}) {
		const el = _resolve(target);
		if (!el) return Promise.resolve();

		return new Promise(resolve => {
			const className = `anim-${type}`;
			const duration = opts.duration || 500; // fallback
			el.style.setProperty("--anim-duration", duration + "ms");

			// restart if already applied
			el.classList.remove(className);
			void el.offsetWidth; // reflow
			el.classList.add(className);

			function cleanup() {
				el.classList.remove(className);
				el.removeEventListener("animationend", cleanup);
				resolve();
			}

			el.addEventListener("animationend", cleanup, { once: true });
		});
	}

	window.Animations = Object.assign({}, window.Animations, {
		animateCSS: animate
	});
})();
