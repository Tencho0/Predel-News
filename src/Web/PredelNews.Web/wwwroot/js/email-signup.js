'use strict';
(function () {
    const form = document.getElementById('email-signup-form');
    if (!form) return;

    const messageDiv = document.getElementById('signup-message');
    const submitBtn = form.querySelector('button[type="submit"]');

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        messageDiv.classList.add('d-none');

        const email = form.querySelector('#signup-email').value.trim();
        const consent = form.querySelector('#signup-consent').checked;

        if (!email) { showMsg('\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u0439\u043b\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.', 'warning'); return; }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { showMsg('\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u0435\u043d \u0438\u043c\u0435\u0439\u043b \u0430\u0434\u0440\u0435\u0441.', 'warning'); return; }
        if (!consent) { showMsg('\u041c\u043e\u043b\u044f, \u043e\u0442\u0431\u0435\u043b\u0435\u0436\u0435\u0442\u0435 \u0441\u044a\u0433\u043b\u0430\u0441\u0438\u0435\u0442\u043e \u0437\u0430 \u043f\u043e\u043b\u0443\u0447\u0430\u0432\u0430\u043d\u0435 \u043d\u0430 \u0438\u043c\u0435\u0439\u043b\u0438.', 'warning'); return; }

        submitBtn.disabled = true;

        try {
            const formData = new FormData(form);
            const token = form.querySelector('input[name="__RequestVerificationToken"]').value;

            const response = await fetch('/api/email-signup', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': token },
                body: formData
            });

            const data = await response.json();

            if (response.status === 429) {
                showMsg('\u041c\u043e\u043b\u044f, \u043e\u043f\u0438\u0442\u0430\u0439\u0442\u0435 \u043e\u0442\u043d\u043e\u0432\u043e \u0441\u043b\u0435\u0434 \u043d\u044f\u043a\u043e\u043b\u043a\u043e \u043c\u0438\u043d\u0443\u0442\u0438.', 'warning');
            } else if (response.ok) {
                showMsg(data.message, 'success');
                form.reset();
            } else {
                showMsg(data.message, 'warning');
            }
        } catch {
            showMsg('\u0412\u044a\u0437\u043d\u0438\u043a\u043d\u0430 \u0433\u0440\u0435\u0448\u043a\u0430. \u041c\u043e\u043b\u044f, \u043e\u043f\u0438\u0442\u0430\u0439\u0442\u0435 \u043e\u0442\u043d\u043e\u0432\u043e.', 'warning');
        } finally {
            submitBtn.disabled = false;
        }
    });

    function showMsg(msg, type) {
        messageDiv.textContent = msg;
        messageDiv.className = type === 'success'
            ? 'text-center mt-2 text-white-50'
            : 'text-center mt-2 text-warning';
        messageDiv.classList.remove('d-none');
        if (type === 'success') setTimeout(function () { messageDiv.classList.add('d-none'); }, 6000);
    }
})();
