using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// User Visits Tracking Service
    /// </summary>
    public interface IUserVisitsTrackingService
    {
        /// <summary>
        /// add record
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddAsync(Guid userId, int poemId);

        /// <summary>
        /// delete record
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteAsync(Guid userId, Guid recordId);

        /// <summary>
        /// start or stop tracking user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SwitchTrackingAsync(Guid userId, bool start);

        /// <summary>
        /// get user history
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] HistoryItems)>> GetUserHistoryAsync(PagingParameterModel paging, Guid userId);
    }
}
