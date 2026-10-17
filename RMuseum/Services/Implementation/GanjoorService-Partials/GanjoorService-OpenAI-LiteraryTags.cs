using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        #region AI literary tags DTOs

        private class AILiteraryPart
        {
            public int? Slot { get; set; }
            public int? Couplet { get; set; }
            public int? Misra { get; set; }
            public int? From { get; set; }
            public int? To { get; set; }
            public string Quote { get; set; }
            public string Concept { get; set; }
            public string Text { get; set; }
        }

        private class AILiteraryTag
        {
            public int? Device { get; set; }
            public int? Couplet { get; set; }
            public bool? Uncertain { get; set; }
            public string Note { get; set; }
            public List<AILiteraryPart> Parts { get; set; }
        }

        private class AILiteraryResult
        {
            public List<AILiteraryTag> Tags { get; set; }
        }

        private class AILiteraryCouplet
        {
            public int N;
            public string[] W1 = new string[0];
            public string[] W2 = new string[0];
        }

        private class AILiteraryContext
        {
            public AIPeopleRun Run;
            public Dictionary<int, LiteraryDevice> Devices;
            //normalized concept name / alias => approved concept id
            public Dictionary<string, int> ConceptIds = new Dictionary<string, int>();
            public string Catalogue;
            public int Rejected;
        }

        #endregion

        private const int AILiteraryWindowThreshold = 30;
        private const int AILiteraryWindowSize = 24;
        private const int AILiteraryWindowOverlap = 3;

        private const string AILiteraryPrompt = @"تو کارشناس بدیع و بیان فارسی هستی. آرایه‌های ادبی شعر زیر را بر پایهٔ فهرست آرایه‌های داده‌شده تشخیص بده.

قواعد:
- فقط از آرایه‌های فهرست استفاده کن و شمارهٔ آرایه (device) و شمارهٔ جایگاه‌ها (slot) را دقیقاً از فهرست بردار.
- کیفیت مهم‌تر از تعداد است. فقط مواردی را بنویس که آشکار و نسبتاً مطمئنی؛ اگر تردید داری uncertain را true بگذار و اگر بیشتر تردید داری اصلاً ننویس. برای هر بیت حداکثر دو آرایهٔ برجسته بنویس و بیتی که آرایهٔ روشنی ندارد را خالی بگذار.
- تناسب و تضاد را فقط وقتی بنویس که دو واژه یا گروه کوتاه آشکارا از یک مجموعهٔ معنایی یا دو قطب مقابل‌اند؛ هر ارتباط دوری یا سلیقه‌ای تناسب نیست. تلمیح را فقط وقتی بنویس که به داستان، آیه، حدیث یا شعری مشخص و شناخته اشاره شده باشد، نه برای هر مضمون دینی یا عرفانی.
- هر کلمهٔ مصرع‌ها پیش از خودش شماره دارد (مثلاً ۳:خار). جای هر جایگاه متنی را با couplet (شمارهٔ بیت درون [ ])، misra (۱ مصرع اول، ۲ مصرع دوم)، from و to (شمارهٔ اولین و آخرین کلمه، هر دو شامل) مشخص کن و همان کلمه‌ها را بی‌تغییر در quote بنویس. بازه را تا حد ممکن کوتاه بگیر: فقط خود واژه‌ها یا گروه کوتاهی که آرایه در آن‌هاست، نه صفت‌ها و واژه‌های اطراف و نه یک مصرع کامل. بازه نباید از یک مصرع بیرون برود.
- جایگاه‌های مفهومی را با concept پر کن: نام کوتاه و متعارف مفهوم، مفرد و حداکثر سه واژه، بی‌حرف اضافه (مثلاً «شمع»، «زلف»، «عاشق»). جمله یا عبارت توضیحی ننویس.
- جایگاه‌های متنی را با text پر کن (کوتاه).
- couplet هر آرایه شمارهٔ بیت اصلی آن است؛ برای آرایه‌ای که کل شعر را در بر می‌گیرد 0 بگذار.
- note را یک جملهٔ کوتاه فارسی بنویس که دلیل تشخیص را بگوید. در note دربارهٔ واژه‌ها ادعای واقعیت‌محور نکن مگر مطمئن باشی، و هیچ مصرعی را بلند نقل نکن.
- هیچ توضیح بیرون از JSON ننویس.

خروجی فقط یک JSON با این ساختار باشد:
{""tags"":[{""device"":شماره,""couplet"":شماره,""uncertain"":false,""note"":""..."",""parts"":[{""slot"":شماره,""couplet"":شماره,""misra"":1,""from"":1,""to"":2,""quote"":""..."",""concept"":""..."",""text"":""...""}]}]}
(در هر part فقط فیلدهای لازم برای نوع آن جایگاه را بیاور.) اگر هیچ آرایه‌ای نبود: {""tags"":[]}

";

        /// <summary>
        /// form used to compare what the model quotes with the poem: no vowel marks, punctuation, spaces or ZWNJ
        /// </summary>
        private static string _AILiteraryNorm(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var sb = new StringBuilder();
            foreach (char ch in text)
            {
                if ((ch >= '\u064B' && ch <= '\u065F') || ch == '\u0670' || ch == '\u0640' || ch == '\u200c' || ch == '\u200d')
                    continue;
                if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch))
                    continue;
                sb.Append(ch == 'ي' ? 'ی' : ch == 'ك' ? 'ک' : ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// finds the words of <paramref name="words"/> that spell <paramref name="quote"/>, preferring the window nearest to
        /// the range the model gave (the model often miscounts words but quotes them right)
        /// </summary>
        private static bool _AILiteraryLocate(string[] words, string quote, int hintFrom, out int from, out int to)
        {
            from = to = 0;
            string wanted = _AILiteraryNorm(quote);
            if (wanted.Length == 0)
                return false;
            int bestDistance = int.MaxValue;
            for (int start = 0; start < words.Length; start++)
            {
                var built = new StringBuilder();
                for (int end = start; end < words.Length && built.Length <= wanted.Length; end++)
                {
                    built.Append(_AILiteraryNorm(words[end]));
                    if (built.Length == wanted.Length && built.ToString() == wanted)
                    {
                        int distance = Math.Abs(start + 1 - hintFrom);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            from = start + 1;
                            to = end + 1;
                        }
                        break;
                    }
                }
            }
            return bestDistance != int.MaxValue;
        }

        /// <summary>
        /// the tag as one log line: device, couplet and what each slot holds (words, concept, text)
        /// </summary>
        private static string _AILiteraryDescribe(AILiteraryContext lc, GanjoorPoemLiteraryTagCorrection tag, Dictionary<int, AILiteraryCouplet> couplets)
        {
            var device = lc.Devices[tag.DeviceId];
            var items = new List<string>();
            foreach (var part in _ReadParts(tag.PartsJson))
            {
                var slot = device.Slots.FirstOrDefault(sl => sl.Id == part.SlotId);
                var pieces = new List<string>();
                if (part.WordStart != null && part.WordEnd != null && couplets.TryGetValue(part.CoupletIndex, out var c))
                {
                    var words = part.Misra == 1 ? c.W1 : c.W2;
                    pieces.Add("«" + string.Join(" ", words.Skip(part.WordStart.Value - 1).Take(part.WordEnd.Value - part.WordStart.Value + 1)) + "»" + (part.CoupletIndex != tag.CoupletIndex ? $" (بیت {part.CoupletIndex})" : ""));
                }
                if (part.ConceptId != null)
                    pieces.Add("مفهوم #" + part.ConceptId);
                if (!string.IsNullOrWhiteSpace(part.SuggestedConceptName))
                    pieces.Add("مفهوم جدید: " + part.SuggestedConceptName);
                if (!string.IsNullOrWhiteSpace(part.Text))
                    pieces.Add(part.Text);
                items.Add($"{slot?.Name}: {string.Join(" - ", pieces)}");
            }
            return string.Join(" | ", items);
        }

        private static string[] _AIWords(string text)
        {
            return Regex.Split((text ?? "").Trim(), @"\s+").Where(w => w.Length > 0).ToArray();
        }

        /// <summary>
        /// couplets numbered exactly like the editor's word picker (see Editor.LiteraryCoupletsJson)
        /// </summary>
        private static List<AILiteraryCouplet> _AILiteraryCouplets(IEnumerable<(VersePosition Position, string Text)> verses)
        {
            var list = new List<AILiteraryCouplet>();
            AILiteraryCouplet current = null;
            int n = 0;
            foreach (var v in verses)
            {
                switch (v.Position)
                {
                    case VersePosition.Comment:
                        break;
                    case VersePosition.Paragraph:
                    case VersePosition.Single:
                        n++;
                        list.Add(new AILiteraryCouplet() { N = n, W1 = _AIWords(v.Text) });
                        current = null;
                        break;
                    case VersePosition.Right:
                    case VersePosition.CenteredVerse1:
                        n++;
                        current = new AILiteraryCouplet() { N = n, W1 = _AIWords(v.Text) };
                        list.Add(current);
                        break;
                    case VersePosition.Left:
                    case VersePosition.CenteredVerse2:
                        if (current != null)
                            current.W2 = _AIWords(v.Text);
                        break;
                }
            }
            return list;
        }

        private static string _AILiteraryRender(AILiteraryCouplet c)
        {
            string hemistich(string[] words) => string.Join(" ", words.Select((w, i) => $"{i + 1}:{w}"));
            return c.W2.Length == 0 ? $"[{c.N}] {hemistich(c.W1)}" : $"[{c.N}] {hemistich(c.W1)} / {hemistich(c.W2)}";
        }

        private static string _AILiteraryCatalogue(IEnumerable<LiteraryDevice> devices)
        {
            var sb = new StringBuilder();
            sb.AppendLine("فهرست آرایه‌ها:");
            foreach (var d in devices.OrderBy(d => d.Id))
            {
                sb.AppendLine($"آرایه {d.Id}: {d.Name} - {_AIShort(d.Definition ?? "", 300)}");
                if (!string.IsNullOrWhiteSpace(d.Example))
                    sb.AppendLine($"  نمونه: {_AIShort(d.Example, 200)}");
                foreach (var s in d.Slots.OrderBy(s => s.Order))
                {
                    string kind;
                    switch (s.Kind)
                    {
                        case LiteraryDeviceSlotKind.Span: kind = "بخشی از متن: couplet, misra, from, to"; break;
                        case LiteraryDeviceSlotKind.SpanConcept: kind = "بخشی از متن و مفهوم آن: couplet, misra, from, to, concept"; break;
                        case LiteraryDeviceSlotKind.Concept: kind = "مفهوم: concept"; break;
                        default: kind = "متن آزاد: text"; break;
                    }
                    sb.AppendLine($"  جایگاه {s.Id}: {s.Name} ({kind}{(s.Required ? "؛ الزامی" : "")}{(s.AllowMultiple ? "؛ چندبار ممکن" : "")})");
                }
            }
            return sb.ToString();
        }

        private static string _AILiteraryPartsSignature(IEnumerable<LiteraryTagPartSuggestion> parts)
        {
            return _PartsSignature(parts.Select(p => Tuple.Create(p.SlotId, p.CoupletIndex, p.Misra, p.WordStart, p.WordEnd, p.ConceptId, _TrimOrNull(p.Text))));
        }

        /// <summary>
        /// checks one AI answer against the device definition and the poem text, same rules as submission validation
        /// </summary>
        /// <returns>null (with error set) if the tag is not acceptable</returns>
        private static GanjoorPoemLiteraryTagCorrection _AILiteraryBuild(AILiteraryContext lc, AILiteraryTag ai, Dictionary<int, AILiteraryCouplet> couplets, bool wholePoemAllowed, out string error)
        {
            error = null;
            if (ai == null || ai.Device == null || !lc.Devices.TryGetValue(ai.Device.Value, out var device))
            {
                error = "unknown device";
                return null;
            }
            var parts = new List<LiteraryTagPartSuggestion>();
            foreach (var p in ai.Parts ?? new List<AILiteraryPart>())
            {
                var slot = p?.Slot == null ? null : device.Slots.FirstOrDefault(s => s.Id == p.Slot.Value);
                if (slot == null)
                {
                    error = $"{device.Name}: unknown slot {p?.Slot}";
                    return null;
                }
                bool needsSpan = slot.Kind == LiteraryDeviceSlotKind.Span || slot.Kind == LiteraryDeviceSlotKind.SpanConcept;
                bool needsConcept = slot.Kind == LiteraryDeviceSlotKind.Concept || slot.Kind == LiteraryDeviceSlotKind.SpanConcept;
                bool needsText = slot.Kind == LiteraryDeviceSlotKind.Text;
                var part = new LiteraryTagPartSuggestion() { SlotId = slot.Id };

                if (needsSpan)
                {
                    int coupletNo = p.Couplet ?? ai.Couplet ?? 0;
                    if (!couplets.TryGetValue(coupletNo, out var c))
                    {
                        error = $"{device.Name}/{slot.Name}: couplet {coupletNo} is not in the poem";
                        return null;
                    }
                    int misra = p.Misra ?? 0;
                    var words = misra == 1 ? c.W1 : misra == 2 ? c.W2 : null;
                    if (words == null || words.Length == 0)
                    {
                        error = $"{device.Name}/{slot.Name}: invalid hemistich {coupletNo}/{misra}";
                        return null;
                    }
                    if (string.IsNullOrWhiteSpace(p.Quote))
                    {
                        error = $"{device.Name}/{slot.Name}: no quote for the words";
                        return null;
                    }
                    // the numbers must point at the words that were quoted; if they do not, the quote wins (models miscount words)
                    int from = p.From ?? 0, to = p.To ?? 0;
                    bool numbersFit = from >= 1 && to >= from && to <= words.Length
                                      && _AILiteraryNorm(string.Concat(words.Skip(from - 1).Take(to - from + 1))) == _AILiteraryNorm(p.Quote);
                    if (!numbersFit && !_AILiteraryLocate(words, p.Quote, from, out from, out to))
                    {
                        error = $"{device.Name}/{slot.Name}: «{_AIShort(p.Quote, 40)}» is not in hemistich {coupletNo}/{misra}";
                        return null;
                    }
                    part.CoupletIndex = coupletNo;
                    part.Misra = misra;
                    part.WordStart = from;
                    part.WordEnd = to;
                }
                if (needsConcept)
                {
                    string name = _NormalizeConceptName(p.Concept);
                    if (name.Length == 0 || name.Length > LiteraryConceptNameMaxLength || name.Split(' ').Length > 3)
                    {
                        error = $"{device.Name}/{slot.Name}: concept «{_AIShort(name, 40)}» is not a short name";
                        return null;
                    }
                    if (lc.ConceptIds.TryGetValue(name, out int conceptId))
                        part.ConceptId = conceptId;
                    else
                        part.SuggestedConceptName = name;
                }
                if (needsText)
                {
                    string text = _TrimOrNull(p.Text);
                    if (text == null || text.Length > LiteraryTextMaxLength)
                    {
                        error = $"{device.Name}/{slot.Name}: invalid text";
                        return null;
                    }
                    part.Text = text;
                }
                parts.Add(part);
            }

            foreach (var slot in device.Slots)
            {
                int count = parts.Count(p => p.SlotId == slot.Id);
                if ((slot.Required && count == 0) || (!slot.AllowMultiple && count > 1))
                {
                    error = $"{device.Name}/{slot.Name}: slot count {count}";
                    return null;
                }
            }

            int main = ai.Couplet ?? 0;
            if (main == 0 && !wholePoemAllowed)
                main = parts.Where(p => p.CoupletIndex > 0).Select(p => p.CoupletIndex).DefaultIfEmpty(0).Min();
            if (main < 0 || (main == 0 && !wholePoemAllowed) || (main > 0 && !couplets.ContainsKey(main)))
            {
                error = $"{device.Name}: invalid couplet {ai.Couplet}";
                return null;
            }

            string note = _TrimOrNull(ai.Note);
            if (note != null && note.Length > LiteraryNoteMaxLength)
                note = note.Substring(0, LiteraryNoteMaxLength);
            return new GanjoorPoemLiteraryTagCorrection()
            {
                CoupletIndex = main,
                DeviceId = device.Id,
                Uncertain = ai.Uncertain == true,
                PartsJson = JsonConvert.SerializeObject(parts),
                SuggestionNote = note,
                Result = CorrectionReviewResult.NotReviewed,
            };
        }

        private async Task _AILiteraryProcessPoemAsync(AILiteraryContext lc, int poemId, string poemTitle)
        {
            var run = lc.Run;
            var ctx = run.Context;
            string tag = $"[{poemId}] {poemTitle}";

            //never clobber a pending literary suggestion of this user on the poem (review it first, then run again)
            if (await ctx.GanjoorPoemCorrections.AsNoTracking().AnyAsync(c => c.PoemId == poemId && c.UserId == run.UserId && !c.Reviewed && c.LiteraryTags.Any()))
            {
                run.PoemsSkipped++;
                run.Log.AppendLine($"{tag}: skipped (a pending literary suggestion of this user exists)");
                return;
            }

            var verseRows = await ctx.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poemId).OrderBy(v => v.VOrder)
                .Select(v => new { v.VersePosition, v.Text }).ToListAsync();
            var couplets = _AILiteraryCouplets(verseRows.Select(v => (v.VersePosition, v.Text)));
            couplets = couplets.Where(c => c.W1.Length > 0).ToList();
            if (couplets.Count == 0)
            {
                run.PoemsSkipped++;
                run.Log.AppendLine($"{tag}: skipped (no couplets)");
                return;
            }
            var coupletMap = couplets.ToDictionary(c => c.N);

            //what is already tagged (live or pending) is not suggested again
            var known = new HashSet<string>();
            foreach (var t in await ctx.PoemLiteraryTags.AsNoTracking().Include(t => t.Parts).Where(t => t.PoemId == poemId).ToListAsync())
            {
                known.Add($"{t.CoupletIndex}|{t.DeviceId}|" + _PartsSignature(t.Parts.Select(p => Tuple.Create(p.SlotId, p.CoupletIndex, p.Misra, p.WordStart, p.WordEnd, p.ConceptId, _TrimOrNull(p.Text)))));
            }
            var pendingRows = await ctx.GanjoorPoemCorrections.AsNoTracking().Where(c => c.PoemId == poemId && !c.Reviewed)
                .SelectMany(c => c.LiteraryTags).Where(t => !t.MarkForDelete).Select(t => new { t.CoupletIndex, t.DeviceId, t.PartsJson }).ToListAsync();
            foreach (var t in pendingRows)
            {
                try
                {
                    known.Add($"{t.CoupletIndex}|{t.DeviceId}|" + _AILiteraryPartsSignature(_ReadParts(t.PartsJson)));
                }
                catch (Exception)
                {
                    //unreadable pending json: nothing to compare with
                }
            }

            //long poems are asked in overlapping windows of couplets
            var windows = new List<List<AILiteraryCouplet>>();
            if (couplets.Count <= AILiteraryWindowThreshold)
            {
                windows.Add(couplets);
            }
            else
            {
                for (int start = 0; start < couplets.Count; start += AILiteraryWindowSize - AILiteraryWindowOverlap)
                {
                    windows.Add(couplets.Skip(start).Take(AILiteraryWindowSize).ToList());
                    if (start + AILiteraryWindowSize >= couplets.Count)
                        break;
                }
            }

            var newTags = new List<GanjoorPoemLiteraryTagCorrection>();
            for (int w = 0; w < windows.Count; w++)
            {
                var text = new StringBuilder();
                foreach (var c in windows[w])
                    text.AppendLine(_AILiteraryRender(c));
                var answer = await _AIAskJsonAsync<AILiteraryResult>(run, AILiteraryPrompt + lc.Catalogue + Environment.NewLine + "شعر:" + Environment.NewLine + text);
                if (answer == null)
                {
                    run.PoemsFailed++;
                    run.Log.AppendLine($"{tag}: AI request failed (part {w + 1}/{windows.Count}) - {run.LastAIError}");
                    return;
                }
                var windowMap = windows[w].ToDictionary(c => c.N);
                foreach (var ai in answer.Tags ?? new List<AILiteraryTag>())
                {
                    var built = _AILiteraryBuild(lc, ai, windowMap, windows.Count == 1, out string error);
                    if (built == null)
                    {
                        lc.Rejected++;
                        run.Log.AppendLine($"{tag}: rejected - {error}");
                        continue;
                    }
                    string key = $"{built.CoupletIndex}|{built.DeviceId}|" + _AILiteraryPartsSignature(_ReadParts(built.PartsJson));
                    if (!known.Add(key))
                        continue;
                    newTags.Add(built);
                    run.Log.AppendLine($"{tag}: {lc.Devices[built.DeviceId].Name} @ {built.CoupletIndex}{(built.Uncertain ? " (uncertain)" : "")} - {_AILiteraryDescribe(lc, built, windowMap)}");
                }
            }

            if (newTags.Count > 0 && !run.DryRun)
            {
                ctx.GanjoorPoemCorrections.Add(new GanjoorPoemCorrection()
                {
                    PoemId = poemId,
                    UserId = run.UserId,
                    OriginalTitle = poemTitle,
                    Note = $"آرایه‌های ادبی پیشنهادی هوش مصنوعی ({run.Model})",
                    Date = DateTime.Now,
                    Result = CorrectionReviewResult.NotReviewed,
                    Reviewed = false,
                    AffectedThePoem = false,
                    HideMyName = false,
                    LiteraryTags = newTags,
                });
                await ctx.SaveChangesAsync();
            }
            run.TagsCreated += newTags.Count;
            if (newTags.Count == 0)
                run.Log.AppendLine($"{tag}: no new literary tags");
        }

        /// <summary>
        /// suggest literary devices for the poems of a book using AI; everything is registered as an unreviewed
        /// poem correction of the configured user, so it passes the usual moderation flow
        /// </summary>
        /// <param name="masterCatId">book category id; 0 = all poems</param>
        /// <param name="startPoemId">first poem id to process (poems are processed in id order); the resume point wins when it is greater</param>
        /// <param name="count">0 = all</param>
        /// <param name="dryRun">only report in the job log, write nothing (the resume point is neither reset nor advanced)</param>
        /// <param name="restart">false: resume after the last processed poem of the previous run (same masterCatId); true: start from the beginning</param>
        public void OpenAIStartSuggestingPoemLiteraryTags(int masterCatId, int startPoemId, int count, bool dryRun, bool restart = false)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartSuggestingPoemLiteraryTags - cat: {masterCatId} - from poem: {startPoemId} - count: {count} - dryRun: {dryRun} - restart: {restart}", "Open AI initialization")).Result;
                      var run = new AIPeopleRun() { Context = context, DryRun = dryRun, MasterCatId = masterCatId };
                      var lc = new AILiteraryContext() { Run = run };
                      try
                      {
                          run.Model = Configuration["OpenAILiteraryModel"];
                          if (string.IsNullOrWhiteSpace(run.Model))
                              run.Model = Configuration["OpenAIPeopleModel"];
                          if (string.IsNullOrWhiteSpace(run.Model))
                              run.Model = Configuration["OpenAITitleModel"];
                          if (string.IsNullOrWhiteSpace(run.Model))
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "OpenAILiteraryModel (or OpenAIPeopleModel / OpenAITitleModel) is not set in configuration");
                              return;
                          }
                          if (!Guid.TryParse(_AILiteraryUserIdText(), out run.UserId))
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "OpenAILiteraryUserId (or OpenAIPeopleUserId / OpenAITitleUserId) is not set (or invalid) in configuration");
                              return;
                          }

                          run.Http = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMinutes(5) };
                          run.Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Configuration["OpenAIAPIKey"]);
                          run.ChatUrl = (Configuration["OpenAIBaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/') + "/chat/completions";

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          var devices = await context.LiteraryDevices.AsNoTracking().Include(d => d.Slots).Where(d => d.Status == LiteraryEntityStatus.Approved).ToListAsync();
                          if (devices.Count == 0)
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "no approved literary device");
                              return;
                          }
                          lc.Devices = devices.ToDictionary(d => d.Id);
                          lc.Catalogue = _AILiteraryCatalogue(devices);
                          foreach (var concept in await context.LiteraryConcepts.AsNoTracking().Where(c => c.Status == LiteraryEntityStatus.Approved).Select(c => new { c.Id, c.Name, c.Aliases }).ToListAsync())
                          {
                              lc.ConceptIds[_NormalizeConceptName(concept.Name)] = concept.Id;
                              foreach (var alias in (concept.Aliases ?? "").Split('|').Select(a => _NormalizeConceptName(a)).Where(a => a.Length > 0))
                                  if (!lc.ConceptIds.ContainsKey(alias))
                                      lc.ConceptIds[alias] = concept.Id;
                          }

                          bool wholeSet = masterCatId <= 0;
                          var catIdList = wholeSet ? new List<int>() : await _AIGetCategoryTreeIdsAsync(context, masterCatId);

                          string checkpointName = _AICheckpointNameFor("PoemLiterary", masterCatId);
                          if (restart && !dryRun)
                              await _AISetCheckpointAsync(context, checkpointName, 0);
                          int lastSafeId = restart ? 0 : await _AIGetCheckpointAsync(context, checkpointName);
                          int afterId = Math.Max(startPoemId - 1, lastSafeId);

                          var poemsQuery = context.GanjoorPoems.AsNoTracking().Where(p => p.Id > afterId);
                          if (!wholeSet)
                              poemsQuery = poemsQuery.Where(p => catIdList.Contains(p.CatId));
                          var orderedPoemsQuery = poemsQuery.OrderBy(p => p.Id).Select(p => new { p.Id, p.Title });
                          var poems = count > 0 ? await orderedPoemsQuery.Take(count).ToListAsync() : await orderedPoemsQuery.ToListAsync();

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poem count = {poems.Count}");

                          int consecutiveFailures = 0, sinceSave = 0;
                          bool failedSeen = false, aborted = false;
                          for (int i = 0; i < poems.Count; i++)
                          {
                              var poem = poems[i];
                              bool poemFailed = false;
                              await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * i / Math.Max(1, poems.Count)),
                                  $"{i + 1} از {poems.Count} - {poem.Id} - {poem.Title} (برچسب: {run.TagsCreated}، ردشده: {lc.Rejected})");
                              try
                              {
                                  int failedBefore = run.PoemsFailed, skippedBefore = run.PoemsSkipped;
                                  await _AILiteraryProcessPoemAsync(lc, poem.Id, poem.Title);
                                  if (run.PoemsFailed == failedBefore && run.PoemsSkipped == skippedBefore)
                                      run.PoemsDone++;
                                  poemFailed = run.PoemsFailed > failedBefore;
                              }
                              catch (Exception exp)
                              {
                                  run.PoemsFailed++;
                                  poemFailed = true;
                                  run.Log.AppendLine($"[{poem.Id}] {poem.Title}: ERROR {_AIShort(exp.Message, 300)}");
                                  context.ChangeTracker.Clear();
                              }

                              if (poemFailed)
                              {
                                  failedSeen = true; //the resume point stays before this poem, so it is retried on the next run
                                  if (++consecutiveFailures >= AIMaxConsecutiveFailures)
                                  {
                                      aborted = true;
                                      break;
                                  }
                              }
                              else
                              {
                                  consecutiveFailures = 0;
                                  if (!failedSeen && !dryRun)
                                  {
                                      lastSafeId = poem.Id;
                                      if (++sinceSave >= 10)
                                      {
                                          sinceSave = 0;
                                          await _AISetCheckpointAsync(context, checkpointName, lastSafeId);
                                      }
                                  }
                              }
                          }

                          if (!dryRun && lastSafeId > 0)
                              await _AISetCheckpointAsync(context, checkpointName, lastSafeId);

                          string summary =
                              $"{(dryRun ? "DRY RUN - " : "")}{(aborted ? $"STOPPED after {AIMaxConsecutiveFailures} consecutive failures - " : "")}last processed poem: {lastSafeId}; poems: done {run.PoemsDone}, skipped {run.PoemsSkipped}, failed {run.PoemsFailed}; " +
                              $"literary tags: {run.TagsCreated}; rejected answers: {lc.Rejected}";
                          string logText = run.Log.ToString();
                          if (logText.Length > 30000)
                              logText = logText.Substring(logText.Length - 30000);
                          if (aborted)
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, summary + Environment.NewLine + logText, false, run.LastAIError);
                          else
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, summary + Environment.NewLine + logText, true);
                      }
                      catch (Exception exp)
                      {
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString() + Environment.NewLine + run.Log.ToString());
                      }
                  }
              });
        }

        /// <summary>
        /// the user the AI literary suggestions are registered for
        /// </summary>
        private string _AILiteraryUserIdText()
        {
            string text = Configuration["OpenAILiteraryUserId"];
            if (string.IsNullOrWhiteSpace(text))
                text = Configuration["OpenAIPeopleUserId"];
            if (string.IsNullOrWhiteSpace(text))
                text = Configuration["OpenAITitleUserId"];
            return text;
        }

        /// <summary>
        /// true if tags approved from a correction of this user are produced by AI
        /// </summary>
        private bool _IsAILiteraryUser(Guid userId)
        {
            return Guid.TryParse(_AILiteraryUserIdText(), out Guid aiUserId) && aiUserId == userId;
        }
    }
}
