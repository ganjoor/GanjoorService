using RMuseum.DbContext;
using RMuseum.Models.ExternalFTPUpload;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    
    /// <summary>
    /// Queued FTP Upload Service
    /// </summary>
    public class QueuedFTPUploadService
    {
        /// <summary>
        /// add upload
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        /// <param name="deleteFileAfterUpload"></param>
        /// <returns></returns>
        public async Task<RServiceResult<QueuedFTPUpload>> AddAsync(string localFilePath, string remoteFilePath, bool deleteFileAfterUpload)
        {
            try
            {
                var q = new QueuedFTPUpload()
                {
                    LocalFilePath = localFilePath,
                    RemoteFilePath = remoteFilePath,
                    DeleteFileAfterUpload = deleteFileAfterUpload,
                    QueueDate = DateTime.Now,
                    Processing = false,
                };
                _context.QueuedFTPUploads.Add(q);
                await _context.SaveChangesAsync();
                return new RServiceResult<QueuedFTPUpload>(q);
            }
            catch (Exception exp)
            {
                return new RServiceResult<QueuedFTPUpload>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public QueuedFTPUploadService(RMuseumDbContext context)
        {
            _context = context;
        }
    }
}
