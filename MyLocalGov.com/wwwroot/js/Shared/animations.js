document.addEventListener("DOMContentLoaded", () => {
	const animatedElements = document.querySelectorAll("[data-animate]");

	const observer = new IntersectionObserver((entries) => {
		entries.forEach(entry => {
			if (entry.isIntersecting) {
				entry.target.classList.add("appear");
				observer.unobserve(entry.target); // animate once
			}
		});
	}, { threshold: 0.2 });

	animatedElements.forEach(el => {
		const delay = el.dataset.delay || 0;
		const duration = el.dataset.duration || 800;
		el.style.transitionDuration = `${duration}ms`;
		el.style.transitionDelay = `${delay}ms`;
		observer.observe(el);
	});
});

/* Optional helper to trigger animations manually */
function triggerAnimation(selector, animationClass) {
	const el = document.querySelector(selector);
	if (el) {
		el.classList.add(animationClass, "appear");
	}
}