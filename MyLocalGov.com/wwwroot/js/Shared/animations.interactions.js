// animations.interactions.js
// Micro-interactions (shake, bounce, etc.) via CSS class toggling

(function () {
	if (typeof window === "undefined") return;

	function _resolve(el) {
		if (!el) return null;
		if (typeof el === "string") return document.querySelector(el);
		return el instanceof Element ? el : null;
	}

	function _restartAnim(el, className) {
		el.classList.remove(className);
		void el.offsetWidth; // reflow trick
		el.classList.add(className);
	}

	function shake(target) {
		const el = _resolve(target);
		if (!el) return;
		_restartAnim(el, "anim-shake");
	}

	function bounce(target) {
		const el = _resolve(target);
		if (!el) return;
		_restartAnim(el, "anim-bounce");
	}

	window.Animations = Object.assign({}, window.Animations, {
		shake,
		bounce
	});
})();
