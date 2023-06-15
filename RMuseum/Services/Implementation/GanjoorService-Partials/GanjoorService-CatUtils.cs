using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using RSecurityBackend.Services.Implementation;
using DNTPersianUtils.Core;
using System.IO;
using RSecurityBackend.Models.Image;
using FluentFTP;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        /// <summary>
        /// set category poem format tag for poems consisting of a single whole poem section
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetCategoryPoemFormatAsync(int catId, GanjoorPoemFormat? format)
        {
            try
            {
                var poemList = await _context.GanjoorPoems.Where(p => p.CatId == catId).ToListAsync();
                foreach (var poem in poemList)
                {
                    var poemSections = await _context.GanjoorPoemSections.Where(s => s.PoemId == poem.Id && s.SectionType == PoemSectionType.WholePoem).ToListAsync();
                    if(poemSections.Count == 1)
                    {
                        foreach (var section in poemSections)
                        {
                            section.PoemFormat = format;
                        }
                        _context.UpdateRange(poemSections);
                    }
                }
                _context.UpdateRange(poemList);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// set category poems language tag
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetCategoryLanguageTagAsync(int catId, string language)
        {
            try
            {
                var poemList = await _context.GanjoorPoems.Where(p => p.CatId == catId).ToListAsync();
                foreach (var poem in poemList)
                {
                    poem.Language = language;
                    var poemSections = await _context.GanjoorPoemSections.Where(s => s.PoemId == poem.Id && s.SectionType == PoemSectionType.WholePoem).ToListAsync();
                    foreach (var section in poemSections)
                    {
                        section.Language = language;
                    }
                    _context.UpdateRange(poemSections);
                }
                _context.UpdateRange(poemList);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// re sulg cat poems
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> BatchReSlugCatPoems(int catId)
        {
            try
            {
                var poems = await _context.GanjoorPoems.Where(p => p.CatId == catId).OrderBy(p => p.Id).ToListAsync();
                if (poems.Count == 0)
                    return new RServiceResult<bool>(true);

                var catPage = await _context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == catId).SingleOrDefaultAsync();
                if (catPage == null)
                {
                    var catItSelf = await _context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                    catPage = await _context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == catItSelf.PoetId).SingleAsync();
                }

                for (int i = 0; i < poems.Count; i++)
                {
                    if (poems[i].UrlSlug.IndexOf("sh") != 0)
                    {
                        return new RServiceResult<bool>(false, $"{poems[i].FullUrl}");
                    }
                    poems[i].UrlSlug = $"sh{i + 1}";
                    poems[i].FullUrl = $"{catPage.FullUrl}/{poems[i].UrlSlug}";
                }

                foreach (var poem in poems)
                {
                    var page = await _context.GanjoorPages.Where(p => p.Id == poem.Id).SingleAsync();
                    page.UrlSlug = poem.UrlSlug;
                    page.FullUrl = poem.FullUrl;
                    _context.GanjoorPages.Update(page);
                }

                _context.GanjoorPoems.UpdateRange(poems);

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// batch rename
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string[]>> BatchRenameCatPoemTitles(int catId, GanjoorBatchNamingModel model, Guid userId)
        {
            var poems = await _context.GanjoorPoems.Where(p => p.CatId == catId).OrderBy(p => p.Id).ToListAsync();
            if (poems.Count == 0)
                return new RServiceResult<string[]>(new string[] { });

            var catPage = await _context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == catId).SingleOrDefaultAsync();
            if (catPage == null)
            {
                var catItSelf = await _context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                catPage = await _context.GanjoorPages.Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == catItSelf.PoetId).SingleAsync();
            }

            if (model.RemovePreviousPattern)
            {
                char[] numbers = "0123456789۰۱۲۳۴۵۶۷۸۹".ToArray();
                foreach (var poem in poems)
                {
                    _context.GanjoorPageSnapshots.Add
                        (
                        new GanjoorPageSnapshot()
                        {
                            GanjoorPageId = poem.Id,
                            MadeObsoleteByUserId = userId,
                            Title = poem.Title,
                            Note = "تغییر نام گروهی اشعار بخش",
                            RecordDate = DateTime.Now
                        }
                        );
                    int index = poem.Title.IndexOfAny(numbers);
                    if (index != 0)
                    {
                        while ((index + 1) < poem.Title.Length)
                        {
                            if (numbers.Contains(poem.Title[index + 1]))
                                index++;
                            else
                                break;
                        }
                        poem.Title = poem.Title[(index + 1)..].Trim();
                        if (poem.Title.IndexOf('-') == 0)
                        {
                            poem.Title = poem.Title[1..].Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(model.RemoveSetOfCharacters))
                    {
                        foreach (var c in model.RemoveSetOfCharacters)
                        {
                            poem.Title = poem.Title.Replace($"{c}", "");
                        }
                    }
                }
            }

            for (int i = 0; i < poems.Count; i++)
            {
                if (poems[i].Title.Length > 0)
                {
                    poems[i].Title = $"{model.StartWithNotIncludingSpaces} {(i + 1).ToPersianNumbers()} - {poems[i].Title}";
                }
                else
                {
                    poems[i].Title = $"{model.StartWithNotIncludingSpaces} {(i + 1).ToPersianNumbers()}";
                }

                poems[i].FullTitle = $"{catPage.FullTitle} » {poems[i].Title}";

            }

            if (!model.Simulate)
            {
                foreach (var poem in poems)
                {
                    var page = await _context.GanjoorPages.Where(p => p.Id == poem.Id).SingleAsync();
                    page.Title = poem.Title;
                    page.FullTitle = poem.FullTitle;
                    _context.GanjoorPages.Update(page);
                }

                _context.GanjoorPoems.UpdateRange(poems);

                await _context.SaveChangesAsync();
            }

            return new RServiceResult<string[]>(poems.Select(p => p.Title).ToArray());
        }

        /// <summary>
        /// list of category saved duplicated poems
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorDuplicateViewModel[]>> GetCategoryDuplicates(int catId)
        {
            try
            {
                List<GanjoorDuplicateViewModel> dups = new List<GanjoorDuplicateViewModel>();

                var poems = await _context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).OrderBy(p => p.Id).ToListAsync();
                foreach (var poem in poems)
                {
                    var firstVerse = await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).FirstAsync();
                    var dupPoem = await _context.GanjoorDuplicates.AsNoTracking().Where(d => d.SrcCatId == catId && d.SrcPoemId == poem.Id).FirstOrDefaultAsync();
                    GanjoorPoem destPoem = dupPoem == null || dupPoem.DestPoemId == null ? null :
                                    await _context.GanjoorPoems.AsNoTracking().Where(p => p.Id == dupPoem.DestPoemId).SingleAsync();
                    var destPoemFirstVerse = dupPoem == null || dupPoem.DestPoemId == null ? null :
                                     await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == dupPoem.DestPoemId).OrderBy(v => v.VOrder).FirstAsync();
                    dups.Add
                        (
                        new GanjoorDuplicateViewModel()
                        {
                            Id = dupPoem == null ? 0 : dupPoem.Id,
                            SrcPoemId = poem.Id,
                            SrcPoemFullTitle = poem.FullTitle,
                            SrcPoemFullUrl = poem.FullUrl,
                            FirstVerse = firstVerse.Text,
                            DestPoemId = dupPoem == null ? null : dupPoem.DestPoemId,
                            DestPoemFullTitle = dupPoem == null ? "" : destPoem.FullTitle,
                            DestPoemFullUrl = dupPoem == null ? "" : destPoem.FullUrl,
                            DestPoemFirstVerse = destPoemFirstVerse == null ? null : destPoemFirstVerse.Text,
                        }
                        );
                }

                return new RServiceResult<GanjoorDuplicateViewModel[]>(dups.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorDuplicateViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Update category extra info
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="bookName"></param>
        /// <param name="imageId"></param>
        /// <param name="sumUpSubsGeoLocations"></param>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCatViewModel>> SetCategoryExtraInfo(int catId, string bookName, Guid? imageId, bool sumUpSubsGeoLocations, string mapName)
        {
            try
            {
                var cat = await _context.GanjoorCategories.Where(c => c.Id == catId).SingleOrDefaultAsync();
                if (cat == null)
                    return new RServiceResult<GanjoorCatViewModel>(null);//not found
                if (!string.IsNullOrEmpty(bookName))
                {
                    if (cat.CatType != GanjoorCatType.Book)
                    {
                        return new RServiceResult<GanjoorCatViewModel>(null, "cat.CatType != GanjoorCatType.Book");
                    }
                }
                if (imageId != null)
                {
                    RServiceResult<RImage> img =
                    await _imageFileService.GetImage((Guid)imageId);
                    if (!string.IsNullOrEmpty(img.ExceptionString))
                    {
                        return new RServiceResult<GanjoorCatViewModel>(null, img.ExceptionString);
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
                            return new RServiceResult<GanjoorCatViewModel>(null, imgPath.ExceptionString);

                        var localFilePath = imgPath.Result;
                        var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/CategoryImages/{Path.GetFileName(localFilePath)}";
                        await ftpClient.UploadFile(localFilePath, remoteFilePath);
                        await ftpClient.Disconnect();
                    }
                }
                cat.BookName = bookName;
                cat.RImageId = imageId;
                cat.SumUpSubsGeoLocations = sumUpSubsGeoLocations;
                cat.MapName = mapName;
                _context.Update(cat);
                await _context.SaveChangesAsync();
                return new RServiceResult<GanjoorCatViewModel>
                    (
                    new GanjoorCatViewModel()
                    {
                        RImageId = imageId,
                    }
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorCatViewModel>(null, exp.ToString());
            }
        }

        private async Task _FindCategoryPoemsRhymesInternal(int catId, bool retag)
        {
            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
            {
                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                var job = (await jobProgressServiceEF.NewJob($"FindCategoryPoemsRhymes Cat {catId}", "Query data")).Result;
                try
                {
                    var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).ToListAsync();

                    int i = 0;
                    foreach (var poem in poems)
                    {
                        var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).ToListAsync();
                        var sections = await context.GanjoorPoemSections.Where(s => s.PoemId == poem.Id).ToListAsync();
                        foreach (var section in sections)
                        {
                            if (retag || string.IsNullOrEmpty(section.RhymeLetters))
                            {
                                await jobProgressServiceEF.UpdateJob(job.Id, i++);
                                var sectionVerses = FilterSectionVerses(section, verses);
                                try
                                {
                                    var res = LanguageUtils.FindRhyme(sectionVerses);
                                    if (!string.IsNullOrEmpty(res.Rhyme))
                                    {
                                        section.RhymeLetters = res.Rhyme;
                                        context.GanjoorPoemSections.Update(section);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    await jobProgressServiceEF.UpdateJob(job.Id, 99);
                    await context.SaveChangesAsync();
                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                }
                catch (Exception exp)
                {
                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                }
            }
        }



        /// <summary>
        /// find category poem rhymes
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="retag"></param>
        /// <returns></returns>
        public RServiceResult<bool> FindCategoryPoemsRhymes(int catId, bool retag)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            await _FindCategoryPoemsRhymesInternal(catId, retag);
                        });

            return new RServiceResult<bool>(true);
        }
    }
}
