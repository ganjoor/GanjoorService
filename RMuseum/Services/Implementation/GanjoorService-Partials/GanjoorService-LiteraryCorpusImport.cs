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
        /// Hafez ghazals): couplets are matched with the poems under <paramref name="urlPrefix"/> by their normalized text;
        /// tags get Origin = AcademicCorpus and OriginRef = file.Ref. A poem that already has tags with the same OriginRef
        /// is skipped, so the import can be repeated. Positions of words are only used if the poem's hemistich has exactly
        /// the same words as the corpus's, otherwise the value stays at couplet level.
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
                    .Where(d => d.Status == LiteraryEntityStatus.Approved && d.Key != null).ToListAsync();
                var deviceByKey = devices.ToDictionary(d => d.Key, d => d);
                var neededKeys = file.Items.SelectMany(i => i.Tags ?? new List<LiteraryCorpusImportTag>()).Select(t => t.Device).Distinct().ToList();
                var missing = neededKeys.Where(k => !deviceByKey.ContainsKey(k)).ToList();
                if (missing.Count > 0)
                    return new RServiceResult<LiteraryCorpusImportResult>(null, "آرایه‌های پایهٔ زیر وجود ندارد (ابتدا seed-core را اجرا کنید): " + string.Join("، ", missing));

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
                    string key = _CorpusCoupletKey(item.M1, item.M2);
                    if (!index.TryGetValue(key, out var candidates))
                    {
                        result.UnmatchedItems++;
                        if (result.Messages.Count < 40)
                            result.Messages.Add($"یافت نشد: غزل {item.GhazalId} بیت {item.BaytId}: {item.M1}");
                        continue;
                    }
                    CorpusCouplet found = null;
                    if (candidates.Count == 1)
                        found = candidates[0];
                    else
                    {
                        var byNumber = candidates.Where(c => c.PoemUrl.TrimEnd('/').EndsWith("/sh" + item.GhazalId)).ToList();
                        if (byNumber.Count == 1)
                            found = byNumber[0];
                    }
                    if (found == null)
                    {
                        result.AmbiguousItems++;
                        if (result.Messages.Count < 40)
                            result.Messages.Add($"چندمعنایی: غزل {item.GhazalId} بیت {item.BaytId}: {item.M1}");
                        continue;
                    }
                    result.MatchedItems++;
                    if (alreadyImportedPoems.Contains(found.PoemId))
                    {
                        result.SkippedExistingPoems++;
                        continue;
                    }

                    //words must be exactly those of the corpus, else positions mean nothing
                    string[] raw1 = (found.M1 ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    string[] raw2 = (found.M2 ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    bool sameWords1 = _CorpusTokens(found.M1).SequenceEqual(_CorpusTokens(item.M1));
                    bool sameWords2 = _CorpusTokens(found.M2).SequenceEqual(_CorpusTokens(item.M2));

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
                            var slot = device.Slots.FirstOrDefault(s => s.Key == part.Slot);
                            if (slot == null)
                            {
                                valid = false;
                                break;
                            }
                            var dbPart = new PoemLiteraryTagPart() { SlotId = slot.Id, Order = order++, CoupletIndex = found.Number };

                            bool isSpan = slot.Kind == LiteraryDeviceSlotKind.Span || slot.Kind == LiteraryDeviceSlotKind.SpanConcept;
                            if (isSpan)
                            {
                                bool located = part.Misra != null && part.WordStart != null && part.WordEnd != null
                                    && part.WordStart >= 1 && part.WordEnd >= part.WordStart
                                    && ((part.Misra == 1 && sameWords1 && part.WordEnd <= raw1.Length) || (part.Misra == 2 && sameWords2 && part.WordEnd <= raw2.Length));
                                if (located)
                                {
                                    dbPart.Misra = part.Misra.Value;
                                    dbPart.WordStart = part.WordStart;
                                    dbPart.WordEnd = part.WordEnd;
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
