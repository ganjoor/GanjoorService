using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
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
        /// great-circle distance between two points, in kilometers - used only for catching
        /// obviously-duplicate new-location suggestions (someone retyping a place that's already
        /// in the catalog under slightly different coordinates), so this doesn't need to be
        /// geodesy-grade precise, just good enough at the few-kilometers scale
        /// </summary>
        private double _GeoDistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLng = (lng2 - lng1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private int? _PrepareLunarDateTotalNumber(PoemGeoDateTag tag)
        {
            if (tag.LunarYear == null)
                return null;
            int res = (int)tag.LunarYear * 10000;
            if(tag.LunarMonth != null)
            {
                res += (int)tag.LunarMonth * 100;

                if(tag.LunarDay != null)
                {
                    res += (int)tag.LunarDay;
                }
            }
            return res;

        }

        /// <summary>
        /// add poem geo tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemGeoDateTag>> AddPoemGeoDateTagAsync(PoemGeoDateTag tag)
        {
            try
            {
                tag.LunarDateTotalNumber = _PrepareLunarDateTotalNumber(tag);
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
                dbTag.VerifiedDate = tag.VerifiedDate;
                dbTag.IgnoreInCategory = tag.IgnoreInCategory;
                dbTag.Note = string.IsNullOrWhiteSpace(tag.Note) ? null : tag.Note.Trim();
                dbTag.LunarDateTotalNumber = _PrepareLunarDateTotalNumber(tag);
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
                var tags = await _context.PoemGeoDateTags.AsNoTracking().Include(t => t.Location).Include(t => t.Person)
                               .Where(t => t.PoemId == poemId && t.MachineGenerated == false)
                               .OrderBy(t => t.LunarDateTotalNumber)
                               .ThenBy(t => t.Id)
                               .ToArrayAsync();
                return new RServiceResult<PoemGeoDateTag[]>(tags);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemGeoDateTag[]>(null, exp.ToString());
            }
        }



        /// <summary>
        /// get a categoty poem tags
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="ignoreSumup"></param>
        /// <param name="onlyWithLocation">leave out tags without a location (person tags live in the same table and
        /// can be thousands for a big book) - what a map needs</param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemGeoDateTag[]>> GetCatPoemGeoDateTagsAsync(int catId, bool ignoreSumup = false, bool onlyWithLocation = false)
        {
            try
            {
                var cat = await _context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();

                var catIds = new List<int>() { catId };
                if (cat.SumUpSubsGeoLocations && !ignoreSumup)
                {
                    // descendants level by level: one light query per tree level instead of loading
                    // every sub-category with its children and querying the tags category by category
                    var frontier = new List<int>() { catId };
                    while (frontier.Count > 0)
                    {
                        var parents = frontier;
                        frontier = await _context.GanjoorCategories.AsNoTracking()
                            .Where(c => c.ParentId != null && parents.Contains(c.ParentId.Value))
                            .Select(c => c.Id).ToListAsync();
                        catIds.AddRange(frontier);
                    }
                }

                var tags = await _context.PoemGeoDateTags.AsNoTracking().Include(t => t.Location).Include(t => t.Poem)
                                .Where(t => catIds.Contains(t.Poem.CatId) && t.MachineGenerated == false && t.IgnoreInCategory == false
                                        && (!onlyWithLocation || t.LocationId != null))
                                .OrderBy(t => t.LunarDateTotalNumber)
                                .ThenBy(t => t.PoemId)
                                .ToArrayAsync();
                foreach (var tag in tags)
                {
                    tag.Poem.HtmlText = "";
                    tag.Poem.PlainText = "";
                }

                return new RServiceResult<PoemGeoDateTag[]>(tags);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemGeoDateTag[]>(null, exp.ToString());
            }
        }



    }
}
