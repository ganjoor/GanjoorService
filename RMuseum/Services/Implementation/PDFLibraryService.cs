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
                            .Include(b => b.PDFSource)
                            .Include(b => b.Contributers)
                            .Include(b => b.Tags)
                            .Include(b => b.Pages)
                            .Where(b => statusArray.Contains(b.Status) && b.Id == id)
                            .SingleOrDefaultAsync();
                if(pdfBook != null)
                {
                    if(pdfBook.Book != null)
                    {
                        pdfBook.Book.PDFBooks = await _context.PDFBooks.AsNoTracking()
                            .Include(a => a.CoverImage)
                            .Where(a => a.Status == PublishStatus.Published && a.BookId == id)
                           .OrderByDescending(t => t.Title).ToArrayAsync();
                    }
                    if(pdfBook.MultiVolumePDFCollection != null)
                    {
                        pdfBook.MultiVolumePDFCollection.PDFBooks = await _context.PDFBooks.AsNoTracking()
                            .Include(a => a.CoverImage)
                            .Where(a => a.Status == PublishStatus.Published && a.MultiVolumePDFCollectionId == id)
                           .OrderBy(t => t.VolumeOrder)
                           .ToArrayAsync();
                    }
                }
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
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetAllPDFBooksAsync(PagingParameterModel paging, PublishStatus[] statusArray)
        {
            try
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
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>((null, null), exp.ToString());
            }
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
                                        pdfBook.PDFSourceId = model.PDFSourceId;
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
                                        if(model.OtherContributerId != null && !string.IsNullOrEmpty(model.OtherContributerRole))
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
        public async Task<RServiceResult<PDFBook>> EditPDFBookMasterRecordAsync(PDFBook model, bool canChangeStatusToAwaiting, bool canPublish)
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
                    pdfBook.VolumeOrder = model.VolumeOrder;
                    pdfBook.MultiVolumePDFCollectionId = model.MultiVolumePDFCollectionId;
                    pdfBook.BookId = model.BookId;
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
        public async Task<RServiceResult<bool>> SetPDFBookCoverImageFromPageAsync(int pdfBookId, int pdfpageId)
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
        /// get tagged publish pdfbooks (including CoverImage info but not pages or tagibutes info) 
        /// </summary>
        /// <param name="tagUrl"></param>
        /// <param name="valueUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PDFBook[]>> GetPDFBookByTagValueAsync(string tagUrl, string valueUrl, PublishStatus[] statusArray)
        {
            try
            {
                RTag tag =
                        await _context.Tags
                        .Where(a => a.FriendlyUrl == tagUrl)
                    .SingleOrDefaultAsync();
                if (tag == null)
                    return new RServiceResult<PDFBook[]>(new PDFBook[] { });


                PDFBook[] taggedItems =
                    await _context.PDFBooks.Include(a => a.Tags)
                     .Include(a => a.CoverImage)
                    .Where(a => statusArray.Contains(a.Status) && a.Tags != null && a.Tags.Any(v => v.RTagId == tag.Id && v.FriendlyUrl == valueUrl))
                    .OrderByDescending(t => t.DateTime)
                    .AsNoTracking()
                    .ToArrayAsync();

                foreach (PDFBook taggedItem in taggedItems)
                    taggedItem.Tags = null;

                return new RServiceResult<PDFBook[]>(taggedItems);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFBook[]>(null, exp.ToString());
            }

        }

        /// <summary>
        /// add pdf book tag value
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="rTag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> TagPDFBookAsync(int pdfBookId, RTag rTag)
        {
            try
            {
                RTag type = await _context.Tags.Where(a => a.Id == rTag.Id).SingleOrDefaultAsync();

                PDFBook item = await _context.PDFBooks.Include(i => i.Tags).Where(i => i.Id == pdfBookId).SingleOrDefaultAsync();

                int order = item.Tags.Where(t => t.RTagId == type.Id).Count() == 0 ? 1 : item.Tags.Where(t => t.RTagId == type.Id).OrderByDescending(t => t.Order).FirstOrDefault().Order + 1;

                RTagValue tag =
                new RTagValue()
                {
                    Order = order,
                    Value = "",
                    ValueInEnglish = "",
                    ValueSupplement = "",
                    RTag = type,
                    Status = PublishStatus.Published
                };

                item.Tags.Add(tag);
                item.LastModified = DateTime.Now;
                _context.Update(item);
                await _context.SaveChangesAsync();

                return new RServiceResult<RTagValue>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagValue>(null, exp.ToString());
            }
        }

        /// <summary>
        /// remove pdf book tag value
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UnTagPDFBookAsync(int pdfBookId, Guid tagValueId)
        {
            try
            {
                PDFBook item = await _context.PDFBooks.Include(i => i.Tags).Where(i => i.Id == pdfBookId).SingleOrDefaultAsync();
                item.Tags.Remove(item.Tags.Where(t => t.Id == tagValueId).SingleOrDefault());
                item.LastModified = DateTime.Now;
                _context.Update(item);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// edit pdf book tag value
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="edited"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> EditPDFBookTagValueAsync(int pdfBookId, RTagValue edited, bool global)
        {
            try
            {
                if (string.IsNullOrEmpty(edited.Value))
                {
                    return new RServiceResult<RTagValue>(null, "Value could not be empty.");
                }

                PDFBook pdfBook =
                    await _context.PDFBooks
                     .Include(a => a.Tags)
                     .Where(a => a.Id == pdfBookId)
                    .SingleOrDefaultAsync();
                if (pdfBook == null)
                    return new RServiceResult<RTagValue>(null);

                RTagValue tag =
                    pdfBook.Tags.Where(a => a.Id == edited.Id)
                    .SingleOrDefault();


                if (tag != null)
                {
                    tag.Order = edited.Order;
                    tag.ValueSupplement = edited.ValueSupplement;
                    _context.Update(tag);

                    if (global)
                    {
                        RTagValue[] sameValueTags = await _context.TagValues.Where(v => v.Value == tag.Value && v.RTagId == tag.RTagId).ToArrayAsync();
                        foreach (RTagValue sameValueTag in sameValueTags)
                        {
                            sameValueTag.Value = edited.Value;
                            sameValueTag.ValueInEnglish = edited.ValueInEnglish;
                            sameValueTag.Status = edited.Status;
                            sameValueTag.FriendlyUrl = edited.FriendlyUrl;
                            _context.Update(sameValueTag);

                            RArtifactMasterRecord correspondingArtifact =
                                await _context.Artifacts.Include(a => a.Tags).Where(a => a.Tags.Contains(sameValueTag)).SingleOrDefaultAsync();
                            if (correspondingArtifact != null)
                            {
                                correspondingArtifact.LastModified = DateTime.Now;
                                _context.Update(correspondingArtifact);
                            }

                            RArtifactItemRecord correspondingItem =
                                await _context.Items.Include(a => a.Tags).Where(a => a.Tags.Contains(sameValueTag)).SingleOrDefaultAsync();
                            if (correspondingItem != null)
                            {
                                correspondingItem.LastModified = DateTime.Now;
                                _context.Update(correspondingItem);
                            }

                        }
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        tag.Value = edited.Value;
                        tag.ValueInEnglish = edited.ValueInEnglish;
                        tag.Order = edited.Order;
                        tag.FriendlyUrl = edited.FriendlyUrl;
                        tag.Status = edited.Status;
                        tag.ValueSupplement = edited.ValueSupplement;
                        _context.Update(tag);
                        pdfBook.LastModified = DateTime.Now;
                        _context.Update(pdfBook);
                        await _context.SaveChangesAsync();
                    }
                }
                return new RServiceResult<RTagValue>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagValue>(null, exp.ToString());
            }
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
        /// update author
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Author>> UpdateAuthorAsync(Author model)
        {
            try
            {
                var dbAuthor = await _context.Authors.Where(author => author.Id == model.Id).SingleOrDefaultAsync();
                dbAuthor.Name = model.Name;
                dbAuthor.NameInOriginalLanguage = model.NameInOriginalLanguage;
                dbAuthor.Bio = model.Bio;
                dbAuthor.ImageId = model.ImageId;
                dbAuthor.ExtenalImageUrl = model.ExtenalImageUrl;
                dbAuthor.LastModified = DateTime.Now;
                _context.Update(dbAuthor);
                await _context.SaveChangesAsync();
                return new RServiceResult<Author>(model);
            }
            catch (Exception exp)
            {
                return new RServiceResult<Author>(null, exp.ToString());
            }
        }

        /// <summary>
        /// delete author by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteAuthorAsync(int id)
        {
            try
            {
                var dbAuthor = await _context.Authors.Where(author => author.Id == id).SingleOrDefaultAsync();
                _context.Remove(dbAuthor);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
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
            try
            {
                var source =
                  _context.Authors
                  .Where(a => string.IsNullOrEmpty(authorName) || (authorName.Contains(a.Name) || (!string.IsNullOrEmpty(a.NameInOriginalLanguage) && authorName.Contains(a.NameInOriginalLanguage))))
                 .AsQueryable();
                (PaginationMetadata PagingMeta, Author[] Items) paginatedResult =
                    await QueryablePaginator<Author>.Paginate(source, paging);
                return new RServiceResult<(PaginationMetadata PagingMeta, Author[] Authors)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, Author[] Authors)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// add pdf book contributer
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddPDFBookContributerAsync(int pdfBookId, int authorId, string role)
        {
            try
            {
                role = role.Trim();
                var pdfBook = await _context.PDFBooks.Include(b => b.Contributers).ThenInclude(a => a.Author).Where(b => b.Id == pdfBookId).SingleAsync();
                if (pdfBook.Contributers.Any(a => a.Author.Id == authorId && a.Role == role))
                {
                    return new RServiceResult<bool>(false, "author contribution already added");
                }
                var author = await _context.Authors.AsNoTracking().Where(a => a.Id == authorId).SingleAsync();
                pdfBook.Contributers.Add(new AuthorRole()
                {
                    Author = author,
                    Role = role
                });
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
        /// remove contribution from pdf book
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="contributionId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePDFBookContributerAsync(int pdfBookId, int contributionId)
        {
            try
            {
                var pdfBook = await _context.PDFBooks.Include(b => b.Contributers).ThenInclude(a => a.Author).Where(b => b.Id == pdfBookId).SingleAsync();
                if (!pdfBook.Contributers.Any(a => a.Id == contributionId))
                {
                    return new RServiceResult<bool>(false, "author contribution not found.");
                }

                var contribution = pdfBook.Contributers.Where(a => a.Id == contributionId).Single();


                pdfBook.Contributers.Remove(contribution);
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
        /// get published pdf books by author
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetPublishedPDFBooksByAuthorAsync(PagingParameterModel paging, int authorId, string role)
        {
            try
            {
                var source =
                _context.PDFBooks.AsNoTracking()
                .Include(a => a.CoverImage).Include(a => a.Contributers).ThenInclude(c => c.Author)
                .Where(a => a.Status == PublishStatus.Published && a.Contributers.Any(a => a.Author.Id == authorId && (string.IsNullOrEmpty(role) || (!string.IsNullOrEmpty(role) && a.Role == role))))
               .AsQueryable();
                (PaginationMetadata PagingMeta, PDFBook[] Books) paginatedResult =
                    await QueryablePaginator<PDFBook>.Paginate(source, paging);
                return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// get published pdf books by author stats (group by role)
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>

        public async Task<RServiceResult<AuthorRoleCount[]>> GetPublishedPDFBookbyAuthorGroupedByRoleAsync(int authorId)
        {
            try
            {
                var books = await _context.PDFBooks.AsNoTracking().Include(b => b.Contributers).ThenInclude(c => c.Author)
                                        .Where(a => a.Status == PublishStatus.Published && a.Contributers.Any(a => a.Author.Id == authorId))
                                        .ToListAsync();
                Dictionary<string, int> roleCount = new Dictionary<string, int>();
                foreach (var book in books)
                {
                    foreach (var contributer in book.Contributers)
                    {
                        if (contributer.Author.Id == authorId)
                        {
                            if (roleCount.ContainsKey(contributer.Role))
                            {
                                roleCount[contributer.Role] = 1;
                            }
                            else
                            {
                                roleCount[contributer.Role]++;
                            }
                        }
                    }
                }

                List<AuthorRoleCount> authorRoles = new List<AuthorRoleCount>();
                foreach (var role in roleCount.Keys)
                {
                    authorRoles.Add
                        (
                        new AuthorRoleCount()
                        {
                            Role = role,
                            Count = roleCount[role]
                        }
                        );
                }
                return new RServiceResult<AuthorRoleCount[]>(authorRoles.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<AuthorRoleCount[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get all books
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>> GetAllBooksAsync(PagingParameterModel paging)
        {
            try
            {
                var source =
                _context.Books.AsNoTracking()
                .Include(a => a.CoverImage)
               .OrderByDescending(t => t.Name)
               .AsQueryable();
                (PaginationMetadata PagingMeta, Book[] Books) paginatedResult =
                    await QueryablePaginator<Book>.Paginate(source, paging);
                return new RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>((null, null), exp.ToString());
            }
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
        /// update book
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Book>> UpdateBookAsync(Book model)
        {
            try
            {
                var dbBook = await _context.Books.Where(b => b.Id == model.Id).SingleAsync();
                dbBook.Name = model.Name;
                dbBook.Description = model.Description;
                dbBook.CoverImageId = model.CoverImageId;
                dbBook.ExtenalCoverImageUrl = model.ExtenalCoverImageUrl;
                dbBook.LastModified = DateTime.Now;

                _context.Update(model);
                await _context.SaveChangesAsync();
                return new RServiceResult<Book>(model);
            }
            catch (Exception exp)
            {
                return new RServiceResult<Book>(null, exp.ToString());
            }
        }

        /// <summary>
        /// delete book
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteBookAsync(int id)
        {
            try
            {
                var dbBook = await _context.Books.Where(b => b.Id == id).SingleAsync();
                _context.Books.Remove(dbBook);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// add book author
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddBookAuthorAsync(int bookId, int authorId, string role)
        {
            try
            {
                role = role.Trim();
                var book = await _context.Books.Include(b => b.Authors).ThenInclude(a => a.Author).Where(b => b.Id == bookId).SingleAsync();
                if (book.Authors.Any(a => a.Author.Id == authorId && a.Role == role))
                {
                    return new RServiceResult<bool>(false, "author contribution already added.");
                }
                var author = await _context.Authors.AsNoTracking().Where(a => a.Id == authorId).SingleAsync();
                book.Authors.Add(new AuthorRole()
                {
                    Author = author,
                    Role = role
                });
                _context.Update(book);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// remove author from book
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="contributionId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteBookAuthorAsync(int bookId, int contributionId)
        {
            try
            {
                var book = await _context.Books.Include(b => b.Authors).ThenInclude(a => a.Author).Where(b => b.Id == bookId).SingleAsync();
                if (!book.Authors.Any(a => a.Id == contributionId))
                {
                    return new RServiceResult<bool>(false, "author contribution not found.");
                }

                var contribution = book.Authors.Where(a => a.Id == contributionId).Single();


                book.Authors.Remove(contribution);
                _context.Update(book);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Book>> GetBookByIdAsync(int id)
        {
            try
            {
                var book = await _context.Books.AsNoTracking()
                    .Include(b => b.CoverImage)
                    .Include(b => b.Authors).ThenInclude(a => a.Author)
                    .Include(b => b.Tags)
                    .Where(b => b.Id == id).SingleOrDefaultAsync();
                if(book != null)
                {
                    book.PDFBooks = await _context.PDFBooks.AsNoTracking()
                        .Include(a => a.CoverImage)
                        .Where(a => a.Status == PublishStatus.Published && a.BookId == id)
                       .OrderByDescending(t => t.Title).ToArrayAsync();
                }

                return new RServiceResult<Book>(book);
            }
            catch (Exception exp)
            {
                return new RServiceResult<Book>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get books by author
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>> GetBooksByAuthorAsync(PagingParameterModel paging, int authorId, string role)
        {
            try
            {
                var source =
                _context.Books.AsNoTracking()
                .Include(a => a.CoverImage).Include(a => a.Authors).ThenInclude(c => c.Author)
                .Where(a => a.Authors.Any(a => a.Author.Id == authorId && (string.IsNullOrEmpty(role) || (!string.IsNullOrEmpty(role) && a.Role == role))))
               .AsQueryable();
                (PaginationMetadata PagingMeta, Book[] Books) paginatedResult =
                    await QueryablePaginator<Book>.Paginate(source, paging);
                return new RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// get books by author stats (group by role)
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>

        public async Task<RServiceResult<AuthorRoleCount[]>> GetBookbyAuthorGroupedByRoleAsync(int authorId)
        {
            try
            {
                var books = await _context.Books.AsNoTracking().Include(b => b.Authors).ThenInclude(c => c.Author)
                                        .Where(a => a.Authors.Any(a => a.Author.Id == authorId))
                                        .ToListAsync();
                Dictionary<string, int> roleCount = new Dictionary<string, int>();
                foreach (var book in books)
                {
                    foreach (var contributer in book.Authors)
                    {
                        if (contributer.Author.Id == authorId)
                        {
                            if (roleCount.ContainsKey(contributer.Role))
                            {
                                roleCount[contributer.Role] = 1;
                            }
                            else
                            {
                                roleCount[contributer.Role]++;
                            }
                        }
                    }
                }

                List<AuthorRoleCount> authorRoles = new List<AuthorRoleCount>();
                foreach (var role in roleCount.Keys)
                {
                    authorRoles.Add
                        (
                        new AuthorRoleCount()
                        {
                            Role = role,
                            Count = roleCount[role]
                        }
                        );
                }
                return new RServiceResult<AuthorRoleCount[]>(authorRoles.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<AuthorRoleCount[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get book related pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="bookId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetBookRelatedPDFBooksAsync(PagingParameterModel paging, int bookId)
        {
            try
            {
                var source =
                _context.PDFBooks.AsNoTracking()
                .Include(a => a.CoverImage)
                .Where(a => a.Status == PublishStatus.Published && a.BookId == bookId)
               .OrderByDescending(t => t.Title)
               .AsQueryable();
                (PaginationMetadata PagingMeta, PDFBook[] Books) paginatedResult =
                    await QueryablePaginator<PDFBook>.Paginate(source, paging);
                return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>((null, null), exp.ToString());
            }
        }


        /// <summary>
        /// add multi volume pdf collection
        /// </summary>
        /// <param name="multiVolumePDFCollection"></param>
        /// <returns></returns>
        public async Task<RServiceResult<MultiVolumePDFCollection>> AddMultiVolumePDFCollectionAsync(MultiVolumePDFCollection multiVolumePDFCollection)
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
        /// update multi volume pdf collection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<MultiVolumePDFCollection>> UpdateMultiVolumePDFCollectionAsync(MultiVolumePDFCollection model)
        {
            try
            {
                var dbVolumes = await _context.MultiVolumePDFCollections.Where(x => x.Id == model.Id).SingleAsync();
                dbVolumes.Name = model.Name;
                dbVolumes.Description = model.Description;
                dbVolumes.VolumeCount = model.VolumeCount;
                dbVolumes.BookId = model.BookId;
                _context.Update(dbVolumes);
                await _context.SaveChangesAsync();
                return new RServiceResult<MultiVolumePDFCollection>(model);
            }
            catch (Exception exp)
            {
                return new RServiceResult<MultiVolumePDFCollection>(null, exp.ToString());
            }
        }

        /// <summary>
        /// delete multi volume pdf collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteMultiVolumePDFCollectionAsync(int id)
        {
            try
            {
                var dbVolumes = await _context.MultiVolumePDFCollections.Where(x => x.Id == id).SingleAsync();
                _context.Remove(dbVolumes);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get volumes by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<MultiVolumePDFCollection>> GetMultiVolumePDFCollectionByIdAsync(int id)
        {
            try
            {
                var volumes = await _context.MultiVolumePDFCollections.Include(v => v.Book).AsNoTracking().Where(x => x.Id == id).SingleOrDefaultAsync();
                if(volumes != null)
                {
                    volumes.PDFBooks = await _context.PDFBooks.AsNoTracking()
                        .Include(a => a.CoverImage)
                        .Where(a => a.Status == PublishStatus.Published && a.MultiVolumePDFCollectionId == id)
                       .OrderBy(t => t.VolumeOrder)
                       .ToArrayAsync();
                }
                return new RServiceResult<MultiVolumePDFCollection>(volumes);
            }
            catch (Exception exp)
            {
                return new RServiceResult<MultiVolumePDFCollection>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get volumes pdf books
        /// </summary>
        /// <param name="volumeId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PDFBook[]>> GetVolumesPDFBooks(int volumeId)
        {
            try
            {
                var books =
                await _context.PDFBooks.AsNoTracking()
                .Include(a => a.CoverImage)
                .Where(a => a.Status == PublishStatus.Published && a.MultiVolumePDFCollectionId == volumeId)
               .OrderBy(t => t.VolumeOrder)
               .ToArrayAsync();
                return new RServiceResult<PDFBook[]>(books);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFBook[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get pdf source by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PDFSource>> GetPDFSourceByIdAsync(int id)
        {
            try
            {
                return new RServiceResult<PDFSource>(await _context.PDFSources.AsNoTracking().Where(s => s.Id == id).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFSource>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get All PDF Sources
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<PDFSource[]>> GetPDFSourcesAsync()
        {
            try
            {
                return new RServiceResult<PDFSource[]>(await _context.PDFSources.AsNoTracking().ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFSource[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Add PDF Source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PDFSource>> AddPDFSourceAsync(PDFSource source)
        {
            try
            {
                _context.PDFSources.Add(source);
                await _context.SaveChangesAsync();
                return new RServiceResult<PDFSource>(source);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFSource>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update PDF Source
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PDFSource>> UpdatePDFSourceAsync(PDFSource model)
        {
            try
            {
                var dbSource = await _context.PDFSources.Where(s => s.Id == model.Id).SingleAsync();
                dbSource.Name = model.Name;
                dbSource.Description = model.Description;
                dbSource.Url = model.Url;
                _context.Update(dbSource);
                await _context.SaveChangesAsync();
                return new RServiceResult<PDFSource>(dbSource);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PDFSource>(null, exp.ToString());
            }
        }

        /// <summary>
        /// delete PDF Source
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePDFSourceAsync(int id)
        {
            try
            {
                var dbSource = await _context.PDFSources.Where(s => s.Id == id).SingleAsync();
                _context.Remove(dbSource);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get source pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetSourceRelatedPDFBooksAsync(PagingParameterModel paging, int sourceId)
        {
            try
            {
                var source =
                _context.PDFBooks.AsNoTracking()
                .Include(a => a.CoverImage)
                .Where(a => a.Status == PublishStatus.Published && a.PDFSourceId == sourceId)
               .OrderByDescending(t => t.Title)
               .AsQueryable();
                (PaginationMetadata PagingMeta, PDFBook[] Books) paginatedResult =
                    await QueryablePaginator<PDFBook>.Paginate(source, paging);
                return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>((null, null), exp.ToString());
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
