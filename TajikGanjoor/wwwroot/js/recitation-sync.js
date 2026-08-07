/* Syncs poem text highlighting to an <audio> recitation's timing XML.
   Simplified port of the equivalent logic in the main ganjoor.net site
   (hilightverse/_setXml/_trackTimeChanged in wwwroot/js/bk.js and
   Pages/Partials/GanjoorPage/PageTypes/_PoemPagePartial.cshtml) - this
   version drops the inline pause/scroll-lock buttons injected into the
   verse itself, since that needs UI TajikGanjoor doesn't have; it only
   does the highlighting.

   Verse elements are counted in document order across .m1, .m2 (the two
   halves of a couplet), .b2 > p (rubaʼi-style already-centered couplets),
   .n and .l (paragraph/single-line verses) - same shape PrepareHtmlText
   produces for both the Persian and Tajik sites, so the sync XML's
   VerseOrder values line up the same way. */
(function () {
    function getVerseElements() {
        return Array.prototype.slice.call(
            document.querySelectorAll('.m1, .m2, .n, .l, .b2 > p')
        );
    }

    function setActiveVerse(elements, index, previousIndex) {
        if (previousIndex >= 0 && previousIndex < elements.length && previousIndex !== index) {
            elements[previousIndex].classList.remove('tg-verse-active');
        }
        if (index >= 0 && index < elements.length) {
            elements[index].classList.add('tg-verse-active');
            if (elements[index].scrollIntoView) {
                elements[index].scrollIntoView({ block: 'center', behavior: 'smooth' });
            }
        }
    }

    window.tgInitRecitationSync = function (audioEl, xmlUrl) {
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
        });

        audioEl.addEventListener('ended', function () {
            setActiveVerse(verseElements || [], -1, lastHighlight);
            lastHighlight = -1;
        });
    };
})();
