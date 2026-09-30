// Renders the interactive family-tree chart on /FamilyTree/{id}. Pure vanilla JS + SVG - no external
// charting library - since the shape of the problem (a handful to a few hundred nodes, drawn once,
// panned/zoomed by the user) doesn't need one, and the project prefers self-hosted, dependency-light
// front-end code.
//
// Input (window.familyTreeData, set inline by FamilyTree.cshtml): the JSON of
// RMuseum.Models.Ganjoor.ViewModels.GanjoorFamilyTreeViewModel, camelCased -
//   { rootId, persons: [{id, name, birthYearInLHijri, deathYearInLHijri, validBirthDate,
//     validDeathDate, description, wikiUrl, familyTreeCaption, ...}], relations: [{person1Id,
//     person2Id, relationType, degreeHint}] } where relationType is PersonRelationType's numeric
//     value: 0=Parent, 1=Sibling, 2=Spouse, 3=Ancestor (Person1 is the parent/ancestor side for
//     0 and 3; order doesn't matter for 1 and 2).
//
// The graph is a general kinship graph, not a strict tree (a person can have two recorded parents,
// multiple spouses, etc.), so this builds a reasonable tree-like DRAWING out of it with a few
// simplifying choices - see comments below - rather than claiming to be an exact rendering of every
// edge. That mirrors what similar Shahnameh genealogy charts do in practice.

(function () {
    'use strict';

    function dedup(arr) {
        return arr ? Array.from(new Set(arr)) : [];
    }

    function buildLayout(data, requestedRootId) {
        var persons = {};
        data.persons.forEach(function (p) { persons[p.id] = p; });

        var parentsOf = {};   // childId -> [parentId,...]
        var childrenOf = {};  // parentId -> [childId,...]
        var spouseOf = {};    // id -> [spouseId,...]

        function addParentChild(parentId, childId) {
            (parentsOf[childId] = parentsOf[childId] || []).push(parentId);
            (childrenOf[parentId] = childrenOf[parentId] || []).push(childId);
        }
        function addSpouse(a, b) {
            (spouseOf[a] = spouseOf[a] || []).push(b);
            (spouseOf[b] = spouseOf[b] || []).push(a);
        }

        (data.relations || []).forEach(function (r) {
            if (r.relationType === 0 || r.relationType === 3) {
                addParentChild(r.person1Id, r.person2Id);
            } else if (r.relationType === 2) {
                addSpouse(r.person1Id, r.person2Id);
            }
            // relationType === 1 (Sibling) isn't needed for layout: siblings normally end up
            // adjacent anyway once they share a primaryParentOf below.
        });

        Object.keys(childrenOf).forEach(function (k) { childrenOf[k] = dedup(childrenOf[k]); });
        Object.keys(parentsOf).forEach(function (k) { parentsOf[k] = dedup(parentsOf[k]); });
        Object.keys(spouseOf).forEach(function (k) { spouseOf[k] = dedup(spouseOf[k]); });

        var allIds = data.persons.map(function (p) { return p.id; });
        var isRoot = {};
        allIds.forEach(function (id) { isRoot[id] = !parentsOf[id] || parentsOf[id].length === 0; });

        // one child can have two recorded parents (mother + father) - pick a single "primary"
        // parent per child to build a drawable tree, preferring whichever parent is NOT themselves
        // a chart-root (so the root-level spouse ends up as the "attached" side, see below)
        var primaryParentOf = {};
        var secondaryParentOf = {};
        allIds.forEach(function (childId) {
            var ps = parentsOf[childId];
            if (!ps || ps.length === 0) return;
            if (ps.length === 1) { primaryParentOf[childId] = ps[0]; return; }
            var sorted = ps.slice().sort(function (a, b) {
                var ar = isRoot[a] ? 1 : 0, br = isRoot[b] ? 1 : 0;
                if (ar !== br) return ar - br;
                return a - b;
            });
            primaryParentOf[childId] = sorted[0];
            secondaryParentOf[childId] = sorted[1];
        });

        var hasOwnChildren = {};
        Object.keys(primaryParentOf).forEach(function (childId) {
            hasOwnChildren[primaryParentOf[childId]] = true;
        });

        // a childless, parentless spouse (the common case - a wife with no recorded parents/kids
        // of her own beyond this marriage) is drawn as a small box attached beside their partner
        // rather than given their own column, same visual idea as the heart-linked pairs in
        // akhakhafrasiyab.ir's chart
        var attachedTo = {};    // spouseId -> primaryId
        var attachedList = {};  // primaryId -> [spouseId,...]

        function tryAttach(primaryId, spouseId) {
            if (attachedTo[spouseId] !== undefined || spouseId === primaryId) return false;
            if (isRoot[spouseId] && !hasOwnChildren[spouseId]) {
                attachedTo[spouseId] = primaryId;
                (attachedList[primaryId] = attachedList[primaryId] || []).push(spouseId);
                return true;
            }
            return false;
        }

        Object.keys(secondaryParentOf).forEach(function (childId) {
            tryAttach(primaryParentOf[childId], secondaryParentOf[childId]);
        });
        Object.keys(spouseOf).forEach(function (a) {
            spouseOf[a].forEach(function (b) {
                if (attachedTo[a] !== undefined || attachedTo[b] !== undefined) return;
                if (tryAttach(a, b)) return;
                tryAttach(b, a);
            });
        });

        var topLevelRoots = allIds.filter(function (id) { return isRoot[id] && attachedTo[id] === undefined; });

        function subtreeContains(rootId, targetId, guard) {
            guard = guard || {};
            if (rootId === targetId) return true;
            if (guard[rootId]) return false;
            guard[rootId] = true;
            var kids = childrenOf[rootId] || [];
            for (var i = 0; i < kids.length; i++) if (subtreeContains(kids[i], targetId, guard)) return true;
            var att = attachedList[rootId] || [];
            for (var j = 0; j < att.length; j++) if (att[j] === targetId) return true;
            return false;
        }
        topLevelRoots.sort(function (a, b) {
            var aHas = subtreeContains(a, requestedRootId) ? 0 : 1;
            var bHas = subtreeContains(b, requestedRootId) ? 0 : 1;
            if (aHas !== bHas) return aHas - bHas;
            return a - b;
        });

        // classic tidy-tree-ish pass: leaves get sequential column indices, a parent's column is
        // the midpoint of its children's - good enough for the mostly-linear/lightly-branching
        // lineages this data tends to have
        var xCounter = 0;
        var nodeX = {}, nodeDepth = {};
        function layout(id, depth) {
            nodeDepth[id] = depth;
            var kids = dedup(childrenOf[id] || []).sort(function (a, b) { return a - b; });
            if (kids.length === 0) {
                nodeX[id] = xCounter++;
            } else {
                kids.forEach(function (k) { layout(k, depth + 1); });
                var xs = kids.map(function (k) { return nodeX[k]; });
                nodeX[id] = (Math.min.apply(null, xs) + Math.max.apply(null, xs)) / 2;
            }
        }
        topLevelRoots.forEach(function (r) { layout(r, 0); });

        return {
            persons: persons,
            childrenOf: childrenOf,
            primaryParentOf: primaryParentOf,
            secondaryParentOf: secondaryParentOf,
            attachedList: attachedList,
            topLevelRoots: topLevelRoots,
            nodeX: nodeX,
            nodeDepth: nodeDepth
        };
    }

    var SVG_NS = 'http://www.w3.org/2000/svg';
    function el(tag, attrs) {
        var e = document.createElementNS(SVG_NS, tag);
        if (attrs) {
            Object.keys(attrs).forEach(function (k) { e.setAttribute(k, attrs[k]); });
        }
        return e;
    }

    function personLabel(p) {
        var s = p.name || '';
        return s;
    }

    function renderFamilyTree(containerId, svgId, tooltipId, data, requestedRootId) {
        var container = document.getElementById(containerId);
        var svg = document.getElementById(svgId);
        var tooltip = document.getElementById(tooltipId);
        if (!container || !svg || !data || !data.persons || data.persons.length === 0) return;

        var layoutInfo = buildLayout(data, requestedRootId);

        var COL_WIDTH = 220;
        var ROW_HEIGHT = 130;
        var BOX_W = 140;
        var BOX_H = 56;
        var ATTACH_GAP = 14;
        var ATTACH_W = 100;
        var MARGIN = 60;

        var canvas = el('g', { id: 'ft-canvas', transform: 'translate(0,0) scale(1)' });
        while (svg.firstChild) svg.removeChild(svg.firstChild);
        svg.appendChild(canvas);

        var linesGroup = el('g', { 'class': 'ft-lines' });
        var nodesGroup = el('g', { 'class': 'ft-nodes' });
        canvas.appendChild(linesGroup);
        canvas.appendChild(nodesGroup);

        function pos(id) {
            return {
                x: MARGIN + layoutInfo.nodeX[id] * COL_WIDTH,
                y: MARGIN + layoutInfo.nodeDepth[id] * ROW_HEIGHT
            };
        }

        function line(x1, y1, x2, y2, cls) {
            var l = el('path', {
                d: 'M ' + x1 + ' ' + y1 + ' C ' + x1 + ' ' + (y1 + (y2 - y1) / 2) + ', ' + x2 + ' ' + (y1 + (y2 - y1) / 2) + ', ' + x2 + ' ' + y2,
                'class': cls || 'ft-line'
            });
            linesGroup.appendChild(l);
        }

        // parent -> child connectors
        Object.keys(layoutInfo.primaryParentOf).forEach(function (childId) {
            childId = parseInt(childId, 10);
            var parentId = layoutInfo.primaryParentOf[childId];
            if (layoutInfo.nodeX[parentId] === undefined || layoutInfo.nodeX[childId] === undefined) return;
            var pp = pos(parentId), cp = pos(childId);
            line(pp.x, pp.y + BOX_H / 2, cp.x, cp.y - BOX_H / 2, 'ft-line ft-line-parent');
        });

        var maxX = 0, maxY = 0;

        function drawBox(id, x, y, w, h, isRequested) {
            var p = layoutInfo.persons[id];
            var g = el('g', { 'class': 'ft-node', 'data-person-id': id, style: 'cursor:pointer' });
            var rect = el('rect', {
                x: x - w / 2, y: y - h / 2, width: w, height: h, rx: 8, ry: 8,
                'class': isRequested ? 'ft-box ft-box-current' : 'ft-box'
            });
            g.appendChild(rect);
            var text = el('text', {
                x: x, y: y + 5, 'text-anchor': 'middle', 'class': 'ft-box-text', direction: 'rtl'
            });
            text.textContent = personLabel(p);
            g.appendChild(text);
            g.addEventListener('click', function () { showDetails(p); });
            nodesGroup.appendChild(g);
            maxX = Math.max(maxX, x + w / 2);
            maxY = Math.max(maxY, y + h / 2);
        }

        Object.keys(layoutInfo.nodeX).forEach(function (idStr) {
            var id = parseInt(idStr, 10);
            var p = pos(id);
            drawBox(id, p.x, p.y, BOX_W, BOX_H, id === requestedRootId);

            var attached = layoutInfo.attachedList[id] || [];
            attached.forEach(function (spouseId, i) {
                var ax = p.x + BOX_W / 2 + ATTACH_GAP + ATTACH_W / 2 + i * (ATTACH_W + ATTACH_GAP);
                var ay = p.y;
                drawBox(spouseId, ax, ay, ATTACH_W, BOX_H, spouseId === requestedRootId);
                line(p.x + BOX_W / 2, p.y, ax - ATTACH_W / 2, ay, 'ft-line ft-line-spouse');
                var heartX = p.x + BOX_W / 2 + ATTACH_GAP / 2;
                var heart = el('text', { x: heartX, y: ay + 4, 'text-anchor': 'middle', 'class': 'ft-heart' });
                heart.textContent = '♥';
                nodesGroup.appendChild(heart);
                maxX = Math.max(maxX, ax + ATTACH_W / 2);
            });
        });

        svg.setAttribute('viewBox', '0 0 ' + (maxX + MARGIN) + ' ' + (maxY + MARGIN));

        function showDetails(p) {
            if (!tooltip) return;
            var html = '<button type="button" class="ft-tooltip-close" aria-label="بستن">×</button>' +
                '<h3 style="margin:4px 0 10px 0">' + escapeHtml(personLabel(p)) + '</h3>';
            if (p.birthYearInLHijri || p.deathYearInLHijri) {
                html += '<p style="margin:4px 0"><small>';
                if (p.birthYearInLHijri) html += 'زاده ' + p.birthYearInLHijri.toLocaleString('fa-IR') + (p.validBirthDate ? '' : ' (تخمینی)');
                if (p.birthYearInLHijri && p.deathYearInLHijri) html += ' — ';
                if (p.deathYearInLHijri) html += 'وفات ' + p.deathYearInLHijri.toLocaleString('fa-IR') + (p.validDeathDate ? '' : ' (تخمینی)');
                html += '</small></p>';
            }
            if (p.description) {
                html += '<p style="margin:4px 0"><small>' + escapeHtml(p.description) + '</small></p>';
            }
            html += '<p style="margin:8px 0 0 0"><a href="javascript:void(0)" onclick="PersonWindow.open(' + p.id + ')">مشاهدهٔ اطلاعات کامل</a></p>';
            tooltip.innerHTML = html;
            tooltip.style.display = 'block';
            tooltip.querySelector('.ft-tooltip-close').addEventListener('click', function () {
                tooltip.style.display = 'none';
            });
        }

        function escapeHtml(s) {
            var d = document.createElement('div');
            d.textContent = s || '';
            return d.innerHTML;
        }

        svg.removeAttribute('viewBox'); // pan/zoom via transform, not viewBox, once we have a canvas group
        var svgWidth = Math.max(container.clientWidth, maxX + MARGIN);
        var svgHeight = Math.max(container.clientHeight, maxY + MARGIN);
        svg.setAttribute('width', svgWidth);
        svg.setAttribute('height', svgHeight);

        // pan + zoom - a small, dependency-free version of the usual SVG drag/wheel recipe
        var scale = 1;
        // The tree itself is only (maxX + MARGIN) x (maxY + MARGIN) "big", but the SVG element is
        // stretched to fill the whole container (so there's room to pan around a small tree) - drawn
        // at a bare translate(0,0), that left it pinned to the top-left corner instead of centered
        // whenever the tree is smaller than the container (e.g. a two-person tree in a wide panel).
        // Starting the pan offset centered - rather than at (0,0) - fixes that without changing how
        // panning/zooming themselves work.
        var tx = Math.max(0, (svgWidth - (maxX + MARGIN)) / 2);
        var ty = Math.max(0, (svgHeight - (maxY + MARGIN)) / 2);
        var dragging = false, lastX = 0, lastY = 0;

        function applyTransform() {
            canvas.setAttribute('transform', 'translate(' + tx + ',' + ty + ') scale(' + scale + ')');
        }

        svg.addEventListener('mousedown', function (e) {
            dragging = true; lastX = e.clientX; lastY = e.clientY;
            svg.style.cursor = 'grabbing';
        });
        window.addEventListener('mouseup', function () { dragging = false; svg.style.cursor = 'grab'; });
        window.addEventListener('mousemove', function (e) {
            if (!dragging) return;
            tx += (e.clientX - lastX); ty += (e.clientY - lastY);
            lastX = e.clientX; lastY = e.clientY;
            applyTransform();
        });
        svg.addEventListener('wheel', function (e) {
            e.preventDefault();
            var factor = e.deltaY < 0 ? 1.1 : 0.9;
            scale = Math.min(3, Math.max(0.2, scale * factor));
            applyTransform();
        }, { passive: false });
        svg.style.cursor = 'grab';
        applyTransform();
    }

    window.GanjoorFamilyTree = { render: renderFamilyTree };
})();
