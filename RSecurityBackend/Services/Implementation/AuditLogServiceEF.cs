using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Audit.Db;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Audit Log Service Implementation
    /// </summary>
    public class AuditLogServiceEF : IAuditLogService
    {
        /// <summary>
        /// get all  audit logs ordered by time
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userName">empty means unfiltered</param>
        /// <param name="orderByTimeDescending"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, REvent[] Items)>> GetAll(PagingParameterModel paging, string userName, bool orderByTimeDescending)
        {
            userName = userName == null ? "" : userName.Trim();
            var source =
                orderByTimeDescending ?
                 _context.AuditLogs
                 .Where(l => string.IsNullOrEmpty(userName) || l.UserName == userName)
                .OrderByDescending(l => l.StartDate)
                .AsQueryable()
                :
                _context.AuditLogs
                 .Where(l => string.IsNullOrEmpty(userName) || l.UserName == userName)
                .OrderBy(l => l.StartDate)
                .AsQueryable();
            (PaginationMetadata PagingMeta, REvent[] Items) paginatedResult =
                await QueryablePaginator<REvent>.Paginate(source, paging);
            return new RServiceResult<(PaginationMetadata PagingMeta, REvent[] Items)>(paginatedResult);
        }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// constuctor
        /// </summary>
        /// <param name="context"></param>
        public AuditLogServiceEF(RSecurityDbContext<RAppUser, RAppRole, Guid> context)
        {
            _context = context;
        }
    }
}
