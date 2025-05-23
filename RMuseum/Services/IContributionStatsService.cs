﻿using RMuseum.Models.Generic.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;
using System;
using RMuseum.Models.Ganjoor.ViewModels;

namespace RMuseum.Services
{
    /// <summary>
    /// contributions stats service
    /// </summary>
    public interface IContributionStatsService
    {
        /// <summary>
        /// user contributions
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<UserContributionsViewModel>> GetUserContributionsAsync(Guid userId);

        /// <summary>
        /// summed up stats of users (Days and UserIds are invalid)
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetUsersSummedUpStatsAsync();

        /// <summary>
        /// users grouped by signup date
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetUsersGroupedByDateAsync(PagingParameterModel paging);


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

        /// <summary>
        /// approved quoted poems daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedQuotedPoemsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved quoted poems grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedQuotedPoemsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved quoted poems
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedQuotedPoemsSummedUpStatsAsync();

        /// <summary>
        /// approved comments daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedCommentsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved comments grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedCommentsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);


        /// <summary>
        /// summed up stats of approved comments
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedCommentsSummedUpStatsAsync();

        /// <summary>
        /// approved recitations daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedRecitationsGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved recitations grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedRecitationsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved recitations
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedRecitationsSummedUpStatsAsync();

        /// <summary>
        /// approved museum links daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedMuseumLinksGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved museum links grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedMuseumLinksGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved museum links
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedMuseumLinksSummedUpStatsAsync();

        /// <summary>
        /// approved pinterest links daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedPinterestLinksGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved pinterest links grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedPinterestLinksGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved pinterest links
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedPinterestLinksSummedUpStatsAsync();

        /// <summary>
        /// approved poet spec lines daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedPoetSpecLinesGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved poet spec lines grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedPoetSpecLinesGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved poet spec lines
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedPoetSpecLinesSummedUpStatsAsync();

        /// <summary>
        /// approved poet photos daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedPoetPicturesGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved poet photos grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedPoetPicturesGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved poet photos
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedPoetPicturesSummedUpStatsAsync();

        /// <summary>
        /// approved user notes
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedUserNotesGroupedByDateAsync(PagingParameterModel paging, Guid? userId);

        /// <summary>
        /// approved user notes grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedUserNotesGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId);

        /// <summary>
        /// summed up stats of approved user notes
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<SummedUpViewModel>> GetApprovedUserNotesSummedUpStatsAsync();


    }
}
