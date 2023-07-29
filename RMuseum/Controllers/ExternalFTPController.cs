using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Services;
using System.Net;
using System.Threading.Tasks;
using RMuseum.Models.FAQ;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/ftp")]
    public class ExternalFTPController : Controller
    {
        /// <summary>
        /// FTP Service
        /// </summary>
        protected readonly IQueuedFTPUploadService _ftpService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ftpService"></param>
        public ExternalFTPController(IQueuedFTPUploadService ftpService)
        {
            _ftpService = ftpService;
        }

    }
}
