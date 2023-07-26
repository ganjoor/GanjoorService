using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RMuseum.Models.PDFLibrary;
using RMuseum.Models.PDFLibrary.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace RMuseum.Services.Implementation
{
    public partial class PDFLibraryService
    {
        public async Task<RServiceResult<bool>> StartImportingSohaLibraryUrlAsync(string srcUrl)
        {
            try
            {
                if (
                    (await _context.PDFBooks.Where(a => a.OriginalSourceUrl == srcUrl).SingleOrDefaultAsync())
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"duplicated srcUrl '{srcUrl}'");
                }
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.Pdf && j.SrcUrl == srcUrl && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing source url: {srcUrl}");
                }

                _backgroundTaskQueue.QueueBackgroundWorkItem
                       (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                               {
                                   ImportJob job = new ImportJob()
                                   {
                                       JobType = JobType.Pdf,
                                       SrcContent = "",
                                       ResourceNumber = "scrapping ...",
                                       FriendlyUrl = "",
                                       SrcUrl = srcUrl,
                                       QueueTime = DateTime.Now,
                                       ProgressPercent = 0,
                                       Status = ImportJobStatus.NotStarted
                                   };
                                   await context.ImportJobs.AddAsync
                                       (
                                       job
                                       );
                                   await context.SaveChangesAsync();

                                   try
                                   {
                                       string html = "";
                                       using (var client = new HttpClient())
                                       {
                                           using (var result = await client.GetAsync(srcUrl))
                                           {
                                               if (result.IsSuccessStatusCode)
                                               {
                                                   html = await result.Content.ReadAsStringAsync();
                                               }
                                               else
                                               {
                                                   job.EndTime = DateTime.Now;
                                                   job.Status = ImportJobStatus.Failed;
                                                   job.Exception = $"Http result is not ok ({result.StatusCode}) for {srcUrl}";
                                                   context.Update(job);
                                                   await context.SaveChangesAsync();
                                                   return;
                                               }
                                           }
                                       }

                                       if (html.IndexOf("/item/download/") == -1)
                                       {
                                           job.EndTime = DateTime.Now;
                                           job.Status = ImportJobStatus.Failed;
                                           job.Exception = $"/item/download/ not found in html source.";
                                           context.Update(job);
                                           await context.SaveChangesAsync();
                                           return;
                                       }

                                       List<RTagValue> meta = new List<RTagValue>();
                                       int idxStart;
                                       int idx = html.IndexOf("branch-link");
                                       if(idx != -1)
                                       {
                                           idxStart = html.IndexOf(">", idx);
                                           if(idxStart != -1)
                                           {
                                               int idxEnd = html.IndexOf("<", idxStart);

                                               if(idxEnd != -1)
                                               {
                                                   meta.Add
                                                   (
                                                        await TagHandler.PrepareAttribute(context, "First Hand Source", html.Substring(idxStart + 1, idxEnd - idxStart - 1), 1)
                                                   );
                                               }
                                           }
                                       }

                                       NewPDFBookViewModel model = new NewPDFBookViewModel();

                                       idx = html.IndexOf("title-normal-for-book-name");
                                       if(idx == -1)
                                       {
                                           job.EndTime = DateTime.Now;
                                           job.Status = ImportJobStatus.Failed;
                                           job.Exception = $"title-normal-for-book-name not found in {srcUrl}";
                                           context.Update(job);
                                           await context.SaveChangesAsync();
                                           return;
                                       }
                                       idxStart = html.IndexOf(">", idx);
                                       if (idxStart != -1)
                                       {
                                           int idxEnd = html.IndexOf("<", idxStart);

                                           if (idxEnd != -1)
                                           {
                                               model.Title = html.Substring(idxStart + 1, idxEnd - idxStart - 1);
                                               //we can try to extract volume information from title here
                                               model.Title = model.Title.ToPersianNumbers().ApplyCorrectYeKe();
                                           }
                                       }

                                       idxStart = html.IndexOf("width-150");
                                       while(idxStart != -1)
                                       {
                                           idxStart = html.IndexOf(">", idxStart);
                                           if (idxStart == -1) break;
                                           int idxEnd = html.IndexOf("<", idxStart);
                                           if (idxEnd == -1) break;

                                           string tagName = html.Substring(idxStart + 1, idxEnd - idxStart - 1).ToPersianNumbers().ApplyCorrectYeKe();
                                           
                                           idxStart = html.IndexOf("value-name", idxEnd);
                                           if (idxStart == -1) break;
                                           idxStart = html.IndexOf(">", idxStart);
                                           if (idxStart == -1) break;
                                           idxEnd = html.IndexOf("</span>", idxStart);
                                           if (idxEnd == -1) break;

                                           string tagValue = html.Substring(idxStart + 1, idxEnd - idxStart - 1).ToPersianNumbers().ApplyCorrectYeKe();
                                           tagValue = Regex.Replace(tagValue, "<.*?>", string.Empty);

                                           if (tagName == "نویسنده")
                                           {
                                               model.AuthorsLine = tagValue;
                                               tagName = "Author";
                                           }
                                           if(tagName == "مترجم")
                                           {
                                               model.TranslatorsLine = tagValue;
                                               model.IsTranslation = true;
                                               tagName = "Translator";
                                           }
                                           if(tagName == "زبان")
                                           {
                                               model.Language = tagValue;
                                               tagName = "Language";
                                           }
                                           if (tagName == "شماره جلد")
                                           {
                                               if (int.TryParse(tagValue, out int v))
                                               {
                                                   model.VolumeOrder = v;
                                               }
                                               tagName = "Volume";
                                           }
                                           meta.Add
                                                   (
                                                        await TagHandler.PrepareAttribute(context, tagName, tagValue, 1)
                                                   );
                                           idxStart = html.IndexOf("width-150", idxEnd);

                                       }

                                       idx = html.IndexOf("/item/download/");
                                       int idxQuote = html.IndexOf('"', idx);
                                       string downloadUrl = html.Substring(idx, idxQuote - idx - 1);
                                       downloadUrl = "https://sohalibrary.com" + downloadUrl;



                                   }
                                   catch (Exception e)
                                   {
                                       job.EndTime = DateTime.Now;
                                       job.Status = ImportJobStatus.Failed;
                                       job.Exception = e.ToString();
                                       job.EndTime = DateTime.Now;
                                       context.Update(job);
                                       await context.SaveChangesAsync();
                                   }
                                  
                               }
                           }
                       );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}
