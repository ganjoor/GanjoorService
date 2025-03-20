﻿using Microsoft.AspNetCore.Http;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.GanjoorIntegration;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Ganjoor Poems Content Privider Service
    /// </summary>
    public interface IGanjoorService
    {
        /// <summary>
        /// Get List of poets
        /// </summary>
        /// <param name="published"></param>
        /// <param name="includeBio"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetViewModel[]>> GetPoets(bool published, bool includeBio = true);

        /// <summary>
        /// get poet by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetById(int id, bool catPoems = false);

        /// <summary>
        /// get poet by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetByUrl(string url, bool catPoems = false);

        /// <summary>
        /// poet image id by url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<RServiceResult<Guid>> GetPoetImageIdByUrl(string url);

        /// <summary>
        /// get cat by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="poems"></param>
        /// <param name="mainSections"></param>
        /// <param name="paperSources"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatById(int id, bool poems = false, bool mainSections = false, bool paperSources = false);

        /// <summary>
        /// get cat by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="poems"></param>
        /// <param name="mainSections"></param>
        /// <param name="paperSources"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatByUrl(string url, bool poems = false, bool mainSections = false, bool paperSources = false);

        /// <summary>
        /// Update category extra info
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="bookName"></param>
        /// <param name="imageId"></param>
        /// <param name="sumUpSubsGeoLocations"></param>
        /// <param name="mapName"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCatViewModel>> SetCategoryExtraInfo(int catId, string bookName, Guid? imageId, bool sumUpSubsGeoLocations, string mapName);

        /// <summary>
        /// get list of books
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCatViewModel[]>> GetBooksAsync();

        /// <summary>
        /// generate missing book covers
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> GenerateMissingBookCoversAsync();

        /// <summary>
        /// get page by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPageCompleteViewModel>> GetPageByUrl(string url, bool catPoems = false);

        /// <summary>
        /// get page url by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> GetPageUrlById(int id);

        /// <summary>
        /// get redirect url for a url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> GetRedirectAddressForPageUrl(string url);

        /// <summary>
        /// delete page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePageAsync(int id);

        /// <summary>
        /// delete a poem
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoemAsync(int id);

        /// <summary>
        /// Get Poem By Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <param name="relatedpoems"></param>
        /// <param name="sections"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemById(int id, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true, bool relatedpoems = true, bool sections = true);

        /// <summary>
        /// get poem verses
        /// </summary>
        /// <param name="id"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorVerseViewModel[]>> GetPoemVersesAsync(int id, int coupletIndex);

        /// <summary>
        /// Get Poem By Url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemByUrl(string url, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true);

        /// <summary>
        /// get poem recitations (PlainText/HtmlText are intentionally empty)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PublicRecitationViewModel[]>> GetPoemRecitations(int id);

        /// <summary>
        /// get poem sections
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSection[]>> GetPoemWholeSections(int id);

        /// <summary>
        /// get user up votes for the recitations of a poem
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<int[]>> GetUserPoemRecitationsUpVotes(int id, Guid userId);

        /// <summary>
        /// get poem images by id (some fields are intentionally field with blank or null),
        /// EntityImageId : the most important data field, image url is {WebServiceUrl.Url}/api/images/thumb/{EntityImageId}.jpg or {WebServiceUrl.Url}/api/images/norm/{EntityImageId}.jpg
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemRelatedImage[]>> GetPoemImages(int id);

        /// <summary>
        /// get poem related songs
        /// </summary>
        /// <param name="id"></param>
        /// <param name="approved"></param>
        /// <param name="trackType"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemMusicTrackViewModel[]>> GetPoemSongs(int id, bool approved, PoemMusicTrackType trackType = PoemMusicTrackType.All);

        /// <summary>
        /// get poem comments
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="userId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCommentSummaryViewModel[]>> GetPoemComments(int poemId, Guid userId, int? coupletIndex);

        /// <summary>
        /// get a single comment information (replies are not included)
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCommentSummaryViewModel>> GetCommentByIdAsync(int commentId);

        /// <summary>
        /// get a section related sections
        /// </summary>
        /// <param name="poemId">poem id</param>
        /// <param name="sectionIndex">section index</param>
        /// <param name="skip"></param>
        /// <param name="itemsCount">if sent 0 or less returns all items</param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCachedRelatedSection[]>> GetRelatedSections(int poemId, int sectionIndex, int skip, int itemsCount);

        /// <summary>
        /// send poem corrections
        /// </summary>
        /// <param name="correction"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCorrectionViewModel>> SuggestPoemCorrection(GanjoorPoemCorrectionViewModel correction);

        /// <summary>
        /// last unreviewed user correction for a poem
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCorrectionViewModel>> GetLastUnreviewedUserCorrectionForPoem(Guid userId, int poemId);

        /// <summary>
        /// user suggested songs
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PoemMusicTrackViewModel[] Items)>> GetUserSuggestedSongs(Guid userId, PagingParameterModel paging);

        /// <summary>
        /// get user or all corrections
        /// </summary>
        /// <param name="userId">if sent empty returns all corrections</param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCorrectionViewModel[] Items)>> GetUserCorrections(Guid userId, PagingParameterModel paging);

        /// <summary>
        /// effective corrections for poem
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCorrectionViewModel[] Items)>> GetPoemEffectiveCorrections(int poemId, PagingParameterModel paging);

        /// <summary>
        /// get correction by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCorrectionViewModel>> GetCorrectionById(int id);

        /// <summary>
        /// delete unreviewed user corrections for a poem
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoemCorrections(Guid userId, int poemId);

        /// <summary>
        /// get next unreviewed correction
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="onlyUserCorrections"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCorrectionViewModel>> GetNextUnreviewedCorrection(int skip, bool onlyUserCorrections);

        /// <summary>
        /// unreview correction count
        /// </summary>
        /// <param name="onlyUserCorrections"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreviewedCorrectionCount(bool onlyUserCorrections);

        /// <summary>
        /// moderate poem correction
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="correction"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCorrectionViewModel>> ModeratePoemCorrection(Guid userId, GanjoorPoemCorrectionViewModel correction);

        /// <summary>
        /// break a poem from a verse forward
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="vOrder"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> BreakPoemAsync(int poemId, int vOrder, Guid userId);

        /// <summary>
        /// update related sections
        /// </summary>
        /// <param name="metreId"></param>
        /// <param name="rhyme"></param>
        void UpdateRelatedSections(int metreId, string rhyme);

        /// <summary>
        /// next unreviewed track
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="suggestedById"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemMusicTrackViewModel>> GetNextUnreviewedSong(int skip, Guid suggestedById);

        /// <summary>
        /// get track of user song suggestions
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<UserSongSuggestionsHistory>> GetUserSongsSuggestionsStatistics(Guid userId);

        /// <summary>
        /// suggest song
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="song"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemMusicTrackViewModel>> SuggestSong(Guid userId, PoemMusicTrackViewModel song);

        /// <summary>
        /// get unreviewed count
        /// </summary>
        /// <param name="suggestedById"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreviewedSongsCount(Guid suggestedById);

        /// <summary>
        /// review song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemMusicTrackViewModel>> ReviewSong(PoemMusicTrackViewModel song);

        /// <summary>
        /// direct insert song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemMusicTrackViewModel>> DirectInsertSong(PoemMusicTrackViewModel song);

        /// <summary>
        /// get song by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        Task<RServiceResult<PoemMusicTrackViewModel>> GetPoemSongById(int id);

        /// <summary>
        /// modify a published song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemMusicTrackViewModel>> ModifyPublishedSong(PoemMusicTrackViewModel song);

        /// <summary>
        /// delete poem song by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoemSongById(int id);

        /// <summary>
        /// new comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ip"></param>
        /// <param name="poemId"></param>
        /// <param name="content"></param>
        /// <param name="inReplyTo"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCommentSummaryViewModel>> NewComment(Guid userId, string ip, int poemId, string content, int? inReplyTo, int? coupletIndex);

        /// <summary>
        /// update user's own comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <param name="htmlComment"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> EditMyComment(Guid userId, int commentId, string htmlComment);

        /// <summary>
        /// link or unlink user's own comment to a coupletIndex
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <param name="coupletIndex">if null then unlinks</param>
        /// <returns>couplet summary</returns>
        Task<RServiceResult<string>> LinkUnLinkMyComment(Guid userId, int commentId, int? coupletIndex);

        /// <summary>
        /// delete a reported  comment
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteModerateComment(int reportId);

        /// <summary>
        /// delete user own comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteMyComment(Guid userId, int commentId);

        /// <summary>
        /// delete anybody's comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteAnybodyComment(int commentId);

        /// <summary>
        /// publish awaiting comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> PublishAwaitingComment(int commentId);

        /// <summary>
        /// get recent comments
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filterUserId">Guid.Empty</param>
        /// <param name="onlyPublished"></param>
        /// <param name="onlyAwaiting"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items)>> GetRecentComments(PagingParameterModel paging, Guid filterUserId, bool onlyPublished, bool onlyAwaiting = false, string term = null);

        /// <summary>
        /// report a comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="report"></param>
        /// <returns>id of report record</returns>
        Task<RServiceResult<int>> ReportComment(Guid userId, GanjoorPostReportCommentViewModel report);

        /// <summary>
        /// delete a report
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteReport(int id);

        /// <summary>
        /// Get list of reported comments
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentAbuseReportViewModel[] Items)>> GetReportedComments(PagingParameterModel paging);

        /// <summary>
        /// Get Similar Poems accroding to prosody and rhyme informations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="metre"></param>
        /// <param name="rhyme"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <param name="language"></param>
        /// <param name="format"></param>
        /// <param name="term"></param>
        /// <param name="coupletCountsFrom"></param>
        /// <param name="coupletCountsTo"></param>
        /// <param name="exceptPoetId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> GetSimilarPoemsAsync(PagingParameterModel paging, string metre, string rhyme, int? poetId, int? catId, string language, GanjoorPoemFormat format , string term , int coupletCountsFrom , int coupletCountsTo , int[] exceptPoetId );

        /// <summary>
        /// language tagged poem sections
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="language"></param>
        /// <param name="poetId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> GetLanguageTaggedPoemSections(PagingParameterModel paging, string language, int? poetId);

        /// <summary>
        /// modify page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editingUserId"></param>
        /// <param name="pageData"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPageCompleteViewModel>> UpdatePageAsync(int id, Guid editingUserId, GanjoorModifyPageViewModel pageData);

        /// <summary>
        /// modify poem => only these fields: NoIndex, RedirectFromFullUrl, MixedModeOrder
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editingUserId"></param>
        /// <param name="pageData"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPageCompleteViewModel>> UpdatePoemAsync(int id, Guid editingUserId, GanjoorModifyPageViewModel pageData);

        /// <summary>
        /// modify poet
        /// </summary>
        /// <param name="poet"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdatePoetAsync(GanjoorPoetViewModel poet, Guid editingUserId);

        /// <summary>
        /// create new poet
        /// </summary>
        /// <param name="poet"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetCompleteViewModel>> AddPoetAsync(GanjoorPoetViewModel poet, Guid editingUserId);

        /// <summary>
        /// delete poet
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        RServiceResult<bool> StartDeletePoet(int id);

        /// <summary>
        /// chaneg poet image
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="imageId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ChangePoetImageAsync(int poetId, Guid imageId);

        /// <summary>
        /// returns metre list (ordered by Rhythm)
        /// </summary>
        /// <param name="sortOnVerseCount"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorMetre[]>> GetGanjoorMetres(bool sortOnVerseCount = false);


        /// <summary>
        /// get a random poem (2 = from hafez)
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="recitation"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemCompleteViewModel>> Faal(int poetId = 2, bool recitation = true);

        /// <summary>
        /// import from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ImportFromSqlite(int poetId, IFormFile file);

        /// <summary>
        /// import a catgory from sqlite
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ImportCategoryFromSqlite(int catId, IFormFile file);

        /// <summary>
        /// Apply corrections from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="file"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ApplyCorrectionsFromSqlite(int poetId, IFormFile file, string note);

        /// <summary>
        /// export to sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> ExportToSqlite(int poetId);

        /// <summary>
        /// start generating gdb files
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartBatchGenerateGDBFiles();

        /// <summary>
        /// examine site pages for broken links
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> HealthCheckContents();

        /// <summary>
        /// examine comments for long links
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> FindAndFixLongUrlsInComments();

        /// <summary>
        /// start filling poems couplet indices
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartFillingPoemsCoupletIndices();

        /// <summary>
        /// refill couplet indices
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RefillCoupletIndicesAsync(int poemId);

        /// <summary>
        /// fill section couplets count
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartFillingSectionCoupletCounts();


        /// <summary>
        /// regenerate poem full titles to fix an old bug
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> RegeneratePoemsFullTitles();

        /// <summary>
        /// start finding rhymes for single couplets
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartFindingRhymesForSingleCouplets();

        /// <summary>
        /// separate verses in poem.PlainText with  Environment.NewLine instead of SPACE
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        RServiceResult<bool> RegerneratePoemsPlainText(int catId);

        /// <summary>
        /// clean cache for paeg by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task CacheCleanForPageById(int id);

        /// <summary>
        /// clean cache for page by url
        /// </summary>
        /// <param name="url"></param>
        void CacheCleanForPageByUrl(string url);

        /// <summary>
        /// clean cache for page by comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        Task CacheCleanForComment(int commentId);

        /// <summary>
        /// page modifications history
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPageSnapshotSummaryViewModel[]>> GetOlderVersionsOfPage(int pageId);

        /// <summary>
        /// get old version
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorModifyPageViewModel>> GetOldVersionOfPage(int id);


        /// <summary>
        /// Search
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <param name="exceptPoetId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> Search(PagingParameterModel paging, string term, int? poetId, int? catId, int[] exceptPoetId);

        /// <summary>
        /// batch rename
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<string[]>> BatchRenameCatPoemTitles(int catId, GanjoorBatchNamingModel model, Guid userId);

        /// <summary>
        /// re sulg cat poems
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> BatchReSlugCatPoems(int catId);

        /// <summary>
        /// find poem rhyme
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjooRhymeAnalysisResult>> FindPoemMainSectionRhyme(int id);

        /// <summary>
        /// find poem section rhyme
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjooRhymeAnalysisResult>> FindSectionRhyme(int id);

        /// <summary>
        /// find category poem rhymes
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="retag"></param>
        /// <returns></returns>
        RServiceResult<bool> FindCategoryPoemsRhymes(int catId, bool retag);

        /// <summary>
        /// Start finding missing rhthms
        /// </summary>
        /// <param name="systemUserId"></param>
        /// <param name="deletedUserId"></param>
        /// <param name="onlyPoemsWithRhymes"></param>
        /// <param name="poemsNum"></param>
        /// <returns></returns>
        RServiceResult<bool> StartFindingMissingRhythms(Guid systemUserId, Guid deletedUserId, bool onlyPoemsWithRhymes, int poemsNum = 1000);

        /// <summary>
        /// find poem rhythm
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> FindPoemMainSectionRhythm(int id);

        /// <summary>
        /// find category poem rhymes
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="retag"></param>
        /// <param name="rhythm"></param>
        /// <returns></returns>
        RServiceResult<bool> FindCategoryPoemsRhythms(int catId, bool retag, string rhythm = "");

        /// <summary>
        /// generate category TOC
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="catId"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<RServiceResult<string>> GenerateTableOfContents(Guid userId, int catId, GanjoorTOC options);

        /// <summary>
        /// directly insert generated TOC
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="userId"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DirectInsertGeneratedTableOfContents(int catId, Guid userId, GanjoorTOC options);

        /// <summary>
        /// start generating sub cats TOC
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="catId"></param>
        /// <param name="ganjoorTOC"></param>
        /// <returns></returns>
        RServiceResult<bool> StartGeneratingSubCatsTOC(Guid userId, int catId, GanjoorTOC ganjoorTOC);


        /// <summary>
        /// build sitemap
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartBuildingSitemap();

        /// <summary>
        /// build tj.ganjoor.net site map
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartBuildingTajikSitemap();

        /// <summary>
        /// start updating stats page
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartUpdatingStatsPage(Guid editingUserId);

        /// <summary>
        /// start updating mundex page
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartUpdatingMundexPage(Guid editingUserId);


        /// <summary>
        /// Switch Bookmark for couplet
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorUserBookmark>> SwitchCoupletBookmark(Guid userId, int poemId, int coupletIndex);

        /// <summary>
        /// Bookmark couplet if it is not
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorUserBookmark>> BookmarkCoupletIfNotBookmarked(Guid userId, int poemId, int coupletIndex);

        /// <summary>
        /// delete user bookmark         
        /// /// </summary>
        /// <param name="bookmarkId"></param>
        /// <param name="userId">to make sure a user can not delete another user's bookmarks</param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteGanjoorBookmark(Guid bookmarkId, Guid userId);

        /// <summary>
        /// modify bookmark private note
        /// </summary>
        /// <param name="bookmarkId"></param>
        /// <param name="userId">to make sure a user can not modify another user's bookmarks</param>
        /// <param name="note"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModifyBookmarkPrivateNoteAsync(Guid bookmarkId, Guid userId, string note);

        /// <summary>
        /// get user ganjoor bookmarks (only  Id, CoupletIndex and DateTime are valid)
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorUserBookmarkViewModel[]>> GetPoemUserBookmarks(Guid userId, int poemId);

        /// <summary>
        /// get verse bookmarks
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> IsCoupletBookmarked(Guid userId, int poemId, int coupletIndex);


        /// <summary>
        /// get user bookmarks (artifacts and items)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] Bookmarks)>> GetUserBookmarks(PagingParameterModel paging, Guid userId, string q);

        /// <summary>
        /// regenerate half centuries
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> RegenerateHalfCenturiesAsync();

        /// <summary>
        /// get centuries with published poets
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCenturyViewModel[]>> GetCenturiesAsync();

        /// <summary>
        /// start generating related sections info
        /// </summary>
        /// <param name="regenerate"></param>
        /// <returns></returns>
        RServiceResult<bool> StartGeneratingRelatedSectionsInfo(bool regenerate);


        /// <summary>
        /// get next ganjoor section probable metre
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSection>> GetNextGanjoorPoemProbableMetre();

        /// <summary>
        /// get a list of ganjoor poem sections probable metres
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemSection[] Items)>> GetUnreviewedGanjoorPoemProbableMetres(PagingParameterModel paging);

        /// <summary>
        /// save ganjoor poem probable metre
        /// </summary>
        /// <param name="id">problable metre id</param>
        /// <param name="metre"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SaveGanjoorPoemProbableMetre(int id, string metre);

        /// <summary>
        /// return list of suggested spec lines
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel[]>> GetPoetSuggestedSpecLinesAsync(int poetId);

        /// <summary>
        /// returns specfic suggested line for poet
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>> GetPoetSuggestedSpecLineAsync(int id);

        /// <summary>
        /// next unpublished suggested line for poets
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>> GetNextUnmoderatedPoetSuggestedSpecLineAsync(int skip);

        /// <summary>
        /// npublished suggested lines count for poets
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<int>> GetNextUnmoderatedPoetSuggestedSpecLinesCountAsync();

        /// <summary>
        /// add a suggestion for poets spec lines
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>

        Task<RServiceResult<GanjoorPoetSuggestedSpecLineViewModel>> AddPoetSuggestedSpecLinesAsync(GanjoorPoetSuggestedSpecLineViewModel model);

        /// <summary>
        /// modify a suggestion for poets spec lines
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModifyPoetSuggestedSpecLinesAsync(GanjoorPoetSuggestedSpecLineViewModel model);

        /// <summary>
        /// reject  a suggestion for poets spec lines
        /// </summary>
        /// <param name="id"></param>
        /// <param name="deleteUserId"></param>
        /// <param name="rejectionCause"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RejectPoetSuggestedSpecLinesAsync(int id, Guid deleteUserId, string rejectionCause);

        /// <summary>
        /// delete published suggested spec line
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoetSuggestedSpecLinesAsync(int id);

        /// <summary>
        /// delete a category
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteCategoryAsync(int id);

        /// <summary>
        /// set category poems language tag
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetCategoryLanguageTagAsync(int catId, string language);

        /// <summary>
        /// set category poem format tag for poems consisting of a single whole poem section
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetCategoryPoemFormatAsync(int catId, GanjoorPoemFormat? format);

        /// <summary>
        /// regenerate TOCs
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        RServiceResult<bool> StartRegeneratingTOCs(Guid userId);

        /// <summary>
        /// Finding Category Poems Duplicates
        /// </summary>
        /// <param name="srcCatId"></param>
        /// <param name="destCatId"></param>
        /// <param name="hardTry"></param>
        /// <returns></returns>
        RServiceResult<bool> StartFindingCategoryPoemsDuplicates(int srcCatId, int destCatId, bool hardTry);

        /// <summary>
        /// list of category saved duplicated poems
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorDuplicateViewModel[]>> GetCategoryDuplicates(int catId);

        /// <summary>
        /// manually add a duplicate for a poems
        /// </summary>
        /// <param name="srcCatId"></param>
        /// <param name="srcPoemId"></param>
        /// <param name="destPoemId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AdDuplicateAsync(int srcCatId, int srcPoemId, int destPoemId);

        /// <summary>
        /// delete duplicate
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteDuplicateAsync(int id);

        /// <summary>
        /// start removing category duplicates
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="destCatId"></param>
        /// <returns></returns>
        RServiceResult<bool> StartRemovingCategoryDuplicates(int catId, int destCatId);


        /// <summary>
        /// sectionizing poems
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartFillingPoemSectionsCoupletIndex();

        /// <summary>
        /// start band couplets fix
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartOneTimeBandCoupletsFix();

        /// <summary>
        /// get couplet sections
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSection[]>> GetCoupletSectionsAsync(int poemId, int coupletIndex);

        /// <summary>
        /// get all poem sections
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSection[]>> GetPoemSectionsAsync(int id);

        /// <summary>
        /// regenerate poem sections (dangerous: wipes out existing data)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RegeneratePoemSections(int id);

        /// <summary>
        /// get a specific poem section
        /// </summary>
        /// <param name="sectionId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSection>> GetPoemSectionByIdAsync(int sectionId);

        /// <summary>
        /// delete a poem section
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="sectionIndex"></param>
        /// <param name="convertVerses"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoemSectionByPoemIdAndIndexAsync(int poemId, int sectionIndex, bool convertVerses);

        /// <summary>
        /// last unreviewed user correction for a section
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sectionId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSectionCorrectionViewModel>> GetLastUnreviewedUserCorrectionForSection(Guid userId, int sectionId);

        /// <summary>
        /// get user section corrections
        /// </summary>
        /// <param name="userId">if sent empty returns all corrections</param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemSectionCorrectionViewModel[] Items)>> GetUserSectionCorrections(Guid userId, PagingParameterModel paging);

        /// <summary>
        /// send a correction for a section
        /// </summary>
        /// <param name="correction"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSectionCorrectionViewModel>> SuggestPoemSectionCorrection(GanjoorPoemSectionCorrectionViewModel correction);

        /// <summary>
        /// moderate poem section correction
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="moderation"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSectionCorrectionViewModel>> ModeratePoemSectionCorrection(Guid userId,
            GanjoorPoemSectionCorrectionViewModel moderation);

        /// <summary>
        /// delete unreviewed user corrections for a poem section
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sectionId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoemSectionCorrections(Guid userId, int sectionId);

        /// <summary>
        /// get next unreviewed correction for a poem section
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="deletedUserSections"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSectionCorrectionViewModel>> GetNextUnreviewedPoemSectionCorrection(int skip, bool deletedUserSections);

        /// <summary>
        /// unreviewed poem section correction count
        /// </summary>
        /// <param name="deletedUserSections"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreviewedPoemSectionCorrectionCount(bool deletedUserSections);

        /// <summary>
        /// get section correction by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPoemSectionCorrectionViewModel>> GetSectionCorrectionById(int id);

        /// <summary>
        /// effective corrections for section
        /// </summary>
        /// <param name="sectionId"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemSectionCorrectionViewModel[] Items)>> GetSectionEffectiveCorrections(int sectionId, PagingParameterModel paging);


        /// <summary>
        /// regenerate category related sections
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        RServiceResult<bool> StartRegeneratingCateoryRelatedSections(int catId);

        /// <summary>
        /// transfer poems and sections from a meter to another one
        /// </summary>
        /// <param name="srcMeterId"></param>
        /// <param name="destMeterId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> TransferMeterAsync(int srcMeterId, int destMeterId);


        /// <summary>
        /// add poem geo tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemGeoDateTag>> AddPoemGeoDateTagAsync(PoemGeoDateTag tag);


        /// <summary>
        /// update poem tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdatePoemGeoDateTagAsync(PoemGeoDateTag tag);

        /// <summary>
        /// delete poem tag
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoemGeoDateTagAsync(int id);

        /// <summary>
        /// get poem tags ordered by LunarDateTotalNumber then by Id
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemGeoDateTag[]>> GetPoemGeoDateTagsAsync(int poemId);

        /// <summary>
        /// get a categoty poem tags
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="ignoreSumup"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemGeoDateTag[]>> GetCatPoemGeoDateTagsAsync(int catId, bool ignoreSumup = false);


        /// <summary>
        /// synchronize naskban links
        /// </summary>
        /// <param name="ganjoorUserId"></param>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        /// <returns>number of synched items</returns>
        void SynchronizeNaskbanLinks(Guid ganjoorUserId, string naskbanUserName, string naskbanPassword);

        /// <summary>
        /// justify naskban links
        /// </summary>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        void JustifyNaskbanPageNumbers(string naskbanUserName, string naskbanPassword);

        /// <summary>
        /// import naskban ganjoor matchings
        /// </summary>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        void ImportNaskbanGanjoorPoemMatchFindings(string naskbanUserName, string naskbanPassword);

        /// <summary>
        /// import paper sources from museum
        /// </summary>
        /// <param name="poetid"></param>
        void ImportPaperSourcesFromMuseum(int poetid);

        /// <summary>
        /// discover poet naskban paper sources
        /// </summary>
        /// <param name="poetid"></param>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        void DiscoverPoetNaskbanPaperSources(int poetid, string naskbanUserName, string naskbanPassword);

        /// <summary>
        /// category paper sources
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPaperSource[]>> GetCategoryPaperSourcesAsync(int categoryId);


        /// <summary>
        /// mark naskban links as human reviewed
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="parentCatId"></param>
        /// <param name="humanReviewed"></param>
        void MarkNaskbanLinksAsHumanReviewed(int bookId, int parentCatId, bool humanReviewed);

        /// <summary>
        /// mark naskban links a text original for a category
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="parentCatId"></param>
        /// <param name="textOriginal"></param>
        void MarkNaskbanLinksAsTextOriginal(int bookId, int parentCatId, bool textOriginal);

        /// <summary>
        /// delete poem related naskban images by url
        /// </summary>
        /// <param name="naskbanUrl"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePoemRelatedNaskbanImagesByNaskbanUrlAsync(string naskbanUrl);

        /// <summary>
        /// get category poem related images
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<PoemRelatedImageEx[]>> GetCatPoemImagesAsync(int catId);

        /// <summary>
        /// filling poem formats
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartFillingSectionsPoemFormats();


        /// <summary>
        /// extracting quoted poems
        /// </summary>
        /// <returns></returns>
        RServiceResult<bool> StartExtractingQuotedPoems();

        /// <summary>
        /// regenerate related poems pages
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        RServiceResult<bool> StartRegeneratingRelatedPoemsPages(Guid editingUserId);

        /// <summary>
        /// generate related poems page
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="poetId"></param>
        /// <param name="relatedPoetId"></param>
        /// <returns></returns>
        RServiceResult<bool> StartRegeneratingRelatedPoemsPageAsync(Guid editingUserId, int poetId, int relatedPoetId);

        /// <summary>
        /// discover related poems
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="relatedPoetId"></param>
        /// <param name="breakOnFirstSimilar"></param>
        /// <param name="relatedSubCatId"></param>
        /// <param name="insertReverse"></param>
        /// <returns></returns>
        RServiceResult<bool> StartDiscoverRelatedPoems(int poetId, int relatedPoetId, bool breakOnFirstSimilar, int? relatedSubCatId, bool insertReverse);

        /// <summary>
        /// get quoted poems for a poem
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="skip"></param>
        /// <param name="itemsCount"></param>
        /// <param name="onlyClaimedByBothPoets"></param>
        /// <param name="published"></param>
        /// <param name="chosenForMainList"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorQuotedPoemViewModel[]>> GetGanjoorQuotedPoemsForPoemAsync(int poemId, int skip, int itemsCount, bool? onlyClaimedByBothPoets, bool? published, bool? chosenForMainList);

        /// <summary>
        /// two poems quoted records
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="relatedPoemId"></param>
        /// <param name="published"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorQuotedPoemViewModel[]>> GetGanjoorQuotedPoemsForRelatedAsync(int poemId, int relatedPoemId, bool? published);

        /// <summary>
        /// get quoted poems
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="relatedPoetId"></param>
        /// <param name="chosen"></param>
        /// <param name="published"></param>
        /// <param name="claimed"></param>
        /// <param name="indirect"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorQuotedPoemViewModel[]>> GetGanjoorQuotedPoemsAsync(int? poetId, int? relatedPoetId, bool? chosen, bool? published, bool? claimed, bool? indirect);

        /// <summary>
        /// get quoted by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorQuotedPoemViewModel>> GetGanjoorQuotedPoemByIdAsync(Guid id);

        /// <summary>
        /// insert quoted poem
        /// </summary>
        /// <param name="quoted"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorQuotedPoemViewModel>> InsertGanjoorQuotedPoemAsync(GanjoorQuotedPoemViewModel quoted, Guid editingUserId);

        /// <summary>
        /// update quoted poem
        /// </summary>
        /// <param name="quoted"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateGanjoorQuotedPoemsAsync(GanjoorQuotedPoemViewModel quoted, Guid editingUserId);

        /// <summary>
        /// delete quoted by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteGanjoorQuotedPoemByIdAsync(Guid id, Guid editingUserId);

        /// <summary>
        /// suggest new quote
        /// </summary>
        /// <param name="quoted"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorQuotedPoemViewModel>> SuggestGanjoorQuotedPoemAsync(GanjoorQuotedPoemViewModel quoted, Guid userId);

        /// <summary>
        /// get user or all quoted suggestions
        /// </summary>
        /// <param name="userId">if sent empty returns all suggestions</param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorQuotedPoemViewModel[] Items)>> GetUserQuotedSuggestionsAsync(Guid userId, PagingParameterModel paging);

        /// <summary>
        /// next unmoderated quoted poem
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorQuotedPoemViewModel>> GetNextUnmoderatedGanjoorQuotedPoemAsync(int skip);

        /// <summary>
        /// unmoderated quoted poems count
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnmoderatedGanjoorQuotedsCountAsync();

        /// <summary>
        /// moderate quoted poems
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ModerateGanjoorQuotedPoemAsync(GanjoorQuotedPoemModerationViewModel model, Guid userId);

        /// <summary>
        /// digital source from tag
        /// </summary>
        /// <param name="sourceUrlSlug"></param>
        /// <returns></returns>
        Task<RServiceResult<DigitalSource>> GetDigitalSourceFromTagAsync(string sourceUrlSlug);

        /// <summary>
        /// tag with sources
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="source"></param>
        void TagCategoryWithSource(int catId, DigitalSource source);

        /// <summary>
        /// update digital sources stats
        /// </summary>
        /// <param name="editingUserId"></param>
        void UpdateDigitalSourcesStats(Guid editingUserId);

        /// <summary>
        /// add page
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPage>> AddPageAsync(GanjoorPage page);

        /// <summary>
        /// build word counts
        /// </summary>
        /// <param name="reset"></param>
        /// <param name="poetId"></param>
        Task BuildCategoryWordCountsAsync(bool reset, int poetId);

        /// <summary>
        /// fill CategoryWordCounts.RowNmbrInCat
        /// </summary>
        void FillCategoryWordCountsRowNmbrInCat();

        /// <summary>
        /// fill CategoryWordCountSummaries
        /// </summary>
        void FillCategoryWordCountSummaries();

        /// <summary>
        /// category words count
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="term"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, CategoryWordCount[] Items)>> GetCategoryWordCountsAsync(int catId, string term, PagingParameterModel paging);

        /// <summary>
        /// category words summary
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<CategoryWordCountSummary>> GetCategoryWordCountSummaryAsync(int catId);

        /// <summary>
        /// comparison of word counts for poets
        /// </summary>
        /// <param name="term"></param>
        /// <param name="paging"></param>
        /// <param name="parentCatId"></param>
        /// <param name="poetId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PoetOrCatWordStat[] Items)>> GetCategoryWordCountsBySubCatsAsync(string term, PagingParameterModel paging, int? parentCatId, int? poetId);

        /// <summary>
        /// get CategoryWordCount for a specific term in a category
        /// </summary>
        /// <param name="term"></param>
        /// <param name="catId"></param>
        /// <param name="poetId"></param>
        /// <returns></returns>
        Task<RServiceResult<CategoryWordCount>> GetCategoryWordCountByTermAsync(string term, int? catId, int? poetId);

        /// <summary>
        /// tajik import from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        RServiceResult<bool> TajikImportFromSqlite(int poetId, string filePath);

        /// <summary>
        /// tajik poets
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorTajikPoet[]>> GetTajikPoetsAsync();

        /// <summary>
        /// tajik page by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorPageCompleteViewModel>> GetTajikPageByUrlAsync(string url, bool catPoems = false);


        /// <summary>
        /// search tajik poems
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> SearchTajikAsync(PagingParameterModel paging, string term, int? poetId, int? catId);



        /// <summary>
        /// fill couplet summaries using open ai
        /// </summary>
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        void OpenAIStartFillingCoupletSummaries(int startFrom, int count);


        /// <summary>
        /// fill poem summaries using open ai
        /// </summary>
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        void OpenAIStartFillingPoemSummaries(int startFrom, int count);

        /// <summary>
        /// geo tag poems using AI
        /// </summary>
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        void OpenAIStartFillingGeoLocations(int startFrom, int count);

        /// <summary>
        /// send cat correction
        /// </summary>
        /// <param name="correction"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCatCorrectionViewModel>> SuggestCatCorrectionAsync(GanjoorCatCorrectionViewModel correction);


        /// <summary>
        /// delete unreviewed user corrections for a cat
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteCatCorrectionsAsync(Guid userId, int catId);

        /// <summary>
        /// last unreviewed user correction for a cat
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCatCorrectionViewModel>> GetLastUnreviewedUserCorrectionForCatAsync(Guid userId, int catId);



        /// <summary>
        /// get user or all corrections for categories
        /// </summary>
        /// <param name="userId">if sent empty returns all corrections</param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorCatCorrectionViewModel[] Items)>> GetUserCatCorrectionsAsync(Guid userId, PagingParameterModel paging);


        /// <summary>
        /// cat effectinve corrections
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorCatCorrectionViewModel[] Items)>> GetCatEffectiveCorrectionsAsync(int catId, PagingParameterModel paging);


        /// <summary>
        /// get cat correction by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCatCorrectionViewModel>> GetCatCorrectionByIdAsync(int id);

        /// <summary>
        /// get next unreviewed cat correction
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="onlyUserCorrections"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCatCorrectionViewModel>> GetNextUnreviewedCatCorrectionAsync(int skip, bool onlyUserCorrections);


        /// <summary>
        /// unreviewed cat corrections count
        /// </summary>
        /// <param name="onlyUserCorrections"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreviewedCatCorrectionCountAsync(bool onlyUserCorrections);


        /// <summary>
        /// moderate cat correction
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="moderation"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorCatCorrectionViewModel>> ModerateCatCorrectionAsync(Guid userId,
            GanjoorCatCorrectionViewModel moderation);


    }
}
