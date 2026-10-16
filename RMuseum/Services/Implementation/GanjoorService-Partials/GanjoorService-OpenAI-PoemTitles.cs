using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// AI model used by the summary/geo jobs (OpenAIModel in configuration, gpt-4o-mini if not set)
        /// </summary>
        private string OpenAIModel
        {
            get
            {
                string model = Configuration["OpenAIModel"];
                return string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model.Trim();
            }
        }

        private const string AITitlePromptHeader =
@"تو یک متخصص ادبیات حماسی فارسی و شاهنامه فردوسی هستی.

وظیفه‌ات این است که برای هر قطعه شعری که از شاهنامه به تو داده می‌شود، **فقط یک عنوان کوتاه، دقیق و ادبی** به زبان فارسی بنویسی.

قوانین عنوان:
- عنوان باید خلاصه‌ای فشرده و دقیق از رویدادهای اصلی همان قطعه باشد.
- سبک عنوان‌ها باید شبیه این نمونه‌ها باشد:
  - شب دیدار رستم و تهمینه، پیمان ازدواج و سپردن مهره
  - شکار رستم در مرز توران، دزدیده شدن رخش و رفتن او به سمنگان
  - دیباچهٔ داستان رستم و سهراب: تأمل در مرگ و تقدیر
  - خان هفتم رستم: کشتن دیو سپید، درمان چشم کاووس و گشودن مازندران

- فقط عنوان را بنویس. هیچ توضیح، مقدمه، شماره بخش یا متن اضافی ننویس.
- عنوان نباید خیلی طولانی باشد (معمولاً بین ۸ تا ۱۸ کلمه).

حالا این قطعه شعر را بخوان و فقط عنوان مناسب آن را بنویس:

";

        private static string _AICheckpointNameFor(string job, int masterCatId)
        {
            return $"AI{job}LastId-cat{masterCatId}";
        }

        private static readonly Regex AITitleBareSectionRegex = new Regex(@"^\s*بخش\s*[0-9۰-۹٠-٩]+\s*$", RegexOptions.Compiled);

        /// <summary>
        /// complete bare "بخش n" titles of poems under a category (e.g. Shahnameh) using AI and
        /// submit the results as ordinary (unreviewed) poem edit suggestions of the given user
        /// </summary>
        /// <param name="masterCatId">book category id (33 = Shahnameh); 0 = all poems</param>
        /// <param name="startFrom">id of the first poem to process (the resume point wins when it is greater)</param>
        /// <param name="count">0 = all</param>
        /// <param name="restart">false: resume after the last processed poem of the previous run (same masterCatId); true: start from the beginning</param>
        public void OpenAIStartSuggestingPoemTitles(int masterCatId, int startFrom, int count, bool restart = false)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartSuggestingPoemTitles - cat: {masterCatId} - start: {startFrom} - count: {count} - restart: {restart}", "Open AI initialization")).Result;
                      try
                      {
                          string model = Configuration["OpenAITitleModel"];
                          if (string.IsNullOrEmpty(model))
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "OpenAITitleModel is not set in configuration");
                              return;
                          }
                          Guid suggestingUserId;
                          if (!Guid.TryParse(Configuration["OpenAITitleUserId"], out suggestingUserId))
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "OpenAITitleUserId is not set (or invalid) in configuration");
                              return;
                          }

                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          }, new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMinutes(10) });

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          string checkpointName = _AICheckpointNameFor("PoemTitles", masterCatId);
                          if (restart)
                              await _AISetCheckpointAsync(context, checkpointName, 0);
                          int lastSafeId = await _AIGetCheckpointAsync(context, checkpointName);
                          var failedIds = await _AIGetFailedIdsAsync(context, checkpointName);
                          if (restart)
                              failedIds.Clear();
                          var failedIdList = failedIds.ToList();

                          var titlesQuery = context.GanjoorPoems.AsNoTracking().Where(p => ((p.Id > lastSafeId && p.Id >= startFrom) || failedIdList.Contains(p.Id)) && p.Title.StartsWith("بخش"));
                          if (masterCatId > 0)
                          {
                              //category tree under the master category
                              var catIds = new HashSet<int>(await _AIGetCategoryTreeIdsAsync(context, masterCatId));
                              titlesQuery = titlesQuery.Where(p => catIds.Contains(p.CatId));
                          }

                          var candidates = (await titlesQuery
                              .Select(p => new { p.Id, p.Title })
                              .ToListAsync())
                              .Where(p => AITitleBareSectionRegex.IsMatch(p.Title))
                              .OrderBy(p => p.Id)
                              .ToList();
                          if (count > 0)
                              candidates = candidates.Take(count).ToList();

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poem count = {candidates.Count}");

                          int created = 0, skipped = 0, failed = 0, consecutiveFailures = 0, sinceSave = 0;
                          bool aborted = false;
                          for (int i = 0; i < candidates.Count; i++)
                          {
                              var poem = candidates[i];
                              bool itemFailed = false;
                              try
                              {
                                  //never clobber an existing pending suggestion of this user for the poem
                                  if (await context.GanjoorPoemCorrections.AsNoTracking().AnyAsync(c => c.PoemId == poem.Id && c.UserId == suggestingUserId && c.Reviewed == false))
                                  {
                                      skipped++;
                                      continue;
                                  }

                                  var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).Select(v => v.Text).ToListAsync();
                                  if (verses.Count == 0)
                                  {
                                      skipped++;
                                      continue;
                                  }
                                  string text = string.Join(Environment.NewLine, verses);

                                  await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i + 1} از {candidates.Count} - {poem.Title} (ایجاد: {created}، ردشده: {skipped}، خطا: {failed})");

                                  var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                  {
                                      Messages = new List<ChatMessage>
                                      {
                                          ChatMessage.FromUser(AITitlePromptHeader + text),
                                      },
                                      Model = model,
                                  });
                                  if (!completionResult.Successful)
                                      throw new InvalidOperationException(completionResult.Error?.Message ?? "Open AI request failed");
                                  consecutiveFailures = 0;

                                  string generated = _CleanAIPoemTitle(completionResult.Choices.First().Message.Content);
                                  if (string.IsNullOrEmpty(generated))
                                  {
                                      failed++;
                                      continue;
                                  }

                                  context.GanjoorPoemCorrections.Add(new GanjoorPoemCorrection()
                                  {
                                      PoemId = poem.Id,
                                      UserId = suggestingUserId,
                                      Title = poem.Title.Trim() + " - " + generated,
                                      OriginalTitle = poem.Title,
                                      Note = $"عنوان پیشنهادی هوش مصنوعی ({model})",
                                      Date = DateTime.Now,
                                      Result = CorrectionReviewResult.NotReviewed,
                                      Reviewed = false,
                                      AffectedThePoem = false,
                                      HideMyName = false,
                                  });
                                  await context.SaveChangesAsync();
                                  created++;
                              }
                              catch (Exception exp)
                              {
                                  failed++;
                                  itemFailed = true; //remembered and retried by the next run; the checkpoint itself keeps moving
                                  context.ChangeTracker.Clear();
                                  if (++consecutiveFailures >= AIMaxConsecutiveFailures)
                                  {
                                      aborted = true;
                                      await jobProgressServiceEF.UpdateJob(job.Id, i, $"متوقف شد پس از {AIMaxConsecutiveFailures} خطای پیاپی در شعر {poem.Id}", false, exp.ToString());
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
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, $"ایجاد: {created}، ردشده: {skipped}، خطا: {failed} (در انتظار تلاش دوباره: {failedIds.Count}) - آخرین شعر پردازش‌شده: {lastSafeId}", true);
                      }
                      catch (Exception exp)
                      {
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                      }
                  }
              });
        }

        private static string _CleanAIPoemTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";
            string t = raw.Trim().Trim('"', '«', '»', '*', '#', ' ', '\'');
            //take the first non empty line only
            t = t.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First().Trim().Trim('"', '«', '»', '*', '#', ' ', '.');
            foreach (var prefix in new[] { "عنوان:", "عنوان :", "عنوان -" })
            {
                if (t.StartsWith(prefix))
                    t = t.Substring(prefix.Length).Trim();
            }
            t = t.Replace("ي", "ی").Replace("ك", "ک").Replace("ۀ", "هٔ");
            int words = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (words < 3 || words > 30)
                return "";
            return t;
        }
    }
}
