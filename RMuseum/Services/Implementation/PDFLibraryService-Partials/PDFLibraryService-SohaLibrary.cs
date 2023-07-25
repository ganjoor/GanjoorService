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
