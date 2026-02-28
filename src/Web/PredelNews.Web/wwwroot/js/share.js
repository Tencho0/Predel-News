(function () {
    'use strict';
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-copy-url]');
        if (!btn) return;
        e.preventDefault();
        var url = btn.getAttribute('data-copy-url');
        if (!url) return;
        navigator.clipboard.writeText(url).then(function () {
            var icon = btn.querySelector('i');
            if (icon) {
                icon.className = 'bi bi-check-lg';
                setTimeout(function () { icon.className = 'bi bi-link-45deg'; }, 2000);
            }
        });
    });
})();
