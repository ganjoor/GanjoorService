/*
 * verse-orthography.js
 * Orthography (dastur-e khatt) hints for edited verse lines in /User/Editor.
 *
 * WARNING ONLY: nothing here blocks saving. A rule is reported only when the edited line has
 * MORE matches of it than the original line had, so old text is never nagged about.
 *
 * Every rule is one entry of GanjoorVerseOrthography.rules:
 *   id          unique key
 *   level       'warn'    : shown with the amber highlight
 *               'suggest' : shown as a quieter suggestion, no highlight
 *   message     text shown to the editor (Persian)
 *   skipLast    true: the last word of the hemistich is ignored (rhyme words are often
 *               deliberately spelled in a special way)
 *   count(text) number of occurrences of the problem in the text
 * To disable a rule, delete its entry or comment it out. Rules never depend on each other.
 */
var GanjoorVerseOrthography = (function () {
    'use strict';

    var ZWNJ = '‌';
    // Persian/Arabic letters and combining marks (including the ezafe hamza-above ٔ)
    var LET = '\\u0621-\\u064A\\u064B-\\u065F\\u0671-\\u06D3\\u06D5';
    var WORD = LET + ZWNJ;

    function re(source, flags) {
        return new RegExp(source, flags || 'g');
    }

    function matches(pattern, text) {
        var m = text.match(pattern);
        return m ? m.length : 0;
    }

    function sum() {
        var total = 0;
        for (var i = 0; i < arguments.length; i++) {
            total += arguments[i];
        }
        return total;
    }

    // drops the last word (and trailing punctuation/space) of the hemistich
    function withoutLastWord(text) {
        var t = text.replace(/[\s‌،؛؟!.:;,?"“”«»()\[\]…–—]+$/, '');
        return t.replace(/[^\s]*$/, '');
    }

    var rules = [
        {
            id: 'punct-spacing',
            level: 'warn',
            message: 'پیش از «،» «؛» «؟» «!» فاصله نگذارید و پس از آن‌ها فاصله بگذارید.',
            count: function (t) {
                return sum(
                    matches(re('[ \\u00a0][،؛؟!]'), t),
                    matches(re('[،؛؟!](?=[' + LET + '])'), t));
            }
        },
        {
            id: 'latin-punct',
            level: 'warn',
            message: 'به‌جای «,» «;» «?» از «،» «؛» «؟» فارسی استفاده کنید.',
            count: function (t) { return matches(/[,;?]/g, t); }
        },
        {
            id: 'latin-quotes',
            level: 'warn',
            message: 'به‌جای گیومهٔ لاتین (" یا “ ”) از «گیومهٔ فارسی» استفاده کنید.',
            count: function (t) { return matches(/["“”]/g, t); }
        },
        {
            id: 'nemi-space',
            level: 'warn',
            message: '«نمی» با فعل بعد از خود با نیم‌فاصله نوشته می‌شود، نه فاصلهٔ کامل: «نمی‌رود».',
            count: function (t) {
                return matches(re('(?:^|[^' + WORD + '])نمی[ \\u00a0]+(?=[' + LET + '])'), t);
            }
        },
        {
            id: 'ha-space',
            level: 'warn',
            skipLast: true,
            message: '«ها» با نیم‌فاصله به واژهٔ پیش از خود می‌چسبد: «کتاب‌ها».',
            count: function (t) {
                return matches(re('[' + LET + '][ \\u00a0]+ها(?![' + WORD + '])'), t);
            }
        },
        {
            id: 'tarin-space',
            level: 'warn',
            skipLast: true,
            message: '«ترین» با نیم‌فاصله به واژهٔ پیش از خود می‌چسبد: «بزرگ‌ترین».',
            count: function (t) {
                return matches(re('[' + LET + '][ \\u00a0]+ترین(?![' + WORD + '])'), t);
            }
        },
        {
            id: 'heh-yeh',
            level: 'warn',
            skipLast: true,
            message: 'در پایان واژه‌هایی مثل «خانه» برای ی ِ اضافه از «هٔ» (ه + همزهٔ بالا) استفاده کنید، نه «ه‌ی» یا «ه ی».',
            count: function (t) {
                // a yeh followed by sukun (ْ) or any other letter/mark is a different word
                return matches(re('[' + LET + ']ه[ \\u00a0' + ZWNJ + ']ی(?![' + WORD + '])'), t);
            }
        },
        {
            id: 'spacing',
            level: 'warn',
            message: 'فاصله‌ها را مرتب کنید: فاصلهٔ اضافی، فاصله در ابتدا یا انتهای مصرع، و نیم‌فاصلهٔ تکراری یا کنار فاصله مجاز نیست.',
            count: function (t) {
                return sum(
                    matches(/[  ]{2,}/g, t),
                    matches(/^[  ]+|[  ]+$/g, t),
                    matches(/‌{2,}/g, t),
                    matches(/‌[  ]|[  ]‌/g, t),
                    matches(/^‌|‌$/g, t));
            }
        },
        {
            id: 'tatweel-digits',
            level: 'warn',
            message: 'از کشیدهٔ «ـ» و ارقام عربی (٠١٢٣…) استفاده نکنید؛ ارقام فارسی (۰۱۲۳…) درست است.',
            count: function (t) { return matches(/[ـ٠-٩]/g, t); }
        },
        {
            id: 'ke-an',
            level: 'suggest',
            message: 'پیشنهاد: «ک‌آن»، «ک‌این»، «که‌آن» و «که‌این» به‌صورت «کآن» و «کاین» نوشته می‌شوند.',
            count: function (t) {
                return matches(re('(?:^|[^' + WORD + '])که?' + ZWNJ + '(?:آن|این)(?![' + WORD + '])'), t);
            }
        }
    ];

    // returns [{id, level, message}] for the rules that match MORE in `text` than in `originalText`
    function analyze(text, originalText) {
        var result = [];
        text = text || '';
        originalText = originalText || '';
        for (var i = 0; i < rules.length; i++) {
            var rule = rules[i];
            var now = rule.skipLast ? withoutLastWord(text) : text;
            var before = rule.skipLast ? withoutLastWord(originalText) : originalText;
            if (rule.count(now) > rule.count(before)) {
                result.push({ id: rule.id, level: rule.level, message: rule.message });
            }
        }
        return result;
    }

    return { rules: rules, analyze: analyze };
})();
