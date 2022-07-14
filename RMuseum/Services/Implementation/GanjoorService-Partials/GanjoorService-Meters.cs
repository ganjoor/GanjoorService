using Microsoft.EntityFrameworkCore;
using RSecurityBackend.Models.Generic;
using System;
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
        /// transfer poems and sections from a meter to another one
        /// </summary>
        /// <param name="srcMeterId"></param>
        /// <param name="destMeterId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> TransferMeterAsync(int srcMeterId, int destMeterId)
        {
            try
            {
                if (srcMeterId == destMeterId)
                    return new RServiceResult<bool>(false, "srcMeterId == destMeterId");
                var src = await _context.GanjoorMetres.Where(m => m.Id == srcMeterId).SingleAsync();
                var dest = await _context.GanjoorMetres.Where(m => m.Id == destMeterId).SingleAsync();
                var poems = await _context.GanjoorPoems.Where(p => p.GanjoorMetreId == srcMeterId).ToListAsync();
                foreach (var poem in poems)
                {
                    poem.GanjoorMetreId = destMeterId;
                }
                _context.UpdateRange(poems);

                var sections = await _context.GanjoorPoemSections.Where(s => s.GanjoorMetreId == srcMeterId).ToListAsync();
                foreach (var section in sections)
                {
                    section.GanjoorMetreId = destMeterId;
                }
                _context.UpdateRange(sections);
                await _context.SaveChangesAsync();

                dest.VerseCount += src.VerseCount;
                _context.Update(dest);
                await _context.SaveChangesAsync();

                _context.Remove(src);
                await _context.SaveChangesAsync();

                foreach (var section in sections)
                {
                    if (!string.IsNullOrEmpty(section.RhymeLetters))
                    {
                        UpdateRelatedSections(destMeterId, section.RhymeLetters);
                    }
                }

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}