// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Homepage inline poet search - simplified port of onInlineSearch (GanjooRazor's wwwroot/js/bk.js):
// substring-matches the typed text against each poet card's data-value (Cyrillic nickname) and
// clones the matches into the results container. No numeral conversion needed here (unlike the
// Persian site, which converts typed digits to Persian-Indic numerals first) since Tajik Cyrillic
// uses plain Western digits.
function tgInlineSearch(value) {
    var resultsNode = document.getElementById('tg-found-poets');
    if (!resultsNode) return;
    resultsNode.innerHTML = '';
    if (value.length === 0) return;

    var needle = value.toLowerCase();
    var cards = document.querySelectorAll('.tg-poet-card[data-value]');
    for (var i = 0; i < cards.length; i++) {
        var haystack = cards[i].getAttribute('data-value').toLowerCase();
        if (haystack.indexOf(needle) !== -1) {
            resultsNode.appendChild(cards[i].cloneNode(true));
        }
    }
}
