using Microsoft.AspNetCore.Http;
using RMuseum.Models.Artifact;
using RMuseum.Models.Artifact.ViewModels;
using RMuseum.Models.Bookmark;
using RMuseum.Models.Bookmark.ViewModels;
using RMuseum.Models.GanjoorIntegration;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RMuseum.Models.ImportJob;
using RMuseum.Models.Note;
using RMuseum.Models.Note.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Artifact Item Service
    /// </summary>
    public interface IArtifactService
    {
        /// <summary>
        /// get all  artifacts (including CoverImage info but not items or attributes info) 
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>> GetAll(PagingParameterModel paging, PublishStatus[] statusArray);

        /// <summary>
        /// get tagged publish artifacts (including CoverImage info but not items or tagibutes info)
        /// </summary>
        /// <param name="tagUrl"></param>
        /// <param name="valueUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<RArtifactMasterRecord[]>> GetByTagValue(string tagUrl, string valueUrl, PublishStatus[] statusArray);


        /// <summary>
        /// gets specified artifact info (including CoverImage + images +  attributes)
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<RArtifactMasterRecordViewModel>> GetByFriendlyUrl(string friendlyUrl, PublishStatus[] statusArray);

        /// <summary>
        /// gets specified artifact info (including CoverImage + images +  tagibutes)
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="statusArray"></param>
        /// <param name="tagFriendlyUrl"></param>
        /// <param name="tagValueFriendlyUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<RArtifactMasterRecordViewModel>> GetByFriendlyUrlFilterItemsByTag(string friendlyUrl, PublishStatus[] statusArray, string tagFriendlyUrl, string tagValueFriendlyUrl);

        /// <summary>
        /// edit master record
        /// </summary>
        /// <param name="edited"></param>
        /// <param name="canChangeStatusToAwaiting"></param>
        /// <param name="canPublish"></param>
        /// <returns></returns>
        Task<RServiceResult<RArtifactMasterRecord>> EditMasterRecord(RArtifactMasterRecord edited, bool canChangeStatusToAwaiting, bool canPublish);

        /// <summary>
        /// Set Artifact Cover Item Index
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetArtifactCoverItemIndex(Guid artifactId, int itemIndex);

        /// <summary>
        /// get tag bundle by frindly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<RTagBundleViewModel>> GetTagBundleByFiendlyUrl(string friendlyUrl);

        /// <summary>
        /// get max lastmodified artifact date for caching purposes
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<DateTime>> GetMaxArtifactLastModified();

        /// <summary>
        /// get all  tags 
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RTag[] Items)>> GetAllTags(PagingParameterModel paging);

        /// <summary>
        /// get tag bu friendly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<RTag>> GetTagByFriendlyUrl(string friendlyUrl);

        /// <summary>
        /// add tag
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<RTag>> AddTag(string tagName);

        /// <summary>
        /// edit tag
        /// </summary>
        /// <param name="edited"></param>
        /// <returns></returns>
        Task<RServiceResult<RTag>> EditTag(RTag edited);

        /// <summary>
        /// changes order of tags based on their position in artifacts
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="artifactId"></param>
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag which its Order has been changed</returns>
        Task<RServiceResult<Guid?>> EditTagOrderBasedOnArtifact(Guid tagId, Guid artifactId, bool up);

        /// <summary>
        /// changes order of tags based on their position in artifact items
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="itemId"></param>
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag which its Order has been changed</returns>
        Task<RServiceResult<Guid?>> EditTagOrderBasedOnItem(Guid tagId, Guid itemId, bool up);

        /// <summary>
        /// add artifact tag value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        Task<RServiceResult<RTagValue>> TagArtifact(Guid artifactId, RTag tag);

        /// <summary>
        /// changes order of tag values based on their position in an artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tagId"></param>
        /// <param name="valueId"></param>        
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag value which its Order has been changed</returns>
        Task<RServiceResult<Guid?>> EditTagValueOrder(Guid artifactId, Guid tagId, Guid valueId, bool up);

        /// <summary>
        /// changes order of tag values based on their position in an item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tagId"></param>
        /// <param name="valueId"></param>        
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag value which its Order has been changed</returns>
        Task<RServiceResult<Guid?>> EditItemTagValueOrder(Guid itemId, Guid tagId, Guid valueId, bool up);

        /// <summary>
        /// get tag value bundle by frindly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="valueUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<RArtifactTagViewModel>> GetTagValueBundleByFiendlyUrl(string friendlyUrl, string valueUrl);

        /// <summary>
        /// get tag value by frindly url
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<RTagValue>> GetTagValueByFriendlyUrl(Guid tagId, string friendlyUrl);

        /// <summary>
        /// edit artifact tagibute value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="edited"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        Task<RServiceResult<RTagValue>> EditTagValue(Guid artifactId, RTagValue edited, bool global);

        /// <summary>
        /// remove artfiact tag value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UnTagArtifact(Guid artifactId, Guid tagValueId);

        /// <summary>
        /// add item tag value
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        Task<RServiceResult<RTagValue>> TagItem(Guid itemId, RTag tag);

        /// <summary>
        /// edit item tag value
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="edited"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        Task<RServiceResult<RTagValue>> EditItemTagValue(Guid itemId, RTagValue edited, bool global);


        /// <summary>
        /// remove item tag value
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UnTagItem(Guid itemId, Guid tagValueId);



        /// <summary>
        /// gets specified artifact item info (including images + attributes) 
        /// </summary>
        /// <param name="artifactUrl"></param>
        /// <param name="itemUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<RArtifactItemRecordViewModel>> GetArtifactItemByFrienlyUrl(string artifactUrl, string itemUrl, PublishStatus[] statusArray);



        /// <summary>
        /// add new artifact
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="srcurl"></param>
        /// <param name="pictitle"></param>
        /// <param name="picdescription"></param>
        /// <param name="file"></param>
        /// <param name="picsrcurl"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        Task<RServiceResult<RArtifactMasterRecord>> Add
            (
            string name, string description, string srcurl,
            string pictitle, string picdescription, IFormFile file, string picsrcurl, Stream stream
            );


        /// <summary>
        /// import from external resources
        /// </summary>
        /// <param name="srcType">loc/princeton/harvard/qajarwomen/hathitrust/penn/cam/bl/folder/walters/cbl</param>
        /// <param name="resourceNumber">119</param>
        /// <param name="friendlyUrl">golestan-baysonghori</param>
        /// <param name="resourcePrefix"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> Import(string srcType, string resourceNumber, string friendlyUrl, string resourcePrefix, bool skipUpload);

        /// <summary>
        /// reschedule jobs
        /// </summary>
        /// <param name="jobType"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RescheduleJobs(JobType jobType, bool skipUpload);

        /// <summary>
        /// due to a bug in loc json outputs some artifacts with more than 1000 pages were downloaded incompletely
        /// </summary>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        Task<RServiceResult<string[]>> ReExamineLocDownloads(bool skipUpload);

        /// <summary>
        /// import jobs
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, ImportJob[] Jobs)>> GetImportJobs(PagingParameterModel paging);

        /// <summary>
        /// Bookmark Artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserBookmark>> BookmarkArtifact(Guid artifactId, Guid userId, RBookmarkType type);

        /// <summary>
        /// get artifact user bookmarks
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserBookmark[]>> GetArtifactUserBookmarks(Guid artifactId, Guid userId);

        /// <summary>
        /// Bookmark Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserBookmark>> BookmarkItem(Guid itemId, Guid userId, RBookmarkType type);

        /// <summary>
        /// get item user bookmarks
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserBookmark[]>> GeItemUserBookmarks(Guid itemId, Guid userId);

        /// <summary>
        /// update bookmark note
        /// </summary>
        /// <param name="bookmarkId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateUserBookmark(Guid bookmarkId, string note);

        /// <summary>
        /// delete user bookmark         
        /// /// </summary>
        /// <param name="bookmarkId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteUserBookmark(Guid bookmarkId);

        /// <summary>
        /// get user bookmarks (artifacts and items)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RUserBookmarkViewModel[] Bookmarks)>> GetBookmarks(PagingParameterModel paging, Guid userId, RBookmarkType type, PublishStatus[] statusArray);

        /// <summary>
        /// Add Note to Artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="noteContents"></param>
        /// <param name="referenceNoteId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNoteViewModel>> AddUserNoteToArtifact(Guid artifactId, Guid userId, RNoteType type, string noteContents, Guid? referenceNoteId);

        /// <summary>
        /// Add Note to Artifact Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="noteContents"></param>
        /// <param name="referenceNoteId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNoteViewModel>> AddUserNoteToArtifactItem(Guid itemId, Guid userId, RNoteType type, string noteContents, Guid? referenceNoteId);

        /// <summary>
        /// Edit Note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="userId">sending null here means user is a moderator</param>
        /// <param name="noteContents"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNoteViewModel>> EditUserNote(Guid noteId, Guid? userId, string noteContents);


        /// <summary>
        /// delete note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="userId">sending null here means user is a moderator or the note is being deleted in a recursive delete of referenced notes</param>
        /// <returns>list of notes deleted</returns>
        Task<RServiceResult<Guid[]>> DeleteUserNote(Guid noteId, Guid? userId);

        /// <summary>
        /// get artifact private user notes
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactUserNotes(Guid artifactId, Guid userId);

        /// <summary>
        /// get artifact public user notes
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactPublicNotes(Guid artifactId);

        /// <summary>
        /// get item artifact private user notes
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactItemUserNotes(Guid itemId, Guid userId);

        /// <summary>
        /// get item artifact item public user notes
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactItemPublicNotes(Guid itemId);

        /// <summary>
        /// Get All USer Notes
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="paging"></param>
        /// <param name="type"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>> GetUserNotes(Guid userId, PagingParameterModel paging, RNoteType type, PublishStatus[] statusArray);

        /// <summary>
        /// Get All Public Notes
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>> GetAllPublicNotes(PagingParameterModel paging);

        /// <summary>
        /// suggest ganjoor link
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLinkViewModel>> SuggestGanjoorLink(Guid userId, LinkSuggestion link);

        /// <summary>
        /// get Unsynchronized image count
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnsynchronizedSuggestedLinksCount();

        /// <summary>
        /// finds what the method name suggests
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLinkViewModel[]>> GetNextUnsynchronizedSuggestedLinkWithAlreadySynchedOneForPoem(int skip);

        /// <summary>
        /// get suggested ganjoor links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLinkViewModel[]>> GetSuggestedLinks(ReviewResult status, bool notSynced);

        /// <summary>
        /// Review Suggested Link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="userId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ReviewSuggestedLink(Guid linkId, Guid userId, ReviewResult result);

        /// <summary>
        /// Temporary api
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<string[]>> AddTOCForSuggestedLinks();

        /// <summary>
        /// Synchronize suggested link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="displayOnPage"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SynchronizeSuggestedLink(Guid linkId, bool displayOnPage);

        /// <summary>
        /// get suggested pinterest links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        Task<RServiceResult<PinterestLinkViewModel[]>> GetSuggestedPinterestLinks(ReviewResult status, bool notSynced);

        /// <summary>
        /// suggest pinterest link
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        Task<RServiceResult<PinterestLinkViewModel>> SuggestPinterestLink(Guid userId, PinterestSuggestion suggestion);

        /// <summary>
        /// Review Suggested Pinterest Link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="userId"></param>
        /// <param name="altText"></param>
        /// <param name="result"></param>
        /// <param name="reviewDesc"></param>
        /// <param name="imageUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ReviewSuggestedPinterestLink(Guid linkId, Guid userId, string altText, ReviewResult result, string reviewDesc, string imageUrl);

        /// <summary>
        /// Synchronize suggested pinterest link
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SynchronizeSuggestedPinterestLink(Guid linkId);

        /// <summary>
        /// an incomplete prototype for removing artifacts
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="checkJobs"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RemoveArtifact(Guid artifactId, bool checkJobs);

        /// <summary>
        /// start filling GanjoorLink table OriginalSource values
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartFillingGanjoorLinkOriginalSources();

        /// <summary>
        /// report a public note
        /// </summary>
        /// <param name="reportUserId"></param>
        /// <param name="noteId"></param>
        /// <param name="reasonText"></param>
        /// <returns>id of report record</returns>
        Task<RServiceResult<Guid>> ReportPublicNote(Guid reportUserId, Guid noteId, string reasonText);

        /// <summary>
        /// Get a list of reported notes
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNoteAbuseReportViewModel[] Items)>> GetReportedPublicNotes(PagingParameterModel paging);

        /// <summary>
        /// delete a report for abuse in public user notes
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeclinePublicNoteReport(Guid id);

        /// <summary>
        /// delete a reported user note (accept the complaint)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AcceptPublicNoteReport(Guid id);

        /// <summary>
        /// start removing original images
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartRemovingOriginalImages();

        /// <summary>
        /// Search Artifacts
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>> SearchArtifacts(PagingParameterModel paging, string term);

        /// <summary>
        /// search artifact items
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RArtifactItemRecordViewModel[] Items)>> SearchArtifactItems(PagingParameterModel paging, string term);

        /// <summary>
        /// start setting an artifact items as text original source
        /// </summary>
        /// <param name="ganjoorCatId"></param>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        RServiceResult<bool> StartSettingArtifactAsTextOriginalSource(int ganjoorCatId, Guid artifactId);


        /// <summary>
        /// upload artifact to external server
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        RServiceResult<bool> StartUploadingArtifactToExternalServer(Guid artifactId, bool skipUpload);

    }
}
