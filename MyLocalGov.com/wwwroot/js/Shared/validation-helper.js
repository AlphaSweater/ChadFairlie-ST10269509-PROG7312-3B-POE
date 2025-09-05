/*!
 * ValidationHelper - jQuery Validate + Unobtrusive helper
 * Works with ASP.NET Core Razor Pages and MVC (data-val attributes).
 * Requires: jQuery, jQuery Validation, jQuery Validation Unobtrusive (optional).
 */
(function (root, factory) {
	if (typeof define === "function" && define.amd) {
		define(["jquery"], factory);
	} else if (typeof exports === "object") {
		module.exports = factory(require("jquery"));
	} else {
		root.ValidationHelper = factory(root.jQuery);
	}
}(typeof self !== "undefined" ? self : this, function ($) {
	"use strict";

	// No-op API when jQuery or validator not present (prevents runtime errors)
	const noopApi = {
		init: () => ({
			validateAll: () => true,
			validateContainer: () => true,
			validateFields: () => true,
			focusFirstError: () => { },
			reparse: () => { },
			setIgnore: () => { },
			destroy: () => { },
			get validator() { return null; }
		}),
		addMethod: () => { },
		registerRichTextRequired: () => { },
		parseUnobtrusive: () => { },
		markRequiredLabels: () => { }
	};
	if (!$ || !$.validator) {
		if (typeof console !== "undefined" && console.warn) {
			console.warn("[ValidationHelper] jQuery Validation not found. Helper is a no-op.");
		}
		return noopApi;
	}

	const hasUnobtrusive = !!($.validator && $.validator.unobtrusive);

	const defaults = {
		// Live (on-change) validation behavior
		live: true,
		liveEvents: "input change blur",
		liveThrottleMs: 120,

		// jQuery Validate 'ignore' selector. Add selectors in includeHidden to opt them back in.
		ignore: ":hidden",
		includeHidden: [],

		// Focus/scroll behavior when invalid
		focusOnError: true,
		scrollToError: true,
		scrollOptions: { behavior: "smooth", block: "center", inline: "nearest" },

		// Unobtrusive parsing
		parseUnobtrusiveOnInit: true,

		// Hooks
		onBeforeValidate: null,  // (ctx) => {}
		onAfterValidate: null    // (ctx, valid) => {}
	};

	function toArray(x) { return Array.isArray(x) ? x : (x ? [x] : []); }
	function isElement(obj) { return obj instanceof Element || obj instanceof HTMLDocument; }

	// Build ignore like ':hidden:not(#a):not(.b)'
	function buildIgnoreSelector(ignore, includeHiddenSelectors) {
		let ig = ignore || "";
		const include = toArray(includeHiddenSelectors).filter(Boolean);
		if (!ig || include.length === 0) return ig;
		if (String(ig).includes(":hidden")) {
			ig += include.map(sel => `:not(${sel})`).join("");
		}
		return ig;
	}

	function ensureParsed(container) {
		if (!hasUnobtrusive || !container) return;
		const $c = container.jquery ? container : $(container);
		if ($c && $c.length) {
			$.validator.unobtrusive.parse($c);
		}
	}

	function getValidator($form) {
		// Ensure a validator instance exists (validate() returns/creates it)
		return $form.validate();
	}

	function allFieldsInContainer(container) {
		const root = isElement(container) ? container : document.querySelector(container) || document;
		return Array.from(root.querySelectorAll("input[name], select[name], textarea[name]"));
	}

	function debounce(fn, delay) {
		let t;
		return function () {
			const ctx = this, args = arguments;
			clearTimeout(t);
			t = setTimeout(() => fn.apply(ctx, args), delay);
		};
	}

	function focusAndScrollTo(el, options) {
		try { el.focus({ preventScroll: true }); } catch { try { el.focus(); } catch { /* ignore */ } }
		if (options && el.scrollIntoView) {
			try { el.scrollIntoView(options); } catch { el.scrollIntoView(); }
		}
	}

	function firstErrorIn(validator, container) {
		const list = (validator && validator.errorList) || [];
		if (!container) return list[0]?.element || null;

		const root = isElement(container) ? container : document.querySelector(container);
		if (!root) return list[0]?.element || null;

		for (const err of list) {
			if (root.contains(err.element)) return err.element;
		}
		// Fallback to any marked invalid control inside container
		const fallback = root.querySelector(".input-validation-error, [aria-invalid='true']");
		return fallback || null;
	}

	function validateFieldsInternal($form, fields, options, ctx) {
		const validator = getValidator($form);
		// Clear cached remote state to re-validate
		fields.forEach(f => $(f).removeData("previousValue"));

		if (typeof options.onBeforeValidate === "function") {
			try { options.onBeforeValidate(ctx); } catch { /* no-op */ }
		}

		let valid = true;
		for (const f of fields) {
			if (!$(f).valid()) valid = false;
		}

		if (typeof options.onAfterValidate === "function") {
			try { options.onAfterValidate(ctx, valid); } catch { /* no-op */ }
		}

		if (!valid && (options.focusOnError || options.scrollToError)) {
			const el = firstErrorIn(validator, ctx.container);
			if (el) focusAndScrollTo(el, options.scrollToError ? options.scrollOptions : null);
		}

		return valid;
	}

	function attachLiveValidation($form, options) {
		const events = options.liveEvents || defaults.liveEvents;
		const handler = debounce(function (e) {
			const target = e.target;
			if (!target || !target.name) return;

			// Skip ignored elements
			const ignoreSelector = $form.data("validator")?.settings?.ignore;
			try {
				if (ignoreSelector && $(target).is(ignoreSelector)) return;
			} catch { /* ignore invalid selectors */ }

			// If dynamically added, parse single element unobtrusively
			if (hasUnobtrusive && !$(target).data("val")) {
				try { $.validator.unobtrusive.parseElement(target); } catch { /* no-op */ }
			}

			$(target).valid();
		}, options.liveThrottleMs || defaults.liveThrottleMs);

		// Delegate to inputs/selects/textareas inside the form
		$form.on(events, "input,select,textarea", handler);
	}

	function createFormApi(form, opts) {
		const options = Object.assign({}, defaults, opts || {});
		const $form = form.jquery ? form : $(form);
		if (!$form || !$form.length) throw new Error("[ValidationHelper] Form not found.");

		// Parse unobtrusive attributes on init
		if (options.parseUnobtrusiveOnInit) {
			ensureParsed($form);
		}

		// Ensure validator exists and set ignore rule (with includeHidden opt-ins)
		const validator = getValidator($form);
		validator.settings.ignore = buildIgnoreSelector(options.ignore, options.includeHidden);

		// Attach live validation if enabled
		if (options.live) attachLiveValidation($form, options);

		return {
			get validator() { return $form.data("validator") || null; },

			// Validate entire form
			validateAll() {
				const fields = allFieldsInContainer($form[0]);
				return validateFieldsInternal($form, fields, options, { form: $form[0], container: null });
			},

			// Validate only fields within a container (e.g., wizard step)
			validateContainer(container) {
				ensureParsed(container);
				const fields = allFieldsInContainer(container)
					.filter(f => f.form === $form[0]); // only those belonging to this form
				return validateFieldsInternal($form, fields, options, { form: $form[0], container });
			},

			// Validate specific fields by selector or element
			validateFields(fields) {
				const list = toArray(fields).flatMap(f => {
					if (typeof f === "string") return Array.from(document.querySelectorAll(f));
					if (isElement(f)) return [f];
					return [];
				});
				return validateFieldsInternal($form, list, options, { form: $form[0], container: null });
			},

			// Focus first error (optionally within a container)
			focusFirstError(container) {
				const v = $form.data("validator");
				const el = firstErrorIn(v, container);
				if (el) focusAndScrollTo(el, options.scrollToError ? options.scrollOptions : null);
			},

			// Re-parse unobtrusive rules for dynamic content
			reparse(container) {
				ensureParsed(container || $form);
			},

			// Update ignore rule at runtime
			setIgnore(ignore, includeHidden) {
				const v = getValidator($form);
				v.settings.ignore = buildIgnoreSelector(ignore, includeHidden || options.includeHidden);
			},

			// Remove handlers
			destroy() {
				try { $form.off(options.liveEvents); } catch { /* no-op */ }
			}
		};
	}

	// Public API (module)
	const api = {
		// Initialize a form and get a scoped API
		init(form, options) {
			return createFormApi(form, options);
		},

		// Register a custom rule and optional unobtrusive adapter
		// adapter: { type: "bool" | "params", name?: string, params?: string[], adapt?: (options)=>void }
		addMethod(name, method, defaultMessage, adapter) {
			if (!name || typeof method !== "function") return;
			$.validator.addMethod(name, method, defaultMessage || "Invalid value.");

			if (hasUnobtrusive && adapter) {
				const u = $.validator.unobtrusive.adapters;
				if (adapter.type === "bool") {
					u.addBool(adapter.name || name);
				} else if (adapter.type === "params" && Array.isArray(adapter.params)) {
					u.add(adapter.name || name, adapter.params, function (options) {
						if (typeof adapter.adapt === "function") {
							adapter.adapt(options);
						} else {
							options.rules[name] = options.params;
							if (options.message) options.messages[name] = options.message;
						}
					});
				}
			}
		},

		// RTE/contenteditable "required" helper (works for hidden textarea or contenteditable)
		registerRichTextRequired(methodName) {
			const ruleName = methodName || "rterequired";
			const stripHtml = (html) => (html || "")
				.replace(/<style[\s\S]*?<\/style>/gi, "")
				.replace(/<script[\s\S]*?<\/script>/gi, "")
				.replace(/<[^>]*>/g, "")
				.replace(/&nbsp;/g, " ")
				.trim();

			this.addMethod(
				ruleName,
				function (value, element) {
					const targetSel = element?.getAttribute?.("data-rte-target");
					if (targetSel) {
						const source = document.querySelector(targetSel);
						const raw = source ? (source.innerHTML || "") : "";
						return stripHtml(raw).length > 0;
					}
					// Default: validate the element's own value (e.g., hidden textarea bound to editor HTML)
					return stripHtml(value).length > 0;
				},
				"This field is required.",
				{ type: "bool", name: ruleName }
			);
		},

		// Unobtrusive parse utility
		parseUnobtrusive(container) {
			ensureParsed(container);
		},

		// Add 'label-required' to labels for inputs that have data-val-required
		markRequiredLabels(root) {
			const scope = isElement(root) ? root : document;

			// Support both standard [Required] and the custom RTE required flag
			const selector = '[data-val="true"][data-val-required], [data-val="true"][data-val-rterequired]';

			scope.querySelectorAll(selector).forEach(el => {
				// Prefer label bound by id
				let label = el.id ? scope.querySelector(`label[for="${el.id}"]`) : null;

				// Fallback: label bound by name (useful when the id was overridden)
				if (!label) {
					const name = el.getAttribute("name");
					if (name) {
						label = scope.querySelector(`label[for="${name}"]`);
					}
				}

				if (label) label.classList.add("label-required");
			});
		}
	};

	return api;
}));

/*
Usage (include after jquery.validate + jquery.validate.unobtrusive):
<script>
document.addEventListener("DOMContentLoaded", function () {
	// Optional custom rule for RTE fields
	ValidationHelper.registerRichTextRequired();

	// Initialize form
	const v = ValidationHelper.init("#reportForm", {
		includeHidden: ["#descriptionHidden"], // opt-in hidden fields for validation
		focusOnError: true,
		scrollToError: true
	});

	// Validate a wizard step:
	// v.validateContainer('.wizard-step[data-step="2"]');

	// Validate entire form before submit:
	// form.addEventListener("submit", (e) => { if (!v.validateAll()) { e.preventDefault(); v.focusFirstError(); } });
});
</script>
*/