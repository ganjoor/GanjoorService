using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// last unreviewed user correction for a section
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sectionId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemSectionCorrectionViewModel>> GetLastUnreviewedUserCorrectionForSection(Guid userId, int sectionId)
        {
            var dbCorrection = await _context.GanjoorPoemSectionCorrections.AsNoTracking().Include(c => c.User)
                .Where(c => c.UserId == userId && c.SectionId == sectionId && c.Reviewed == false)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (dbCorrection == null)
                return new RServiceResult<GanjoorPoemSectionCorrectionViewModel>(null);

            return new RServiceResult<GanjoorPoemSectionCorrectionViewModel>
                (
                new GanjoorPoemSectionCorrectionViewModel()
                {
                    Id = dbCorrection.Id,
                    SectionId = dbCorrection.SectionId,
                    UserId = dbCorrection.UserId,
                    Rhythm = dbCorrection.Rhythm,
                    OriginalRhythm = dbCorrection.OriginalRhythm,
                    RhythmResult = dbCorrection.RhythmResult,
                    Rhythm2 = dbCorrection.Rhythm2,
                    OriginalRhythm2 = dbCorrection.OriginalRhythm2,
                    RhythmResult2 = dbCorrection.RhythmResult2,
                    BreakFromVerse1VOrder = dbCorrection.BreakFromVerse1VOrder,
                    BreakFromVerse1VOrderResult = dbCorrection.BreakFromVerse1VOrderResult,
                    BreakFromVerse2VOrder = dbCorrection.BreakFromVerse2VOrder,
                    BreakFromVerse2VOrderResult = dbCorrection.BreakFromVerse2VOrderResult,
                    BreakFromVerse3VOrder = dbCorrection.BreakFromVerse3VOrder,
                    BreakFromVerse3VOrderResult = dbCorrection.BreakFromVerse3VOrderResult,
                    BreakFromVerse4VOrder = dbCorrection.BreakFromVerse4VOrder,
                    BreakFromVerse4VOrderResult = dbCorrection.BreakFromVerse4VOrderResult,
                    Note = dbCorrection.Note,
                    Date = dbCorrection.Date,
                    Reviewed = dbCorrection.Reviewed,
                    ReviewNote = dbCorrection.ReviewNote,
                    ReviewDate = dbCorrection.ReviewDate,
                    UserNickname = string.IsNullOrEmpty(dbCorrection.User.NickName) ? dbCorrection.User.Id.ToString() : dbCorrection.User.NickName
                }
                );
        }
    }
}