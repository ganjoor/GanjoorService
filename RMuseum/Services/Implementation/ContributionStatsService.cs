using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Generic.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// contributions stats service implementation
    /// </summary>
    public class ContributionStatsService : IContributionStatsService
    {
        /// <summary>
        /// approved edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedEditsGroupedByDateAsync(PagingParameterModel paging, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByDateViewModel>.Paginate(
                   _context.GanjoorPoemCorrections
                        .Where(c =>
                        c.AffectedThePoem == true
                        &&
                        (userId == null || c.UserId == userId)
                        )
                        .GroupBy(a => a.Date.Date)
                        .Select(a => new GroupedByDateViewModel()
                        {
                            Date = a.Key.Date.ToString(),
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Date)
                   , paging));
            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// approved edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedEditsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByUserViewModel>.Paginate(
                        _context.GanjoorPoemCorrections
                        .Join
                        (
                            _context.Users,
                            correction => correction.UserId,
                            user => user.Id,
                            (correction, user) => new
                            {
                                correction.Date,
                                UserId = user.Id,
                                UserName = user.NickName,
                                correction.AffectedThePoem,
                            }
                        )
                        .Where(f =>
                         f.AffectedThePoem
                        &&
                        (day == null || f.Date.Date == day) && (userId == null || f.UserId == userId))
                        .GroupBy(a => new { a.UserId, a.UserName }).Select(a => new GroupedByUserViewModel()
                        {
                            UserId = a.Key.UserId,
                            UserName = a.Key.UserName,
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Number)
                        , paging));

            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// summed up stats of approved poem corrections
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<SummedUpViewModel>> GetApprrovedEditsSummedUpStatsAsync()
        {
            try
            {
                return new RServiceResult<SummedUpViewModel>
                    (
                    new SummedUpViewModel()
                    {
                        Days = await _context.GanjoorPoemCorrections
                        .Where(f => f.AffectedThePoem
                        )
                        .GroupBy(f => f.Date.Date).CountAsync(),
                        TotalCount = await _context.GanjoorPoemCorrections

                        .Where(f => f.AffectedThePoem
                        )
                        .CountAsync(),
                        UserIds = await _context.GanjoorPoemCorrections
                        .Where(f => f.AffectedThePoem
                        )
                        .GroupBy(f => f.UserId).CountAsync(),
                    }
                    );

            }
            catch (Exception e)
            {
                return new RServiceResult<SummedUpViewModel>(null, e.ToString());
            }
        }

        /// <summary>
        /// approved section edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedSectionEditsGroupedByDateAsync(PagingParameterModel paging, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByDateViewModel>.Paginate(
                   _context.GanjoorPoemSectionCorrections
                        .Where(c =>
                        c.AffectedThePoem == true
                        &&
                        (userId == null || c.UserId == userId)
                        )
                        .GroupBy(a => a.Date.Date)
                        .Select(a => new GroupedByDateViewModel()
                        {
                            Date = a.Key.Date.ToString(),
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Date)
                   , paging));
            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// approved section edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedSectionEditsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByUserViewModel>.Paginate(
                        _context.GanjoorPoemSectionCorrections
                        .Join
                        (
                            _context.Users,
                            correction => correction.UserId,
                            user => user.Id,
                            (correction, user) => new
                            {
                                correction.Date,
                                UserId = user.Id,
                                UserName = user.NickName,
                                correction.AffectedThePoem,
                            }
                        )
                        .Where(f =>
                         f.AffectedThePoem
                        &&
                        (day == null || f.Date.Date == day) && (userId == null || f.UserId == userId))
                        .GroupBy(a => new { a.UserId, a.UserName }).Select(a => new GroupedByUserViewModel()
                        {
                            UserId = a.Key.UserId,
                            UserName = a.Key.UserName,
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Number)
                        , paging));

            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// summed up stats of approved section corrections
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<SummedUpViewModel>> GetApprrovedSectionEditsSummedUpStatsAsync()
        {
            try
            {
                return new RServiceResult<SummedUpViewModel>
                    (
                    new SummedUpViewModel()
                    {
                        Days = await _context.GanjoorPoemSectionCorrections
                        .Where(f => f.AffectedThePoem
                        )
                        .GroupBy(f => f.Date.Date).CountAsync(),
                        TotalCount = await _context.GanjoorPoemSectionCorrections

                        .Where(f => f.AffectedThePoem
                        )
                        .CountAsync(),
                        UserIds = await _context.GanjoorPoemSectionCorrections
                        .Where(f => f.AffectedThePoem
                        )
                        .GroupBy(f => f.UserId).CountAsync(),
                    }
                    );

            }
            catch (Exception e)
            {
                return new RServiceResult<SummedUpViewModel>(null, e.ToString());
            }
        }


        /// <summary>
        /// approved cat edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedCatEditsGroupedByDateAsync(PagingParameterModel paging, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByDateViewModel>.Paginate(
                   _context.GanjoorCatCorrections
                        .Where(c =>
                        c.Result == Models.Ganjoor.CorrectionReviewResult.Approved
                        &&
                        (userId == null || c.UserId == userId)
                        )
                        .GroupBy(a => a.Date.Date)
                        .Select(a => new GroupedByDateViewModel()
                        {
                            Date = a.Key.Date.ToString(),
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Date)
                   , paging));
            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// approved cat edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedCatEditsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByUserViewModel>.Paginate(
                        _context.GanjoorCatCorrections
                        .Join
                        (
                            _context.Users,
                            correction => correction.UserId,
                            user => user.Id,
                            (correction, user) => new
                            {
                                correction.Date,
                                UserId = user.Id,
                                UserName = user.NickName,
                                correction.Result,
                            }
                        )
                        .Where(f =>
                         f.Result == Models.Ganjoor.CorrectionReviewResult.Approved
                        &&
                        (day == null || f.Date.Date == day) && (userId == null || f.UserId == userId))
                        .GroupBy(a => new { a.UserId, a.UserName }).Select(a => new GroupedByUserViewModel()
                        {
                            UserId = a.Key.UserId,
                            UserName = a.Key.UserName,
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Number)
                        , paging));

            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// summed up stats of approved cat corrections
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<SummedUpViewModel>> GetApprrovedCatEditsSummedUpStatsAsync()
        {
            try
            {
                return new RServiceResult<SummedUpViewModel>
                    (
                    new SummedUpViewModel()
                    {
                        Days = await _context.GanjoorCatCorrections
                        .Where(f => f.Result == Models.Ganjoor.CorrectionReviewResult.Approved
                        )
                        .GroupBy(f => f.Date.Date).CountAsync(),
                        TotalCount = await _context.GanjoorCatCorrections

                        .Where(f => f.Result == Models.Ganjoor.CorrectionReviewResult.Approved
                        )
                        .CountAsync(),
                        UserIds = await _context.GanjoorCatCorrections
                        .Where(f => f.Result == Models.Ganjoor.CorrectionReviewResult.Approved
                        )
                        .GroupBy(f => f.UserId).CountAsync(),
                    }
                    );

            }
            catch (Exception e)
            {
                return new RServiceResult<SummedUpViewModel>(null, e.ToString());
            }
        }


        /// <summary>
        /// approved related songs daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedRelatedSongsGroupedByDateAsync(PagingParameterModel paging, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByDateViewModel>.Paginate(
                   _context.GanjoorPoemMusicTracks
                        .Where(c =>
                        c.Approved
                        &&
                        (userId == null || c.SuggestedById == userId)
                        )
                        .GroupBy(a => a.ApprovalDate.Date)
                        .Select(a => new GroupedByDateViewModel()
                        {
                            Date = a.Key.Date.ToString(),
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Date)
                   , paging));
            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// approved related songs grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedRelatedSongsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByUserViewModel>.Paginate(
                        _context.GanjoorPoemMusicTracks
                        .Join
                        (
                            _context.Users,
                            correction => correction.SuggestedById,
                            user => user.Id,
                            (correction, user) => new
                            {
                                correction.ApprovalDate,
                                UserId = user.Id,
                                UserName = user.NickName,
                                correction.Approved,
                            }
                        )
                        .Where(f =>
                         f.Approved
                        &&
                        (day == null || f.ApprovalDate.Date == day) && (userId == null || f.UserId == userId))
                        .GroupBy(a => new { a.UserId, a.UserName }).Select(a => new GroupedByUserViewModel()
                        {
                            UserId = a.Key.UserId,
                            UserName = a.Key.UserName,
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Number)
                        , paging));

            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// summed up stats of approved related songs
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<SummedUpViewModel>> GetApprovedRelatedSongsSummedUpStatsAsync()
        {
            try
            {
                return new RServiceResult<SummedUpViewModel>
                    (
                    new SummedUpViewModel()
                    {
                        Days = await _context.GanjoorPoemMusicTracks
                        .Where(f => f.Approved
                        )
                        .GroupBy(f => f.ApprovalDate.Date).CountAsync(),
                        TotalCount = await _context.GanjoorPoemMusicTracks

                        .Where(f => f.Approved
                        )
                        .CountAsync(),
                        UserIds = await _context.GanjoorPoemMusicTracks
                        .Where(f => f.Approved
                        )
                        .GroupBy(f => f.SuggestedById).CountAsync(),
                    }
                    );

            }
            catch (Exception e)
            {
                return new RServiceResult<SummedUpViewModel>(null, e.ToString());
            }
        }

        /// <summary>
        /// approved quoted poems daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>> GetApprovedQuotedPoemsGroupedByDateAsync(PagingParameterModel paging, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByDateViewModel>.Paginate(
                   _context.GanjoorQuotedPoems
                        .Where(c =>
                        c.Published && c.SuggestionDate != null
                        &&
                        (userId == null || c.SuggestedById == userId)
                        )
                        .GroupBy(a => a.SuggestionDate!.Value.Date)
                        .Select(a => new GroupedByDateViewModel()
                        {
                            Date = a.Key.Date.ToString(),
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Date)
                   , paging));
            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// approved quoted poems grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>> GetApprovedQuotedPoemsGroupedByUserAsync(PagingParameterModel paging, DateTime? day, Guid? userId)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByUserViewModel>.Paginate(
                        _context.GanjoorQuotedPoems
                        .Join
                        (
                            _context.Users,
                            correction => correction.SuggestedById,
                            user => user.Id,
                            (correction, user) => new
                            {
                                correction.SuggestionDate,
                                UserId = user.Id,
                                UserName = user.NickName,
                                correction.Published,
                            }
                        )
                        .Where(f =>
                         f.Published && f.SuggestionDate != null
                        &&
                        (day == null || f.SuggestionDate!.Value.Date == day) && (userId == null || f.UserId == userId))
                        .GroupBy(a => new { a.UserId, a.UserName }).Select(a => new GroupedByUserViewModel()
                        {
                            UserId = a.Key.UserId,
                            UserName = a.Key.UserName,
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Number)
                        , paging));

            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks)>((null, null), e.ToString());
            }
        }

        /// <summary>
        /// summed up stats of approved quoted poems
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<SummedUpViewModel>> GetApprovedQuotedPoemsSummedUpStatsAsync()
        {
            try
            {
                return new RServiceResult<SummedUpViewModel>
                    (
                    new SummedUpViewModel()
                    {
                        Days = await _context.GanjoorQuotedPoems
                        .Where(f => f.Published && f.SuggestionDate != null
                        )
                        .GroupBy(f => f.SuggestionDate!.Value.Date).CountAsync(),
                        TotalCount = await _context.GanjoorQuotedPoems

                        .Where(f => f.Published
                        )
                        .CountAsync(),
                        UserIds = await _context.GanjoorQuotedPoems
                        .Where(f => f.Published
                        )
                        .GroupBy(f => f.SuggestedById).CountAsync(),
                    }
                    );

            }
            catch (Exception e)
            {
                return new RServiceResult<SummedUpViewModel>(null, e.ToString());
            }
        }




        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public ContributionStatsService(RMuseumDbContext context)
        {
            _context = context;
        }
    }
}
