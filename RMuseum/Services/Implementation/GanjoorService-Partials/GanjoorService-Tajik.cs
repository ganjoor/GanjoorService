using Microsoft.EntityFrameworkCore;
using System;
using RMuseum.Models.Ganjoor;
using System.Threading.Tasks;
using RSecurityBackend.Models.Generic;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Linq;
using RSecurityBackend.Services.Implementation;
using System.Collections.Generic;
using RMuseum.DbContext;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// tajik poets
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorTajikPoet[]>> GetTajikPoetsAsync()
        {
            try
            {
                return new RServiceResult<GanjoorTajikPoet[]>(await _context.TajikPoets.AsNoTracking().ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorTajikPoet[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// tajik page by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPageCompleteViewModel>> GetTajikPageByUrlAsync(string url, bool catPoems = false)
        {
            try
            {
                var res = await GetPageByUrl(url, catPoems);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return res;
                GanjoorPageCompleteViewModel page = res.Result;

                int poetId = page.Poem != null ? page.Poem.Category.Poet.Id : page.PoetOrCat.Poet.Id;

                var tajikPage = await _context.TajikPages.AsNoTracking().Where(p => p.Id == page.Id).SingleOrDefaultAsync();
                if (tajikPage == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, "Ин сафҳа ҳануз ба тоҷикии баргардонида нашуда.");

                page.HtmlText = tajikPage.TajikHtmlText;

                var tajikPoet = await _context.TajikPoets.AsNoTracking().Where(p => p.Id == poetId).SingleOrDefaultAsync();
                if (tajikPoet == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, "Ин суханвар ҳануз ба тоҷикӣ дар дастрас нест.");
                if(page.PoetOrCat != null && page.PoetOrCat.Poet != null)
                {
                    page.PoetOrCat.Poet.Nickname = tajikPoet.TajikNickname;
                    page.PoetOrCat.Poet.Description = tajikPoet.TajikDescription;
                }

                if(page.Poem != null && page.Poem.Category != null && page.Poem.Category.Poet != null)
                {
                    page.Poem.Category.Poet.Nickname = tajikPoet.TajikNickname;
                    page.Poem.Category.Poet.Description = tajikPoet.TajikDescription;
                }

                int catId = page.Poem != null ? page.Poem.Category.Cat.Id : page.PoetOrCat.Cat.Id;

                var tajikCat = await _context.TajikCats.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                if (page.Poem != null && page.Poem.Category != null && page.Poem.Category.Cat != null)
                {
                    page.Poem.Category.Cat.Title = tajikCat.TajikTitle;
                    page.Poem.Category.Cat.Description = tajikCat.TajikDescription;
                    foreach (var parent in page.Poem.Category.Cat.Ancestors)
                    {
                        var tajikParent = await _context.TajikCats.AsNoTracking().Where(c => c.Id == parent.Id).SingleAsync();
                        parent.Title = tajikParent.TajikTitle;
                        parent.Description = tajikParent.TajikDescription;
                    }
                    foreach (var child in page.Poem.Category.Cat.Children)
                    {
                        var tajikChild = await _context.TajikCats.AsNoTracking().Where(c => c.Id == child.Id).SingleAsync();
                        child.Title = tajikChild.TajikTitle;
                        child.Description = tajikChild.TajikDescription;
                    }
                }

                if (page.PoetOrCat != null && page.PoetOrCat.Cat != null)
                {
                    page.PoetOrCat.Cat.Title = tajikCat.TajikTitle;
                    page.PoetOrCat.Cat.Description = tajikCat.TajikDescription;
                    foreach (var parent in page.PoetOrCat.Cat.Ancestors)
                    {
                        var tajikParent = await _context.TajikCats.AsNoTracking().Where(c => c.Id == parent.Id).SingleAsync();
                        parent.Title = tajikParent.TajikTitle;
                        parent.Description = tajikParent.TajikDescription;
                    }
                    foreach (var child in page.PoetOrCat.Cat.Children)
                    {
                        var tajikChild = await _context.TajikCats.AsNoTracking().Where(c => c.Id == child.Id).SingleAsync();
                        child.Title = tajikChild.TajikTitle;
                        child.Description = tajikChild.TajikDescription;
                    }
                    if (page.PoetOrCat.Cat.Next != null)
                    {
                        var nextCat = await _context.TajikCats.AsNoTracking().Where(c => c.Id == page.PoetOrCat.Cat.Next.Id).SingleAsync();
                        page.PoetOrCat.Cat.Next.Title = nextCat.TajikTitle;
                    }
                    if (page.PoetOrCat.Cat.Previous != null)
                    {
                        var preCat = await _context.TajikCats.AsNoTracking().Where(c => c.Id == page.PoetOrCat.Cat.Previous.Id).SingleAsync();
                        page.PoetOrCat.Cat.Previous.Title = preCat.TajikTitle;
                    }

                    if (page.PoetOrCat.Cat.Poems != null)
                    {
                        var tajikPoems = await _context.TajikPoems.AsNoTracking().Where(p => p.CatId == catId).ToListAsync();
                        foreach (var poem in page.PoetOrCat.Cat.Poems)
                        {
                            var tajikPoem = tajikPoems.Where(p => p.Id == poem.Id).SingleOrDefault();
                            if (tajikPoem != null)
                            {
                                poem.Title = tajikPoem.TajikTitle;
                            }
                        }
                    }
                }

                if (page.Poem != null)
                {
                    var tajikPoem = await _context.TajikPoems.AsNoTracking().Where(p => p.Id == page.Poem.Id).SingleAsync();
                    page.Poem.Title = tajikPoem.TajikTitle;
                    page.Poem.PlainText = tajikPoem.TajikPlainText;


                    var tajikVerses = await _context.TajikVerses.AsNoTracking().Where(v => v.PoemId ==  page.Poem.Id).OrderBy(v => v.VOrder).ToListAsync();
                    foreach( var verse in page.Poem.Verses)
                    {
                        verse.Text = tajikVerses.Where(v => v.VOrder == verse.VOrder).Single().TajikText;
                    }

                    if (page.Poem.Recitations != null)
                    {
                        // no TajikRecitations table exists - this audio metadata has no real Tajik
                        // translation source anywhere, so transliteration is the only option
                        foreach (var recitation in page.Poem.Recitations)
                        {
                            recitation.AudioTitle = LanguageUtils.TransliteratePersianToTajik(recitation.AudioTitle);
                            recitation.AudioArtist = LanguageUtils.TransliteratePersianToTajik(recitation.AudioArtist);
                        }
                    }

                    if(page.Poem.Next != null)
                    {
                        var nextPoem = await _context.TajikPoems.AsNoTracking().Where(p => p.Id == page.Poem.Next.Id).SingleOrDefaultAsync();
                        if(nextPoem != null)
                        {
                            // real Tajik translation exists for the neighbouring poem - use it
                            page.Poem.Next.Title = nextPoem.TajikTitle;
                            page.Poem.Next.Excerpt = ExtractExcerpt(nextPoem.TajikPlainText);
                        }
                        else
                        {
                            // no Tajik translation for this poem yet - fall back to a simple
                            // best-effort letter transliteration rather than showing raw
                            // Perso-Arabic script (see LanguageUtils.TransliteratePersianToTajik)
                            page.Poem.Next.Title = LanguageUtils.TransliteratePersianToTajik(page.Poem.Next.Title);
                            page.Poem.Next.Excerpt = LanguageUtils.TransliteratePersianToTajik(page.Poem.Next.Excerpt);
                        }
                    }
                    if (page.Poem.Previous != null)
                    {
                        var prePoem = await _context.TajikPoems.AsNoTracking().Where(p => p.Id == page.Poem.Previous.Id).SingleOrDefaultAsync();
                        if (prePoem != null)
                        {
                            page.Poem.Previous.Title = prePoem.TajikTitle;
                            page.Poem.Previous.Excerpt = ExtractExcerpt(prePoem.TajikPlainText);
                        }
                        else
                        {
                            page.Poem.Previous.Title = LanguageUtils.TransliteratePersianToTajik(page.Poem.Previous.Title);
                            page.Poem.Previous.Excerpt = LanguageUtils.TransliteratePersianToTajik(page.Poem.Previous.Excerpt);
                        }
                    }
                }
                return new RServiceResult<GanjoorPageCompleteViewModel>(page);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPageCompleteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// short plain-text excerpt (first non-empty line) for a Tajik poem's next/previous nav link
        /// </summary>
        private static string ExtractExcerpt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return "";
            var firstLine = plainText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return firstLine.Length > 80 ? firstLine.Substring(0, 80) + "…" : firstLine;
        }

        /// <summary>
        /// re-runs the Tajik poet/category HTML generators for every already-imported poet and
        /// category page, overwriting their stored TajikHtmlText. Needed one-time after changing
        /// either generator function (e.g. adding poem excerpts to the table of contents), since
        /// the normal SQLite import skips pages that already exist and never refreshes them.
        /// Runs as a background job (same pattern as TajikImportFromSqlite) since the number of
        /// poets/categories/poems involved can be large enough to exceed a normal request timeout;
        /// progress can be tracked the same way as other long running jobs (e.g. the Admin area's
        /// LongRunningJobs page).
        /// </summary>
        public RServiceResult<bool> RegenerateTajikCatAndPoetHtmlTextAsync()
        {
            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                            (
                            async token =>
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                                {
                                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                    var job = (await jobProgressServiceEF.NewJob("RegenerateTajikCatAndPoetHtmlText", "Query data")).Result;

                                    try
                                    {
                                        var tajikPoets = await context.TajikPoets.AsNoTracking().ToListAsync();
                                        int done = 0;
                                        foreach (var tajikPoet in tajikPoets)
                                        {
                                            await jobProgressServiceEF.UpdateJob(job.Id, done, $"poet: {tajikPoet.Id}");
                                            var poetPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == tajikPoet.Id).SingleOrDefaultAsync();
                                            if (poetPage != null)
                                            {
                                                var tajikPage = await context.TajikPages.Where(p => p.Id == poetPage.Id).SingleOrDefaultAsync();
                                                if (tajikPage != null)
                                                {
                                                    tajikPage.TajikHtmlText = await PrepareTajikPoetHtmlTextAsync(context, tajikPoet);
                                                    await context.SaveChangesAsync();
                                                }
                                            }
                                            done++;
                                        }

                                        var tajikCats = await context.TajikCats.AsNoTracking().ToListAsync();
                                        done = 0;
                                        foreach (var tajikCat in tajikCats)
                                        {
                                            await jobProgressServiceEF.UpdateJob(job.Id, done, $"cat: {tajikCat.Id}");
                                            var tajikPage = await context.TajikPages.Where(p => p.Id == tajikCat.Id).SingleOrDefaultAsync();
                                            if (tajikPage != null)
                                            {
                                                tajikPage.TajikHtmlText = await PrepareTajikCatHtmlTextAsync(context, tajikCat);
                                                await context.SaveChangesAsync();
                                            }
                                            done++;
                                        }

                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                    }
                                    catch (Exception exp)
                                    {
                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                    }
                                }
                            }
                            );
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            return new RServiceResult<bool>(true);
        }



        /// <summary>
        /// Search
        /// You need to run this scripts manually on the database before using this method:
        /// 
        /// CREATE FULLTEXT CATALOG [TajikPoemPlainTextCatalog] WITH ACCENT_SENSITIVITY = OFF AS DEFAULT
        /// 
        /// CREATE FULLTEXT INDEX ON [dbo].[TajikPoems](
        /// [TajikPlainText] LANGUAGE 'English')
        /// KEY INDEX [PK_TajikPoems] ON ([TajikPoemPlainTextCatalog], FILEGROUP [PRIMARY])
        /// WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> SearchTajikAsync(PagingParameterModel paging, string term, int? poetId, int? catId)
        {
            term = term.Trim();

            if (string.IsNullOrEmpty(term))
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>((null, null), "Лутфан иборатиро ворид кунед");
            }


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
                await _populateCategoryChildren(_context, (int)catId, catIdList);
            }

            var source =
                _context.TajikPoems
                .Where(p =>
                        (catId == null || catIdList.Contains(p.CatId))
                        &&
                       EF.Functions.Contains(p.TajikPlainText, searchConditions)
                        )
                .Include(p => p.Cat).ThenInclude(c => c.Poet)
                .OrderBy(p => p.Cat.Poet.BirthYearInLHijri).ThenBy(p => p.Cat.Poet.TajikNickname).ThenBy(p => p.Id)
                .Select
                (
                    poem =>
                    new GanjoorPoemCompleteViewModel()
                    {
                        Id = poem.Id,
                        Title = poem.TajikTitle,
                        FullTitle = poem.FullTitle,
                        FullUrl = poem.FullUrl,
                        PlainText = poem.TajikPlainText,
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

                    var tajikPoet = await _context.TajikPoets.AsNoTracking().Where(p => p.Id == item.Category.Poet.Id).SingleAsync();
                    poet.Poet.Nickname = tajikPoet.TajikNickname;

                    cachedPoets.Add(item.Category.Poet.Id, poet);

                    item.Category = poet;
                }

            }
            return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>(paginatedResult);
        }

    }
}