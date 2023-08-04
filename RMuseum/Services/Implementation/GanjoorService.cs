using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RMuseum.Models.Artifact;
using RSecurityBackend.Services.Implementation;
using DNTPersianUtils.Core;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Web;
using System.Text.RegularExpressions;
using RMuseum.Models.Auth.Memory;
using System.IO;
using RSecurityBackend.Models.Image;
using FluentFTP;
using System.Drawing;
using RSecurityBackend.Models.Auth.ViewModels;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using RMuseum.Models.Auth.ViewModel;
using System.Net.Http.Headers;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RMuseum.Models.GanjoorIntegration;
using Microsoft.VisualBasic.ApplicationServices;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        /// <summary>
        /// Get List of poets
        /// </summary>
        /// <param name="published"></param>
        /// <param name="includeBio"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetViewModel[]>> GetPoets(bool published, bool includeBio = true)
        {
            var cacheKey = $"/api/ganjoor/poets?published={published}&includeBio={includeBio}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetViewModel[] poets))
            {
                var res =
                 await
                 (from poet in _context.GanjoorPoets.Include(p => p.BirthLocation).Include(p => p.DeathLocation)
                  join cat in _context.GanjoorCategories.Where(c => c.ParentId == null)
                  on poet.Id equals cat.PoetId
                  where !published || poet.Published
                  select new GanjoorPoetViewModel()
                  {
                      Id = poet.Id,
                      Name = poet.Name,
                      Description = includeBio ? poet.Description : null,
                      FullUrl = cat.FullUrl,
                      RootCatId = cat.Id,
                      Nickname = poet.Nickname,
                      Published = poet.Published,
                      ImageUrl = poet.RImageId == null ? "" : $"/api/ganjoor/poet/image{cat.FullUrl}.gif",
                      BirthYearInLHijri = poet.BirthYearInLHijri,
                      DeathYearInLHijri = poet.DeathYearInLHijri,
                      ValidBirthDate = poet.ValidBirthDate,
                      ValidDeathDate = poet.ValidDeathDate,
                      BirthPlace = poet.BirthLocation == null ? "" : poet.BirthLocation.Name,
                      BirthPlaceLatitude = poet.BirthLocation == null ? 0 : poet.BirthLocation.Latitude,
                      BirthPlaceLongitude = poet.BirthLocation == null ? 0 : poet.BirthLocation.Longitude,
                      DeathPlace = poet.DeathLocation == null ? "" : poet.DeathLocation.Name,
                      DeathPlaceLatitude = poet.DeathLocation == null ? 0 : poet.DeathLocation.Latitude,
                      DeathPlaceLongitude = poet.DeathLocation == null ? 0 : poet.DeathLocation.Longitude,
                      PinOrder = poet.PinOrder,
                  }
                  )
                  .AsNoTracking()
                 .ToListAsync();

                StringComparer fa = StringComparer.Create(new CultureInfo("fa-IR"), true);
                res.Sort((a, b) => fa.Compare(a.Nickname, b.Nickname));
                poets = res.ToArray();
                if (AggressiveCacheEnabled)
                    _memoryCache.Set(cacheKey, poets);
            }

            return new RServiceResult<GanjoorPoetViewModel[]>
                (
                    poets
                );
        }

        /// <summary>
        /// get poet by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetById(int id, bool catPoems = false)
        {
            var cacheKey = $"/api/ganjoor/poet/{id}";

            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poetCat))
            {
                var poet = await _context.GanjoorPoets.Where(p => p.Id == id).AsNoTracking().FirstOrDefaultAsync();
                if (poet == null)
                    return new RServiceResult<GanjoorPoetCompleteViewModel>(null);
                var cat = await _context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == id).AsNoTracking().FirstOrDefaultAsync();
                poetCat = (await GetCatById(cat.Id, catPoems)).Result;
                if (poetCat != null && AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poetCat);
                }
            }
            return new RServiceResult<GanjoorPoetCompleteViewModel>(poetCat);
        }

        /// <summary>
        /// get poet by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetPoetByUrl(string url, bool catPoems = false)
        {
            // /hafez/ => /hafez :
            if (url.LastIndexOf('/') == url.Length - 1)
            {
                url = url.Substring(0, url.Length - 1);
            }
            var cat = await _context.GanjoorCategories.Where(c => c.FullUrl == url && c.ParentId == null).AsNoTracking().SingleOrDefaultAsync();
            if (cat == null)
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null);
            return await GetCatById(cat.Id, catPoems);
        }

        /// <summary>
        /// poet image id by url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<RServiceResult<Guid>> GetPoetImageIdByUrl(string url)
        {
            // /hafez/ => /hafez :
            if (url.LastIndexOf('/') == url.Length - 1)
            {
                url = url.Substring(0, url.Length - 1);
            }
            var cat = await _context.GanjoorCategories.Where(c => c.FullUrl == url && c.ParentId == null).AsNoTracking().SingleOrDefaultAsync();
            if (cat == null)
                return new RServiceResult<Guid>(Guid.Empty);
            var poet = await _context.GanjoorPoets.Where(p => p.Id == cat.PoetId).AsNoTracking().SingleOrDefaultAsync();
            return new RServiceResult<Guid>((Guid)poet.RImageId);
        }

        /// <summary>
        /// get cat by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="poems"></param>
        /// <param name="mainSections"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatByUrl(string url, bool poems = false, bool mainSections = false)
        {
            // /hafez/ => /hafez :
            if (url.LastIndexOf('/') == url.Length - 1)
            {
                url = url.Substring(0, url.Length - 1);
            }
            var cat = await _context.GanjoorCategories.Where(c => c.FullUrl == url).AsNoTracking().SingleOrDefaultAsync();
            if (cat == null)
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null);
            return await GetCatById(cat.Id, poems, mainSections);
        }

        
        
        /// <summary>
        /// get list of books
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCatViewModel[]>> GetBooksAsync()
        {
            try
            {
                return new RServiceResult<GanjoorCatViewModel[]>
                    (
                    await _context.GanjoorCategories.AsNoTracking().Where(c => !string.IsNullOrEmpty(c.BookName)).OrderBy(c => c.BookName)
                    .Select(c => new GanjoorCatViewModel()
                    {
                        BookName = c.BookName,
                        FullUrl = c.FullUrl,
                        RImageId = c.RImageId
                    }
                    )
                    .ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorCatViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// generate missing book covers
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> GenerateMissingBookCoversAsync()
        {
            try
            {
                var books = await _context.GanjoorCategories.Where(c => !string.IsNullOrEmpty(c.BookName) && c.RImageId == null).ToListAsync();
                foreach (var book in books)
                {
                    using (System.Drawing.Image coverImg = System.Drawing.Image.FromFile(Configuration.GetSection("Ganjoor")["BooksCoverTemplate"]))
                    {
                        using (Graphics g = Graphics.FromImage(coverImg))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                            using(Font font = new Font(Configuration.GetSection("Ganjoor")["BooksCoverTitleFontName"], float.Parse(Configuration.GetSection("Ganjoor")["BooksCoverTitleFontSize"])))
                            {
                                SizeF sz = g.MeasureString(book.BookName, font);
                                using (SolidBrush brsh = new SolidBrush(Color.FromArgb(51, 0, 0)))
                                    g.DrawString(book.BookName, font, brsh, new PointF(200.0f - sz.Width / 2, 230.0f - sz.Height / 2));
                            }
                            
                        }
                        MemoryStream coverData = new MemoryStream();
                        coverImg.Save(coverData, System.Drawing.Imaging.ImageFormat.Jpeg);
                        coverData.Seek(0, SeekOrigin.Begin);
                        RServiceResult<RImage> imageRes = await _imageFileService.Add(null, coverData, $"Book-{book.Id}", "CategoryImages");
                        if(!string.IsNullOrEmpty(imageRes.ExceptionString))
                        {
                            return new RServiceResult<bool>(false, imageRes.ExceptionString);
                        }
                        imageRes = await _imageFileService.Store(imageRes.Result);
                        if (!string.IsNullOrEmpty(imageRes.ExceptionString))
                        {
                            return new RServiceResult<bool>(false, imageRes.ExceptionString);
                        }
                        var finalRes = await SetCategoryExtraInfo(book.Id, book.BookName, imageRes.Result.Id, book.SumUpSubsGeoLocations, book.MapName);
                        if(!string.IsNullOrEmpty(finalRes.ExceptionString))
                        {
                            return new RServiceResult<bool>(false, finalRes.ExceptionString);
                        }    
                    }
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get cat by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="poems"></param>
        /// <param name="mainSections"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> GetCatById(int id, bool poems = false, bool mainSections = false)
        {
            var cat = await _context.GanjoorCategories.Include(c => c.Poet).Include(c => c.Parent).Where(c => c.Id == id).AsNoTracking().FirstOrDefaultAsync();
            if (cat == null)
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null);

            List<GanjoorCatViewModel> ancetors = new List<GanjoorCatViewModel>();

            var parent = cat.Parent;
            while (parent != null)
            {
                ancetors.Insert(0, new GanjoorCatViewModel()
                {
                    Id = parent.Id,
                    Title = parent.Title,
                    UrlSlug = parent.UrlSlug,
                    FullUrl = parent.FullUrl,
                    TableOfContentsStyle = parent.TableOfContentsStyle,
                    CatType = parent.CatType,
                    Description = parent.Description,
                    DescriptionHtml = parent.DescriptionHtml,
                    MixedModeOrder = parent.MixedModeOrder,
                    Published = parent.Published,
                    BookName = parent.BookName,
                    RImageId = parent.RImageId,
                    SumUpSubsGeoLocations = parent.SumUpSubsGeoLocations,
                    MapName = parent.MapName,
                });

                parent = await _context.GanjoorCategories.Where(c => c.Id == parent.ParentId).AsNoTracking().FirstOrDefaultAsync();
            }


            int nextCatId =
                await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id > id).AnyAsync() ?
                await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id > id).MinAsync(c => c.Id)
                :
                0;
            var nextCat = nextCatId == 0 ? null : await _context
                                        .GanjoorCategories
                                        .Where(c => c.Id == nextCatId)
                                        .Select
                                        (
                                            c =>
                                                new GanjoorCatViewModel()
                                                {
                                                    Id = c.Id,
                                                    Title = c.Title,
                                                    UrlSlug = c.UrlSlug,
                                                    FullUrl = c.FullUrl,
                                                    TableOfContentsStyle = c.TableOfContentsStyle,
                                                    CatType = c.CatType,
                                                    Description = c.Description,
                                                    DescriptionHtml = c.DescriptionHtml,
                                                    MixedModeOrder = c.MixedModeOrder,
                                                    Published = c.Published,
                                                    BookName = c.BookName,
                                                    RImageId = c.RImageId,
                                                    SumUpSubsGeoLocations = c.SumUpSubsGeoLocations,
                                                    MapName = c.MapName,
                                                    //other fields null
                                                }
                                        ).AsNoTracking().SingleOrDefaultAsync();

            int preCatId =
                 await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id < id).AnyAsync() ?
                await _context.GanjoorCategories.Where(c => c.PoetId == cat.PoetId && c.ParentId == cat.ParentId && c.Id < id).MaxAsync(c => c.Id)
                :
                0;
            var preCat = preCatId == 0 ? null : await _context
                                        .GanjoorCategories
                                        .Where(c => c.Id == preCatId)
                                        .Select
                                        (
                                            c =>
                                                new GanjoorCatViewModel()
                                                {
                                                    Id = c.Id,
                                                    Title = c.Title,
                                                    UrlSlug = c.UrlSlug,
                                                    FullUrl = c.FullUrl,
                                                    TableOfContentsStyle = c.TableOfContentsStyle,
                                                    CatType = c.CatType,
                                                    Description = c.Description,
                                                    DescriptionHtml = c.DescriptionHtml,
                                                    MixedModeOrder = c.MixedModeOrder,
                                                    Published = c.Published,
                                                    BookName = c.BookName,
                                                    RImageId = c.RImageId,
                                                    SumUpSubsGeoLocations = c.SumUpSubsGeoLocations,
                                                    MapName = c.MapName,
                                                    //other fields null
                                                }
                                        ).AsNoTracking().SingleOrDefaultAsync();

            GanjoorCatViewModel catViewModel = new GanjoorCatViewModel()
            {
                Id = cat.Id,
                Title = cat.Title,
                UrlSlug = cat.UrlSlug,
                FullUrl = cat.FullUrl,
                TableOfContentsStyle = cat.TableOfContentsStyle,
                CatType = cat.CatType,
                Description = cat.Description,
                DescriptionHtml = cat.DescriptionHtml,
                MixedModeOrder = cat.MixedModeOrder,
                Published = cat.Published,
                BookName = cat.BookName,
                RImageId = cat.RImageId,
                SumUpSubsGeoLocations = cat.SumUpSubsGeoLocations,
                MapName = cat.MapName,
                Next = nextCat,
                Previous = preCat,
                Ancestors = ancetors,
                Children = await _context.GanjoorCategories.Where(c => c.ParentId == cat.Id).OrderBy(cat => cat.Id).Select
                 (
                 c => new GanjoorCatViewModel()
                 {
                     Id = c.Id,
                     Title = c.Title,
                     UrlSlug = c.UrlSlug,
                     FullUrl = c.FullUrl,
                     MixedModeOrder = c.MixedModeOrder,
                     Published = c.Published,
                 }
                 ).AsNoTracking().ToListAsync(),
                Poems = poems ? await _context.GanjoorPoems
                .Where(p => p.CatId == cat.Id).OrderBy(p => p.Id).Select
                 (
                     p => new GanjoorPoemSummaryViewModel()
                     {
                         Id = p.Id,
                         Title = p.Title,
                         UrlSlug = p.UrlSlug,
                         Excerpt = _context.GanjoorVerses.Where(v => v.PoemId == p.Id && v.VOrder == 1).FirstOrDefault().Text,
                     }
                 ).AsNoTracking().ToListAsync()
                 :
                 null
            };

            if (poems && mainSections)
            {
                foreach (var poem in catViewModel.Poems)
                {
                    poem.MainSections = await _context.GanjoorPoemSections.AsNoTracking().Include(s => s.GanjoorMetre).Where(s => s.PoemId == poem.Id && s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).OrderBy(s => s.Index).ToArrayAsync();
                    foreach (var section in poem.MainSections)
                    {
                        var firstVerse = await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == section.PoemId &&
                            (
                            (section.VerseType == VersePoemSectionType.First && v.SectionIndex1 == section.Index)
                            ||
                            (section.VerseType == VersePoemSectionType.Second && v.SectionIndex2 == section.Index)
                            ||
                            (section.VerseType == VersePoemSectionType.Third && v.SectionIndex3 == section.Index)
                            ||
                            (section.VerseType == VersePoemSectionType.Forth && v.SectionIndex4 == section.Index)
                            )
                        ).OrderBy(v => v.VOrder).FirstOrDefaultAsync();
                        section.Excerpt = firstVerse == null ? "" : firstVerse.Text;
                    }
                }
            }

            return new RServiceResult<GanjoorPoetCompleteViewModel>
               (
               new GanjoorPoetCompleteViewModel()
               {
                   Poet = await _context.GanjoorPoets.Include(p => p.BirthLocation).Include(p => p.DeathLocation).Where(p => p.Id == cat.PoetId)
                                        .Select(poet => new GanjoorPoetViewModel()
                                        {
                                            Id = poet.Id,
                                            Name = poet.Name,
                                            Description = poet.Description,
                                            FullUrl = _context.GanjoorCategories.Where(c => c.PoetId == poet.Id && c.ParentId == null).Single().FullUrl,
                                            RootCatId = _context.GanjoorCategories.Where(c => c.PoetId == poet.Id && c.ParentId == null).Single().Id,
                                            Nickname = poet.Nickname,
                                            Published = poet.Published,
                                            ImageUrl = poet.RImageId == null ? "" : $"/api/ganjoor/poet/image{_context.GanjoorCategories.Where(c => c.PoetId == poet.Id && c.ParentId == null).Single().FullUrl}.gif",
                                            BirthYearInLHijri = poet.BirthYearInLHijri,
                                            DeathYearInLHijri = poet.DeathYearInLHijri,
                                            ValidBirthDate = poet.ValidBirthDate,
                                            ValidDeathDate = poet.ValidDeathDate,
                                            PinOrder = poet.PinOrder,
                                            BirthPlace = poet.BirthLocation == null ? "" : poet.BirthLocation.Name,
                                            BirthPlaceLatitude = poet.BirthLocation == null ? 0 : poet.BirthLocation.Latitude,
                                            BirthPlaceLongitude = poet.BirthLocation == null ? 0 : poet.BirthLocation.Longitude,
                                            DeathPlace = poet.DeathLocation == null ? "" : poet.DeathLocation.Name,
                                            DeathPlaceLatitude = poet.DeathLocation == null ? 0 : poet.DeathLocation.Latitude,
                                            DeathPlaceLongitude = poet.DeathLocation == null ? 0 : poet.DeathLocation.Longitude,
                                        }).AsNoTracking().FirstOrDefaultAsync(),
                   Cat = catViewModel
               }
               );
        }

        /// <summary>
        /// get page url by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> GetPageUrlById(int id)
        {
            var dbPage = await _context.GanjoorPages.Where(p => p.Id == id).AsNoTracking().SingleOrDefaultAsync();
            if (dbPage == null)
                return new RServiceResult<string>(null); //not found
            return new RServiceResult<string>(dbPage.FullUrl);
        }

        /// <summary>
        /// clean cache for paeg by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task CacheCleanForPageById(int id)
        {
            var dbPage = await _context.GanjoorPages.Where(p => p.Id == id).AsNoTracking().SingleOrDefaultAsync();
            if (dbPage != null)
            {
                CacheCleanForPageByUrl(dbPage.FullUrl);
            }
        }

        /// <summary>
        /// clean cache for page by url
        /// </summary>
        /// <param name="url"></param>
        public void CacheCleanForPageByUrl(string url)
        {
            var cachKey = $"GanjoorService::GetPageByUrl::{url}";
            if (_memoryCache.TryGetValue(cachKey, out GanjoorPageCompleteViewModel page))
            {
                _memoryCache.Remove(cachKey);

                var poemCachKey = $"GetPoemById({page.Id}, {true}, {false}, {true}, {true}, {true}, {true}, {true}, {true}, {true})";
                if (_memoryCache.TryGetValue(poemCachKey, out GanjoorPoemCompleteViewModel p))
                {
                    _memoryCache.Remove(poemCachKey);
                }
            }
        }

        /// <summary>
        /// clean cache for page by comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        public async Task CacheCleanForComment(int commentId)
        {
            var comment = await _context.GanjoorComments.Where(c => c.Id == commentId).SingleOrDefaultAsync();
            if (comment != null)
            {
                await CacheCleanForPageById(comment.PoemId);
            }
        }

        /// <summary>
        /// get redirect url for a url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> GetRedirectAddressForPageUrl(string url)
        {
            if (url.IndexOf('?') != -1)
            {
                url = url.Substring(0, url.IndexOf('?'));
            }

            // /hafez/ => /hafez :
            if (url.LastIndexOf('/') == url.Length - 1)
            {
                url = url.Substring(0, url.Length - 1);
            }

            url = url.Replace("//", "/"); //duplicated slashes would be merged

            var pages = await _context.GanjoorPages.Where(p => url.StartsWith(p.RedirectFromFullUrl)).AsNoTracking().ToListAsync();
            if (pages.Count == 0)
                return new RServiceResult<string>(null); //not found

            var dbPage = pages[0];
            for (int i = 1; i < pages.Count; i++)
            {
                if (pages[i].RedirectFromFullUrl.Length > dbPage.RedirectFromFullUrl.Length)
                {
                    dbPage = pages[i];
                }
            }

            var target = dbPage.FullUrl;
            if (url != dbPage.RedirectFromFullUrl)
            {
                target = dbPage.FullUrl + url.Substring(dbPage.RedirectFromFullUrl.Length);
            }

            var dbPageRedirected = await _context.GanjoorPages.Where(p => p.FullUrl == target).AsNoTracking().SingleOrDefaultAsync();
            if (dbPageRedirected == null)
                return new RServiceResult<string>(null); //not found

            return new RServiceResult<string>(target);
        }

        /// <summary>
        /// get page by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPageCompleteViewModel>> GetPageByUrl(string url, bool catPoems = false)
        {
            if (url.IndexOf('?') != -1)
            {
                url = url.Substring(0, url.IndexOf('?'));
            }

            // /hafez/ => /hafez :
            if (url.LastIndexOf('/') == url.Length - 1)
            {
                url = url.Substring(0, url.Length - 1);
            }

            url = url.Replace("//", "/"); //duplicated slashes would be merged

            var cachKey = $"GanjoorService::GetPageByUrl::{url}";
            if (!_memoryCache.TryGetValue(cachKey, out GanjoorPageCompleteViewModel page))
            {
                var dbPage = await _context.GanjoorPages.Where(p => p.FullUrl == url).AsNoTracking().SingleOrDefaultAsync();
                if (dbPage == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null); //not found
                var secondPoet = dbPage.SecondPoetId == null ? null :
                     await
                     (from poet in _context.GanjoorPoets
                      join cat in _context.GanjoorCategories.Where(c => c.ParentId == null)
                      on poet.Id equals cat.PoetId
                      where poet.Id == (int)dbPage.SecondPoetId
                      orderby poet.Nickname descending
                      select new GanjoorPoetViewModel()
                      {
                          Id = poet.Id,
                          Name = poet.Name,
                          FullUrl = cat.FullUrl,
                          RootCatId = cat.Id,
                          Nickname = poet.Nickname,
                          Published = poet.Published,
                          ImageUrl = poet.RImageId == null ? "" : $"/api/ganjoor/poet/image{cat.FullUrl}.gif",
                          BirthYearInLHijri = poet.BirthYearInLHijri,
                          ValidBirthDate = poet.ValidBirthDate,
                          ValidDeathDate = poet.ValidDeathDate,
                          DeathYearInLHijri = poet.DeathYearInLHijri,
                          PinOrder = poet.PinOrder,
                      }
                      )
                     .AsNoTracking().SingleAsync();
                page = new GanjoorPageCompleteViewModel()
                {
                    Id = dbPage.Id,
                    GanjoorPageType = dbPage.GanjoorPageType,
                    Title = dbPage.Title,
                    FullTitle = dbPage.FullTitle,
                    UrlSlug = dbPage.UrlSlug,
                    FullUrl = dbPage.FullUrl,
                    HtmlText = dbPage.HtmlText,
                    SecondPoet = secondPoet,
                    NoIndex = dbPage.NoIndex,
                    RedirectFromFullUrl = dbPage.RedirectFromFullUrl,

                };
                switch (page.GanjoorPageType)
                {
                    case GanjoorPageType.PoemPage:
                        {
                            var poemRes = await GetPoemById((int)dbPage.PoemId);
                            if (!string.IsNullOrEmpty(poemRes.ExceptionString))
                            {
                                return new RServiceResult<GanjoorPageCompleteViewModel>(null, poemRes.ExceptionString);
                            }
                            page.Poem = poemRes.Result;
                        }
                        break;

                    case GanjoorPageType.CatPage:
                        {
                            var catRes = await GetCatById((int)dbPage.CatId, catPoems);
                            if (!string.IsNullOrEmpty(catRes.ExceptionString))
                            {
                                return new RServiceResult<GanjoorPageCompleteViewModel>(null, catRes.ExceptionString);
                            }
                            page.PoetOrCat = catRes.Result;
                        }
                        break;
                    default:
                        {
                            if (dbPage.PoetId != null)
                            {
                                var poetRes = await GetPoetById((int)dbPage.PoetId, catPoems);
                                if (!string.IsNullOrEmpty(poetRes.ExceptionString))
                                {
                                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, poetRes.ExceptionString);
                                }
                                page.PoetOrCat = poetRes.Result;

                                var pre = await _context.GanjoorPages.Where(p => p.GanjoorPageType == page.GanjoorPageType && p.ParentId == dbPage.ParentId && p.PoetId == dbPage.PoetId &&
                                    ((p.PageOrder < dbPage.PageOrder) || (p.PageOrder == dbPage.PageOrder && p.Id < dbPage.Id)))
                                    .OrderByDescending(p => p.PageOrder)
                                    .ThenByDescending(p => p.Id)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();
                                if (pre != null)
                                {
                                    page.Previous = new GanjoorPageSummaryViewModel()
                                    {
                                        Id = pre.Id,
                                        Title = pre.Title,
                                        FullUrl = pre.FullUrl
                                    };
                                }

                                var next = await _context.GanjoorPages.Where(p => p.GanjoorPageType == page.GanjoorPageType && p.ParentId == dbPage.ParentId && p.PoetId == dbPage.PoetId &&
                                    ((p.PageOrder > dbPage.PageOrder) || (p.PageOrder == dbPage.PageOrder && p.Id > dbPage.Id)))
                                    .OrderBy(p => p.PageOrder)
                                    .ThenBy(p => p.Id)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync();
                                if (next != null)
                                {
                                    page.Next = new GanjoorPageSummaryViewModel()
                                    {
                                        Id = next.Id,
                                        Title = next.Title,
                                        FullUrl = next.FullUrl
                                    };
                                }
                            }
                        }
                        break;
                }
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cachKey, page);
                }
            }

            return new RServiceResult<GanjoorPageCompleteViewModel>(page);
        }



        /// <summary>
        /// get poem recitations  (PlainText/HtmlText are intentionally empty)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PublicRecitationViewModel[]>> GetPoemRecitations(int id)
        {
            var source =
                 from audio in _context.Recitations
                 join poem in _context.GanjoorPoems
                 on audio.GanjoorPostId equals poem.Id
                 where
                 audio.ReviewStatus == AudioReviewStatus.Approved
                 &&
                 poem.Id == id
                 orderby audio.AudioOrder
                 select new PublicRecitationViewModel()
                 {
                     Id = audio.Id,
                     PoemId = audio.GanjoorPostId,
                     PoemFullTitle = poem.FullTitle,
                     PoemFullUrl = poem.FullUrl,
                     AudioTitle = audio.AudioTitle,
                     AudioArtist = audio.AudioArtist,
                     AudioArtistUrl = audio.AudioArtistUrl,
                     AudioSrc = audio.AudioSrc,
                     AudioSrcUrl = audio.AudioSrcUrl,
                     LegacyAudioGuid = audio.LegacyAudioGuid,
                     Mp3FileCheckSum = audio.Mp3FileCheckSum,
                     Mp3SizeInBytes = audio.Mp3SizeInBytes,
                     PublishDate = audio.ReviewDate,
                     FileLastUpdated = audio.FileLastUpdated,
                     Mp3Url = $"{WebServiceUrl.Url}/api/audio/file/{audio.Id}.mp3",
                     XmlText = $"{WebServiceUrl.Url}/api/audio/xml/{audio.Id}",
                     PlainText = "", //poem.PlainText 
                     HtmlText = "",//poem.HtmlText
                     AudioOrder = audio.AudioOrder,
                     UpVotedByUser = false,
                 };
            var recitations = await source.AsNoTracking().ToArrayAsync();
            foreach (var recitation in recitations)
            {
                recitation.Mistakes =
                    await _context.RecitationApprovedMistakes.AsNoTracking()
                          .Where(m => m.RecitationId == recitation.Id)
                          .Select(m => new RecitationMistakeViewModel()
                          {
                              Mistake = m.Mistake,
                              NumberOfLinesAffected = m.NumberOfLinesAffected,
                              CoupletIndex = m.CoupletIndex
                          }).ToArrayAsync();
            }
            return new RServiceResult<PublicRecitationViewModel[]>(recitations);
        }


        /// <summary>
        /// get poem whole sections
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemSection[]>> GetPoemWholeSections(int id)
        {
            var sections =
                await _context.GanjoorPoemSections.AsNoTracking().Include(s => s.GanjoorMetre).Where(s => s.PoemId == id && s.SectionType == PoemSectionType.WholePoem).OrderBy(s => s.Index).ToArrayAsync();

            foreach (var section in sections)
            {
                section.Top6RelatedSections = (await GetRelatedSections(section.PoemId, section.Index, 0, 6)).Result;
            }

            return new RServiceResult<GanjoorPoemSection[]>(sections);
        }


        /// <summary>
        /// get user up votes for the recitations of a poem
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int[]>> GetUserPoemRecitationsUpVotes(int id, Guid userId)
        {
            var source =
                 from audio in _context.Recitations
                 join poem in _context.GanjoorPoems
                 on audio.GanjoorPostId equals poem.Id
                 where
                 audio.ReviewStatus == AudioReviewStatus.Approved
                 &&
                 poem.Id == id
                 orderby audio.AudioOrder
                 select audio.Id;

            List<int> upVotedRecitations = new List<int>();
            var recitationIds = await source.ToArrayAsync();
            foreach (var recitationId in recitationIds)
            {
                if (await _context.RecitationUserUpVotes.Where(v => v.RecitationId == recitationId && v.UserId == userId).AnyAsync())
                {
                    upVotedRecitations.Add(recitationId);
                }
            }

            return new RServiceResult<int[]>(upVotedRecitations.ToArray());
        }

        private async Task _FillPoemCoupletIndices(RMuseumDbContext context, int poemId)
        {
            var verses = await context.GanjoorVerses.Where(v => v.PoemId == poemId).OrderBy(v => v.VOrder).ToListAsync();
            int cIndex = -1;
            foreach (var verse in verses)
            {
                if (verse.VersePosition != VersePosition.Left && verse.VersePosition != VersePosition.CenteredVerse2 && verse.VersePosition != VersePosition.Comment)
                    cIndex++;
                if (verse.VersePosition != VersePosition.Comment)
                {
                    verse.CoupletIndex = cIndex;
                }
                else
                {
                    verse.CoupletIndex = null;
                }
            }
            context.GanjoorVerses.UpdateRange(verses);
        }




        /// <summary>
        /// get poem comments
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="userId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCommentSummaryViewModel[]>> GetPoemComments(int poemId, Guid userId, int? coupletIndex)
        {
            var source =
                  from comment in _context.GanjoorComments.Include(c => c.User)
                  where
                  (comment.Status == PublishStatus.Published || (userId != Guid.Empty && comment.Status == PublishStatus.Awaiting && comment.UserId == userId))
                  &&
                  comment.PoemId == poemId
                  &&
                  ((coupletIndex == null) || (coupletIndex != null && comment.CoupletIndex == coupletIndex))
                  orderby comment.CommentDate
                  select new GanjoorCommentSummaryViewModel()
                  {
                      Id = comment.Id,
                      AuthorName = comment.User == null ? comment.AuthorName : $"{comment.User.NickName}",
                      AuthorUrl = comment.AuthorUrl,
                      CommentDate = comment.CommentDate,
                      HtmlComment = comment.HtmlComment,
                      PublishStatus = comment.Status == PublishStatus.Awaiting ? "در انتظار تأیید" : "",
                      InReplyToId = comment.InReplyToId,
                      UserId = comment.UserId,
                      CoupletIndex = comment.CoupletIndex == null ? -1 : (int)comment.CoupletIndex,
                  };

            GanjoorCommentSummaryViewModel[] allComments = await source.AsNoTracking().ToArrayAsync();

            foreach (GanjoorCommentSummaryViewModel comment in allComments)
            {
                comment.AuthorName = comment.AuthorName.ToPersianNumbers().ApplyCorrectYeKe();
                var relatedVerses = comment.CoupletIndex == -1 ? new List<GanjoorVerse>() : await _context.GanjoorVerses.Where(v => v.PoemId == poemId && v.CoupletIndex == comment.CoupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                string coupleText = relatedVerses.Count == 0 ? "" : relatedVerses[0].Text;
                for (int nVerseIndex = 1; nVerseIndex < relatedVerses.Count; nVerseIndex++)
                {
                    coupleText += $" {relatedVerses[nVerseIndex].Text}";
                }
                comment.CoupletSummary = _CutSummary(coupleText);
            }

            GanjoorCommentSummaryViewModel[] rootComments = allComments.Where(c => c.InReplyToId == null).ToArray();

            foreach (GanjoorCommentSummaryViewModel comment in rootComments)
            {
                _FindReplies(comment, allComments);
            }
            return new RServiceResult<GanjoorCommentSummaryViewModel[]>(rootComments);
        }

        private void _FindReplies(GanjoorCommentSummaryViewModel comment, GanjoorCommentSummaryViewModel[] allComments)
        {
            comment.Replies = allComments.Where(c => c.InReplyToId == comment.Id).ToArray();
            foreach (GanjoorCommentSummaryViewModel reply in comment.Replies)
            {
                _FindReplies(reply, allComments);
            }
        }

        /// <summary>
        /// new comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ip"></param>
        /// <param name="poemId"></param>
        /// <param name="content"></param>
        /// <param name="inReplyTo"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCommentSummaryViewModel>> NewComment(Guid userId, string ip, int poemId, string content, int? inReplyTo, int? coupletIndex)
        {
            if (string.IsNullOrEmpty(content))
            {
                return new RServiceResult<GanjoorCommentSummaryViewModel>(null, "متن حاشیه خالی است.");
            }

            var userRes = await _appUserService.GetUserInformation(userId);

            if (string.IsNullOrEmpty(userRes.Result.NickName))
            {
                return new RServiceResult<GanjoorCommentSummaryViewModel>(null, "لطفاً با مراجعه به پیشخان کاربری (دکمهٔ گوشهٔ پایین سمت چپ) «نام مستعار» خود را مشخص کنید و سپس اقدام به ارسال حاشیه بفرمایید.");
            }

            string coupletSummary = "";
            if (inReplyTo != null)
            {
                GanjoorComment refComment = await _context.GanjoorComments.Where(c => c.Id == (int)inReplyTo).SingleAsync();
                coupletIndex = refComment.CoupletIndex;
                if (refComment.CoupletIndex != null)
                {
                    var relatedVerses = refComment.CoupletIndex == -1 ? new List<GanjoorVerse>() : await _context.GanjoorVerses.Where(v => v.PoemId == poemId && v.CoupletIndex == refComment.CoupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                    coupletSummary = relatedVerses.Count > 0 ? relatedVerses[0].Text : "";
                    if (relatedVerses.Count > 1)
                    {
                        coupletSummary += $" {relatedVerses[1].Text}";
                    }
                }
            }
            else
            if (coupletIndex != null)
            {
                var relatedVerses = await _context.GanjoorVerses.Where(v => v.PoemId == poemId && v.CoupletIndex == coupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                coupletSummary = relatedVerses.Count > 0 ? relatedVerses[0].Text : "";
                if (relatedVerses.Count > 1)
                {
                    coupletSummary += $" {relatedVerses[1].Text}";
                }
            }

            content = content.ApplyCorrectYeKe();

            content = await _ProcessCommentHtml(content, _context);

            string commentText = Regex.Replace(content, "<.*?>", string.Empty);
            if (commentText.Split(" ", StringSplitOptions.RemoveEmptyEntries).Max(s => s.Length) > 50)
            {
                return new RServiceResult<GanjoorCommentSummaryViewModel>(null, "متن حاشیه شامل کلمات به هم پیوستهٔ طولانی است.");
            }

            if (string.IsNullOrEmpty(commentText))
            {
                return new RServiceResult<GanjoorCommentSummaryViewModel>(null, "متن حاشیه خالی است.");
            }

            PublishStatus status = PublishStatus.Published;
            var keepFirstTimeUsersComments = await _optionsService.GetValueAsync("KeepFirstTimeUsersComments", null);
            if (keepFirstTimeUsersComments.Result == true.ToString())
            {
                if ((await _context.GanjoorComments.AsNoTracking().Where(c => c.UserId == userId && c.Status == PublishStatus.Published).AnyAsync()) == false)//First time commenter
                {
                    status = PublishStatus.Awaiting;
                }
            }

            GanjoorComment comment = new GanjoorComment()
            {
                UserId = userId,
                AuthorIpAddress = ip,
                CommentDate = DateTime.Now,
                HtmlComment = content,
                InReplyToId = inReplyTo,
                PoemId = poemId,
                Status = status,
                CoupletIndex = coupletIndex,
            };

            _context.GanjoorComments.Add(comment);
            await _context.SaveChangesAsync();

            if (inReplyTo != null)
            {
                GanjoorComment refComment = await _context.GanjoorComments.Where(c => c.Id == (int)inReplyTo).SingleAsync();
                if (refComment.UserId != null)
                {

                    var poem = await _context.GanjoorPoems.Where(p => p.Id == comment.PoemId).SingleAsync();

                    await _notificationService.PushNotification((Guid)refComment.UserId,
                                       "پاسخ به حاشیهٔ شما",
                                       $"{userRes.Result.NickName} برای حاشیهٔ شما روی <a href=\"{poem.FullUrl}\">{poem.FullTitle}</a> این پاسخ را نوشته است: {Environment.NewLine}" +
                                       $"{content}" +
                                       $"این متن حاشیهٔ خود شماست: {Environment.NewLine}" +
                                       $"{refComment.HtmlComment}"
                                       );
                }
            }

            await CacheCleanForPageById(poemId);


            return new RServiceResult<GanjoorCommentSummaryViewModel>
                (
                new GanjoorCommentSummaryViewModel()
                {
                    Id = comment.Id,
                    AuthorName = $"{userRes.Result.NickName}",
                    AuthorUrl = comment.AuthorUrl,
                    CommentDate = comment.CommentDate,
                    HtmlComment = comment.HtmlComment,
                    PublishStatus = comment.Status == PublishStatus.Awaiting ? "در انتظار تأیید" : "",
                    InReplyToId = comment.InReplyToId,
                    UserId = comment.UserId,
                    Replies = new GanjoorCommentSummaryViewModel[] { },
                    CoupletIndex = coupletIndex == null ? -1 : (int)coupletIndex,
                    MyComment = true,
                    CoupletSummary = _CutSummary(coupletSummary)
                }
                );
        }

        private string _CutSummary(string summary)
        {
            if (summary.Length > 50)
            {
                summary = summary.Substring(0, 30);
                int n = summary.LastIndexOf(' ');
                if (n >= 0)
                {
                    summary = summary.Substring(0, n) + " ...";
                }
                else
                {
                    summary += "...";
                }
            }
            return summary;
        }

        /// <summary>
        /// update user's own comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <param name="htmlComment"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> EditMyComment(Guid userId, int commentId, string htmlComment)
        {
            GanjoorComment comment = await _context.GanjoorComments.Where(c => c.Id == commentId && c.UserId == userId).SingleOrDefaultAsync();//userId is not part of key but it helps making call secure
            if (comment == null)
            {
                return new RServiceResult<bool>(false); //not found
            }

            await CacheCleanForComment(commentId);

            htmlComment = htmlComment.ApplyCorrectYeKe();

            htmlComment = await _ProcessCommentHtml(htmlComment, _context);

            comment.HtmlComment = htmlComment;

            _context.GanjoorComments.Update(comment);
            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// link or unlink user's own comment to a coupletIndex
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <param name="coupletIndex">if null then unlinks</param>
        /// <returns>couplet summary</returns>
        public async Task<RServiceResult<string>> LinkUnLinkMyComment(Guid userId, int commentId, int? coupletIndex)
        {
            var userRes = await _appUserService.GetUserInformation(userId);

            if (string.IsNullOrEmpty(userRes.Result.NickName))
            {
                return new RServiceResult<string>(null, "لطفاً با مراجعه به پیشخان کاربری (دکمهٔ گوشهٔ پایین سمت چپ) «نام مستعار» خود را مشخص کنید و سپس اقدام به ارسال حاشیه بفرمایید.");
            }

            GanjoorComment comment = await _context.GanjoorComments.Where(c => c.Id == commentId && c.UserId == userId).SingleOrDefaultAsync();//userId is not part of key but it helps making call secure
            if (comment == null)
            {
                return new RServiceResult<string>(null); //not found
            }

            await CacheCleanForComment(commentId);



            comment.CoupletIndex = coupletIndex;

            string coupletSummary = "";
            if (coupletIndex != null)
            {
                var relatedVerses = await _context.GanjoorVerses.Where(v => v.PoemId == comment.PoemId && v.CoupletIndex == coupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                coupletSummary = relatedVerses.Count > 0 ? relatedVerses[0].Text : "";
                if (relatedVerses.Count > 1)
                {
                    coupletSummary += $" {relatedVerses[1].Text}";
                }
            }

            _context.GanjoorComments.Update(comment);
            await _context.SaveChangesAsync();

            return new RServiceResult<string>
                (
               coupletSummary
                );
        }

        /// <summary>
        /// delete a reported  comment
        /// </summary>
        /// <param name="repordId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteModerateComment(int repordId)
        {
            GanjoorCommentAbuseReport report = await _context.GanjoorReportedComments.Where(r => r.Id == repordId).SingleOrDefaultAsync();
            if (report == null)
            {
                return new RServiceResult<bool>(false);
            }

            var reportUserId = report.ReportedById;


            GanjoorComment comment = await _context.GanjoorComments.Where(c => c.Id == report.GanjoorCommentId).SingleOrDefaultAsync();
            if (comment == null)
            {
                return new RServiceResult<bool>(false); //not found
            }

            var commentId = report.GanjoorCommentId;
            string commentHtmltext = comment.HtmlComment;


            if (comment.UserId != null)
            {
                string reason = "";
                switch (report.ReasonCode)
                {
                    case "offensive":
                        reason = "توهین‌آمیز است.";
                        break;
                    case "religious":
                        reason = "بحث مذهبی کرده.";
                        break;
                    case "repeated":
                        reason = "تکراری است.";
                        break;
                    case "unrelated":
                        reason = "به این شعر ربطی ندارد.";
                        break;
                    case "brokenlink":
                        reason = "لینک شکسته است.";
                        break;
                    case "ad":
                        reason = "تبلیغاتی است.";
                        break;
                    case "bogus":
                        reason = "نامفهوم است.";
                        break;
                    case "latin":
                        reason = "فارسی ننوشته.";
                        break;
                    case "other":
                        reason = "دلیل دیگر";
                        break;
                }
                if (!string.IsNullOrEmpty(report.ReasonText))
                    reason += $" {report.ReasonText}";
                reason = reason.Trim();
                reason = string.IsNullOrEmpty(reason) ? "" : $"علت ارائه شده برای حذف یا متن گزارش کاربر شاکی: {Environment.NewLine}" +
                                       $"{reason} {Environment.NewLine}";
                await _notificationService.PushNotification((Guid)comment.UserId,
                                       "حذف حاشیهٔ شما",
                                       $"حاشیهٔ شما به دلیل ناسازگاری با قوانین حاشیه‌گذاری گنجور و طبق گزارشات دیگر کاربران حذف شده است.{Environment.NewLine}" +
                                       $"{reason}{Environment.NewLine}" +
                                       $"<a href=\"https://ganjoor.net?p={comment.PoemId}\">نشانی صفحهٔ متناظر در گنجور</a>{Environment.NewLine}" +
                                       $"این متن حاشیهٔ حذف شدهٔ شماست: {Environment.NewLine}" +
                                       $"{comment.HtmlComment}"
                                       );
            }

            //if user has got replies, delete them and notify their owners of what happened
            var replies = await _FindReplies(comment);
            for (int i = replies.Count - 1; i >= 0; i--)
            {
                if (replies[i].UserId != null)
                {
                    await _notificationService.PushNotification((Guid)replies[i].UserId,
                                           "حذف پاسخ شما به حاشیه",
                                           $"پاسخ شما به یکی از حاشیه‌های گنجور به دلیل حذف زنجیرهٔ حاشیه توسط یکی از حاشیه‌گذاران حذف شده است.{Environment.NewLine}" +
                                           $"این متن حاشیهٔ حذف شدهٔ شماست: {Environment.NewLine}" +
                                           $"{replies[i].HtmlComment}"
                                           );
                }
                _context.GanjoorComments.Remove(replies[i]);
            }

            _context.GanjoorComments.Remove(comment);
            await _context.SaveChangesAsync();

            await CacheCleanForComment(report.GanjoorCommentId);

            if (reportUserId != null)
            {
                await _notificationService.PushNotification((Guid)reportUserId, "حذف حاشیهٔ گزارش شده توسط شما",
                    $"گزارش شما برای حاشیه‌ای با متن ذیل پذیرفته و حاشیه حذف شد. متن حاشیهٔ گزارش شده توسط شما:{Environment.NewLine}" +
                    commentHtmltext
                    );
            }

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// publish awaiting comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> PublishAwaitingComment(int commentId)
        {
            GanjoorComment comment = await _context.GanjoorComments.Where(c => c.Id == commentId).SingleOrDefaultAsync();//userId is not part of key but it helps making call secure
            if (comment == null)
            {
                return new RServiceResult<bool>(false); //not found
            }
            comment.Status = PublishStatus.Published;
            _context.Update(comment);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// delete anybody's comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteAnybodyComment(int commentId)
        {
            GanjoorComment comment = await _context.GanjoorComments.AsNoTracking().Where(c => c.Id == commentId).SingleOrDefaultAsync();//userId is not part of key but it helps making call secure
            if (comment == null)
            {
                return new RServiceResult<bool>(false); //not found
            }

            return await DeleteMyComment((Guid)comment.UserId, commentId);
        }


        /// <summary>
        /// delete user own comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteMyComment(Guid userId, int commentId)
        {
            GanjoorComment comment = await _context.GanjoorComments.Where(c => c.Id == commentId && c.UserId == userId).SingleOrDefaultAsync();//userId is not part of key but it helps making call secure
            if (comment == null)
            {
                return new RServiceResult<bool>(false); //not found
            }

            await CacheCleanForComment(commentId);

            //if user has got replies, delete them and notify their owners of what happened
            var replies = await _FindReplies(comment);
            for (int i = replies.Count - 1; i >= 0; i--)
            {
                if (replies[i].UserId != null && replies[i].UserId != userId)
                {
                    await _notificationService.PushNotification((Guid)replies[i].UserId,
                                           "حذف پاسخ شما به حاشیه",
                                           $"پاسخ شما به یکی از حاشیه‌های گنجور به دلیل حذف زنجیرهٔ حاشیه توسط یکی از حاشیه‌گذاران حذف شده است.{Environment.NewLine}" +
                                           $"<a href=\"https://ganjoor.net?p={comment.PoemId}\">نشانی صفحهٔ متناظر در گنجور</a>{Environment.NewLine}" +
                                           $"این متن حاشیهٔ حذف شدهٔ شماست: {Environment.NewLine}" +
                                           $"{replies[i].HtmlComment}"
                                           );
                }
                _context.GanjoorComments.Remove(replies[i]);
            }

            _context.GanjoorComments.Remove(comment);
            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
        }

        private async Task<List<GanjoorComment>> _FindReplies(GanjoorComment comment)
        {
            List<GanjoorComment> replies = await _context.GanjoorComments.Where(c => c.InReplyToId == comment.Id).AsNoTracking().ToListAsync();
            List<GanjoorComment> replyToReplies = new List<GanjoorComment>();
            foreach (GanjoorComment reply in replies)
            {
                replyToReplies.AddRange(await _FindReplies(reply));
            }
            if (replyToReplies.Count > 0)
            {
                replies.AddRange(replyToReplies);
            }
            return replies;
        }


        /// <summary>
        /// get recent comments
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filterUserId"></param>
        /// <param name="onlyPublished"></param>
        /// <param name="onlyAwaiting"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items)>> GetRecentComments(PagingParameterModel paging, Guid filterUserId, bool onlyPublished, bool onlyAwaiting = false, string term = null)
        {
            string searchConditions = null;
            if (!string.IsNullOrEmpty(term))
            {
                /* You need to run this scripts manually on the database before using this method:
                 CREATE FULLTEXT CATALOG [GanjoorHtmlCommentTextCatalog] WITH ACCENT_SENSITIVITY = OFF AS DEFAULT
                 
                 CREATE FULLTEXT INDEX ON [dbo].[GanjoorComments](
                 [HtmlComment] LANGUAGE 'English')
                 KEY INDEX [PK_GanjoorComments]ON ([GanjoorHtmlCommentTextCatalog], FILEGROUP [PRIMARY])
                 WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)
                */
                term = term.Replace("‌", " ");//replace zwnj with space

                if (term.IndexOf('"') == 0 && term.LastIndexOf('"') == (term.Length - 1))
                {
                    searchConditions = term.Replace("\"", "").Replace("'", "");
                    searchConditions = $"\"{searchConditions}\"";
                }
                else
                {
                    string[] words = term.Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    searchConditions = "";
                    string emptyOrAnd = "";
                    foreach (string word in words)
                    {
                        searchConditions += $" {emptyOrAnd} \"*{word}*\" ";
                        emptyOrAnd = " AND ";
                    }
                }
            }

            var source =
                 from comment in _context.GanjoorComments.Include(c => c.Poem).Include(c => c.User).Include(c => c.InReplyTo).ThenInclude(r => r.User)
                 where
                  ((comment.Status == PublishStatus.Published) || !onlyPublished)
                  &&
                  ((comment.Status == PublishStatus.Awaiting) || !onlyAwaiting)
                 &&
                 ((filterUserId == Guid.Empty) || (filterUserId != Guid.Empty && comment.UserId == filterUserId))
                 &&
                 (string.IsNullOrEmpty(searchConditions) || (!string.IsNullOrEmpty(searchConditions) && EF.Functions.Contains(comment.HtmlComment, searchConditions)))
                 orderby comment.CommentDate descending
                 select new GanjoorCommentFullViewModel()
                 {
                     Id = comment.Id,
                     AuthorName = comment.User == null ? comment.AuthorName : $"{comment.User.NickName}",
                     AuthorUrl = comment.AuthorUrl,
                     CommentDate = comment.CommentDate,
                     HtmlComment = comment.HtmlComment,
                     PublishStatus = "",//invalid!
                     UserId = comment.UserId,
                     CoupletIndex = comment.CoupletIndex == null ? -1 : (int)comment.CoupletIndex,
                     InReplyTo = comment.InReplyTo == null ? null :
                        new GanjoorCommentSummaryViewModel()
                        {
                            Id = comment.InReplyTo.Id,
                            AuthorName = comment.InReplyTo.User == null ? comment.InReplyTo.AuthorName : $"{comment.InReplyTo.User.NickName}",
                            AuthorUrl = comment.InReplyTo.AuthorUrl,
                            CommentDate = comment.InReplyTo.CommentDate,
                            HtmlComment = comment.InReplyTo.HtmlComment,
                            PublishStatus = "",
                            UserId = comment.InReplyTo.UserId,
                            CoupletIndex = comment.InReplyTo.CoupletIndex == null ? -1 : (int)comment.InReplyTo.CoupletIndex,
                        },
                     Poem = new GanjoorPoemSummaryViewModel()
                     {
                         Id = comment.Poem.Id,
                         Title = comment.Poem.FullTitle,
                         UrlSlug = comment.Poem.FullUrl,
                         Excerpt = ""
                     }
                 };

            (PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items) paginatedResult =
                await QueryablePaginator<GanjoorCommentFullViewModel>.Paginate(source, paging);


            foreach (GanjoorCommentFullViewModel comment in paginatedResult.Items)
            {
                comment.AuthorName = comment.AuthorName.ToPersianNumbers().ApplyCorrectYeKe();
                var relatedVerses = comment.CoupletIndex == -1 ? new List<GanjoorVerse>() : await _context.GanjoorVerses.Where(v => v.PoemId == comment.Poem.Id && v.CoupletIndex == comment.CoupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                string coupleText = relatedVerses.Count == 0 ? "" : relatedVerses[0].Text;
                for (int nVerseIndex = 1; nVerseIndex < relatedVerses.Count; nVerseIndex++)
                {
                    coupleText += $" {relatedVerses[nVerseIndex].Text}";
                }


                comment.CoupletSummary = _CutSummary(coupleText);
                if (comment.InReplyTo != null)
                {

                    var replyRelatedVerses = comment.InReplyTo.CoupletIndex == -1 ? new List<GanjoorVerse>() : await _context.GanjoorVerses.Where(v => v.PoemId == comment.Poem.Id && v.CoupletIndex == comment.InReplyTo.CoupletIndex).OrderBy(v => v.VOrder).ToListAsync();
                    string replyCoupleText = relatedVerses.Count == 0 ? "" : relatedVerses[0].Text;
                    for (int nVerseIndex = 1; nVerseIndex < replyRelatedVerses.Count; nVerseIndex++)
                    {
                        replyCoupleText += $" {replyRelatedVerses[nVerseIndex].Text}";
                    }
                    comment.InReplyTo.CoupletSummary = _CutSummary(replyCoupleText);
                }
            }

            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentFullViewModel[] Items)>(paginatedResult);
        }

        /// <summary>
        /// report a comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="report"></param>
        /// <returns>id of report record</returns>
        public async Task<RServiceResult<int>> ReportComment(Guid userId, GanjoorPostReportCommentViewModel report)
        {
            GanjoorCommentAbuseReport r = new GanjoorCommentAbuseReport()
            {
                GanjoorCommentId = report.CommentId,
                ReportedById = userId,
                ReasonCode = report.ReasonCode,
                ReasonText = report.ReasonText,
            };
            _context.GanjoorReportedComments.Add(r);
            await _context.SaveChangesAsync();
            var moderators = await _appUserService.GetUsersHavingPermission(RMuseumSecurableItem.GanjoorEntityShortName, RMuseumSecurableItem.ModerateOperationShortName);
            if (string.IsNullOrEmpty(moderators.ExceptionString)) //if not, do nothing!
            {
                foreach (var moderator in moderators.Result)
                {
                    await _notificationService.PushNotification
                                    (
                                        (Guid)moderator.Id,
                                        "گزارش حاشیه",
                                        $"گزارشی برای یک حاشیه ثبت شده است. لطفاً بخش <a href=\"/User/ReportedComments\">حاشیه‌های گزارش شده</a> را بررسی فرمایید.{Environment.NewLine}" +
                                        $"توجه فرمایید که اگر کاربر دیگری که دارای مجوز بررسی حاشیه‌هاست پیش از شما به آن رسیدگی کرده باشد آن را در صف نخواهید دید."
                                    );
                }
            }
            return new RServiceResult<int>(r.GanjoorCommentId);
        }

        /// <summary>
        /// delete a report
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteReport(int id)
        {
            GanjoorCommentAbuseReport report = await _context.GanjoorReportedComments.Where(r => r.Id == id).SingleOrDefaultAsync();
            if (report == null)
            {
                return new RServiceResult<bool>(false);
            }
            _context.GanjoorReportedComments.Remove(report);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Get list of reported comments
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentAbuseReportViewModel[] Items)>> GetReportedComments(PagingParameterModel paging)
        {
            var source =
                 from report in _context.GanjoorReportedComments
                 join comment in _context.GanjoorComments.Include(c => c.Poem).Include(c => c.User).Include(c => c.InReplyTo).ThenInclude(r => r.User)
                 on report.GanjoorCommentId equals comment.Id
                 orderby report.Id descending
                 select
                 new GanjoorCommentAbuseReportViewModel()
                 {
                     Id = report.Id,
                     ReasonCode = report.ReasonCode,
                     ReasonText = report.ReasonText,
                     Comment = new GanjoorCommentFullViewModel()
                     {
                         Id = comment.Id,
                         AuthorName = comment.User == null ? comment.AuthorName : $"{comment.User.NickName}",
                         AuthorUrl = comment.AuthorUrl,
                         CommentDate = comment.CommentDate,
                         HtmlComment = comment.HtmlComment,
                         PublishStatus = "",//invalid!
                         UserId = comment.UserId,
                         InReplyTo = comment.InReplyTo == null ? null :
                        new GanjoorCommentSummaryViewModel()
                        {
                            Id = comment.InReplyTo.Id,
                            AuthorName = comment.InReplyTo.User == null ? comment.InReplyTo.AuthorName : $"{comment.InReplyTo.User.NickName}",
                            AuthorUrl = comment.InReplyTo.AuthorUrl,
                            CommentDate = comment.InReplyTo.CommentDate,
                            HtmlComment = comment.InReplyTo.HtmlComment,
                            PublishStatus = "",
                            UserId = comment.InReplyTo.UserId
                        },
                         Poem = new GanjoorPoemSummaryViewModel()
                         {
                             Id = comment.Poem.Id,
                             Title = comment.Poem.FullTitle,
                             UrlSlug = comment.Poem.FullUrl,
                             Excerpt = ""
                         }
                     }
                 };

            (PaginationMetadata PagingMeta, GanjoorCommentAbuseReportViewModel[] Items) paginatedResult =
                await QueryablePaginator<GanjoorCommentAbuseReportViewModel>.Paginate(source, paging);


            foreach (GanjoorCommentAbuseReportViewModel report in paginatedResult.Items)
            {
                report.Comment.AuthorName = report.Comment.AuthorName.ToPersianNumbers().ApplyCorrectYeKe();
            }

            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorCommentAbuseReportViewModel[] Items)>(paginatedResult);
        }


        /// <summary>
        /// get poem images by id (some fields are intentionally field with blank or null),
        /// EntityImageId : the most important data field, image url is {WebServiceUrl.Url}/api/images/thumb/{EntityImageId}.jpg or {WebServiceUrl.Url}/api/images/norm/{EntityImageId}.jpg
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemRelatedImage[]>> GetPoemImages(int id)
        {
            var museumSrc =
                 from link in _context.GanjoorLinks.Include(l => l.Artifact).Include(l => l.Item).ThenInclude(i => i.Images)
                 join poem in _context.GanjoorPoems
                 on link.GanjoorPostId equals poem.Id
                 where
                 link.DisplayOnPage == true
                 &&
                 link.ReviewResult == ReviewResult.Approved
                 &&
                 poem.Id == id
                 orderby link.IsTextOriginalSource descending, link.ReviewDate
                 select new PoemRelatedImage()
                 {
                     PoemRelatedImageType = PoemRelatedImageType.MuseumLink,
                     ThumbnailImageUrl = link.Item.Images.First().ExternalNormalSizeImageUrl.Replace("/norm/", "/thumb/").Replace("/orig/", "/thumb/"),
                     TargetPageUrl = link.LinkToOriginalSource ? link.OriginalSourceUrl : $"https://museum.ganjoor.net/items/{link.Artifact.FriendlyUrl}/{link.Item.FriendlyUrl}",
                     AltText = $"{link.Artifact.Name} » {link.Item.Name}",
                     IsTextOriginalSource = link.IsTextOriginalSource
                 };
            List<PoemRelatedImage> museumImages = await museumSrc.ToListAsync();

            var externalSrc =
                 from link in _context.PinterestLinks
                 join poem in _context.GanjoorPoems
                 on link.GanjoorPostId equals poem.Id
                 where
                 link.ReviewResult == ReviewResult.Approved
                 &&
                 poem.Id == id
                 orderby link.ReviewDate
                 select new PoemRelatedImage()
                 {
                     PoemRelatedImageType = PoemRelatedImageType.ExternalLink,
                     ThumbnailImageUrl = link.LinkType == LinkType.Naskban ? link.PinterestImageUrl : link.Item.Images.First().ExternalNormalSizeImageUrl.Replace("/norm/", "/thumb/").Replace("/orig/", "/thumb/"),
                     TargetPageUrl = link.PinterestUrl,
                     AltText = link.AltText,
                     IsTextOriginalSource = link.IsTextOriginalSource
                 };

            museumImages.AddRange(await externalSrc.AsNoTracking().ToListAsync());

            for (int i = 0; i < museumImages.Count; i++)
            {
                museumImages[i].ImageOrder = 0;
            }
            return new RServiceResult<PoemRelatedImage[]>(museumImages.ToArray());
        }

        /// <summary>
        /// Get Poem By Url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemByUrl(string url, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true)
        {
            // /hafez/ => /hafez :
            if (url.LastIndexOf('/') == url.Length - 1)
            {
                url = url.Substring(0, url.Length - 1);
            }
            var poem = await _context.GanjoorPoems.Where(p => p.FullUrl == url).SingleOrDefaultAsync();
            if (poem == null)
            {
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null); //not found
            }
            return await GetPoemById(poem.Id, catInfo, catPoems, rhymes, recitations, images, songs, comments, verseDetails, navigation);
        }

        /// <summary>
        /// get poem verses
        /// </summary>
        /// <param name="id"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorVerseViewModel[]>> GetPoemVersesAsync(int id, int coupletIndex)
        {
            try
            {
                return new RServiceResult<GanjoorVerseViewModel[]>(await _context.GanjoorVerses
                                                    .Where(v => v.PoemId == id && (coupletIndex == -1 || v.CoupletIndex == coupletIndex))
                                                    .OrderBy(v => v.VOrder)
                                                    .Select
                                                    (
                                                        v => new GanjoorVerseViewModel()
                                                        {
                                                            Id = v.Id,
                                                            VOrder = v.VOrder,
                                                            CoupletIndex = v.CoupletIndex,
                                                            VersePosition = v.VersePosition,
                                                            Text = v.Text,
                                                            SectionIndex1 = v.SectionIndex1,
                                                            SectionIndex2 = v.SectionIndex2,
                                                            SectionIndex3 = v.SectionIndex3,
                                                            SectionIndex4 = v.SectionIndex4,
                                                            LanguageId = v.LanguageId,
                                                            CoupletSummary = v.CoupletSummary,
                                                        }
                                                    ).AsNoTracking().ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorVerseViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get Poem By Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments"></param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation"></param>
        /// <param name="relatedpoems"></param>
        /// <param name="sections">sections</param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetPoemById(int id, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true, bool relatedpoems = true, bool sections = true)
        {
            var cachKey = $"GetPoemById({id}, {catInfo}, {catPoems}, {rhymes}, {recitations}, {images}, {songs}, {comments}, {verseDetails}, {navigation})";
            if (!_memoryCache.TryGetValue(cachKey, out GanjoorPoemCompleteViewModel poemViewModel))
            {
                var poem = await _context.GanjoorPoems.Where(p => p.Id == id).AsNoTracking().SingleOrDefaultAsync();
                if (poem == null)
                {
                    return new RServiceResult<GanjoorPoemCompleteViewModel>(null); //not found
                }
                GanjoorPoetCompleteViewModel cat = null;
                if (catInfo)
                {
                    var catRes = await GetCatById(poem.CatId, catPoems);
                    if (!string.IsNullOrEmpty(catRes.ExceptionString))
                    {
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, catRes.ExceptionString);
                    }
                    cat = catRes.Result;
                }

                GanjoorPoemSummaryViewModel next = null;
                if (navigation)
                {
                    int nextId =
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id > poem.Id)
                                                       .AnyAsync()
                                                       ?
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id > poem.Id)
                                                       .MinAsync(p => p.Id)
                                                       :
                                                       0;
                    if (nextId != 0)
                    {
                        next = await _context.GanjoorPoems.Where(p => p.Id == nextId).Select
                            (
                            p =>
                            new GanjoorPoemSummaryViewModel()
                            {
                                Id = p.Id,
                                Title = p.Title,
                                UrlSlug = p.UrlSlug,
                                Excerpt = _context.GanjoorVerses.Where(v => v.PoemId == p.Id && v.VOrder == 1).FirstOrDefault().Text
                            }
                            ).AsNoTracking().SingleAsync();
                    }

                }

                GanjoorPoemSummaryViewModel previous = null;
                if (navigation)
                {
                    int preId =
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id < poem.Id)
                                                       .AnyAsync()
                                                       ?
                        await _context.GanjoorPoems
                                                       .Where(p => p.CatId == poem.CatId && p.Id < poem.Id)
                                                       .MaxAsync(p => p.Id)
                                                       :
                                                       0;
                    if (preId != 0)
                    {
                        previous = await _context.GanjoorPoems.Where(p => p.Id == preId).Select
                            (
                            p =>
                            new GanjoorPoemSummaryViewModel()
                            {
                                Id = p.Id,
                                Title = p.Title,
                                UrlSlug = p.UrlSlug,
                                Excerpt = _context.GanjoorVerses.Where(v => v.PoemId == p.Id && v.VOrder == 1).FirstOrDefault().Text
                            }
                            ).AsNoTracking().SingleAsync();
                    }

                }

                PublicRecitationViewModel[] rc = null;
                if (recitations)
                {
                    var rcRes = await GetPoemRecitations(id);
                    if (!string.IsNullOrEmpty(rcRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, rcRes.ExceptionString);
                    rc = rcRes.Result;
                }

                PoemRelatedImage[] imgs = null;
                if (images)
                {
                    var imgsRes = await GetPoemImages(id);
                    if (!string.IsNullOrEmpty(imgsRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, imgsRes.ExceptionString);
                    imgs = imgsRes.Result;
                }

                GanjoorVerseViewModel[] verses = null;
                if (verseDetails)
                {
                    verses = await _context.GanjoorVerses
                                                    .Where(v => v.PoemId == id)
                                                    .OrderBy(v => v.VOrder)
                                                    .Select
                                                    (
                                                        v => new GanjoorVerseViewModel()
                                                        {
                                                            Id = v.Id,
                                                            VOrder = v.VOrder,
                                                            CoupletIndex = v.CoupletIndex,
                                                            VersePosition = v.VersePosition,
                                                            Text = v.Text,
                                                            SectionIndex1 = v.SectionIndex1,
                                                            SectionIndex2 = v.SectionIndex2,
                                                            SectionIndex3 = v.SectionIndex3,
                                                            SectionIndex4 = v.SectionIndex4,
                                                            LanguageId = v.LanguageId,
                                                            CoupletSummary = v.CoupletSummary,
                                                        }
                                                    ).AsNoTracking().ToArrayAsync();
                };


                PoemMusicTrackViewModel[] tracks = null;
                if (songs)
                {
                    var songsRes = await GetPoemSongs(id, true, PoemMusicTrackType.All);
                    if (!string.IsNullOrEmpty(songsRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, songsRes.ExceptionString);
                    tracks = songsRes.Result;
                }

                GanjoorCommentSummaryViewModel[] poemComments = null;

                if (comments)
                {
                    var commentsRes = await GetPoemComments(id, Guid.Empty, null);
                    if (!string.IsNullOrEmpty(commentsRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, commentsRes.ExceptionString);
                    poemComments = commentsRes.Result;
                }

                GanjoorPoemSection[] poemSections = null;
                if (sections)
                {
                    var poemSectionsRes = await GetPoemWholeSections(id);
                    if (!string.IsNullOrEmpty(poemSectionsRes.ExceptionString))
                        return new RServiceResult<GanjoorPoemCompleteViewModel>(null, poemSectionsRes.ExceptionString);
                    poemSections = poemSectionsRes.Result;
                }

                var tagsRes = await GetPoemGeoDateTagsAsync(id);
                if (!string.IsNullOrEmpty(tagsRes.ExceptionString))
                    return new RServiceResult<GanjoorPoemCompleteViewModel>(null, tagsRes.ExceptionString);
                PoemGeoDateTag[] geoDateTags = tagsRes.Result;



                poemViewModel = new GanjoorPoemCompleteViewModel()
                {
                    Id = poem.Id,
                    Title = poem.Title,
                    FullTitle = poem.FullTitle,
                    FullUrl = poem.FullUrl,
                    UrlSlug = poem.UrlSlug,
                    HtmlText = poem.HtmlText,
                    PlainText = poem.PlainText,

                    SourceName = poem.SourceName,
                    SourceUrlSlug = poem.SourceUrlSlug,
                    OldTag = poem.OldTag,
                    OldTagPageUrl = poem.OldTagPageUrl,
                    MixedModeOrder = poem.MixedModeOrder,
                    Published = poem.Published,
                    Language = poem.Language,
                    PoemSummary = poem.PoemSummary,
                    Category = cat,
                    Next = next,
                    Previous = previous,
                    Recitations = rc,
                    Images = imgs,
                    Verses = verses,
                    Songs = tracks,
                    Comments = poemComments,
                    Sections = poemSections,
                    GeoDateTags = geoDateTags
                };

                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cachKey, poemViewModel);
                }
            }
            return new RServiceResult<GanjoorPoemCompleteViewModel>
                (
                poemViewModel
                );
        }

        /// <summary>
        /// delete unreviewed user corrections for a poem
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoemCorrections(Guid userId, int poemId)
        {
            var preCorrections = await _context.GanjoorPoemCorrections.Include(c => c.VerseOrderText)
                .Where(c => c.UserId == userId && c.PoemId == poemId && c.Reviewed == false)
                .ToListAsync();
            if (preCorrections.Count > 0)
            {
                foreach (var preCorrection in preCorrections)
                {
                    preCorrection.VerseOrderText.Clear();
                }
                _context.GanjoorPoemCorrections.RemoveRange(preCorrections);
                await _context.SaveChangesAsync();
            }
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// send poem correction
        /// </summary>
        /// <param name="correction"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCorrectionViewModel>> SuggestPoemCorrection(GanjoorPoemCorrectionViewModel correction)
        {
            if (!string.IsNullOrEmpty(correction.Rhythm3) || !string.IsNullOrEmpty(correction.Rhythm4))
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "انتساب وزن سوم و چهارم هنوز پیاده‌سازی نشده است.");

            var sections = await _context.GanjoorPoemSections.AsNoTracking().Include(s => s.GanjoorMetre)
                .Where(s => s.PoemId == correction.PoemId).OrderBy(s => s.SectionType).ThenBy(s => s.Index).ToListAsync();
            //beware: items consisting only of paragraphs have no main setion (mainSection in the following line can legitimately become null)
            var mainSection = sections.FirstOrDefault(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First);
            var secondSection = sections.FirstOrDefault(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.Second);

            if (correction.Rhythm != null || correction.Rhythm2 != null)
            {
                if (correction.Rhythm == "")
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان حذف وزن اول با وجود وزن دوم وجود ندارد.");

                if (mainSection == null)
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان تعیین وزن برای این مورد وجود ندارد.");
                var poemVerses = await _context.GanjoorVerses.AsNoTracking().
                    Where(p => p.PoemId == correction.PoemId).OrderBy(v => v.VOrder).ToListAsync();
                if (poemVerses.Where(v => v.VersePosition == VersePosition.Paragraph).Any())
                {
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب وزن به متون مخلوط از طریق ویرایشگر کاربر وجود ندارد.");
                }
                if (sections.Where(s => s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).Count() > 1)
                {
                    return new RServiceResult<GanjoorPoemCorrectionViewModel>(null, "امکان انتساب وزن به متون حاوی بیش از یک شعر از طریق ویرایشگر کاربر وجود ندارد.");
                }
            }

            var preCorrections = await _context.GanjoorPoemCorrections.Include(c => c.VerseOrderText)
                .Where(c => c.UserId == correction.UserId && c.PoemId == correction.PoemId && c.Reviewed == false)
                .ToListAsync();

            var poem = (await GetPoemById(correction.PoemId, false, false, true, false, false, false, false, true, false)).Result;

            foreach (var verse in correction.VerseOrderText)
            {
                if (!verse.NewVerse)
                {
                    var v = poem.Verses.Where(poemVerse => poemVerse.VOrder == verse.VORder).Single();
                    verse.OriginalText = v.Text;
                    verse.OriginalVersePosition = v.VersePosition;
                    verse.OriginalLanguageId = v.LanguageId;
                    verse.OriginalCoupletSummary = v.CoupletSummary;
                }
            }
            GanjoorPoemCorrection dbCorrection = new GanjoorPoemCorrection()
            {
                PoemId = correction.PoemId,
                UserId = correction.UserId,
                VerseOrderText = correction.VerseOrderText,
                Title = correction.Title,
                OriginalTitle = poem.Title,
                Rhythm = correction.Rhythm,
                OriginalRhythm = (mainSection == null || mainSection.GanjoorMetre == null) ? null : mainSection.GanjoorMetre.Rhythm,
                Rhythm2 = correction.Rhythm2,
                OriginalRhythm2 = (secondSection == null || secondSection.GanjoorMetre == null) ? null : secondSection.GanjoorMetre.Rhythm,
                RhymeLetters = correction.RhymeLetters,
                OriginalRhymeLetters = mainSection == null ? null : mainSection.RhymeLetters,
                PoemFormat = correction.PoemFormat,
                OriginalPoemFormat = correction.PoemFormat == null ? null : mainSection.PoemFormat,
                Note = correction.Note,
                Date = DateTime.Now,
                Result = CorrectionReviewResult.NotReviewed,
                Reviewed = false,
                AffectedThePoem = false,
                //Language = correction.Language, not used
                PoemSummary = correction.PoemSummary,
                HideMyName = correction.HideMyName,

            };
            _context.GanjoorPoemCorrections.Add(dbCorrection);
            await _context.SaveChangesAsync();
            correction.Id = dbCorrection.Id;

            if (preCorrections.Count > 0)
            {
                foreach (var preCorrection in preCorrections)
                {
                    preCorrection.VerseOrderText.Clear();
                }
                _context.GanjoorPoemCorrections.RemoveRange(preCorrections);
                await _context.SaveChangesAsync();
            }

            return new RServiceResult<GanjoorPoemCorrectionViewModel>(correction);
        }

        /// <summary>
        /// last unreviewed user correction for a poem
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="poemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCorrectionViewModel>> GetLastUnreviewedUserCorrectionForPoem(Guid userId, int poemId)
        {
            var dbCorrection = await _context.GanjoorPoemCorrections.AsNoTracking().Include(c => c.VerseOrderText).Include(c => c.User)
                .Where(c => c.UserId == userId && c.PoemId == poemId && c.Reviewed == false)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (dbCorrection == null)
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null);

            return new RServiceResult<GanjoorPoemCorrectionViewModel>
                (
                new GanjoorPoemCorrectionViewModel()
                {
                    Id = dbCorrection.Id,
                    PoemId = dbCorrection.PoemId,
                    UserId = dbCorrection.UserId,
                    VerseOrderText = dbCorrection.VerseOrderText == null ? null : dbCorrection.VerseOrderText.ToArray(),
                    Title = dbCorrection.Title,
                    OriginalTitle = dbCorrection.OriginalTitle,
                    Rhythm = dbCorrection.Rhythm,
                    OriginalRhythm = dbCorrection.OriginalRhythm,
                    RhythmResult = dbCorrection.RhythmResult,
                    Rhythm2 = dbCorrection.Rhythm2,
                    OriginalRhythm2 = dbCorrection.OriginalRhythm2,
                    RhymeLetters = dbCorrection.RhymeLetters,
                    OriginalRhymeLetters = dbCorrection.OriginalRhymeLetters,
                    RhymeLettersReviewResult = dbCorrection.RhymeLettersReviewResult,
                    PoemSummary = dbCorrection.PoemSummary,
                    OriginalPoemSummary = dbCorrection.OriginalPoemSummary,
                    SummaryReviewResult = dbCorrection.SummaryReviewResult,
                    Note = dbCorrection.Note,
                    Date = dbCorrection.Date,
                    Reviewed = dbCorrection.Reviewed,
                    Result = dbCorrection.Result,
                    ReviewNote = dbCorrection.ReviewNote,
                    ReviewDate = dbCorrection.ReviewDate,
                    UserNickname = dbCorrection.HideMyName && dbCorrection.Reviewed ? "" : string.IsNullOrEmpty(dbCorrection.User.NickName) ? dbCorrection.User.Id.ToString() : dbCorrection.User.NickName,
                    PoemFormat = dbCorrection.PoemFormat,
                    OriginalPoemFormat = dbCorrection.OriginalPoemFormat,
                    PoemFormatReviewResult = dbCorrection.PoemFormatReviewResult,
                    HideMyName = dbCorrection.HideMyName,
                }
                ); ;
        }

        /// <summary>
        /// get user or all corrections
        /// </summary>
        /// <param name="userId">if sent empty returns all corrections</param>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCorrectionViewModel[] Items)>> GetUserCorrections(Guid userId, PagingParameterModel paging)
        {
            var source = from dbCorrection in
                             _context.GanjoorPoemCorrections.AsNoTracking().Include(c => c.VerseOrderText).Include(c => c.User)
                         where userId == Guid.Empty || dbCorrection.UserId == userId
                         orderby dbCorrection.Id descending
                         select
                          dbCorrection;

            (PaginationMetadata PagingMeta, GanjoorPoemCorrection[] Items) dbPaginatedResult =
                await QueryablePaginator<GanjoorPoemCorrection>.Paginate(source, paging);

            List<GanjoorPoemCorrectionViewModel> list = new List<GanjoorPoemCorrectionViewModel>();
            foreach (var dbCorrection in dbPaginatedResult.Items)
            {
                list.Add
                    (
                new GanjoorPoemCorrectionViewModel()
                {
                    Id = dbCorrection.Id,
                    PoemId = dbCorrection.PoemId,
                    UserId = dbCorrection.UserId,
                    VerseOrderText = dbCorrection.VerseOrderText == null ? null : dbCorrection.VerseOrderText.ToArray(),
                    Title = dbCorrection.Title,
                    OriginalTitle = dbCorrection.OriginalTitle,
                    Rhythm = dbCorrection.Rhythm,
                    OriginalRhythm = dbCorrection.OriginalRhythm,
                    RhythmResult = dbCorrection.RhythmResult,
                    Rhythm2 = dbCorrection.Rhythm2,
                    OriginalRhythm2 = dbCorrection.OriginalRhythm2,
                    Rhythm2Result = dbCorrection.Rhythm2Result,
                    RhymeLetters = dbCorrection.RhymeLetters,
                    OriginalRhymeLetters = dbCorrection.OriginalRhymeLetters,
                    RhymeLettersReviewResult = dbCorrection.RhymeLettersReviewResult,
                    PoemSummary = dbCorrection.PoemSummary,
                    OriginalPoemSummary = dbCorrection.OriginalPoemSummary,
                    SummaryReviewResult = dbCorrection.SummaryReviewResult,
                    Note = dbCorrection.Note,
                    Date = dbCorrection.Date,
                    Reviewed = dbCorrection.Reviewed,
                    Result = dbCorrection.Result,
                    ReviewNote = dbCorrection.ReviewNote,
                    ReviewDate = dbCorrection.ReviewDate,
                    UserNickname = dbCorrection.HideMyName && dbCorrection.Reviewed ? "" : string.IsNullOrEmpty(dbCorrection.User.NickName) ? dbCorrection.User.Id.ToString() : dbCorrection.User.NickName,
                    PoemFormat = dbCorrection.PoemFormat,
                    OriginalPoemFormat = dbCorrection.OriginalPoemFormat,
                    PoemFormatReviewResult = dbCorrection.PoemFormatReviewResult,
                    HideMyName = dbCorrection.HideMyName,
                }
                );
            }

            return new RServiceResult<(PaginationMetadata, GanjoorPoemCorrectionViewModel[])>
                ((dbPaginatedResult.PagingMeta, list.ToArray()));
        }

        /// <summary>
        /// effective corrections for poem
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCorrectionViewModel[] Items)>> GetPoemEffectiveCorrections(int poemId, PagingParameterModel paging)
        {
            var source = from dbCorrection in
                             _context.GanjoorPoemCorrections.AsNoTracking().Include(c => c.VerseOrderText)
                         where
                         dbCorrection.PoemId == poemId
                         &&
                         dbCorrection.Reviewed == true
                         &&
                         (
                         dbCorrection.Result == CorrectionReviewResult.Approved || dbCorrection.RhythmResult == CorrectionReviewResult.Approved 
                         || dbCorrection.Rhythm2Result == CorrectionReviewResult.Approved || dbCorrection.RhymeLettersReviewResult == CorrectionReviewResult.Approved
                         || dbCorrection.PoemFormatReviewResult == CorrectionReviewResult.Approved
                         ||
                         dbCorrection.SummaryReviewResult == CorrectionReviewResult.Approved
                         ||
                         dbCorrection.VerseOrderText
                            .Any(v =>
                                v.Result == CorrectionReviewResult.Approved
                                ||
                                v.VersePositionResult == CorrectionReviewResult.Approved
                                ||
                                v.MarkForDeleteResult == CorrectionReviewResult.Approved
                                ||
                                v.NewVerseResult == CorrectionReviewResult.Approved
                                ||
                                v.SummaryReviewResult == CorrectionReviewResult.Approved
                                ||
                                v.LanguageReviewResult == CorrectionReviewResult.Approved
                                )
                         )
                         orderby dbCorrection.Id descending
                         select
                         dbCorrection;

            (PaginationMetadata PagingMeta, GanjoorPoemCorrection[] Items) dbPaginatedResult =
                await QueryablePaginator<GanjoorPoemCorrection>.Paginate(source, paging);

            List<GanjoorPoemCorrectionViewModel> list = new List<GanjoorPoemCorrectionViewModel>();
            foreach (var dbCorrection in dbPaginatedResult.Items)
            {
                list.Add
                    (
                new GanjoorPoemCorrectionViewModel()
                {
                    Id = dbCorrection.Id,
                    PoemId = dbCorrection.PoemId,
                    UserId = dbCorrection.UserId,
                    VerseOrderText = dbCorrection.VerseOrderText == null ? null : dbCorrection.VerseOrderText.ToArray(),
                    Title = dbCorrection.Title,
                    OriginalTitle = dbCorrection.OriginalTitle,
                    Rhythm = dbCorrection.Rhythm,
                    OriginalRhythm = dbCorrection.OriginalRhythm,
                    RhythmResult = dbCorrection.RhythmResult,
                    Rhythm2 = dbCorrection.Rhythm2,
                    OriginalRhythm2 = dbCorrection.OriginalRhythm2,
                    Rhythm2Result = dbCorrection.Rhythm2Result,
                    RhymeLetters = dbCorrection.RhymeLetters,
                    OriginalRhymeLetters = dbCorrection.OriginalRhymeLetters,
                    RhymeLettersReviewResult = dbCorrection.RhymeLettersReviewResult,
                    PoemSummary = dbCorrection.PoemSummary,
                    OriginalPoemSummary = dbCorrection.OriginalPoemSummary,
                    SummaryReviewResult = dbCorrection.SummaryReviewResult,
                    Note = dbCorrection.Note,
                    Date = dbCorrection.Date,
                    Reviewed = dbCorrection.Reviewed,
                    Result = dbCorrection.Result,
                    ReviewNote = dbCorrection.ReviewNote,
                    ReviewDate = dbCorrection.ReviewDate,
                    PoemFormat = dbCorrection.PoemFormat,
                    OriginalPoemFormat = dbCorrection.OriginalPoemFormat,
                    PoemFormatReviewResult = dbCorrection.PoemFormatReviewResult,
                }
                );
            }

            return new RServiceResult<(PaginationMetadata, GanjoorPoemCorrectionViewModel[])>
                ((dbPaginatedResult.PagingMeta, list.ToArray()));
        }

        /// <summary>
        /// get correction by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCorrectionViewModel>> GetCorrectionById(int id)
        {
            var dbCorrection = await _context.GanjoorPoemCorrections.AsNoTracking().Include(c => c.VerseOrderText).Include(c => c.User)
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (dbCorrection == null)
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null);

            return new RServiceResult<GanjoorPoemCorrectionViewModel>
                (
                new GanjoorPoemCorrectionViewModel()
                {
                    Id = dbCorrection.Id,
                    PoemId = dbCorrection.PoemId,
                    UserId = dbCorrection.UserId,
                    VerseOrderText = dbCorrection.VerseOrderText == null ? null : dbCorrection.VerseOrderText.OrderBy(v => v.VORder).ToArray(),
                    Title = dbCorrection.Title,
                    OriginalTitle = dbCorrection.OriginalTitle,
                    Rhythm = dbCorrection.Rhythm,
                    OriginalRhythm = dbCorrection.OriginalRhythm,
                    RhythmResult = dbCorrection.RhythmResult,
                    Rhythm2 = dbCorrection.Rhythm2,
                    OriginalRhythm2 = dbCorrection.OriginalRhythm2,
                    Rhythm2Result = dbCorrection.Rhythm2Result,
                    RhymeLetters = dbCorrection.RhymeLetters,
                    OriginalRhymeLetters = dbCorrection.OriginalRhymeLetters,
                    RhymeLettersReviewResult = dbCorrection.RhymeLettersReviewResult,
                    PoemSummary = dbCorrection.PoemSummary,
                    OriginalPoemSummary = dbCorrection.OriginalPoemSummary,
                    SummaryReviewResult = dbCorrection.SummaryReviewResult,
                    Note = dbCorrection.Note,
                    Date = dbCorrection.Date,
                    Reviewed = dbCorrection.Reviewed,
                    Result = dbCorrection.Result,
                    ReviewNote = dbCorrection.ReviewNote,
                    ReviewDate = dbCorrection.ReviewDate,
                    UserNickname = dbCorrection.HideMyName && dbCorrection.Reviewed ? "" : string.IsNullOrEmpty(dbCorrection.User.NickName) ? dbCorrection.User.Id.ToString() : dbCorrection.User.NickName,
                    PoemFormat = dbCorrection.PoemFormat,
                    OriginalPoemFormat = dbCorrection.OriginalPoemFormat,
                    PoemFormatReviewResult = dbCorrection.PoemFormatReviewResult,
                    HideMyName = dbCorrection.HideMyName,
                }
                );
        }

        /// <summary>
        /// get next unreviewed correction
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="onlyUserCorrections"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCorrectionViewModel>> GetNextUnreviewedCorrection(int skip, bool onlyUserCorrections)
        {
            string systemEmail = $"{Configuration.GetSection("Ganjoor")["SystemEmail"]}";
            var systemUser = await _appUserService.FindUserByEmail(systemEmail);
            var systemUserId = systemUser.Result == null ? Guid.Empty : (Guid)systemUser.Result.Id;

            var dbCorrection = await _context.GanjoorPoemCorrections.AsNoTracking().Include(c => c.VerseOrderText).Include(c => c.User)
                .Where(c => c.Reviewed == false && (onlyUserCorrections == false || c.UserId != systemUserId))
                .OrderBy(c => c.Id)
                .Skip(skip)
                .FirstOrDefaultAsync();

            if (dbCorrection == null)
                return new RServiceResult<GanjoorPoemCorrectionViewModel>(null);

            return new RServiceResult<GanjoorPoemCorrectionViewModel>
                (
                new GanjoorPoemCorrectionViewModel()
                {
                    Id = dbCorrection.Id,
                    PoemId = dbCorrection.PoemId,
                    UserId = dbCorrection.UserId,
                    VerseOrderText = dbCorrection.VerseOrderText == null ? null : dbCorrection.VerseOrderText.ToArray(),
                    Title = dbCorrection.Title,
                    OriginalTitle = dbCorrection.OriginalTitle,
                    Rhythm = dbCorrection.Rhythm,
                    OriginalRhythm = dbCorrection.OriginalRhythm,
                    RhythmResult = dbCorrection.RhythmResult,
                    RhymeLetters = dbCorrection.RhymeLetters,
                    OriginalRhymeLetters = dbCorrection.OriginalRhymeLetters,
                    RhymeLettersReviewResult = dbCorrection.RhymeLettersReviewResult,
                    PoemSummary = dbCorrection.PoemSummary,
                    OriginalPoemSummary = dbCorrection.OriginalPoemSummary,
                    SummaryReviewResult = dbCorrection.SummaryReviewResult,
                    Note = dbCorrection.Note,
                    Date = dbCorrection.Date,
                    Reviewed = dbCorrection.Reviewed,
                    Result = dbCorrection.Result,
                    Rhythm2 = dbCorrection.Rhythm2,
                    OriginalRhythm2 = dbCorrection.OriginalRhythm2,
                    Rhythm2Result = dbCorrection.Rhythm2Result,
                    ReviewNote = dbCorrection.ReviewNote,
                    ReviewDate = dbCorrection.ReviewDate,
                    UserNickname = dbCorrection.HideMyName && dbCorrection.Reviewed ? "" : string.IsNullOrEmpty(dbCorrection.User.NickName) ? dbCorrection.User.Id.ToString() : dbCorrection.User.NickName,
                    PoemFormat = dbCorrection.PoemFormat,
                    OriginalPoemFormat = dbCorrection.OriginalPoemFormat,
                    PoemFormatReviewResult = dbCorrection.PoemFormatReviewResult,
                    HideMyName = dbCorrection.HideMyName,
                }
                );
        }

        /// <summary>
        /// unreview correction count
        /// </summary>
        /// <param name="onlyUserCorrections"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreviewedCorrectionCount(bool onlyUserCorrections)
        {
            string systemEmail = $"{Configuration.GetSection("Ganjoor")["SystemEmail"]}";
            var systemUser = await _appUserService.FindUserByEmail(systemEmail);
            var systemUserId = systemUser.Result == null ? Guid.Empty : (Guid)systemUser.Result.Id;
            return new RServiceResult<int>(await _context.GanjoorPoemCorrections.AsNoTracking().Include(c => c.VerseOrderText)
                .Where(c => c.Reviewed == false && (onlyUserCorrections == false || c.UserId != systemUserId))
                .CountAsync());
        }

        /// <summary>
        /// random poem id from hafez sonnets and old c.ganjoor.net service
        /// </summary>
        /// <returns></returns>
        private int _GetRandomPoemId(int poetId, int loopBreaker = 0)
        {
            if (loopBreaker > 10)
                return 0;
            Random r = new Random(DateTime.Now.Millisecond);

            switch (poetId)
            {
                case 2://حافظ
                    {
                        //this is magic number based method!
                        int startPoemId = 2130;
                        int endPoemId = 2624 + 1; //one is added for مژده ای دل که مسیحا نفسی می‌آید
                        int poemId = r.Next(startPoemId, endPoemId);
                        if (poemId == endPoemId)
                        {
                            poemId = 33179;//مژده ای دل که مسیحا نفسی می‌آید
                        }
                        return poemId;
                    }
                case 3://خیام
                    return r.Next(1119, 1296);
                case 26://ابوسعید
                    return r.Next(20509, 21232);
                case 22://صائب
                    return r.Next(52198, 59193);
                case 7://سعدی
                    return r.Next(9323, 9959);
                case 28://بابا طاهر
                    return r.Next(21309, 21674);
                case 5://مولانا
                    return r.Next(2625, 5853);
                case 19://اوحدی
                    return r.Next(16955, 17839);
                case 35://شهریار
                    return r.Next(27065, 27224);
                case 20://خواجو
                    return r.Next(18288, 19219);
                case 32://فروغی
                    return r.Next(22996, 23511);
                case 21://عراقی
                    return r.Next(19222, 19526);
                case 40://سلمان
                    return r.Next(38411, 39320);
                case 29://محتشم
                    return r.Next(21744, 22338);
                case 34://امیرخسرو
                    return r.Next(60582, 62578);
                case 31://سیف
                    return r.Next(62837, 63418);
                case 33://عبید
                    return r.Next(23551, 23656);
                case 25://هاتف
                    return r.Next(20275, 20364);
                case 41://رهی
                    return r.Next(39441, 39546);
            }

            int[] poetIdArray = new int[]
            {
                2,
                3,
                26,
                22,
                7,
                28,
                5,
                19,
                35,
                20,
                23,
                21,
                40,
                29,
                34,
                31,
                33,
                25,
                41
            };

            return _GetRandomPoemId(poetIdArray[r.Next(0, poetIdArray.Length - 1)], loopBreaker++);

        }



        /// <summary>
        /// get a random poem from hafez
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="recitation"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemCompleteViewModel>> Faal(int poetId = 2, bool recitation = true)
        {
            int poemId = _GetRandomPoemId(poetId);
            var poem = await _context.GanjoorPoems.Where(p => p.Id == poemId).AsNoTracking().SingleOrDefaultAsync();
            PublicRecitationViewModel[] recitations = poem == null || !recitation ? new PublicRecitationViewModel[] { } : (await GetPoemRecitations(poemId)).Result;
            int loopPreventer = 0;
            while (poem == null || (recitation && recitations.Length == 0))
            {
                poem = await _context.GanjoorPoems.Where(p => p.Id == poemId).AsNoTracking().SingleOrDefaultAsync();
                recitations = poem == null ? new PublicRecitationViewModel[] { } : (await GetPoemRecitations(poemId)).Result;
                loopPreventer++;
                if (loopPreventer > 5)
                {
                    return new RServiceResult<GanjoorPoemCompleteViewModel>(null);
                }
            }

            return await GetPoemById(poemId, false, false, false, recitation, false, false, false, true /*verse details*/, false);
        }

        /// <summary>
        /// Get Similar Poems accroding to prosody and rhyme informations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="metre"></param>
        /// <param name="rhyme"></param>
        /// <param name="poetId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> GetSimilarPoems(PagingParameterModel paging, string metre, string rhyme, int? poetId)
        {
            var source =
                _context.GanjoorPoemSections.Include(s => s.Poem).Include(s => s.Poet).Include(s => s.GanjoorMetre)
                .Where(s =>
                        (poetId == null || s.PoetId == poetId)
                        &&
                        (string.IsNullOrEmpty(s.Language) || s.Language == "fa-IR")
                        &&
                        (string.IsNullOrEmpty(metre) || (metre == "null" && s.GanjoorMetreId == null) || (!string.IsNullOrEmpty(metre) && s.GanjoorMetre.Rhythm == metre))
                        &&
                        ((string.IsNullOrEmpty(rhyme) && s.SectionType == PoemSectionType.WholePoem) || (!string.IsNullOrEmpty(rhyme) && s.RhymeLetters == rhyme))
                        )
                .OrderBy(p => p.Poet.BirthYearInLHijri).ThenBy(p => p.Poet.Nickname).ThenBy(p => p.SectionType).ThenBy(p => p.Poem.Id)
                .Select
                (
                    section =>
                    new GanjoorPoemCompleteViewModel()
                    {
                        Id = section.Poem.Id,
                        Title = section.Poem.Title,
                        FullTitle = section.Poem.FullTitle,
                        FullUrl = section.CachedFirstCoupletIndex == 0 ? section.Poem.FullUrl : section.Poem.FullUrl + "#bn" + (section.CachedFirstCoupletIndex + 1).ToString(),
                        UrlSlug = section.Poem.UrlSlug,
                        HtmlText = section.HtmlText,
                        PlainText = section.PlainText,
                        MixedModeOrder = section.Poem.MixedModeOrder,
                        Published = section.Poem.Published,
                        Language = section.Poem.Language,
                        PoemSummary = section.Poem.PoemSummary,
                        Category = new GanjoorPoetCompleteViewModel()
                        {
                            Poet = new GanjoorPoetViewModel()
                            {
                                Id = section.Poet.Id,
                            }
                        },
                        SectionIndex = section.Index

                    }
                ).AsNoTracking();


            (PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items) paginatedResult =
               await QueryablePaginator<GanjoorPoemCompleteViewModel>.Paginate(source, paging);


            Dictionary<int, GanjoorPoetCompleteViewModel> cachedPoets = new Dictionary<int, GanjoorPoetCompleteViewModel>();

            foreach (var item in paginatedResult.Items)
            {
                if (cachedPoets.TryGetValue(item.Category.Poet.Id, out GanjoorPoetCompleteViewModel poet))
                {
                    item.Category = poet;
                }
                else
                {
                    poet = (await GetPoetById(item.Category.Poet.Id)).Result;

                    cachedPoets.Add(item.Category.Poet.Id, poet);

                    item.Category = poet;
                }
            }

            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>(paginatedResult);
        }
        private async Task _populateCategoryChildren(int catId, List<int> catListId)
        {
            var catRes = await GetCatById(catId, false);
            foreach (var c in catRes.Result.Cat.Children)
            {
                catListId.Add(c.Id);
                await _populateCategoryChildren(c.Id, catListId);
            }
        }

        /// <summary>
        /// language tagged poem sections
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="language"></param>
        /// <param name="poetId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> GetLanguageTaggedPoemSections(PagingParameterModel paging, string language, int? poetId)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = "fa-IR";
            }
            var source =
                _context.GanjoorPoemSections.Include(s => s.Poem).Include(s => s.Poet).Include(s => s.GanjoorMetre)
                .Where(s =>
                        (poetId == null || s.PoetId == poetId)
                        &&
                        ((language == "fa-IR" && string.IsNullOrEmpty(s.Language)) || s.Language == language)
                        &&
                        s.SectionType == PoemSectionType.WholePoem
                        )
                .OrderBy(p => p.Poet.BirthYearInLHijri).ThenBy(p => p.Poet.Nickname).ThenBy(p => p.Poem.Id)
                .Select
                (
                    section =>
                    new GanjoorPoemCompleteViewModel()
                    {
                        Id = section.Poem.Id,
                        Title = section.Poem.Title,
                        FullTitle = section.Poem.FullTitle,
                        FullUrl = section.CachedFirstCoupletIndex == 0 ? section.Poem.FullUrl : section.Poem.FullUrl + "#bn" + (section.CachedFirstCoupletIndex + 1).ToString(),
                        UrlSlug = section.Poem.UrlSlug,
                        HtmlText = section.HtmlText,
                        PlainText = section.PlainText,
                        MixedModeOrder = section.Poem.MixedModeOrder,
                        Published = section.Poem.Published,
                        Language = section.Poem.Language,
                        PoemSummary = section.Poem.PoemSummary,
                        Category = new GanjoorPoetCompleteViewModel()
                        {
                            Poet = new GanjoorPoetViewModel()
                            {
                                Id = section.Poet.Id,
                            }
                        },
                        SectionIndex = section.Index

                    }
                ).AsNoTracking();


            (PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items) paginatedResult =
               await QueryablePaginator<GanjoorPoemCompleteViewModel>.Paginate(source, paging);


            Dictionary<int, GanjoorPoetCompleteViewModel> cachedPoets = new Dictionary<int, GanjoorPoetCompleteViewModel>();

            foreach (var item in paginatedResult.Items)
            {
                if (cachedPoets.TryGetValue(item.Category.Poet.Id, out GanjoorPoetCompleteViewModel poet))
                {
                    item.Category = poet;
                }
                else
                {
                    poet = (await GetPoetById(item.Category.Poet.Id)).Result;

                    cachedPoets.Add(item.Category.Poet.Id, poet);

                    item.Category = poet;
                }
            }

            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>(paginatedResult);
        }



        /// <summary>
        /// Search
        /// You need to run this scripts manually on the database before using this method:
        /// 
        /// CREATE FULLTEXT CATALOG [GanjoorPoemPlainTextCatalog] WITH ACCENT_SENSITIVITY = OFF AS DEFAULT
        /// 
        /// CREATE FULLTEXT INDEX ON [dbo].[GanjoorPoems](
        /// [PlainText] LANGUAGE 'English')
        /// KEY INDEX [PK_GanjoorPoems]ON ([GanjoorPoemPlainTextCatalog], FILEGROUP [PRIMARY])
        /// WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> Search(PagingParameterModel paging, string term, int? poetId, int? catId)
        {
            term = term.Trim().ApplyCorrectYeKe();

            if (string.IsNullOrEmpty(term))
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>((null, null), "خطای جستجوی عبارت خالی");
            }

            term = term.Replace("‌", " ");//replace zwnj with space


            string searchConditions;
            if (term.IndexOf('"') == 0 && term.LastIndexOf('"') == (term.Length - 1))
            {
                searchConditions = term.Replace("\"", "").Replace("'", "");
                searchConditions = $"\"{searchConditions}\"";
            }
            else
            {
                string[] words = term.Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                searchConditions = "";
                string emptyOrAnd = "";
                foreach (string word in words)
                {
                    searchConditions += $" {emptyOrAnd} \"*{word}*\" ";
                    emptyOrAnd = " AND ";
                }
            }
            if (poetId == null)
            {
                catId = null;
            }
            if (poetId != null && catId == null)
            {
                var poetRes = await GetPoetById((int)poetId);
                if (!string.IsNullOrEmpty(poetRes.ExceptionString))
                    return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>((null, null), poetRes.ExceptionString);
                catId = poetRes.Result.Cat.Id;
            }
            List<int> catIdList = new List<int>();
            if (catId != null)
            {
                catIdList.Add((int)catId);
                await _populateCategoryChildren((int)catId, catIdList);
            }

            var source =
                _context.GanjoorPoems
                .Where(p =>
                        (catId == null || catIdList.Contains(p.CatId))
                        &&
                       EF.Functions.Contains(p.PlainText, searchConditions)
                        )
                .Include(p => p.Cat).ThenInclude(c => c.Poet)
                .OrderBy(p => p.Cat.Poet.BirthYearInLHijri).ThenBy(p => p.Cat.Poet.Nickname).ThenBy(p => p.Id)
                .Select
                (
                    poem =>
                    new GanjoorPoemCompleteViewModel()
                    {
                        Id = poem.Id,
                        Title = poem.Title,
                        FullTitle = poem.FullTitle,
                        FullUrl = poem.FullUrl,
                        UrlSlug = poem.UrlSlug,
                        HtmlText = poem.HtmlText,
                        PlainText = poem.PlainText,
                        MixedModeOrder = poem.MixedModeOrder,
                        Published = poem.Published,
                        Language = poem.Language,
                        PoemSummary = poem.PoemSummary,
                        Category = new GanjoorPoetCompleteViewModel()
                        {
                            Poet = new GanjoorPoetViewModel()
                            {
                                Id = poem.Cat.Poet.Id,
                            }
                        },
                    }
                ).AsNoTracking();



            (PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items) paginatedResult =
               await QueryablePaginator<GanjoorPoemCompleteViewModel>.Paginate(source, paging);


            Dictionary<int, GanjoorPoetCompleteViewModel> cachedPoets = new Dictionary<int, GanjoorPoetCompleteViewModel>();

            foreach (var item in paginatedResult.Items)
            {
                if (cachedPoets.TryGetValue(item.Category.Poet.Id, out GanjoorPoetCompleteViewModel poet))
                {
                    item.Category = poet;
                }
                else
                {
                    poet = (await GetPoetById(item.Category.Poet.Id)).Result;

                    cachedPoets.Add(item.Category.Poet.Id, poet);

                    item.Category = poet;
                }

            }
            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>(paginatedResult);
        }

        private async Task _UpdatePageChildrenTitleAndUrl(RMuseumDbContext context, GanjoorPage dbPage, bool messWithTitles, bool messWithUrls)
        {
            var children = await context.GanjoorPages.Where(p => p.ParentId == dbPage.Id).ToListAsync();
            foreach (var child in children)
            {
                child.FullUrl = dbPage.FullUrl + "/" + child.UrlSlug;
                child.FullTitle = dbPage.FullTitle + " » " + child.Title;

                switch (child.GanjoorPageType)
                {
                    case GanjoorPageType.PoemPage:
                        {
                            GanjoorPoem poem = await context.GanjoorPoems.Where(p => p.Id == child.Id).SingleAsync();
                            if (messWithTitles)
                                poem.FullTitle = child.FullTitle;
                            if (messWithUrls)
                                poem.FullUrl = child.FullUrl;

                            context.GanjoorPoems.Update(poem);
                        }
                        break;
                    case GanjoorPageType.CatPage:
                        {
                            if (messWithUrls)
                            {
                                GanjoorCat cat = await context.GanjoorCategories.Where(c => c.Id == child.CatId).SingleAsync();
                                cat.FullUrl = child.FullUrl;
                                context.GanjoorCategories.Update(cat);
                            }

                        }
                        break;
                }

                await _UpdatePageChildrenTitleAndUrl(context, child, messWithTitles, messWithUrls);

                CacheCleanForPageByUrl(child.FullUrl);
            }
            context.GanjoorPages.UpdateRange(children);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// modify page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editingUserId"></param>
        /// <param name="pageData"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPageCompleteViewModel>> UpdatePageAsync(int id, Guid editingUserId, GanjoorModifyPageViewModel pageData)
        {
            return await _UpdatePageAsync(_context, id, editingUserId, pageData, true);
        }

        /// <summary>
        /// modify poem => only these fields: NoIndex, RedirectFromFullUrl, MixedModeOrder
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editingUserId"></param>
        /// <param name="pageData"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPageCompleteViewModel>> UpdatePoemAsync(int id, Guid editingUserId, GanjoorModifyPageViewModel pageData)
        {
            return await _UpdatePoemAsync(_context, id, editingUserId, pageData, true);
        }

        /// <summary>
        /// break a poem from a verse forward
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="vOrder"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> BreakPoemAsync(int poemId, int vOrder, Guid userId)
        {
            var poem = (await GetPoemById(poemId, true, false, true, false, false, false, false, true, true)).Result;
            var parentPage = await _context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == poem.Category.Cat.Id).SingleAsync();
            var poemTitleStaticPart = "شمارهٔ";
            if (poem.Next == null)
            {
                return await _BreakLastPoemInItsCategoryAsync(_context, poemId, vOrder, userId, poem, parentPage, poemTitleStaticPart);
            }

            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                var job = (await jobProgressServiceEF.NewJob($"Breaking poem {poem.FullTitle}", "Query data")).Result;
                                try
                                {
                                    var res = await _BreakPoemAsync(context, poemId, vOrder, userId, poem, parentPage, poemTitleStaticPart);
                                    if (!string.IsNullOrEmpty(res.ExceptionString))
                                    {
                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, res.ExceptionString);
                                        return;
                                    }

                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                }
                                catch (Exception exp)
                                {
                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                }

                            }
                        });

            return new RServiceResult<int>(-1);
        }

        /// <summary>
        /// update related sections
        /// </summary>
        /// <param name="metreId"></param>
        /// <param name="rhyme"></param>
        public void UpdateRelatedSections(int metreId, string rhyme)
        {
            if (string.IsNullOrEmpty(rhyme))
                return;
            if (metreId <= 0)
                return;
            _backgroundTaskQueue.QueueBackgroundWorkItem
                                    (
                                    async token =>
                                    {
                                        using (RMuseumDbContext inlineContext = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so context might be already been freed/collected by GC
                                        {
                                            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(inlineContext);
                                            var job = (await jobProgressServiceEF.NewJob($"بازسازی فهرست بخش‌های مرتبط", $"M: {metreId}, G: {rhyme}")).Result;

                                            try
                                            {
                                                await _UpdateRelatedSections(inlineContext, metreId, rhyme);
                                                await inlineContext.SaveChangesAsync();

                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                            }
                                            catch (Exception exp)
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                            }
                                        }
                                    });
        }

        /// <summary>
        /// return page modifications history
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPageSnapshotSummaryViewModel[]>> GetOlderVersionsOfPage(int pageId)
        {
            return
                new RServiceResult<GanjoorPageSnapshotSummaryViewModel[]>
                (
                    await _context.GanjoorPageSnapshots.AsNoTracking()
                                    .Where(s => s.GanjoorPageId == pageId)
                                    .OrderByDescending(s => s.RecordDate)
                                    .Select
                                    (
                                        s =>
                                            new GanjoorPageSnapshotSummaryViewModel()
                                            {
                                                Id = s.Id,
                                                RecordDate = s.RecordDate,
                                                Note = s.Note
                                            }
                                    )
                                    .ToArrayAsync()
                );
        }

        /// <summary>
        /// get old version
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorModifyPageViewModel>> GetOldVersionOfPage(int id)
        {
            return new RServiceResult<GanjoorModifyPageViewModel>
                (
                await _context.GanjoorPageSnapshots.AsNoTracking()
                              .Where(s => s.Id == id)
                              .Select
                              (
                                s =>
                                    new GanjoorModifyPageViewModel()
                                    {
                                        HtmlText = s.HtmlText,
                                        Note = s.Note,
                                        OldTag = s.OldTag,
                                        OldTagPageUrl = s.OldTagPageUrl,
                                        RhymeLetters = s.RhymeLetters,
                                        Rhythm = s.Rhythm,
                                        SourceName = s.SourceName,
                                        SourceUrlSlug = s.SourceUrlSlug,
                                        Title = s.Title,
                                        UrlSlug = s.UrlSlug
                                    }
                              )
                              .SingleOrDefaultAsync()
                );
        }

        /// <summary>
        /// returns metre list (ordered by Rhythm)
        /// </summary>
        /// <param name="sortOnVerseCount"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorMetre[]>> GetGanjoorMetres(bool sortOnVerseCount = false)
        {
            return new RServiceResult<GanjoorMetre[]>(
                sortOnVerseCount ?
                await _context.GanjoorMetres.OrderByDescending(m => m.VerseCount).AsNoTracking().ToArrayAsync()
                :
                await _context.GanjoorMetres.OrderBy(m => m.Rhythm).AsNoTracking().ToArrayAsync()
                );
        }

        private void CleanPoetCache(int poetId)
        {
            //cache clean:
            _memoryCache.Remove($"/api/ganjoor/poets?published={true}&includeBio={false}");
            _memoryCache.Remove($"/api/ganjoor/poets?published={false}&includeBio={true}");
            _memoryCache.Remove("ganjoor/poets");
            _memoryCache.Remove($"/api/ganjoor/poet/{poetId}");
            _memoryCache.Remove($"poet/byid/{poetId}");
        }

        /// <summary>
        /// modify poet
        /// </summary>
        /// <param name="poet"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdatePoetAsync(GanjoorPoetViewModel poet, Guid editingUserId)
        {
            var dbPoet = await _context.GanjoorPoets.Where(p => p.Id == poet.Id).SingleAsync();
            var dbPoetPage = await _context.GanjoorPages.Where(page => page.PoetId == poet.Id && page.GanjoorPageType == GanjoorPageType.PoetPage).SingleAsync();
            if (string.IsNullOrEmpty(poet.Nickname))
            {
                poet.Nickname = dbPoet.Nickname;
                poet.Published = dbPoet.Published;
            }
            if (string.IsNullOrEmpty(poet.FullUrl))
                poet.FullUrl = dbPoetPage.FullUrl;
            if (string.IsNullOrEmpty(poet.Name))
                poet.Name = dbPoet.Name;
            if (string.IsNullOrEmpty(poet.Description))
                poet.Description = dbPoet.Description;

            if (dbPoet.Nickname != poet.Nickname || dbPoetPage.FullUrl != poet.FullUrl)
            {
                var resPageEdit =
                    await UpdatePageAsync
                    (
                    dbPoetPage.Id,
                    editingUserId,
                    new GanjoorModifyPageViewModel()
                    {
                        Title = poet.Nickname,
                        HtmlText = dbPoetPage.HtmlText,
                        Note = "ویرایش مستقیم مشخصات سخنور",
                        UrlSlug = poet.FullUrl.Substring(1),
                    }
                    );
                if (!string.IsNullOrEmpty(resPageEdit.ExceptionString))
                    new RServiceResult<bool>(false, resPageEdit.ExceptionString);

                dbPoet.Nickname = poet.Nickname;
            }
            dbPoet.Name = poet.Name;
            dbPoet.Description = poet.Description;
            bool publishedChange = dbPoet.Published != poet.Published;
            dbPoet.Published = poet.Published;
            dbPoet.BirthYearInLHijri = poet.BirthYearInLHijri;
            dbPoet.ValidBirthDate = poet.ValidBirthDate;
            dbPoet.DeathYearInLHijri = poet.DeathYearInLHijri;
            dbPoet.ValidDeathDate = poet.ValidDeathDate;
            dbPoet.PinOrder = poet.PinOrder;
            dbPoet.BirthLocationId = string.IsNullOrEmpty(poet.BirthPlace) ? null
                : (await _context.GanjoorGeoLocations.Where(l => l.Name == poet.BirthPlace).SingleAsync()).Id;
            dbPoet.DeathLocationId = string.IsNullOrEmpty(poet.DeathPlace) ? null
               : (await _context.GanjoorGeoLocations.Where(l => l.Name == poet.DeathPlace).SingleAsync()).Id;
            _context.GanjoorPoets.Update(dbPoet);
            await _context.SaveChangesAsync();

            if (publishedChange)
            {
                var pages = await _context.GanjoorPages.Where(p => p.PoemId == poet.Id).ToListAsync();
                foreach (var page in pages)
                {
                    page.Published = poet.Published;
                }

                _context.GanjoorPages.UpdateRange(pages);

                await _context.SaveChangesAsync();
            }

            CleanPoetCache(poet.Id);

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// create new poet
        /// </summary>
        /// <param name="poet"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoetCompleteViewModel>> AddPoetAsync(GanjoorPoetViewModel poet, Guid editingUserId)
        {
            if (await _context.GanjoorPoets.Where(p => p.Nickname == poet.Nickname || p.Name == poet.Name).AnyAsync())
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, "conflicting poet");
            }

            if (await _context.GanjoorCategories.Where(c => c.FullUrl == poet.FullUrl).AnyAsync())
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, "conflicting cat");
            }

            if (await _context.GanjoorPages.Where(p => p.FullUrl == poet.FullUrl).AnyAsync())
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, "conflicting page");
            }

            if (poet.FullUrl.IndexOf('/') != 0)
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, "Invalid FullUrl, it must start with /");
            }

            if (poet.FullUrl.Substring(1).IndexOf('/') >= 0)
            {
                return new RServiceResult<GanjoorPoetCompleteViewModel>(null, "Invalid FullUrl, it must contain only one /");
            }

            var id = 1 + await _context.GanjoorPoets.MaxAsync(p => p.Id);

            for (int i = 2; i < id; i++)
            {
                if (!(await _context.GanjoorPoets.Where(p => p.Id == i).AnyAsync()))
                {
                    id = i;
                    break;
                }
            }

            if (string.IsNullOrEmpty(poet.Description))
                poet.Description = "";

            GanjoorPoet dbPoet = new GanjoorPoet()
            {
                Id = id,
                Name = poet.Name,
                Nickname = poet.Nickname,
                Description = poet.Description,
                Published = poet.Published,
                BirthYearInLHijri = poet.BirthYearInLHijri,
                ValidBirthDate = poet.ValidBirthDate,
                DeathYearInLHijri = poet.DeathYearInLHijri,
                ValidDeathDate = poet.ValidDeathDate,
                PinOrder = poet.PinOrder,
                BirthLocationId = string.IsNullOrEmpty(poet.BirthPlace) ? null
                : (await _context.GanjoorGeoLocations.Where(l => l.Name == poet.BirthPlace).SingleAsync()).Id,
                DeathLocationId = string.IsNullOrEmpty(poet.DeathPlace) ? null
               : (await _context.GanjoorGeoLocations.Where(l => l.Name == poet.DeathPlace).SingleAsync()).Id

            };

            _context.GanjoorPoets.Add(dbPoet);

            var poetCatId = 1 + await _context.GanjoorCategories.MaxAsync(c => c.Id);

            GanjoorCat dbCat = new GanjoorCat()
            {
                Id = poetCatId,
                PoetId = id,
                Title = poet.Nickname,
                UrlSlug = poet.FullUrl.Substring(1),
                FullUrl = poet.FullUrl,
                TableOfContentsStyle = GanjoorTOC.Analyse,
                Published = true,
            };
            _context.GanjoorCategories.Add(dbCat);

            var poetPageId = 1 + await _context.GanjoorPages.MaxAsync(p => p.Id);
            while (await _context.GanjoorPoems.Where(p => p.Id == poetPageId).AnyAsync())
                poetPageId++;

            var pageText = "";
            foreach (var line in poet.Description.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                pageText += $"<p>{line}</p>{Environment.NewLine}";
            }

            GanjoorPage dbPage = new GanjoorPage()
            {
                Id = poetPageId,
                GanjoorPageType = GanjoorPageType.PoetPage,
                Published = poet.Published,
                PageOrder = -1,
                Title = poet.Nickname,
                FullTitle = poet.Nickname,
                UrlSlug = poet.FullUrl.Substring(1),
                FullUrl = poet.FullUrl,
                HtmlText = pageText,
                PoetId = id,
                CatId = poetCatId,
                PostDate = DateTime.Now
            };

            _context.GanjoorPages.Add(dbPage);

            GanjoorPageSnapshot snapshot = new GanjoorPageSnapshot()
            {
                GanjoorPageId = poetPageId,
                MadeObsoleteByUserId = editingUserId,
                RecordDate = DateTime.Now,
                Note = "ایجاد سخنور",
                Title = dbPage.Title,
                UrlSlug = dbPage.UrlSlug,
                HtmlText = dbPage.HtmlText,
            };

            _context.GanjoorPageSnapshots.Add(snapshot);

            await _context.SaveChangesAsync();

            CleanPoetCache(0);

            return await GetPoetById(id);
        }

        /// <summary>
        /// delete poet
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartDeletePoet(int id)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                var job = (await jobProgressServiceEF.NewJob($"Deleting Poet {id}", "Query data")).Result;
                                try
                                {
                                    var pages = await context.GanjoorPages.Where(p => p.PoetId == id).ToListAsync();
                                    context.GanjoorPages.RemoveRange(pages);
                                    await jobProgressServiceEF.UpdateJob(job.Id, 50, "Deleting page and Querying the poet - if no progress unplublish and regen group by centuries");
                                    var poet = await context.GanjoorPoets.Where(p => p.Id == id).SingleAsync();
                                    context.GanjoorPoets.Remove(poet);
                                    await jobProgressServiceEF.UpdateJob(job.Id, 99, "Deleting poet");
                                    await context.SaveChangesAsync();
                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                }
                                catch (Exception exp)
                                {
                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                }

                            }
                        });

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// delete a page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePageAsync(int id)
        {
            var firstChild = await _context.GanjoorPages.Where(p => p.ParentId == id).FirstOrDefaultAsync();
            if (firstChild != null)
            {
                return new RServiceResult<bool>(false, "Please delete children of the page first.");
            }
            var page = await _context.GanjoorPages.Where(p => p.Id == id).SingleAsync();
            if (page.PoemId != null)
            {
                return new RServiceResult<bool>(false, "Poem related pages can not be deleted.");
            }
            var cat = await _context.GanjoorCategories.Where(c => c.FullUrl == page.FullUrl).FirstOrDefaultAsync();
            if (cat != null)
            {
                return new RServiceResult<bool>(false, "Category related pages can not be deleted.");
            }
            _context.GanjoorPages.Remove(page);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// delete a poem
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoemAsync(int id)
        {
            try
            {
                var poem = await _context.GanjoorPoems.Where(p => p.Id == id).SingleAsync();


                var comments = await _context.GanjoorComments.Where(c => c.PoemId == id).ToListAsync();
                _context.RemoveRange(comments);

                var music = await _context.GanjoorPoemMusicTracks.Where(m => m.PoemId == id).ToListAsync();
                _context.RemoveRange(music);

                //these lines cause timeout, so I commented them:
                /*
                var similars = await _context.GanjoorCachedRelatedSections.Where(s => s.FullUrl.Contains(poem.FullUrl)).ToListAsync();
                _context.RemoveRange(similars);
                */
                var corrections = await _context.GanjoorPoemCorrections.Include(c => c.VerseOrderText).Where(c => c.PoemId == id).ToListAsync();
                _context.RemoveRange(corrections);

                var page = await _context.GanjoorPages.Where(p => p.Id == id && p.GanjoorPageType == GanjoorPageType.PoemPage).SingleAsync();
                _context.Remove(page);

                _context.Remove(poem);

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete a category
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteCategoryAsync(int id)
        {
            try
            {
                if (true == await _context.GanjoorPoems.Where(p => p.CatId == id).AnyAsync())
                {
                    return new RServiceResult<bool>(false, "cat has poems!");
                }

                var page = await _context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == id).SingleAsync();
                _context.Remove(page);
                await _context.SaveChangesAsync();

                var cat = await _context.GanjoorCategories.Where(c => c.Id == id).SingleAsync();
                _context.Remove(cat);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// chaneg poet image
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="imageId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ChangePoetImageAsync(int poetId, Guid imageId)
        {
            try
            {
                RServiceResult<RImage> img =
                   await _imageFileService.GetImage(imageId);
                if (!string.IsNullOrEmpty(img.ExceptionString))
                {
                    return new RServiceResult<bool>(false, img.ExceptionString);
                }
                if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                {
                    var ftpClient = new AsyncFtpClient
                                        (
                                            Configuration.GetSection("ExternalFTPServer")["Host"],
                                            Configuration.GetSection("ExternalFTPServer")["Username"],
                                            Configuration.GetSection("ExternalFTPServer")["Password"]
                                        );
                    ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                    await ftpClient.AutoConnect();
                    ftpClient.Config.RetryAttempts = 3;
                    RServiceResult<string> imgPath = _imageFileService.GetImagePath(img.Result);
                    if (!string.IsNullOrEmpty(imgPath.ExceptionString))
                        return new RServiceResult<bool>(false, imgPath.ExceptionString);

                    var localFilePath = imgPath.Result;
                    var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/PoetImages/{Path.GetFileName(localFilePath)}";
                    await ftpClient.UploadFile(localFilePath, remoteFilePath);
                    await ftpClient.Disconnect();
                }
                var dbPoet = await _context.GanjoorPoets.Where(p => p.Id == poetId).SingleAsync();
                dbPoet.RImageId = imageId;
                _context.GanjoorPoets.Update(dbPoet);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private void FtpClient_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }

        

       


        /// <summary>
        /// find poem rhyme
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjooRhymeAnalysisResult>> FindPoemMainSectionRhyme(int id)
        {
            var section = await _context.GanjoorPoemSections.AsNoTracking().Where(s => s.PoemId == id && s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).OrderBy(s => s.Index).FirstOrDefaultAsync();
            if (section == null)
                return new RServiceResult<GanjooRhymeAnalysisResult>(null, "no sections");
            return await FindSectionRhyme(section.Id);
        }

        /// <summary>
        /// find poem section rhyme
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjooRhymeAnalysisResult>> FindSectionRhyme(int id)
        {
            return await _FindSectionRhyme(_context, id);
        }


        private async Task<RServiceResult<GanjooRhymeAnalysisResult>> _FindSectionRhyme(RMuseumDbContext context, int id)
        {
            var section = await context.GanjoorPoemSections.Include(s => s.GanjoorMetre).AsNoTracking().Where(s => s.Id == id).FirstOrDefaultAsync();
            if (section == null)
                return new RServiceResult<GanjooRhymeAnalysisResult>(null, "no sections");
            var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == section.PoemId).OrderBy(v => v.VOrder).ToListAsync();
            var rhymeAnalysisResult = LanguageUtils.FindRhyme(FilterSectionVerses(section, verses));
            if (rhymeAnalysisResult.Rhyme.Length > 30 && verses.Count == 2 && section.GanjoorMetre != null)//single verse
            {
                var rhymingSection = await context.GanjoorPoemSections.AsNoTracking()
                                        .Where(s => s.GanjoorMetreId == section.GanjoorMetreId && section.RhymeLetters != null && s.RhymeLetters.Length < 15 && rhymeAnalysisResult.Rhyme.Contains(s.RhymeLetters))
                                        .OrderByDescending(s => s.RhymeLetters.Length)
                                        .FirstOrDefaultAsync();
                if (rhymingSection != null)
                {
                    rhymeAnalysisResult.Rhyme = rhymingSection.RhymeLetters;
                }
            }
            return new RServiceResult<GanjooRhymeAnalysisResult>(rhymeAnalysisResult);
        }



        /// <summary>
        /// find poem rhythm
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> FindPoemMainSectionRhythm(int id)
        {
            var metres = (await GetGanjoorMetres()).Result.Select(m => m.Rhythm).ToArray();
            return await _FindPoemMainSectionRhythm(id, _context, _httpClient, metres);
        }


        private async Task<RServiceResult<string>> _FindPoemMainSectionRhythm(int id, RMuseumDbContext context, HttpClient httpClient, string[] metres, bool alwaysReturnaAResult = false)
        {
            var section = await context.GanjoorPoemSections.AsNoTracking().Where(s => s.PoemId == id && s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).OrderBy(s => s.Index).FirstOrDefaultAsync();
            if (section == null)
                return new RServiceResult<string>(null, "no main sections");
            return await _FindSectionRhythm(section, context, httpClient, metres, alwaysReturnaAResult);
        }
        private async Task<RServiceResult<string>> _FindSectionRhythm(GanjoorPoemSection section, RMuseumDbContext context, HttpClient httpClient, string[] metres, bool alwaysReturnaAResult = false)
        {
            try
            {
                var poemVerses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == section.PoemId).OrderBy(v => v.VOrder).ToListAsync();
                var verses = FilterSectionVerses(section, poemVerses);
                if (verses.Any(v => v.VersePosition == VersePosition.Paragraph))
                {
                    return new RServiceResult<string>("paragraph");
                }

                Dictionary<string, int> rhytmCounter = new Dictionary<string, int>();

                for (int i = 0; i < verses.Count; i++)
                {
                    var verse = verses[i];

                    try
                    {
                        var response = await httpClient.GetAsync($"http://sorud.info/?Text={HttpUtility.UrlEncode(LanguageUtils.MakeTextSearchable(verse.Text))}");
                        response.EnsureSuccessStatusCode();
                        string result = await response.Content.ReadAsStringAsync();
                        if (result.IndexOf("آهنگِ همه‌ی بندها شناسایی نشد.") != -1)
                        {
                            continue;
                        }
                        int nVaznIndex = result.IndexOf("ctl00_MainContent_lblMetricBottom");
                        if (nVaznIndex == -1)
                        {
                            continue;
                        }
                        nVaznIndex += 2;
                        int nQuote1Index = result.IndexOf('\'', nVaznIndex);
                        if (nQuote1Index == -1)
                        {
                            continue;
                        }

                        int nQuote2Index = result.IndexOf('\'', nQuote1Index + 1);
                        if (nQuote2Index == -1)
                        {
                            continue;
                        }

                        string strRokn = result.Substring(nQuote1Index + 1, nQuote2Index - nQuote1Index - 1);

                        int nSpanClose = result.IndexOf("</span>", nQuote2Index + 1);

                        if (nSpanClose == -1)
                        {
                            continue;
                        }

                        int nFQ1 = result.IndexOf('«', nQuote2Index + 1);

                        if (nFQ1 == -1)
                        {
                            continue;
                        }


                        string rhythm = metres.Where(m => m.IndexOf(strRokn) == 0).SingleOrDefault();

                        if (string.IsNullOrEmpty(rhythm))
                            continue;

                        if (rhythm == "فاعلاتن فاعلن فاعلاتن فاعلن")
                            rhythm = "فاعلاتن فاعلاتن فاعلاتن فاعلن (رمل مثمن محذوف)";

                        if (rhythm == "مفاعلتن مفاعلتن مفاعلتن مفاعلتن")
                            rhythm = "مفاعیلن مفاعیلن مفاعیلن مفاعیلن (هزج مثمن سالم)";

                        if (rhythm == "فاعلات مفعولن فاعلات مفعولن")
                            rhythm = "فاعلن مفاعیلن فاعلن مفاعیلن (مقتضب مثمن مطوی مقطوع)";

                        if (rhytmCounter.TryGetValue(rhythm, out int count))
                        {
                            count++;
                            if (count > 10 || (count * 100.0 / verses.Count > 60))
                            {
                                return new RServiceResult<string>(rhythm);
                            }

                        }
                        else
                        {
                            count = 1;
                        }
                        rhytmCounter[rhythm] = count;
                    }
                    catch
                    {
                        //continue
                    }
                }

                if (alwaysReturnaAResult)
                {
                    int maxCount = -1;
                    string rhytm = "";
                    foreach (var r in rhytmCounter)
                    {
                        if (r.Value > maxCount)
                        {
                            maxCount = r.Value;
                            rhytm = r.Key;
                        }
                    }

                    if (!string.IsNullOrEmpty(rhytm))
                        return new RServiceResult<string>(rhytm);

                    if (verses.Count < 9)
                        return new RServiceResult<string>("paragraph");
                }

                return new RServiceResult<string>("");
            }
            catch (Exception exp)
            {
                return new RServiceResult<string>(null, exp.ToString());
            }
        }

        

        /// <summary>
        /// manually add a duplicate for a poems
        /// </summary>
        /// <param name="srcCatId"></param>
        /// <param name="srcPoemId"></param>
        /// <param name="destPoemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AdDuplicateAsync(int srcCatId, int srcPoemId, int destPoemId)
        {
            try
            {
                var alreadyDup = await _context.GanjoorDuplicates.AsNoTracking().Where(p => p.SrcPoemId == srcPoemId).FirstOrDefaultAsync();
                if (alreadyDup != null)
                {
                    return new RServiceResult<bool>(false, $"already dupped : {alreadyDup.DestPoemId}");
                }
                var dup = new GanjoorDuplicate()
                {
                    SrcCatId = srcCatId,
                    SrcPoemId = srcPoemId,
                    DestPoemId = destPoemId
                };
                _context.GanjoorDuplicates.Add(dup);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete duplicate
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteDuplicateAsync(int id)
        {
            try
            {
                var dup = await _context.GanjoorDuplicates.Where(d => d.Id == id).SingleAsync();
                _context.Remove(dup);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(false);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// synchronize naskban links
        /// </summary>
        /// <param name="ganjoorUserId"></param>
        /// <param name="naskbanUserName"></param>
        /// <param name="naskbanPassword"></param>
        /// <returns>number of synched items</returns>
        public async Task<RServiceResult<int>> SynchronizeNaskbanLinksAsync(Guid ganjoorUserId, string naskbanUserName, string naskbanPassword)
        {
            try
            {
                LoginViewModel loginViewModel = new LoginViewModel()
                {
                    Username = naskbanUserName,
                    Password = naskbanPassword,
                    ClientAppName = "Ganjoor API",
                    Language = "fa-IR"
                };
                var loginResponse = await _httpClient.PostAsync("https://api.naskban.ir/api/users/login", new StringContent(JsonConvert.SerializeObject(loginViewModel), Encoding.UTF8, "application/json"));

                if (loginResponse.StatusCode != HttpStatusCode.OK)
                {
                    return new RServiceResult<int>(0, "login error: " + JsonConvert.DeserializeObject<string>(await loginResponse.Content.ReadAsStringAsync()));
                }
                LoggedOnUserModelEx loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await loginResponse.Content.ReadAsStringAsync());

                using (HttpClient secureClient = new HttpClient())
                {
                    secureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedOnUser.Token);
                    var unsyncedResponse = await secureClient.GetAsync("https://api.naskban.ir/api/pdf/ganjoor/unsynched");
                    if (!unsyncedResponse.IsSuccessStatusCode)
                    {
                        return new RServiceResult<int>(0, "unsync error: " + JsonConvert.DeserializeObject<string>(await unsyncedResponse.Content.ReadAsStringAsync()));
                    }
                    var unsynchronizeds = JsonConvert.DeserializeObject<PDFGanjoorLink[]>(await unsyncedResponse.Content.ReadAsStringAsync());
                    foreach (var unsynchronized in unsynchronizeds)
                    {
                        if(false == await _context.PinterestLinks.Where(p => p.NaskbanLinkId == unsynchronized.Id).AnyAsync() )
                        {
                            PinterestLink link = new PinterestLink()
                            {
                                GanjoorPostId = unsynchronized.GanjoorPostId,
                                GanjoorTitle = unsynchronized.GanjoorTitle,
                                GanjoorUrl = unsynchronized.GanjoorUrl,
                                AltText = unsynchronized.PDFPageTitle,
                                LinkType = LinkType.Naskban,
                                PinterestUrl = $"https://naskban.ir/{unsynchronized.PDFBookId}/{unsynchronized.PageNumber}",
                                PinterestImageUrl = unsynchronized.ExternalThumbnailImageUrl,
                                ReviewResult = ReviewResult.Approved,
                                SuggestionDate = DateTime.Now,
                                SuggestedById = ganjoorUserId,
                                Synchronized = true,
                                ReviewerId = ganjoorUserId,
                                IsTextOriginalSource = unsynchronized.IsTextOriginalSource,
                                PDFBookId = unsynchronized.PDFBookId,
                                PageNumber = unsynchronized.PageNumber,
                                NaskbanLinkId = unsynchronized.Id
                            };
                            _context.PinterestLinks.Add(link);
                            await _context.SaveChangesAsync();
                        }
                        await secureClient.PutAsync($"https://api.naskban.ir/api/pdf/ganjoor/sync/{unsynchronized.Id}", null);
                    }

                    var logoutUrl = $"https://api.naskban.ir/api/users/delsession?userId={loggedOnUser.User.Id}&sessionId={loggedOnUser.SessionId}";
                    await secureClient.DeleteAsync(logoutUrl);
                    return new RServiceResult<int>(unsynchronizeds.Length);
                }
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// aggressive cache
        /// </summary>
        public bool AggressiveCacheEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["AggressiveCacheEnabled"]);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Background Task Queue Instance
        /// </summary>
        protected readonly IBackgroundTaskQueue _backgroundTaskQueue;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;


        /// <summary>
        /// Messaging service
        /// </summary>
        protected readonly IRNotificationService _notificationService;

        /// <summary>
        /// Image File Service
        /// </summary>
        protected readonly IImageFileService _imageFileService;

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// http client
        /// </summary>
        protected readonly HttpClient _httpClient;

        /// <summary>
        /// options service
        /// </summary>

        protected readonly IRGenericOptionsService _optionsService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="appUserService"></param>
        /// <param name="notificationService"></param>
        /// <param name="imageFileService"></param>
        /// <param name="memoryCache"></param>
        /// <param name="httpClient"></param>
        /// <param name="optionsService"></param>
        public GanjoorService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue, IAppUserService appUserService, IRNotificationService notificationService, IImageFileService imageFileService, IMemoryCache memoryCache, HttpClient httpClient, IRGenericOptionsService optionsService)
        {
            _context = context;
            _backgroundTaskQueue = backgroundTaskQueue;
            _appUserService = appUserService;
            _notificationService = notificationService;
            _imageFileService = imageFileService;
            _memoryCache = memoryCache;
            Configuration = configuration;
            _httpClient = httpClient;
            _optionsService = optionsService;
        }
    }
}
