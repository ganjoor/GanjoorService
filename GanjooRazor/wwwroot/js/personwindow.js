// Generic inline-modal shell, loaded on every page (see _Layout.cshtml / _AdminLayout.cshtml),
// used to replace what used to be three standalone pages: Person.cshtml (a person's profile),
// People.cshtml (family-tree-roots index) and PeopleGraph.cshtml (whole-site relationship graph).
// Those pages had no independent purpose of their own - they only ever existed to show data
// reachable from somewhere else (a poem's tagged persons, a category's "شخصیت‌ها" tab, an admin
// moderation queue, the family-tree chart) - so instead of navigating away, callers open one of
// these two windows over whatever page they're already on:
//
//   PersonWindow.open(id)   - a small modal: one person's bio/relations/affiliations/poems,
//                             fetched from /PersonWindow/{id} (Person.cshtml's old content,
//                             stripped of site chrome - see PersonWindow.cshtml).
//   PeopleExplorer.open()   - a large modal: the whole-site force-directed graph (formerly
//                             /PeopleGraph) plus the family-tree-roots list (formerly /People),
//                             merged into one fragment fetched from /PeopleExplorer.
//
// Both render into the same reusable overlay/modal shell built here on first use.
(function () {
    'use strict';

    var STYLE_ID = 'up-modal-styles';
    var overlay, modal, body, closeBtn;

    function ensureStyles() {
        if (document.getElementById(STYLE_ID)) return;
        var style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent =
            '.up-modal-overlay{position:fixed;inset:0;background:rgba(30,20,10,.55);z-index:9000;' +
            'display:none;align-items:flex-start;justify-content:center;overflow-y:auto;padding:5vh 16px;}' +
            '.up-modal-overlay--open{display:flex;}' +
            '.up-modal{position:relative;background:#fffdf7;border-radius:10px;box-shadow:0 8px 30px rgba(0,0,0,.35);' +
            'padding:20px 24px;max-width:640px;width:100%;max-height:90vh;overflow-y:auto;direction:rtl;text-align:right;}' +
            '.up-modal--lg{max-width:900px;}' +
            '.up-modal-close{position:absolute;left:12px;top:10px;border:none;background:none;font-size:22px;' +
            'line-height:1;cursor:pointer;color:#7a4a2a;}' +
            '.up-modal-loading{text-align:center;padding:30px 0;}' +
            'body.up-modal-noscroll{overflow:hidden;}';
        document.head.appendChild(style);
    }

    function ensureShell() {
        if (overlay) return;
        ensureStyles();
        overlay = document.createElement('div');
        overlay.id = 'up-modal-overlay';
        overlay.className = 'up-modal-overlay';
        overlay.innerHTML =
            '<div class="up-modal" id="up-modal">' +
                '<button type="button" class="up-modal-close" id="up-modal-close" aria-label="بستن">×</button>' +
                '<div class="up-modal-body" id="up-modal-body"></div>' +
            '</div>';
        document.body.appendChild(overlay);
        modal = document.getElementById('up-modal');
        body = document.getElementById('up-modal-body');
        closeBtn = document.getElementById('up-modal-close');

        closeBtn.addEventListener('click', closeModal);
        overlay.addEventListener('click', function (evt) {
            if (evt.target === overlay) closeModal();
        });
        document.addEventListener('keydown', function (evt) {
            if (evt.key === 'Escape' && overlay.classList.contains('up-modal-overlay--open')) {
                closeModal();
            }
        });
    }

    function openModal(large) {
        ensureShell();
        modal.classList.toggle('up-modal--lg', !!large);
        body.innerHTML = '<div class="up-modal-loading"><img src="/image/loading.gif" alt="بارگذاری" /></div>';
        overlay.classList.add('up-modal-overlay--open');
        document.body.classList.add('up-modal-noscroll');
    }

    function closeModal() {
        if (!overlay) return;
        overlay.classList.remove('up-modal-overlay--open');
        document.body.classList.remove('up-modal-noscroll');
    }

    function loadInto(url, onReady) {
        $.ajax({
            type: 'GET',
            url: url,
            error: function () {
                if (body) body.innerHTML = '<p>خطا در بارگذاری اطلاعات.</p>';
            },
            success: function (data) {
                body.innerHTML = data;
                if (onReady) onReady();
            }
        });
    }

    function openPerson(id) {
        openModal(false);
        loadInto('/PersonWindow/' + String(id));
    }

    function openExplorer() {
        openModal(true);
        loadInto('/PeopleExplorer', function () {
            var dataEl = document.getElementById('pge-data');
            if (dataEl && window.GanjoorPeopleGraph) {
                var graphData = JSON.parse(dataEl.textContent);
                GanjoorPeopleGraph.render({
                    containerId: 'pge-container',
                    svgId: 'pge-svg',
                    tableBodyId: 'pge-table-body',
                    legendId: 'pge-legend',
                    searchInputId: 'pge-search',
                    resetButtonId: 'pge-reset',
                    data: graphData
                });
            }
        });
    }

    window.PersonWindow = { open: openPerson, close: closeModal };
    window.PeopleExplorer = { open: openExplorer, close: closeModal };
})();
