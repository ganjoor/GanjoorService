using DNTPersianUtils.Core;
using ganjoor;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RMuseum.Models.PDFLibrary;
using RMuseum.Models.PDFLibrary.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace RMuseum.Services.Implementation
{
    public partial class PDFLibraryService
    {
        private async Task<RServiceResult<int>> StartImportingELiteratureBookUrlAsync(RMuseumDbContext ctx, string srcUrl)
        {
            try
            {
                if (
                    (await ctx.PDFBooks.Where(a => a.OriginalSourceUrl == srcUrl).SingleOrDefaultAsync())
                    !=
                    null
                    )
                {
                    return new RServiceResult<int>(-1, $"duplicated srcUrl '{srcUrl}'");
                }
                if (
                    (
                    await ctx.ImportJobs
                        .Where(j => j.JobType == JobType.Pdf && j.SrcContent == ("scrapping ..." + srcUrl) && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<int>(0, $"Job is already scheduled or running for importing source url: {srcUrl}");
                }

                return await ImportELiteratureBookLibraryUrlAsync(srcUrl, ctx, true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }
        private async Task<RServiceResult<int>> ImportELiteratureBookLibraryUrlAsync(string srcUrl, RMuseumDbContext context, bool finalizeDownload)
        {
            /*
            var oldJobs = await context.ImportJobs.ToArrayAsync();
            context.RemoveRange(oldJobs);
            await context.SaveChangesAsync();
            */

            ImportJob job = new ImportJob()
            {
                JobType = JobType.Pdf,
                SrcContent = "",
                ResourceNumber = "scrapping ..." + srcUrl,
                FriendlyUrl = "",
                SrcUrl = "",
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
                var pdfSource = await context.PDFSources.Where(s => s.Name == "کتابخانهٔ مجازی ادبیات").SingleOrDefaultAsync();
                if (pdfSource == null)
                {
                    PDFSource newSource = new PDFSource()
                    {
                        Name = "کتابخانهٔ مجازی ادبیات",
                        Url = "https://eliteraturebook.com",
                        Description = "کتابخانهٔ مجازی ادبیات"
                    };
                    context.PDFSources.Add(newSource);
                    await context.SaveChangesAsync();
                    pdfSource = await context.PDFSources.Where(s => s.Name == "کتابخانهٔ مجازی ادبیات").SingleAsync();
                }
                NewPDFBookViewModel model = new NewPDFBookViewModel();
                model.PDFSourceId = pdfSource.Id;
                model.OriginalSourceName = pdfSource.Name;
                model.OriginalSourceUrl = srcUrl;
                model.BookScriptType = BookScriptType.Printed;
                model.Language = "فارسی";
                model.SkipUpload = true;

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
                            return new RServiceResult<int>(0, job.Exception);
                        }
                    }
                }

                if (html.IndexOf("/download") == -1)
                {
                    job.EndTime = DateTime.Now;
                    job.Status = ImportJobStatus.Failed;
                    job.Exception = $"/download/ not found in html source.";
                    context.Update(job);
                    await context.SaveChangesAsync();
                    return new RServiceResult<int>(0, job.Exception);
                }

                List<RTagValue> meta = new List<RTagValue>();
                int idxStart;
                meta.Add
                             (
                                  await TagHandler.PrepareAttribute(context, "First Hand Source", "کتابخانه تخصصی ادبیات", 1)
                             );

                int idx = html.IndexOf("\"book-title\"");
                if (idx == -1)
                {
                    job.EndTime = DateTime.Now;
                    job.Status = ImportJobStatus.Failed;
                    job.Exception = $"\"book-title\" not found in {srcUrl}";
                    context.Update(job);
                    await context.SaveChangesAsync();
                    return new RServiceResult<int>(0, job.Exception);
                }
                idxStart = html.IndexOf(">", idx);
                if (idxStart != -1)
                {
                    int idxEnd = html.IndexOf("<", idxStart);

                    if (idxEnd != -1)
                    {
                        model.Title = html.Substring(idxStart + 1, idxEnd - idxStart - 1);
                        //we can try to extract volume information from title here
                        model.Title = model.Title.Replace("\n", "").Replace("\r", "").Trim();
                    }
                }

                idxStart = html.IndexOf("box summary-box");
                if(idxStart != -1)
                {
                    idxStart = html.IndexOf("<p>", idxStart);
                    if(idxStart != -1)
                    {
                        idxStart += "<p>".Length;
                        int idxEnd = html.IndexOf("<", idxStart);

                        if (idxEnd != -1)
                        {
                            model.Description = html.Substring(idxStart + 1, idxEnd - idxStart - 1).ApplyCorrectYeKe();
                            model.Description = model.Description.Trim();
                        }
                    }
                }
                string tagValue;
                string tagName;

                idx = html.IndexOf("\"author-name\"");
                if (idx != -1)
                {
                    idx = html.IndexOf("ref=", idx);
                    idxStart = html.IndexOf(">", idx);
                    if (idxStart != -1)
                    {
                        int idxEnd = html.IndexOf("<", idxStart);

                        if (idxEnd != -1)
                        {
                            tagValue = html.Substring(idxStart + 1, idxEnd - idxStart - 1);
                            tagValue = Regex.Replace(tagValue, "<.*?>", string.Empty).Trim();

                            string tagValueCleaned = tagValue.ToPersianNumbers().ApplyCorrectYeKe();
                            tagName = "نویسنده";

                            if (tagName == "نویسنده")
                            {
                                model.AuthorsLine = tagValueCleaned;
                                tagName = "Author";

                                string[] authors = tagValueCleaned.Split(',', StringSplitOptions.RemoveEmptyEntries);

                                for (int i = 0; i < authors.Length; i++)
                                {
                                    string authorTrimmed = authors[i].Trim();
                                    var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == authorTrimmed).FirstOrDefaultAsync();
                                    if (existingAuthor != null)
                                    {
                                        if (i == 0)
                                        {
                                            model.WriterId = existingAuthor.Id;
                                        }
                                        if (i == 1)
                                        {
                                            model.Writer2Id = existingAuthor.Id;
                                        }
                                        if (i == 2)
                                        {
                                            model.Writer3Id = existingAuthor.Id;
                                        }
                                        if (i == 3)
                                        {
                                            model.Writer4Id = existingAuthor.Id;
                                        }

                                    }
                                    else
                                    {
                                        var newAuthor = new Author()
                                        {
                                            Name = authorTrimmed
                                        };
                                        context.Authors.Add(newAuthor);
                                        await context.SaveChangesAsync();
                                        if (i == 0)
                                        {
                                            model.WriterId = newAuthor.Id;
                                        }
                                        if (i == 1)
                                        {
                                            model.Writer2Id = newAuthor.Id;
                                        }
                                        if (i == 2)
                                        {
                                            model.Writer3Id = newAuthor.Id;
                                        }
                                        if (i == 3)
                                        {
                                            model.Writer4Id = newAuthor.Id;
                                        }
                                    }
                                }


                            }
                        }
                    }
                }
                



                idxStart = html.IndexOf("\"part view\"");
                while (idxStart != -1)
                {
                    idxStart = html.IndexOf(">", idxStart);
                    if (idxStart == -1) break;
                    int idxEnd = html.IndexOf("<", idxStart);
                    if (idxEnd == -1) break;

                    tagName = html.Substring(idxStart + 1, idxEnd - idxStart - 1).Replace(":", "").ToPersianNumbers().ApplyCorrectYeKe().Trim();

                    idxStart = html.IndexOf(">", idxEnd);
                    if (idxStart == -1) break;
                    idxEnd = html.IndexOf("<", idxStart);
                    if (idxEnd == -1) break;

                    tagValue = html.Substring(idxStart + 1, idxEnd - idxStart - 1);
                    tagValue = Regex.Replace(tagValue, "<.*?>", string.Empty).Replace("\n", "").Replace("\r", "").Trim();

                    string tagValueCleaned = tagValue.Replace("\n", "").Replace("\r", "").Trim().ToPersianNumbers().ApplyCorrectYeKe();

                    
                    if (tagName == "مترجم")
                    {
                        model.TranslatorsLine = tagValueCleaned;
                        model.IsTranslation = true;
                        tagName = "Translator";

                        string[] authors = tagValueCleaned.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < authors.Length; i++)
                        {
                            string authorTrimmed = authors[i].Trim();
                            var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == authorTrimmed).FirstOrDefaultAsync();
                            if (existingAuthor != null)
                            {
                                if (i == 0)
                                {
                                    model.TranslatorId = existingAuthor.Id;
                                }
                                if (i == 1)
                                {
                                    model.Translator2Id = existingAuthor.Id;
                                }
                                if (i == 2)
                                {
                                    model.Translator3Id = existingAuthor.Id;
                                }
                                if (i == 3)
                                {
                                    model.Translator4Id = existingAuthor.Id;
                                }
                            }
                            else
                            {
                                var newAuthor = new Author()
                                {
                                    Name = authorTrimmed
                                };
                                context.Authors.Add(newAuthor);
                                await context.SaveChangesAsync();

                                if (i == 0)
                                {
                                    model.TranslatorId = newAuthor.Id;
                                }
                                if (i == 1)
                                {
                                    model.Translator2Id = newAuthor.Id;
                                }
                                if (i == 2)
                                {
                                    model.Translator3Id = newAuthor.Id;
                                }
                                if (i == 3)
                                {
                                    model.Translator4Id = newAuthor.Id;
                                }
                            }
                        }


                    }
                    if (tagName == "مصحح")
                    {
                        tagName = "Collector";

                        string[] authors = tagValueCleaned.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < authors.Length; i++)
                        {
                            string authorTrimmed = authors[i].Trim();
                            var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == authorTrimmed).FirstOrDefaultAsync();
                            if (existingAuthor != null)
                            {
                                if (i == 0)
                                {
                                    model.CollectorId = existingAuthor.Id;
                                }
                                if (i == 1)
                                {
                                    model.Collector2Id = existingAuthor.Id;
                                }

                            }
                            else
                            {
                                var newAuthor = new Author()
                                {
                                    Name = authorTrimmed
                                };
                                context.Authors.Add(newAuthor);
                                await context.SaveChangesAsync();
                                if (i == 0)
                                {
                                    model.CollectorId = newAuthor.Id;
                                }
                                if (i == 1)
                                {
                                    model.Collector2Id = newAuthor.Id;
                                }
                            }
                        }


                    }
                    if (tagName == "زبان")
                    {
                        model.Language = tagValueCleaned;
                        tagName = "Language";

                        if (!tagValueCleaned.Contains("فارسی"))
                        {
                            job.EndTime = DateTime.Now;
                            job.Status = ImportJobStatus.Failed;
                            job.Exception = "Language is not فارسی";
                            context.Update(job);
                            await context.SaveChangesAsync();
                            return new RServiceResult<int>(0, job.Exception);
                        }
                    }
                    if (tagName == "شماره جلد")
                    {
                        if (int.TryParse(tagValue, out int v))
                        {
                            model.VolumeOrder = v;
                        }
                        tagName = "Volume";
                    }
                    if (tagName == "ناشر")
                    {
                        model.PublisherLine = tagValueCleaned;
                        tagName = "Publisher";
                    }
                    if (tagName == "محل نشر")
                    {
                        model.PublishingLocation = tagValueCleaned;
                        tagName = "Publishing Location";
                    }
                    if (tagName == "تاریخ انتشار")
                    {
                        model.PublishingDate = tagValueCleaned;
                        tagName = "Publishing Date";
                    }
                    if (tagName == "موضوع")
                    {
                        tagName = "Subject";
                    }
                    if (tagName == "تعداد صفحات")
                    {
                        if (tagValue.Contains("ـ"))
                        {
                            tagValue = tagValue.Substring(tagValue.IndexOf("ـ") + 1).Trim();

                        }

                        if (int.TryParse(tagValue, out int v))
                        {
                            model.ClaimedPageCount = v;
                        }

                        tagName = "Page Count";
                    }

                    meta.Add
                            (
                                 await TagHandler.PrepareAttribute(context, tagName, tagValueCleaned, 1)
                            );
                    idxStart = html.IndexOf("\"part view\"", idxEnd);

                }

                idx = html.IndexOf("\"tag\"");
                while(idx != -1)
                {
                    idxStart = html.IndexOf(">", idx);
                    if (idxStart != -1)
                    {
                        int idxEnd = html.IndexOf("<", idxStart);

                        if (idxEnd != -1)
                        {
                            var tv = html.Substring(idxStart + 1, idxEnd - idxStart - 1).Replace("\n", "").Replace("\r", "").Trim().ApplyCorrectYeKe();
                            meta.Add
                           (
                                await TagHandler.PrepareAttribute(context, "Subject", tv, 1)
                           );
                        }
                    }
                    idx = html.IndexOf("\"tag\"", idxStart);
                }

                string bookTitle = model.Title;
                int volumeNumber = 0;
                if (bookTitle.Contains("ـ ج"))
                {
                    bookTitle = bookTitle.Substring(0, model.Title.IndexOf("ـ ج") - 1);
                    int.TryParse(model.Title.Substring(model.Title.IndexOf("ـ ج") + "ـ ج".Length).Trim(), out volumeNumber);
                }

                bookTitle = bookTitle.ToPersianNumbers().ApplyCorrectYeKe().Trim();
                model.Title = model.Title.ToPersianNumbers().ApplyCorrectYeKe().Trim();

                var book = await context.Books.AsNoTracking().Where(b => b.Name == bookTitle).FirstOrDefaultAsync();
                if (book != null)
                {
                    model.BookId = book.Id;
                }
                else
                {
                    Book newBook = new Book()
                    {
                        Name = bookTitle,
                        Description = model.Description,
                        LastModified = DateTime.Now,
                    };
                    context.Books.Add(newBook);
                    await context.SaveChangesAsync();
                    model.BookId = newBook.Id;
                }

                if (volumeNumber != 0)
                {
                    MultiVolumePDFCollection collection = await context.MultiVolumePDFCollections.Where(v => v.Name == bookTitle && v.BookId == model.BookId).SingleOrDefaultAsync();
                    if (collection != null)
                    {
                        model.MultiVolumePDFCollectionId = collection.Id;

                        collection.VolumeCount += 1;
                        context.Update(collection);
                        await context.SaveChangesAsync();

                    }
                    else
                    {
                        MultiVolumePDFCollection newCollection = new MultiVolumePDFCollection()
                        {
                            Name = bookTitle,
                            BookId = model.BookId,
                            Description = model.Description,
                            VolumeCount = 1,
                        };
                        context.MultiVolumePDFCollections.Add(newCollection);
                        await context.SaveChangesAsync();
                        model.MultiVolumePDFCollectionId = newCollection.Id;
                    }
                }

                idx = html.IndexOf("https://eliteraturebook.com/books/download");
                int idxQuote = html.IndexOf('"', idx);
                string downloadUrl = html.Substring(idx, idxQuote - idx);
       


                model.OriginalFileUrl = downloadUrl;

                if (!finalizeDownload)
                {
                    context.QueuedPDFBooks.Add
                        (
                        new QueuedPDFBook()
                        {
                            Title = model.Title,
                            DownloadOrder = await context.QueuedPDFBooks.CountAsync(),
                            AuthorsLine = model.AuthorsLine,
                            Description = model.Description,
                            Language = model.Language,
                            IsTranslation = model.IsTranslation,
                            TranslatorsLine = model.TranslatorsLine,
                            OriginalSourceName = model.OriginalSourceName,
                            OriginalSourceUrl = model.OriginalSourceUrl,
                            OriginalFileUrl = model.OriginalFileUrl,
                            Processed = false,
                        }
                        );
                    await context.SaveChangesAsync();
                    job.EndTime = DateTime.Now;
                    job.Status = ImportJobStatus.Succeeded;
                    context.Update(job);
                    await context.SaveChangesAsync();
                    return new RServiceResult<int>(0, job.Exception);
                }

                using (var client = new HttpClient())
                {
                    using (var result = await client.GetAsync(downloadUrl))
                    {
                        if (result.IsSuccessStatusCode)
                        {
                            string fileName = (1 + await context.PDFBooks.MaxAsync(p => p.Id)).ToString().PadLeft(8, '0') + "-eliteraturebook.pdf";
                            

                            model.LocalImportingPDFFilePath = Path.Combine(_imageFileService.ImageStoragePath, fileName);
                            if (File.Exists(model.LocalImportingPDFFilePath))
                                File.Delete(model.LocalImportingPDFFilePath);

                            using (Stream pdfStream = await result.Content.ReadAsStreamAsync())
                            {
                                pdfStream.Seek(0, SeekOrigin.Begin);
                                using (FileStream fs = File.OpenWrite(model.LocalImportingPDFFilePath))
                                {
                                    pdfStream.CopyTo(fs);
                                }
                            }
                            job.EndTime = DateTime.Now;
                            job.Status = ImportJobStatus.Succeeded;
                            context.Update(job);
                            await context.SaveChangesAsync();

                            string fileChecksum = PoemAudio.ComputeCheckSum(model.LocalImportingPDFFilePath);

                            var res = await ImportLocalPDFFileAsync(context, model, fileChecksum);
                            File.Delete(model.LocalImportingPDFFilePath);
                            if (res.Result != null)
                            {
                                var pdf = await context.PDFBooks.Include(p => p.Tags).Where(p => p.Id == res.Result.Id).SingleAsync();
                                if (pdf.Tags.Count > 0)
                                {
                                    //not working
                                    foreach (var tag in meta)
                                    {
                                        pdf.Tags.Add(tag);
                                    }
                                }
                                else
                                {
                                    pdf.Tags = meta;
                                }

                                context.Update(pdf);
                                await context.SaveChangesAsync();
                                return new RServiceResult<int>(res.Result.Id);
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(res.ExceptionString))
                                {
                                    return new RServiceResult<int>(0, "ImportLocalPDFFileAsync result was null");
                                }

                                if(res.ExceptionString.Contains("duplicated pdf with checksum"))
                                {
                                    return new RServiceResult<int>(-2, res.ExceptionString);
                                }

                                return new RServiceResult<int>(0, res.ExceptionString);
                            }

                        }
                        else
                        {
                            job.EndTime = DateTime.Now;
                            job.Status = ImportJobStatus.Failed;
                            job.Exception = $"Http result is not ok ({result.StatusCode}) for {downloadUrl}";
                            context.Update(job);
                            await context.SaveChangesAsync();
                            return new RServiceResult<int>(0, job.Exception);
                        }
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
                return new RServiceResult<int>(0, job.Exception);
            }

        }

        /// <summary>
        /// batch import eliteraturebook.com library
        /// </summary>
        /// <param name="ajaxPageIndexStart">from 0</param>
        /// <param name="ajaxPageIndexEnd"></param>
        /// <param name="finalizeDownload"></param>
        public void BatchImportELiteratureBookLibraryAsync(int ajaxPageIndexStart, int ajaxPageIndexEnd, bool finalizeDownload)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                       (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                               {
                                   string ajaxPageUrl = "https://eliteraturebook.com/index";
                                   for (int nAjaxPageIndex = ajaxPageIndexEnd; nAjaxPageIndex >= ajaxPageIndexStart; nAjaxPageIndex--)
                                   {
                                       try
                                       {
                                           ImportJob job = new ImportJob()
                                           {
                                               JobType = JobType.Pdf,
                                               SrcContent = "",
                                               ResourceNumber = $"scrapping ajax page ... {nAjaxPageIndex}",
                                               FriendlyUrl = "",
                                               SrcUrl = "",
                                               QueueTime = DateTime.Now,
                                               ProgressPercent = 0,
                                               Status = ImportJobStatus.NotStarted
                                           };
                                           await context.ImportJobs.AddAsync
                                               (
                                               job
                                               );
                                           await context.SaveChangesAsync();
                                           string html = string.Empty;
                                           using (var client = new HttpClient())
                                           {
                                               using (var result = await client.PostAsync(ajaxPageUrl,
                                                   new StringContent($"ajaxPage={nAjaxPageIndex}")
                                                   ))
                                               {
                                                   if (result.IsSuccessStatusCode)
                                                   {
                                                       html = await result.Content.ReadAsStringAsync();
                                                       job.EndTime = DateTime.Now;
                                                       job.Status = ImportJobStatus.Succeeded;
                                                       context.Update(job);
                                                       await context.SaveChangesAsync();
                                                   }
                                                   else
                                                   {
                                                       job.EndTime = DateTime.Now;
                                                       job.Status = ImportJobStatus.Failed;
                                                       job.Exception = $"Http result is not ok ({result.StatusCode}) for ajaxPage={nAjaxPageIndex}";
                                                       context.Update(job);
                                                       await context.SaveChangesAsync();
                                                       return;
                                                   }
                                               }
                                           }
                                           if(!string.IsNullOrEmpty(html))
                                           {
                                               int idx = html.IndexOf("https://eliteraturebook.com/books/view/");
                                               while(idx != -1)
                                               {
                                                   int endIdx = html.IndexOf("\"", idx);
                                                   string srcUrl = html.Substring(idx, endIdx - idx);
                                                   idx = html.IndexOf("https://eliteraturebook.com/books/view/", idx + 1);

                                                   if (
                                                    (await context.PDFBooks.Where(a => a.OriginalSourceUrl == srcUrl).SingleOrDefaultAsync())
                                                    ==
                                                    null
                                                    )
                                                   {
                                                       await ImportELiteratureBookLibraryUrlAsync(srcUrl, context, finalizeDownload);
                                                   }
                                                   
                                               }
                                              
                                           }
                                           
                                       }
                                       catch
                                       {
                                           //ignore
                                       }
                                   }
                               }
                           }
                       );
        }

    }
}

