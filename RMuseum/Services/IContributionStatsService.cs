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

        /// <summary>
        /// approved section edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedSectionEditsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved section edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedSectionEditsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved section corrections
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprrovedSectionEditsSummedUpStatsAsync();

        /// <summary>
        /// approved cat edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedCatEditsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved cat edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedCatEditsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved cat corrections
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprrovedCatEditsSummedUpStatsAsync();

        /// <summary>
        /// approved related songs daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedRelatedSongsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved related songs grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedRelatedSongsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved related songs
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedRelatedSongsSummedUpStatsAsync();

    }
}
