using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.PDFLibrary;
using RMuseum.Models.PDFLibrary.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System.Collections.Generic;
using System.Linq;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// PDF Library Services
    /// </summary>
    public partial class PDFLibraryService : IPDFLibraryService
    {

        /// <summary>
        /// start importing local pdf file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartImportingLocalPDF(NewPDFBookViewModel model)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                            {
                                var pdfRes = await ImportLocalPDFFileAsync(context, model.BookId, model.MultiVolumePDFCollectionId, model.VolumeOrder, model.LocalImportingPDFFilePath, model.OriginalSourceUrl, model.SkipUpload);
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
                                    pdfBook.PublishingNumber = model.PublishingNumber;
                                    pdfBook.ClaimedPageCount = model.ClaimedPageCount;
                                    pdfBook.OriginalSourceName = model.OriginalSourceName;
                                    pdfBook.OriginalFileUrl = model.OriginalFileUrl;
                                    List<AuthorRole> roles = new List<AuthorRole>();
                                    if(model.WriterId != null)
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
                                    if(model.TranslatorId != null)
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
                                    if(roles.Count > 0)
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
