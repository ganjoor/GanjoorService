/* Syncs poem text highlighting to an <audio> recitation's timing XML.
   Simplified port of the equivalent logic in the main ganjoor.net site
   (hilightverse/_setXml/_trackTimeChanged in wwwroot/js/bk.js and
   Pages/Partials/GanjoorPage/PageTypes/_PoemPagePartial.cshtml) - this
   version drops the inline pause button injected into the verse itself,
   since that needs UI TajikGanjoor doesn't have; it only does the
   highlighting, plus a shared sticky mini-player (also ported from the
   main site) instead of the inline controls.

   Verse elements are counted in document order across .m1, .m2 (the two
   halves of a couplet), .b2 > p (rubaʼi-style already-centered couplets,
   excluding the interlinear Persian .tg-fa line added alongside each one),
   .n and .l (paragraph/single-line verses) - same shape PrepareHtmlText
   produces for both the Persian and Tajik sites, so the sync XML's
   VerseOrder values line up the same way. */
(function () {
    var LOCK_CLOSED_SVG = '<svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor"><path d="M12 17a2 2 0 002-2 2 2 0 00-2-2 2 2 0 00-2 2 2 2 0 002 2m6-9a2 2 0 012 2v10a2 2 0 01-2 2H6a2 2 0 01-2-2V10a2 2 0 012-2h1V6a5 5 0 0110 0v2zm-6-5a3 3 0 00-3 3v2h6V6a3 3 0 00-3-3z"/></svg>';
    var LOCK_OPEN_SVG = '<svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor"><path d="M12 17a2 2 0 002-2 2 2 0 00-2-2 2 2 0 00-2 2 2 2 0 002 2m6-9h-1V6a5 5 0 00-10 0h1.9a3.1 3.1 0 016.2 0v2H6a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V10a2 2 0 00-2-2z"/></svg>';

    function getCookie(name) {
        var match = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
        return match ? decodeURIComponent(match[1]) : null;
    }

    function setCookie(name, value, days) {
        var expires = new Date(Date.now() + days * 24 * 60 * 60 * 1000).toUTCString();
        document.cookie = name + '=' + encodeURIComponent(value) + '; expires=' + expires + '; path=/';
    }

    // default: off - the previously-unconditional auto-scroll was reported
    // as confusing (users didn't know how to turn it off), so now it starts
    // disabled and only turns on if a user explicitly enables it
    var scrollLock = getCookie('tgScrollLock') === 'true';

    function updateLockIcon() {
        var icon = document.getElementById('tg-sticky-lock-icon');
        if (!icon) return;
        icon.innerHTML = scrollLock ? LOCK_CLOSED_SVG : LOCK_OPEN_SVG;
    }

    window.tgSwitchScrollLock = function () {
        scrollLock = !scrollLock;
        setCookie('tgScrollLock', scrollLock ? 'true' : 'false', 365);
        updateLockIcon();
    };

    function getVerseElements() {
        return Array.prototype.slice.call(
            document.querySelectorAll('.m1, .m2, .n, .l, .b2 > p:not(.tg-fa)')
        );
    }

    function setActiveVerse(elements, index, previousIndex) {
        if (previousIndex >= 0 && previousIndex < elements.length && previousIndex !== index) {
            elements[previousIndex].classList.remove('tg-verse-active');
        }
        if (index >= 0 && index < elements.length) {
            elements[index].classList.add('tg-verse-active');
            if (scrollLock && elements[index].scrollIntoView) {
                elements[index].scrollIntoView({ block: 'center', behavior: 'smooth' });
            }
        }
    }

    // Jumps to whichever verse is currently marked active, regardless of the
    // scroll-lock setting - a one-shot jump distinct from the continuous
    // auto-scroll that scrollLock controls.
    window.tgScrollToCurrentVerse = function () {
        var active = document.querySelector('.tg-verse-active');
        if (active && active.scrollIntoView) {
            active.scrollIntoView({ block: 'center', behavior: 'smooth' });
        }
    };

    // ---------- sticky mini audio-player ----------
    // Re-parents the actual playing <audio> element into a fixed bottom bar,
    // same technique as the main site's showStickyPlayer/closeStickyPlayer in
    // bk.js: moving an <audio> element via appendChild does not interrupt
    // playback, so its native controls stay reachable while scrolling.
    var stickyOriginalParent = null;
    var stickyOriginalNextSibling = null;
    var stickyDismissed = false;
    var stickyLastAudioId = null;

    function showStickyPlayer(audioEl, narratorName, audioId) {
        if (audioId !== stickyLastAudioId) {
            stickyDismissed = false;
            stickyLastAudioId = audioId;
        }
        if (stickyDismissed) return;

        var bar = document.getElementById('tg-sticky-player');
        var slot = document.getElementById('tg-sticky-audio-slot');
        if (!bar || !slot) return;

        if (audioEl.parentElement !== slot) {
            // if a different recitation is already sitting in the slot, put it
            // back where it came from first so the slot never holds two
            // <audio> elements at once
            var existing = slot.firstElementChild;
            if (existing && existing !== audioEl && stickyOriginalParent) {
                if (stickyOriginalNextSibling) {
                    stickyOriginalParent.insertBefore(existing, stickyOriginalNextSibling);
                } else {
                    stickyOriginalParent.appendChild(existing);
                }
            }

            stickyOriginalParent = audioEl.parentElement;
            stickyOriginalNextSibling = audioEl.nextSibling;
            slot.appendChild(audioEl);
        }

        var narratorSpan = document.getElementById('tg-sticky-narrator');
        if (narratorSpan) narratorSpan.textContent = narratorName || '';
        updateLockIcon();
        bar.style.display = 'flex';
    }

    window.tgCloseStickyPlayer = function () {
        var slot = document.getElementById('tg-sticky-audio-slot');
        var bar = document.getElementById('tg-sticky-player');
        if (!slot || !bar) return;

        var audioEl = slot.firstElementChild;
        if (audioEl && stickyOriginalParent) {
            if (stickyOriginalNextSibling) {
                stickyOriginalParent.insertBefore(audioEl, stickyOriginalNextSibling);
            } else {
                stickyOriginalParent.appendChild(audioEl);
            }
        }

        bar.style.display = 'none';
        stickyDismissed = true;
    };

    window.tgInitRecitationSync = function (audioEl, xmlUrl, narratorName, audioId) {
        var verseStart = [];
        var verseEnd = [];
        var verseIndex = [];
        var vCount = 0;
        var lastHighlight = -1;
        var verseElements = null;

        fetch(xmlUrl)
            .then(function (res) { return res.text(); })
            .then(function (text) {
                var xml = new window.DOMParser().parseFromString(text, 'text/xml');
                var bugFixNode = xml.querySelector('OneSecondBugFix');
                var oneSecondBugFix = bugFixNode ? parseInt(bugFixNode.textContent, 10) : 2000;

                var syncNodes = xml.querySelectorAll('SyncInfo');
                var v = 0;
                syncNodes.forEach(function (node) {
                    var msNode = node.querySelector('AudioMiliseconds');
                    var orderNode = node.querySelector('VerseOrder');
                    if (!msNode || !orderNode) return;
                    verseStart[v] = parseInt(msNode.textContent, 10) / oneSecondBugFix;
                    verseIndex[v] = parseInt(orderNode.textContent, 10);
                    if (v > 0) verseEnd[v - 1] = verseStart[v];
                    v++;
                });
                v--;
                if (v > 1) verseEnd[v] = verseStart[v] + 2 * (verseEnd[v - 1] - verseStart[v - 1]);
                vCount = v;
                verseElements = getVerseElements();
            })
            .catch(function () { /* sync data unavailable - audio still plays normally without highlighting */ });

        audioEl.addEventListener('timeupdate', function () {
            if (!verseElements) return;
            var currentTime = audioEl.currentTime;
            if (currentTime <= 0) return;
            for (var i = 0; i <= vCount; i++) {
                if (currentTime >= verseStart[i] && currentTime <= verseEnd[i]) {
                    if (verseIndex[i] !== lastHighlight) {
                        setActiveVerse(verseElements, verseIndex[i], lastHighlight);
                        lastHighlight = verseIndex[i];
                    }
                    break;
                }
            }
        });

        audioEl.addEventListener('play', function () {
            document.querySelectorAll('audio').forEach(function (other) {
                if (other !== audioEl) other.pause();
            });
            showStickyPlayer(audioEl, narratorName, audioId);
        });

        audioEl.addEventListener('ended', function () {
            setActiveVerse(verseElements || [], -1, lastHighlight);
            lastHighlight = -1;
        });
    };
})();
