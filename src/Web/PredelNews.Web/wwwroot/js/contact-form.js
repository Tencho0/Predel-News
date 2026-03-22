'use strict';
(function () {
    const form = document.getElementById('contact-form');
    if (!form) return;

    const submitBtn = form.querySelector('button[type="submit"]');
    const messageDiv = document.getElementById('contact-message');

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        clearErrors();

        const name = form.querySelector('#c-name').value.trim();
        const email = form.querySelector('#c-email').value.trim();
        const subject = form.querySelector('#c-subject').value.trim();
        const message = form.querySelector('#c-message').value.trim();

        // Client-side validation
        let hasError = false;
        if (!name) { showFieldError('c-name', '\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.'); hasError = true; }
        if (!email) { showFieldError('c-email', '\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u0439\u043b\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.'); hasError = true; }
        else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { showFieldError('c-email', '\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u0435\u043d \u0438\u043c\u0435\u0439\u043b \u0430\u0434\u0440\u0435\u0441.'); hasError = true; }
        if (!subject) { showFieldError('c-subject', '\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0422\u0435\u043c\u0430\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.'); hasError = true; }
        if (!message) { showFieldError('c-message', '\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.'); hasError = true; }
        if (hasError) return;

        submitBtn.disabled = true;
        submitBtn.textContent = '\u0418\u0437\u043f\u0440\u0430\u0449\u0430\u043d\u0435...';

        try {
            const formData = new FormData(form);
            const token = form.querySelector('input[name="__RequestVerificationToken"]').value;

            const response = await fetch('/api/contact', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': token },
                body: formData
            });

            const data = await response.json();

            if (response.status === 429) {
                showAlert('\u041c\u043e\u043b\u044f, \u043e\u043f\u0438\u0442\u0430\u0439\u0442\u0435 \u043e\u0442\u043d\u043e\u0432\u043e \u0441\u043b\u0435\u0434 \u043d\u044f\u043a\u043e\u043b\u043a\u043e \u043c\u0438\u043d\u0443\u0442\u0438.', 'warning');
            } else if (response.ok) {
                showAlert(data.message, 'success');
                form.reset();
            } else {
                showAlert(data.message, 'danger');
            }
        } catch {
            showAlert('\u0412\u044a\u0437\u043d\u0438\u043a\u043d\u0430 \u0433\u0440\u0435\u0448\u043a\u0430. \u041c\u043e\u043b\u044f, \u043e\u043f\u0438\u0442\u0430\u0439\u0442\u0435 \u043e\u0442\u043d\u043e\u0432\u043e.', 'danger');
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = '\u0418\u0437\u043f\u0440\u0430\u0442\u0435\u0442\u0435';
        }
    });

    function showAlert(msg, type) {
        messageDiv.className = 'alert alert-' + type;
        messageDiv.textContent = msg;
        messageDiv.classList.remove('d-none');
        setTimeout(function () { messageDiv.classList.add('d-none'); }, 6000);
    }

    function showFieldError(fieldId, msg) {
        const field = document.getElementById(fieldId);
        if (!field) return;
        field.classList.add('is-invalid');
        let feedback = field.nextElementSibling;
        if (!feedback || !feedback.classList.contains('invalid-feedback')) {
            feedback = document.createElement('div');
            feedback.className = 'invalid-feedback';
            field.parentNode.insertBefore(feedback, field.nextSibling);
        }
        feedback.textContent = msg;
    }

    function clearErrors() {
        form.querySelectorAll('.is-invalid').forEach(function (el) { el.classList.remove('is-invalid'); });
        form.querySelectorAll('.invalid-feedback').forEach(function (el) { el.remove(); });
        messageDiv.classList.add('d-none');
    }
})();
