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

			// set custom vars (extensible)
			const vars = {
				duration: "--anim-duration",
				ease: "--anim-ease",
				delay: "--anim-delay",
				iterations: "--anim-iteration",
				distance: "--anim-distance",
				scale: "--anim-scale",
				rotate: "--anim-rotate",
				opacity: "--anim-opacity",
				color: "--anim-color"
			};

			for (const key in opts) {
				if (vars[key]) {
					el.style.setProperty(vars[key], typeof opts[key] === "number" ? opts[key] + "" : opts[key]);
				}
			}

			// restart animation
			el.classList.remove(className);
			void el.offsetWidth; // force reflow
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
