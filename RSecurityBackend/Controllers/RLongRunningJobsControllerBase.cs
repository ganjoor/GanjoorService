using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Long Running Jobs
    /// </summary>
    [Produces("application/json")]
    [Route("api/rjobs")]
    public abstract class RLongRunningJobsControllerBase : Controller
    {
        /// <summary>
        /// get long running jobs
        /// </summary>
        /// <param name="succeeded"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Policy = SecurableItem.AuditLogEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RLongRunningJobStatus>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Get(bool succeeded = true, bool failed = true)
        {
            var res = await _jobService.GetJobs(succeeded, failed);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="jobService">
        /// </param>
        public RLongRunningJobsControllerBase(ILongRunningJobProgressService jobService)
        {
            _jobService = jobService;
        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly ILongRunningJobProgressService _jobService;
    }
}
