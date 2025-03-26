using RMuseum.Models.Generic.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;
using System;

namespace RMuseum.Services
{
    /// <summary>
    /// contributions stats service
    /// </summary>
    public interface IContributionStatsService
    {
        /// <summary>
        /// get approved edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedEditsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);
    }
}
