using FluentFTP;
using FluentFTP.Client.BaseClient;
using ganjoor;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RMuseum.Models.PDFLibrary;
using RMuseum.Models.PDFLibrary.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace RMuseum.Services.Implementation
{
    public partial class PDFLibraryService
    {
        /// <summary>
        /// start importing local pdf file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> StartImportingLocalPDFAsync(NewPDFBookViewModel model)
        {
            try
            {
                if (model == null)
                {
                    return new RServiceResult<bool>(false, "model == null");
                }
                if (!File.Exists(model.LocalImportingPDFFilePath))
                {
                    return new RServiceResult<bool>(false, $"file does not exist! : {model.LocalImportingPDFFilePath}");
                }
                string fileChecksum = PoemAudio.ComputeCheckSum(model.LocalImportingPDFFilePath);
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.Pdf && j.SrcContent == fileChecksum && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing pdf file {model.LocalImportingPDFFilePath} (duplicated checksum: {fileChecksum})");
                }
                _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                            async token =>
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                {
                                    await ImportLocalPDFFileAsync(context, model, fileChecksum);
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
        private async Task<RServiceResult<PDFBook>> ImportLocalPDFFileAsync(RMuseumDbContext context, NewPDFBookViewModel model, string fileChecksum)
        {
            try
            {
                var pdfRes = await ImportLocalPDFFileAsync(context, model.BookId, model.MultiVolumePDFCollectionId, model.VolumeOrder, model.LocalImportingPDFFilePath, model.OriginalSourceUrl, model.SkipUpload, fileChecksum);
                if (pdfRes.Result != null)
                {
                    var pdfBook = pdfRes.Result;
                    pdfBook.Title = model.Title;
                    pdfBook.SubTitle = model.SubTitle;
                    pdfBook.AuthorsLine = model.AuthorsLine;
                    pdfBook.ISBN = model.ISBN;
                    pdfBook.Description = model.Description;
                    pdfBook.IsTranslation = model.IsTranslation;
                    pdfBook.TranslatorsLine = model.TranslatorsLine;
                    pdfBook.TitleInOriginalLanguage = model.TitleInOriginalLanguage;
                    pdfBook.PublisherLine = model.PublisherLine;
                    pdfBook.PublishingDate = model.PublishingDate;
                    pdfBook.PublishingLocation = model.PublishingLocation;
                    pdfBook.PublishingNumber = model.PublishingNumber == 0 ? null : model.PublishingNumber;
                    pdfBook.ClaimedPageCount = model.ClaimedPageCount == 0 ? null : model.ClaimedPageCount;
                    pdfBook.OriginalSourceName = model.OriginalSourceName;
                    pdfBook.OriginalFileUrl = model.OriginalFileUrl;
                    pdfBook.PDFSourceId = model.PDFSourceId;
                    pdfBook.BookScriptType = model.BookScriptType;
                    List<AuthorRole> roles = new List<AuthorRole>();
                    if (model.WriterId != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.WriterId).SingleAsync(),
                            Role = "نویسنده",
                        });
                    }
                    if (model.Writer2Id != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.Writer2Id).SingleAsync(),
                            Role = "نویسنده",
                        });
                    }
                    if (model.Writer3Id != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.Writer3Id).SingleAsync(),
                            Role = "نویسنده",
                        });
                    }
                    if (model.Writer4Id != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.Writer4Id).SingleAsync(),
                            Role = "نویسنده",
                        });
                    }
                    if (model.TranslatorId != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.TranslatorId).SingleAsync(),
                            Role = "مترجم",
                        });
                    }
                    if (model.Translator2Id != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.Translator2Id).SingleAsync(),
                            Role = "مترجم",
                        });
                    }
                    if (model.Translator3Id != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.Translator3Id).SingleAsync(),
                            Role = "مترجم",
                        });
                    }
                    if (model.Translator4Id != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.Translator4Id).SingleAsync(),
                            Role = "مترجم",
                        });
                    }
                    if (model.CollectorId != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.CollectorId).SingleAsync(),
                            Role = "مصحح",
                        });
                    }
                    if (model.Collector2Id != null)
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.Collector2Id).SingleAsync(),
                            Role = "مصحح",
                        });
                    }
                    if (model.OtherContributerId != null && !string.IsNullOrEmpty(model.OtherContributerRole))
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.OtherContributerId).SingleAsync(),
                            Role = model.OtherContributerRole,
                        });
                    }
                    if (model.OtherContributer2Id != null && !string.IsNullOrEmpty(model.OtherContributer2Role))
                    {
                        roles.Add(new AuthorRole()
                        {
                            Author = await context.Authors.Where(a => a.Id == model.OtherContributer2Id).SingleAsync(),
                            Role = model.OtherContributer2Role,
                        });
                    }
                    if (roles.Count > 0)
                    {
                        pdfBook.Contributers = roles;
                    }
                    context.Update(pdfBook);
                    await context.SaveChangesAsync();

                    return new RServiceResult<PDFBook>(pdfBook);
                }
                return pdfRes;
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFBook>(null, exp.ToString());
            }
        }

        private async Task<RServiceResult<PDFBook>> ImportLocalPDFFileAsync(RMuseumDbContext context, int bookId, int? volumeId, int volumeOrder, string filePath, string srcUrl, bool skipUpload, string fileChecksum)

        {
            try
            {
                
                ImportJob job = new ImportJob()
                {
                    JobType = JobType.Pdf,
                    SrcContent = fileChecksum,
                    ResourceNumber = filePath,
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

                if (volumeId == 0) volumeId = null;
               
                
                if (!string.IsNullOrEmpty(srcUrl))
                {
                    if (
                        (await context.PDFBooks.Where(a => a.OriginalSourceUrl == srcUrl).SingleOrDefaultAsync())
                        !=
                        null
                        )
                    {
                        job.EndTime = DateTime.Now;
                        job.Status = ImportJobStatus.Failed;
                        job.Exception = $"duplicated srcUrl '{srcUrl}'";
                        context.Update(job);
                        await context.SaveChangesAsync();
                        return new RServiceResult<PDFBook>(null, job.Exception);
                    }
                }
                if (
                (await context.PDFBooks.Where(a => a.FileMD5CheckSum == fileChecksum).SingleOrDefaultAsync())
                !=
                null
                )
                {
                    job.EndTime = DateTime.Now;
                    job.Status = ImportJobStatus.Failed;
                    job.Exception = $"duplicated pdf with checksum '{fileChecksum}'";
                    context.Update(job);
                    await context.SaveChangesAsync();
                    return new RServiceResult<PDFBook>(null, job.Exception);
                }
               
                try
                {
                    var folderNumber = 1 + await context.PDFBooks.CountAsync();
                    while (Directory.Exists(Path.Combine(_imageFileService.ImageStoragePath, $"pdfbook-{folderNumber}")))
                    {
                        folderNumber++;
                    }
                    Directory.CreateDirectory(Path.Combine(_imageFileService.ImageStoragePath, $"pdfbook-{folderNumber}"));
                    PDFBook pdfBook = new PDFBook()
                    {
                        Status = PublishStatus.Draft,
                        DateTime = DateTime.Now,
                        LastModified = DateTime.Now,
                        FileMD5CheckSum = fileChecksum,
                        OriginalSourceUrl = srcUrl,
                        OriginalFileName = Path.GetFileName(filePath),
                        StorageFolderName = $"pdfbook-{folderNumber}",
                        BookId = bookId,
                        VolumeOrder = volumeOrder,
                        MultiVolumePDFCollectionId = volumeId,
                    };
                    job.FriendlyUrl = pdfBook.StorageFolderName;
                    List<RTagValue> meta = new List<RTagValue>();
                    job.StartTime = DateTime.Now;
                    job.Status = ImportJobStatus.Running;
                    job.SrcContent = "";
                    context.Update(job);
                    await context.SaveChangesAsync();
                    List<PDFPage> pages = await _ImportAndReturnPDFJobImages(pdfBook, job, 0);
                    pdfBook.Tags = meta.ToArray();
                    pdfBook.Pages = pages.ToArray();
                    pdfBook.PageCount = pages.Count;
                    if (pages.Count == 0)
                    {
                        job.EndTime = DateTime.Now;
                        job.Status = ImportJobStatus.Failed;
                        job.Exception = "Pages.Count == 0";
                        context.Update(job);
                        await context.SaveChangesAsync();
                        return new RServiceResult<PDFBook>(null, job.Exception);
                    }
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        var pdfStorageResult = await _imageFileService.Add(null, fs, pdfBook.OriginalFileName, pdfBook.StorageFolderName, false, "application/pdf");
                        if (!string.IsNullOrEmpty(pdfStorageResult.ExceptionString))
                        {
                            job.EndTime = DateTime.Now;
                            job.Status = ImportJobStatus.Failed;
                            job.Exception = $"pdfStorageResult.ExceptionString: {pdfStorageResult.ExceptionString}";
                            job.EndTime = DateTime.Now;
                            context.Update(job);
                            await context.SaveChangesAsync();
                            return new RServiceResult<PDFBook>(null, job.Exception);
                        }
                        pdfBook.PDFFile = pdfStorageResult.Result;
                    }
                    await context.PDFBooks.AddAsync(pdfBook);
                    await context.SaveChangesAsync();
                    var book = await context.Books.Where(b => b.Id == bookId).SingleAsync();
                    if (book.CoverImageId == null)
                    {
                        book.CoverImage = RImage.DuplicateExcludingId(pdfBook.CoverImage);
                        context.Update(book);
                        await context.SaveChangesAsync();
                    }
                    var resFTPUpload = await _UploadPDFBookToExternalServer(pdfBook, context, skipUpload);
                    if (!string.IsNullOrEmpty(resFTPUpload.ExceptionString))
                    {
                        job.EndTime = DateTime.Now;
                        job.Status = ImportJobStatus.Failed;
                        job.Exception = $"UploadArtifactToExternalServer: {resFTPUpload.ExceptionString}";
                        job.EndTime = DateTime.Now;
                        context.Update(job);
                        await context.SaveChangesAsync();
                        return new RServiceResult<PDFBook>(null, job.Exception);
                    }
                    job.ProgressPercent = 100;
                    job.Status = ImportJobStatus.Succeeded;
                    job.EndTime = DateTime.Now;
                    context.Update(job);
                    await context.SaveChangesAsync();
                    return new RServiceResult<PDFBook>(pdfBook);
                }
                catch (Exception exp)
                {
                    job.EndTime = DateTime.Now;
                    job.Status = ImportJobStatus.Failed;
                    job.Exception = exp.ToString();
                    job.EndTime = DateTime.Now;
                    context.Update(job);
                    await context.SaveChangesAsync();
                    return new RServiceResult<PDFBook>(null, job.Exception);
                }
            }
            catch(Exception e)
            {
                return new RServiceResult<PDFBook>(null, e.ToString());
            }
        }
        private async Task<List<PDFPage>> _ImportAndReturnPDFJobImages(PDFBook pdfBook, ImportJob job, int order)
        {
            List<PDFPage> pages = new List<PDFPage>();
            string pdfFilePath = job.ResourceNumber;
            string intermediateFolder = Path.Combine(Path.GetDirectoryName(pdfFilePath), Path.GetFileNameWithoutExtension(pdfFilePath));
            Directory.CreateDirectory(intermediateFolder);
            List<string> fileNames = new List<string>();
            int imageOrder = 1;
            using (FileStream fs = File.OpenRead(pdfFilePath))
            {
                var skBitmaps = PDFtoImage.Conversion.ToImages(fs);
                foreach (var skBitmap in skBitmaps)
                {
                    string outFileName = Path.Combine(intermediateFolder, $"{imageOrder}".PadLeft(4, '0') + ".jpg");
                    using (FileStream fsOut = File.OpenWrite(outFileName))
                    {
                        skBitmap.Encode(fsOut, SkiaSharp.SKEncodedImageFormat.Jpeg, 90);
                    }
                    fileNames.Add(outFileName);
                    imageOrder++;
                }
            }
            foreach (string fileName in fileNames)
            {
                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                {
                    job.ProgressPercent = order * 100 / (decimal)fileNames.Count;
                    importJobUpdaterDb.Update(job);
                    await importJobUpdaterDb.SaveChangesAsync();
                }
                order++;
                PDFPage page = new PDFPage()
                {
                    PageNumber = order,
                    Description = "",
                    Tags = new RTagValue[] { },
                    LastModified = DateTime.Now
                };
                string thumbnailImageName = $"{page.PageNumber}".PadLeft(5, '0') + ".jpg";
                string thumbnailPath = Path.Combine(Path.Combine(_imageFileService.ImageStoragePath, pdfBook.StorageFolderName), thumbnailImageName);
                if (File.Exists(thumbnailPath))
                {
                    File.Delete(thumbnailPath);
                }
                using (Image img = Image.FromFile(fileName))
                {
                    int imageWidth = img.Width;
                    int imageHeight = img.Height;
                    int thumbnailImageWidth = ThumbnailImageWidth;
                    int thumbnailImageHeight = ThumbnailImageWidth * imageHeight / imageWidth;
                    if (thumbnailImageHeight > ThumbnailImageMaxHeight)
                    {
                        thumbnailImageHeight = ThumbnailImageMaxHeight;
                        thumbnailImageWidth = thumbnailImageHeight * imageWidth / imageHeight;
                    }
                    //روی خود img تأثیر می گذارد
                    // به احتمال قوی داریم روی تصویر resize کار می‌کنیم
                    Image thumbnail = new Bitmap(thumbnailImageWidth, thumbnailImageHeight);
                    using (Graphics gThumbnail = Graphics.FromImage(thumbnail))
                    {
                        gThumbnail.DrawImage(img, 0, 0, thumbnailImageWidth, thumbnailImageHeight);
                    }
                    using (MemoryStream msThumbnail = new MemoryStream())
                    {
                        thumbnail.Save(msThumbnail, ImageFormat.Jpeg);
                        msThumbnail.Seek(0, SeekOrigin.Begin);
                        RServiceResult<RImage> picture = await _imageFileService.Add(null, msThumbnail, thumbnailImageName, pdfBook.StorageFolderName);
                        if (picture.Result == null)
                        {
                            throw new Exception($"_imageFileService.Add : {picture.ExceptionString}");
                        }
                        page.ThumbnailImage = picture.Result;
                        if (page.PageNumber == 1)
                        {
                            pdfBook.CoverImage = RImage.DuplicateExcludingId(picture.Result);
                        }
                    }
                }
                pages.Add(page);
            }
            foreach (var fileName in fileNames)
            {
                File.Delete(fileName);
            }
            Directory.Delete(intermediateFolder, true);
            return pages;
        }
        private async Task<RServiceResult<bool>> _UploadPDFBookToExternalServer(PDFBook book, RMuseumDbContext context, bool skipUpload)
        {
            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
            var job = (await jobProgressServiceEF.NewJob("_UploadArtifactToExternalServer", $"Uploading {book.StorageFolderName}")).Result;
            try
            {
                if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                {
                    var ftpClient = new AsyncFtpClient
                    (
                        Configuration.GetSection("ExternalFTPServer")["Host"],
                        Configuration.GetSection("ExternalFTPServer")["Username"],
                        Configuration.GetSection("ExternalFTPServer")["Password"]
                    );
                    if (!skipUpload)
                    {
                        ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                        await ftpClient.AutoConnect();
                        ftpClient.Config.RetryAttempts = 3;
                    }
                    var localPDFFilePath = _imageFileService.GetImagePath(book.PDFFile).Result;
                    var remotePDFFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/{book.StorageFolderName}/{Path.GetFileName(localPDFFilePath)}";
                    if (!skipUpload)
                    {
                        await jobProgressServiceEF.UpdateJob(job.Id, 0, localPDFFilePath);
                        await ftpClient.UploadFile(localPDFFilePath, remotePDFFilePath, createRemoteDir: true);
                    }
                    var localCoverImageFilePath = _imageFileService.GetImagePath(book.CoverImage).Result;
                    var remoteCoverImageFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/{book.StorageFolderName}/{Path.GetFileName(localCoverImageFilePath)}";
                    if (!skipUpload)
                    {
                        await jobProgressServiceEF.UpdateJob(job.Id, 0, localCoverImageFilePath);
                        await ftpClient.UploadFile(localCoverImageFilePath, remoteCoverImageFilePath, createRemoteDir: true);
                    }
                    book.ExternalPDFFileUrl = remotePDFFilePath;
                    book.ExtenalCoverImageUrl = remoteCoverImageFilePath;
                    context.Update(book);
                    foreach (var item in book.Pages)
                    {
                        var localFilePath = _imageFileService.GetImagePath(item.ThumbnailImage).Result;
                        item.ExtenalThumbnailImageUrl = $"{Configuration.GetSection("ExternalFTPServer")["RootUrl"]}/{book.StorageFolderName}/{Path.GetFileName(localFilePath)}";
                        context.Update(item);
                        if (!skipUpload)
                        {
                            await jobProgressServiceEF.UpdateJob(job.Id, 0, localFilePath);
                            var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/{book.CoverImage.FolderName}/{Path.GetFileName(localFilePath)}";
                            await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);
                        }
                    }
                    if (!skipUpload)
                    {
                        await ftpClient.Disconnect();
                    }
                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
        private void FtpClient_ValidateCertificate(BaseFtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }
        /// <summary>
        /// عرض تصویر بندانگشتی
        /// </summary>
        protected int ThumbnailImageWidth { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["ThumbnailImageWidth"]}"); } }
        /// <summary>
        /// طول تصویر بندانگشتی
        /// </summary>
        protected int ThumbnailImageMaxHeight { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["ThumbnailMaxHeight"]}"); } }
    }
}
