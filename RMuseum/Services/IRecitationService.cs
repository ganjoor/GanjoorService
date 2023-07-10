using Microsoft.AspNetCore.Http;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
using RMuseum.Models.UploadSession.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Audio Narration Service
    /// </summary>
    public interface IRecitationService
    {
        /// <summary>
        /// returns list of narrations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filteredUserId">send Guid.Empty if you want all narrations</param>
        /// <param name="status"></param>
        /// <param name="searchTerm"></param>
        /// <param name="mistakes"></param>
        /// <returns></returns>
        public Task<RServiceResult<(PaginationMetadata PagingMeta, RecitationViewModel[] Items)>> SecureGetAll(PagingParameterModel paging, Guid filteredUserId, AudioReviewStatus status, string searchTerm, bool mistakes);

        /// <summary>
        /// returns list of publish narrations (if poetId or catId is non-zero its ordered by poemId ascending if not it is ordered by publish date descending)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="searchTerm"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>> GetPublishedRecitations(PagingParameterModel paging, string searchTerm = "", int poetId = 0, int catId = 0);

        /// <summary>
        /// get category top recitations
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="includePoemText"></param>
        /// <returns></returns>
        Task<RServiceResult<PublicRecitationViewModel[]>> GetPoemCategoryTopRecitations(int catId, bool includePoemText);

        /// <summary>
        /// check if a category has any recitations
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> GetPoemCategoryHasAnyRecitations(int catId);

        /// <summary>
        /// get published recitation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PublicRecitationViewModel>> GetPublishedRecitationById(int id);

        /// <summary>
        /// return selected narration information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<RecitationViewModel>> Get(int id);

        /// <summary>
        /// Delete recitation (recitation should belong to userId)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> Delete(int id, Guid userId);

        /// <summary>
        /// Gets Verse Sync Range Information
        /// </summary>
        /// <param name="id">narration id</param>
        /// <returns></returns>
        Task<RServiceResult<RecitationVerseSync[]>> GetPoemNarrationVerseSyncArray(int id);

        /// <summary>
        /// updates metadata for narration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        Task<RServiceResult<RecitationViewModel>> UpdatePoemNarration(int id, RecitationViewModel metadata);

        /// <summary>
        /// build profiles from exisng narrations data
        /// </summary>
        /// <param name="ownerRAppUserId">User Id which becomes owner of imported data</param>
        /// <returns>error string if occurs</returns>
        Task<string> BuildProfilesFromExistingData(Guid ownerRAppUserId);

        /// <summary>
        /// Initiate New Upload Session for audio
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        Task<RServiceResult<UploadSession>> InitiateNewUploadSession(Guid userId, bool replace);

        /// <summary>
        /// Save uploaded file
        /// </summary>
        /// <param name="uploadedFile"></param>
        /// <returns></returns>
        Task<RServiceResult<UploadSessionFile>> SaveUploadedFile(IFormFile uploadedFile);

        /// <summary>
        /// finalize upload session (add files)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        Task<RServiceResult<UploadSession>> FinalizeNewUploadSession(UploadSession session, UploadSessionFile[] files);

        /// <summary>
        /// Moderate pending narration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="moderatorId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<RecitationViewModel>> ModeratePoemNarration(int id, Guid moderatorId, RecitationModerateViewModel model);

        /// <summary>
        /// Get Upload Session (including files)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<UploadSession>> GetUploadSession(Guid id);

        /// <summary>
        /// Get User Profiles
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="artistName"></param>
        /// <returns></returns>
        Task<RServiceResult<UserRecitationProfileViewModel[]>> GetUserNarrationProfiles(Guid userId, string artistName);

        /// <summary>
        /// Get User Default Profile
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<UserRecitationProfileViewModel>> GetUserDefProfile(Guid userId);

        /// <summary>
        /// Add a narration profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        Task<RServiceResult<UserRecitationProfileViewModel>> AddUserNarrationProfiles(UserRecitationProfileViewModel profile);

        /// <summary>
        /// Update a narration profile 
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        Task<RServiceResult<UserRecitationProfileViewModel>> UpdateUserNarrationProfiles(UserRecitationProfileViewModel profile);

        /// <summary>
        /// Delete a narration profile 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteUserNarrationProfiles(Guid id, Guid userId);

        /// <summary>
        /// Transfer Recitations Ownership
        /// </summary>
        /// <param name="currentOwenerId"></param>
        /// <param name="newOwnerId"></param>
        /// <param name="artistName"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> TransferRecitationsOwnership(Guid currentOwenerId, Guid newOwnerId, string artistName);

        /// <summary>
        /// Get uploads descending by upload time
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId">if userId is empty all user uploads would be returned</param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, UploadedItemViewModel[] Items)>> GetUploads(PagingParameterModel paging, Guid userId);

        /// <summary>
        /// publishing tracker data
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="unfinished"></param>
        /// <param name="filteredUserId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RecitationPublishingTrackerViewModel[] Items)>> GetPublishingQueueStatus(PagingParameterModel paging, bool unfinished, Guid filteredUserId);

        /// <summary>
        /// move recitaions of an artist to the first position
        /// </summary>
        /// <param name="artistName"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> MakeArtistRecitationsFirst(string artistName);

        /// <summary>
        /// Synchronization Queue
        /// </summary>
        /// <param name="filteredUserId"></param>
        /// <returns></returns>
        Task<RServiceResult<RecitationViewModel[]>> GetSynchronizationQueue(Guid filteredUserId);

        /// <summary>
        /// report an error in a recitation
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="report"></param>
        /// <returns></returns>
        Task<RServiceResult<RecitationErrorReportViewModel>> ReportErrorAsync(Guid userId, RecitationErrorReportViewModel report);

        /// <summary>
        /// get errors reported for recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RecitationErrorReportViewModel[] Items)>> GetReportedErrorsAsync(PagingParameterModel paging);

        /// <summary>
        /// reject a reported error for recitations and notify the reporter (and deletes the report)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rejectionNote"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RejectReportedErrorAsync(int id, string rejectionNote = "عدم تطابق با معیارهای حذف خوانش");

        /// <summary>
        /// accepts a reported error for recitations, change status of the recitation to rejected and notify the reporter and recitation owner (and deletes the report)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AcceptReportedErrorAsync(int id);

        /// <summary>
        /// accepts a reported error for recitations, add mistake to approve the mistake and notify the reporter and recitation owner (and deletes the report)
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddReportToTheApprovedMistakesAsync(RecitationErrorReportViewModel report);

        /// <summary>
        /// compute poem recitations order
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        Task<RServiceResult<RecitationOrderingViewModel[]>> ComputePoemRecitationsOrdersAsync(int poemId, bool update = true);

        /// <summary>
        /// up vote a recitation
        /// </summary>
        /// <param name="id">recitation id</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpVoteRecitationAsync(int id, Guid userId);

        /// <summary>
        /// revoke recitaion up vote
        /// </summary>
        /// <param name="id">recitaion id</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RevokeUpVoteFromRecitationAsync(int id, Guid userId);

        /// <summary>
        /// switches recitation upvote
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns>upvote status</returns>
        Task<RServiceResult<bool>> SwitchRecitationUpVoteAsync(int id, Guid userId);

        /// <summary>
        /// get user upvoted recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>> GetUserUpvotedRecitationsAsync(PagingParameterModel paging, Guid userId);


        /// <summary>
        /// check recitaions with missing files and add them to reported errors list
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        RServiceResult<bool> StartCheckingRecitationsHealthCheck(Guid userId);

        /// <summary>
        /// retry publish unpublished narrations
        /// </summary>
        Task RetryPublish();

        /// <summary>
        /// Upload Enabled (temporary switch off/on for upload)
        /// </summary>
        bool UploadEnabled { get; }
    }
}
