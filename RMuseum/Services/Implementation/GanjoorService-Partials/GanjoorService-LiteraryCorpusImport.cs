using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        private static readonly Regex _corpusStripRegex = new Regex("[ً-ٰٟـ‌‍‎‏؜]", RegexOptions.Compiled);
        private static readonly Regex _corpusNonWordRegex = new Regex(@"[^\w]", RegexOptions.Compiled);

        /// <summary>
        /// word normalization used to compare corpus text with the poem text (same rules as the converter that
        /// produced the import file): letter forms, diacritics, ZWNJ and punctuation are ignored
        /// </summary>
        private static string _CorpusNormalizeWord(string w)
        {
            w = w.Replace('ي', 'ی').Replace('ك', 'ک').Replace('ۀ', 'ه').Replace('ة', 'ه')
                .Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا').Replace('ؤ', 'و').Replace('ئ', 'ی');
            w = _corpusStripRegex.Replace(w, "");
            return _corpusNonWordRegex.Replace(w, "");
        }

        private static string[] _CorpusTokens(string text)
        {
            return (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Select(_CorpusNormalizeWord).ToArray();
        }

        private static string _CorpusCoupletKey(string m1, string m2)
        {
            return string.Join(" ", _CorpusTokens(m1).Where(t => t != "")) + "|" + string.Join(" ", _CorpusTokens(m2).Where(t => t != ""));
        }

        /// <summary>
        /// the letters of a text (normalized, without spaces/ZWNJ/punctuation) and where each whitespace separated
        /// token starts and ends in them
        /// </summary>
        private class CorpusLetterMap
        {
            public string Letters { get; set; }
            public List<int> TokenStart { get; set; } = new List<int>();
            public List<int> TokenEnd { get; set; } = new List<int>();
        }

        private static CorpusLetterMap _CorpusBuildLetterMap(string text)
        {
            var map = new CorpusLetterMap();
            var sb = new System.Text.StringBuilder();
            foreach (var token in (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                map.TokenStart.Add(sb.Length);
                sb.Append(_CorpusNormalizeWord(token));
                map.TokenEnd.Add(sb.Length);
            }
            map.Letters = sb.ToString();
            return map;
        }

        /// <summary>
        /// Levenshtein alignment; result[i] is the index in b aligned with a[i] (substitutions count as aligned), -1 if a[i] was deleted
        /// </summary>
        private static int[] _CorpusAlign(string a, string b, out int distance)
        {
            int n = a.Length, m = b.Length;
            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            distance = d[n, m];
            var res = new int[n];
            for (int i = 0; i < n; i++) res[i] = -1;
            int x = n, y = m;
            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0 && d[x, y] == d[x - 1, y - 1] + (a[x - 1] == b[y - 1] ? 0 : 1))
                {
                    res[x - 1] = y - 1;
                    x--; y--;
                }
                else if (x > 0 && d[x, y] == d[x - 1, y] + 1)
                {
                    x--;
                }
                else
                {
                    y--;
                }
            }
            return res;
        }

        private static double _CorpusSimilarity(string a, string b)
        {
            int max = Math.Max(a.Length, b.Length);
            if (max == 0) return 1;
            _CorpusAlign(a, b, out int distance);
            return 1.0 - (double)distance / max;
        }

        /// <summary>
        /// moves a word range of the corpus text to the current text of the same hemistich by aligning their letters,
        /// so typo fixes, different spacing and new punctuation do not matter. Returns false if the hemistich changed too much.
        /// </summary>
        private static bool _CorpusMapRange(CorpusLetterMap corpus, CorpusLetterMap current, int wordStart, int wordEnd, out int newStart, out int newEnd)
        {
            newStart = newEnd = 0;
            if (wordStart < 1 || wordEnd < wordStart || wordEnd > corpus.TokenStart.Count)
                return false;
            int cs = corpus.TokenStart[wordStart - 1];
            int ce = corpus.TokenEnd[wordEnd - 1];
            if (ce <= cs || current.Letters.Length == 0)
                return false;
            var map = _CorpusAlign(corpus.Letters, current.Letters, out int distance);
            if (1.0 - (double)distance / Math.Max(corpus.Letters.Length, current.Letters.Length) < 0.7)
                return false;
            int first = -1, last = -1;
            for (int k = cs; k < ce; k++)
            {
                if (map[k] < 0) continue;
                if (first < 0) first = map[k];
                last = map[k];
            }
            if (first < 0)
                return false;
            int s = -1, e = -1;
            for (int t = 0; t < current.TokenStart.Count; t++)
            {
                if (current.TokenEnd[t] <= current.TokenStart[t]) continue;
                if (s < 0 && first >= current.TokenStart[t] && first < current.TokenEnd[t]) s = t;
                if (last >= current.TokenStart[t] && last < current.TokenEnd[t]) e = t;
            }
            if (s < 0 || e < s)
                return false;
            newStart = s + 1;
            newEnd = e + 1;
            return true;
        }

        private class CorpusCouplet
        {
            public int PoemId { get; set; }
            public string PoemUrl { get; set; }
            public int Number { get; set; }
            public string M1 { get; set; }
            public string M2 { get; set; }
        }

        /// <summary>
        /// imports literary tags from an annotated corpus file (e.g. the MIT licensed "A Corpus of Persian Literary Text",
        /// Hafez ghazals): couplets are matched with the poems under <paramref name="urlPrefix"/> by poem number (…/shN) and couplet number, checked by letter similarity, with exact text as fallback;
        /// tags get Origin = AcademicCorpus and OriginRef = file.Ref. A poem that already has tags with the same OriginRef
        /// is skipped, so the import can be repeated. Positions of words are moved to the current text by aligning the letters
        /// of the hemistichs (typo fixes, spacing and punctuation changes are tolerated); if a hemistich changed too much the value stays at couplet level.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="urlPrefix">e.g. /hafez/ghazal/</param>
        /// <param name="dryRun">only report</param>
        /// <returns></returns>
        public async Task<RServiceResult<LiteraryCorpusImportResult>> ImportLiteraryCorpusAsync(LiteraryCorpusImportFile file, string urlPrefix, bool dryRun)
        {
            try
            {
                if (file == null || file.Items == null || file.Items.Count == 0)
                    return new RServiceResult<LiteraryCorpusImportResult>(null, "فایل خالی است.");
                if (string.IsNullOrWhiteSpace(file.Ref))
                    return new RServiceResult<LiteraryCorpusImportResult>(null, "مرجع (Ref) فایل مشخص نشده است.");
                if (string.IsNullOrWhiteSpace(urlPrefix) || !urlPrefix.StartsWith("/"))
                    return new RServiceResult<LiteraryCorpusImportResult>(null, "پیشوند نشانی شعرها نامعتبر است.");

                var result = new LiteraryCorpusImportResult() { DryRun = dryRun, Items = file.Items.Count };

                var devices = await _context.LiteraryDevices.AsNoTracking().Include(d => d.Slots)
                    .Where(d => d.Status == LiteraryEntityStatus.Approved).ToListAsync();
                //a device is referred to by its key (core devices) or by its name (approved suggested devices have no key)
                var deviceByKey = new Dictionary<string, LiteraryDevice>();
                foreach (var d in devices.Where(d => d.Key != null))
                    deviceByKey[d.Key] = d;
                foreach (var d in devices)
                {
                    string deviceName = d.Name?.Trim();
                    if (!string.IsNullOrEmpty(deviceName) && !deviceByKey.ContainsKey(deviceName))
                        deviceByKey[deviceName] = d;
                }
                var neededKeys = file.Items.SelectMany(i => i.Tags ?? new List<LiteraryCorpusImportTag>()).Select(t => t.Device).Distinct().ToList();
                var missing = neededKeys.Where(k => !deviceByKey.ContainsKey(k)).ToList();
                if (missing.Count > 0)
                    return new RServiceResult<LiteraryCorpusImportResult>(null, "آرایه‌های زیر وجود ندارند یا هنوز تأیید نشده‌اند (برای آرایه‌های پایه seed-core را اجرا کنید و آرایه‌های پیشنهادی را با نام دقیق بنویسید): " + string.Join("، ", missing));

                var poems = await _context.GanjoorPoems.AsNoTracking()
                    .Where(p => p.FullUrl.StartsWith(urlPrefix))
                    .Select(p => new { p.Id, p.FullUrl }).ToListAsync();
                if (poems.Count == 0)
                    return new RServiceResult<LiteraryCorpusImportResult>(null, "شعری با این پیشوند نشانی یافت نشد.");
                var poemIds = poems.Select(p => p.Id).ToList();
                var poemUrl = poems.ToDictionary(p => p.Id, p => p.FullUrl);

                var verses = await _context.GanjoorVerses.AsNoTracking()
                    .Where(v => poemIds.Contains(v.PoemId))
                    .OrderBy(v => v.PoemId).ThenBy(v => v.VOrder)
                    .Select(v => new { v.PoemId, v.VersePosition, v.Text }).ToListAsync();

                //couplets, numbered like the poem editor does (1 based, comments skipped)
                var couplets = new List<CorpusCouplet>();
                foreach (var group in verses.GroupBy(v => v.PoemId))
                {
                    int n = 0;
                    CorpusCouplet current = null;
                    foreach (var v in group)
                    {
                        switch (v.VersePosition)
                        {
                            case VersePosition.Right:
                            case VersePosition.CenteredVerse1:
                                n++;
                                current = new CorpusCouplet() { PoemId = group.Key, PoemUrl = poemUrl[group.Key], Number = n, M1 = v.Text };
                                couplets.Add(current);
                                break;
                            case VersePosition.Left:
                            case VersePosition.CenteredVerse2:
                                if (current != null)
                                    current.M2 = v.Text;
                                break;
                            case VersePosition.Single:
                            case VersePosition.Paragraph:
                                n++;
                                current = null;
                                break;
                        }
                    }
                }
                var index = couplets.Where(c => c.M2 != null).GroupBy(c => _CorpusCoupletKey(c.M1, c.M2)).ToDictionary(g => g.Key, g => g.ToList());

                //poem number (…/sh12) and couplet number: the corpus is older than the text, but order and number of couplets are the same
                var coupletByPosition = new Dictionary<string, CorpusCouplet>();
                foreach (var c in couplets)
                {
                    var m = Regex.Match(c.PoemUrl.TrimEnd('/'), @"/sh(\d+)$");
                    if (m.Success)
                        coupletByPosition[m.Groups[1].Value + "/" + c.Number] = c;
                }

                var alreadyImportedPoems = new HashSet<int>(await _context.PoemLiteraryTags.AsNoTracking()
                    .Where(t => t.Origin == LiteraryTagOrigin.AcademicCorpus && t.OriginRef == file.Ref && poemIds.Contains(t.PoemId))
                    .Select(t => t.PoemId).Distinct().ToListAsync());

                //concepts by normalized name and alias
                var concepts = await _context.LiteraryConcepts.ToListAsync();
                var conceptByName = new Dictionary<string, LiteraryConcept>();
                foreach (var c in concepts)
                {
                    conceptByName[c.Name] = c;
                }
                foreach (var c in concepts.Where(c => !string.IsNullOrEmpty(c.Aliases)))
                    foreach (var alias in c.Aliases.Split('|').Select(a => a.Trim()).Where(a => a != ""))
                        if (!conceptByName.ContainsKey(alias))
                            conceptByName[alias] = c;
                var plannedNewConcepts = new HashSet<string>();

                var signatures = new HashSet<string>();
                var newTags = new List<PoemLiteraryTag>();
                var now = DateTime.Now;

                foreach (var item in file.Items)
                {
                    if (item.Tags == null || item.Tags.Count == 0)
                        continue;
                    string corpusText1 = string.Join("", _CorpusTokens(item.M1));
                    string corpusText2 = string.Join("", _CorpusTokens(item.M2));
                    CorpusCouplet found = null;
                    bool rejectedHere = false;
                    if (coupletByPosition.TryGetValue(item.GhazalId + "/" + item.BaytId, out var byPosition) && byPosition.M2 != null)
                    {
                        double similarity = _CorpusSimilarity(corpusText1 + corpusText2, string.Join("", _CorpusTokens(byPosition.M1)) + string.Join("", _CorpusTokens(byPosition.M2)));
                        if (similarity >= 0.7)
                        {
                            found = byPosition;
                            result.MatchedByPosition++;
                        }
                        else
                        {
                            rejectedHere = true;
                            if (result.Messages.Count < 40)
                                result.Messages.Add($"شباهت کم ({similarity:0.00}) در غزل {item.GhazalId} بیت {item.BaytId}: «{item.M1}» ≠ «{byPosition.M1}»");
                        }
                    }
                    if (found == null)
                    {
                        //fall back to the exact text
                        if (index.TryGetValue(_CorpusCoupletKey(item.M1, item.M2), out var candidates) && candidates.Count == 1)
                        {
                            found = candidates[0];
                            result.MatchedByText++;
                        }
                    }
                    if (found == null)
                    {
                        result.UnmatchedItems++;
                        if (rejectedHere)
                            result.RejectedLowSimilarity++;
                        continue;
                    }
                    result.MatchedItems++;
                    if (alreadyImportedPoems.Contains(found.PoemId))
                    {
                        result.SkippedExistingPoems++;
                        continue;
                    }

                    var corpusMap1 = _CorpusBuildLetterMap(item.M1);
                    var corpusMap2 = _CorpusBuildLetterMap(item.M2);
                    var currentMap1 = _CorpusBuildLetterMap(found.M1);
                    var currentMap2 = _CorpusBuildLetterMap(found.M2);

                    foreach (var tag in item.Tags)
                    {
                        var device = deviceByKey[tag.Device];
                        var dbTag = new PoemLiteraryTag()
                        {
                            PoemId = found.PoemId,
                            CoupletIndex = found.Number,
                            DeviceId = device.Id,
                            Origin = LiteraryTagOrigin.AcademicCorpus,
                            OriginRef = file.Ref,
                            CreateDate = now,
                            Parts = new List<PoemLiteraryTagPart>()
                        };
                        int order = 0;
                        bool valid = true;
                        foreach (var part in tag.Parts ?? new List<LiteraryCorpusImportPart>())
                        {
                            var slot = device.Slots.FirstOrDefault(s => s.Key == part.Slot) ?? device.Slots.FirstOrDefault(s => s.Name != null && s.Name.Trim() == part.Slot?.Trim());
                            if (slot == null)
                            {
                                valid = false;
                                break;
                            }
                            var dbPart = new PoemLiteraryTagPart() { SlotId = slot.Id, Order = order++, CoupletIndex = found.Number };

                            bool isSpan = slot.Kind == LiteraryDeviceSlotKind.Span || slot.Kind == LiteraryDeviceSlotKind.SpanConcept;
                            if (isSpan)
                            {
                                bool located = false;
                                int newStart = 0, newEnd = 0;
                                if ((part.Misra == 1 || part.Misra == 2) && part.WordStart != null && part.WordEnd != null)
                                {
                                    located = part.Misra == 1
                                        ? _CorpusMapRange(corpusMap1, currentMap1, part.WordStart.Value, part.WordEnd.Value, out newStart, out newEnd)
                                        : _CorpusMapRange(corpusMap2, currentMap2, part.WordStart.Value, part.WordEnd.Value, out newStart, out newEnd);
                                }
                                if (located)
                                {
                                    dbPart.Misra = part.Misra.Value;
                                    dbPart.WordStart = newStart;
                                    dbPart.WordEnd = newEnd;
                                    result.PartsLocated++;
                                }
                                else
                                {
                                    result.PartsUnlocated++;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(part.Concept))
                            {
                                string name = _NormalizeConceptName(part.Concept);
                                if (name.Length > LiteraryConceptNameMaxLength)
                                    name = name.Substring(0, LiteraryConceptNameMaxLength);
                                if (!conceptByName.TryGetValue(name, out var concept))
                                {
                                    concept = new LiteraryConcept() { Name = name, Status = LiteraryEntityStatus.Approved, CreateDate = now };
                                    conceptByName[name] = concept;
                                    plannedNewConcepts.Add(name);
                                    if (!dryRun)
                                        _context.LiteraryConcepts.Add(concept);
                                }
                                if (!dryRun)
                                    dbPart.Concept = concept;
                            }
                            if (!string.IsNullOrWhiteSpace(part.Text))
                            {
                                string text = part.Text.Trim();
                                dbPart.Text = text.Length > LiteraryTextMaxLength ? text.Substring(0, LiteraryTextMaxLength) : text;
                            }
                            dbTag.Parts.Add(dbPart);
                        }
                        if (!valid || dbTag.Parts.Count == 0)
                            continue;

                        string signature = $"{found.PoemId}|{found.Number}|{device.Id}|" + string.Join(";", tag.Parts
                            .Select(p => $"{p.Slot}/{p.Misra}/{p.WordStart}/{p.WordEnd}/{p.Concept}/{p.Text}").OrderBy(x => x));
                        if (!signatures.Add(signature))
                            continue;

                        newTags.Add(dbTag);
                        result.TagsCreated++;
                    }
                }

                result.ConceptsCreated = plannedNewConcepts.Count;

                if (!dryRun && newTags.Count > 0)
                {
                    const int batch = 200;
                    for (int i = 0; i < newTags.Count; i += batch)
                    {
                        _context.PoemLiteraryTags.AddRange(newTags.Skip(i).Take(batch));
                        await _context.SaveChangesAsync();
                    }
                }

                return new RServiceResult<LiteraryCorpusImportResult>(result);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryCorpusImportResult>(null, exp.ToString());
            }
        }
    }
}
