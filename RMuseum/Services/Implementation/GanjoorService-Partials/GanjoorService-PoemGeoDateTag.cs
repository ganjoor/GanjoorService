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
                var tags = await _context.PoemGeoDateTags.AsNoTracking().Include(t => t.Location).Include(t => t.Person).Where(t => t.PoemId == poemId)
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
        /// <returns></returns>
        public async Task<RServiceResult<PoemGeoDateTag[]>> GetCatPoemGeoDateTagsAsync(int catId, bool ignoreSumup = false)
        {
            try
            {
                var cat = await _context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();

                var tags = await _context.PoemGeoDateTags.AsNoTracking().Include(t => t.Location).Include(t => t.Poem).Where(t => t.Poem.CatId == catId && t.IgnoreInCategory == false)
                                .OrderBy(t => t.LunarDateTotalNumber)
                                .ThenBy(t => t.PoemId)
                                .ToArrayAsync();
                foreach (var tag in tags)
                {
                    tag.Poem.HtmlText = "";
                    tag.Poem.PlainText = "";
                }

                if(cat.SumUpSubsGeoLocations && !ignoreSumup)
                {
                    List<PoemGeoDateTag> summedTags = new List<PoemGeoDateTag>(tags);
                    List<int> catIdList = new List<int>();
                    await _populateCategoryChildren(catId, catIdList);

                    foreach (var childCatId in catIdList)
                    {
                        var childCatRes = await GetCatPoemGeoDateTagsAsync(childCatId, true);
                        if (!string.IsNullOrEmpty(childCatRes.ExceptionString))
                        {
                            return new RServiceResult<PoemGeoDateTag[]>(null, childCatRes.ExceptionString);
                        }
                        if(childCatRes.Result.Length > 0)
                        {
                            summedTags.AddRange(childCatRes.Result);
                        }
                    }
                    
                    return new RServiceResult<PoemGeoDateTag[]>(summedTags.OrderBy(t => t.LunarDateTotalNumber).ThenBy(t => t.PoemId).ToArray());
                }
                else
                {
                    return new RServiceResult<PoemGeoDateTag[]>(tags);
                }
                
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemGeoDateTag[]>(null, exp.ToString());
            }
        }



    }
}
