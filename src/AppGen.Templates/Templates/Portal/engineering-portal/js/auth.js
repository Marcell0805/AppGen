(function () {
  function getSettings() {
    return (window.DELTACORE_PORTAL && DELTACORE_PORTAL.settings) || {};
  }
  function getStorageKey() {
    var auth = getSettings().auth;
    return (auth && auth.storageKey) || 'deltacore_portal_auth';
  }
  function getPassword() {
    var auth = getSettings().auth;
    return (auth && auth.password) || 'deltacore';
  }
  function unlock() {
    sessionStorage.setItem(getStorageKey(), '1');
    document.documentElement.classList.add('auth-ok');
    var gate = document.getElementById('auth-gate');
    if (gate) gate.remove();
  }
  function showGate() {
    var script = document.querySelector('script[src*="auth.js"]');
    var logoUrl = script ? new URL('../assets/logo.svg', script.src).href : '../assets/logo.svg';
    var s = getSettings();
    var title = s.portalName || 'DeltaCore Engineering Portal';
    var subtitle = s.tagline || 'Industrial intelligence platform';
    var gate = document.createElement('div');
    gate.id = 'auth-gate';
    gate.className = 'auth-gate';
    gate.innerHTML =
      '<div class="auth-gate-card">' +
        '<img src="' + logoUrl + '" alt="" class="auth-gate-logo">' +
        '<h2 class="auth-gate-title">' + title + '</h2>' +
        '<p class="auth-gate-subtitle">' + subtitle + '</p>' +
        '<form class="auth-gate-form" id="auth-form">' +
          '<input type="password" id="auth-password" class="auth-gate-input" placeholder="Enter password" autocomplete="off" autofocus>' +
          '<p class="auth-gate-error" id="auth-error" hidden>Incorrect password.</p>' +
          '<button type="submit" class="auth-gate-button">Enter</button>' +
        '</form>' +
      '</div>';
    document.body.prepend(gate);
    document.getElementById('auth-form').addEventListener('submit', function (e) {
      e.preventDefault();
      var input = document.getElementById('auth-password');
      var error = document.getElementById('auth-error');
      if (input.value === getPassword()) { unlock(); }
      else { error.hidden = false; input.value = ''; input.focus(); }
    });
  }
  function init() {
    if (sessionStorage.getItem(getStorageKey()) === '1') {
      document.documentElement.classList.add('auth-ok');
      return;
    }
    showGate();
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
