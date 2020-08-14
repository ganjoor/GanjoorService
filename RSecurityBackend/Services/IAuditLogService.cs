using RSecurityBackend.Models.Audit.Db;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Audit Log Service
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// get all  audit logs ordered by time
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userName">empty means unfiltered</param>
        /// <param name="orderByTimeDescending"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, REvent[] Items)>> GetAll(PagingParameterModel paging, string userName, bool orderByTimeDescending);
    }
}
