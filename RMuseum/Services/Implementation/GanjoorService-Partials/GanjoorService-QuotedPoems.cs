using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// insert quoted poem
        /// </summary>
        /// <param name="quoted"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoem>> InsertGanjoorQuotedPoemAsync(GanjoorQuotedPoem quoted)
        {
            try
            {
                _context.Add(quoted);
                await _context.SaveChangesAsync();
                return new RServiceResult<GanjoorQuotedPoem>(quoted);

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoem>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update quoted poem
        /// </summary>
        /// <param name="quoted"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateGanjoorQuotedPoemsAsync(GanjoorQuotedPoem quoted)
        {
            try
            {
                var dbModel = await _context.GanjoorQuotedPoems.Where(q => q.Id == quoted.Id).SingleAsync();
                dbModel.PoemId = quoted.PoemId;
                dbModel.PoetId = quoted.PoetId;
                dbModel.RelatedPoetId = quoted.RelatedPoetId;
                dbModel.RelatedPoemId = quoted.RelatedPoemId;
                dbModel.IsPriorToRelated = quoted.IsPriorToRelated;
                dbModel.ChosenForMainList = quoted.ChosenForMainList;
                dbModel.CachedRelatedPoemPoetDeathYearInLHijri = quoted.CachedRelatedPoemPoetDeathYearInLHijri;
                dbModel.CachedRelatedPoemPoetName = quoted.CachedRelatedPoemPoetName;
                dbModel.CachedRelatedPoemPoetUrl = quoted.CachedRelatedPoemPoetUrl;
                dbModel.CachedRelatedPoemPoetImage = quoted.CachedRelatedPoemPoetImage;
                dbModel.CachedRelatedPoemFullTitle = quoted.CachedRelatedPoemFullTitle;
                dbModel.CachedRelatedPoemFullUrl = quoted.CachedRelatedPoemFullUrl;
                dbModel.SortOrder = quoted.SortOrder;
                dbModel.Note = quoted.Note;
                dbModel.Published = quoted.Published;
                dbModel.RelatedCoupletVerse1 = quoted.RelatedCoupletVerse1;
                dbModel.RelatedCoupletVerse1ShouldBeEmphasized = quoted.RelatedCoupletVerse1ShouldBeEmphasized;
                dbModel.RelatedCoupletVerse2 = quoted.RelatedCoupletVerse2;
                dbModel.RelatedCoupletVerse2ShouldBeEmphasized = quoted.RelatedCoupletVerse2ShouldBeEmphasized;
                dbModel.RelatedCoupletIndex = quoted.RelatedCoupletIndex;
                dbModel.CoupletVerse1 = quoted.CoupletVerse1;
                dbModel.CoupletVerse1ShouldBeEmphasized = quoted.CoupletVerse1ShouldBeEmphasized;
                dbModel.CoupletVerse2 = quoted.CoupletVerse2;
                dbModel.CoupletVerse2ShouldBeEmphasized = quoted.CoupletVerse2ShouldBeEmphasized;
                dbModel.CoupletIndex = quoted.CoupletIndex;
                dbModel.ClaimedByBothPoets = quoted.ClaimedByBothPoets;
                dbModel.IndirectQuotation = quoted.IndirectQuotation;
                dbModel.SamePoemsQuotedCount = quoted.SamePoemsQuotedCount;

                _context.Update(dbModel);   
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get quoted poems
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="relatedPoetId"></param>
        /// <param name="chosen"></param>
        /// <param name="published"></param>
        /// <param name="claimed"></param>
        /// <param name="indirect"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoem[]>> GetGanjoorQuotedPoemsAsync(int? poetId, int? relatedPoetId, bool? chosen, bool? published, bool? claimed, bool? indirect)
        {
            try
            {
                return new RServiceResult<GanjoorQuotedPoem[]>(await
                _context.GanjoorQuotedPoems
                         .AsNoTracking()
                        .Where(r =>
                        (poetId == null || r.PoetId == poetId)
                        &&
                        (chosen == null || r.ChosenForMainList == chosen)
                        &&
                        (relatedPoetId == null || r.RelatedPoetId == relatedPoetId)
                        &&
                        (published == null || r.Published == published)
                        &&
                        (claimed == null || r.ClaimedByBothPoets == claimed)
                        &&
                        (indirect == null || r.IndirectQuotation == indirect)
                        )
                        .OrderBy(r => r.PoetId).ThenBy(r => r.PoemId).ThenBy(r => r.SortOrder).ThenBy(r => r.CachedRelatedPoemPoetDeathYearInLHijri)
                        .ToArrayAsync()
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoem[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get quoted poems for a poem
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="skip"></param>
        /// <param name="itemsCount"></param>
        /// <param name="onlyClaimedByBothPoets"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoem[]>> GetGanjoorQuotedPoemsForPoemAsync(int poemId, int skip, int itemsCount, bool onlyClaimedByBothPoets)
        {
            try
            {
                var source =
                _context.GanjoorQuotedPoems
                         .AsNoTracking()
                        .Where(r => r.PoemId == poemId && r.ChosenForMainList == true
                                && (!onlyClaimedByBothPoets || r.ClaimedByBothPoets == true))
                        .OrderBy(r => r.SortOrder).ThenBy(r => r.CachedRelatedPoemPoetDeathYearInLHijri);

                if (itemsCount <= 0)
                    return new RServiceResult<GanjoorQuotedPoem[]>(await source.ToArrayAsync());
                return new RServiceResult<GanjoorQuotedPoem[]>
                    (
                    await source.Skip(skip).Take(itemsCount).ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoem[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// two poems quoted records
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="relatedPoemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoem[]>> GetGanjoorQuotedPoemsForRelatedAsync(int poemId, int relatedPoemId)
        {
            try
            {
                return new RServiceResult<GanjoorQuotedPoem[]>
                (
                    await _context.GanjoorQuotedPoems
                         .AsNoTracking()
                        .Where(r => r.PoemId == poemId && r.RelatedPoemId == relatedPoemId)
                        .OrderBy(r => r.SortOrder).ThenBy(r => r.CachedRelatedPoemPoetDeathYearInLHijri).ToArrayAsync()
                        );


            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoem[]>(null, exp.ToString());
            }
        }


        /// <summary>
        /// extracting quoted poems
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartExtractingQuotedPoems()
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

                                   var job = (await jobProgressServiceEF.NewJob("StartExtractingQuotedPoems", "Query data")).Result;

                                   var pages = await context.GanjoorPages.AsNoTracking().Where(p => p.SecondPoetId != null).ToListAsync();
                                   foreach (var page in pages)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 0, page.FullTitle);
                                       var res = await _ParseRelatedPageAsync(context, page.Id);
                                       if (!string.IsNullOrEmpty(res.ExceptionString))
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, 100, page.FullTitle, false, res.ExceptionString);
                                           return;
                                       }

                                   }
                                   await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                               }
                           });
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// parse related pages
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pageId"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _ParseRelatedPageAsync(RMuseumDbContext context, int pageId)
        {
            try
            {
                var dbPage = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == pageId).SingleAsync();
                int endIndex = dbPage.HtmlText.IndexOf("در بخش دوم");
                if (endIndex == -1)
                {
                    return new RServiceResult<bool>(false, "endIndex == -1");
                }
                int index = dbPage.HtmlText.IndexOf("<li>");
                while (index != -1 && index < endIndex)
                {
                    int closeTagIndex = dbPage.HtmlText.IndexOf("</li>", index);
                    int tagIndex = dbPage.HtmlText.IndexOf("\"", index);
                    string poem1Url = dbPage.HtmlText.Substring("https://ganjoor.net".Length + tagIndex + 1, dbPage.HtmlText.IndexOf("\"", tagIndex + 1) - tagIndex - 1 - 1  /*remove trailing slash*/ - "https://ganjoor.net".Length);
                    var poem1 = await context.GanjoorPoems.AsNoTracking().Where(p => p.FullUrl == poem1Url).SingleOrDefaultAsync();
                    if (poem1 == null)
                    {
                        var redirectedPage = await context.GanjoorPages.AsNoTracking().Where(p => p.RedirectFromFullUrl == poem1Url).SingleAsync();
                        poem1 = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == redirectedPage.Id).SingleAsync();
                    }
                    var poem1Cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == poem1.CatId).SingleAsync();
                    var poem1Poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poem1Cat.PoetId).SingleAsync();


                    tagIndex = dbPage.HtmlText.IndexOf("</a>", tagIndex);
                    tagIndex = dbPage.HtmlText.IndexOf("\"", tagIndex);
                    string poem2Url = dbPage.HtmlText.Substring("https://ganjoor.net".Length + tagIndex + 1, dbPage.HtmlText.IndexOf("\"", tagIndex + 1) - tagIndex - 1 - 1 /*remove trailing slash*/  - "https://ganjoor.net".Length);
                    var poem2 = await context.GanjoorPoems.AsNoTracking().Where(p => p.FullUrl == poem2Url).SingleOrDefaultAsync();
                    if (poem2 == null)
                    {
                        var redirectedPage = await context.GanjoorPages.AsNoTracking().Where(p => p.RedirectFromFullUrl == poem2Url).SingleAsync();
                        poem2 = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == redirectedPage.Id).SingleAsync();
                    }

                    var poem2Cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == poem2.CatId).SingleAsync();
                    var poem2Poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poem2Cat.PoetId).SingleAsync();

                    GanjoorQuotedPoem relatedPoem = new GanjoorQuotedPoem()
                    {
                        PoemId = poem1.Id,
                        PoetId = poem1Poet.Id,
                        RelatedPoetId = poem2Poet.Id,
                        RelatedPoemId = poem2.Id,
                        IsPriorToRelated = false,
                        ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem1.Id && p.RelatedPoemId == poem2.Id).AnyAsync(),
                        CachedRelatedPoemPoetDeathYearInLHijri = poem2Poet.DeathYearInLHijri,
                        CachedRelatedPoemPoetName = poem2Poet.Nickname,
                        CachedRelatedPoemPoetUrl = poem2Cat.FullUrl,
                        CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{poem2Cat.FullUrl}.gif",
                        CachedRelatedPoemFullTitle = poem2.FullTitle,
                        CachedRelatedPoemFullUrl = poem2.FullUrl,
                        SortOrder = 1000,
                        Note = "",
                        Published = true,
                        ClaimedByBothPoets = false,
                        IndirectQuotation = false,
                        SamePoemsQuotedCount = await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem1.Id && p.RelatedPoemId == poem2.Id).AnyAsync() ?
                                                1 + await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem1.Id && p.RelatedPoemId == poem2.Id).CountAsync() : 1
                    };

                    //first couplet:
                    tagIndex = dbPage.HtmlText.IndexOf("<p>", tagIndex);
                    if (tagIndex != -1 && tagIndex < closeTagIndex)
                    {
                        tagIndex = dbPage.HtmlText.IndexOf("\">", tagIndex);
                        tagIndex += "\">".Length;
                        string bnumstring = PersianNumbersUtils.ToEnglishNumbers(dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("<", tagIndex) - tagIndex)).Replace("بیت", "").Trim();
                        if (int.TryParse(bnumstring, out int bnum))
                        {
                            relatedPoem.CoupletIndex = -1 + bnum;
                        }
                        else
                        {
                            int? b = null;
                            switch (bnumstring)
                            {
                                case "آخر":
                                    b = 1 + await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem1.Id).MaxAsync(v => v.CoupletIndex);
                                    break;
                                case "آغازین":
                                case "اول":
                                    b = 1;
                                    break;
                                case "دوم":
                                    b = 2;
                                    break;
                                case "سوم":
                                    b = 3;
                                    break;
                                case "چهارم":
                                    b = 4;
                                    break;
                                case "پنجم":
                                    b = 5;
                                    break;
                                case "ششم":
                                    b = 6;
                                    break;
                                case "هفتم":
                                    b = 7;
                                    break;
                                case "هشتم":
                                    b = 8;
                                    break;
                                case "نهم":
                                    b = 9;
                                    break;
                                case "سی و پنجم":
                                    b = 35;
                                    break;
                            }
                            if (b != null)
                            {
                                relatedPoem.CoupletIndex = (int)b - 1;
                            }
                        }


                        tagIndex = dbPage.HtmlText.IndexOf(":", tagIndex) + 1;

                        relatedPoem.CoupletVerse1 = dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("-", tagIndex) - 1 - tagIndex).Trim().Replace("\r", "").Replace("\n", "");
                        relatedPoem.CoupletVerse1ShouldBeEmphasized = false;
                        if (relatedPoem.CoupletVerse1.Contains("strong"))
                        {
                            relatedPoem.CoupletVerse1ShouldBeEmphasized = true;
                            relatedPoem.CoupletVerse1 = relatedPoem.CoupletVerse1.Replace("<strong>", "").Replace("</strong>", "").Trim();
                        }

                        relatedPoem.CoupletVerse2 = dbPage.HtmlText.Substring(dbPage.HtmlText.IndexOf("-", tagIndex) + 1, dbPage.HtmlText.IndexOf("</p>", tagIndex) - dbPage.HtmlText.IndexOf("-", tagIndex) - 1).Trim().Replace("\r", "").Replace("\n", "");
                        relatedPoem.CoupletVerse2ShouldBeEmphasized = false;
                        if (relatedPoem.CoupletVerse2.Contains("strong"))
                        {
                            relatedPoem.CoupletVerse2ShouldBeEmphasized = true;
                            relatedPoem.CoupletVerse2 = relatedPoem.CoupletVerse2.Replace("<strong>", "").Replace("</strong>", "").Trim();
                        }
                    }

                    tagIndex = dbPage.HtmlText.IndexOf("<p>", tagIndex);
                    if (tagIndex != -1 && tagIndex < closeTagIndex)
                    {
                        tagIndex = dbPage.HtmlText.IndexOf("\">", tagIndex);
                        tagIndex += "\">".Length;
                        string bnumstring = PersianNumbersUtils.ToEnglishNumbers(dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("<", tagIndex) - tagIndex)).Replace("بیت", "").Trim();
                        if (int.TryParse(bnumstring, out int bnum))
                        {
                            relatedPoem.RelatedCoupletIndex = -1 + bnum;
                        }
                        else
                        {
                            int? b = null;
                            switch (bnumstring)
                            {
                                case "آخر":
                                    b = 1 + await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem1.Id).MaxAsync(v => v.CoupletIndex);
                                    break;
                                case "آغازین":
                                case "اول":
                                    b = 1;
                                    break;
                                case "دوم":
                                    b = 2;
                                    break;
                                case "سوم":
                                    b = 3;
                                    break;
                                case "چهارم":
                                    b = 4;
                                    break;
                                case "پنجم":
                                    b = 5;
                                    break;
                                case "ششم":
                                    b = 6;
                                    break;
                                case "هفتم":
                                    b = 7;
                                    break;
                                case "هشتم":
                                    b = 8;
                                    break;
                                case "نهم":
                                    b = 9;
                                    break;
                                case "سی و پنجم":
                                    b = 35;
                                    break;
                            }
                            if (b != null)
                            {
                                relatedPoem.RelatedCoupletIndex = (int)b - 1;
                            }
                        }


                        tagIndex = dbPage.HtmlText.IndexOf(":", tagIndex) + 1;

                        relatedPoem.RelatedCoupletVerse1 = dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("-", tagIndex) - 1 - tagIndex).Trim().Replace("\r", "").Replace("\n", "");
                        relatedPoem.RelatedCoupletVerse1ShouldBeEmphasized = false;
                        if (relatedPoem.RelatedCoupletVerse1.Contains("strong"))
                        {
                            relatedPoem.RelatedCoupletVerse1ShouldBeEmphasized = true;
                            relatedPoem.RelatedCoupletVerse1 = relatedPoem.RelatedCoupletVerse1.Replace("<strong>", "").Replace("</strong>", "").Trim();
                        }

                        relatedPoem.RelatedCoupletVerse2 = dbPage.HtmlText.Substring(dbPage.HtmlText.IndexOf("-", tagIndex) + 1, dbPage.HtmlText.IndexOf("</p>", tagIndex) - dbPage.HtmlText.IndexOf("-", tagIndex) - 1).Trim().Replace("\r", "").Replace("\n", "");
                        relatedPoem.RelatedCoupletVerse2ShouldBeEmphasized = false;
                        if (relatedPoem.RelatedCoupletVerse2.Contains("strong"))
                        {
                            relatedPoem.RelatedCoupletVerse2ShouldBeEmphasized = true;
                            relatedPoem.RelatedCoupletVerse2 = relatedPoem.RelatedCoupletVerse2.Replace("<strong>", "").Replace("</strong>", "").Trim();
                        }
                    }



                    context.Add(relatedPoem);
                    await context.SaveChangesAsync();


                    GanjoorQuotedPoem reverseRelation = new GanjoorQuotedPoem()
                    {
                        PoemId = poem2.Id,
                        PoetId = poem2Poet.Id,
                        RelatedPoetId = poem1Poet.Id,
                        RelatedPoemId = poem1.Id,
                        IsPriorToRelated = true,
                        ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem2.Id && p.RelatedPoemId == poem1.Id).AnyAsync(),
                        CachedRelatedPoemPoetDeathYearInLHijri = poem1Poet.DeathYearInLHijri,
                        CachedRelatedPoemPoetName = poem1Poet.Nickname,
                        CachedRelatedPoemPoetUrl = poem1Cat.FullUrl,
                        CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{poem1Cat.FullUrl}.gif",
                        CachedRelatedPoemFullTitle = poem1.FullTitle,
                        CachedRelatedPoemFullUrl = poem1.FullUrl,
                        SortOrder = 1000,
                        Note = "",
                        Published = true,
                        RelatedCoupletVerse1 = relatedPoem.CoupletVerse1,
                        RelatedCoupletVerse1ShouldBeEmphasized = relatedPoem.CoupletVerse1ShouldBeEmphasized,
                        RelatedCoupletVerse2 = relatedPoem.CoupletVerse2,
                        RelatedCoupletVerse2ShouldBeEmphasized = relatedPoem.CoupletVerse2ShouldBeEmphasized,
                        RelatedCoupletIndex = relatedPoem.CoupletIndex,
                        CoupletVerse1 = relatedPoem.RelatedCoupletVerse1,
                        CoupletVerse1ShouldBeEmphasized = relatedPoem.RelatedCoupletVerse1ShouldBeEmphasized,
                        CoupletVerse2 = relatedPoem.RelatedCoupletVerse2,
                        CoupletVerse2ShouldBeEmphasized = relatedPoem.RelatedCoupletVerse2ShouldBeEmphasized,
                        CoupletIndex = relatedPoem.RelatedCoupletIndex,
                        ClaimedByBothPoets = false,
                        IndirectQuotation = false,
                        SamePoemsQuotedCount = await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem2.Id && p.RelatedPoemId == poem1.Id).AnyAsync() ?
                                                1 + await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem2.Id && p.RelatedPoemId == poem1.Id).CountAsync() : 1

                    };
                    context.Add(reverseRelation);
                    await context.SaveChangesAsync();

                    index = dbPage.HtmlText.IndexOf("<li>", tagIndex);
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
