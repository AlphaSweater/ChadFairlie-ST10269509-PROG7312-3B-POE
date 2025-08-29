// Site-wide Animation Engine

document.addEventListener('DOMContentLoaded', function () {
	// Scroll-triggered animations
	const animatedEls = document.querySelectorAll(
		'.slide-in-left, .slide-in-right, .slide-in-up, .grow-in, .fade-in'
	);

	const isInViewport = el => {
		const rect = el.getBoundingClientRect();
		return (
			rect.top < window.innerHeight &&
			rect.bottom > 0 &&
			rect.left < window.innerWidth &&
			rect.right > 0
		);
	};

	// Animate elements already in the viewport on load
	animatedEls.forEach(el => {
		if (isInViewport(el)) {
			el.classList.add('animated');
		}
	});

	// Observe elements for scroll-in animation
	const observer = new IntersectionObserver((entries) => {
		entries.forEach(entry => {
			if (entry.isIntersecting) {
				entry.target.classList.add('animated');
			}
		});
	}, { threshold: 0.1 });

	animatedEls.forEach(el => {
		observer.observe(el);
	});

	// Optional: Fade out on navigation (for SPA-like transitions)
	document.querySelectorAll('a').forEach(link => {
		link.addEventListener('click', function (e) {
			const main = document.querySelector('main, .dashboard-container');
			if (main && main.classList.contains('page-fade-in')) {
				main.classList.remove('page-fade-in');
				main.classList.add('page-fade-out');
			}
		});
	});
});