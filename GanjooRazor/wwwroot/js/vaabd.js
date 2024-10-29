var $meaning = $('<a>لغتنامه</a>').css({
	boxShadow: '10px 0 0 -9px rgba(255,255,255,0.2), -10px 0 0 -9px rgba(255,255,255,0.2)'
})
var $abjad = $('<a>ابجد</a>')
var $search = $('<a>🔍</a>')
var $quran = $('<a>قرآن</a>')
var $vazn = $('<a>وزن</a>')


var $tooltip = $('<div>').addClass('tooltip').css({
	transform: 'scale(0)',
	transformOrigin: 'top',
	position: 'absolute',
	height: '2.5em',
	width: '14em',
	borderRadius: '10px',
	display: 'flex',
	justifyContent: 'space-around',
	alignItems: 'center',
	background: 'rgba(14,17,17,0.9)'
}).append($meaning, $abjad, $quran, $search,  $vazn)

$(document.body).append($tooltip)

var prevtext = ''
document.addEventListener('selectionchange', function() {
	var sel = window.getSelection() || document.getSelection()
	var text = sel.toString().trim()
	// if (sel.isCollapsed || !sel.rangeCount) {
	if (!text) {
		$tooltip.css({ transform: 'scale(0)' })
		prevtext = ''
		return
	}

	text = text.replaceAll("‌", " ")
		.replaceAll("‌", " ")//replace zwnj with space
		.replaceAll("ّ", "")//tashdid
		.replaceAll("َ", "")//a
		.replaceAll("ِ", "")//e
		.replaceAll("ُ", "")//o
		.replaceAll("ً", "")//an
		.replaceAll("ٍ", "")//en
		.replaceAll("ٌ", "")//on
		.replaceAll(".", "")//dot
		.replaceAll("،", "")//virgool
		.replaceAll("!", "")
		.replaceAll("؟", "")
		.replaceAll("ٔ", "")//ye
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
		.replaceAll("ْ", "")//sokoon
		;

	
	$meaning.attr({
		href: 'https://www.vajehyab.com/?q=' + encodeURI(text),
		title: 'جستجو در واژه‌یاب',
		target: '_blank'
	})
	$abjad.attr({
		href: 'https://abjad.ganjoor.net/?q=' + encodeURI(text) + '&r=' + window.location.href,
		title: 'محاسبه ابجد معادل عبارت',
		target: '_blank'
	})
	
	$search.attr({
		href: text.indexOf(' ') == -1 ? 'https://ganjoor.net/search?s=' + encodeURI(text) : 'https://ganjoor.net/search?s="' + encodeURI(text) + '"',
		title: 'جستجوی عبارت در گنجور',
		target: '_blank'
	})

	$quran.attr({
		href: 'https://tanzil.ir/#search/quran/' + encodeURI(text),
		title: 'جستجوی عبارت در قرآن',
		target: '_blank'
	})
	
	$vazn.attr({
		href: 'http://sorud.info/?Text=' + encodeURI(text), 
		title: 'تعیین وزن عبارت',
		target: '_blank'
	})

	var rect = sel.getRangeAt(0).getBoundingClientRect()
	$tooltip.css({
		transform: 'scale(1)',
		transition: 'none'
	})
	tooltipWidth = $tooltip[0].getBoundingClientRect().width
	if (!prevtext) {
		$tooltip.css({
			transform: 'scale(0)',
			transition: 'none'
		})
	}
	$tooltip.css({
		transform: 'scale(1)',
		transition: 'transform 0.2s ease-out',
		top: rect.top + $(window).scrollTop(),
		marginTop: '2em',
		left: rect.right - tooltipWidth
	})
	prevtext = text
});

