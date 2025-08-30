// animation-helper.js

console.log("animation-helper.js loaded");

// ===============================
// AOS Initialization
// ===============================
document.addEventListener("DOMContentLoaded", function () {
	AOS.init({
		duration: 800,
		easing: "ease-in-out",
		once: true
	});

	window.Animations = {
		fadeIn: function (selector) {
			const el = document.querySelector(selector);
			console.log("[fadeIn] Before:", el ? el.style.opacity : "not found");
			anime({
				targets: selector,
				opacity: [0, 1],
				easing: "easeOutQuad",
				duration: 1000,
				complete: function() {
					const el = document.querySelector(selector);
					console.log("[fadeIn] After:", el ? el.style.opacity : "not found");
				}
			});
		},

		slideUp: function (selector) {
			const el = document.querySelector(selector);
			console.log("[slideUp] Before:", el ? {opacity: el.style.opacity, y: el.style.transform} : "not found");
			anime({
				targets: selector,
				translateY: [50, 0],
				opacity: [0, 1],
				easing: "easeOutExpo",
				duration: 1000,
				complete: function() {
					const el = document.querySelector(selector);
					console.log("[slideUp] After:", el ? {opacity: el.style.opacity, y: el.style.transform} : "not found");
				}
			});
		},

		slideLeft: function (selector) {
			const el = document.querySelector(selector);
			console.log("[slideLeft] Before:", el ? {opacity: el.style.opacity, x: el.style.transform} : "not found");
			anime({
				targets: selector,
				translateX: [-100, 0],
				opacity: [0, 1],
				easing: "easeOutExpo",
				duration: 1000,
				complete: function() {
					const el = document.querySelector(selector);
					console.log("[slideLeft] After:", el ? {opacity: el.style.opacity, x: el.style.transform} : "not found");
				}
			});
		},

		scaleIn: function (selector) {
			const el = document.querySelector(selector);
			console.log("[scaleIn] Before:", el ? {opacity: el.style.opacity, scale: el.style.transform} : "not found");
			anime({
				targets: selector,
				scale: [0.8, 1],
				opacity: [0, 1],
				easing: "easeOutBack",
				duration: 800,
				complete: function() {
					const el = document.querySelector(selector);
					console.log("[scaleIn] After:", el ? {opacity: el.style.opacity, scale: el.style.transform} : "not found");
				}
			});
		},

		pageTransition: function () {
			console.log("[pageTransition] Triggered");
			document.body.classList.add("page-exit");
			setTimeout(() => {
				document.body.classList.remove("page-exit");
			}, 500);
		}
	};

	// ===============================
	// Optional: Page transition hook
	// ===============================
	// Works if you have links with class="page-link"
	document.querySelectorAll("a.page-link").forEach(link => {
		link.addEventListener("click", function (e) {
			e.preventDefault();
			const url = this.href;
			Animations.pageTransition();
			setTimeout(() => window.location.href = url, 400);
		});
	});
});
