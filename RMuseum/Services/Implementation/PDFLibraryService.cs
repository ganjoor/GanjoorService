using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RSecurityBackend.Services;

namespace RMuseum.Services.Implementation
{
    public partial class PDFLibraryService
    {
        /// <summary>
        /// عرض تصویر بندانگشتی
        /// </summary>
        protected int ThumbnailImageWidth { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["ThumbnailImageWidth"]}"); } }

        /// <summary>
        /// طول تصویر بندانگشتی
        /// </summary>
        protected int ThumbnailImageMaxHeight { get { return int.Parse($"{Configuration.GetSection("PictureFileService")["ThumbnailMaxHeight"]}"); } }


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
