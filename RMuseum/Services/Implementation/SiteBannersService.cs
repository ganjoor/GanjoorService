using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// ganjoor.net banners service
    /// </summary>
    public class SiteBannersService : ISiteBannersService
    {
        /// <summary>
        /// Add site banner
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="fileName"></param>
        /// <param name="alternateText"></param>
        /// <param name="targetUrl"></param>
        /// <param name="active"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorSiteBannerViewModel>> AddSiteBanner(Stream imageStream, string fileName, string alternateText, string targetUrl, bool active)
        {
            try
            {
                RServiceResult<RImage> image = await _imageFileService.Add(null, imageStream, fileName, "SiteBanners");

                if (!string.IsNullOrEmpty(image.ExceptionString))
                {
                    return new RServiceResult<GanjoorSiteBannerViewModel>(null, image.ExceptionString);
                }


                GanjoorSiteBanner banner = new GanjoorSiteBanner()
                {
                    RImage = image.Result,
                    AlternateText = alternateText,
                    TargetUrl = targetUrl,
                    Active = active
                };

                _context.GanjoorSiteBanners.Add(banner);
                await _context.SaveChangesAsync();

                return new RServiceResult<GanjoorSiteBannerViewModel>
                    (
                    new GanjoorSiteBannerViewModel()
                    {
                        Id = banner.Id,
                        ImageUrl = $"api/rimages/{banner.RImageId}.jpg",
                        AlternateText = banner.AlternateText,
                        TargetUrl = banner.TargetUrl,
                        Active = banner.Active
                    }
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorSiteBannerViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// modify site banner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="alternateText"></param>
        /// <param name="targetUrl"></param>
        /// <param name="active"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ModifySiteBanner(int id, string alternateText, string targetUrl, bool active)
        {
            try
            {
                GanjoorSiteBanner target = await _context.GanjoorSiteBanners.Where(b => b.Id == id).SingleOrDefaultAsync();
                if (target == null)
                    return new RServiceResult<bool>(false);//not found

                target.AlternateText = alternateText;
                target.TargetUrl = targetUrl;
                target.Active = active;

                _context.GanjoorSiteBanners.Update(target);

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete site banner
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteSiteBanner(int id)
        {
            try
            {
                GanjoorSiteBanner target = await _context.GanjoorSiteBanners.Where(b => b.Id == id).SingleOrDefaultAsync();
                if (target == null)
                    return new RServiceResult<bool>(false);//not found

                _context.GanjoorSiteBanners.Remove(target);

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get site banners
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorSiteBannerViewModel[]>> GetSiteBanners()
        {
            try
            {
                return new RServiceResult<GanjoorSiteBannerViewModel[]>(
                    await _context.GanjoorSiteBanners
                    .Select(b => new GanjoorSiteBannerViewModel()
                    {
                        Id = b.Id,
                        ImageUrl = $"api/rimages/{b.RImageId}.jpg",
                        AlternateText = b.AlternateText,
                        TargetUrl = b.TargetUrl,
                        Active = b.Active
                    }).ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorSiteBannerViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get a random site banner
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorSiteBannerViewModel>> GetARandomActiveSiteBanner()
        {
            try
            {
                var cacheKeyForIdSet = $"GetARandomActiveSiteBanner::idSet";
                if (!_memoryCache.TryGetValue(cacheKeyForIdSet, out int[] idSet))
                {
                    idSet = await _context.GanjoorSiteBanners.Where(b => b.Active == true).Select(b => b.Id).ToArrayAsync();
                    _memoryCache.Set(cacheKeyForIdSet, idSet);
                }
                if (idSet.Length == 0)
                    return new RServiceResult<GanjoorSiteBannerViewModel>(null);//not found

                Random rnd = new Random(DateTime.Now.Millisecond);
                int id = idSet[rnd.Next(0, idSet.Length)];

                return new RServiceResult<GanjoorSiteBannerViewModel>(
                    await _context.GanjoorSiteBanners
                    .Where(b => b.Id == id)
                    .Select(b => new GanjoorSiteBannerViewModel()
                    {
                        Id = b.Id,
                        ImageUrl = $"api/rimages/{b.RImageId}.jpg",
                        AlternateText = b.AlternateText,
                        TargetUrl = b.TargetUrl,
                        Active = b.Active
                    }).AsNoTracking().SingleAsync()
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorSiteBannerViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Image File Service
        /// </summary>
        protected readonly IImageFileService _imageFileService;

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;



        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="imageFileService"></param>
        /// <param name="memoryCache"></param>
        public SiteBannersService(RMuseumDbContext context,IImageFileService imageFileService, IMemoryCache memoryCache)
        {
            _context = context;
            _imageFileService = imageFileService;
            _memoryCache = memoryCache;
        }
    }
}
