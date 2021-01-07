using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Controllers;
using RSecurityBackend.Services;

namespace RMuseum.Controllers
{
    /// <summary>
    /// Long Running Jobs
    /// </summary>
    [Produces("application/json")]
    [Route("api/rjobs")]
    public class RLongRunningJobsController : RLongRunningJobsControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="jobService">
        /// </param>
        public RLongRunningJobsController(ILongRunningJobProgressService jobService) : base(jobService)
        {
        }
    }
}
