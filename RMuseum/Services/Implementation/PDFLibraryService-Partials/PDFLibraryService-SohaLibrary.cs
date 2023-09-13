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
        private async Task<RServiceResult<int>> StartImportingSohaLibraryUrlAsync(RMuseumDbContext ctx, string srcUrl)
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

                return await ImportSohaLibraryUrlAsync(srcUrl, ctx, true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        private async Task<RServiceResult<int>> ImportSohaLibraryUrlAsync(string srcUrl, RMuseumDbContext context, bool finalizeDownload)
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
                var pdfSource = await context.PDFSources.Where(s => s.Name == "سها").SingleOrDefaultAsync();
                if (pdfSource == null)
                {
                    PDFSource newSource = new PDFSource()
                    {
                        Name = "سها",
                        Url = "https://sohalibrary.com",
                        Description = "سامانهٔ جستجوی یکپارچهٔ نرم‌افزار سنا"
                    };
                    context.PDFSources.Add(newSource);
                    await context.SaveChangesAsync();
                    pdfSource = await context.PDFSources.Where(s => s.Name == "سها").SingleAsync();
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

                if (html.IndexOf("/item/download/") == -1)
                {
                    job.EndTime = DateTime.Now;
                    job.Status = ImportJobStatus.Failed;
                    job.Exception = $"/item/download/ not found in html source.";
                    context.Update(job);
                    await context.SaveChangesAsync();
                    return new RServiceResult<int>(0, job.Exception);
                }

                List<RTagValue> meta = new List<RTagValue>();
                int idxStart;
                int idx = html.IndexOf("branch-link");
                string firstHandSource = "";
                if (idx != -1)
                {
                    idxStart = html.IndexOf(">", idx);
                    if (idxStart != -1)
                    {
                        int idxEnd = html.IndexOf("<", idxStart);

                        if (idxEnd != -1)
                        {
                            firstHandSource = html.Substring(idxStart + 1, idxEnd - idxStart - 1);
                            meta.Add
                            (
                                 await TagHandler.PrepareAttribute(context, "First Hand Source", firstHandSource, 1)
                            );
                        }
                    }
                }


                idx = html.IndexOf("title-normal-for-book-name");
                if (idx == -1)
                {
                    job.EndTime = DateTime.Now;
                    job.Status = ImportJobStatus.Failed;
                    job.Exception = $"title-normal-for-book-name not found in {srcUrl}";
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



                idxStart = html.IndexOf("width-150");
                while (idxStart != -1)
                {
                    idxStart = html.IndexOf(">", idxStart);
                    if (idxStart == -1) break;
                    int idxEnd = html.IndexOf("<", idxStart);
                    if (idxEnd == -1) break;

                    string tagName = html.Substring(idxStart + 1, idxEnd - idxStart - 1).ToPersianNumbers().ApplyCorrectYeKe().Trim();

                    idxStart = html.IndexOf("value-name", idxEnd);
                    if (idxStart == -1) break;
                    idxStart = html.IndexOf(">", idxStart);
                    if (idxStart == -1) break;
                    idxEnd = html.IndexOf("</span>", idxStart);
                    if (idxEnd == -1) break;

                    string tagValue = html.Substring(idxStart + 1, idxEnd - idxStart - 1).Trim();
                    tagValue = Regex.Replace(tagValue, "<.*?>", string.Empty).Replace("\n", "").Replace("\r", "").Trim();

                    string tagValueCleaned = tagValue.Replace("\n", "").Replace("\r", "").Trim().ToPersianNumbers().ApplyCorrectYeKe();

                    if (tagName == "نویسنده")
                    {
                        model.AuthorsLine = tagValueCleaned;
                        tagName = "Author";

                        string[] authors = tagValueCleaned.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        for(int i = 0; i < authors.Length; i++)
                        {
                            string authorTrimmed = authors[i].Trim();
                            var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == authorTrimmed).FirstOrDefaultAsync();
                            if (existingAuthor != null)
                            {
                                if(i == 0)
                                {
                                    model.WriterId = existingAuthor.Id;
                                }
                                if(i == 1)
                                {
                                    model.Writer2Id = existingAuthor.Id;
                                }
                                if(i == 2)
                                {
                                    model.Writer3Id = existingAuthor.Id;
                                }
                                if(i == 3)
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
                                if(i == 0)
                                {
                                    model.WriterId = newAuthor.Id;
                                }
                                if(i == 1)
                                {
                                    model.Writer2Id = newAuthor.Id;
                                }
                                if(i == 2)
                                {
                                    model.Writer3Id = newAuthor.Id;
                                }
                                if(i == 3)
                                {
                                    model.Writer4Id = newAuthor.Id;
                                }
                            }
                        }

                        
                    }
                    if (tagName == "مترجم")
                    {
                        model.TranslatorsLine = tagValueCleaned;
                        model.IsTranslation = true;
                        tagName = "Translator";

                        string[] authors = tagValueCleaned.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for(int i = 0; i < authors.Length; i++)
                        {
                            string authorTrimmed = authors[i].Trim();
                            var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == authorTrimmed).FirstOrDefaultAsync();
                            if (existingAuthor != null)
                            {
                                if( i == 0 )
                                {
                                    model.TranslatorId = existingAuthor.Id;
                                }
                                if( i == 1)
                                {
                                    model.Translator2Id = existingAuthor.Id;
                                }
                                if( i == 2)
                                {
                                    model.Translator3Id = existingAuthor.Id;
                                }
                                if( i == 3)
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

                                if(i == 0)
                                {
                                    model.TranslatorId = newAuthor.Id;
                                }
                                if(i == 1)
                                {
                                    model.Translator2Id = newAuthor.Id;
                                }
                                if(i == 2)
                                {
                                    model.Translator3Id = newAuthor.Id;
                                }
                                if(i == 3)
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
                        for(int i=0; i<authors.Length; i++)
                        {
                            string authorTrimmed = authors[i].Trim();
                            var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == authorTrimmed).FirstOrDefaultAsync();
                            if (existingAuthor != null)
                            {
                                if(i == 0)
                                {
                                    model.CollectorId = existingAuthor.Id;
                                }
                                if(i == 1) 
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
                    idxStart = html.IndexOf("width-150", idxEnd);

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
                        Description = "",
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
                            Description = "",
                            VolumeCount = 1,
                        };
                        context.MultiVolumePDFCollections.Add(newCollection);
                        await context.SaveChangesAsync();
                        model.MultiVolumePDFCollectionId = newCollection.Id;
                    }
                }

                idx = html.IndexOf("/item/download/");
                int idxQuote = html.IndexOf('"', idx);
                string downloadUrl = html.Substring(idx, idxQuote - idx);
                downloadUrl = "https://sohalibrary.com" + downloadUrl;


                model.OriginalFileUrl = downloadUrl;

                if(!finalizeDownload)
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
                            string fileName = (1 + await context.PDFBooks.MaxAsync(p => p.Id)).ToString().PadLeft(8, '0') + "-soha.pdf";
                            /*
                            string fileName = string.Empty;
                            if (result.Content.Headers.ContentDisposition != null)
                            {
                                fileName = System.Net.WebUtility.UrlDecode(result.Content.Headers.ContentDisposition.FileName).Replace("\"", "");
                            }

                            if (string.IsNullOrEmpty(fileName) || File.Exists(Path.Combine(_imageFileService.ImageStoragePath, fileName)))
                            {
                                fileName = downloadUrl.Substring(downloadUrl.LastIndexOf('/') + 1) + ".pdf";
                            }

                            if(fileName.Length > 70)
                            {
                                fileName = Path.GetFileNameWithoutExtension(fileName).Substring(0, 64) + ".pdf";
                            }
                            */
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
                                if(string.IsNullOrEmpty(res.ExceptionString))
                                {
                                    return new RServiceResult<int>(0, "ImportLocalPDFFileAsync result was null");
                                }

                                if (res.ExceptionString.Contains("duplicated pdf with checksum"))
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
        /// batch import soha library
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="finalizeDownload"></param>
        public void BatchImportSohaLibraryAsync(int start, int end, bool finalizeDownload)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                       (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                               {
                                   for (int nUrlIndex = start; nUrlIndex <= end; nUrlIndex++)
                                   {
                                       string srcUrl = $"https://sohalibrary.com/item/view/{nUrlIndex}";
                                       if (
                                        (await context.PDFBooks.Where(a => a.OriginalSourceUrl == srcUrl).SingleOrDefaultAsync())
                                        !=
                                        null
                                        )
                                       {
                                           continue;
                                       }

                                       try
                                       {

                                           await ImportSohaLibraryUrlAsync(srcUrl, context, finalizeDownload);
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
