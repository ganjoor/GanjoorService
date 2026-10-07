// Word-level diff for moderation tables. Any <tr data-diff> whose first cell has class
// "diff-old-cell" and second "diff-new-cell" gets its two texts re-rendered with removed words
// struck through (old cell) and added words highlighted (new cell). Plain textContent in, escaped
// HTML out - nothing from the suggestion is ever interpreted as markup.
(function () {
    function tokenize(s) {
        return (s || '').split(/(\s+)/).filter(function (x) { return x.length > 0; });
    }

    function esc(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    // classic LCS over tokens; returns ops [{t: 'eq'|'del'|'add', v: string}]
    function diff(a, b) {
        var n = a.length, m = b.length, i, j;
        var dp = [];
        for (i = 0; i <= n; i++) { dp.push(new Array(m + 1).fill(0)); }
        for (i = n - 1; i >= 0; i--) {
            for (j = m - 1; j >= 0; j--) {
                dp[i][j] = a[i] === b[j] ? dp[i + 1][j + 1] + 1 : Math.max(dp[i + 1][j], dp[i][j + 1]);
            }
        }
        var ops = [];
        i = 0; j = 0;
        while (i < n && j < m) {
            if (a[i] === b[j]) { ops.push({ t: 'eq', v: a[i] }); i++; j++; }
            else if (dp[i + 1][j] >= dp[i][j + 1]) { ops.push({ t: 'del', v: a[i] }); i++; }
            else { ops.push({ t: 'add', v: b[j] }); j++; }
        }
        while (i < n) { ops.push({ t: 'del', v: a[i++] }); }
        while (j < m) { ops.push({ t: 'add', v: b[j++] }); }
        return ops;
    }

    function apply() {
        var rows = document.querySelectorAll('tr[data-diff]');
        Array.prototype.forEach.call(rows, function (tr) {
            var oldCell = tr.querySelector('.diff-old-cell');
            var newCell = tr.querySelector('.diff-new-cell');
            if (!oldCell || !newCell) { return; }
            var a = tokenize(oldCell.textContent), b = tokenize(newCell.textContent);
            if (a.length * b.length > 4000000) { return; } // too large to diff cheaply - leave as plain text
            var ops = diff(a, b), oldHtml = '', newHtml = '';
            ops.forEach(function (op) {
                var v = esc(op.v);
                if (op.t === 'eq') { oldHtml += v; newHtml += v; }
                else if (op.t === 'del') { oldHtml += /^\s+$/.test(op.v) ? v : '<span class="up-diff-del">' + v + '</span>'; }
                else { newHtml += /^\s+$/.test(op.v) ? v : '<span class="up-diff-add">' + v + '</span>'; }
            });
            oldCell.innerHTML = oldHtml;
            newCell.innerHTML = newHtml;
        });
    }

    if (document.readyState === 'loading') { document.addEventListener('DOMContentLoaded', apply); }
    else { apply(); }
})();
