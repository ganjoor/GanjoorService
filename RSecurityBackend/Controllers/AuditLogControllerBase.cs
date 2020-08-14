using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RSecurityBackend.Models.Audit.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Audit Log Controller Base
    /// </summary>
    [Produces("application/json")]
    [Route("api/auditlogs")]
    public abstract class AuditLogControllerBase : Controller
    {
        /// <summary>
        /// get all  audit logs ordered by time
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userName"></param>
        /// <param name="orderByTimeDescending"></param>
        /// <returns></returns>

        [HttpGet]
        [Authorize(Policy = SecurableItem.AuditLogEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<REvent>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> Get([FromQuery]PagingParameterModel paging, string userName, bool orderByTimeDescending)
        {
            RServiceResult<(PaginationMetadata PagingMeta, REvent[] Items)> itemsInfo
                = await _auditLogService.GetAll(paging, userName, orderByTimeDescending);
            if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
            {
                return BadRequest(itemsInfo.ExceptionString);
            }          


            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(itemsInfo.Result.PagingMeta));

            return Ok(itemsInfo.Result.Items);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="auditLogService">
        /// </param>
        public AuditLogControllerBase(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly IAuditLogService _auditLogService;
    }
}
