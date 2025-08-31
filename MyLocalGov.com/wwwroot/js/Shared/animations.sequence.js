// animations.sequence.js
// Sequence helpers for chaining animations

(function () {
	if (typeof window === "undefined") return;

	function runSequence(tasks) {
		tasks = tasks || [];
		return tasks.reduce(
			(p, task) => p.then(() => task()),
			Promise.resolve()
		);
	}

	window.Animations = Object.assign({}, window.Animations, {
		runSequence
	});
})();
