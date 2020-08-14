using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Controllers;
using RSecurityBackend.Services;


namespace RMuseum.Controllers
{
    /// <summary>
    /// Audit Log Controller Base
    /// </summary>
    [Produces("application/json")]
    [Route("api/auditlogs")]
    public class AuditLogController : AuditLogControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="auditLogService">
        /// </param>
        public AuditLogController(IAuditLogService auditLogService)
            : base(auditLogService)
        {
            
        }
    }
}
