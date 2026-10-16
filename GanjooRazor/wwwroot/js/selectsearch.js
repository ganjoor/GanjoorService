// A searchable replacement for the long poet / category drop-downs of the search box
// (#author and #cat in #searchform and #searchhorm). The <select> itself stays in the page
// (layout, form submission and onchange handlers are untouched); only its pop-up list is replaced
// by a panel with a filter box. Short lists (fewer than MIN_OPTIONS items) keep the native pop-up.
(function () {
    'use strict';

    var MIN_OPTIONS = 8;
    var panel = null, input = null, list = null, current = null, items = [], active = -1;

    function normalize(s) {
        return (s || '')
            .replace(/[ً-ٰٟـ]/g, '')   // harakat, small hamza (ezafe), tatweel
            .replace(/[‌‏‎]/g, ' ')          // ZWNJ and direction marks
            .replace(/[يى]/g, 'ی').replace(/ك/g, 'ک').replace(/ۀ/g, 'ه').replace(/ة/g, 'ه')
            .replace(/[آأإٱ]/g, 'ا')
            .replace(/[۰-۹]/g, function (c) { return String(c.charCodeAt(0) - 0x06F0); })
            .replace(/[٠-٩]/g, function (c) { return String(c.charCodeAt(0) - 0x0660); })
            .replace(/\s+/g, ' ')
            .trim()
            .toLowerCase();
    }

    function close() {
        if (!panel) return;
        panel.style.display = 'none';
        current = null;
    }

    function build() {
        panel = document.createElement('div');
        panel.className = 'ss-panel';
        panel.dir = 'rtl';
        input = document.createElement('input');
        input.type = 'text';
        input.className = 'ss-filter';
        input.placeholder = 'جستجو در فهرست…';
        input.autocomplete = 'off';
        list = document.createElement('div');
        list.className = 'ss-list';
        panel.appendChild(input);
        panel.appendChild(list);
        document.body.appendChild(panel);

        input.addEventListener('input', function () { active = 0; render(); });
        input.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown') { move(1); e.preventDefault(); }
            else if (e.key === 'ArrowUp') { move(-1); e.preventDefault(); }
            else if (e.key === 'Enter') {
                e.preventDefault();      // never submit the search form from here
                var shown = visible();
                if (active >= 0 && shown[active]) choose(shown[active]);
                else if (shown.length === 1) choose(shown[0]);
            }
            else if (e.key === 'Escape') {
                var sel = current;
                close();
                if (sel) sel.focus();
                e.preventDefault();
            }
            else if (e.key === 'Tab') close();
        });
        list.addEventListener('mousedown', function (e) {
            e.preventDefault();
            var el = e.target.closest('.ss-item');
            if (el) choose(items[parseInt(el.getAttribute('data-i'), 10)]);
        });
        document.addEventListener('mousedown', function (e) {
            if (panel.style.display === 'none') return;
            if (panel.contains(e.target) || e.target === current) return;
            close();
        }, true);
        window.addEventListener('resize', close);
    }

    function visible() {
        return items.filter(function (it) { return !it.hidden; });
    }

    function render() {
        var tokens = normalize(input.value).split(' ').filter(Boolean);
        var html = [];
        var shownIndex = 0;
        items.forEach(function (it, i) {
            it.hidden = !tokens.every(function (t) { return it.norm.indexOf(t) >= 0; });
            if (it.hidden) return;
            var cls = 'ss-item' + (it.selected ? ' ss-selected' : '') + (shownIndex === active ? ' ss-active' : '');
            html.push('<div class="' + cls + '" data-i="' + i + '"></div>');
            shownIndex++;
        });
        list.innerHTML = html.join('') || '<div class="ss-empty">موردی پیدا نشد</div>';
        var nodes = list.querySelectorAll('.ss-item');
        var k = 0;
        items.forEach(function (it) {
            if (!it.hidden && nodes[k]) { nodes[k].textContent = it.text; k++; }
        });
    }

    function move(delta) {
        var shown = visible();
        if (shown.length === 0) return;
        active = Math.max(0, Math.min(shown.length - 1, active + delta));
        var nodes = list.querySelectorAll('.ss-item');
        for (var i = 0; i < nodes.length; i++)
            nodes[i].classList.toggle('ss-active', i === active);
        if (nodes[active] && nodes[active].scrollIntoView)
            nodes[active].scrollIntoView({ block: 'nearest' });
    }

    function choose(it) {
        var sel = current;
        close();
        if (!sel || !it) return;
        if (sel.value !== it.value) {
            sel.value = it.value;
            sel.dispatchEvent(new Event('change', { bubbles: true }));
        }
        sel.focus();
    }

    function open(sel) {
        if (sel.options.length < MIN_OPTIONS) return false;
        if (!panel) build();
        current = sel;
        items = Array.prototype.map.call(sel.options, function (o) {
            return { value: o.value, text: o.text, norm: normalize(o.text), selected: o.selected, hidden: false };
        });
        input.value = '';
        active = Math.max(0, items.findIndex(function (it) { return it.selected; }));
        render();

        var rect = sel.getBoundingClientRect();
        var width = Math.max(rect.width, 220);
        var left = rect.left + window.pageXOffset;
        if (rect.left + width > document.documentElement.clientWidth)
            left = Math.max(0, document.documentElement.clientWidth - width - 4) + window.pageXOffset;
        panel.style.width = width + 'px';
        panel.style.left = left + 'px';
        panel.style.top = (rect.bottom + window.pageYOffset + 2) + 'px';
        panel.style.display = 'block';
        var selectedNode = list.querySelector('.ss-selected');
        if (selectedNode && selectedNode.scrollIntoView) selectedNode.scrollIntoView({ block: 'center' });
        input.focus({ preventScroll: true });
        return true;
    }

    function enhance(sel) {
        if (sel.getAttribute('data-ss') === '1') return;
        sel.setAttribute('data-ss', '1');
        sel.addEventListener('mousedown', function (e) {
            if (e.button !== 0) return;
            if (sel.options.length < MIN_OPTIONS) return;
            e.preventDefault();
            if (panel && panel.style.display === 'block' && current === sel) { close(); return; }
            open(sel);
        });
        sel.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ' || e.key === 'F4' || (e.altKey && e.key === 'ArrowDown')) {
                if (sel.options.length >= MIN_OPTIONS) {
                    e.preventDefault();
                    open(sel);
                }
            }
        });
    }

    function init() {
        var selects = document.querySelectorAll('#searchform select, #searchhorm select');
        for (var i = 0; i < selects.length; i++)
            enhance(selects[i]);
    }

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', init);
    else
        init();
})();
