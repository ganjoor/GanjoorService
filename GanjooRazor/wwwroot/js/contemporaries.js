// Presence chart in the style of printed "who was present when" charts: columns are the sections of one
// book (e.g. reigns of the Shahnameh, earliest at the right), rows are persons. A person's line starts at the
// first section where he/she is tagged and ends at the last; solid where tagged in that section, dashed in
// between. Nothing here uses birth/death years or assumptions: only where the poems actually mention people.
(function () {
    'use strict';

    var BROWN = 'var(--pw-accent)', GREEN = 'var(--pw-green)', BLUE = 'var(--pw-blue)', GREY = 'var(--pw-grey)';

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"]/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c];
        });
    }

    function render(containerId, noteId, allToggleId, data, focusId) {
        var box = document.getElementById(containerId);
        var note = document.getElementById(noteId);
        var allBox = document.getElementById(allToggleId);
        if (!box) return;
        var cols = (data && data.columns) || [];
        var people = (data && data.people) || [];
        if (!cols.length || !people.length) {
            box.style.padding = '12px';
            box.textContent = 'برای این شخصیت در شعرهای نشانه‌گذاری‌شده‌ای که ساختار بخش‌بندی دارند اطلاعاتی نیست.';
            if (note) note.textContent = '';
            return;
        }
        var focus = null, i;
        for (i = 0; i < people.length; i++) if (people[i].id === focusId) focus = people[i];
        function span(p) { return [p.columns[0], p.columns[p.columns.length - 1]]; }
        var fs = focus ? span(focus) : null;

        function draw() {
            var showAll = !!(allBox && allBox.checked);
            var rows = [];
            if (focus) rows.push(focus);
            for (var k = 0; k < people.length; k++) {
                var p = people[k];
                if (focus && p.id === focus.id) continue;
                var s = span(p);
                var overlap = fs && s[0] <= fs[1] && s[1] >= fs[0];
                if (showAll || !fs || overlap || p.related) rows.push(p);
            }
            var h = '<table class="ct-table" dir="rtl"><thead><tr><th class="ct-name">&nbsp;</th>';
            for (var c = 0; c < cols.length; c++) h += '<th title="' + esc(cols[c].title) + '"><span>' + esc(cols[c].title) + '</span></th>';
            h += '</tr></thead><tbody>';
            for (var r = 0; r < rows.length; r++) {
                var pr = rows[r], ps = span(pr), isFocus = focus && pr.id === focus.id;
                var set = {};
                pr.columns.forEach(function (x) { set[x] = 1; });
                var color = GREY;
                if (isFocus) color = BROWN;
                else if (focus) {
                    var shares = focus.columns.some(function (x) { return set[x]; });
                    color = shares ? GREEN : (pr.related ? BLUE : ps[0] <= fs[1] && ps[1] >= fs[0] ? BLUE : GREY);
                }
                h += '<tr' + (isFocus ? ' class="focus"' : '') + '><td class="ct-name" title="' + esc(pr.name) + '">' +
                    '<a href="javascript:void(0)" onclick="ContemporariesWindow.open(' + pr.id + (data.masterCatId ? ',' + data.masterCatId : '') + ')" style="color:' + color + '">' + esc(pr.name) + '</a>' +
                    '<a class="ct-tree" href="javascript:void(0)" title="شجره" onclick="FamilyTreeWindow.open(' + pr.id + ')">🌳</a></td>';
                for (var q = 0; q < cols.length; q++) {
                    var cls = 'ct-cell';
                    if (fs && q >= fs[0] && q <= fs[1]) cls += ' band';
                    if (set[q]) cls += ' on';
                    else if (q > ps[0] && q < ps[1]) cls += ' gap';
                    if (q === ps[0]) cls += ' start-right';
                    if (q === ps[1]) cls += ' end-left';
                    var line = set[q] || (q > ps[0] && q < ps[1]);
                    h += '<td class="' + cls + '" style="--c:' + color + '">' + (line ? '<i></i>' : '') + '</td>';
                }
                h += '</tr>';
            }
            h += '</tbody></table>';
            box.innerHTML = h;
            if (note) note.textContent = rows.length + ' شخصیت';
            // start at the right edge (earliest sections) - this is the natural RTL start
            box.scrollLeft = 0;
        }
        if (allBox) allBox.onchange = draw;
        draw();
    }

    window.GanjoorContemporaries = { render: render };
})();
