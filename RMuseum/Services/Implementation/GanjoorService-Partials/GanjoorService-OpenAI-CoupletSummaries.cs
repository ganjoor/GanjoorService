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

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// fill couplet summaries using open ai
        /// </summary>
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        public void OpenAIStartFillingCoupletSummaries(int startFrom, int count)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIFillCoupletSummaries - start: {startFrom} - count: {count}", "Open AI initialization")).Result;
                      try
                      {

                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          });

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          var verses =
                            count == 0 ?
                                startFrom == 0 ?
                                await context.GanjoorVerses.AsNoTracking()
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
                                        string.IsNullOrEmpty(v.CoupletSummary)
                                )
                                .ToListAsync()
                                :
                                await context.GanjoorVerses.AsNoTracking()
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
                                        string.IsNullOrEmpty(v.CoupletSummary)
                                )
                                .Skip(startFrom)
                                .ToListAsync()
                                :
                                await context.GanjoorVerses.AsNoTracking()
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
                                        string.IsNullOrEmpty(v.CoupletSummary)
                                )
                                .Skip(startFrom).Take(count)
                                .ToListAsync()
                                ;
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Verse count = {verses.Count}");

                          for (var i = 0; i < verses.Count; i++)
                          {
                              var verse = verses[i];
                              var editableVerse = await context.GanjoorVerses.Where(v => v.Id == verse.Id).SingleAsync();
                              if (!string.IsNullOrEmpty(editableVerse.CoupletSummary))
                                  continue;
                              var relatedVerses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == verse.PoemId && v.CoupletIndex == verse.CoupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                              string couplet = "";
                              foreach (var relatedVerse in relatedVerses)
                              {
                                  couplet += relatedVerse.Text;
                                  couplet += " ";
                              }
                              couplet = couplet.Trim();

                              if (!string.IsNullOrEmpty(couplet))
                              {
                                  await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {verses.Count} - {couplet}");
                                  string command = "این بیت را به فارسی روان معنی کن، در متن اشاره نکن که این معنی این بیت است:";
                                  if (verse.VersePosition == VersePosition.Paragraph)
                                  {
                                      command = "این پاراگراف را به فارسی روان معنی کن، در متن اشاره نکن که این معنی این پاراگراف است:";
                                  }
                                  var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                  {
                                      Messages = new List<ChatMessage>
                                        {
                                            ChatMessage.FromSystem(command
                                                    +
                                                    Environment.NewLine
                                                    +
                                                    couplet
                                                    ),
                                        },
                                      Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o_mini,
                                  });
                                  if (completionResult.Successful)
                                  {
                                      string summary = completionResult.Choices.First().Message.Content;
                                      if (summary.Contains("اکتبر"))
                                      {
                                          summary = "";
                                      }
                                      if (!string.IsNullOrEmpty(summary))
                                      {
                                          summary = "هوش مصنوعی: " + summary;

                                          editableVerse.CoupletSummary = summary;
                                          context.Update(editableVerse);
                                          await context.SaveChangesAsync();
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


        /// <summary>
        /// fill poem summaries using open ai
        /// </summary>
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        public void OpenAIStartFillingPoemSummaries(int startFrom, int count)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartFillingPoemSummaries - start: {startFrom} - count: {count}", "Open AI initialization")).Result;
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
                                .Where(
                                    p => string.IsNullOrEmpty(p.PoemSummary)
                                )
                                .ToListAsync()
                                :
                                await context.GanjoorPoems.AsNoTracking()
                                .Where(
                                    p => string.IsNullOrEmpty(p.PoemSummary)
                                )
                                .Skip(startFrom)
                                .ToListAsync()
                                :
                            await context.GanjoorPoems.AsNoTracking()
                                .Where(
                                    p => string.IsNullOrEmpty(p.PoemSummary)
                                )
                                .Skip(startFrom)
                                .Take(count)
                                .ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poems count = {poems.Count}");

                          for (var i = 0; i < poems.Count; i++)
                          {
                              var poem = poems[i];


                              if (!string.IsNullOrEmpty(poem.PlainText))
                              {
                                  var editablePoem = await context.GanjoorPoems.Where(p => p.Id == poem.Id).SingleAsync();
                                  if (!string.IsNullOrEmpty(editablePoem.PoemSummary))
                                      continue;

                                  await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle}");
                                  string command = "به فارسی روان خلاصه کن:";
                                  var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                  {
                                      Messages = new List<ChatMessage>
                                        {
                                            ChatMessage.FromSystem(command
                                                    +
                                                    Environment.NewLine
                                                    +
                                                    poem.PlainText
                                                    ),
                                        },
                                      Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o_mini,
                                  });
                                  if (completionResult.Successful)
                                  {
                                      string summary = completionResult.Choices.First().Message.Content;
                                      if (summary.Contains("اکتبر"))
                                      {
                                          summary = "";
                                      }
                                      if (!string.IsNullOrEmpty(summary))
                                      {
                                          summary = "هوش مصنوعی: " + summary;

                                          editablePoem.PoemSummary = summary;
                                          context.Update(editablePoem);
                                          await context.SaveChangesAsync();
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
                                .Where(
                                    p => string.IsNullOrEmpty(p.PoemSummary)
                                )
                                .ToListAsync()
                                :
                                await context.GanjoorPoems.AsNoTracking()
                                .Where(
                                    p => string.IsNullOrEmpty(p.PoemSummary)
                                )
                                .Skip(startFrom)
                                .ToListAsync()
                                :
                            await context.GanjoorPoems.AsNoTracking()
                                .Where(
                                    p => string.IsNullOrEmpty(p.PoemSummary)
                                )
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
                                      Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o_mini,
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