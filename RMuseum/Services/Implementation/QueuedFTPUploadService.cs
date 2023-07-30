using FluentFTP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.ExternalFTPUpload;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    
    /// <summary>
    /// Queued FTP Upload Service Implementation
    /// </summary>
    public class QueuedFTPUploadService : IQueuedFTPUploadService
    {
        /// <summary>
        /// add upload (you should call ProcessQueue manually)
        /// </summary>
        /// <param name="context">s</param>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        /// <param name="deleteFileAfterUpload"></param>
        /// <returns></returns>
        public async Task<RServiceResult<QueuedFTPUpload>> AddAsync(RMuseumDbContext context, string localFilePath, string remoteFilePath, bool deleteFileAfterUpload)
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
                context.QueuedFTPUploads.Add(q);
                await context.SaveChangesAsync();
                return new RServiceResult<QueuedFTPUpload>(q);
            }
            catch (Exception exp)
            {
                return new RServiceResult<QueuedFTPUpload>(null, exp.ToString());
            }
        }

        /// <summary>
        /// process queue
        /// </summary>
        /// <param name="callContext"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ProcessQueueAsync(RMuseumDbContext callContext)
        {
            try
            {
                if(callContext == null)
                {
                    callContext = _context;
                }
                var processing = await callContext.QueuedFTPUploads.AsNoTracking().Where(q => q.Processing).FirstOrDefaultAsync();
                if(processing != null)
                {
                    return new RServiceResult<bool>(false, $"already processing {processing.Id}.");
                }

                if (false == bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                {
                    return new RServiceResult<bool>(false, "ExternalFTPServer.UploadEnabled is not set to True.");
                }

                _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                            async token =>
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                                {
                                    var next = await context.QueuedFTPUploads.Where(q  => q.Processing == false).FirstOrDefaultAsync();
                                    if (next == null) return;
                                    var ftpClient = new AsyncFtpClient
                                        (
                                            Configuration.GetSection("ExternalFTPServer")["Host"],
                                            Configuration.GetSection("ExternalFTPServer")["Username"],
                                            Configuration.GetSection("ExternalFTPServer")["Password"]
                                        );
                                    ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                                    await ftpClient.AutoConnect();
                                    ftpClient.Config.RetryAttempts = 3;
                                    while (next != null)
                                    {
                                        try
                                        {
                                            next.Processing = true;
                                            next.ProcessDate = DateTime.Now;
                                            context.Update(next);
                                            await context.SaveChangesAsync();
                                            var status = await ftpClient.UploadFile(next.LocalFilePath, next.RemoteFilePath, createRemoteDir: true);
                                            if(status != FtpStatus.Failed)
                                            {
                                                if(next.DeleteFileAfterUpload)
                                                {
                                                    try
                                                    {
                                                        File.Delete(next.LocalFilePath);
                                                      
                                                        var dir = Path.GetDirectoryName(next.LocalFilePath);
                                                        if (Directory.GetFiles(dir).Length == 0)
                                                        {
                                                            Directory.Delete(dir);
                                                        }
                                                       
                                                    }
                                                    catch
                                                    {
                                                        //do nothing! not very important
                                                    }
                                                }
                                                context.Remove(next);
                                                await context.SaveChangesAsync();
                                            }
                                            else
                                            {
                                                next.Error = "ftp client status is FtpStatus.Failed";
                                                context.Update(next);
                                                await context.SaveChangesAsync();
                                                await ftpClient.Disconnect();
                                                return;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            next.Error = e.ToString();
                                            context.Update(next);
                                            await context.SaveChangesAsync();
                                            await ftpClient.Disconnect();
                                            return;
                                        }

                                        next = await context.QueuedFTPUploads.Where(q => q.Processing == false).FirstOrDefaultAsync();
                                    }
                                    await ftpClient.Disconnect();
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

        private void FtpClient_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }

        /// <summary>
        /// reset queue
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ResetQueueAsync()
        {
            try
            {
                var queued = await _context.QueuedFTPUploads.Where(q => q.Processing || !string.IsNullOrEmpty(q.Error)).ToListAsync();
                foreach (var queuedFTPUpload in queued)
                {
                    queuedFTPUpload.Processing = false;
                    queuedFTPUpload.Error = null;
                }
                _context.UpdateRange(queued);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get queued ftp uploads
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>

        public async Task<RServiceResult<(PaginationMetadata PagingMeta, QueuedFTPUpload[] Items)>> GetQueuedFTPUploadsAsync(PagingParameterModel paging)
        {
            try
            {
                var source =
                _context.QueuedFTPUploads.AsNoTracking()
               .OrderBy(t => t.QueueDate)
               .AsQueryable();
                (PaginationMetadata PagingMeta, QueuedFTPUpload[] Items) paginatedResult =
                    await QueryablePaginator<QueuedFTPUpload>.Paginate(source, paging);
                return new RServiceResult<(PaginationMetadata PagingMeta, QueuedFTPUpload[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, QueuedFTPUpload[] Items)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Background Task Queue Instance
        /// </summary>
        protected readonly IBackgroundTaskQueue _backgroundTaskQueue;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        public QueuedFTPUploadService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _context = context;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
        }
    }
}
