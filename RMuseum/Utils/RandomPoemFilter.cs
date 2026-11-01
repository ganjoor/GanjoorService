using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RMuseum.Utils
{
    /// <summary>
    /// A deliberately blunt word check used ONLY by the random-poem API (poem/random and the faal
    /// endpoint) so that explicit poems are not served to people (and third-party apps) who did not ask
    /// for them. Excluding a poem from the random pool costs nothing - it stays everywhere else on the
    /// site - so the check errs on the side of excluding: a few innocent poems being skipped is fine.
    ///
    /// The text is normalized first (Arabic yeh/kaf unified, vowel marks, tatweel and ZWNJ removed) and a
    /// stem matches when it starts a word and is followed by at most three more letters (suffixes such as
    /// the possessive endings: کیرم، کونی ...). Words that are common in innocent text (e.g. «کس») must
    /// never be listed on their own.
    ///
    /// More stems can be added without touching the code: appsettings.json,
    /// "RandomPoem": { "ExtraBannedStems": [ "..." ] } (written in normal Persian spelling).
    /// </summary>
    public static class RandomPoemFilter
    {
        private const int MaxSuffixLetters = 3;

        private static readonly string[] DefaultStems = new[]
        {
            "کیر", "کون", "کسکش",
            "جنده", "مادرجنده", "جاکش", "قحبه", "مادرقحبه", "فاحشه",
            "گایی", "گائی", "لواط",
            "حرامزاده",
        };

        private static Regex _regex = Build(DefaultStems);

        /// <summary>
        /// reads the optional extra stems from configuration (RandomPoem:ExtraBannedStems)
        /// </summary>
        public static void Configure(IConfiguration configuration)
        {
            var extra = configuration?.GetSection("RandomPoem:ExtraBannedStems")
                .GetChildren()
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray() ?? new string[] { };
            _regex = Build(DefaultStems.Concat(extra));
        }

        /// <summary>
        /// true if none of the given texts contains a banned stem
        /// </summary>
        public static bool IsAllowed(params string[] texts)
        {
            var regex = _regex;
            foreach (var text in texts)
            {
                if (string.IsNullOrEmpty(text))
                    continue;
                if (regex.IsMatch(Normalize(text)))
                    return false;
            }
            return true;
        }

        private static Regex Build(IEnumerable<string> stems)
        {
            var normalized = stems
                .Select(s => Normalize(s))
                .Where(s => s.Length > 0)
                .Distinct()
                .Select(Regex.Escape);
            return new Regex(
                @"(?<!\p{L})(?:" + string.Join("|", normalized) + @")\p{L}{0," + MaxSuffixLetters + @"}(?!\p{L})",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        private static string Normalize(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char ch in text)
            {
                if ((ch >= 'ً' && ch <= 'ٟ') || ch == 'ٰ' || ch == 'ـ' || ch == '‌' || ch == '‍')
                    continue;
                switch (ch)
                {
                    case 'ي': sb.Append('ی'); break;
                    case 'ى': sb.Append('ی'); break;
                    case 'ك': sb.Append('ک'); break;
                    case 'ۀ': sb.Append('ه'); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }
    }
}
