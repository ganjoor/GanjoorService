// Create tooltip elements
var $meaning = $('<a>لغتنامه</a>').css({
    padding: '10px',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    textAlign: 'center',
});

var $abjad = $('<a>ابجد</a>').css({
    padding: '10px',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    textAlign: 'center',
});

var $search = $('<a>🔍</a>').css({
    padding: '10px',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    textAlign: 'center',
});

var $quran = $('<a>قرآن</a>').css({
    padding: '10px',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    textAlign: 'center',
});

var $vazn = $('<a>وزن</a>').css({
    padding: '10px',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    textAlign: 'center',
});

var $google = $('<a>گوگل</a>').css({
    padding: '10px',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    textAlign: 'center',
    color: 'white',
});

var $close = $('<a href="#" id="vaabx">غیرفعال شود</a>').css({
    cursor: "pointer",
    color: "white",
    padding: "10px",
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    textAlign: 'center',
    borderTop: '1px solid rgba(255,255,255,0.2)', // Visual separator
});

// Tooltip container styling
var $tooltip = $('<div>').addClass('tooltip').css({
    transform: 'scale(0)',
    transformOrigin: 'top',
    position: 'absolute',
    borderRadius: '10px',
    display: 'flex',
    flexDirection: 'column', // Vertical layout
    background: 'rgba(14,17,17,0.9)',
    transition: 'transform 0.2s ease-out',
    zIndex: '2000',
    maxWidth: '90%', // Responsive width for smaller screens
    wordWrap: 'break-word',
    padding: '5px',
});

// Append links and close button to tooltip
$tooltip.append($meaning, $abjad, $quran, $search, $google, $vazn, $close);

// Append tooltip to body
$(document.body).append($tooltip);

// Attach click event to the close button
$close.on("click", function (event) {
    event.preventDefault();
    alert('منو موقتاً غیرفعال شد.\r\nبرای فعالسازی مجدد دوباره صفحه را بارگذاری کنید.');
    $tooltip.css({ transform: 'scale(0)' });
    document.removeEventListener('selectionchange', vaabSelectionChanged);
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

    // Update tooltip links
    $meaning.attr({
        href: 'https://www.vajehyab.com/?q=' + encodeURI(text),
        title: 'جستجو در واژه‌یاب',
        target: '_blank',
    });
    $abjad.attr({
        href: 'https://abjad.ganjoor.net/?q=' + encodeURI(text) + '&r=' + window.location.href,
        title: 'محاسبه ابجد معادل عبارت',
        target: '_blank',
    });
    $search.attr({
        href: text.indexOf(' ') == -1 ? 'https://ganjoor.net/search?s=' + encodeURI(text) : 'https://ganjoor.net/search?s="' + encodeURI(text) + '"',
        title: 'جستجوی عبارت در گنجور',
        target: '_blank',
    });
    $quran.attr({
        href: 'https://tanzil.ir/#search/quran/' + encodeURI(text),
        title: 'جستجوی عبارت در قرآن',
        target: '_blank',
    });
    $vazn.attr({
        href: 'http://sorud.info/?Text=' + encodeURI(text),
        title: 'تعیین وزن عبارت',
        target: '_blank',
    });
    $google.attr({
        href: 'https://www.google.com/search?q=' + encodeURI(text),
        title: 'جستجو در گپگل',
        target: '_blank',
    });

    // Position tooltip near the selection
    var rect = sel.getRangeAt(0).getBoundingClientRect();
    var tooltipWidth = $tooltip.outerWidth();

    $tooltip.css({
        transform: 'scale(1)',
        top: rect.bottom + window.scrollY + 10, // Place below the selection
        left: rect.left + (rect.width / 2) - (tooltipWidth / 2), // Center tooltip horizontally
    });

    prevtext = text;
}

// Attach selection change listener
document.addEventListener('selectionchange', vaabSelectionChanged);
