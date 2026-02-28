(function () {
    'use strict';
    var toggle = document.querySelector('[data-nav-toggle]');
    var nav = document.querySelector('[data-nav-menu]');
    if (toggle && nav) {
        toggle.addEventListener('click', function () {
            var expanded = toggle.getAttribute('aria-expanded') === 'true';
            toggle.setAttribute('aria-expanded', String(!expanded));
            nav.hidden = expanded;
        });
    }
})();
