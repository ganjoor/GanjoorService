// "Who could have been alive at the same time as this person" - worked out only from relations and
// affiliations, never from birth/death dates. Used by the ContemporariesWindow modal
// (personwindow.js openContemporaries()).
//
// Model: every person has an unknown birth B and death D (years). Ties give difference constraints
// (x_v - x_u <= w) on those moments, which form a simple temporal network solved with Floyd-Warshall:
//   - everyone: B <= D <= B + maxLifespan
//   - parent -> child: B_parent + minParentAge <= B_child, and the parent was alive when the child
//     was born (a father may die up to a year before the birth)
//   - ancestor -> descendant: B_anc + minParentAge * generations <= B_desc (generations = degree hint, else 2)
//   - sibling, spouse, and the affiliations that imply living at the same time (see
//     GanjoorRelatedPersonService.OverlapAffiliationTypes): each was born no later than the other's death
//   - killer -> victim: the victim's death falls inside the killer's lifetime
// For a chosen person every other person is then "certain" (overlap is forced by the constraints),
// "impossible" (overlap is ruled out) or "possible". The assumptions (max lifespan, min parent age)
// are the viewer's to change - legendary figures live far longer than historical ones. Output is
// relative order under those assumptions, never real dates.

(function () {
    'use strict';

    var INF = 1e15;
    var OVERLAP_AFFILIATIONS = { 0: 1, 1: 1, 2: 1, 3: 1, 4: 1, 5: 1, 6: 1, 7: 1, 9: 1, 11: 1, 12: 1, 13: 1, 14: 1 };
    var AFF_KILLER = 14;

    function solve(graph, opts) {
        var L = (opts && opts.maxLifespan) || 100;
        var A = (opts && opts.minParentAge) || 15;
        // a child may be born after the parent's death (up to a year, i.e. a posthumous birth) - off by
        // default, so parent and child always count as having overlapped
        var posthumous = (opts && opts.allowPosthumous) ? 1 : 0;

        var persons = graph.persons;
        var n = persons.length;
        var index = {};
        persons.forEach(function (p, i) { index[p.id] = i; });
        var N = 2 * n;
        var d = new Float64Array(N * N);
        d.fill(INF);
        for (var i = 0; i < N; i++) d[i * N + i] = 0;

        function B(id) { return 2 * index[id]; }
        function D(id) { return 2 * index[id] + 1; }
        // constraint x_v - x_u <= w
        function edge(u, v, w) { if (w < d[u * N + v]) d[u * N + v] = w; }
        function overlap(a, b) {
            edge(D(b), B(a), 0); // B_a <= D_b
            edge(D(a), B(b), 0); // B_b <= D_a
        }

        persons.forEach(function (p) {
            edge(B(p.id), D(p.id), L); // D <= B + L
            edge(D(p.id), B(p.id), 0); // B <= D
        });

        (graph.kin || []).forEach(function (r) {
            if (index[r.person1Id] === undefined || index[r.person2Id] === undefined) return;
            var a = r.person1Id, b = r.person2Id;
            if (r.relationType === 0) { // a is parent of b
                edge(B(b), B(a), -A);
                // the parent was alive when the child was born (unless posthumous births are allowed)
                edge(D(a), B(b), posthumous);
            } else if (r.relationType === 3) { // a is an ancestor of b
                edge(B(b), B(a), -A * (r.degreeHint || 2));
            } else if (r.relationType === 1 || r.relationType === 2) {
                overlap(a, b);
            }
        });

        (graph.affiliations || []).forEach(function (t) {
            if (index[t.person1Id] === undefined || index[t.person2Id] === undefined) return;
            if (!OVERLAP_AFFILIATIONS[t.affiliationType]) return;
            overlap(t.person1Id, t.person2Id);
            if (t.affiliationType === AFF_KILLER) {
                edge(D(t.person2Id), B(t.person1Id), 0); // B_killer <= D_victim
                edge(D(t.person1Id), D(t.person2Id), 0); // D_victim <= D_killer
            }
        });

        // Floyd-Warshall (all-pairs tightest bounds)
        for (var k = 0; k < N; k++) {
            var kN = k * N;
            for (var u = 0; u < N; u++) {
                var duk = d[u * N + k];
                if (duk >= INF) continue;
                var uN = u * N;
                for (var v = 0; v < N; v++) {
                    var dkv = d[kN + v];
                    if (dkv >= INF) continue;
                    var nd = duk + dkv;
                    if (nd < d[uN + v]) d[uN + v] = nd;
                }
            }
        }
        var consistent = true;
        for (var z = 0; z < N; z++) if (d[z * N + z] < 0) { consistent = false; break; }

        function dist(u, v) { return d[u * N + v]; }

        function classify(f, y) {
            if (f === y) return 'self';
            var Bf = B(f), Df = D(f), By = B(y), Dy = D(y);
            if (dist(Dy, Bf) <= 0 && dist(Df, By) <= 0) return 'certain';
            var dBfDf = dist(Bf, Df), dByDy = dist(By, Dy);
            if (dist(Bf, Dy) < 0 || dist(By, Df) < 0 || (dBfDf < INF && dByDy < INF && dBfDf + dByDy < 0)) return 'impossible';
            return 'possible';
        }

        // windows in years relative to the focus person's birth
        function window(f, y) {
            var Bf = B(f);
            function lo(v) { var x = dist(v, Bf); return x >= INF ? -Infinity : -x; }
            function hi(v) { var x = dist(Bf, v); return x >= INF ? Infinity : x; }
            return { bMin: lo(B(y)), bMax: hi(B(y)), dMin: lo(D(y)), dMax: hi(D(y)) };
        }

        return {
            consistent: consistent,
            classify: classify,
            window: window,
            persons: persons,
            index: index
        };
    }

    // ---------- rendering ----------

    var KIN_LABEL = { 0: ['والد', 'فرزند'], 1: ['برادر/خواهر', 'برادر/خواهر'], 2: ['همسر', 'همسر'], 3: ['نیا', 'نواده'] };
    var AFF_LABEL = {
        0: ['وزیر', 'وزیردهنده'], 1: ['مشاور', 'مشاوره‌گیرنده'], 2: ['درباری', 'مخدوم'], 3: ['حامی', 'تحت حمایت'],
        4: ['متحد', 'متحد'], 5: ['رقیب', 'رقیب'], 6: ['خدمتکار', 'مخدوم'], 7: ['همراه', 'همراه'],
        9: ['مدح‌سرا', 'ممدوح'], 11: ['سردار', 'فرمانده'], 12: ['پهلوان', 'سرور'], 13: ['هم‌عصر', 'هم‌عصر'],
        14: ['قاتل', 'کشته‌شده به دست او']
    };
    var CLASS_LABEL = { self: 'این شخصیت', certain: 'قطعاً هم‌عصر', possible: 'احتمالاً هم‌عصر', impossible: 'هم‌عصر نبوده' };

    function fa(x) { return Math.round(x).toLocaleString('fa-IR'); }

    // direct ties of the focus person: otherId -> [labels]
    function directTies(graph, focusId) {
        var m = {};
        function add(id, label) { (m[id] = m[id] || []).push(label); }
        (graph.kin || []).forEach(function (r) {
            var lab = KIN_LABEL[r.relationType];
            if (!lab) return;
            // lab[0]: what Person1 is to Person2 (parent/ancestor); lab[1]: what Person2 is to Person1
            if (r.person1Id === focusId) add(r.person2Id, lab[1]);
            else if (r.person2Id === focusId) add(r.person1Id, lab[0]);
        });
        (graph.affiliations || []).forEach(function (t) {
            var lab = AFF_LABEL[t.affiliationType];
            if (!lab) return;
            if (t.person1Id === focusId) add(t.person2Id, lab[1]);
            else if (t.person2Id === focusId) add(t.person1Id, lab[0]);
        });
        return m;
    }

    var SVG_NS = 'http://www.w3.org/2000/svg';
    function el(tag, attrs, text) {
        var e = document.createElementNS(SVG_NS, tag);
        if (attrs) Object.keys(attrs).forEach(function (k) { e.setAttribute(k, attrs[k]); });
        if (text !== undefined) e.textContent = text;
        return e;
    }

    function render(containerId, noteId, graph, focusId, opts) {
        var container = document.getElementById(containerId);
        var note = document.getElementById(noteId);
        if (!container || !graph || !graph.persons) return;
        container.innerHTML = '';

        var n = graph.persons.length;
        if (n > 450) {
            container.innerHTML = '<p style="padding:12px">تعداد شخصیت‌های مرتبط (' + n + ') برای محاسبهٔ این نمودار بیش از حد است.</p>';
            return;
        }

        var res = solve(graph, opts);
        if (!res.consistent) {
            container.innerHTML = '<p style="padding:12px;color:#a33">با فرض‌های فعلی (حداکثر عمر و حداقل سن پدر/مادر) روابط ثبت‌شده با هم سازگار نیستند؛ حداکثر عمر را بیشتر یا حداقل سن را کمتر کنید.</p>';
            if (note) note.textContent = '';
            return;
        }

        var ties = directTies(graph, focusId);
        var rows = graph.persons.map(function (p) {
            var cls = res.classify(focusId, p.id);
            return { person: p, cls: cls, win: res.window(focusId, p.id), ties: ties[p.id] || [] };
        });
        var counts = { certain: 0, possible: 0, impossible: 0 };
        rows.forEach(function (r) { if (counts[r.cls] !== undefined) counts[r.cls]++; });
        if (note) {
            note.textContent = 'قطعاً هم‌عصر: ' + fa(counts.certain) + ' · احتمالاً هم‌عصر: ' + fa(counts.possible) +
                ' · هم‌عصر نبوده: ' + fa(counts.impossible);
        }

        var showImpossible = !!(opts && opts.showImpossible);
        var order = { self: 0, certain: 1, possible: 2, impossible: 3 };
        rows = rows.filter(function (r) { return showImpossible || r.cls !== 'impossible'; });
        rows.sort(function (a, b) {
            if (order[a.cls] !== order[b.cls]) return order[a.cls] - order[b.cls];
            var am = isFinite(a.win.bMin) ? a.win.bMin : -1e9, bm = isFinite(b.win.bMin) ? b.win.bMin : -1e9;
            return am - bm || a.person.id - b.person.id;
        });

        // x range from the finite window ends
        var lo = 0, hi = 0;
        rows.forEach(function (r) {
            [r.win.bMin, r.win.dMin, r.win.bMax, r.win.dMax].forEach(function (v) {
                if (isFinite(v)) { lo = Math.min(lo, v); hi = Math.max(hi, v); }
            });
        });
        var span = Math.max(hi - lo, 1);
        lo -= span * 0.04; hi += span * 0.04;

        var LABEL_W = 230, W = Math.max(container.clientWidth || 900, 700), ROW_H = 26, TOP = 34;
        var plotW = W - LABEL_W - 10;
        function X(v) {
            if (v === -Infinity) v = lo;
            if (v === Infinity) v = hi;
            // right-to-left page: time flows right -> left like the rest of the site's charts? keep
            // left -> right (earlier at left) so the axis numbers read naturally
            return LABEL_W + (v - lo) / (hi - lo) * plotW;
        }
        var H = TOP + rows.length * ROW_H + 10;
        var svg = el('svg', { width: W, height: H, viewBox: '0 0 ' + W + ' ' + H, style: 'direction:ltr;font-family:inherit' });

        // axis ticks
        var step = 1;
        var raw = (hi - lo) / 8;
        var pow = Math.pow(10, Math.floor(Math.log10(raw || 1)));
        [1, 2, 5, 10].some(function (m) { step = m * pow; return step >= raw; });
        for (var t = Math.ceil(lo / step) * step; t <= hi; t += step) {
            svg.appendChild(el('line', { x1: X(t), x2: X(t), y1: TOP - 6, y2: H - 6, stroke: t === 0 ? '#8b6b4a' : '#e6dcc6', 'stroke-width': t === 0 ? 1.5 : 1 }));
            svg.appendChild(el('text', { x: X(t), y: 14, 'text-anchor': 'middle', 'font-size': 11, fill: '#7a6a50' }, fa(t)));
        }
        svg.appendChild(el('text', { x: LABEL_W, y: 28, 'font-size': 10, fill: '#7a6a50' }, 'سال از تولد این شخصیت (نسبی، نه تاریخ واقعی)'));

        var COLORS = { self: '#7a4a2a', certain: '#3d6b35', possible: '#2f6fb0', impossible: '#b5b5b5' };
        rows.forEach(function (r, i) {
            var y = TOP + i * ROW_H + ROW_H / 2;
            var color = COLORS[r.cls];
            var g = el('g', { style: 'cursor:pointer' });
            if (i % 2) g.appendChild(el('rect', { x: 0, y: y - ROW_H / 2, width: W, height: ROW_H, fill: '#f7f1e4', opacity: 0.6 }));

            // possible window: faded bar from earliest birth to latest death
            var x1 = X(r.win.bMin), x2 = X(r.win.dMax);
            g.appendChild(el('rect', { x: Math.min(x1, x2), y: y - 6, width: Math.max(Math.abs(x2 - x1), 2), height: 12, rx: 6, fill: color, opacity: 0.22 }));
            // certainly alive core: from latest possible birth to earliest possible death
            if (isFinite(r.win.bMax) && isFinite(r.win.dMin) && r.win.bMax < r.win.dMin) {
                g.appendChild(el('rect', { x: X(r.win.bMax), y: y - 6, width: Math.max(X(r.win.dMin) - X(r.win.bMax), 2), height: 12, rx: 6, fill: color, opacity: 0.9 }));
            }
            if (!isFinite(r.win.bMin)) g.appendChild(el('text', { x: LABEL_W + 2, y: y + 4, 'font-size': 11, fill: color }, '◀'));
            if (!isFinite(r.win.dMax)) g.appendChild(el('text', { x: W - 14, y: y + 4, 'font-size': 11, fill: color }, '▶'));

            var label = r.person.name + (r.ties.length ? ' — ' + r.ties.join('، ') : '');
            var tx = el('text', { x: LABEL_W - 8, y: y + 4, 'text-anchor': 'end', 'font-size': 12.5, fill: r.cls === 'impossible' ? '#999' : '#4a3520', direction: 'rtl', 'font-weight': r.cls === 'self' ? 700 : 400 });
            tx.textContent = label.length > 34 ? label.substring(0, 33) + '…' : label;
            g.appendChild(tx);
            var title = el('title', null, r.person.name + ' — ' + CLASS_LABEL[r.cls] +
                (r.ties.length ? ' (' + r.ties.join('، ') + ')' : ''));
            g.appendChild(title);
            (function (id) { g.addEventListener('click', function () { if (window.PersonWindow) PersonWindow.open(id); }); })(r.person.id);
            svg.appendChild(g);
        });
        container.appendChild(svg);
    }

    var api = { solve: solve, render: render, directTies: directTies };
    if (typeof window !== 'undefined') window.GanjoorContemporaries = api;
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
})();
