// Renders the force-directed "ontology" explorer on /PeopleGraph. Pure vanilla JS + SVG - no
// external charting/physics library - same rationale as familytree.js: the graph is small (a few
// hundred nodes/edges at most), drawn once and then lightly interacted with, and the project prefers
// self-hosted, dependency-light front-end code over pulling in something like d3-force.
//
// Data shape (RMuseum.Models.Ganjoor.ViewModels.GanjoorPersonGraphViewModel, camelCased) -
//   { nodes: [{id, name, hasFamilyTree, directlyTagged}], edges: [{person1Id, person1Name,
//     person2Id, person2Name, category, typeValue, degreeHint, note}] } where category is
//     "Relation" (typeValue is PersonRelationType: 0=Parent,1=Sibling,2=Spouse,3=Ancestor) or
//     "Affiliation" (typeValue is PersonAffiliationType: 0=Minister,1=Advisor,2=Courtier,3=Patron,
//     4=Ally,5=Rival,6=Servant,7=Companion,8=Successor,99=Other). directlyTagged is false only on
// the category-scoped graph (GET api/ganjoor/cat/{id}/persongraph, the "characters in this work"
// tab): such a node was pulled in as a one-hop relative/affiliate of someone actually named in the
// work's verses, and is never false on the whole-site graph fed by PeopleExplorer.cshtml.
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
        'Affiliation:99': { label: 'سایر', color: '#888888', directional: false }
    };

    function kindKey(edge) { return edge.category + ':' + edge.typeValue; }
    function kindMeta(edge) { return KIND_META[kindKey(edge)] || { label: edge.category, color: '#888888', directional: false }; }

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

    function renderPeopleGraph(opts) {
        var container = document.getElementById(opts.containerId);
        var svg = document.getElementById(opts.svgId);
        var tableBody = document.getElementById(opts.tableBodyId);
        var legend = document.getElementById(opts.legendId);
        var searchInput = opts.searchInputId ? document.getElementById(opts.searchInputId) : null;
        var resetBtn = opts.resetButtonId ? document.getElementById(opts.resetButtonId) : null;
        var data = opts.data;

        if (!container || !svg || !data || !data.nodes || data.nodes.length === 0) return;

        var W = Math.max(container.clientWidth, 600);
        var H = Math.max(container.clientHeight, 500);

        // --- build simulation state -----------------------------------------------------------
        var nodesById = {};
        var nodes = data.nodes.map(function (n) {
            var node = {
                id: n.id, name: n.name, hasFamilyTree: n.hasFamilyTree,
                // absent on the whole-site graph payload (always directly tagged there) - default true
                directlyTagged: n.directlyTagged !== false,
                x: W / 2 + (Math.random() - 0.5) * W * 0.6,
                y: H / 2 + (Math.random() - 0.5) * H * 0.6,
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
        var CENTER_K = 0.01;
        var DAMPING = 0.82;
        var alpha = 1, alphaDecay = 0.985, alphaMin = 0.005;

        function tick() {
            if (alpha < alphaMin) return false;

            // mutual repulsion - O(n^2), fine for a few hundred nodes
            for (var i = 0; i < nodes.length; i++) {
                for (var j = i + 1; j < nodes.length; j++) {
                    var a = nodes[i], b = nodes[j];
                    var dx = a.x - b.x, dy = a.y - b.y;
                    var distSq = dx * dx + dy * dy || 0.01;
                    var dist = Math.sqrt(distSq);
                    var force = (REPULSION / distSq) * alpha;
                    var fx = (dx / dist) * force, fy = (dy / dist) * force;
                    a.vx += fx; a.vy += fy;
                    b.vx -= fx; b.vy -= fy;
                }
            }

            // spring attraction along edges
            edges.forEach(function (e) {
                var dx = e.target.x - e.source.x, dy = e.target.y - e.source.y;
                var dist = Math.sqrt(dx * dx + dy * dy) || 0.01;
                var force = SPRING_K * (dist - SPRING_LENGTH) * alpha;
                var fx = (dx / dist) * force, fy = (dy / dist) * force;
                e.source.vx += fx; e.source.vy += fy;
                e.target.vx -= fx; e.target.vy -= fy;
            });

            // light centering so the whole graph doesn't drift off-canvas
            nodes.forEach(function (n) {
                n.vx += (W / 2 - n.x) * CENTER_K * alpha;
                n.vy += (H / 2 - n.y) * CENTER_K * alpha;
            });

            nodes.forEach(function (n) {
                if (n.fx != null) { n.x = n.fx; n.y = n.fy; n.vx = 0; n.vy = 0; return; }
                n.vx *= DAMPING; n.vy *= DAMPING;
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

        function nodeRadius(n) {
            return (n.hasFamilyTree ? 10 : 7) + Math.min(6, n.degree * 0.6);
        }

        var nodeEls = nodes.map(function (n) {
            var g = el('g', { 'class': 'pg-node', 'data-person-id': n.id, style: 'cursor:pointer' });
            var circleClass = n.hasFamilyTree ? 'pg-circle pg-circle-tree' : 'pg-circle';
            if (!n.directlyTagged) {
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
                'class': n.directlyTagged ? 'pg-label' : 'pg-label pg-label-secondary', direction: 'rtl',
                style: 'cursor:pointer'
            });
            text.textContent = n.name;
            // the label opens the person's profile window; the circle (handled below, on the
            // whole <g>) keeps its own click behaviour of focusing this node's ego-network, so the
            // label needs its own listener with stopPropagation to not also trigger that
            text.addEventListener('click', function (evt) {
                evt.stopPropagation();
                if (window.PersonWindow) window.PersonWindow.open(n.id);
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
        function buildLegend() {
            if (!legend) return;
            var usedKeys = {};
            edges.forEach(function (e) { usedKeys[kindKey(e.raw)] = e.meta; });
            legend.innerHTML = '';
            Object.keys(usedKeys).forEach(function (k) {
                var meta = usedKeys[k];
                var item = document.createElement('span');
                item.className = 'pg-legend-item';
                item.innerHTML = '<span class="pg-legend-swatch" style="background:' + meta.color + '"></span>' + escapeHtml(meta.label);
                legend.appendChild(item);
            });
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
                        if (window.PersonWindow) window.PersonWindow.open(Number(link.getAttribute('data-person-id')));
                    });
                });
                tableBody.appendChild(tr);
                tableRows.push(tr);
            });
        }

        // --- focus / ego-network highlighting -----------------------------------------------------
        var focusedIds = null; // null = nothing focused (everything full opacity)

        function neighborsOf(personId) {
            var set = { };
            set[personId] = true;
            edges.forEach(function (e) {
                if (e.source.id === personId) set[e.target.id] = true;
                if (e.target.id === personId) set[e.source.id] = true;
            });
            return set;
        }

        function focusOnPersons(personIds) {
            var set = {};
            personIds.forEach(function (id) {
                var nb = neighborsOf(id);
                Object.keys(nb).forEach(function (k) { set[k] = true; });
            });
            focusedIds = set;
            applyFocus();
        }

        function clearFocus() {
            focusedIds = null;
            applyFocus();
        }

        function applyFocus() {
            nodeEls.forEach(function (item) {
                var dim = focusedIds && !focusedIds[item.node.id];
                item.el.style.opacity = dim ? 0.15 : 1;
            });
            edgeEls.forEach(function (item) {
                var dim = focusedIds && !(focusedIds[item.edge.source.id] && focusedIds[item.edge.target.id]);
                item.el.style.opacity = dim ? 0.05 : 0.55;
            });
            tableRows.forEach(function (tr, idx) {
                var e = edges[idx];
                var dim = focusedIds && !(focusedIds[e.source.id] && focusedIds[e.target.id]);
                tr.style.display = dim ? 'none' : '';
            });
        }

        nodeEls.forEach(function (item) {
            item.el.addEventListener('click', function (evt) {
                evt.stopPropagation();
                if (focusedIds && focusedIds[item.node.id] && Object.keys(focusedIds).length <= (item.node.degree + 1)) {
                    // clicking an already-focused node again clears the focus (toggle)
                    clearFocus();
                } else {
                    focusOnPersons([item.node.id]);
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
                if (matchIds.length === 0) { focusedIds = {}; applyFocus(); return; }
                focusOnPersons(matchIds);
            });
        }

        // --- drag to reposition (pins the node; double-click releases it) ------------------------
        var draggingNode = null;
        var panDragging = false, lastX = 0, lastY = 0;
        var scale = 1, tx = 0, ty = 0;

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

        nodeEls.forEach(function (item) {
            item.el.addEventListener('mousedown', function (evt) {
                evt.stopPropagation();
                draggingNode = item.node;
            });
            item.el.addEventListener('dblclick', function (evt) {
                evt.stopPropagation();
                item.node.fx = null; item.node.fy = null;
                alpha = Math.max(alpha, 0.3);
                ensureRunning();
            });
        });

        svg.addEventListener('mousedown', function (evt) {
            if (draggingNode) return;
            panDragging = true; lastX = evt.clientX; lastY = evt.clientY;
            svg.style.cursor = 'grabbing';
        });
        window.addEventListener('mousemove', function (evt) {
            if (draggingNode) {
                var p = svgPoint(evt);
                draggingNode.fx = p.x; draggingNode.fy = p.y;
                draggingNode.x = p.x; draggingNode.y = p.y;
                if (alpha < 0.05) { alpha = 0.05; ensureRunning(); }
                return;
            }
            if (!panDragging) return;
            tx += (evt.clientX - lastX); ty += (evt.clientY - lastY);
            lastX = evt.clientX; lastY = evt.clientY;
            applyTransform();
        });
        window.addEventListener('mouseup', function () {
            draggingNode = null;
            panDragging = false;
            svg.style.cursor = 'grab';
        });
        svg.addEventListener('wheel', function (evt) {
            evt.preventDefault();
            var factor = evt.deltaY < 0 ? 1.1 : 0.9;
            scale = Math.min(3, Math.max(0.2, scale * factor));
            applyTransform();
        }, { passive: false });
        svg.style.cursor = 'grab';

        // --- animation loop -----------------------------------------------------------------------
        var running = false;
        function ensureRunning() {
            if (running) return;
            running = true;
            requestAnimationFrame(step);
        }
        function step() {
            var keepGoing = tick();
            applyPositions();
            if (keepGoing) {
                requestAnimationFrame(step);
            } else {
                running = false;
            }
        }

        buildLegend();
        buildTable();
        applyPositions();
        ensureRunning();
    }

    window.GanjoorPeopleGraph = { render: renderPeopleGraph };
})();
