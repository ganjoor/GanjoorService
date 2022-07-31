using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
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
        /// add poem geo tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemGeoDateTag>> AddPoemGeoDateTagAsync(PoemGeoDateTag tag)
        {
            try
            {
                _context.PoemGeoDateTags.Add(tag);
                await _context.SaveChangesAsync();
                return new RServiceResult<PoemGeoDateTag>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemGeoDateTag>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update poem tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdatePoemGeoDateTagAsync(PoemGeoDateTag tag)
        {
            try
            {
                var dbTag = await _context.PoemGeoDateTags.Where(t => t.Id == tag.Id).SingleAsync();
                dbTag.PoemId = tag.PoemId;
                dbTag.CoupletIndex = tag.CoupletIndex;
                dbTag.LocationId = tag.LocationId;
                dbTag.LunarYear = tag.LunarYear;
                dbTag.LunarMonth = tag.LunarMonth;
                dbTag.LunarDay = tag.LunarDay;
                dbTag.PoemId = tag.PoemId;
                dbTag.LunarDateTotalNumber = tag.LunarDateTotalNumber;
                _context.Update(dbTag);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete poem tag
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoemGeoDateTagAsync(int id)
        {
            try
            {
                var dbTag = await _context.PoemGeoDateTags.Where(t => t.Id == id).SingleAsync();
                _context.Remove(dbTag);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get poem tags ordered by LunarDateTotalNumber then by Id
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemGeoDateTag[]>> GetPoemGeoDateTagsAsync(int poemId)
        {
            try
            {
                return new RServiceResult<PoemGeoDateTag[]>(
                    await _context.PoemGeoDateTags.AsNoTracking().Where(t => t.PoemId == poemId)
                                .OrderBy(t => t.LunarDateTotalNumber)
                                .ThenBy(t => t.Id)
                                .ToArrayAsync()
            );
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemGeoDateTag[]>(null, exp.ToString());
            }
        }



    }
}
