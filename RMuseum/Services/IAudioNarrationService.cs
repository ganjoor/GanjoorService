using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Audio Narration Service
    /// </summary>
    public interface IAudioNarrationService
    {
        /// <summary>
        /// returns list of narrations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filteredUserId">send Guid.Empty if you want all narrations</param>
        /// <param name="status"></param>
        /// <returns></returns>
        public Task<RServiceResult<(PaginationMetadata PagingMeta, PoemNarrationViewModel[] Items)>> GetAll(PagingParameterModel paging, Guid filteredUserId, AudioReviewStatus status);

        /// <summary>
        /// imports data from ganjoor MySql database
        /// </summary>
        /// <param name="OwnrRAppUserId">User Id which becomes owner of imported data</param>
        /// <returns></returns>
        Task<RServiceResult<bool>> OneTimeImport(Guid OwnrRAppUserId);
    }
}
