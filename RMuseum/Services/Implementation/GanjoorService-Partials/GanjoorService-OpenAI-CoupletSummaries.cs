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

        private class AIPoemCoupletCache
        {
            public int PoemId;
            public string FullTitle;
            public List<(int Index, string Text, bool IsParagraph)> Couplets = new List<(int, string, bool)>();
        }

        /// <summary>
        /// fill (or regenerate) couplet summaries using open ai
        /// </summary>
        /// <param name="startFrom">offset in the list of couplets to process (the list is ordered by verse id, so with regenerateAI it is stable and can be used to resume)</param>
        /// <param name="count">0 = all</param>
        /// <param name="masterCatId">only couplets of poems under this category (0 or null = the whole set)</param>
        /// <param name="regenerateAI">false: only couplets without any summary; true: also couplets whose summary starts with «هوش مصنوعی:» (human written summaries are never touched)</param>
        public void OpenAIStartFillingCoupletSummaries(int startFrom, int count, int? masterCatId, bool regenerateAI)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIFillCoupletSummaries - start: {startFrom} - count: {count} - cat: {masterCatId ?? 0} - regenerateAI: {regenerateAI}", "Open AI initialization")).Result;
                      try
                      {
                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          });
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
                          if (catIds != null)
                              versesQuery = versesQuery.Where(v => context.GanjoorPoems.Any(p => p.Id == v.PoemId && catIds.Contains(p.CatId)));

                          var orderedQuery = versesQuery.OrderBy(v => v.Id).Select(v => new { v.Id, v.PoemId, v.CoupletIndex, v.VersePosition });
                          var pagedQuery = startFrom > 0 ? orderedQuery.Skip(startFrom) : orderedQuery;
                          var verses = count > 0 ? await pagedQuery.Take(count).ToListAsync() : await pagedQuery.ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Verse count = {verses.Count}");

                          AIPoemCoupletCache cache = null;
                          int done = 0, rejected = 0;
                          for (var i = 0; i < verses.Count; i++)
                          {
                              var verse = verses[i];
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
                                  string summary = completionResult.Successful ? _CleanAISummary(completionResult.Choices.First().Message.Content, target.IsParagraph ? 4000 : 1200) : null;
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
                              catch (Exception)
                              {
                                  rejected++;
                                  context.ChangeTracker.Clear();
                              }
                          }

                          await jobProgressServiceEF.UpdateJob(job.Id, 100, $"ایجاد: {done}، ردشده یا ناموفق: {rejected}", true);
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
        /// <param name="startFrom">offset in the list of poems to process (ordered by poem id, so with regenerateAI it is stable and can be used to resume)</param>
        /// <param name="count">0 = all</param>
        /// <param name="masterCatId">only poems under this category (0 or null = the whole set)</param>
        /// <param name="regenerateAI">false: only poems without any summary; true: also poems whose summary starts with «هوش مصنوعی:» (human written summaries are never touched)</param>
        public void OpenAIStartFillingPoemSummaries(int startFrom, int count, int? masterCatId, bool regenerateAI)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartFillingPoemSummaries - start: {startFrom} - count: {count} - cat: {masterCatId ?? 0} - regenerateAI: {regenerateAI}", "Open AI initialization")).Result;
                      try
                      {
                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          });
                          string model = OpenAIModel;

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          List<int> catIds = null;
                          if (masterCatId != null && masterCatId.Value > 0)
                              catIds = await _AIGetCategoryTreeIdsAsync(context, masterCatId.Value);

                          var poemsQuery = context.GanjoorPoems.AsNoTracking()
                              .Where(p => string.IsNullOrEmpty(p.PoemSummary) || (regenerateAI && p.PoemSummary.StartsWith(AISummaryPrefix)));
                          if (catIds != null)
                              poemsQuery = poemsQuery.Where(p => catIds.Contains(p.CatId));

                          var orderedQuery = poemsQuery.OrderBy(p => p.Id).Select(p => new { p.Id, p.FullTitle });
                          var pagedQuery = startFrom > 0 ? orderedQuery.Skip(startFrom) : orderedQuery;
                          var poems = count > 0 ? await pagedQuery.Take(count).ToListAsync() : await pagedQuery.ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poems count = {poems.Count}");

                          int done = 0, rejected = 0;
                          for (var i = 0; i < poems.Count; i++)
                          {
                              var poem = poems[i];
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
                                  string summary = completionResult.Successful ? _CleanAISummary(completionResult.Choices.First().Message.Content, 3000) : null;
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
                              catch (Exception)
                              {
                                  rejected++;
                                  context.ChangeTracker.Clear();
                              }
                          }

                          await jobProgressServiceEF.UpdateJob(job.Id, 100, $"ایجاد: {done}، ردشده یا ناموفق: {rejected}", true);
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
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        public void OpenAIStartFillingGeoLocations(int startFrom, int count)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartFillingGeoLocations - start: {startFrom} - count: {count}", "Open AI initialization")).Result;
                      try
                      {

                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          });

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          var poems =
                            count == 0 ?
                            startFrom == 0 ?
                            await context.GanjoorPoems.AsNoTracking()
                                .ToListAsync()
                                :
                                await context.GanjoorPoems.AsNoTracking()
                                .Skip(startFrom)
                                .ToListAsync()
                                :
                            await context.GanjoorPoems.AsNoTracking()
                                .Skip(startFrom)
                                .Take(count)
                                .ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poems count = {poems.Count}");

                          for (var i = 0; i < poems.Count; i++)
                          {
                              var poem = poems[i];

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
                                  if (completionResult.Successful)
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
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
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