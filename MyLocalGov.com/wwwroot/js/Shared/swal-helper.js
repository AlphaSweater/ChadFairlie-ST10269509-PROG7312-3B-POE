/* ============================================================================
   SweetAlert2 Super Helper (SwalHelper)
   ----------------------------------------------------------------------------
   Purpose:
     Centralize AJAX form submission (loading -> validate -> post -> response ->
     modal -> redirect) + generic alert utilities.

   Requirements:
     - SweetAlert2 loaded globally as Swal BEFORE this file
     - Optionally a validation object (custom) exposing:
         validateAll(): bool
         focusFirstError(): void   (optional)
     - Server JSON response convention (default):
         { success: bool, message: string, redirectUrl?: string, errors?: [{field, messages[]}] }

   Core APIs:
     SwalHelper.loading(opts)
     SwalHelper.success(opts)
     SwalHelper.error(opts)
     SwalHelper.confirm(opts)
     SwalHelper.toast(opts)
     SwalHelper.validationErrors(errors, opts)

     // High-level form binding (MOST IMPORTANT):
     const handle = SwalHelper.bindAjaxForm({
        form: '#myForm',
        submitButton: '#submitBtn',
        wizard: wizardObj,                // optional (must expose currentStep & totalSteps)
        lastStepOnly: true,               // only allow submit on last wizard step
        validate: () => validator.validateAll(),
        focusFirstError: () => validator.focusFirstError?.(),
        sync: [ () => {...}, ... ],       // callbacks before validation & send (e.g. sync RTE)
        beforeSend: async (fd, ctx) => {},// mutate FormData or abort via throw
        transformFormData: fd => fd,      // return (new) FormData
        request: {                        // fetch parts
           headers: { },                  // extra headers (X-Requested-With added automatically)
           method: 'POST'                 // override if needed
        },
        parseResponse: r => ({            // map raw server JSON to normalized shape
           success: r.success,
           message: r.message,
           redirectUrl: r.redirectUrl,
           errors: r.errors
        }),
        onSuccess: (norm, raw) => {},
        onServerValidation: (norm) => {}, // if 400 with field errors
        onServerFailure: (norm, raw, resp) => {},
        onNetworkError: (err) => {},
        showDialogs: true,                // disable to fully custom handle
        autoRedirect: true,               // redirect on user confirm if success & redirectUrl present
        successTitle: 'Success',
        loadingTitle: 'Submitting',
        loadingHtml: 'Please wait...',
        successConfirmText: 'OK',
        preventDoubleSubmit: true,
        resetOnSuccess: false,
        applyFieldErrors: (errors, ctx) => {}, // optional mapping errors -> UI
     });

     handle.dispose(); // remove listeners

   Notes:
     - Defensive: silently no-op if Swal missing (avoids crashes).
     - All hooks optional.
============================================================================ */
(function (global) {
	if (global.SwalHelper && global.SwalHelper.__v >= 2) return;

	const VERSION = 2;
	const NOOP = () => { };

	function hasSwal() { return !!global.Swal; }

	function fire(opts) { return hasSwal() ? global.Swal.fire(opts) : Promise.resolve(); }

	function loading(opts = {}) {
		if (!hasSwal()) return;
		const {
			title = 'Please wait',
			html = 'Processing...',
			icon = 'info',
			backdrop = true,
			allowOutsideClick = false,
			allowEscapeKey = false,
			didOpen
		} = opts;
		global.Swal.fire({
			title,
			html,
			icon,
			allowOutsideClick,
			allowEscapeKey,
			showConfirmButton: false,
			backdrop,
			didOpen: () => {
				global.Swal.showLoading();
				didOpen?.();
			}
		});
	}

	function success(opts = {}) {
		if (!hasSwal()) return Promise.resolve();
		const {
			title = 'Success',
			text = 'Operation completed successfully.',
			html,
			confirmButtonText = 'OK',
			timer,
			footer
		} = opts;
		return fire({
			icon: 'success',
			title,
			text: html ? undefined : text,
			html,
			footer,
			timer,
			confirmButtonText
		});
	}

	function error(opts = {}) {
		if (!hasSwal()) return Promise.resolve();
		const {
			title = 'Error',
			text = 'Something went wrong.',
			html,
			footer,
			confirmButtonText = 'OK'
		} = opts;
		return fire({
			icon: 'error',
			title,
			text: html ? undefined : text,
			html,
			footer,
			confirmButtonText
		});
	}

	function confirm(opts = {}) {
		if (!hasSwal()) return Promise.resolve({ isConfirmed: false });
		const {
			title = 'Are you sure?',
			text = 'This action cannot be undone.',
			icon = 'warning',
			confirmButtonText = 'Yes',
			cancelButtonText = 'Cancel',
			showCancelButton = true,
			reverseButtons = true,
			danger = false
		} = opts;
		return fire({
			icon,
			title,
			text,
			showCancelButton,
			reverseButtons,
			confirmButtonText,
			cancelButtonText,
			customClass: danger
				? { confirmButton: 'swal2-danger-btn' }
				: undefined,
			focusCancel: true
		});
	}

	function toast(opts = {}) {
		if (!hasSwal()) return;
		const {
			icon = 'info',
			title = 'Notice',
			timer = 3000,
			position = 'top-end',
			showConfirmButton = false,
			timerProgressBar = true
		} = opts;
		return fire({
			icon,
			title,
			toast: true,
			position,
			timer,
			showConfirmButton,
			timerProgressBar
		});
	}

	function escapeHtml(str) {
		return String(str ?? '')
			.replace(/&/g, '&amp;')
			.replace(/</g, '&lt;')
			.replace(/>/g, '&gt;')
			.replace(/"/g, '&quot;')
			.replace(/'/g, '&#39;');
	}

	function validationErrors(errors, opts = {}) {
		if (!hasSwal()) return Promise.resolve();
		const {
			title = 'Validation Errors',
			fallback = 'Please correct the highlighted fields.'
		} = opts;
		let html = fallback;
		if (Array.isArray(errors) && errors.length) {
			html = errors
				.map(e =>
					(e.messages || [])
						.map(m => `&bull; <strong>${escapeHtml(e.field)}</strong>: ${escapeHtml(m)}`)
						.join('<br/>'))
				.join('<br/>');
		}
		return fire({ icon: 'error', title, html });
	}

	// Internal: add / remove field errors generically (simple approach)
	function defaultApplyFieldErrors(errors) {
		if (!errors) return;
		errors.forEach(e => {
			const name = e.field;
			if (!name) return;
			// Try input by name
			const el = document.querySelector(`[name="${CSS.escape(name)}"]`);
			if (el) {
				el.classList.add('is-invalid');
				if (e.messages?.length) {
					let span = el.parentElement?.querySelector('.field-validation-error') ||
						el.parentElement?.querySelector('.text-danger');
					if (!span) {
						span = document.createElement('span');
						span.className = 'text-danger';
						el.parentElement?.appendChild(span);
					}
					span.innerHTML = escapeHtml(e.messages[0]);
				}
			}
		});
	}

	function close() { if (hasSwal()) global.Swal.close(); }

	/* -------------------------------------------------------------------------
	   bindAjaxForm : Opinionated high-level form submission orchestrator
	   ----------------------------------------------------------------------- */
	function bindAjaxForm(cfg) {
		const {
			form,
			submitButton,
			wizard,
			lastStepOnly = false,
			validate,
			focusFirstError,
			sync = [],
			beforeSend,
			transformFormData,
			request = {},
			parseResponse = r => r
				? {
					success: !!r.success,
					message: r.message,
					redirectUrl: r.redirectUrl,
					errors: r.errors
				}
				: { success: false, message: 'Empty response.' },
			onSuccess = NOOP,
			onServerValidation = NOOP,
			onServerFailure = NOOP,
			onNetworkError = NOOP,
			showDialogs = true,
			autoRedirect = true,
			successTitle = 'Success',
			loadingTitle = 'Submitting',
			loadingHtml = 'Processing...',
			successConfirmText = 'OK',
			preventDoubleSubmit = true,
			resetOnSuccess = false,
			applyFieldErrors = defaultApplyFieldErrors
		} = cfg;

		const formEl = typeof form === 'string' ? document.querySelector(form) : form;
		if (!formEl) {
			console.warn('SwalHelper.bindAjaxForm: form not found');
			return { dispose: NOOP };
		}
		const btnEl = submitButton
			? (typeof submitButton === 'string' ? document.querySelector(submitButton) : submitButton)
			: formEl.querySelector('[type="submit"]');

		function canSubmitNow() {
			if (!lastStepOnly || !wizard) return true;
			if (typeof wizard.currentStep === 'number' && wizard.totalSteps) {
				return wizard.currentStep === wizard.totalSteps;
			}
			return true; // fallback if not discernible
		}

		async function handler(e) {
			e.preventDefault();
			if (!canSubmitNow()) return;

			if (preventDoubleSubmit && formEl.dataset.submitting === '1') return;

			// Clear previous generic field invalid highlights
			formEl.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));

			// Sync callbacks (e.g., update hidden textarea from editor)
			try { sync.forEach(fn => { try { fn(); } catch { } }); } catch { }

			// Client validation
			if (typeof validate === 'function') {
				const valid = !!validate();
				if (!valid) {
					focusFirstError?.();
					return;
				}
			}

			formEl.dataset.submitting = '1';
			if (btnEl) btnEl.disabled = true;

			let fd = new FormData(formEl);
			if (typeof transformFormData === 'function') {
				const result = transformFormData(fd);
				if (result instanceof FormData) fd = result;
			}

			try {
				if (beforeSend) {
					await beforeSend(fd, { form: formEl });
				}
			} catch (abortErr) {
				// Hook aborted submission
				formEl.dataset.submitting = '0';
				if (btnEl) btnEl.disabled = false;
				return;
			}

			if (showDialogs) {
				loading({ title: loadingTitle, html: loadingHtml });
			}

			let resp, data, norm;
			try {
				resp = await fetch(formEl.action, {
					method: request.method || 'POST',
					body: fd,
					headers: {
						'X-Requested-With': 'XMLHttpRequest',
						...(request.headers || {})
					},
					...(request.init || {})
				});

				const ct = resp.headers.get('content-type') || '';
				if (ct.includes('application/json')) {
					data = await resp.json();
				} else {
					data = null;
				}
				norm = parseResponse(data);

				// Success path
				if (resp.ok && norm.success) {
					close();
					if (showDialogs) {
						await success({
							title: successTitle,
							text: norm.message || 'Completed successfully.',
							confirmButtonText: successConfirmText
						});
					}
					onSuccess(norm, data, resp);
					if (autoRedirect && norm.redirectUrl) {
						window.location.href = norm.redirectUrl;
						return;
					}
					if (resetOnSuccess) formEl.reset();
				}
				// Validation errors (400)
				else if (resp.status === 400 && norm?.errors?.length) {
					close();
					applyFieldErrors(norm.errors, { form: formEl, norm, raw: data });
					onServerValidation(norm, data, resp);
					if (showDialogs)
						await validationErrors(norm.errors, { fallback: norm.message || 'Please correct highlighted fields.' });
				}
				// Other failure
				else {
					close();
					onServerFailure(norm, data, resp);
					if (showDialogs)
						await error({
							title: 'Submission Failed',
							text: norm?.message || `Request failed (${resp.status})`
						});
				}
			} catch (netErr) {
				close();
				onNetworkError(netErr);
				if (showDialogs)
					await error({
						title: 'Network / Server Error',
						text: netErr?.message || 'Unable to complete request.'
					});
			} finally {
				if (document.body.contains(formEl)) {
					formEl.dataset.submitting = '0';
					if (btnEl) btnEl.disabled = false;
				}
			}
		}

		formEl.addEventListener('submit', handler);

		return {
			dispose() {
				formEl.removeEventListener('submit', handler);
			}
		};
	}

	global.SwalHelper = {
		__v: VERSION,
		loading,
		success,
		error,
		confirm,
		toast,
		validationErrors,
		close,
		bindAjaxForm
	};

})(window);