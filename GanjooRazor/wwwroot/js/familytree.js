// Renders the interactive family-tree chart inside the FamilyTreeWindow.open(id) modal (see
// personwindow.js and FamilyTreeWindow.cshtml). Pure vanilla JS + SVG - no external charting
// library - since the shape of the problem (a handful to a few hundred nodes, drawn once,
// panned/zoomed by the user) doesn't need one, and the project prefers self-hosted, dependency-light
// front-end code.
//
// Input (passed as the render() call's data/requestedRootId arguments - see personwindow.js's
// openFamilyTree(), which reads them from the #ftw-data/#ftw-root-id elements
// FamilyTreeWindow.cshtml embeds): the JSON of
// RMuseum.Models.Ganjoor.ViewModels.GanjoorFamilyTreeViewModel, camelCased -
//   { rootId, persons: [{id, name, birthYearInLHijri, deathYearInLHijri, validBirthDate,
//     validDeathDate, description, wikiUrl, familyTreeCaption, importance, ...}], relations: [{person1Id,
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

        var siblingPairs = [];
        (data.relations || []).forEach(function (r) {
            if (r.relationType === 0 || r.relationType === 3) {
                addParentChild(r.person1Id, r.person2Id);
            } else if (r.relationType === 2) {
                addSpouse(r.person1Id, r.person2Id);
            } else if (r.relationType === 1) {
                siblingPairs.push([r.person1Id, r.person2Id]);
            }
        });

        // NOTE: a recorded Sibling relation deliberately does not say which parent(s), if any, are
        // shared (see PersonRelationType.Sibling's own doc comment: "doesn't require either parent
        // to be known/recorded") - a text might call two people brothers without saying whether
        // they share a father, a mother, or both. So siblingPairs is intentionally NOT turned into
        // addParentChild() calls here (an earlier version of this code did exactly that, inheriting
        // ALL of the known sibling's recorded parents onto the parentless one - fabricating a
        // specific shared-parentage claim the source material never made, and quietly corrupting
        // isRoot/parentsOf for that person everywhere else in this function too, e.g. the gender
        // tie-break above). Instead, a parentless sibling is drawn as a plain "sibling, parentage
        // unspecified" attachment beside whichever sibling does have a known position - see
        // tryAttachSibling below.

        Object.keys(childrenOf).forEach(function (k) { childrenOf[k] = dedup(childrenOf[k]); });
        Object.keys(parentsOf).forEach(function (k) { parentsOf[k] = dedup(parentsOf[k]); });
        Object.keys(spouseOf).forEach(function (k) { spouseOf[k] = dedup(spouseOf[k]); });

        var allIds = data.persons.map(function (p) { return p.id; });
        var isRoot = {};
        allIds.forEach(function (id) { isRoot[id] = !parentsOf[id] || parentsOf[id].length === 0; });

        // one child can have two recorded parents (mother + father) - pick a single "primary"
        // parent per child to build a drawable tree. Preference order:
        //   1) whichever parent is NOT themselves a chart-root (so the tree keeps following a
        //      documented lineage instead of dead-ending at a root with no known ancestors) -
        //      the root-level parent then ends up as the "attached" side, see below;
        //   2) if that's a tie (both parents are roots, or both have their own recorded
        //      ancestors - common in legendary/mythological genealogies where both sides are
        //      fully documented), prefer the father (PersonGender.Male = 1), matching
        //      conventional patrilineal genealogy-chart practice;
        //   3) if gender doesn't resolve it either (same gender, or neither recorded), fall back
        //      to ascending person id, just to be deterministic - this last step has no meaning
        //      of its own and is only a last resort.
        function genderRank(id) {
            var g = (persons[id] && persons[id].gender) || 0;
            return g === 1 ? 0 : (g === 2 ? 1 : 2); // Male first, then Female, then Unknown
        }
        var primaryParentOf = {};
        var secondaryParentOf = {};
        var extraParentsOf = {}; // childId -> [third, fourth, ... recorded parent] - possible when different
                                 // books name different parents for the same person
        allIds.forEach(function (childId) {
            var ps = parentsOf[childId];
            if (!ps || ps.length === 0) return;
            if (ps.length === 1) { primaryParentOf[childId] = ps[0]; return; }
            var sorted = ps.slice().sort(function (a, b) {
                var ar = isRoot[a] ? 1 : 0, br = isRoot[b] ? 1 : 0;
                if (ar !== br) return ar - br;
                var ag = genderRank(a), bg = genderRank(b);
                if (ag !== bg) return ag - bg;
                return a - b;
            });
            primaryParentOf[childId] = sorted[0];
            secondaryParentOf[childId] = sorted[1];
            if (sorted.length > 2) extraParentsOf[childId] = sorted.slice(2);
        });

        // The recorded Parent/Ancestor edges are NOT guaranteed to be acyclic: a legendary person who is
        // really two namesakes merged into one (a Kayanid and a Sasanian "Kavad", say), or one wrong
        // Ancestor edge, closes a loop (A -> ... -> A). Everything below walks parent -> child and used to
        // recurse forever, and since the chart shows the whole connected component, one such loop broke
        // the chart for every person connected to it. Each loop is cut at one edge: Ancestor edges (a
        // looser claim than Parent) are cut in preference, then the lowest child id for determinism. The
        // child of the cut edge becomes a chart root of its own, and every parent recorded for it is still
        // drawn, as a plain connector (see unattachedSecondaryParents), so no relation silently disappears.
        var ancestorOnlyEdge = {}; // 'parent:child' -> true when every recorded edge in that direction is an Ancestor one
        (data.relations || []).forEach(function (r) {
            if (r.relationType !== 0 && r.relationType !== 3) return;
            var k = r.person1Id + ':' + r.person2Id;
            if (r.relationType === 3) { if (ancestorOnlyEdge[k] === undefined) ancestorOnlyEdge[k] = true; }
            else ancestorOnlyEdge[k] = false;
        });
        var cutLinks = []; // [{parentId, childId}] - drawn later as plain connectors
        (function breakParentCycles() {
            var state = {}; // undefined = unseen, 1 = on the current walk, 2 = done
            allIds.forEach(function (startId) {
                var path = [];
                var cur = startId;
                while (cur !== undefined && state[cur] === undefined) {
                    state[cur] = 1;
                    path.push(cur);
                    cur = primaryParentOf[cur];
                }
                if (cur !== undefined && state[cur] === 1) {
                    var members = path.slice(path.indexOf(cur));
                    var best = null;
                    members.forEach(function (m) {
                        var anc = ancestorOnlyEdge[primaryParentOf[m] + ':' + m] === true ? 0 : 1;
                        if (best === null || anc < best.anc || (anc === best.anc && m < best.childId)) {
                            best = { childId: m, anc: anc };
                        }
                    });
                    var c = best.childId;
                    cutLinks.push({ parentId: primaryParentOf[c], childId: c });
                    if (secondaryParentOf[c] !== undefined) cutLinks.push({ parentId: secondaryParentOf[c], childId: c });
                    (extraParentsOf[c] || []).forEach(function (pid) { cutLinks.push({ parentId: pid, childId: c }); });
                    delete primaryParentOf[c];
                    delete secondaryParentOf[c];
                    delete extraParentsOf[c];
                    isRoot[c] = true;
                }
                path.forEach(function (p) { state[p] = 2; });
            });
        })();

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
        var attachedAsSibling = {}; // spouseId -> true, for ids attached via tryAttachSibling below -
                                     // drawBox/the attached-box renderer uses this to draw a plain line
                                     // with no heart icon, instead of implying a marriage that was
                                     // never recorded

        function tryAttach(primaryId, spouseId) {
            if (attachedTo[spouseId] !== undefined || spouseId === primaryId) return false;
            if (isRoot[spouseId] && !hasOwnChildren[spouseId]) {
                attachedTo[spouseId] = primaryId;
                (attachedList[primaryId] = attachedList[primaryId] || []).push(spouseId);
                return true;
            }
            return false;
        }

        // A secondary parent (e.g. the mother, when the father was picked as the primary/spine
        // parent above) who is themselves a root with no children of their own gets drawn as an
        // attached spouse-box right next to the primary parent. But when the secondary parent has
        // their OWN recorded ancestors (not uncommon - e.g. two fully-documented royal lines
        // marrying), tryAttach refuses to move them next to someone else's position, since they
        // already have a real, independent one of their own elsewhere in the tree. That used to
        // mean the relation was just dropped - the child only ever showed a line to the primary
        // parent. Track those cases here instead, so an extra connector can still be drawn straight
        // to wherever that parent actually ended up, once every node has a position (see
        // unattachedSecondaryParents below and its use in renderFamilyTree).
        var unattachedSecondaryParents = []; // [{parentId, childId}]
        Object.keys(secondaryParentOf).forEach(function (childId) {
            var parentId = secondaryParentOf[childId];
            var primaryId = primaryParentOf[childId];
            if (!tryAttach(primaryId, parentId)) {
                unattachedSecondaryParents.push({ parentId: parentId, childId: parseInt(childId, 10) });
            }
        });
        // a third (or further) recorded parent used to be dropped from the drawing without a trace - treat
        // them like the second one: attach beside the spine parent when they have no position of their
        // own, otherwise draw a connector to wherever they ended up
        Object.keys(extraParentsOf).forEach(function (childId) {
            extraParentsOf[childId].forEach(function (parentId) {
                if (!tryAttach(primaryParentOf[childId], parentId)) {
                    unattachedSecondaryParents.push({ parentId: parentId, childId: parseInt(childId, 10) });
                }
            });
        });
        cutLinks.forEach(function (l) { unattachedSecondaryParents.push(l); });
        Object.keys(spouseOf).forEach(function (a) {
            spouseOf[a].forEach(function (b) {
                if (attachedTo[a] !== undefined || attachedTo[b] !== undefined) return;
                if (tryAttach(a, b)) return;
                tryAttach(b, a);
            });
        });

        // Siblings with no (or no other) known position are drawn beside whichever sibling does have
        // one - purely a placement choice, not a parentage claim (see the long comment above
        // siblingPairs). Deliberately reuses tryAttach's own "isRoot && no children of their own"
        // guard, so this never steals a sibling who already has a real, independently-documented
        // position (their own recorded parents, or children) in the tree.
        function tryAttachSibling(primaryId, siblingId) {
            if (tryAttach(primaryId, siblingId)) {
                attachedAsSibling[siblingId] = true;
                return true;
            }
            return false;
        }
        siblingPairs.forEach(function (pair) {
            var a = pair[0], b = pair[1];
            if (attachedTo[a] !== undefined || attachedTo[b] !== undefined) return;
            if (tryAttachSibling(a, b)) return;
            tryAttachSibling(b, a);
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
        // Only a child's PRIMARY parent lays it out. The old version walked every recorded child of every
        // parent, so a person with two parents was laid out once per parent, and a person reachable by
        // many paths (common once Ancestor edges join the lineages) many times over; on a cycle it never
        // returned. Secondary parents are drawn as connectors afterwards instead.
        var laidOut = {};
        function layout(id, depth) {
            laidOut[id] = true;
            nodeDepth[id] = depth;
            var kids = dedup(childrenOf[id] || []).filter(function (k) {
                return primaryParentOf[k] === id && !laidOut[k];
            }).sort(function (a, b) { return a - b; });
            if (kids.length === 0) {
                nodeX[id] = xCounter++;
            } else {
                kids.forEach(function (k) { layout(k, depth + 1); });
                var xs = kids.map(function (k) { return nodeX[k]; });
                nodeX[id] = (Math.min.apply(null, xs) + Math.max.apply(null, xs)) / 2;
            }
        }
        topLevelRoots.forEach(function (r) { if (!laidOut[r]) layout(r, 0); });
        // safety net: whoever is still unplaced (a shape this function did not foresee) becomes a root of
        // its own instead of silently vanishing from the chart or breaking it
        allIds.forEach(function (id) {
            if (!laidOut[id] && attachedTo[id] === undefined) { topLevelRoots.push(id); layout(id, 0); }
        });

        return {
            persons: persons,
            childrenOf: childrenOf,
            primaryParentOf: primaryParentOf,
            secondaryParentOf: secondaryParentOf,
            attachedList: attachedList,
            attachedAsSibling: attachedAsSibling,
            unattachedSecondaryParents: unattachedSecondaryParents,
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

    // same PersonImportance-driven size bump idea as peoplegraph.js's nodeRadius()/
    // IMPORTANCE_RADIUS_BUMP - 0=Normal, 1=Important, 2=VeryImportant. Kept modest relative to
    // COL_WIDTH/ATTACH_GAP below so a bumped box never overlaps its column neighbors.
    var IMPORTANCE_BOX_BUMP = { 0: { w: 0, h: 0 }, 1: { w: 20, h: 4 }, 2: { w: 40, h: 10 } };
    function boxSizeFor(p, baseW, baseH) {
        var bump = IMPORTANCE_BOX_BUMP[(p && p.importance) || 0] || IMPORTANCE_BOX_BUMP[0];
        return { w: baseW + bump.w, h: baseH + bump.h };
    }

    // 0=Normal (regular weight), 1=Important (semi-bold), 2=VeryImportant (bold)
    var IMPORTANCE_FONT_WEIGHT = { 0: 400, 1: 600, 2: 700 };

    var STATE_RANK = { attested: 0, otherBook: 1, unattested: 2, contradicted: 3 };
    var STATE_CLASS = { attested: 'ft-state-attested', otherBook: 'ft-state-otherbook', unattested: 'ft-state-unattested', contradicted: 'ft-state-contradicted' };

    // Book view (edges carry a `state`): drops contradicted edges unless showHidden, then keeps only
    // the component still reachable from the requested root over the remaining edges, so a person
    // reachable only through a hidden edge disappears with it. Returns data unchanged when no
    // edge has a state (no book selected).
    function applyBookFilter(data, requestedRootId, showHidden) {
        var rels = data.relations || [];
        if (!rels.some(function (r) { return r.state; })) return data;
        var kept = rels.filter(function (r) { return showHidden || r.state !== 'contradicted'; });
        var adj = {};
        kept.forEach(function (r) {
            (adj[r.person1Id] = adj[r.person1Id] || []).push(r.person2Id);
            (adj[r.person2Id] = adj[r.person2Id] || []).push(r.person1Id);
        });
        var seen = {}; seen[requestedRootId] = true;
        var queue = [requestedRootId];
        while (queue.length) {
            var cur = queue.shift();
            (adj[cur] || []).forEach(function (n) { if (!seen[n]) { seen[n] = true; queue.push(n); } });
        }
        return {
            rootId: data.rootId, masterCatId: data.masterCatId, books: data.books,
            persons: data.persons.filter(function (p) { return seen[p.id]; }),
            relations: kept.filter(function (r) { return seen[r.person1Id] && seen[r.person2Id]; })
        };
    }

    // An Ancestor (نیا/نواده) edge is only worth drawing when the chart has no other way to get from that
    // ancestor to the descendant. When a path of two or more Parent/Ancestor edges already connects them
    // (e.g. Fereydun -> Pashang -> Afrasiyab, while Fereydun -> Afrasiyab is also recorded as an
    // Ancestor relation because a poem says so), the direct edge adds nothing: the layout treats Ancestor
    // like Parent, so it made the ancestor a second "parent" of the descendant and drew a long extra line
    // that hid the real lineage. Only Ancestor edges are dropped, never Parent/Spouse/Sibling ones, and
    // never when the two people are also linked the other way round (a data cycle) or by a second direct
    // edge (a book-dependent variant of the same pair, which the chart shows on purpose).
    function pruneRedundantAncestorEdges(relations) {
        var out = {}; // ancestorId -> [{ to, idx }] over Parent (0) and Ancestor (3) edges
        relations.forEach(function (r, i) {
            if (r.relationType === 0 || r.relationType === 3) {
                (out[r.person1Id] = out[r.person1Id] || []).push({ to: r.person2Id, idx: i });
            }
        });

        // is `to` reachable from `from`? skipIdx (optional) is an edge to ignore; needIntermediate demands
        // that the path has at least one person in between, i.e. is not a single direct edge
        function reachable(from, to, skipIdx, needIntermediate) {
            var seen = {};
            seen[from] = true;
            var stack = [from];
            while (stack.length) {
                var cur = stack.pop();
                var list = out[cur] || [];
                for (var k = 0; k < list.length; k++) {
                    var e = list[k];
                    if (e.idx === skipIdx) continue;
                    if (e.to === to) {
                        if (!needIntermediate || cur !== from) return true;
                        continue;
                    }
                    if (!seen[e.to]) { seen[e.to] = true; stack.push(e.to); }
                }
            }
            return false;
        }

        return relations.filter(function (r, i) {
            if (r.relationType !== 3) return true;
            if (reachable(r.person2Id, r.person1Id, -1, false)) return true; // cycle: leave it alone
            return !reachable(r.person1Id, r.person2Id, i, true);
        });
    }

    // best state among edges linking the two people (any relation type, either direction)
    function pairInfo(edgesByPair, a, b) {
        var list = edgesByPair[Math.min(a, b) + ':' + Math.max(a, b)];
        if (!list || !list.length) return null;
        var best = list[0];
        list.forEach(function (e) { if ((STATE_RANK[e.state] === undefined ? 9 : STATE_RANK[e.state]) < (STATE_RANK[best.state] === undefined ? 9 : STATE_RANK[best.state])) best = e; });
        return best;
    }

    var REL_NAME = { 0: 'والد/فرزند', 1: 'برادر/خواهر', 2: 'همسر', 3: 'نیا/نواده' };

    // Unfiltered view: a pair of people linked by more than one kind of relation (e.g. father/daughter
    // in one book, brother/sister in another) is a book-dependent version - returns the description for
    // its tooltip, or null
    function variantInfo(edgesByPair, a, b) {
        var list = edgesByPair[Math.min(a, b) + ':' + Math.max(a, b)];
        if (!list || list.length < 2) return null;
        var kinds = {};
        list.forEach(function (e) { kinds[e.relationType] = 1; });
        if (Object.keys(kinds).length < 2) return null;
        return 'نسبت در منابع مختلف متفاوت است: ' + list.map(function (e) {
            return (REL_NAME[e.relationType] || '؟') + (e.attestedIn && e.attestedIn.length ? ' (' + e.attestedIn.join('، ') + ')' : ' (بدون شاهد)');
        }).join(' / ');
    }

    function renderFamilyTree(containerId, svgId, tooltipId, fullData, requestedRootId, opts) {
        var data = fullData ? applyBookFilter(fullData, requestedRootId, !!(opts && opts.showHidden)) : fullData;
        if (data && data.relations) {
            data = { rootId: data.rootId, masterCatId: data.masterCatId, books: data.books, persons: data.persons, relations: pruneRedundantAncestorEdges(data.relations) };
        }
        var edgesByPair = {};
        if (data && data.relations) data.relations.forEach(function (r) {
            var k = Math.min(r.person1Id, r.person2Id) + ':' + Math.max(r.person1Id, r.person2Id);
            (edgesByPair[k] = edgesByPair[k] || []).push(r);
        });
        function stateCls(a, b) {
            var e = pairInfo(edgesByPair, a, b);
            if (e && !e.state && variantInfo(edgesByPair, a, b)) return ' ft-variant';
            return e && e.state && STATE_CLASS[e.state] ? ' ' + STATE_CLASS[e.state] : '';
        }
        function stateTitle(a, b) {
            var e = pairInfo(edgesByPair, a, b);
            if (e && !e.state) {
                var v = variantInfo(edgesByPair, a, b);
                if (v) return v;
                return e.attestedIn && e.attestedIn.length ? 'مستند در: ' + e.attestedIn.join('، ') : null;
            }
            if (!e || !e.state) return null;
            if (e.state === 'attested') return 'مستند در این کتاب' + (e.attestedIn && e.attestedIn.length ? ': ' + e.attestedIn.join('، ') : '');
            if (e.state === 'otherBook') return 'مستند در کتاب دیگر' + (e.attestedIn && e.attestedIn.length ? ': ' + e.attestedIn.join('، ') : '');
            if (e.state === 'unattested') return 'بدون شاهد';
            return 'متناقض با شواهد این کتاب';
        }
        var container = document.getElementById(containerId);
        var svg = document.getElementById(svgId);
        var tooltip = document.getElementById(tooltipId);
        if (!container || !svg || !data || !data.persons || data.persons.length === 0) return;

        var layoutInfo;
        try {
            layoutInfo = buildLayout(data, requestedRootId);
        } catch (err) {
            if (window.console && console.error) console.error('family tree layout failed', err);
            while (svg.firstChild) svg.removeChild(svg.firstChild);
            var msg = el('text', { x: 20, y: 40, direction: 'rtl' });
            msg.textContent = 'نمایش این درخت خانوادگی ممکن نشد.';
            svg.appendChild(msg);
            return;
        }

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

        function line(x1, y1, x2, y2, cls, a, b) {
            if (a !== undefined) cls = (cls || 'ft-line') + stateCls(a, b);
            var l = el('path', {
                d: 'M ' + x1 + ' ' + y1 + ' C ' + x1 + ' ' + (y1 + (y2 - y1) / 2) + ', ' + x2 + ' ' + (y1 + (y2 - y1) / 2) + ', ' + x2 + ' ' + y2,
                'class': cls || 'ft-line'
            });
            if (a !== undefined) {
                var t = stateTitle(a, b);
                if (t) { var ti = el('title'); ti.textContent = t; l.appendChild(ti); }
            }
            linesGroup.appendChild(l);
        }

        // parent -> child connectors
        Object.keys(layoutInfo.primaryParentOf).forEach(function (childId) {
            childId = parseInt(childId, 10);
            var parentId = layoutInfo.primaryParentOf[childId];
            if (layoutInfo.nodeX[parentId] === undefined || layoutInfo.nodeX[childId] === undefined) return;
            var pp = pos(parentId), cp = pos(childId);
            var parentH = boxSizeFor(layoutInfo.persons[parentId], BOX_W, BOX_H).h;
            var childH = boxSizeFor(layoutInfo.persons[childId], BOX_W, BOX_H).h;
            line(pp.x, pp.y + parentH / 2, cp.x, cp.y - childH / 2, 'ft-line ft-line-parent', parentId, childId);
        });

        var maxX = 0, maxY = 0;
        var drawnPos = {}; // id -> {x,y} for every box actually drawn (primary nodes and attached spouses)

        function drawBox(id, x, y, w, h, isRequested) {
            var p = layoutInfo.persons[id];
            var g = el('g', { 'class': 'ft-node', 'data-person-id': id, style: 'cursor:pointer' });
            var rect = el('rect', {
                x: x - w / 2, y: y - h / 2, width: w, height: h, rx: 8, ry: 8,
                'class': isRequested ? 'ft-box ft-box-current' : 'ft-box'
            });
            g.appendChild(rect);
            // same importance->weight idea as the width/height bump above - Important/VeryImportant
            // names are drawn bolder, not just in a bigger box, so they stand out at a glance
            var fontWeight = IMPORTANCE_FONT_WEIGHT[(p && p.importance) || 0] || IMPORTANCE_FONT_WEIGHT[0];
            var text = el('text', {
                x: x, y: y + 5, 'text-anchor': 'middle', 'class': 'ft-box-text', direction: 'rtl',
                style: 'font-weight:' + fontWeight
            });
            text.textContent = personLabel(p);
            g.appendChild(text);
            g.addEventListener('click', function () { showDetails(p); });
            nodesGroup.appendChild(g);
            maxX = Math.max(maxX, x + w / 2);
            maxY = Math.max(maxY, y + h / 2);
            drawnPos[id] = { x: x, y: y };
        }

        Object.keys(layoutInfo.nodeX).forEach(function (idStr) {
            var id = parseInt(idStr, 10);
            var p = pos(id);
            var size = boxSizeFor(layoutInfo.persons[id], BOX_W, BOX_H);
            drawBox(id, p.x, p.y, size.w, size.h, id === requestedRootId);

            var attached = layoutInfo.attachedList[id] || [];
            var edgeX = p.x + size.w / 2;
            attached.forEach(function (spouseId, i) {
                var isSibling = !!layoutInfo.attachedAsSibling[spouseId];
                var attSize = boxSizeFor(layoutInfo.persons[spouseId], ATTACH_W, BOX_H);
                var ax = edgeX + ATTACH_GAP + attSize.w / 2;
                var ay = p.y;
                drawBox(spouseId, ax, ay, attSize.w, attSize.h, spouseId === requestedRootId);
                // a sibling drawn this way is placed next to the OTHER sibling purely for layout -
                // it is not a marriage, so it gets a plain neutral connector and no heart icon (see
                // the long comment above siblingPairs/tryAttachSibling in buildLayout)
                line(edgeX, p.y, ax - attSize.w / 2, ay, isSibling ? 'ft-line ft-line-sibling' : 'ft-line ft-line-spouse', id, spouseId);
                if (!isSibling) {
                    var heartX = edgeX + ATTACH_GAP / 2;
                    var heart = el('text', { x: heartX, y: ay + 4, 'text-anchor': 'middle', 'class': 'ft-heart' });
                    heart.textContent = '♥';
                    nodesGroup.appendChild(heart);
                }
                maxX = Math.max(maxX, ax + attSize.w / 2);
                edgeX = ax + attSize.w / 2 + ATTACH_GAP;
            });
        });

        // extra connector for a recorded second parent who couldn't be drawn attached next to the
        // primary parent (see unattachedSecondaryParents in buildLayout) - drawn last, once every
        // box (both plain tree nodes and attached ones) has a real position in drawnPos, straight
        // from wherever that parent actually ended up to the child. Styled distinctly from the
        // primary parent-child line (solid) and from spouse/sibling attachments (both dashed) so it
        // doesn't get misread as either - it's a real, independently-documented parent, just not the
        // one the chart chose as this child's spine.
        layoutInfo.unattachedSecondaryParents.forEach(function (edge) {
            var pp = drawnPos[edge.parentId], cp = drawnPos[edge.childId];
            if (!pp || !cp) return; // shouldn't happen, but never let a bad edge break the whole chart
            var parentH = boxSizeFor(layoutInfo.persons[edge.parentId], BOX_W, BOX_H).h;
            var childH = boxSizeFor(layoutInfo.persons[edge.childId], BOX_W, BOX_H).h;
            var dy = cp.y > pp.y ? 1 : -1; // usually the child is drawn lower, but not guaranteed
            line(pp.x, pp.y + dy * parentH / 2, cp.x, cp.y - dy * childH / 2, 'ft-line ft-line-second-parent', edge.parentId, edge.childId);
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
        // The SVG element is always sized to the CONTAINER - never to the full tree - so it acts as a
        // fixed "viewport" onto the (possibly much larger) tree, which is panned/zoomed via the canvas
        // group's transform below. Previously this grew the SVG itself to (maxX+MARGIN)/(maxY+MARGIN)
        // whenever the tree was bigger than the container; combined with the page's RTL layout and the
        // container's overflow:hidden, that pinned the oversized SVG's right edge to the container's
        // right edge, which could hide the requested root's own subtree entirely if it was laid out
        // toward the left - with nothing below ever panning the view back to it.
        var svgWidth = container.clientWidth || (maxX + MARGIN);
        var svgHeight = container.clientHeight || (maxY + MARGIN);
        svg.setAttribute('width', svgWidth);
        svg.setAttribute('height', svgHeight);

        // pan + zoom - a small, dependency-free version of the usual SVG drag/wheel recipe
        var scale = 1;
        // Center the initial view on the requested root's own drawn position (falling back to an
        // attached/spouse box's position if that's how it was drawn) so the person actually asked for
        // is what's visible first, rather than whatever subtree happened to land at low x. When the
        // whole tree already fits inside the viewport, center the whole tree instead (matches the old,
        // still-correct behavior for small trees).
        var tx, ty;
        if ((maxX + MARGIN) <= svgWidth && (maxY + MARGIN) <= svgHeight) {
            tx = Math.max(0, (svgWidth - (maxX + MARGIN)) / 2);
            ty = Math.max(0, (svgHeight - (maxY + MARGIN)) / 2);
        } else {
            var focus = drawnPos[requestedRootId] || { x: (maxX + MARGIN) / 2, y: (maxY + MARGIN) / 2 };
            tx = svgWidth / 2 - focus.x;
            ty = svgHeight / 2 - focus.y;
        }
        var dragging = false, lastX = 0, lastY = 0, dragMoved = 0;
        var pinching = false, pinchStartDist = 1, pinchStartScale = 1;
        var pointers = {};
        var MIN_SCALE = 0.2, MAX_SCALE = 3;

        // Pointer Events cover mouse, touch and pen: one finger pans, two fingers pinch-zoom. The graph
        // handles every gesture itself, so the browser must not scroll/zoom the page underneath it.
        svg.style.touchAction = 'none';
        svg.style.userSelect = 'none';
        svg.style.webkitUserSelect = 'none';

        function applyTransform() {
            canvas.setAttribute('transform', 'translate(' + tx + ',' + ty + ') scale(' + scale + ')');
        }

        // zoom keeping the tree point under (clientX, clientY) where it is
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

        svg.addEventListener('pointerdown', function (e) {
            if (e.pointerType === 'mouse' && e.button !== 0) return;
            pointers[e.pointerId] = { x: e.clientX, y: e.clientY };
            if (pointerCount() === 1) {
                dragging = true; dragMoved = 0; lastX = e.clientX; lastY = e.clientY;
                svg.style.cursor = 'grabbing';
            } else if (pointerCount() === 2) {
                dragging = false;
                pinching = true;
                var p = pinchInfo();
                pinchStartDist = p.dist; pinchStartScale = scale;
                lastX = p.cx; lastY = p.cy;
                dragMoved = 100; // a pinch is never a tap on a person box
            }
        });
        window.addEventListener('pointermove', function (e) {
            if (pointers[e.pointerId]) {
                pointers[e.pointerId].x = e.clientX;
                pointers[e.pointerId].y = e.clientY;
            }
            if (pinching && pointerCount() >= 2) {
                var pi = pinchInfo();
                zoomAt(pi.cx, pi.cy, pinchStartScale * pi.dist / pinchStartDist);
                tx += pi.cx - lastX; ty += pi.cy - lastY;
                lastX = pi.cx; lastY = pi.cy;
                applyTransform();
                return;
            }
            if (!dragging || !pointers[e.pointerId]) return;
            var dx = e.clientX - lastX, dy = e.clientY - lastY;
            dragMoved += Math.abs(dx) + Math.abs(dy);
            tx += dx; ty += dy;
            lastX = e.clientX; lastY = e.clientY;
            applyTransform();
        });
        function endPointer(e) {
            delete pointers[e.pointerId];
            if (pointerCount() < 2) pinching = false;
            if (pointerCount() === 0) {
                dragging = false;
                svg.style.cursor = 'grab';
            } else if (pointerCount() === 1) {
                // one finger lifted after a pinch: keep panning with the other one
                var rest = pointers[Object.keys(pointers)[0]];
                dragging = true; lastX = rest.x; lastY = rest.y;
            }
        }
        window.addEventListener('pointerup', endPointer);
        window.addEventListener('pointercancel', endPointer);

        // a pan/pinch that happens to end on a person's box is not a tap on it: swallow that click
        // before it reaches the box (capture phase, so it runs ahead of the box's own handler)
        svg.addEventListener('click', function (e) {
            if (dragMoved > 8) { e.stopPropagation(); dragMoved = 0; }
        }, true);

        svg.addEventListener('wheel', function (e) {
            e.preventDefault();
            zoomAt(e.clientX, e.clientY, scale * (e.deltaY < 0 ? 1.1 : 0.9));
        }, { passive: false });
        svg.style.cursor = 'grab';
        applyTransform();

        // full-screen button (defined in peoplegraph.js, which every layout loads): the tree is hard to
        // use in a small box on a phone
        if (window.GanjoorChartFullscreen) {
            window.GanjoorChartFullscreen.attach(container, svg, function (w, h) {
                svgWidth = w; svgHeight = h;
                svg.setAttribute('width', w);
                svg.setAttribute('height', h);
            });
        }
    }

    window.GanjoorFamilyTree = { render: renderFamilyTree };
})();
