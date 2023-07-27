using DNTPersianUtils.Core;
using ganjoor;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace RMuseum.Services.Implementation
{
    public partial class PDFLibraryService
    {
        private async Task<RServiceResult<bool>> StartImportingSohaLibraryUrlAsync(string srcUrl)
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
                        .Where(j => j.JobType == JobType.Pdf && j.SrcContent == ("scrapping ..." + srcUrl) && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
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
                                       if (idx != -1)
                                       {
                                           idxStart = html.IndexOf(">", idx);
                                           if (idxStart != -1)
                                           {
                                               int idxEnd = html.IndexOf("<", idxStart);

                                               if (idxEnd != -1)
                                               {
                                                   meta.Add
                                                   (
                                                        await TagHandler.PrepareAttribute(context, "First Hand Source", html.Substring(idxStart + 1, idxEnd - idxStart - 1), 1)
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
                                               model.Title = model.Title.Trim();
                                           }
                                       }

                                       string bookTitle = model.Title;
                                       int volumeNumber = 0;
                                       if(bookTitle.Contains("ـ ج"))
                                       {
                                           bookTitle = bookTitle.Substring(0, model.Title.IndexOf("ـ ج") -1 );
                                           int.TryParse(model.Title.Substring(model.Title.IndexOf("ـ ج") + "ـ ج".Length).Trim(), out volumeNumber);
                                       }

                                       bookTitle = bookTitle.ToPersianNumbers().ApplyCorrectYeKe().Trim();
                                       model.Title = model.Title.ToPersianNumbers().ApplyCorrectYeKe().Trim();

                                       var book = await context.Books.AsNoTracking().Where(b => b.Name == bookTitle).FirstOrDefaultAsync();
                                       if(book != null)
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

                                       if(volumeNumber != 0)
                                       {
                                           MultiVolumePDFCollection collection = await context.MultiVolumePDFCollections.Where(v => v.Name == bookTitle && v.BookId == model.BookId).SingleOrDefaultAsync();
                                           if(collection != null)
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

                                           string tagValue = html.Substring(idxStart + 1, idxEnd - idxStart - 1);
                                           tagValue = Regex.Replace(tagValue, "<.*?>", string.Empty).Trim();

                                           string tagValueCleaned = tagValue.ToPersianNumbers().ApplyCorrectYeKe();

                                           if (tagName == "نویسنده")
                                           {
                                               model.AuthorsLine = tagValueCleaned;
                                               tagName = "Author";

                                               var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == tagValueCleaned).FirstOrDefaultAsync();
                                               if (existingAuthor != null)
                                               {
                                                   model.WriterId = existingAuthor.Id;
                                               }
                                               else
                                               {
                                                   var newAuthor = new Author()
                                                   {
                                                       Name = tagValueCleaned
                                                   };
                                                   context.Authors.Add(newAuthor);
                                                   await context.SaveChangesAsync();
                                                   model.WriterId = newAuthor.Id;
                                               }
                                           }
                                           if (tagName == "مترجم")
                                           {
                                               model.TranslatorsLine = tagValueCleaned;
                                               model.IsTranslation = true;
                                               tagName = "Translator";

                                               var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == tagValueCleaned).FirstOrDefaultAsync();
                                               if (existingAuthor != null)
                                               {
                                                   model.TranslatorId = existingAuthor.Id;
                                               }
                                               else
                                               {
                                                   var newAuthor = new Author()
                                                   {
                                                       Name = tagValueCleaned
                                                   };
                                                   context.Authors.Add(newAuthor);
                                                   await context.SaveChangesAsync();
                                                   model.TranslatorId = newAuthor.Id;
                                               }
                                           }
                                           if (tagName == "مصحح")
                                           {
                                               tagName = "Collector";

                                               var existingAuthor = await context.Authors.AsNoTracking().Where(a => a.Name == tagValueCleaned).FirstOrDefaultAsync();
                                               if (existingAuthor != null)
                                               {
                                                   model.CollectorId = existingAuthor.Id;
                                               }
                                               else
                                               {
                                                   var newAuthor = new Author()
                                                   {
                                                       Name = tagValueCleaned
                                                   };
                                                   context.Authors.Add(newAuthor);
                                                   await context.SaveChangesAsync();
                                                   model.CollectorId = newAuthor.Id;
                                               }
                                           }
                                           if (tagName == "زبان")
                                           {
                                               model.Language = tagValueCleaned;
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
                                                   if (int.TryParse(tagValue, out int v))
                                                   {
                                                       model.ClaimedPageCount = v;
                                                   }
                                               }
                                               tagName = "Page Count";
                                           }

                                           meta.Add
                                                   (
                                                        await TagHandler.PrepareAttribute(context, tagName, tagValueCleaned, 1)
                                                   );
                                           idxStart = html.IndexOf("width-150", idxEnd);

                                       }

                                       idx = html.IndexOf("/item/download/");
                                       int idxQuote = html.IndexOf('"', idx);
                                       string downloadUrl = html.Substring(idx, idxQuote - idx);
                                       downloadUrl = "https://sohalibrary.com" + downloadUrl;


                                       model.OriginalFileUrl = downloadUrl;

                                       using (var client = new HttpClient())
                                       {
                                           using (var result = await client.GetAsync(downloadUrl))
                                           {
                                               if (result.IsSuccessStatusCode)
                                               {
                                                   string fileName = string.Empty;
                                                   if (result.Content.Headers.ContentDisposition != null)
                                                   {
                                                       fileName = System.Net.WebUtility.UrlDecode(result.Content.Headers.ContentDisposition.FileName).Replace("\"", "");
                                                   }

                                                   if (string.IsNullOrEmpty(fileName) || File.Exists(Path.Combine(_imageFileService.ImageStoragePath, fileName)))
                                                   {
                                                       fileName = downloadUrl.Substring(downloadUrl.LastIndexOf('/') + 1) + ".pdf";
                                                   }

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
                                                       if(pdf.Tags.Count > 0)
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
                                                   }

                                               }
                                               else
                                               {
                                                   job.EndTime = DateTime.Now;
                                                   job.Status = ImportJobStatus.Failed;
                                                   job.Exception = $"Http result is not ok ({result.StatusCode}) for {downloadUrl}";
                                                   context.Update(job);
                                                   await context.SaveChangesAsync();
                                                   return;
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
