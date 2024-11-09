// Create tooltip elements
var $close = $('<a href="#" id="vaabx">x</a>').css({
    cursor: "pointer",
    color: "white",
    padding: "0 10px",
    lineHeight: '2.5em', // Set line-height to match the tooltip height
    display: 'flex',      // Ensure it behaves like a flex item
    justifyContent: 'center', // Center horizontally
    alignItems: 'center'  // Center vertically
});

var $meaning = $('<a>لغتنامه</a>').css({
    boxShadow: '10px 0 0 -9px rgba(255,255,255,0.2), -10px 0 0 -9px rgba(255,255,255,0.2)',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center'
});

var $abjad = $('<a>ابجد</a>').css({
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center'
});

var $search = $('<a>🔍</a>').css({
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center'
});

var $quran = $('<a>قرآن</a>').css({
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center'
});

var $vazn = $('<a>وزن</a>').css({
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center'
});

// Tooltip container styling
var $tooltip = $('<div>').addClass('tooltip').css({
    transform: 'scale(0)',
    transformOrigin: 'top',
    position: 'absolute',
    height: '2.5em',
    width: '16em',
    borderRadius: '10px',
    display: 'flex',
    justifyContent: 'space-around',
    alignItems: 'center',
    background: 'rgba(14,17,17,0.9)',
    transition: 'transform 0.2s ease-out', // Add transition for scale effect
    zIndex: '2000',
}).append($close, $meaning, $abjad, $quran, $search, $vazn);

// Append tooltip to body
$(document.body).append($tooltip);

// Attach click event to the close button
$close.on("click", function (event) {
    event.preventDefault(); // Prevent default anchor behavior
    alert('منو موقتاً غیرفعال شد.\r\nبرای فعالسازی مجدد دوباره صفحه را بارگذاری کنید.'); // Alert to indicate the click was registered
    $tooltip.css({ transform: 'scale(0)' }); // Hide the tooltip
    document.removeEventListener('selectionchange', vaabSelectionChanged); // Remove the selection change listener
});

// Handle selection changes
var prevtext = '';
function vaabSelectionChanged() {
    var sel = window.getSelection() || document.getSelection();
    var text = sel.toString().trim();

    if (!text) {
        $tooltip.css({ transform: 'scale(0)' });
        prevtext = '';
        return;
    }

    // Normalize text (same logic as before)
    text = text.replaceAll("‌", " ")
        .replaceAll("ّ", "")
        .replaceAll("َ", "")
        .replaceAll("ِ", "")
        .replaceAll("ُ", "")
        .replaceAll("ً", "")
        .replaceAll("ٍ", "")
        .replaceAll("ٌ", "")
        .replaceAll(".", "")
        .replaceAll("،", "")
        .replaceAll("!", "")
        .replaceAll("؟", "")
        .replaceAll("ٔ", "")
        .replaceAll(":", "")
        .replaceAll("ئ", "ی")
        .replaceAll("؛", "")
        .replaceAll(";", "")
        .replaceAll("*", "")
        .replaceAll(")", "")
        .replaceAll("(", "")
        .replaceAll("[", "")
        .replaceAll("]", "")
        .replaceAll("\"", "")
        .replaceAll("'", "")
        .replaceAll("«", "")
        .replaceAll("»", "")
        .replaceAll("ْ", "");

    // Update tooltip links (same logic as before)
    $meaning.attr({
        href: 'https://www.vajehyab.com/?q=' + encodeURI(text),
        title: 'جستجو در واژه‌یاب',
        target: '_blank'
    });
    $abjad.attr({
        href: 'https://abjad.ganjoor.net/?q=' + encodeURI(text) + '&r=' + window.location.href,
        title: 'محاسبه ابجد معادل عبارت',
        target: '_blank'
    });
    $search.attr({
        href: text.indexOf(' ') == -1 ? 'https://ganjoor.net/search?s=' + encodeURI(text) : 'https://ganjoor.net/search?s="' + encodeURI(text) + '"',
        title: 'جستجوی عبارت در گنجور',
        target: '_blank'
    });
    $quran.attr({
        href: 'https://tanzil.ir/#search/quran/' + encodeURI(text),
        title: 'جستجوی عبارت در قرآن',
        target: '_blank'
    });
    $vazn.attr({
        href: 'http://sorud.info/?Text=' + encodeURI(text),
        title: 'تعیین وزن عبارت',
        target: '_blank'
    });

    // Position tooltip near the selection
    var rect = sel.getRangeAt(0).getBoundingClientRect();
    $tooltip.css({
        transform: 'scale(1)',
        top: rect.top + window.scrollY,
        left: rect.right - $tooltip[0].getBoundingClientRect().width,
        marginTop: '2em'
    });

    prevtext = text;
}

// Attach selection change listener
document.addEventListener('selectionchange', vaabSelectionChanged);
