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
        /// approved edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedEditsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved edits grouped by user / daily
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateUserViewModel[] Tracks)>> GetApprovedEditsGroupedByDateAndUserAsync(PagingParameterModel paging);

        /// <summary>
        /// approved edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedEditsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved poem corrections
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprrovedEditsSummedUpStatsAsync();

    }
}
