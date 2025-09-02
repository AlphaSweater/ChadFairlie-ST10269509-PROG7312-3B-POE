// ==============================
// Report Submission Wizard Script
// ==============================
// Purpose: Handles navigation, progress, form syncing, review step
// and now also saves step data to the server via AJAX
// ==============================

document.addEventListener('DOMContentLoaded', () => {
	// ------------------------------
	// 1. Setup & Element References
	// ------------------------------
	const form = document.getElementById('reportForm');
	const steps = Array.from(document.querySelectorAll('.wizard-step'));
	const prevBtn = document.getElementById('prevBtn');
	const nextBtn = document.getElementById('nextBtn');
	const progressBar = document.getElementById('wizardProgressBar');
	const engagementMessage = document.getElementById('engagementMessage');
	const totalSteps = steps.length;

	// Step 1 inputs (address/location)
	const street = document.getElementById('Street');
	const suburb = document.getElementById('Suburb');
	const city = document.getElementById('City');
	const postal = document.getElementById('PostalCode');
	const lat = document.getElementById('Latitude');
	const lng = document.getElementById('Longitude');
	const addressHidden = document.getElementById('Address');

	// Step 2 inputs (category & description)
	const category = document.getElementById('CategoryID');
	const rteEditor = document.getElementById('rteEditor');
	const rteToolbar = document.getElementById('rteToolbar');
	const descriptionHidden = document.getElementById('descriptionHidden');

	// Step 3 inputs (file uploads)
	const fileInput = document.getElementById('fileInput');
	const fileList = document.getElementById('fileList');
	const browseBtn = document.getElementById('browseBtn');

	// Hidden fields for step state
	const currentStepInput = document.getElementById('CurrentStep');
	const stepHistoryInput = document.getElementById('StepHistory');

	// Review page summary fields
	const revAddress = document.getElementById('revAddress');
	const revLat = document.getElementById('revLat');
	const revLng = document.getElementById('revLng');
	const revCategory = document.getElementById('revCategory');
	const revDescription = document.getElementById('revDescription');
	const revFiles = document.getElementById('revFiles');

	// ------------------------------
	// 2. State Management
	// ------------------------------
	let stack = [1];      // Track history of visited steps
	let currentStep = 1;  // Track current step number

	// Restore step history if server provided it
	if (stepHistoryInput && stepHistoryInput.value) {
		const parsed = stepHistoryInput.value
			.split(',')
			.map(s => parseInt(s.trim(), 10))
			.filter(n => !Number.isNaN(n) && n >= 1 && n <= totalSteps);

		if (parsed.length) {
			stack = parsed;
			currentStep = stack[stack.length - 1]; // last one is current
		}
	} else {
		writeState(); // initialize hidden inputs
	}

	// ------------------------------
	// 3. Rich Text Editor (Step 2)
	// ------------------------------
	if (rteToolbar && rteEditor) {
		rteToolbar.addEventListener('click', (e) => {
			const btn = e.target.closest('button[data-cmd]');
			if (!btn) return;
			e.preventDefault();
			const cmd = btn.getAttribute('data-cmd');
			document.execCommand(cmd, false, null); // old-school but works
			rteEditor.focus();
			syncDescription();
		});

		rteEditor.addEventListener('input', syncDescription);
	}

	// ------------------------------
	// 4. File Uploads (Step 3)
	// ------------------------------
	if (browseBtn && fileInput) {
		browseBtn.addEventListener('click', () => fileInput.click());
	}
	if (fileInput && fileList) {
		fileInput.addEventListener('change', renderSelectedFiles);
	}

	// ------------------------------
	// 5. Navigation (Prev/Next)
	// ------------------------------
	prevBtn.addEventListener('click', () => {
		if (stack.length > 1) {
			stack.pop(); // remove current
			const previous = peek();
			setStep(previous);
		}
	});

	nextBtn.addEventListener('click', async () => {
		if (currentStep >= totalSteps) return;

		// Collect the data for this step
		const data = collectStepData(currentStep);

		// Save step data to server before moving forward
		await saveStep(currentStep, data);

		// Move forward
		const next = currentStep + 1;
		stack.push(next);
		setStep(next);
	});

	// ------------------------------
	// 6. Initialize UI
	// ------------------------------
	setStep(currentStep);

	// ------------------------------
	// 7. Helper Functions
	// ------------------------------

	function peek() {
		return stack[stack.length - 1];
	}

	function setStep(step) {
		currentStep = step;

		steps.forEach(s => {
			const n = parseInt(s.getAttribute('data-step'), 10);
			s.classList.toggle('flex-hidden', n !== step);
		});

		const pct = Math.max(1, Math.min(totalSteps, step)) / totalSteps * 100;
		if (progressBar) progressBar.style.width = `${pct}%`;

		if (engagementMessage) {
			engagementMessage.textContent = `Let’s get started — Step ${step} of ${totalSteps}`;
		}

		prevBtn.disabled = stack.length <= 1;

		if (step >= totalSteps) {
			nextBtn.classList.add('flex-hidden');
		} else {
			nextBtn.classList.remove('flex-hidden');
			nextBtn.textContent = (step === totalSteps - 1) ? 'Review' : 'Next';
		}

		if (step === totalSteps) {
			syncAddress();
			syncDescription();
			fillReview();
		}

		writeState();
	}

	function writeState() {
		if (currentStepInput) currentStepInput.value = String(currentStep);
		if (stepHistoryInput) stepHistoryInput.value = stack.join(',');
	}

	function syncAddress() {
		if (!addressHidden) return;
		const parts = [val(street), val(suburb), val(city), val(postal)]
			.map(s => s.trim())
			.filter(s => s.length > 0);
		addressHidden.value = parts.join(', ');
	}

	function syncDescription() {
		if (!descriptionHidden || !rteEditor) return;
		descriptionHidden.value = rteEditor.innerHTML.trim();
	}

	function fillReview() {
		if (revAddress) revAddress.textContent = addressHidden ? addressHidden.value : '';
		if (revLat) revLat.textContent = val(lat);
		if (revLng) revLng.textContent = val(lng);

		if (revCategory && category) {
			const text = category.selectedOptions.length
				? category.selectedOptions[0].textContent
				: '';
			revCategory.textContent = text || '';
		}

		if (revDescription && descriptionHidden) {
			revDescription.innerHTML = descriptionHidden.value || '<em>No description provided</em>';
		}

		if (revFiles) {
			if (fileInput && fileInput.files && fileInput.files.length) {
				const names = Array.from(fileInput.files).map(f => f.name);
				revFiles.textContent = names.join(', ');
			} else {
				revFiles.textContent = 'No files selected';
			}
		}
	}

	function renderSelectedFiles() {
		if (!fileList || !fileInput) return;
		fileList.innerHTML = '';
		const files = Array.from(fileInput.files || []);
		if (!files.length) return;
		for (const f of files) {
			const li = document.createElement('li');
			li.textContent = `${f.name} (${Math.round(f.size / 1024)} KB)`;
			fileList.appendChild(li);
		}
	}

	function val(input) {
		return input && input.value !== undefined ? input.value : '';
	}

	// ------------------------------
	// 8. NEW: Step Data Collection
	// ------------------------------
	function collectStepData(step) {
		const data = {};
		if (step === 1) {
			data.Street = val(street);
			data.Suburb = val(suburb);
			data.City = val(city);
			data.PostalCode = val(postal);
			data.Latitude = val(lat);
			data.Longitude = val(lng);
		}
		if (step === 2) {
			syncDescription();
			data.CategoryID = category ? category.value : "";
			data.Description = descriptionHidden ? descriptionHidden.value : "";
		}
		if (step === 3) {
			if (fileInput && fileInput.files.length) {
				data.Files = Array.from(fileInput.files).map(f => f.name);
			}
		}
		return data;
	}

	// ------------------------------
	// 9. NEW: AJAX Save Step
	// ------------------------------
	async function saveStep(step, data) {
		try {
			const response = await fetch('/Report/SaveStep', {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json',
					'RequestVerificationToken': getAntiForgeryToken()
				},
				body: JSON.stringify({ step, data })
			});

			if (!response.ok) {
				console.error("Save failed", response.status);
				return;
			}

			const result = await response.json();
			if (!result.success) {
				console.error("Server rejected save", result);
			}
		} catch (err) {
			console.error("Error saving step", err);
		}
	}

	// ------------------------------
	// 10. NEW: Anti-forgery Token Helper
	// ------------------------------
	function getAntiForgeryToken() {
		const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
		return tokenInput ? tokenInput.value : '';
	}
});
