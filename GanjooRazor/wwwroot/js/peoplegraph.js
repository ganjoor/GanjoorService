// Renders the force-directed "ontology" explorer on /PeopleGraph. Pure vanilla JS + SVG - no
// external charting/physics library - same rationale as familytree.js: the graph is small (a few
// hundred nodes/edges at most), drawn once and then lightly interacted with, and the project prefers
// self-hosted, dependency-light front-end code over pulling in something like d3-force.
//
// Data shape (RMuseum.Models.Ganjoor.ViewModels.GanjoorPersonGraphViewModel, camelCased) -
//   { nodes: [{id, name, hasFamilyTree, directlyTagged, importance}], edges: [{person1Id, person1Name,
//     person2Id, person2Name, category, typeValue, degreeHint, note}] } where category is
//     "Relation" (typeValue is PersonRelationType: 0=Parent,1=Sibling,2=Spouse,3=Ancestor) or
//     "Affiliation" (typeValue is PersonAffiliationType: 0=Minister,1=Advisor,2=Courtier,3=Patron,
//     4=Ally,5=Rival,6=Servant,7=Companion,8=Successor,9=Panegyrized,10=Satirized,11=MilitaryCommander,
//     12=Champion,13=Contemporary,14=Killer,15=Lover,99=Other). directlyTagged is false only on
// the category-scoped graph (GET api/ganjoor/cat/{id}/persongraph, the "characters in this work"
// tab): such a node was pulled in as a one-hop relative/affiliate of someone actually named in the
// work's verses, and is never false on the whole-site graph fed by PeopleExplorer.cshtml. importance
// is PersonImportance (0=Normal,1=Important,2=VeryImportant), an editorial prominence set on the
// person themselves (see Editor.cshtml's new-person widget and SuggestPersonEdit.cshtml) - used
// below, alongside hasFamilyTree and degree, to size a node's circle (see nodeRadius()).
//
// Called with an options object (see PeopleExplorer.cshtml and _PersonGraphPartial.cshtml for two
// call sites with different element-id prefixes and data payloads), so the same renderer serves
// both the whole-site "explore all characters" modal (PeopleExplorer.open() in personwindow.js)
// and any number of category-scoped "شخصیت‌ها" tabs on cat/poet pages. A clicked node label or
// table-row name opens that person's own profile via PersonWindow.open(id) (personwindow.js),
// which is why this file assumes personwindow.js is also loaded on the page.
//
// Unlike FamilyTree.cshtml (a strict tree layout for one connected component), this lays out the
// WHOLE graph with a simple force simulation (mutual repulsion + spring edges + light centering,
// cooled down like d3-force's alpha decay) since the data isn't tree-shaped - the same node can be
// someone's minister AND brother-in-law AND rival, all at once - and a force layout is the natural
// way to show that without picking one relationship to draw and hiding the rest.

(function () {
    'use strict';

    // one entry per (category, typeValue) pair actually in use - label for the legend/table, color
    // for the edge/legend swatch, directional true when Person1 -> Person2 has a specific meaning
    // (drawn with an arrowhead), symmetric ones are drawn as plain lines
    var KIND_META = {
        'Relation:0': { label: 'پدر/مادر و فرزند', color: '#7a4a2a', directional: true },
        'Relation:1': { label: 'خواهر/برادر', color: '#4a7a5a', directional: false },
        'Relation:2': { label: 'همسر', color: '#a0455c', directional: false },
        'Relation:3': { label: 'نیا و نواده', color: '#8b6b4a', directional: true },
        'Affiliation:0': { label: 'وزیر', color: '#3a6ea5', directional: true },
        'Affiliation:1': { label: 'مشاور', color: '#3a8ea5', directional: true },
        'Affiliation:2': { label: 'درباری', color: '#5a7ea5', directional: true },
        'Affiliation:3': { label: 'حامی', color: '#2a5e8a', directional: true },
        'Affiliation:4': { label: 'دوست/متحد', color: '#3a9a4a', directional: false },
        'Affiliation:5': { label: 'دشمن/رقیب', color: '#c0392b', directional: false },
        'Affiliation:6': { label: 'خدمتکار', color: '#c07a2b', directional: true },
        'Affiliation:7': { label: 'همراه', color: '#5aa08a', directional: false },
        'Affiliation:8': { label: 'جانشین', color: '#8a4ac0', directional: true },
        'Affiliation:9': { label: 'مدح‌گو', color: '#b08a2a', directional: true },
        'Affiliation:10': { label: 'هجوگو', color: '#7a3a9a', directional: true },
        'Affiliation:11': { label: 'سردار', color: '#4a6ea5', directional: true },
        'Affiliation:12': { label: 'پهلوان', color: '#a56e3a', directional: true },
        'Affiliation:13': { label: 'هم‌عصر', color: '#6a8a6a', directional: false },
        'Affiliation:14': { label: 'قاتل', color: '#8a1a1a', directional: true },
        'Affiliation:15': { label: 'عاشق (به معشوق)', color: '#d6336c', directional: true },
        'Affiliation:99': { label: 'سایر', color: '#888888', directional: false }
    };

    function kindKey(edge) { return edge.category + ':' + edge.typeValue; }
    // edge/legend colours are tuned for a light background; on a dark theme they are mixed towards white
    function isDarkTheme() {
        try { return getComputedStyle(document.documentElement).colorScheme === 'dark'; } catch (e) { return false; }
    }
    function lighten(hex) {
        var m = /^#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i.exec(hex);
        if (!m) return hex;
        var out = '#';
        for (var i = 1; i <= 3; i++) {
            var v = parseInt(m[i], 16);
            v = Math.round(v + (255 - v) * 0.45);
            out += (v < 16 ? '0' : '') + v.toString(16);
        }
        return out;
    }
    function kindMeta(edge) {
        var m = KIND_META[kindKey(edge)] || { label: edge.category, color: '#888888', directional: false };
        return isDarkTheme() ? { label: m.label, color: lighten(m.color), directional: m.directional } : m;
    }

    function escapeHtml(s) {
        var d = document.createElement('div');
        d.textContent = s || '';
        return d.innerHTML;
    }

    var SVG_NS = 'http://www.w3.org/2000/svg';
    function el(tag, attrs) {
        var e = document.createElementNS(SVG_NS, tag);
        if (attrs) {
            Object.keys(attrs).forEach(function (k) { e.setAttribute(k, attrs[k]); });
        }
        return e;
    }

    // --- full-screen toggle shared by the people graph and the family tree ---------------------------
    // GanjoorChartFullscreen.attach(container, svg, onResize) puts a round button in the corner of the
    // chart container. Entering moves the container itself to <body> and pins it over the whole screen
    // (so it also works from inside the modal windows, and on iPhones, which have no Fullscreen API for
    // ordinary elements); where the browser does support the Fullscreen API it is requested as well, so
    // the browser's own bars disappear. onResize(width, height) is called after every size change so
    // the chart can resize its SVG. Safe to call again for the same container (the chart is re-rendered
    // in place by some pages): only the callback is replaced.
    window.GanjoorChartFullscreen = (function () {
        function attach(container, svg, onResize) {
            if (!container) return;
            var st = container._gcfs;
            if (st) { st.onResize = onResize; return; }
            st = container._gcfs = { onResize: onResize, active: false, home: null, next: null, style: null, bodyOverflow: '' };

            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'gcfs-btn';
            btn.style.cssText = 'position:absolute;top:8px;left:8px;z-index:20;width:40px;height:40px;border-radius:50%;' +
                'border:1px solid var(--pw-line);background:var(--pw-paper);color:var(--pw-ink);font-size:20px;line-height:1;' +
                'cursor:pointer;padding:0;box-shadow:0 1px 4px rgba(0,0,0,0.25);touch-action:manipulation;';
            container.appendChild(btn);

            function fire() {
                setTimeout(function () {
                    if (st.onResize) st.onResize(container.clientWidth, container.clientHeight);
                }, 60);
            }
            function paint() {
                btn.textContent = st.active ? '✕' : '⛶';
                var label = st.active ? 'خروج از تمام‌صفحه' : 'تمام‌صفحه';
                btn.title = label;
                btn.setAttribute('aria-label', label);
            }
            function enter() {
                if (st.active) return;
                st.active = true;
                st.home = container.parentNode;
                st.next = container.nextSibling;
                st.style = container.getAttribute('style');
                st.bodyOverflow = document.body.style.overflow;
                document.body.appendChild(container);
                container.style.cssText = (st.style || '') + ';position:fixed;left:0;top:0;width:100vw;height:100vh;height:100dvh;' +
                    'z-index:2147483000;border:0;border-radius:0;';
                document.body.style.overflow = 'hidden';
                if (container.requestFullscreen) {
                    try { var pr = container.requestFullscreen(); if (pr && pr.catch) pr.catch(function () { }); } catch (e) { }
                }
                paint();
                fire();
            }
            function exit() {
                if (!st.active) return;
                st.active = false;
                if (document.fullscreenElement === container && document.exitFullscreen) {
                    try { var pr = document.exitFullscreen(); if (pr && pr.catch) pr.catch(function () { }); } catch (e) { }
                }
                document.body.style.overflow = st.bodyOverflow;
                if (st.style == null) container.removeAttribute('style'); else container.setAttribute('style', st.style);
                if (st.home && st.home.isConnected) {
                    st.home.insertBefore(container, st.next && st.next.parentNode === st.home ? st.next : null);
                } else if (container.parentNode) {
                    // the page/modal the chart belonged to was closed while it was full screen
                    container.parentNode.removeChild(container);
                }
                paint();
                fire();
            }
            btn.addEventListener('click', function (e) { e.stopPropagation(); if (st.active) exit(); else enter(); });
            // pointer events on the button must not start a pan of the chart underneath
            btn.addEventListener('pointerdown', function (e) { e.stopPropagation(); });
            document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && st.active && !document.fullscreenElement) exit(); });
            // leaving native full screen with the browser's own gesture (Esc, swipe) leaves our state behind
            document.addEventListener('fullscreenchange', function () { if (st.active && !document.fullscreenElement) exit(); });
            window.addEventListener('resize', function () { if (st.active) fire(); });
            window.addEventListener('orientationchange', function () { if (st.active) fire(); });
            paint();
        }
        return { attach: attach };
    })();

    function renderPeopleGraph(opts) {
        var container = document.getElementById(opts.containerId);
        var svg = document.getElementById(opts.svgId);
        var tableBody = document.getElementById(opts.tableBodyId);
        var legend = document.getElementById(opts.legendId);
        var searchInput = opts.searchInputId ? document.getElementById(opts.searchInputId) : null;
        var resetBtn = opts.resetButtonId ? document.getElementById(opts.resetButtonId) : null;
        var data = opts.data;

        if (!container || !svg || !data || !data.nodes || data.nodes.length === 0) return;

        var W = Math.max(container.clientWidth, 320);
        var H = Math.max(container.clientHeight, 500);

        // --- build simulation state -----------------------------------------------------------
        var nodesById = {};
        // deterministic start: the best-connected people go first on a sunflower spiral around the centre,
        // so the same data always settles into the same picture, centred in the view (no Math.random)
        var order = data.nodes.map(function (n, i) { return i; });
        var degreeOf = {};
        (data.edges || []).forEach(function (e) {
            degreeOf[e.person1Id] = (degreeOf[e.person1Id] || 0) + 1;
            degreeOf[e.person2Id] = (degreeOf[e.person2Id] || 0) + 1;
        });
        order.sort(function (a, b) {
            var da = degreeOf[data.nodes[a].id] || 0, db = degreeOf[data.nodes[b].id] || 0;
            return (db - da) || (a - b);
        });
        var startRank = {};
        order.forEach(function (idx, rank) { startRank[idx] = rank; });
        var nodes = data.nodes.map(function (n, idx) {
            var rank = startRank[idx];
            var node = {
                id: n.id, name: n.name, hasFamilyTree: n.hasFamilyTree, importance: n.importance || 0,
                // absent on the whole-site graph payload (always directly tagged there) - default true
                directlyTagged: n.directlyTagged !== false,
                allusionOnly: !!n.allusionOnly,
                x: W / 2 + 30 * Math.sqrt(rank + 0.5) * Math.cos(rank * 2.399963),
                y: H / 2 + 30 * Math.sqrt(rank + 0.5) * Math.sin(rank * 2.399963),
                vx: 0, vy: 0, fx: null, fy: null, degree: 0
            };
            nodesById[n.id] = node;
            return node;
        });

        var edges = (data.edges || []).filter(function (e) {
            return nodesById[e.person1Id] && nodesById[e.person2Id];
        }).map(function (e) {
            nodesById[e.person1Id].degree++;
            nodesById[e.person2Id].degree++;
            return {
                source: nodesById[e.person1Id], target: nodesById[e.person2Id],
                meta: kindMeta(e), note: e.note, degreeHint: e.degreeHint, raw: e
            };
        });

        // --- force simulation (simplified d3-force-alike, no external library) ----------------
        var REPULSION = 2600;
        var SPRING_LENGTH = 110;
        var SPRING_K = 0.02;
        var CENTER_K = 0.03;
        var DAMPING = 0.82;
        var MAX_SPEED = 25;
        var alpha = 1, alphaDecay = 0.985, alphaMin = 0.005;

        // only what is currently shown takes part in the simulation (filters / "only named" hide the rest)
        var simNodes = nodes, simEdges = edges;

        function tick() {
            if (alpha < alphaMin) return false;

            // mutual repulsion - O(n^2), fine for a few hundred nodes
            for (var i = 0; i < simNodes.length; i++) {
                for (var j = i + 1; j < simNodes.length; j++) {
                    var a = simNodes[i], b = simNodes[j];
                    var dx = a.x - b.x, dy = a.y - b.y;
                    var distSq = Math.max(dx * dx + dy * dy, 144); // floor: nodes almost on top of each other must not get a huge kick
                    var dist = Math.sqrt(distSq);
                    var force = (REPULSION / distSq) * alpha;
                    var fx = (dx / dist) * force, fy = (dy / dist) * force;
                    a.vx += fx; a.vy += fy;
                    b.vx -= fx; b.vy -= fy;
                }
            }

            // spring attraction along edges
            simEdges.forEach(function (e) {
                var dx = e.target.x - e.source.x, dy = e.target.y - e.source.y;
                var dist = Math.sqrt(dx * dx + dy * dy) || 0.01;
                var force = SPRING_K * (dist - SPRING_LENGTH) * alpha;
                var fx = (dx / dist) * force, fy = (dy / dist) * force;
                e.source.vx += fx; e.source.vy += fy;
                e.target.vx -= fx; e.target.vy -= fy;
            });

            // light centering so the whole graph doesn't drift off-canvas
            simNodes.forEach(function (n) {
                n.vx += (W / 2 - n.x) * CENTER_K * alpha;
                n.vy += (H / 2 - n.y) * CENTER_K * alpha;
            });

            simNodes.forEach(function (n) {
                if (n.fx != null) { n.x = n.fx; n.y = n.fy; n.vx = 0; n.vy = 0; return; }
                n.vx *= DAMPING; n.vy *= DAMPING;
                // speed limit: with thousands of nodes the summed repulsion otherwise catapults them away
                var sp = Math.sqrt(n.vx * n.vx + n.vy * n.vy);
                if (sp > MAX_SPEED) { n.vx *= MAX_SPEED / sp; n.vy *= MAX_SPEED / sp; }
                n.x += n.vx; n.y += n.vy;
            });

            alpha *= alphaDecay;
            return true;
        }

        // --- SVG scaffolding --------------------------------------------------------------------
        while (svg.firstChild) svg.removeChild(svg.firstChild);
        svg.setAttribute('width', W);
        svg.setAttribute('height', H);
        svg.removeAttribute('viewBox');

        var canvas = el('g', { id: 'pg-canvas', transform: 'translate(0,0) scale(1)' });
        svg.appendChild(canvas);

        // arrowhead marker per directional kind color (SVG markers can't take dynamic per-edge fill
        // easily, so define one marker per color actually in use)
        var defs = el('defs');
        canvas.appendChild(defs);
        var markerIdsByColor = {};
        function markerFor(color) {
            var safeId = 'pg-arrow-' + color.replace('#', '');
            if (markerIdsByColor[color]) return markerIdsByColor[color];
            var marker = el('marker', {
                id: safeId, viewBox: '0 0 10 10', refX: '8', refY: '5',
                markerWidth: '7', markerHeight: '7', orient: 'auto-start-reverse'
            });
            var path = el('path', { d: 'M 0 0 L 10 5 L 0 10 z', fill: color });
            marker.appendChild(path);
            defs.appendChild(marker);
            markerIdsByColor[color] = safeId;
            return safeId;
        }

        var edgesGroup = el('g', { 'class': 'pg-edges' });
        var nodesGroup = el('g', { 'class': 'pg-nodes' });
        canvas.appendChild(edgesGroup);
        canvas.appendChild(nodesGroup);

        var edgeEls = edges.map(function (e) {
            var line = el('line', {
                stroke: e.meta.color, 'stroke-width': 1.6, opacity: 0.55, 'class': 'pg-edge'
            });
            if (e.meta.directional) {
                line.setAttribute('marker-end', 'url(#' + markerFor(e.meta.color) + ')');
            }
            edgesGroup.appendChild(line);
            return { el: line, edge: e };
        });

        // importance (PersonImportance: 0=Normal,1=Important,2=VeryImportant) is the dominant term -
        // a VeryImportant person should read as clearly bigger even with few edges - hasFamilyTree
        // adds a smaller, independent bump on top (a tree root is worth noting regardless of its
        // own importance), and degree only fine-tunes within that.
        var IMPORTANCE_RADIUS_BUMP = { 0: 0, 1: 4, 2: 9 };
        function nodeRadius(n) {
            var base = (n.hasFamilyTree ? 10 : 7) + (IMPORTANCE_RADIUS_BUMP[n.importance] || 0);
            return base + Math.min(6, n.degree * 0.6);
        }

        var nodeEls = nodes.map(function (n) {
            var g = el('g', { 'class': 'pg-node', 'data-person-id': n.id, style: 'cursor:pointer' });
            var circleClass = n.hasFamilyTree ? 'pg-circle pg-circle-tree' : 'pg-circle';
            if (n.allusionOnly) {
                // named in this section only as a comparison/reminder: solid but lighter than a real participant
                circleClass += ' pg-circle-allusion';
            } else if (!n.directlyTagged) {
                // context pulled in one hop out (a relative/affiliate never actually named in the
                // work's verses) - draw dashed/dimmer so it reads as secondary, not part of the text
                circleClass += ' pg-circle-secondary';
            }
            var circle = el('circle', {
                r: nodeRadius(n),
                'class': circleClass
            });
            var text = el('text', {
                'text-anchor': 'middle', dy: -(nodeRadius(n) + 6),
                'class': n.allusionOnly ? 'pg-label pg-label-allusion' : (n.directlyTagged ? 'pg-label' : 'pg-label pg-label-secondary'), direction: 'rtl',
                style: 'cursor:pointer'
            });
            text.textContent = n.allusionOnly ? n.name + ' (اشاره)' : n.name;
            // the label opens the person's profile window; the circle (handled below, on the
            // whole <g>) keeps its own click behaviour of focusing this node's ego-network, so the
            // label needs its own listener with stopPropagation to not also trigger that
            text.addEventListener('click', function (evt) {
                evt.stopPropagation();
                if (window.PersonWindow) window.PersonWindow.open(n.id, opts.catId);
            });
            g.appendChild(circle);
            g.appendChild(text);
            nodesGroup.appendChild(g);
            return { el: g, circle: circle, text: text, node: n };
        });

        function applyPositions() {
            edgeEls.forEach(function (item) {
                item.el.setAttribute('x1', item.edge.source.x);
                item.el.setAttribute('y1', item.edge.source.y);
                item.el.setAttribute('x2', item.edge.target.x);
                item.el.setAttribute('y2', item.edge.target.y);
            });
            nodeEls.forEach(function (item) {
                item.el.setAttribute('transform', 'translate(' + item.node.x + ',' + item.node.y + ')');
            });
        }

        // --- table + legend ----------------------------------------------------------------------
        // kinds switched off by the filters ('Relation:0' -> true)
        var hiddenKinds = {};
        var usedKeys = {};
        edges.forEach(function (e) { usedKeys[kindKey(e.raw)] = e.meta; });
        var legendItems = {};

        function refreshLegendState() {
            Object.keys(legendItems).forEach(function (k) {
                var off = !!hiddenKinds[k];
                legendItems[k].style.opacity = off ? 0.4 : 1;
                legendItems[k].style.textDecoration = off ? 'line-through' : 'none';
            });
        }

        function buildLegend() {
            if (!legend) return;
            legend.innerHTML = '';
            Object.keys(usedKeys).forEach(function (k) {
                var meta = usedKeys[k];
                var item = document.createElement('span');
                item.className = 'pg-legend-item';
                item.style.cursor = 'pointer';
                item.title = 'نمایش/پنهان‌کردن این نوع ارتباط';
                item.innerHTML = '<span class="pg-legend-swatch" style="background:' + meta.color + '"></span>' + escapeHtml(meta.label);
                item.addEventListener('click', function () {
                    if (hiddenKinds[k]) delete hiddenKinds[k]; else hiddenKinds[k] = true;
                    refilter();
                });
                legend.appendChild(item);
                legendItems[k] = item;
            });

            // quick groups + re-centre, above the legend
            if (legend.parentNode) {
                var old = legend.previousSibling;
                if (old && old.className === 'pg-filterbar') old.parentNode.removeChild(old);
                var bar = document.createElement('div');
                bar.className = 'pg-filterbar';
                bar.style.cssText = 'display:flex;flex-wrap:wrap;gap:6px;margin-bottom:8px;align-items:center;';
                function mk(label, title, fn) {
                    var b = document.createElement('button');
                    b.type = 'button';
                    b.className = 'up-btn up-btn--ghost up-btn--sm';
                    b.textContent = label;
                    b.title = title;
                    b.addEventListener('click', fn);
                    bar.appendChild(b);
                }
                function only(category) {
                    hiddenKinds = {};
                    Object.keys(usedKeys).forEach(function (k) {
                        if (k.indexOf(category + ':') !== 0) hiddenKinds[k] = true;
                    });
                    refilter();
                }
                mk('همه', 'نمایش همهٔ ارتباط‌ها', function () { hiddenKinds = {}; refilter(); });
                mk('هیچکدام', 'پنهان‌کردن همهٔ ارتباط‌ها؛ سپس نوع دلخواه را از راهنما روشن کنید', function () {
                    hiddenKinds = {};
                    Object.keys(usedKeys).forEach(function (k) { hiddenKinds[k] = true; });
                    refilter();
                });
                mk('فقط خویشاوندی', 'پدر/مادر، فرزند، همسر، خواهر/برادر، نیا', function () { only('Relation'); });
                mk('فقط غیرخویشاوندی', 'وزیر، دوست، دشمن، حامی و ...', function () { only('Affiliation'); });
                mk('فاصله ＋', 'دورکردن گره‌ها از یکدیگر (برای کلیک‌کردن راحت‌تر)', function () { changeSpacing(1.3); });
                mk('فاصله －', 'نزدیک‌کردن گره‌ها به یکدیگر', function () { changeSpacing(1 / 1.3); });
                var hint = document.createElement('small');
                hint.style.color = 'var(--pw-muted)';
                hint.textContent = 'با کلیک روی هر نوع در راهنما می‌توانید آن را نمایش یا پنهان کنید';
                bar.appendChild(hint);
                legend.parentNode.insertBefore(bar, legend);
            }
            refreshLegendState();
        }

        // --- selected-person bar ------------------------------------------------------------------
        // shown above the relations table for the currently selected node, independent of the
        // table rows (a person with no relations has no rows at all, but can still be selected):
        // name + 👁 full info + 🌳 family tree (only if the person has one)
        var selectedBar = null;
        if (tableBody) {
            var tableEl = tableBody.closest ? tableBody.closest('table') : null;
            if (tableEl && tableEl.parentNode) {
                selectedBar = document.createElement('div');
                selectedBar.className = 'pg-selected-bar';
                selectedBar.style.cssText = 'display:none;margin:6px 0;padding:6px 10px;border:1px solid var(--pw-line);border-radius:6px;background:var(--pw-panel);';
                tableEl.parentNode.insertBefore(selectedBar, tableEl);
            }
        }

        function showSelected(node) {
            if (!selectedBar) return;
            if (!node) { selectedBar.style.display = 'none'; selectedBar.innerHTML = ''; return; }
            selectedBar.innerHTML =
                '<span>شخص انتخاب‌شده: </span><b>' + escapeHtml(node.name) + '</b> ' +
                '<a href="javascript:void(0)" class="pg-sel-info" title="مشاهدهٔ اطلاعات کامل این شخصیت">👁</a>' +
                (node.hasFamilyTree ? ' <a href="javascript:void(0)" class="pg-sel-tree" title="مشاهدهٔ شجره‌نامهٔ این شخصیت">🌳</a>' : '') +
                (node.degree === 0 ? ' <small>(بدون ارتباط ثبت‌شده)</small>' : '');
            selectedBar.querySelector('.pg-sel-info').addEventListener('click', function () {
                if (window.PersonWindow) window.PersonWindow.open(node.id, opts.catId);
            });
            var treeLink = selectedBar.querySelector('.pg-sel-tree');
            if (treeLink) treeLink.addEventListener('click', function () {
                if (window.FamilyTreeWindow) window.FamilyTreeWindow.open(node.id);
            });
            selectedBar.style.display = '';
        }

        var tableRows = [];
        function buildTable() {
            if (!tableBody) return;
            tableBody.innerHTML = '';
            edges.forEach(function (e, idx) {
                var tr = document.createElement('tr');
                tr.setAttribute('data-edge-index', idx);
                tr.style.cursor = 'pointer';
                tr.innerHTML =
                    '<td><a href="javascript:void(0)" class="pg-person-link" data-person-id="' + e.raw.person1Id + '">' + escapeHtml(e.raw.person1Name) + '</a></td>' +
                    '<td><span class="pg-legend-swatch" style="background:' + e.meta.color + '"></span> ' + escapeHtml(e.meta.label) + '</td>' +
                    '<td><a href="javascript:void(0)" class="pg-person-link" data-person-id="' + e.raw.person2Id + '">' + escapeHtml(e.raw.person2Name) + '</a></td>' +
                    '<td><small>' + escapeHtml(e.note || '') + '</small></td>';
                tr.addEventListener('click', function () { focusOnPersons([e.raw.person1Id, e.raw.person2Id]); });
                // the name links open the person's profile window without also triggering the
                // row's own focus-on-click behaviour above
                tr.querySelectorAll('.pg-person-link').forEach(function (link) {
                    link.addEventListener('click', function (evt) {
                        evt.stopPropagation();
                        if (window.PersonWindow) window.PersonWindow.open(Number(link.getAttribute('data-person-id')), opts.catId);
                    });
                });
                tableBody.appendChild(tr);
                tableRows.push(tr);
            });
        }

        // optional second table (category pages only): every person tagged in this category, each with
        // a link to the poems of THIS category that mention them (the person window is opened with the
        // category, so poems of other books - e.g. the Shahnameh on a Hafez page - are left out)
        var peopleRows = [];
        var peopleEmptyRow = null;
        function buildPeopleTable() {
            var body = opts.peopleTableBodyId ? document.getElementById(opts.peopleTableBodyId) : null;
            if (!body) return;
            body.innerHTML = '';
            peopleRows = [];
            peopleEmptyRow = document.createElement('tr');
            peopleEmptyRow.innerHTML = '<td colspan="3"><small>هیچ‌یک از نامبردگان انتخاب‌شده در اشعار این بخش نیامده‌اند.</small></td>';
            peopleEmptyRow.style.display = 'none';
            body.appendChild(peopleEmptyRow);
            data.nodes
                .filter(function (n) { return n.directlyTagged !== false || n.allusionOnly; })
                .sort(function (a, b) { return (a.name || '').localeCompare(b.name || '', 'fa'); })
                .forEach(function (n) {
                    var tr = document.createElement('tr');
                    tr.innerHTML =
                        '<td><a href="javascript:void(0)" class="pg-person-poems" data-person-id="' + n.id + '">' + escapeHtml(n.name) + '</a></td>' +
                        '<td><small>' + (n.allusionOnly ? 'فقط اشاره' : 'در متن') + '</small></td>' +
                        '<td><a href="javascript:void(0)" class="pg-person-poems up-btn up-btn--ghost up-btn--sm" data-person-id="' + n.id + '">اشعار این بخش</a></td>';
                    tr.querySelectorAll('.pg-person-poems').forEach(function (link) {
                        link.addEventListener('click', function () {
                            if (window.PersonWindow) window.PersonWindow.open(n.id, opts.catId);
                        });
                    });
                    body.appendChild(tr);
                    peopleRows.push({ tr: tr, node: n });
                });
        }

        // --- focus / ego-network highlighting -----------------------------------------------------
        var focusedIds = null; // null = nothing focused (everything full opacity)
        var primaryIds = null; // the searched / clicked people themselves (focusedIds also holds their neighbours)

        function neighborsOf(personId) {
            var set = { };
            set[personId] = true;
            edges.forEach(function (e) {
                if (e.hidden) return;
                if (e.source.id === personId) set[e.target.id] = true;
                if (e.target.id === personId) set[e.source.id] = true;
            });
            return set;
        }

        function focusOnPersons(personIds) {
            var set = {};
            primaryIds = {};
            personIds.forEach(function (id) {
                primaryIds[id] = true;
                var nb = neighborsOf(id);
                Object.keys(nb).forEach(function (k) { set[k] = true; });
            });
            focusedIds = set;
            applyFocus();
        }

        function clearFocus() {
            showSelected(null);
            focusedIds = null;
            primaryIds = null;
            applyFocus();
        }

        // optional checkbox (category pages): hide everyone who is not actually named in this section's
        // verses - i.e. the relatives/affiliates drawn only as one-hop context
        var onlyNamedBox = opts.onlyNamedCheckboxId ? document.getElementById(opts.onlyNamedCheckboxId) : null;
        function isHiddenNode(n) {
            return !!n.kindHidden || !!(onlyNamedBox && onlyNamedBox.checked && !n.directlyTagged && !n.allusionOnly);
        }
        if (onlyNamedBox) {
            onlyNamedBox.addEventListener('change', function () { refilter(); });
        }

        function applyFocus() {
            nodeEls.forEach(function (item) {
                var dim = focusedIds && !focusedIds[item.node.id];
                item.el.style.opacity = dim ? 0.15 : 1;
                item.el.style.display = isHiddenNode(item.node) ? 'none' : '';
            });
            edgeEls.forEach(function (item) {
                var dim = focusedIds && !(focusedIds[item.edge.source.id] && focusedIds[item.edge.target.id]);
                item.el.style.opacity = dim ? 0.05 : 0.55;
                item.el.style.display = (item.edge.hidden || isHiddenNode(item.edge.source) || isHiddenNode(item.edge.target)) ? 'none' : '';
            });
            tableRows.forEach(function (tr, idx) {
                var e = edges[idx];
                var dim = focusedIds && !(focusedIds[e.source.id] && focusedIds[e.target.id]);
                tr.style.display = (dim || e.hidden || isHiddenNode(e.source) || isHiddenNode(e.target)) ? 'none' : '';
            });
            // the people table follows the search/selection: only the searched or clicked people themselves
            // (not their neighbours) stay, so their poem links are right there
            var shownPeople = 0;
            peopleRows.forEach(function (item) {
                var visible = !isHiddenNode(item.node) && (!primaryIds || primaryIds[item.node.id]);
                item.tr.style.display = visible ? '' : 'none';
                if (visible) shownPeople++;
            });
            if (peopleEmptyRow) peopleEmptyRow.style.display = (primaryIds && shownPeople === 0) ? '' : 'none';
        }

        nodeEls.forEach(function (item) {
            item.el.addEventListener('click', function (evt) {
                evt.stopPropagation();
                // a click that ends a drag of the node is not a selection
                if (suppressClick) { suppressClick = false; return; }
                if (focusedIds && focusedIds[item.node.id] && Object.keys(focusedIds).length <= (item.node.degree + 1)) {
                    // clicking an already-focused node again clears the focus (toggle)
                    clearFocus();
                } else {
                    focusOnPersons([item.node.id]);
                    showSelected(item.node);
                }
            });
        });

        if (resetBtn) {
            resetBtn.addEventListener('click', function () { clearFocus(); if (searchInput) searchInput.value = ''; });
        }

        if (searchInput) {
            searchInput.addEventListener('input', function () {
                var q = searchInput.value.trim();
                if (!q) { clearFocus(); return; }
                var matchIds = nodes.filter(function (n) { return n.name && n.name.indexOf(q) !== -1; }).map(function (n) { return n.id; });
                if (matchIds.length === 0) { showSelected(null); focusedIds = {}; primaryIds = {}; applyFocus(); return; }
                focusOnPersons(matchIds);
                showSelected(matchIds.length === 1 ? nodesById[matchIds[0]] : null);
            });
        }

        // --- drag to reposition (pins the node; double-click / double-tap releases it) ---------------
        // Pointer Events cover mouse, touch and pen with one code path: one finger on the background
        // pans, two fingers pinch-zoom (and pan), one finger on a node drags it.
        var draggingNode = null, dragPointerId = null, dragStartX = 0, dragStartY = 0, dragStarted = false;
        var panDragging = false, lastX = 0, lastY = 0;
        var pinching = false, pinchStartDist = 1, pinchStartScale = 1;
        var scale = 1, tx = 0, ty = 0;
        var pointers = {};
        var suppressClick = false;
        var lastTap = { id: null, t: 0 };
        var MIN_SCALE = 0.04, MAX_SCALE = 3;

        // the graph handles every gesture itself, so the browser must not scroll/zoom the page under it
        svg.style.touchAction = 'none';
        svg.style.userSelect = 'none';
        svg.style.webkitUserSelect = 'none';

        function applyTransform() {
            canvas.setAttribute('transform', 'translate(' + tx + ',' + ty + ') scale(' + scale + ')');
        }

        function svgPoint(evt) {
            var rect = svg.getBoundingClientRect();
            return {
                x: (evt.clientX - rect.left - tx) / scale,
                y: (evt.clientY - rect.top - ty) / scale
            };
        }

        // zoom keeping the graph point under (clientX, clientY) where it is
        function zoomAt(clientX, clientY, newScale) {
            newScale = Math.min(MAX_SCALE, Math.max(MIN_SCALE, newScale));
            var rect = svg.getBoundingClientRect();
            var px = clientX - rect.left, py = clientY - rect.top;
            var k = newScale / scale;
            tx = px - (px - tx) * k;
            ty = py - (py - ty) * k;
            scale = newScale;
            applyTransform();
        }

        function pointerCount() { return Object.keys(pointers).length; }

        function pinchInfo() {
            var ids = Object.keys(pointers);
            var a = pointers[ids[0]], b = pointers[ids[1]];
            return { dist: Math.max(1, Math.sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y))), cx: (a.x + b.x) / 2, cy: (a.y + b.y) / 2 };
        }

        function releaseNode(node) {
            node.fx = null; node.fy = null;
            alpha = Math.max(alpha, 0.3);
            ensureRunning();
        }

        nodeEls.forEach(function (item) {
            item.el.addEventListener('pointerdown', function (evt) {
                if (evt.pointerType === 'mouse' && evt.button !== 0) return;
                // a second finger landing on a node while another gesture is under way is part of that gesture
                if (pointerCount() > 0 || draggingNode) return;
                evt.stopPropagation();
                pointers[evt.pointerId] = { x: evt.clientX, y: evt.clientY };
                draggingNode = item.node;
                dragPointerId = evt.pointerId;
                dragStartX = evt.clientX; dragStartY = evt.clientY;
                dragStarted = false;
            });
            item.el.addEventListener('dblclick', function (evt) {
                evt.stopPropagation();
                releaseNode(item.node);
            });
        });

        svg.addEventListener('pointerdown', function (evt) {
            if (evt.pointerType === 'mouse' && evt.button !== 0) return;
            pointers[evt.pointerId] = { x: evt.clientX, y: evt.clientY };
            if (draggingNode) return;
            if (pointerCount() === 1) {
                panDragging = true; lastX = evt.clientX; lastY = evt.clientY;
                svg.style.cursor = 'grabbing';
            } else if (pointerCount() === 2) {
                panDragging = false;
                pinching = true;
                var p = pinchInfo();
                pinchStartDist = p.dist; pinchStartScale = scale;
                lastX = p.cx; lastY = p.cy;
            }
        });

        window.addEventListener('pointermove', function (evt) {
            if (pointers[evt.pointerId]) {
                pointers[evt.pointerId].x = evt.clientX;
                pointers[evt.pointerId].y = evt.clientY;
            }
            if (draggingNode) {
                if (evt.pointerId !== dragPointerId) return;
                // a finger always jitters a little: only a real movement turns a tap into a drag (and pins the node)
                if (!dragStarted) {
                    var mx = evt.clientX - dragStartX, my = evt.clientY - dragStartY;
                    if (mx * mx + my * my < 36) return;
                    dragStarted = true;
                }
                var p = svgPoint(evt);
                draggingNode.fx = p.x; draggingNode.fy = p.y;
                draggingNode.x = p.x; draggingNode.y = p.y;
                if (alpha < 0.05) { alpha = 0.05; ensureRunning(); }
                return;
            }
            if (pinching && pointerCount() >= 2) {
                var pi = pinchInfo();
                zoomAt(pi.cx, pi.cy, pinchStartScale * pi.dist / pinchStartDist);
                tx += pi.cx - lastX; ty += pi.cy - lastY;
                lastX = pi.cx; lastY = pi.cy;
                applyTransform();
                return;
            }
            if (!panDragging || !pointers[evt.pointerId]) return;
            tx += (evt.clientX - lastX); ty += (evt.clientY - lastY);
            lastX = evt.clientX; lastY = evt.clientY;
            applyTransform();
        });

        function endPointer(evt) {
            delete pointers[evt.pointerId];
            if (draggingNode && evt.pointerId === dragPointerId) {
                if (dragStarted) {
                    // the click the browser sends after a drag must not select/deselect the node
                    suppressClick = true;
                    setTimeout(function () { suppressClick = false; }, 0);
                } else if (evt.type === 'pointerup') {
                    // double-tap releases a pinned node (touch screens have no dblclick on SVG nodes)
                    var now = Date.now();
                    if (lastTap.id === draggingNode.id && now - lastTap.t < 350) {
                        releaseNode(draggingNode);
                        lastTap = { id: null, t: 0 };
                    } else {
                        lastTap = { id: draggingNode.id, t: now };
                    }
                }
                draggingNode = null;
                dragPointerId = null;
            }
            if (pointerCount() < 2) pinching = false;
            if (pointerCount() === 0) {
                panDragging = false;
                svg.style.cursor = 'grab';
            } else if (pointerCount() === 1 && !draggingNode) {
                // one finger lifted after a pinch: keep panning with the other one
                var rest = pointers[Object.keys(pointers)[0]];
                panDragging = true; lastX = rest.x; lastY = rest.y;
            }
        }
        window.addEventListener('pointerup', endPointer);
        window.addEventListener('pointercancel', endPointer);

        svg.addEventListener('wheel', function (evt) {
            evt.preventDefault();
            zoomAt(evt.clientX, evt.clientY, scale * (evt.deltaY < 0 ? 1.1 : 0.9));
        }, { passive: false });
        svg.style.cursor = 'grab';

        // full-screen button: on a phone the 55vh box is too small to work in
        if (window.GanjoorChartFullscreen) {
            window.GanjoorChartFullscreen.attach(container, svg, function (w, h) {
                W = Math.max(w, 320); H = Math.max(h, 200);
                svg.setAttribute('width', W);
                svg.setAttribute('height', H);
                // let the layout re-centre itself on the new size
                alpha = Math.max(alpha, 0.2);
                autoFit = true;
                ensureRunning();
                recenter();
                fitView();
            });
        }

        // distance between nodes: scales the repulsion and the spring length of the simulation, and
        // spreads the current picture around its centre of mass right away (the view is left as it is,
        // so the nodes visibly move apart instead of the whole picture merely being zoomed out)
        var spacing = 1;
        function changeSpacing(factor) {
            var next = Math.min(4, Math.max(0.4, spacing * factor));
            factor = next / spacing;
            if (factor === 1) return;
            spacing = next;
            REPULSION = 2600 * spacing * spacing;
            SPRING_LENGTH = 110 * spacing;
            autoFit = false;
            if (simNodes.length > 0) {
                var cx = 0, cy = 0;
                simNodes.forEach(function (n) { cx += n.x; cy += n.y; });
                cx /= simNodes.length; cy /= simNodes.length;
                simNodes.forEach(function (n) {
                    n.x = cx + (n.x - cx) * factor;
                    n.y = cy + (n.y - cy) * factor;
                    if (n.fx != null) { n.fx = n.x; n.fy = n.y; }
                });
            }
            alpha = Math.max(alpha, 0.25);
            applyPositions();
            ensureRunning();
        }

        // keep the picture framed while it settles, until the user pans/zooms/drags by hand
        var autoFit = true;
        svg.addEventListener('pointerdown', function () { autoFit = false; }, true);
        svg.addEventListener('wheel', function () { autoFit = false; }, true);

        // --- deterministic layout helpers ---------------------------------------------------------
        // run the simulation synchronously so the first paint is already the settled picture
        function settle(maxTicks) {
            var cap = simNodes.length > 800 ? 60 : simNodes.length > 400 ? 120 : maxTicks;
            for (var i = 0; i < cap; i++) { if (!tick()) break; }
        }

        // move the whole picture so its centre of mass sits in the middle of the canvas
        function recenter() {
            if (simNodes.length === 0) return;
            var sx = 0, sy = 0;
            simNodes.forEach(function (n) { sx += n.x; sy += n.y; });
            var dx = W / 2 - sx / simNodes.length, dy = H / 2 - sy / simNodes.length;
            simNodes.forEach(function (n) {
                n.x += dx; n.y += dy;
                if (n.fx != null) { n.fx += dx; n.fy += dy; }
            });
            applyPositions();
        }

        // zoom/pan so every visible node is inside the frame (never zoomed in beyond 1:1)
        function fitView() {
            if (simNodes.length === 0) return;
            // frame the bulk of the picture: with many nodes the extreme 2% on each side (stray, loosely
            // connected people drifting outward) must not shrink everyone else to dots
            var xs = simNodes.map(function (n) { return n.x; }).sort(function (a, b) { return a - b; });
            var ys = simNodes.map(function (n) { return n.y; }).sort(function (a, b) { return a - b; });
            var cut = simNodes.length > 50 ? Math.floor(simNodes.length * 0.02) : 0;
            var minX = xs[cut], maxX = xs[xs.length - 1 - cut], minY = ys[cut], maxY = ys[ys.length - 1 - cut];
            if (!isFinite(minX) || !isFinite(maxX) || !isFinite(minY) || !isFinite(maxY)) return;
            var margin = 50;
            var bw = Math.max(maxX - minX, 1) + margin * 2, bh = Math.max(maxY - minY, 1) + margin * 2;
            var s = Math.min(1, W / bw, H / bh);
            scale = Math.min(MAX_SCALE, Math.max(MIN_SCALE, s));
            tx = W / 2 - ((minX + maxX) / 2) * scale;
            ty = H / 2 - ((minY + maxY) / 2) * scale;
            applyTransform();
        }

        function rebuildSim() {
            simNodes = nodes.filter(function (n) { return !isHiddenNode(n); });
            simEdges = edges.filter(function (e) { return !e.hidden && !isHiddenNode(e.source) && !isHiddenNode(e.target); });
        }

        // a kind filter / "only named" change: recompute what is visible, re-settle and re-centre
        function refilter() {
            var any = Object.keys(hiddenKinds).length > 0;
            nodes.forEach(function (n) { n.visEdges = 0; });
            edges.forEach(function (e) {
                e.hidden = !!hiddenKinds[kindKey(e.raw)];
                if (!e.hidden) { e.source.visEdges++; e.target.visEdges++; }
            });
            nodes.forEach(function (n) { n.kindHidden = any && n.visEdges === 0; });
            rebuildSim();
            refreshLegendState();
            autoFit = true;
            alpha = 0.8;
            settle(250);
            recenter();
            fitView();
            applyFocus();
            applyPositions();
            ensureRunning();
        }

        // --- animation loop -----------------------------------------------------------------------
        var running = false;
        function ensureRunning() {
            if (running) return;
            running = true;
            requestAnimationFrame(step);
        }
        function step() {
            var keepGoing = tick();
            // while the layout is still moving and the user hasn't taken over, keep it framed
            if (autoFit) { recenter(); fitView(); }
            applyPositions();
            if (keepGoing) {
                requestAnimationFrame(step);
            } else {
                running = false;
            }
        }

        buildLegend();
        buildTable();
        buildPeopleTable();
        // first paint = settled, centred and fitted picture; the same data gives the same picture
        rebuildSim();
        alpha = 1;
        settle(300);
        recenter();
        fitView();
        applyFocus();
        applyPositions();
        ensureRunning();
    }

    window.GanjoorPeopleGraph = { render: renderPeopleGraph };
})();
