// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Homepage inline poet search - simplified port of onInlineSearch (GanjooRazor's wwwroot/js/bk.js):
// substring-matches the typed text against each poet card's data-value (Cyrillic nickname) and
// clones the matches into the results container. No numeral conversion needed here (unlike the
// Persian site, which converts typed digits to Persian-Indic numerals first) since Tajik Cyrillic
// uses plain Western digits.
//
// A "popular" poet's card appears more than once in the page (once in the popular-poets section,
// again in their real century group, and again in the alphabetical listing) - deduped here by
// data-poet-id so a matching search doesn't show the same poet two or three times.
function tgInlineSearch(value) {
    var resultsNode = document.getElementById('tg-found-poets');
    if (!resultsNode) return;
    resultsNode.innerHTML = '';
    if (value.length === 0) return;

    var needle = value.toLowerCase();
    var cards = document.querySelectorAll('.tg-poet-card[data-value]');
    var seenIds = {};
    for (var i = 0; i < cards.length; i++) {
        var poetId = cards[i].getAttribute('data-poet-id');
        if (poetId && seenIds[poetId]) continue;

        var haystack = cards[i].getAttribute('data-value').toLowerCase();
        if (haystack.indexOf(needle) !== -1) {
            resultsNode.appendChild(cards[i].cloneNode(true));
            if (poetId) seenIds[poetId] = true;
        }
    }
}

// Toggles the homepage between the century-grouped view and the flat alphabetical listing.
function tgSwitchView(view) {
    var centuryView = document.getElementById('tg-view-century');
    var alphaView = document.getElementById('tg-view-alpha');
    if (!centuryView || !alphaView) return;

    centuryView.style.display = view === 'century' ? '' : 'none';
    alphaView.style.display = view === 'alpha' ? '' : 'none';

    var buttons = document.querySelectorAll('.tg-view-switch-btn');
    for (var i = 0; i < buttons.length; i++) {
        buttons[i].classList.toggle('active', buttons[i].getAttribute('data-view') === view);
    }
}
