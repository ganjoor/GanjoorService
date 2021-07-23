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

            await CleanBannersCache();

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
            GanjoorSiteBanner target = await _context.GanjoorSiteBanners.Where(b => b.Id == id).SingleOrDefaultAsync();
            if (target == null)
                return new RServiceResult<bool>(false);//not found

            target.AlternateText = alternateText;
            target.TargetUrl = targetUrl;
            target.Active = active;

            _context.GanjoorSiteBanners.Update(target);

            await _context.SaveChangesAsync();

            await CleanBannersCache();

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// delete site banner
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteSiteBanner(int id)
        {
            GanjoorSiteBanner target = await _context.GanjoorSiteBanners.Where(b => b.Id == id).SingleOrDefaultAsync();
            if (target == null)
                return new RServiceResult<bool>(false);//not found

            _context.GanjoorSiteBanners.Remove(target);

            await _context.SaveChangesAsync();

            await CleanBannersCache();

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// get site banners
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorSiteBannerViewModel[]>> GetSiteBanners()
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

        private const string _cacheKeyForIdSet = "SiteBannersService::GetARandomActiveSiteBanner::idSet";

        private static string GetBannerCacheKey(int bannerId)
        {
            return $"SiteBannersService::BannerCacheKey::{bannerId}";
        }

        private async Task CleanBannersCache()
        {
            int[] idSet = await _context.GanjoorSiteBanners.Select(b => b.Id).ToArrayAsync();
            foreach (int id in idSet)
            {
                var cacheKey = GetBannerCacheKey(id);
                if (_memoryCache.TryGetValue(id, out GanjoorSiteBannerViewModel o))
                {
                    _memoryCache.Remove(id);
                }
            }
            if (_memoryCache.TryGetValue(_cacheKeyForIdSet, out idSet))
            {
                _memoryCache.Remove(_cacheKeyForIdSet);
            }
        }

        /// <summary>
        /// get a random site banner
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorSiteBannerViewModel>> GetARandomActiveSiteBanner()
        {
            if (!_memoryCache.TryGetValue(_cacheKeyForIdSet, out int[] idSet))
            {
                idSet = await _context.GanjoorSiteBanners.Where(b => b.Active == true).Select(b => b.Id).ToArrayAsync();
                _memoryCache.Set(_cacheKeyForIdSet, idSet);
            }
            if (idSet.Length == 0)
                return new RServiceResult<GanjoorSiteBannerViewModel>(null);//no active banner

            Random rnd = new Random(DateTime.Now.Millisecond);
            int id = idSet[rnd.Next(0, idSet.Length)];

            var cachKey = GetBannerCacheKey(id);
            if (!_memoryCache.TryGetValue(cachKey, out GanjoorSiteBannerViewModel banner))
            {
                banner =
                    await _context.GanjoorSiteBanners
                    .Where(b => b.Id == id)
                    .Select(b => new GanjoorSiteBannerViewModel()
                    {
                        Id = b.Id,
                        ImageUrl = $"api/rimages/{b.RImageId}.jpg",
                        AlternateText = b.AlternateText,
                        TargetUrl = b.TargetUrl,
                        Active = b.Active
                    })
                    .AsNoTracking()
                    .SingleAsync();
                _memoryCache.Set(cachKey, banner);
            }

            return new RServiceResult<GanjoorSiteBannerViewModel>(banner);
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
        public SiteBannersService(RMuseumDbContext context, IImageFileService imageFileService, IMemoryCache memoryCache)
        {
            _context = context;
            _imageFileService = imageFileService;
            _memoryCache = memoryCache;
        }
    }
}
