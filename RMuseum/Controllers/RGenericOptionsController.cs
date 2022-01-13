using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RSecurityBackend.Controllers;
using RSecurityBackend.Services;

namespace RMuseum.Controllers
{
    /// <summary>
    /// options
    /// </summary>
    [Produces("application/json")]
    [Route("api/options")]
    public class RGenericOptionsController : RGenericOptionsControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="optionsService"></param>
        public RGenericOptionsController(IRGenericOptionsService optionsService) : base(optionsService)
        {
        }
    }
}
