var $vazn = $('<a>وزن</a>').attr('title', 'salam')
var $meaning = $('<a>لغتنامه</a>').css({
	boxShadow: '10px 0 0 -9px rgba(255,255,255,0.2), -10px 0 0 -9px rgba(255,255,255,0.2)'
})
var $abjad = $('<a>ابجد</a>')
var $search = $('<a>جستجو</a>')


var $tooltip = $('<div>').addClass('tooltip').css({
	transform: 'scale(0)',
	transformOrigin: 'top',
	position: 'absolute',
	height: '2.5em',
	width: '13em',
	borderRadius: '10px',
	display: 'flex',
	justifyContent: 'space-around',
	alignItems: 'center',
	background: 'rgba(14,17,17,0.9)'
}).append($meaning, $abjad, $search, $vazn)

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

	
	$meaning.attr({
		href: 'http://www.vajehyab.com/?q=' + encodeURI(text), 
		title: 'جستجو در واژه‌یاب',
		target: '_blank'
	})
	$abjad.attr({
		href: 'http://abjad.ganjoor.net/?q=' + encodeURI(text) + '&r=' + window.location.href,
		title: 'محاسبه ابجد معادل عبارت',
		target: '_blank'
	})
	
	$search.attr({
		href: text.indexOf(' ') == -1 ? 'https://ganjoor.net/index.php?s=' + encodeURI(text) : 'https://ganjoor.net/index.php?s="' + encodeURI(text) + '"',
		title: 'جستجوی عبارت در گنجور',
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
		left: rect.left + rect.width / 2 - tooltipWidth / 2 - document.getElementById('fa').getBoundingClientRect().left
	})
	prevtext = text
});

