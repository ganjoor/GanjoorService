using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// User Visits Tracking Service Implementation
    /// </summary>
    public class UserVisitsTrackingService : IUserVisitsTrackingService
    {
        /// <summary>
        /// add record
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddAsync(Guid userId, int poemId)
        {
            bool keepHistory = false;
            var kRes = await _optionsService.GetValueAsync("KeepHistory", userId);
            if (!string.IsNullOrEmpty(kRes.Result))
                bool.TryParse(kRes.Result, out keepHistory);
            if (!keepHistory)
                return new RServiceResult<bool>(false);

            var oldTracks = await _context.GanjoorUserPoemVisits.Where(v => v.PoemId == poemId && v.UserId == userId).ToArrayAsync();
            _context.RemoveRange(oldTracks);
            await _context.SaveChangesAsync();

            GanjoorUserPoemVisit visit = new GanjoorUserPoemVisit()
            {
                UserId = userId,
                PoemId = poemId,
                DateTime = DateTime.Now
            };

            _context.Add(visit);
            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// delete record
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteAsync(Guid userId, Guid recordId)
        {
            var rec = await _context.GanjoorUserPoemVisits.Where(v => v.UserId == userId && v.Id == recordId).SingleOrDefaultAsync();//userId is not needed but it is added to prevent accidental delete of other users data
            if(rec == null)
                return new RServiceResult<bool>(false, "record not found!");
            _context.Remove(rec);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }



        /// <summary>
        /// start or stop tracking user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SwitchTrackingAsync(Guid userId, bool start)
        {
            var res = await _optionsService.SetAsync("KeepHistory", start.ToString(), userId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return new RServiceResult<bool>(false, res.ExceptionString);
            if(!start)
            {
                try
                {
                    var recs = await _context.GanjoorUserPoemVisits.Where(v => v.UserId == userId).ToArrayAsync();
                    _context.RemoveRange(recs);
                    await _context.SaveChangesAsync();
                }
                catch (Exception exp)
                {
                    return new RServiceResult<bool>(false, exp.ToString());
                }
            }
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// get user history
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] HistoryItems)>> GetUserHistoryAsync(PagingParameterModel paging, Guid userId)
        {
            var source =
                 _context.GanjoorUserPoemVisits
                 .Include(b => b.Poem).ThenInclude(p => p.Cat).ThenInclude(c => c.Poet)
                 .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.DateTime)
                .AsQueryable();

            (PaginationMetadata PagingMeta, GanjoorUserPoemVisit[] HistoryItems) historiesPage =
                await QueryablePaginator<GanjoorUserPoemVisit>.Paginate(source, paging);


            List<GanjoorUserBookmarkViewModel> result = new List<GanjoorUserBookmarkViewModel>();
            foreach (var historyItem in historiesPage.HistoryItems)
            {
                var verses = await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == historyItem.PoemId && v.CoupletIndex == 0).OrderBy(v => v.VOrder).ToListAsync();
                result.Add
                    (
                    new GanjoorUserBookmarkViewModel()
                    {
                        Id = historyItem.Id,
                        PoetName = historyItem.Poem.Cat.Poet.Nickname,
                        PoetImageUrl = $"{WebServiceUrl.Url}{$"/api/ganjoor/poet/image/{historyItem.Poem.FullUrl.Substring(1, historyItem.Poem.FullUrl.IndexOf('/', 1) - 1)}.gif"}",
                        PoemFullTitle = historyItem.Poem.FullTitle,
                        PoemFullUrl = historyItem.Poem.FullUrl,
                        CoupletIndex = 0,
                        VerseText = verses.Count == 0 ? "" : verses[0].Text,
                        Verse2Text = verses.Count < 2 ? "" : verses[1].Text,
                        DateTime = historyItem.DateTime
                    }
                    );
            }
            
            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorUserBookmarkViewModel[] HistoryItems)>
                ((historiesPage.PagingMeta, result.ToArray()));
        }


        /// <summary>
        /// options service
        /// </summary>

        protected readonly IRGenericOptionsService _optionsService;

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="optionsService"></param>
        public UserVisitsTrackingService(RMuseumDbContext context, IRGenericOptionsService optionsService)
        {
            _context = context;
            _optionsService = optionsService;
        }
    }
}
