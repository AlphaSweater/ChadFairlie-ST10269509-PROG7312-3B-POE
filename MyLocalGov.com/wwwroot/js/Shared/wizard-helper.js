/*!
 * WizardHelper - Lightweight multi-step form controller (vanilla JS)
 * - Framework-agnostic; works with ASP.NET Core Razor Pages and MVC
 * - Optional integration with ValidationHelper (jQuery Validate helper)
 * - Auto-fallback to jQuery Validate (Unobtrusive) if present
 *
 * Usage:
 *   const wiz = WizardHelper.init({
 *     container: "#myWizardRoot",
 *     form: "#myForm",
 *     stepSelector: ".wizard-step",
 *     nextSelector: "#nextBtn",
 *     prevSelector: "#prevBtn",
 *     progressBarSelector: "#wizardProgressBar",
 *     stepIndicatorSelector: "#stepIndicator",
 *     engagementSelector: "#engagementMessage",
 *     hiddenClass: "flex-hidden",
 *     validator: ValidationHelper?.init("#myForm", { includeHidden: ["#hiddenEditor"] }),
 *     messages: ["Step 1", "Step 2", "Step 3"],
 *     beforeNext: (ctx) => { sync derived fields etc. return true; },
 *     onShowStep: (ctx) => { update review on last step, etc. },
 *     beforeFinish: (ctx) => true
 *   });
 */
(function (root, factory) {
	if (typeof define === "function" && define.amd) {
		define([], factory);
	} else if (typeof exports === "object") {
		module.exports = factory();
	} else {
		root.WizardHelper = factory();
	}
}(typeof self !== "undefined" ? self : this, function () {
	"use strict";

	const noop = () => { };
	const isElement = (x) => x instanceof Element;
	const resolveEl = (elOrSelector) => isElement(elOrSelector) ? elOrSelector : document.querySelector(elOrSelector);
	const qs = (root, sel) => (isElement(root) ? root : resolveEl(root))?.querySelector(sel) || null;
	const qsa = (root, sel) => Array.from((isElement(root) ? root : resolveEl(root))?.querySelectorAll(sel) || []);
	const clamp = (n, min, max) => Math.max(min, Math.min(max, n));

	function Wizard(options) {
		const defaults = {
			// DOM
			container: null,     // required: selector or element
			form: null,          // optional: selector or element (auto-detected from container if omitted)
			stepSelector: ".wizard-step",
			nextSelector: "[data-wizard-next], #nextBtn",
			prevSelector: "[data-wizard-prev], #prevBtn",
			progressBarSelector: ".wizard-progress-bar, #wizardProgressBar",
			stepIndicatorSelector: ".wizard-step-indicator, #stepIndicator",
			engagementSelector: "#engagementMessage",
			hiddenClass: "flex-hidden",

			// Behavior
			startStep: 1,
			useStackHistory: true,      // Back goes to previous visited step (not necessarily N-1)
			changeNextTextOnLast: true, // Change "Next" to "Finish" on last step
			nextText: "Next",
			finishText: "Finish",
			submitOnFinish: true,

			// Validation (optional)
			validator: null,               // instance from ValidationHelper.init(...) or auto jQuery Validate adapter
			includeHiddenForValidation: [],// used if auto-initializing ValidationHelper
			validateStep: null,            // custom validation function (stepNumber, ctx) => bool

			// Messaging
			messages: null, // array or function(step) => string

			// Hooks
			onInit: noop,         // (ctx)
			onShowStep: noop,     // (ctx)
			beforeNext: noop,     // (ctx) return false to block
			afterNext: noop,      // (ctx)
			beforePrev: noop,     // (ctx) return false to block
			afterPrev: noop,      // (ctx)
			beforeFinish: noop,   // (ctx) return false to block
			onFinish: noop        // (ctx)
		};

		const cfg = Object.assign({}, defaults, options || {});
		if (!cfg.container) throw new Error("[WizardHelper] container is required.");

		// Resolve elements
		const root = resolveEl(cfg.container);
		if (!root) throw new Error("[WizardHelper] container not found.");

		const form = cfg.form ? resolveEl(cfg.form) : root.closest("form");
		const steps = qsa(root, cfg.stepSelector);
		if (steps.length === 0) throw new Error("[WizardHelper] No steps found with selector " + cfg.stepSelector);

		const btnNext = cfg.nextSelector ? qs(root, cfg.nextSelector) : null;
		const btnPrev = cfg.prevSelector ? qs(root, cfg.prevSelector) : null;
		const progressBar = cfg.progressBarSelector ? qs(root, cfg.progressBarSelector) : null;
		const stepIndicator = cfg.stepIndicatorSelector ? qs(root, cfg.stepIndicatorSelector) : null;
		const engagement = cfg.engagementSelector ? qs(root, cfg.engagementSelector) : null;

		// jQuery Validate (Unobtrusive) adapter
		function createJQueryValidatorAdapter(formEl) {
			const $ = window.jQuery;
			const v = $(formEl).data("validator") || $(formEl).validate();
			let lastInvalid = null;
			function inputsIn(container) {
				// Mirror jQuery Validate defaults: ignore disabled/hidden buttons; unobtrusive already wires rules.
				return $(container).find(":input")
					.not(":disabled")
					.not("[type=button],[type=submit],[type=reset]");
			}
			return {
				validateContainer(container) {
					lastInvalid = null;
					let valid = true;
					inputsIn(container).each(function () {
						// .element() validates a single field and shows messages
						const ok = v.element(this);
						if (ok === false) {
							valid = false;
							if (!lastInvalid) lastInvalid = this;
						}
					});
					// If the step has no inputs, consider it valid
					return valid;
				},
				focusFirstError(/*container*/) {
					const el = lastInvalid
						|| $(formEl).find(".input-validation-error, [aria-invalid=true]").filter(":input").get(0)
						|| $(formEl).find(".input-validation-error :input").get(0);
					if (el && typeof el.focus === "function") {
						el.focus();
					}
				}
			};
		}

		// Validation auto-wire if not provided
		let validator = cfg.validator;
		if (!validator && form && window.ValidationHelper) {
			try {
				validator = window.ValidationHelper.init(form, {
					includeHidden: cfg.includeHiddenForValidation,
					focusOnError: true,
					scrollToError: true
				});
			} catch { /* ignore */ }
		}
		// Fallback to jQuery Validate (commonly present in ASP.NET MVC/Razor Pages)
		if (!validator && form && window.jQuery && window.jQuery.fn && typeof window.jQuery.fn.validate === "function") {
			try {
				validator = createJQueryValidatorAdapter(form);
			} catch { /* ignore */ }
		}

		const TOTAL = steps.length;
		const initialStep = clamp(Number(cfg.startStep) || 1, 1, TOTAL);
		const stack = [initialStep];
		let destroyed = false;

		function dispatch(name, detail) {
			try { root.dispatchEvent(new CustomEvent(name, { detail })); } catch { /* no-op */ }
		}
		function currentStep() { return stack[stack.length - 1]; }
		function stepEl(n) { return steps.find(s => Number(s.getAttribute("data-step")) === n) || steps[n - 1]; }

		function makeCtx() {
			return {
				container: root,
				form,
				steps,
				stepCount: TOTAL,
				step: currentStep(),
				currentStepEl: stepEl(currentStep()),
				validator
			};
		}

		function updateUIForStep(num) {
			// Toggle visibility
			steps.forEach(s => {
				const sNum = Number(s.getAttribute("data-step")) || (steps.indexOf(s) + 1);
				s.classList.toggle(cfg.hiddenClass, sNum !== num);
				// Optional ARIA friendliness
				s.setAttribute("aria-hidden", String(sNum !== num));
			});

			// Progress
			if (progressBar) {
				const pct = TOTAL > 1 ? ((num - 1) / (TOTAL - 1)) * 100 : 100;
				progressBar.style.width = pct + "%";
				progressBar.setAttribute("aria-valuenow", String(Math.round(pct)));
				progressBar.setAttribute("aria-valuemin", "0");
				progressBar.setAttribute("aria-valuemax", "100");
			}

			// Indicator
			if (stepIndicator) stepIndicator.textContent = `Step ${num} of ${TOTAL}`;

			// Engagement/message
			if (cfg.messages) {
				const msg = Array.isArray(cfg.messages)
					? (cfg.messages[num - 1] || "")
					: (typeof cfg.messages === "function" ? cfg.messages(num) : "");
				if (engagement) engagement.textContent = msg;
			}

			// Nav buttons
			if (btnPrev) btnPrev.disabled = num === 1;
			if (btnNext && cfg.changeNextTextOnLast) {
				btnNext.textContent = (num === TOTAL ? cfg.finishText : cfg.nextText);
			}
		}

		function showStep(n) {
			const num = clamp(n, 1, TOTAL);
			updateUIForStep(num);
			cfg.onShowStep(makeCtx());
			dispatch("wizard:showstep", { step: num });
		}

		function doValidate(n) {
			if (typeof cfg.validateStep === "function") {
				return !!cfg.validateStep(n, makeCtx());
			}
			const container = stepEl(n);
			if (!validator || !container) return true;
			if (typeof validator.validateContainer === "function") {
				return !!validator.validateContainer(container);
			}
			// If a custom validator object was provided without validateContainer, assume success
			return true;
		}

		function next() {
			const cur = currentStep();
			const ctx = makeCtx();
			if (cfg.beforeNext(ctx) === false) return;

			if (!doValidate(cur)) {
				if (validator && typeof validator.focusFirstError === "function") {
					validator.focusFirstError(stepEl(cur));
				}
				return;
			}

			if (cur < TOTAL) {
				if (cfg.useStackHistory) stack.push(cur + 1);
				else stack[stack.length - 1] = cur + 1;

				showStep(currentStep());
				cfg.afterNext(makeCtx());
				dispatch("wizard:next", { step: currentStep() });
			} else {
				// Finish
				if (cfg.beforeFinish(makeCtx()) === false) return;
				if (cfg.submitOnFinish && form) {
					if (form.requestSubmit) form.requestSubmit(); else form.submit();
				}
				cfg.onFinish(makeCtx());
				dispatch("wizard:finish", { step: currentStep() });
			}
		}

		function prev() {
			const ctx = makeCtx();
			if (cfg.beforePrev(ctx) === false) return;

			if (cfg.useStackHistory && stack.length > 1) {
				stack.pop();
			} else {
				stack[stack.length - 1] = clamp(currentStep() - 1, 1, TOTAL);
			}

			showStep(currentStep());
			cfg.afterPrev(makeCtx());
			dispatch("wizard:prev", { step: currentStep() });
		}

		function goTo(n) {
			const target = clamp(n, 1, TOTAL);
			if (cfg.useStackHistory) {
				const cur = currentStep();
				if (target !== cur) stack.push(target);
			} else {
				stack[stack.length - 1] = target;
			}
			showStep(target);
		}

		function onNextClick(e) { e.preventDefault(); next(); }
		function onPrevClick(e) { e.preventDefault(); prev(); }

		function destroy() {
			if (destroyed) return;
			destroyed = true;
			try {
				if (btnNext) btnNext.removeEventListener("click", onNextClick);
				if (btnPrev) btnPrev.removeEventListener("click", onPrevClick);
			} catch { /* no-op */ }
		}

		// Wire buttons
		if (btnNext) btnNext.addEventListener("click", onNextClick);
		if (btnPrev) btnPrev.addEventListener("click", onPrevClick);

		// Initial render
		showStep(currentStep());
		cfg.onInit(makeCtx());
		dispatch("wizard:init", { step: currentStep() });

		// Public API
		return {
			currentStep,
			showStep,
			next,
			prev,
			goTo,
			destroy,
			get validator() { return validator; }
		};
	}

	return {
		init(opts) { return new Wizard(opts); }
	};
}));