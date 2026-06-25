(function () {
  'use strict';

  function esc(t) {
    var d = document.createElement('div');
    d.textContent = t == null ? '' : String(t);
    return d.innerHTML;
  }

  function statusBadge(status) {
    if (status === 'live') return '<span class="status-badge status-live">Live</span>';
    if (status === 'in_progress') return '<span class="status-badge status-progress">In Progress</span>';
    return '<span class="status-badge status-planned">Planned</span>';
  }

  function getNav() {
    return (window.DELTACORE_PORTAL && DELTACORE_PORTAL.nav && DELTACORE_PORTAL.nav.items) || [];
  }

  function getSection(id) {
    return window.DELTACORE_PORTAL && DELTACORE_PORTAL.sections && DELTACORE_PORTAL.sections[id];
  }

  var SVG_BACK = '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5"/><path d="M12 19l-7-7 7-7"/></svg>';
  var SVG_HOME = '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>';
  var SVG_PRINT = '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 9V2h12v7"/><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/></svg>';
  var SVG_SEARCH = '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>';

  function renderToolbar() {
    var mount = document.getElementById('portal-toolbar');
    if (!mount) return;
    var scope = document.body.getAttribute('data-nav-scope') || 'landing';
    var backHref = scope === 'section' ? '../index.html' : 'index.html';
    var backTitle = scope === 'section' ? 'Back to portal home' : 'Portal home';
    mount.outerHTML =
      '<header class="portal-toolbar no-print" aria-label="Page tools">' +
        '<div class="toolbar-inner">' +
          (scope === 'section'
            ? '<a href="' + backHref + '" class="toolbar-btn" title="' + backTitle + '">' + SVG_BACK + '</a>'
            : '') +
          '<a href="../index.html" class="toolbar-btn" title="Home" style="' + (scope === 'landing' ? 'display:none' : '') + '">' + SVG_HOME + '</a>' +
          '<button type="button" class="toolbar-btn" id="toolbar-search-btn" title="Search (Ctrl+K)">' + SVG_SEARCH + '</button>' +
          '<button type="button" class="toolbar-btn toolbar-print" title="Print (Ctrl+P)">' + SVG_PRINT + '</button>' +
        '</div>' +
      '</header>';
    document.querySelectorAll('.toolbar-print').forEach(function (btn) {
      btn.addEventListener('click', function () { window.print(); });
    });
    var searchBtn = document.getElementById('toolbar-search-btn');
    if (searchBtn && window.DeltaCoreSearch) searchBtn.addEventListener('click', window.DeltaCoreSearch.open);
  }

  function renderSidebar() {
    var aside = document.querySelector('[data-portal-sidebar]');
    if (!aside) return;
    var active = document.body.getAttribute('data-section-id') || '';
    var scope = document.body.getAttribute('data-nav-scope') || 'section';
    var prefix = scope === 'section' ? '' : 'sections/';
    var html = '<nav class="sidebar-nav"><ol>';
    getNav().forEach(function (item) {
      var label = item.num + '. ' + item.label;
      if (item.id === active) {
        html += '<li class="active">' + esc(label) + '</li>';
      } else if (item.available && item.file) {
        html += '<li><a href="' + prefix + item.file + '">' + esc(label) + '</a></li>';
      } else {
        html += '<li class="unavailable">' + esc(label) + ' <em>(soon)</em></li>';
      }
    });
    html += '</ol></nav>';
    var section = getSection(active);
    if (section && section.sidebarNote) {
      html += '<div class="sidebar-note"><strong>Note</strong><p>' + esc(section.sidebarNote) + '</p></div>';
    }
    aside.innerHTML = html;
  }

  function renderSection() {
    var mount = document.getElementById('section-content');
    var id = document.body.getAttribute('data-section-id');
    if (!mount || !id) return;
    var section = getSection(id);
    if (!section) {
      mount.innerHTML = '<p>Section not found.</p>';
      return;
    }
    var html = '<header class="section-header">' +
      '<h1>' + esc(section.title) + '</h1>' +
      statusBadge(section.status) +
      '</header>';
    if (section.summary) html += '<p class="section-summary">' + esc(section.summary) + '</p>';
    (section.blocks || []).forEach(function (b) {
      html += '<section class="content-block" id="' + esc(b.id || '') + '">';
      if (b.heading) html += '<h2>' + esc(b.heading) + '</h2>';
      if (b.content) html += '<p>' + esc(b.content) + '</p>';
      if (b.bullets && b.bullets.length) {
        html += '<ul>';
        b.bullets.forEach(function (li) { html += '<li>' + esc(li) + '</li>'; });
        html += '</ul>';
      }
      html += '</section>';
    });
    mount.innerHTML = html;
  }

  function renderLandingNav() {
    var mount = document.querySelector('[data-portal-landing-nav]');
    if (!mount) return;
    var html = '';
    getNav().forEach(function (item) {
      if (item.available && item.file) {
        html += '<li><a href="sections/' + item.file + '">' + item.num + '. ' + esc(item.label) + '</a></li>';
      } else {
        html += '<li class="unavailable">' + item.num + '. ' + esc(item.label) + ' <em>(coming soon)</em></li>';
      }
    });
    mount.innerHTML = html;
  }

  function init() {
    renderToolbar();
    renderSidebar();
    renderSection();
    renderLandingNav();
    var settings = (window.DELTACORE_PORTAL && DELTACORE_PORTAL.settings) || {};
    var tagline = document.querySelector('[data-portal-tagline]');
    if (tagline && settings.homeQuote) tagline.textContent = settings.homeQuote;
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
