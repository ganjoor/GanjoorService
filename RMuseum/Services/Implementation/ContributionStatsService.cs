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
        /// approved edits grouped by user / daily
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateUserViewModel[] Tracks)>> GetApprovedEditsGroupedByDateAndUserAsync(PagingParameterModel paging)
        {
            try
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateUserViewModel[] Tracks)>(
                    await QueryablePaginator<GroupedByDateUserViewModel>.Paginate(
                        _context.GanjoorPoemCorrections
                        .Join
                        (
                            _context.Users,
                            correction => correction.UserId,
                            user => user.Id,
                            (correction, user) => new
                            {
                                UserId = user.Id,
                                UserName = user.NickName,
                                DateTime = correction.Date,
                                correction.AffectedThePoem,
                            }
                        )
                        .Where(f => f.AffectedThePoem)
                        .GroupBy(a => new { a.DateTime.Date, a.UserId, a.UserName })
                        .Select(a => new GroupedByDateUserViewModel()
                        {
                            Date = a.Key.Date.ToString(),
                            UserId = a.Key.UserId,
                            UserName = a.Key.UserName,
                            Number = a.Count(),
                        }).OrderByDescending(s => s.Date).ThenBy(s => s.Number)
                        , paging));

            }
            catch (Exception e)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GroupedByDateUserViewModel[] Tracks)>((null, null), e.ToString());
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
