(function () {
  'use strict';

  var toggle = document.getElementById('headerSearchToggle');
  var form = document.getElementById('headerSearchForm');
  var input = document.getElementById('headerSearchInput');

  if (!toggle || !form || !input) return;

  var isExpanded = false;

  function expand() {
    isExpanded = true;
    form.classList.add('expanded');
    toggle.setAttribute('aria-expanded', 'true');
    input.focus();
  }

  function collapse() {
    isExpanded = false;
    form.classList.remove('expanded');
    toggle.setAttribute('aria-expanded', 'false');
    input.value = '';
  }

  toggle.addEventListener('click', function (e) {
    e.preventDefault();
    if (!isExpanded) {
      expand();
    } else if (input.value.trim()) {
      form.submit();
    } else {
      collapse();
    }
  });

  input.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      collapse();
      toggle.focus();
    }
  });

  document.addEventListener('click', function (e) {
    if (isExpanded && !form.contains(e.target) && !toggle.contains(e.target)) {
      collapse();
    }
  });
})();
