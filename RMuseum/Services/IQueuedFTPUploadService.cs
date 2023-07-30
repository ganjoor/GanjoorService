using RMuseum.DbContext;
using RMuseum.Models.ExternalFTPUpload;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Queued FTP Upload Service
    /// </summary>
    public interface IQueuedFTPUploadService
    {
        /// <summary>
        /// add upload (you should call ProcessQueue manually)
        /// </summary>
        /// <param name="context">s</param>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        /// <param name="deleteFileAfterUpload"></param>
        /// <returns></returns>
        Task<RServiceResult<QueuedFTPUpload>> AddAsync(RMuseumDbContext context, string localFilePath, string remoteFilePath, bool deleteFileAfterUpload);

        /// <summary>
        /// process queue
        /// </summary>
        /// <param name="callContext"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ProcessQueueAsync(RMuseumDbContext callContext);

        /// <summary>
        /// reset queue
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> ResetQueueAsync();

        /// <summary>
        /// get queued ftp uploads
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>

        Task<RServiceResult<(PaginationMetadata PagingMeta, QueuedFTPUpload[] Items)>> GetQueuedFTPUploadsAsync(PagingParameterModel paging);
    }
}
