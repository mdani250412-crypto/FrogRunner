/* ============================================================
   FROGRUNNER RELEASE CONFIG — EDIT THIS BLOCK ONLY
   After you publish a GitHub Release, paste the direct asset
   download URLs here. Everything on the page reads from this.
   ============================================================ */
const RELEASE_CONFIG = {
  releaseTag: "v0.1.0-test",
  releaseNotes: "First test build — 10 rounds, manual controls, river logs, crocodiles, animal police, boost cards, save slots.",
  androidApkUrl: "downloads/FrogRunner-Android.apk",   // Replace with GitHub Releases direct .apk URL
  windowsZipUrl: "downloads/FrogRunner-Windows.zip",   // Replace with GitHub Releases direct .zip URL
  // Set to true once you've pasted real GitHub Releases URLs above
  configured: false
};

/* ============================================================
   END CONFIG — no need to edit below this line
   ============================================================ */

(function () {
  'use strict';

  // --- Apply config to the page ---
  function applyConfig() {
    // Version badge
    document.querySelectorAll('[data-release-tag]').forEach(function (el) {
      el.textContent = RELEASE_CONFIG.releaseTag;
    });

    // Android button
    var androidBtn = document.querySelector('a[data-platform="android"]');
    if (androidBtn) {
      androidBtn.href = RELEASE_CONFIG.androidApkUrl;
      if (!RELEASE_CONFIG.configured) {
        androidBtn.classList.add('unconfigured');
      }
    }

    // Windows button
    var windowsBtn = document.querySelector('a[data-platform="windows"]');
    if (windowsBtn) {
      windowsBtn.href = RELEASE_CONFIG.windowsZipUrl;
      if (!RELEASE_CONFIG.configured) {
        windowsBtn.classList.add('unconfigured');
      }
    }

    // Release notes line
    var notesEl = document.getElementById('releaseNotesLine');
    if (notesEl) {
      notesEl.textContent = RELEASE_CONFIG.releaseNotes;
    }

    // Unconfigured notice
    var notice = document.getElementById('unconfiguredNotice');
    if (notice) {
      notice.hidden = RELEASE_CONFIG.configured;
    }
  }

  // Smooth scroll
  document.querySelectorAll('a[href^="#"]').forEach(function (link) {
    link.addEventListener('click', function (e) {
      var id = this.getAttribute('href');
      if (id.length > 1) {
        var target = document.querySelector(id);
        if (target) {
          e.preventDefault();
          target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      }
    });
  });

  // Fallback: check if local build files exist
  function checkFile(url, platform) {
    var link = document.querySelector('a[data-platform="' + platform + '"]');
    if (!link) return;
    // Skip check for external (GitHub) URLs
    if (url.indexOf('http') === 0 && url.indexOf(window.location.hostname) === -1) return;

    fetch(url, { method: 'HEAD' })
      .then(function (res) {
        if (!res.ok) throw new Error('not found');
      })
      .catch(function () {
        link.classList.add('unavailable');
        link.setAttribute('aria-disabled', 'true');
        link.addEventListener('click', function (e) {
          e.preventDefault();
          showFallback('The ' + platform + ' build file is not available yet. Please check back soon.');
        });
        var fallbackNote = document.getElementById('fallbackNote');
        if (fallbackNote) fallbackNote.hidden = false;
      });
  }

  function showFallback(msg) {
    var fallbackNote = document.getElementById('fallbackNote');
    if (fallbackNote) {
      fallbackNote.textContent = '⚠️ ' + msg;
      fallbackNote.hidden = false;
      fallbackNote.scrollIntoView({ behavior: 'smooth', block: 'center' });
    } else {
      alert(msg);
    }
  }

  // Init
  document.addEventListener('DOMContentLoaded', function () {
    applyConfig();
    if (!RELEASE_CONFIG.configured) {
      checkFile(RELEASE_CONFIG.androidApkUrl, 'android');
      checkFile(RELEASE_CONFIG.windowsZipUrl, 'windows');
    }
  });
})();
