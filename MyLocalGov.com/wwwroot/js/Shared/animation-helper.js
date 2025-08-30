// animation-helper.js

document.addEventListener("DOMContentLoaded", function () {
	// ===============================
	// AOS Initialization
	// ===============================
	AOS.init({
		duration: 900,      // base duration
		easing: "ease-in-out-cubic",
		once: true,         // animate only once per element
		offset: 80          // trigger a bit before element enters view
	});

	// ===============================
	// Anime.js Presets
	// ===============================
	window.Animations = {
		fadeIn: function (selector, delay = 0) {
			anime({
				targets: selector,
				opacity: [0, 1],
				easing: "easeOutCubic",
				duration: 800,
				delay: delay
			});
		},

		slideUp: function (selector, delay = 0) {
			anime({
				targets: selector,
				translateY: [60, 0],
				opacity: [0, 1],
				easing: "easeOutExpo",
				duration: 1000,
				delay: delay
			});
		},

		slideLeft: function (selector, delay = 0) {
			anime({
				targets: selector,
				translateX: [-80, 0],
				opacity: [0, 1],
				easing: "easeOutExpo",
				duration: 1000,
				delay: delay
			});
		},

		slideRight: function (selector, delay = 0) {
			anime({
				targets: selector,
				translateX: [80, 0],
				opacity: [0, 1],
				easing: "easeOutExpo",
				duration: 1000,
				delay: delay
			});
		},

		scaleIn: function (selector, delay = 0) {
			anime({
				targets: selector,
				scale: [0.5, 1],   // smaller start, bigger end
				opacity: [0, 1],
				easing: "easeOutBack",
				duration: 800,
				delay: delay
			});
		},

		zoomIn: function (selector, delay = 0) {
			anime({
				targets: selector,
				scale: [1.2, 1],   // zoom from bigger down to normal
				opacity: [0, 1],
				easing: "easeOutCubic",
				duration: 800,
				delay: delay
			});
		},

		rotateIn: function (selector, delay = 0) {
			anime({
				targets: selector,
				rotate: [-15, 0],
				opacity: [0, 1],
				easing: "easeOutExpo",
				duration: 900,
				delay: delay
			});
		},

		bounceIn: function (selector, delay = 0) {
			anime({
				targets: selector,
				scale: [0.3, 1.1, 0.9, 1], // squash & stretch
				opacity: [0, 1],
				easing: "easeOutElastic(1, .6)",
				duration: 1200,
				delay: delay
			});
		},

		shake: function (selector, delay = 0) {
			anime({
				targets: selector,
				translateX: [
					{ value: -10 }, { value: 10 },
					{ value: -10 }, { value: 10 },
					{ value: 0 }
				],
				easing: "easeInOutQuad",
				duration: 600,
				delay: delay
			});
		},

		// Page transition (fade body out before navigation)
		pageTransition: function () {
			document.body.classList.add("page-exit");
			setTimeout(() => {
				document.body.classList.remove("page-exit");
			}, 600);
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
