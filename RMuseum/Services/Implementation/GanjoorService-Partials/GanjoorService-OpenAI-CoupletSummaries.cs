using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using RSecurityBackend.Services.Implementation;
using System.Collections.Generic;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using RMuseum.Models.Artifact;
using System.Security.Policy;
using Azure;
using System.Diagnostics;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        private const string AISummaryPrefix = "هوش مصنوعی:";

        private static bool _IsAIGeneratedSummary(string summary)
        {
            return !string.IsNullOrEmpty(summary) && summary.TrimStart().StartsWith(AISummaryPrefix);
        }

        /// <summary>
        /// should this summary be (re)generated? empty ones always, AI generated ones only when regenerateAI is true
        /// </summary>
        private static bool _NeedsAISummary(string summary, bool regenerateAI)
        {
            return string.IsNullOrEmpty(summary) || (regenerateAI && _IsAIGeneratedSummary(summary));
        }

        /// <summary>
        /// all category ids under (and including) a root category
        /// </summary>
        private static async Task<List<int>> _AIGetCategoryTreeIdsAsync(RMuseumDbContext context, int rootCatId)
        {
            var allCats = await context.GanjoorCategories.AsNoTracking().Select(c => new { c.Id, c.ParentId }).ToListAsync();
            var ids = new HashSet<int> { rootCatId };
            bool added = true;
            while (added)
            {
                added = false;
                foreach (var c in allCats)
                {
                    if (c.ParentId != null && ids.Contains(c.ParentId.Value) && ids.Add(c.Id))
                        added = true;
                }
            }
            return ids.ToList();
        }

        /// <summary>
        /// cleans the model's answer: removes leading labels/quotes, rejects empty, suspicious (the known
        /// "اکتبر" garbage), mostly Latin or overly long answers. Returns null when the answer must not be used.
        /// </summary>
        private static string _CleanAISummary(string raw, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            string s = raw.Trim().Trim('"', '«', '»', '*', '#', ' ');
            foreach (var label in new[] { "معنی بیت:", "معنی بیت :", "معنی:", "مفهوم بیت:", "مفهوم:", "خلاصه:", "خلاصهٔ شعر:" })
            {
                if (s.StartsWith(label))
                    s = s.Substring(label.Length).Trim();
            }
            s = s.Replace("ي", "ی").Replace("ك", "ک");
            if (s.Length == 0 || s.Length > maxLength || s.Contains("اکتبر"))
                return null;
            int latin = s.Count(ch => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'));
            int letters = s.Count(ch => char.IsLetter(ch));
            if (letters > 0 && latin * 100 / letters > 30)
                return null;
            return s;
        }

        private const string AICoupletMeaningInstructions =
@"تو یک ادبیات‌دان و ویراستار فارسی هستی و معنی ابیات شعر کهن فارسی را برای خوانندهٔ امروزی می‌نویسی.

قوانین:
- فقط معنی «بیتی که باید معنی شود» را بنویس. بیت پیشین و پسین (اگر آمده باشد) فقط برای فهم زمینه است و نباید معنی آن‌ها را بنویسی.
- مفهوم را به نثر روان و امروزی بنویس، نه ترجمهٔ واژه‌به‌واژه و نه شرح مفصل. معمولاً یک تا سه جمله کافی است.
- نام‌ها (کسان، جاها) را همان‌گونه که در بیت آمده بنویس و اگر برای فهم بیت لازم است، با یک عبارت کوتاه بگو او کیست (فقط اگر از خود شعر یا زمینه روشن است).
- ضمیرها و اشاره‌ها را با توجه به زمینه روشن کن (مثلاً «او» چه کسی است).
- اگر واژه یا تعبیر کهنه‌ای هست، معنایش را در متن بیاور، بدون اینکه جداگانه واژه‌نامه بنویسی.
- مصرع‌ها را تکرار نکن، هیچ مقدمه‌ای مانند «معنی بیت:» یا «این بیت می‌گوید» ننویس، و ارزش‌گذاری یا نقد ادبی نکن.
- اگر متن بیت آشکارا مخدوش یا مبهم است، فقط آنچه با اطمینان از آن برمی‌آید را بنویس و حدس نزن.
- فقط به فارسی بنویس.";

        private const string AIParagraphMeaningInstructions =
@"تو یک ادبیات‌دان و ویراستار فارسی هستی و متن‌های کهن فارسی را برای خوانندهٔ امروزی به فارسی روان بازنویسی می‌کنی.

قوانین:
- فقط «پاراگرافی که باید بازنویسی شود» را به فارسی روان و امروزی بازگو کن؛ متن پیشین و پسین فقط برای فهم زمینه است.
- مفهوم را کامل و دقیق برسان، بدون افزودن اطلاعات بیرونی، بدون مقدمه و بدون ارزش‌گذاری.
- نام‌ها را همان‌گونه که آمده بنویس و ضمیرها را با توجه به زمینه روشن کن.
- فقط به فارسی بنویس.";

        private const string AIPoemSummaryInstructions =
@"تو یک ادبیات‌دان و ویراستار فارسی هستی و برای سایت شعر فارسی، خلاصهٔ شعرها را می‌نویسی.

قوانین:
- شعر را در سه تا شش جمله (برای شعر بسیار کوتاه دو تا سه جمله) به نثر روان و امروزی خلاصه کن.
- اگر شعر روایی است (داستان، حکایت)، رویدادهای اصلی را به ترتیب و با نام شخصیت‌های مهم بگو. اگر شعر غنایی، عرفانی، اخلاقی یا ستایشی است، مضمون اصلی و حال‌وهوای آن را بگو، نه اینکه بیت‌به‌بیت بازگویی کنی.
- فقط بر پایهٔ خود متن بنویس و اطلاعات بیرونی نیفزای.
- هیچ مصرعی را نقل نکن، هیچ مقدمه‌ای مانند «خلاصه:» ننویس، و ارزش‌گذاری یا نقد ادبی نکن.
- فقط به فارسی بنویس.";

        private const int AIMaxConsecutiveFailures = 5;

        private static string _AICheckpointName(string kind, int? masterCatId, bool regenerateAI)
        {
            return $"AI{kind}SummariesLastId-cat{masterCatId ?? 0}-regen{(regenerateAI ? 1 : 0)}";
        }

        /// <summary>
        /// last processed id of a resumable AI job (stored in the generic options table), 0 if none
        /// </summary>
        private static async Task<int> _AIGetCheckpointAsync(RMuseumDbContext context, string name)
        {
            var value = await context.Options.AsNoTracking().Where(o => o.Name == name && o.RAppUserId == null).Select(o => o.Value).SingleOrDefaultAsync();
            return int.TryParse(value, out int id) ? id : 0;
        }

        private static async Task _AISetCheckpointAsync(RMuseumDbContext context, string name, int id)
        {
            var option = await context.Options.Where(o => o.Name == name && o.RAppUserId == null).SingleOrDefaultAsync();
            if (option == null)
            {
                context.Options.Add(new RSecurityBackend.Models.Generic.Db.RGenericOption { Name = name, Value = id.ToString(), RAppUserId = null });
            }
            else
            {
                option.Value = id.ToString();
                context.Options.Update(option);
            }
            await context.SaveChangesAsync();
        }

        private const int AIMaxRememberedFailures = 1000;

        /// <summary>
        /// ids that failed (an exception, not a rejected answer) in earlier runs of a resumable AI job; they are retried
        /// by the next run while the checkpoint itself keeps moving forward
        /// </summary>
        private static async Task<HashSet<int>> _AIGetFailedIdsAsync(RMuseumDbContext context, string name)
        {
            var value = await context.Options.AsNoTracking().Where(o => o.Name == name + "-failed" && o.RAppUserId == null).Select(o => o.Value).SingleOrDefaultAsync();
            var res = new HashSet<int>();
            foreach (var part in (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part, out int id))
                    res.Add(id);
            return res;
        }

        private static async Task _AISetFailedIdsAsync(RMuseumDbContext context, string name, HashSet<int> ids)
        {
            string value = string.Join(",", ids.OrderBy(i => i).Take(AIMaxRememberedFailures));
            var option = await context.Options.Where(o => o.Name == name + "-failed" && o.RAppUserId == null).SingleOrDefaultAsync();
            if (option == null)
            {
                context.Options.Add(new RSecurityBackend.Models.Generic.Db.RGenericOption { Name = name + "-failed", Value = value, RAppUserId = null });
            }
            else
            {
                option.Value = value;
                context.Options.Update(option);
            }
            await context.SaveChangesAsync();
        }

        private class AIPoemCoupletCache
        {
            public int PoemId;
            public string FullTitle;
            public List<(int Index, string Text, bool IsParagraph)> Couplets = new List<(int, string, bool)>();
        }

        /// <summary>
        /// fill (or regenerate) couplet summaries using open ai
        /// </summary>
        /// <param name="startFrom">id of the first poem to process (poems are processed in id order; the resume point wins when it is greater)</param>
        /// <param name="count">number of poems to process, 0 = all</param>
        /// <param name="masterCatId">only couplets of poems under this category (0 or null = the whole set)</param>
        /// <param name="regenerateAI">false: only couplets without any summary; true: also couplets whose summary starts with «هوش مصنوعی:» (human written summaries are never touched)</param>
        /// <param name="restart">false: resume after the last processed poem of the previous run (same category and regenerateAI); true: start from the beginning</param>
        public void OpenAIStartFillingCoupletSummaries(int startFrom, int count, int? masterCatId, bool regenerateAI, bool restart = false)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIFillCoupletSummaries - start: {startFrom} - count: {count} - cat: {masterCatId ?? 0} - regenerateAI: {regenerateAI} - restart: {restart}", "Open AI initialization")).Result;
                      try
                      {
                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          }, new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMinutes(10) });
                          string model = OpenAIModel;

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          List<int> catIds = null;
                          if (masterCatId != null && masterCatId.Value > 0)
                              catIds = await _AIGetCategoryTreeIdsAsync(context, masterCatId.Value);

                          var versesQuery = context.GanjoorVerses.AsNoTracking()
                              .Where(v =>
                                      (
                                      v.VersePosition == VersePosition.Right
                                      ||
                                      v.VersePosition == VersePosition.CenteredVerse1
                                      ||
                                      v.VersePosition == VersePosition.Paragraph
                                      )
                                      &&
                                      v.CoupletIndex != null
                                      &&
                                      (
                                      string.IsNullOrEmpty(v.CoupletSummary)
                                      ||
                                      (regenerateAI && v.CoupletSummary.StartsWith(AISummaryPrefix))
                                      )
                              );
                          //the resume point is a POEM id (all poems up to it are done); a poem interrupted in the middle is processed again
                          //from its first couplet, which is harmless: couplets that already have a summary are skipped unless regenerateAI is set
                          string checkpointName = _AICheckpointName("CoupletPoem", masterCatId, regenerateAI);
                          if (restart)
                              await _AISetCheckpointAsync(context, checkpointName, 0);
                          int lastSafeId = await _AIGetCheckpointAsync(context, checkpointName);
                          var failedIds = await _AIGetFailedIdsAsync(context, checkpointName); //poem ids
                          if (restart)
                              failedIds.Clear();
                          var failedIdList = failedIds.ToList();
                          int fromPoemId = Math.Max(startFrom, 1);
                          versesQuery = versesQuery.Where(v => (v.PoemId > lastSafeId && v.PoemId >= fromPoemId) || failedIdList.Contains(v.PoemId));
                          if (catIds != null)
                              versesQuery = versesQuery.Where(v => context.GanjoorPoems.Any(p => p.Id == v.PoemId && catIds.Contains(p.CatId)));
                          if (count > 0)
                          {
                              var runPoemIds = await versesQuery.Select(v => v.PoemId).Distinct().OrderBy(id => id).Take(count).ToListAsync();
                              int maxPoemId = runPoemIds.Count == 0 ? 0 : runPoemIds.Max();
                              versesQuery = versesQuery.Where(v => v.PoemId <= maxPoemId);
                          }

                          var verses = await versesQuery.OrderBy(v => v.PoemId).ThenBy(v => v.Id)
                              .Select(v => new { v.Id, v.PoemId, v.CoupletIndex, v.VersePosition }).ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Verse count = {verses.Count}");

                          AIPoemCoupletCache cache = null;
                          int done = 0, rejected = 0, consecutiveFailures = 0, sinceSave = 0;
                          bool aborted = false;
                          int currentPoemId = 0;
                          bool currentPoemFailed = false;
                          async Task FinishPoemAsync(int poemId, bool failedHere)
                          {
                              if (failedHere)
                                  failedIds.Add(poemId);
                              else
                                  failedIds.Remove(poemId);
                              lastSafeId = Math.Max(lastSafeId, poemId);
                              if (++sinceSave >= 10)
                              {
                                  sinceSave = 0;
                                  await _AISetCheckpointAsync(context, checkpointName, lastSafeId);
                                  await _AISetFailedIdsAsync(context, checkpointName, failedIds);
                              }
                          }
                          for (var i = 0; i < verses.Count; i++)
                          {
                              var verse = verses[i];
                              if (verse.PoemId != currentPoemId)
                              {
                                  if (currentPoemId != 0)
                                      await FinishPoemAsync(currentPoemId, currentPoemFailed);
                                  currentPoemId = verse.PoemId;
                                  currentPoemFailed = false;
                              }
                              bool itemFailed = false;
                              try
                              {
                                  var editableVerse = await context.GanjoorVerses.Where(v => v.Id == verse.Id).SingleAsync();
                                  if (!_NeedsAISummary(editableVerse.CoupletSummary, regenerateAI))
                                      continue;

                                  if (cache == null || cache.PoemId != verse.PoemId)
                                  {
                                      var poemInfo = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == verse.PoemId).Select(p => new { p.FullTitle }).SingleOrDefaultAsync();
                                      var poemVerses = await context.GanjoorVerses.AsNoTracking()
                                          .Where(v => v.PoemId == verse.PoemId && v.CoupletIndex != null)
                                          .OrderBy(v => v.VOrder)
                                          .Select(v => new { v.CoupletIndex, v.Text, v.VersePosition })
                                          .ToListAsync();
                                      cache = new AIPoemCoupletCache() { PoemId = verse.PoemId, FullTitle = poemInfo?.FullTitle };
                                      foreach (var g in poemVerses.GroupBy(v => v.CoupletIndex.Value).OrderBy(g => g.Key))
                                      {
                                          cache.Couplets.Add((g.Key,
                                              string.Join(" ", g.Select(x => (x.Text ?? "").Trim())).Trim(),
                                              g.Any(x => x.VersePosition == VersePosition.Paragraph)));
                                      }
                                  }

                                  int pos = cache.Couplets.FindIndex(c => c.Index == verse.CoupletIndex.Value);
                                  if (pos < 0 || string.IsNullOrWhiteSpace(cache.Couplets[pos].Text))
                                      continue;
                                  var target = cache.Couplets[pos];
                                  string previous = pos > 0 ? cache.Couplets[pos - 1].Text : null;
                                  string next = pos < cache.Couplets.Count - 1 ? cache.Couplets[pos + 1].Text : null;

                                  await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * i / Math.Max(1, verses.Count)), $"{i + 1} از {verses.Count} (ایجاد: {done}، ردشده: {rejected}) - {target.Text}");

                                  var user = new StringBuilder();
                                  if (!string.IsNullOrWhiteSpace(cache.FullTitle))
                                      user.AppendLine($"منبع: {cache.FullTitle}");
                                  if (!string.IsNullOrWhiteSpace(previous))
                                      user.AppendLine($"بیت پیشین (فقط برای زمینه): {previous}");
                                  if (!string.IsNullOrWhiteSpace(next))
                                      user.AppendLine($"بیت پسین (فقط برای زمینه): {next}");
                                  user.AppendLine(target.IsParagraph ? "پاراگرافی که باید بازنویسی شود:" : "بیتی که باید معنی شود:");
                                  user.AppendLine(target.Text);

                                  var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                  {
                                      Messages = new List<ChatMessage>
                                      {
                                          ChatMessage.FromSystem((target.IsParagraph ? AIParagraphMeaningInstructions : AICoupletMeaningInstructions)),
                                          ChatMessage.FromUser(user.ToString()),
                                      },
                                      Model = model,
                                  });
                                  if (!completionResult.Successful)
                                      throw new InvalidOperationException(completionResult.Error?.Message ?? "Open AI request failed");
                                  consecutiveFailures = 0;
                                  string summary = _CleanAISummary(completionResult.Choices.First().Message.Content, target.IsParagraph ? 4000 : 1200);
                                  if (summary == null)
                                  {
                                      rejected++; //an existing (AI) summary is kept as it is
                                      continue;
                                  }

                                  editableVerse.CoupletSummary = AISummaryPrefix + " " + summary;
                                  context.Update(editableVerse);
                                  await context.SaveChangesAsync();
                                  done++;
                              }
                              catch (Exception exp)
                              {
                                  rejected++;
                                  itemFailed = true; //remembered and retried by the next run; the checkpoint itself keeps moving
                                  context.ChangeTracker.Clear();
                                  if (++consecutiveFailures >= AIMaxConsecutiveFailures)
                                  {
                                      aborted = true;
                                      await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * i / Math.Max(1, verses.Count)), $"متوقف شد پس از {AIMaxConsecutiveFailures} خطای پیاپی در بیت {verse.Id} (شعر {verse.PoemId})", false, exp.ToString());
                                  }
                              }
                              finally
                              {
                                  if (itemFailed)
                                      currentPoemFailed = true;
                              }
                              if (aborted)
                                  break;
                          }
                          if (currentPoemId != 0)
                          {
                              if (aborted)
                              {
                                  if (currentPoemFailed)
                                      failedIds.Add(currentPoemId); //the poem itself is processed again by the next run
                              }
                              else
                                  await FinishPoemAsync(currentPoemId, currentPoemFailed);
                          }

                          if (lastSafeId > 0)
                              await _AISetCheckpointAsync(context, checkpointName, lastSafeId);
                          await _AISetFailedIdsAsync(context, checkpointName, failedIds);
                          if (!aborted)
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, $"ایجاد: {done}، ردشده یا ناموفق: {rejected} (شعرهای در انتظار تلاش دوباره: {failedIds.Count}) - آخرین شعر پردازش‌شده: {lastSafeId}", true);
                      }
                      catch (Exception exp)
                      {
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                      }

                  }
              });

        }

        /// <summary>
        /// fill (or regenerate) poem summaries using open ai
        /// </summary>
        /// <param name="startFrom">id of the first poem to process (poems are processed in id order; the resume point wins when it is greater)</param>
        /// <param name="count">number of poems to process, 0 = all</param>
        /// <param name="masterCatId">only poems under this category (0 or null = the whole set)</param>
        /// <param name="regenerateAI">false: only poems without any summary; true: also poems whose summary starts with «هوش مصنوعی:» (human written summaries are never touched)</param>
        /// <param name="restart">false: resume after the last processed poem of the previous run (same category and regenerateAI); true: start from the beginning</param>
        public void OpenAIStartFillingPoemSummaries(int startFrom, int count, int? masterCatId, bool regenerateAI, bool restart = false)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartFillingPoemSummaries - start: {startFrom} - count: {count} - cat: {masterCatId ?? 0} - regenerateAI: {regenerateAI} - restart: {restart}", "Open AI initialization")).Result;
                      try
                      {
                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          }, new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMinutes(10) });
                          string model = OpenAIModel;

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          List<int> catIds = null;
                          if (masterCatId != null && masterCatId.Value > 0)
                              catIds = await _AIGetCategoryTreeIdsAsync(context, masterCatId.Value);

                          var poemsQuery = context.GanjoorPoems.AsNoTracking()
                              .Where(p => string.IsNullOrEmpty(p.PoemSummary) || (regenerateAI && p.PoemSummary.StartsWith(AISummaryPrefix)));
                          string checkpointName = _AICheckpointName("Poem", masterCatId, regenerateAI);
                          if (restart)
                              await _AISetCheckpointAsync(context, checkpointName, 0);
                          int lastSafeId = await _AIGetCheckpointAsync(context, checkpointName);
                          var failedIds = await _AIGetFailedIdsAsync(context, checkpointName);
                          if (restart)
                              failedIds.Clear();
                          var failedIdList = failedIds.ToList();
                          poemsQuery = poemsQuery.Where(p => (p.Id > lastSafeId && p.Id >= startFrom) || failedIdList.Contains(p.Id));
                          if (catIds != null)
                              poemsQuery = poemsQuery.Where(p => catIds.Contains(p.CatId));

                          var orderedQuery = poemsQuery.OrderBy(p => p.Id).Select(p => new { p.Id, p.FullTitle });
                          var poems = count > 0 ? await orderedQuery.Take(count).ToListAsync() : await orderedQuery.ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poems count = {poems.Count}");

                          int done = 0, rejected = 0, consecutiveFailures = 0, sinceSave = 0;
                          bool aborted = false;
                          for (var i = 0; i < poems.Count; i++)
                          {
                              var poem = poems[i];
                              bool itemFailed = false;
                              try
                              {
                                  var editablePoem = await context.GanjoorPoems.Where(p => p.Id == poem.Id).SingleAsync();
                                  if (string.IsNullOrWhiteSpace(editablePoem.PlainText) || !_NeedsAISummary(editablePoem.PoemSummary, regenerateAI))
                                      continue;

                                  await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * i / Math.Max(1, poems.Count)), $"{i + 1} از {poems.Count} (ایجاد: {done}، ردشده: {rejected}) - {poem.FullTitle}");

                                  string text = editablePoem.PlainText;
                                  if (text.Length > 24000)
                                      text = text.Substring(0, 24000);
                                  var user = new StringBuilder();
                                  if (!string.IsNullOrWhiteSpace(editablePoem.FullTitle))
                                      user.AppendLine($"منبع: {editablePoem.FullTitle}");
                                  user.AppendLine("شعری که باید خلاصه شود:");
                                  user.AppendLine(text);

                                  var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                  {
                                      Messages = new List<ChatMessage>
                                      {
                                          ChatMessage.FromSystem(AIPoemSummaryInstructions),
                                          ChatMessage.FromUser(user.ToString()),
                                      },
                                      Model = model,
                                  });
                                  if (!completionResult.Successful)
                                      throw new InvalidOperationException(completionResult.Error?.Message ?? "Open AI request failed");
                                  consecutiveFailures = 0;
                                  string summary = _CleanAISummary(completionResult.Choices.First().Message.Content, 3000);
                                  if (summary == null)
                                  {
                                      rejected++; //an existing (AI) summary is kept as it is
                                      continue;
                                  }

                                  editablePoem.PoemSummary = AISummaryPrefix + " " + summary;
                                  context.Update(editablePoem);
                                  await context.SaveChangesAsync();
                                  done++;
                              }
                              catch (Exception exp)
                              {
                                  rejected++;
                                  itemFailed = true; //remembered and retried by the next run; the checkpoint itself keeps moving
                                  context.ChangeTracker.Clear();
                                  if (++consecutiveFailures >= AIMaxConsecutiveFailures)
                                  {
                                      aborted = true;
                                      await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * i / Math.Max(1, poems.Count)), $"متوقف شد پس از {AIMaxConsecutiveFailures} خطای پیاپی در شعر {poem.Id}", false, exp.ToString());
                                  }
                              }
                              finally
                              {
                                  if (itemFailed)
                                      failedIds.Add(poem.Id);
                                  else
                                      failedIds.Remove(poem.Id);
                                  lastSafeId = Math.Max(lastSafeId, poem.Id);
                                  if (++sinceSave >= 10)
                                  {
                                      sinceSave = 0;
                                      await _AISetCheckpointAsync(context, checkpointName, lastSafeId);
                                      await _AISetFailedIdsAsync(context, checkpointName, failedIds);
                                  }
                              }
                              if (aborted)
                                  break;
                          }

                          if (lastSafeId > 0)
                              await _AISetCheckpointAsync(context, checkpointName, lastSafeId);
                          await _AISetFailedIdsAsync(context, checkpointName, failedIds);
                          if (!aborted)
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, $"ایجاد: {done}، ردشده یا ناموفق: {rejected} (در انتظار تلاش دوباره: {failedIds.Count}) - آخرین شعر پردازش‌شده: {lastSafeId}", true);
                      }
                      catch (Exception exp)
                      {
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                      }

                  }
              });

        }

        /// <summary>
        /// geo tag poems using AI
        /// </summary>
        /// <param name="startFrom">id of the first poem to process (the resume point wins when it is greater)</param>
        /// <param name="count">0 = all</param>
        /// <param name="restart">false: resume after the last processed poem of the previous run; true: start from the beginning</param>
        public void OpenAIStartFillingGeoLocations(int startFrom, int count, bool restart = false)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartFillingGeoLocations - start: {startFrom} - count: {count} - restart: {restart}", "Open AI initialization")).Result;
                      try
                      {

                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          }, new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMinutes(10) });

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          string checkpointName = _AICheckpointNameFor("PoemGeo", 0);
                          if (restart)
                              await _AISetCheckpointAsync(context, checkpointName, 0);
                          int lastSafeId = await _AIGetCheckpointAsync(context, checkpointName);

                          var poemsQuery = context.GanjoorPoems.AsNoTracking().Where(p => p.Id > lastSafeId && p.Id >= startFrom).OrderBy(p => p.Id);
                          var poems = count > 0 ? await poemsQuery.Take(count).ToListAsync() : await poemsQuery.ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poems count = {poems.Count}");

                          int consecutiveFailures = 0, sinceSave = 0;
                          bool failedSeen = false, aborted = false;
                          for (var i = 0; i < poems.Count; i++)
                          {
                              var poem = poems[i];
                              try
                              {

                              if (true == await context.PoemGeoDateTags.AsNoTracking().Where(p => p.PoemId == poem.Id && p.MachineGenerated == false).AnyAsync())
                              {
                                  continue;
                              }


                              if (!string.IsNullOrEmpty(poem.PlainText))
                              {

                                  await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle}");
                                  string command = "نام شهرها یا مکانهای جغرافیایی که در این متن آمده است را بده.اگر هیچ شهر یا مکان جغرافیایی در متن نیست جواب خالی برگردان." + Environment.NewLine +
                                    "اگر نام مکان واقعاً اشاره به آن مکان نیست مثلاً در مورد مردی از اهالی آن شهر صحبت می‌کند یا در این متن آن کلمه معنی اسم مکان مورد نظر را نمی‌دهد آن را حذف کن." + Environment.NewLine +
                                    "اگر هست به ازای هر مورد در یک خط ابتدا نام مکان، سپس یک سمی کالن، سپس عرض جغرافیایی سپس یک سمی کالن سپس طول جغرافیایی را بده." + Environment.NewLine +
                                    "نمونهٔ جواب:" + Environment.NewLine +
                                    "شیراز;29.5926;52.5836" + Environment.NewLine +
                                    "برای پایان خط از استاندارد ویندوز استفاده کن.";
                                  var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                  {
                                      Messages = new List<ChatMessage>
                                        {
                                            ChatMessage.FromSystem(command
                                                    +
                                                    Environment.NewLine
                                                    +
                                                    poem.Title +
                                                    Environment.NewLine +
                                                    poem.PlainText
                                                    ),
                                        },
                                      Model = OpenAIModel,
                                  });
                                  if (!completionResult.Successful)
                                      throw new InvalidOperationException(completionResult.Error?.Message ?? "Open AI request failed");
                                  consecutiveFailures = 0;
                                  {
                                      string aiResponse = completionResult.Choices.First().Message.Content;
                                      if (aiResponse.Contains("اکتبر"))
                                      {
                                          aiResponse = "";
                                      }
                                      if (!string.IsNullOrEmpty(aiResponse))
                                      {
                                          var lines = aiResponse.Split(new[] { '\r', '\n' });
                                          foreach (var line in lines)
                                          {
                                              var parts = line.Split(';');
                                              if (parts.Length == 3)
                                              {
                                                  var locationName = parts[0];
                                                  if (!double.TryParse(parts[1], out double latitude))
                                                      continue;
                                                  if (!double.TryParse(parts[2], out double longitude))
                                                      continue;
                                                  GanjoorGeoLocation geoLocation = await context.GanjoorGeoLocations.AsNoTracking().Where(g => g.Name == locationName).FirstOrDefaultAsync();
                                                  if (geoLocation == null)
                                                  {
                                                      geoLocation = new GanjoorGeoLocation()
                                                      {
                                                          Name = locationName,
                                                          Latitude = latitude,
                                                          Longitude = longitude,
                                                          MachineGenerated = true,
                                                      };
                                                      context.Add(geoLocation);
                                                      await context.SaveChangesAsync();
                                                  }

                                                  if (true == await context.PoemGeoDateTags.AsNoTracking().Where(p => p.PoemId == poem.Id && p.LocationId == geoLocation.Id && p.CoupletIndex == 0).AnyAsync())
                                                  {
                                                      continue;
                                                  }

                                                  PoemGeoDateTag poemGeoDateTag = new PoemGeoDateTag()
                                                  {
                                                      PoemId = poem.Id,
                                                      CoupletIndex = 0,
                                                      LocationId = geoLocation.Id,
                                                      MachineGenerated = true,
                                                  };
                                                  context.Add(poemGeoDateTag);
                                                  await context.SaveChangesAsync();
                                              }


                                          }
                                      }
                                  }

                              }
                              }
                              catch (Exception exp)
                              {
                                  failedSeen = true; //the checkpoint stays before this poem, so it is retried on the next run
                                  context.ChangeTracker.Clear();
                                  if (++consecutiveFailures >= AIMaxConsecutiveFailures)
                                  {
                                      aborted = true;
                                      await jobProgressServiceEF.UpdateJob(job.Id, i, $"متوقف شد پس از {AIMaxConsecutiveFailures} خطای پیاپی در شعر {poem.Id}", false, exp.ToString());
                                  }
                              }
                              finally
                              {
                                  if (!failedSeen)
                                  {
                                      lastSafeId = poem.Id;
                                      if (++sinceSave >= 10)
                                      {
                                          sinceSave = 0;
                                          await _AISetCheckpointAsync(context, checkpointName, lastSafeId);
                                      }
                                  }
                              }
                              if (aborted)
                                  break;
                          }
                          if (lastSafeId > 0)
                              await _AISetCheckpointAsync(context, checkpointName, lastSafeId);
                          if (!aborted)
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, $"آخرین شعر پردازش‌شده: {lastSafeId}", true);
                      }
                      catch (Exception exp)
                      {
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                      }

                  }
              });

        }

    }
}