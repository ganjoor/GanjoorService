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
        public void OpenAIStartFillingCoupletSummaries()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIFillCoupletSummaries", "Open AI initialization")).Result;
                      try
                      {

                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAPIBaseUrl"],
                              BaseDomain = Configuration["Configuration"]
                          });

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          var verses = await context.GanjoorVerses.AsNoTracking()
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
                                .ToListAsync();
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Verse count = {verses.Count}");

                          for ( var i = 0; i < verses.Count; i++ )
                          {
                              var verse = verses[i];
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
                                  if(verse.VersePosition == VersePosition.Paragraph)
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
                                      if(summary.Contains("اکتبر"))
                                      {
                                          summary = "";
                                      }
                                      if (!string.IsNullOrEmpty(summary))
                                      {
                                          summary = "هوش مصنوعی: " + summary;
                                          var editableVerse = await context.GanjoorVerses.Where(v => v.Id == verse.Id).SingleAsync();
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
    }
}