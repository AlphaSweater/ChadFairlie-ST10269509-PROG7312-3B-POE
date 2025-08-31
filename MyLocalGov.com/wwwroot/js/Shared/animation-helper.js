// animation-helper.js

document.addEventListener("DOMContentLoaded", function () {
	// ===============================
	// AOS Initialization
	// ===============================
	AOS.init({
		duration: 900,
		easing: "ease-in-out-cubic",
		once: true,
		offset: 80
	});

	// ===============================
	// Anime.js Presets
	// ===============================
	function animateWithClass(selector, animationFn, delay = 0) {
		const elements = typeof selector === "string"
			? document.querySelectorAll(selector)
			: [selector];
		elements.forEach(el => {
			if (!el) return;
			setTimeout(() => {
				el.classList.add("animated");
				animationFn(el);
			}, delay);
		});
	}

	window.Animations = {
		fadeIn: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					opacity: [0, 1],
					easing: "easeOutCubic",
					duration: 800
				});
			}, delay);
		},

		slideUp: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					translateY: [60, 0], // only visual, not layout
					opacity: [0, 1],
					easing: "easeOutExpo",
					duration: 1000
				});
			}, delay);
		},

		slideLeft: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					translateX: [80, 0],
					easing: "easeOutExpo",
					duration: 1000
				});
			}, delay);
		},

		slideInFromRight: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					translateX: [200, 0], // starts offscreen to the right, moves left
					opacity: [0, 1],
					easing: "easeOutExpo",
					duration: 1000,
					delay: delay
				});
			}, delay);
		},

		slideInFromRightNoOpacity: function (selector, delay = 0) {
			function animationFn(el) {
				anime({
					targets: el,
					translateX: [200, 0], // Only move, do not fade
					easing: "easeOutExpo",
					duration: 1000,
					delay: delay
				});
			}
			animateWithClass(selector, animationFn, delay);
		},

		slideRight: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					translateX: [-80, 0],
					easing: "easeOutExpo",
					duration: 1000
				});
			}, delay);
		},

		scaleIn: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					scale: [0.5, 1],
					opacity: [0, 1],
					easing: "easeOutBack",
					duration: 800
				});
			}, delay);
		},

		zoomIn: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					scale: [1.2, 1],
					opacity: [0, 1],
					easing: "easeOutCubic",
					duration: 800
				});
			}, delay);
		},

		rotateIn: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					rotate: [-15, 0],
					opacity: [0, 1],
					easing: "easeOutExpo",
					duration: 900
				});
			}, delay);
		},

		bounceIn: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					scale: [0.3, 1.1, 0.9, 1],
					opacity: [0, 1],
					easing: "easeOutElastic(1, .6)",
					duration: 1200
				});
			}, delay);
		},

		shake: function (selector, delay = 0) {
			animateWithClass(selector, el => {
				anime({
					targets: el,
					translateX: [
						{ value: -10 }, { value: 10 },
						{ value: -10 }, { value: 10 },
						{ value: 0 }
					],
					easing: "easeInOutQuad",
					duration: 600
				});
			}, delay);
		},

		pageTransition: function () {
			document.body.classList.add("page-exit");
			setTimeout(() => {
				document.body.classList.remove("page-exit");
			}, 600);
		},

		// --- Added FLIP animation helper ---
		moveToNewSpot: function (target, layoutChangeFn, {
			duration = 800,
			easing = "easeInOutQuad"
		} = {}) {
			const element = typeof target === "string" ? document.querySelector(target) : target;
			const firstRect = element.getBoundingClientRect();
			layoutChangeFn();
			element.offsetWidth; 
			const lastRect = element.getBoundingClientRect();
			const deltaX = firstRect.left - lastRect.left;
			const deltaY = firstRect.top - lastRect.top;
			anime({
				targets: element,
				translateX: [deltaX, 0],
				translateY: [deltaY, 0],
				duration: duration,
				easing: easing
			});
		}
	};

	// ===============================
	// Page Transition Hook
	// ===============================
	document.querySelectorAll("a.page-link").forEach(link => {
		link.addEventListener("click", function (e) {
			e.preventDefault();
			const url = this.href;
			Animations.pageTransition();
			setTimeout(() => window.location.href = url, 500);
		});
	});

	// ===============================
	// Delayed Page Loading Logic
	// ===============================
	let overlayTimer;
	let overlayVisible = false;
	const overlay = document.getElementById("pageLoadingOverlay");

	// Start a timer to show the overlay only if loading takes longer than 150ms
	overlayTimer = setTimeout(() => {
		if (overlay) {
			overlay.style.opacity = "1";
			overlay.style.display = "flex";
			overlayVisible = true;
		}
	}, 150);

	// When the page fully loads
	window.addEventListener("load", function () {
		clearTimeout(overlayTimer);

		document.body.classList.remove("body-anim-loading");
		document.body.classList.add("page-loaded");

		if (overlay && overlayVisible) {
			overlay.style.opacity = "0";
			setTimeout(() => {
				overlay.style.display = "none";
				document.dispatchEvent(new Event("pageReady"));
			}, 300);
		} else {
			// Overlay was never shown, just dispatch event
			if (overlay) overlay.style.display = "none";
			document.dispatchEvent(new Event("pageReady"));
		}
	});
});
