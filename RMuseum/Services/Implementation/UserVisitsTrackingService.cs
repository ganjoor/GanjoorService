using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// User Visits Tracking Service
    /// </summary>
    public class UserVisitsTrackingService
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
