using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using RSecurityBackend.Controllers;
using Audit.WebApi;

namespace RMuseum.Controllers
{
    /// <summary>
    /// Generic Image Provider
    /// </summary>
    [Produces("application/json")]
    [Route("api/rimages")]
    public class RImageController : RImageControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="pictureFileService">
        /// </param>
        public RImageController(IImageFileService pictureFileService) : base(pictureFileService)
        {

        }
    }
}
