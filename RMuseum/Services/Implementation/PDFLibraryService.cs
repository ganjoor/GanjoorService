using ganjoor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.ImportJob;
using RMuseum.Models.PDFLibrary;
using RMuseum.Models.PDFLibrary.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// PDF Library Services
    /// </summary>
    public partial class PDFLibraryService : IPDFLibraryService
    {
        /// <summary>
        /// get pdf book by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PDFBook>> GetPDFBookByIdAsync(int id, PublishStatus[] statusArray)
        {
            try
            {
                var pdfBook = await _context.PDFBooks.AsNoTracking()
                            .Include(b => b.Book)
                            .Include(b => b.PDFFile)
                            .Include(b => b.MultiVolumePDFCollection)
                            .Include(b => b.Contributers)
                            .Include(b => b.Tags)
                            .Include(b => b.Pages)
                            .Where(b => statusArray.Contains(b.Status) && b.Id == id)
                            .SingleOrDefaultAsync();
                return new RServiceResult<PDFBook>(pdfBook);

            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFBook>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get all pdfbooks (including CoverImage info but not pages or tagibutes info)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetAllPDFBooks(PagingParameterModel paging, PublishStatus[] statusArray)
        {
            var source =
                 _context.PDFBooks.AsNoTracking()
                 .Include(a => a.CoverImage)
                 .Where(a => statusArray.Contains(a.Status))
                .OrderByDescending(t => t.DateTime)
                .AsQueryable();
            (PaginationMetadata PagingMeta, PDFBook[] Books) paginatedResult =
                await QueryablePaginator<PDFBook>.Paginate(source, paging);
            return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>(paginatedResult);
        }

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
                                        List<AuthorRole> roles = new List<AuthorRole>();
                                        if (model.WriterId != null && model.WriterId != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.WriterId).SingleAsync(),
                                                Role = "نویسنده",
                                            });
                                        }
                                        if (model.Writer2Id != null && model.Writer2Id != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.Writer2Id).SingleAsync(),
                                                Role = "نویسنده",
                                            });
                                        }
                                        if (model.Writer3Id != null && model.Writer3Id != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.Writer3Id).SingleAsync(),
                                                Role = "نویسنده",
                                            });
                                        }
                                        if (model.Writer4Id != null && model.Writer4Id != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.Writer4Id).SingleAsync(),
                                                Role = "نویسنده",
                                            });
                                        }
                                        if (model.TranslatorId != null && model.TranslatorId != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.TranslatorId).SingleAsync(),
                                                Role = "مترجم",
                                            });
                                        }
                                        if (model.Translator2Id != null && model.Translator2Id != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.Translator2Id).SingleAsync(),
                                                Role = "مترجم",
                                            });
                                        }
                                        if (model.Translator3Id != null && model.Translator3Id != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.Translator3Id).SingleAsync(),
                                                Role = "مترجم",
                                            });
                                        }
                                        if (model.Translator4Id != null && model.Translator4Id != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.Translator4Id).SingleAsync(),
                                                Role = "مترجم",
                                            });
                                        }
                                        if (model.CollectorId != null && model.CollectorId != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.CollectorId).SingleAsync(),
                                                Role = "مصحح",
                                            });
                                        }
                                        if (model.Collector2Id != null && model.Collector2Id != 0)
                                        {
                                            roles.Add(new AuthorRole()
                                            {
                                                Author = await context.Authors.Where(a => a.Id == model.Collector2Id).SingleAsync(),
                                                Role = "مصحح",
                                            });
                                        }
                                        if (roles.Count > 0)
                                        {
                                            pdfBook.Contributers = roles;
                                        }
                                        context.Update(pdfBook);
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

        /// <summary>
        /// edit pdf book master record
        /// </summary>
        /// <param name="model"></param>
        /// <param name="canChangeStatusToAwaiting"></param>
        /// <param name="canPublish"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PDFBook>> EditPDFBookMasterRecord(PDFBook model, bool canChangeStatusToAwaiting, bool canPublish)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Title))
                {
                    return new RServiceResult<PDFBook>(null, "Name could not be empty.");
                }

                PDFBook pdfBook =
                     await _context.PDFBooks
                     .Where(a => a.Id == model.Id)
                    .SingleOrDefaultAsync();


                if (pdfBook != null)
                {
                    if (pdfBook.Status != model.Status)
                    {
                        if (!canChangeStatusToAwaiting)
                        {
                            return new RServiceResult<PDFBook>(null, "User should be able to change status to Awaiting to complete this operation.");
                        }

                        if (
                            !
                            (
                            (pdfBook.Status == PublishStatus.Draft && model.Status == PublishStatus.Awaiting)
                            ||
                            (pdfBook.Status == PublishStatus.Awaiting && model.Status == PublishStatus.Draft)
                            )
                            )
                        {
                            if (!canPublish)
                            {
                                return new RServiceResult<PDFBook>(null, "User should have Publish permission to complete this operation.");
                            }
                        }
                    }

                    pdfBook.Status = model.Status;
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
                    pdfBook.LastModified = DateTime.Now;

                    _context.Update(pdfBook);
                    await _context.SaveChangesAsync();
                }
                return new RServiceResult<PDFBook>(pdfBook);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFBook>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Copy PDF Book Cover Image From Page Thumbnail image
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="pdfpageId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetPDFBookCoverImageFromPage(int pdfBookId, int pdfpageId)
        {
            try
            {
                PDFBook pdfBook = await _context
                    .PDFBooks.Where(a => a.Id == pdfBookId)
                    .SingleOrDefaultAsync();
                if (pdfBook == null)
                    return new RServiceResult<bool>(false, "pdf book not found.");

                PDFPage pdfPage = await _context.PDFPages.AsNoTracking().Include(p => p.ThumbnailImage).Where(p => p.Id == pdfpageId).SingleOrDefaultAsync();

                if (pdfPage == null)
                    return new RServiceResult<bool>(false, "Page not found.");

                pdfBook.CoverImage = RImage.DuplicateExcludingId(pdfPage.ThumbnailImage);

                pdfBook.LastModified = DateTime.Now;

                _context.Update(pdfBook);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// an incomplete prototype for removing PDF books
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RemovePDFBookAsync(int pdfBookId)
        {
            try
            {
                PDFBook record = await _context.PDFBooks
                        .Include(a => a.Pages).ThenInclude(i => i.ThumbnailImage)
                        .Include(a => a.Pages).ThenInclude(i => i.Tags)
                        .Include(a => a.Tags)
                        .Where(a => a.Id == pdfBookId)
                        .SingleOrDefaultAsync();
                if (record == null)
                {
                    return new RServiceResult<bool>(false, "PDFBook not found.");
                }
                if (record.Status == PublishStatus.Published)
                {
                    return new RServiceResult<bool>(false, "Can not delete published pdf book");
                }

                

                string artifactFolder = Path.Combine(_imageFileService.ImageStoragePath, record.StorageFolderName);

                foreach (PDFPage pages in record.Pages)
                {
                    _context.TagValues.RemoveRange(pages.Tags);
                }

                _context.RemoveRange(record.Pages);
                _context.TagValues.RemoveRange(record.Tags);
                _context.Remove(record);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(artifactFolder))
                {
                    try
                    {
                        Directory.Delete(artifactFolder, true);
                    }
                    catch
                    {
                        //ignore errors
                    }
                }
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// add author
        /// </summary>
        /// <param name="author"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Author>> AddAuthorAsync(Author author)
        {
            try
            {
                _context.Add(author);
                await _context.SaveChangesAsync();
                return new RServiceResult<Author>(author);
            }
            catch (Exception exp)
            {
                return new RServiceResult<Author>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get author by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Author>> GetAuthorByIdAsync(int id)
        {
            try
            {
                return new RServiceResult<Author>(await _context.Authors.AsNoTracking().Where(a => a.Id == id).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<Author>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get authors
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, Author[] Authors)>> GetAuthorsAsync(PagingParameterModel paging, string authorName)
        {
            var source =
                 _context.Authors
                 .Where(a => string.IsNullOrEmpty(authorName) || (authorName.Contains(a.Name) || (!string.IsNullOrEmpty(a.NameInOriginalLanguage) && authorName.Contains(a.NameInOriginalLanguage))))
                .AsQueryable();
            (PaginationMetadata PagingMeta, Author[] Items) paginatedResult =
                await QueryablePaginator<Author>.Paginate(source, paging);
            return new RServiceResult<(PaginationMetadata PagingMeta, Author[] Authors)>(paginatedResult);
        }

        /// <summary>
        /// add book
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Book>> AddBookAsync(Book book)
        {
            try
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return new RServiceResult<Book>(book);
            }
            catch (Exception exp)
            {
                return new RServiceResult<Book>(null, exp.ToString());
            }
        }

        /// <summary>
        /// add multi volume pdf collection
        /// </summary>
        /// <param name="multiVolumePDFCollection"></param>
        /// <returns></returns>
        public async Task<RServiceResult<MultiVolumePDFCollection>> AddMultiVolumePDFCollection(MultiVolumePDFCollection multiVolumePDFCollection)
        {
            try
            {
                _context.Add(multiVolumePDFCollection);
                await _context.SaveChangesAsync();
                return new RServiceResult<MultiVolumePDFCollection>(multiVolumePDFCollection);
            }
            catch (Exception exp)
            {
                return new RServiceResult<MultiVolumePDFCollection>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;
        /// <summary>
        /// Background Task Queue Instance
        /// </summary>
        protected readonly IBackgroundTaskQueue _backgroundTaskQueue;
        /// <summary>
        /// image file service
        /// </summary>
        protected readonly IImageFileService _imageFileService;
        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="imageFileService"></param>
        /// <param name="configuration"></param>
        public PDFLibraryService(RMuseumDbContext context, IBackgroundTaskQueue backgroundTaskQueue, IImageFileService imageFileService, IConfiguration configuration)
        {
            _context = context;
            _backgroundTaskQueue = backgroundTaskQueue;
            _imageFileService = imageFileService;
            Configuration = configuration;
        }
    }
}
