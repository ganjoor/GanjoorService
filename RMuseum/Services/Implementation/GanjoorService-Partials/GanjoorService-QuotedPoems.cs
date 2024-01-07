using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
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
        /// discover related poems
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="relatedPoetId"></param>
        /// <param name="breakOnFirstSimilar"></param>
        /// <param name="relatedSubCatId"></param>
        /// <param name="insertReverse"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartDiscoverRelatedPoems(int poetId, int relatedPoetId, bool breakOnFirstSimilar, int? relatedSubCatId, bool insertReverse)
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

                                   var job = (await jobProgressServiceEF.NewJob($"StartDiscoverRelatedPoems({poetId}, {relatedPoetId})", "Query data")).Result;

                                   try
                                   {
                                       await _DiscoverRelatedPoemsAsync(context, poetId, relatedPoetId, insertReverse, jobProgressServiceEF, job, breakOnFirstSimilar, relatedSubCatId);

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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
        private static bool AreSimilar(string str1, string str2, bool reverse)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return false;

            string[] words2 = str2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int total = words2.Length;
            int found = 0;
            for (int i = 0; i < total; i++)
            {
                if (str1.IndexOf(words2[i]) != -1)
                    found++;
            }

            if (!reverse)
                return (float)found / total > 0.7f;

            return (float)found / total > 0.7f && AreSimilar(str2, str1, false);
        }

        private async Task<RServiceResult<bool>> _DiscoverRelatedPoemsAsync(RMuseumDbContext context, int poetId, int relatedPoetId, bool insertReverse, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job, bool breakOnFirstSimilar, int? relatedSubCatId)
        {
            DiscoverQuotedQueueItem discoverQuotedQueueItem = await context.DiscoverQuotedQueueItems.Where(i => i.PoetId == poetId && i.RelatedPoetId == relatedPoetId).SingleOrDefaultAsync();
            if (discoverQuotedQueueItem == null)
            {
                discoverQuotedQueueItem = new DiscoverQuotedQueueItem()
                {
                    PoetId = poetId,
                    PoemId = 0,
                    RelatedPoetId = relatedPoetId,
                    RelatedPoemId = 0
                };
                context.Add(discoverQuotedQueueItem);
                await context.SaveChangesAsync();
            }

            var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync();
            var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poetId && c.ParentId == null).SingleAsync();
            var relatedPoet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == relatedPoetId).SingleAsync();
            var relatedPoetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == relatedPoetId && c.ParentId == null).SingleAsync();
            bool isPriorToRelated = poet.DeathYearInLHijri < relatedPoet.DeathYearInLHijri;

            List<int> catIdList = new List<int>();
            if (relatedSubCatId != null)
            {
                catIdList.Add((int)relatedSubCatId);
                await _populateCategoryChildren(context, (int)relatedSubCatId, catIdList);
            }


            var poems = await context.GanjoorPoems.AsNoTracking().Include(p => p.Cat).Where(p => p.Id >= discoverQuotedQueueItem.PoemId && p.Cat.PoetId == poetId).OrderBy(p => p.Id).ToListAsync();
            foreach (var poem in poems)
            {
                discoverQuotedQueueItem.PoemId = poem.Id;
                context.Update(discoverQuotedQueueItem);
                await jobProgressServiceEF.UpdateJob(job.Id, poem.Id);

                var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id
                    && v.VersePosition != VersePosition.Paragraph
                    && v.VersePosition != VersePosition.Single
                    && v.VersePosition != VersePosition.Comment
                    && v.CoupletIndex != null
                    ).OrderBy(v => v.VOrder).ToListAsync();

                var relatedPoems = await context.GanjoorPoems.AsNoTracking().Include(p => p.Cat)
                            .Where(p => p.Id >= discoverQuotedQueueItem.RelatedPoemId && p.Cat.PoetId == relatedPoetId && (relatedSubCatId == null || catIdList.Contains(p.CatId)))
                            .OrderBy(p => p.Id).ToListAsync();
                foreach (var otherPoem in relatedPoems)
                {
                    discoverQuotedQueueItem.RelatedPoemId = otherPoem.Id;
                    context.Update(discoverQuotedQueueItem);
                    await jobProgressServiceEF.UpdateJob(job.Id, poem.Id, otherPoem.Id.ToString());

                    var relatedVerses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == otherPoem.Id
                    && v.VersePosition != VersePosition.Paragraph
                    && v.VersePosition != VersePosition.Single
                    && v.VersePosition != VersePosition.Comment
                    && v.CoupletIndex != null
                    ).OrderBy(v => v.VOrder).ToListAsync();

                    foreach (var verse in verses)
                    {
                        string text = LanguageUtils.MakeTextSearchable(verse.Text);
                        foreach (var relatedVerse in relatedVerses)
                        {
                            if (AreSimilar(text, LanguageUtils.MakeTextSearchable(relatedVerse.Text), true))
                            {
                                var alreadyAdded = await context.GanjoorQuotedPoems.AsNoTracking()
                                            .AnyAsync(q => q.PoemId == poem.Id &&
                                                        q.RelatedPoemId == otherPoem.Id
                                                        &&
                                                        q.CoupletIndex == verse.CoupletIndex
                                                        &&
                                                        q.RelatedCoupletIndex == relatedVerse.CoupletIndex
                                                        );
                                if (alreadyAdded) continue;

                                GanjoorQuotedPoem relatedPoem = new GanjoorQuotedPoem()
                                {
                                    PoemId = poem.Id,
                                    PoetId = poetId,
                                    RelatedPoetId = relatedPoetId,
                                    RelatedPoemId = otherPoem.Id,
                                    IsPriorToRelated = isPriorToRelated,
                                    ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem.Id && p.RelatedPoemId == otherPoem.Id).AnyAsync(),
                                    CachedRelatedPoemPoetDeathYearInLHijri = relatedPoet.DeathYearInLHijri,
                                    CachedRelatedPoemPoetName = relatedPoet.Nickname,
                                    CachedRelatedPoemPoetUrl = relatedPoetCat.FullUrl,
                                    CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{relatedPoetCat.FullUrl}.gif",
                                    CachedRelatedPoemFullTitle = otherPoem.FullTitle,
                                    CachedRelatedPoemFullUrl = otherPoem.FullUrl,
                                    SortOrder = 1000,
                                    Note = "",
                                    Published = false,
                                    ClaimedByBothPoets = false,
                                    IndirectQuotation = false,
                                    SamePoemsQuotedCount = await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem.Id && p.RelatedPoemId == otherPoem.Id).AnyAsync() ?
                                               1 + await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem.Id && p.RelatedPoemId == otherPoem.Id).CountAsync() : 1,
                                    RelatedCoupletVerse1 = relatedVerses.Where(v => v.CoupletIndex == relatedVerse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[0].Text,
                                    RelatedCoupletVerse1ShouldBeEmphasized = relatedVerses.Where(v => v.CoupletIndex == relatedVerse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[0].VOrder == relatedVerse.VOrder,
                                    RelatedCoupletVerse2 = relatedVerses.Where(v => v.CoupletIndex == relatedVerse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[1].Text,
                                    RelatedCoupletVerse2ShouldBeEmphasized = relatedVerses.Where(v => v.CoupletIndex == relatedVerse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[1].VOrder == relatedVerse.VOrder,
                                    RelatedCoupletIndex = relatedVerse.CoupletIndex,
                                    CoupletVerse1 = verses.Where(v => v.CoupletIndex == verse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[0].Text,
                                    CoupletVerse1ShouldBeEmphasized = verses.Where(v => v.CoupletIndex == verse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[0].VOrder == verse.VOrder,
                                    CoupletVerse2 = verses.Where(v => v.CoupletIndex == verse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[1].Text,
                                    CoupletVerse2ShouldBeEmphasized = verses.Where(v => v.CoupletIndex == verse.CoupletIndex).OrderBy(v => v.VOrder).ToArray()[1].VOrder == verse.VOrder,
                                    CoupletIndex = verse.CoupletIndex,
                                };

                                context.Add(relatedPoem);
                                await context.SaveChangesAsync();

                                if (insertReverse)
                                {
                                    var alreadyAdded2 = await context.GanjoorQuotedPoems.AsNoTracking()
                                           .AnyAsync(q => q.PoemId == otherPoem.Id &&
                                                       q.RelatedPoemId == poem.Id
                                                       &&
                                                       q.CoupletIndex == relatedVerse.CoupletIndex
                                                       &&
                                                       q.RelatedCoupletIndex == verse.CoupletIndex
                                                       );
                                    if (alreadyAdded2) continue;

                                    GanjoorQuotedPoem reverseRelation = new GanjoorQuotedPoem()
                                    {
                                        PoemId = otherPoem.Id,
                                        PoetId = relatedPoet.Id,
                                        RelatedPoetId = poet.Id,
                                        RelatedPoemId = poem.Id,
                                        IsPriorToRelated = !relatedPoem.IsPriorToRelated,
                                        ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == otherPoem.Id && p.RelatedPoemId == poem.Id).AnyAsync(),
                                        CachedRelatedPoemPoetDeathYearInLHijri = poet.DeathYearInLHijri,
                                        CachedRelatedPoemPoetName = poet.Nickname,
                                        CachedRelatedPoemPoetUrl = poetCat.FullUrl,
                                        CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{poetCat.FullUrl}.gif",
                                        CachedRelatedPoemFullTitle = poem.FullTitle,
                                        CachedRelatedPoemFullUrl = poem.FullUrl,
                                        SortOrder = 1000,
                                        Note = "",
                                        Published = false,
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
                                        SamePoemsQuotedCount = await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == otherPoem.Id && p.RelatedPoemId == poem.Id).AnyAsync() ?
                                                   1 + await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == otherPoem.Id && p.RelatedPoemId == poem.Id).CountAsync() : 1

                                    };
                                    context.Add(reverseRelation);
                                    await context.SaveChangesAsync();
                                }


                                if (breakOnFirstSimilar)
                                    break;

                            }
                        }
                    }
                }
            }

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// regenerate related poems pages
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartRegeneratingRelatedPoemsPages(Guid editingUserId)
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

                                   var job = (await jobProgressServiceEF.NewJob($"StartRegeneratingRelatedPoemsPagesAsync", "Query data")).Result;

                                   try
                                   {
                                       var pages = await context.GanjoorPages.AsNoTracking().Where(p => p.SecondPoetId != null).ToListAsync();
                                       foreach (var page in pages)
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, page.Id, $"StartRegeneratingRelatedPoemsPageAsync({page.PoetId}, {page.SecondPoetId})");
                                           await _RegenerateRelatedPoemsPageAsync(editingUserId, context, (int)page.PoetId, (int)page.SecondPoetId);
                                       }

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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private RServiceResult<bool> _StartRegeneratingRelatedPoemsPageAsync(Guid editingUserId, int poetId, int relatedPoetId)
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

                                   var job = (await jobProgressServiceEF.NewJob($"StartRegeneratingRelatedPoemsPageAsync({poetId}, {relatedPoetId})", "Query data")).Result;

                                   try
                                   {
                                       await _RegenerateRelatedPoemsPageAsync(editingUserId, context, poetId, relatedPoetId);
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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// parse related pages
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="context"></param>
        /// <param name="poetId"></param>
        /// <param name="relatedPoetId"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _RegenerateRelatedPoemsPageAsync(Guid editingUserId, RMuseumDbContext context, int poetId, int relatedPoetId)
        {
            var page = await context.GanjoorPages.AsNoTracking().Where(p => p.PoetId == poetId && p.SecondPoetId == relatedPoetId).SingleOrDefaultAsync();
            if (page == null)
            {
                return new RServiceResult<bool>(false, $"No page for {poetId} - {relatedPoetId}");
            }

            var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == page.PoetId).SingleAsync();

            string html = "";

            var claimedByBothList = await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoetId == poetId && p.RelatedPoetId == relatedPoetId && p.ClaimedByBothPoets && p.Published).OrderBy(p => p.PoemId).ThenBy(p => p.RelatedPoemId).ToListAsync();
            if (claimedByBothList.Any())
            {
                html += $"<p>فهرست زیر شامل اشعاری است که در نسخه‌های دیوان‌های هر دو سخنور آمده است و به هر دو منتسب است:</p>{Environment.NewLine}";
                html += $"<br style=\"clear:both;\">{Environment.NewLine}";
                html += $"<ol>{Environment.NewLine}";
                foreach (var quotedPoem in claimedByBothList)
                {
                    var poem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == quotedPoem.PoemId).SingleAsync();
                    html += $"<li>{Environment.NewLine}";
                    html += $"<h3>{Environment.NewLine}";
                    html += $"<a href=\"{poem.FullUrl}\">{poem.FullTitle}</a> :: <a href=\"{quotedPoem.CachedRelatedPoemFullUrl}\">{quotedPoem.CachedRelatedPoemFullTitle}</a>{Environment.NewLine}";
                    html += $"</h3>{Environment.NewLine}";

                    html += $"<p>{poet.Nickname} (بیت {(quotedPoem.CoupletIndex + 1).ToPersianNumbers()}): ";
                    if (quotedPoem.CoupletVerse1ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.CoupletVerse1}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.CoupletVerse1;
                    }
                    html += " - ";
                    if (quotedPoem.CoupletVerse2ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.CoupletVerse2}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.CoupletVerse2;
                    }
                    html += $"</p>{Environment.NewLine}";

                    html += $"<p>{quotedPoem.CachedRelatedPoemPoetName} (بیت {(quotedPoem.RelatedCoupletIndex + 1).ToPersianNumbers()}): ";
                    if (quotedPoem.RelatedCoupletVerse1ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.RelatedCoupletVerse1}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.RelatedCoupletVerse1;
                    }
                    html += " - ";
                    if (quotedPoem.RelatedCoupletVerse2ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.RelatedCoupletVerse2}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.RelatedCoupletVerse2;
                    }
                    html += $"</p>{Environment.NewLine}";

                    if (!string.IsNullOrEmpty(quotedPoem.Note))
                    {
                        html += $"<div class=\"notice\">{quotedPoem.Note}</div>{Environment.NewLine}";
                    }

                    html += $"</li>{Environment.NewLine}";
                }
                html += $"</ol>{Environment.NewLine}";
                html += $"<br style=\"clear:both;\">{Environment.NewLine}";
            }

            var relatedPoet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == page.SecondPoetId).SingleAsync();

            var normalRelatedPoems = await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoetId == poetId && p.RelatedPoetId == relatedPoetId && p.ClaimedByBothPoets == false && p.Published).OrderBy(p => p.PoemId).ThenBy(p => p.CoupletIndex).ToListAsync();
            if (normalRelatedPoems.Any())
            {
                html += $"<p>در این بخش شعرهایی را فهرست کرده‌ایم که در آنها {poet.Nickname} مصرع یا بیتی از {relatedPoet.Nickname} را عیناً نقل قول کرده است:</p>{Environment.NewLine}";
                html += $"<br style=\"clear:both;\">{Environment.NewLine}";
                html += $"<ol>{Environment.NewLine}";
                foreach (var quotedPoem in normalRelatedPoems)
                {
                    var poem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == quotedPoem.PoemId).SingleAsync();
                    html += $"<li>{Environment.NewLine}";
                    html += $"<h3>{Environment.NewLine}";
                    html += $"<a href=\"{poem.FullUrl}\">{poem.FullTitle}</a> :: <a href=\"{quotedPoem.CachedRelatedPoemFullUrl}\">{quotedPoem.CachedRelatedPoemFullTitle}</a>{Environment.NewLine}";
                    html += $"</h3>{Environment.NewLine}";

                    html += $"<p>{poet.Nickname} (بیت {(quotedPoem.CoupletIndex + 1).ToPersianNumbers()}): ";
                    if (quotedPoem.CoupletVerse1ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.CoupletVerse1}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.CoupletVerse1;
                    }
                    html += " - ";
                    if (quotedPoem.CoupletVerse2ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.CoupletVerse2}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.CoupletVerse2;
                    }
                    html += $"</p>{Environment.NewLine}";

                    html += $"<p>{quotedPoem.CachedRelatedPoemPoetName} (بیت {(quotedPoem.RelatedCoupletIndex + 1).ToPersianNumbers()}): ";
                    if (quotedPoem.RelatedCoupletVerse1ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.RelatedCoupletVerse1}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.RelatedCoupletVerse1;
                    }
                    html += " - ";
                    if (quotedPoem.RelatedCoupletVerse2ShouldBeEmphasized)
                    {
                        html += $"<strong>{quotedPoem.RelatedCoupletVerse2}</strong>";
                    }
                    else
                    {
                        html += quotedPoem.RelatedCoupletVerse2;
                    }
                    html += $"</p>{Environment.NewLine}";

                    if (!string.IsNullOrEmpty(quotedPoem.Note))
                    {
                        html += $"<div class=\"notice\">{quotedPoem.Note}</div>{Environment.NewLine}";
                    }

                    html += $"</li>{Environment.NewLine}";
                }
                html += $"</ol>{Environment.NewLine}";
                html += $"<br style=\"clear:both;\">{Environment.NewLine}";

                var poemSections = await context.GanjoorPoemSections.AsNoTracking()
                                .Include(s => s.Poem).ThenInclude(p => p.Cat)
                                .Include(s => s.GanjoorMetre)
                                .Where(s => s.SectionType == PoemSectionType.WholePoem && s.Poem.Cat.PoetId == poetId && s.GanjoorMetreId != null && !string.IsNullOrEmpty(s.RhymeLetters))
                                .OrderBy(s => s.PoemId)
                                .ToListAsync();
                if (poemSections.Any())
                {
                    var relatedPoemSections = await context.GanjoorPoemSections.AsNoTracking()
                             .Include(s => s.Poem).ThenInclude(p => p.Cat)
                             .Include(s => s.GanjoorMetre)
                             .Where(s => s.SectionType == PoemSectionType.WholePoem && s.Poem.Cat.PoetId == relatedPoetId && s.GanjoorMetreId != null && !string.IsNullOrEmpty(s.RhymeLetters))
                             .OrderBy(s => s.PoemId)
                             .ToListAsync();

                    if (relatedPoemSections.Any())
                    {
                        Dictionary<(int, string), (List<GanjoorPoemSection>, List<GanjoorPoemSection>)> list = [];

                        foreach (var poemSection in poemSections)
                        {
                            var relatedPoemList = relatedPoemSections.Where(s => s.GanjoorMetreId == poemSection.GanjoorMetreId && s.RhymeLetters == poemSection.RhymeLetters).ToList();
                            if (!relatedPoemList.Any())
                            {
                                continue;
                            }
                            if (!list.TryGetValue(((int)poemSection.GanjoorMetreId, poemSection.RhymeLetters), out (List<GanjoorPoemSection>, List<GanjoorPoemSection>) poemsList))
                            {
                                poemsList = (new List<GanjoorPoemSection>(), relatedPoemList);
                                list.Add
                                    (
                                    ((int)poemSection.GanjoorMetreId, poemSection.RhymeLetters), poemsList
                                    );
                            }
                            poemsList.Item1.Add(poemSection);
                        }

                        if (list.Any())
                        {
                            html += $"<hr />{Environment.NewLine}";
                            html += $"<p>در این بخش مجموعه شعرهایی از دو شاعر را که توأماً هموزن و همقافیه هستند در گروه‌های مجزا فهرست کرده‌ایم: </p>{Environment.NewLine}";
                            html += $"<br style=\"clear:both;\">{Environment.NewLine}";

                            html += $"<ol>{Environment.NewLine}";
                            foreach (var pair in list.Values)
                            {
                                html += $"<li>{Environment.NewLine}";
                                foreach (var section in pair.Item1)
                                {
                                    var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == section.PoemId && v.SectionIndex1 == section.Index).OrderBy(v => v.VOrder).Take(2).ToListAsync();
                                    html += $"<p><a href=\"{section.Poem.FullUrl}\">{section.Poem.FullTitle}</a>:{verses[0].Text} - {verses[1].Text}</p>{Environment.NewLine}";
                                }
                                html += $"<br style=\"clear:both;\">{Environment.NewLine}";
                                foreach (var section in pair.Item2)
                                {
                                    var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == section.PoemId && v.SectionIndex1 == section.Index).OrderBy(v => v.VOrder).Take(2).ToListAsync();
                                    html += $"<p><a href=\"{section.Poem.FullUrl}\">{section.Poem.FullTitle}</a>:{verses[0].Text} - {verses[1].Text}</p>{Environment.NewLine}";
                                }
                                html += $"<hr />{Environment.NewLine}";
                                html += $"</li>{Environment.NewLine}";
                            }
                            html += $"</ol>{Environment.NewLine}";
                            html += $"<br style=\"clear:both;\">{Environment.NewLine}";
                        }
                    }
                }



                await _UpdatePageAsync(context, page.Id, editingUserId,
                    new GanjoorModifyPageViewModel()
                    {
                        Title = page.Title,
                        HtmlText = html,
                        Note = $"_RegenerateRelatedPoemsPageAsync({poetId}, {relatedPoetId})",
                        UrlSlug = page.UrlSlug,
                        NoIndex = page.NoIndex,
                    }
                    , false);
            }

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// get quoted by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoemViewModel>> GetGanjoorQuotedPoemByIdAsync(Guid id)
        {
            try
            {
                return new RServiceResult<GanjoorQuotedPoemViewModel>(
                    new GanjoorQuotedPoemViewModel(
                    await _context.GanjoorQuotedPoems.AsNoTracking().Where(q => q.Id == id).SingleAsync()
                    ));

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoemViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// insert quoted poem
        /// </summary>
        /// <param name="quoted"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoemViewModel>> InsertGanjoorQuotedPoemAsync(GanjoorQuotedPoemViewModel quoted, Guid editingUserId)
        {
            try
            {
                if (await _context.GanjoorQuotedPoems.AsNoTracking()
                                        .Where(q =>
                                                    q.PoemId == quoted.PoemId
                                                    &&
                                                    q.RelatedPoemId != null && q.RelatedPoemId == quoted.RelatedPoemId
                                                    &&
                                                    q.CoupletIndex == quoted.CoupletIndex
                                                    &&
                                                    q.RelatedCoupletIndex != null && q.RelatedCoupletIndex == quoted.RelatedCoupletIndex
                                                    ).AnyAsync())
                {
                    return new RServiceResult<GanjoorQuotedPoemViewModel>(null, "نقل قول تکراری");
                }

                GanjoorQuotedPoem dbQuoted = new GanjoorQuotedPoem()
                {
                    Id = quoted.Id,
                    PoemId = quoted.PoemId,
                    Poem = quoted.Poem,
                    RelatedPoemId = quoted.RelatedPoemId,
                    IsPriorToRelated = quoted.IsPriorToRelated,
                    ChosenForMainList = quoted.ChosenForMainList,
                    SortOrder = quoted.SortOrder,
                    CachedRelatedPoemPoetDeathYearInLHijri = quoted.CachedRelatedPoemPoetDeathYearInLHijri,
                    CachedRelatedPoemPoetName = quoted.CachedRelatedPoemPoetName,
                    CachedRelatedPoemPoetUrl = quoted.CachedRelatedPoemPoetUrl,
                    CachedRelatedPoemPoetImage = quoted.CachedRelatedPoemPoetImage,
                    CachedRelatedPoemFullTitle = quoted.CachedRelatedPoemFullTitle,
                    CachedRelatedPoemFullUrl = quoted.CachedRelatedPoemFullUrl,
                    CoupletVerse1 = quoted.CoupletVerse1,
                    CoupletVerse1ShouldBeEmphasized = quoted.CoupletVerse1ShouldBeEmphasized,
                    CoupletVerse2 = quoted.CoupletVerse2,
                    CoupletVerse2ShouldBeEmphasized = quoted.CoupletVerse2ShouldBeEmphasized,
                    CoupletIndex = quoted.CoupletIndex,
                    RelatedCoupletVerse1 = quoted.RelatedCoupletVerse1,
                    RelatedCoupletVerse1ShouldBeEmphasized = quoted.RelatedCoupletVerse1ShouldBeEmphasized,
                    RelatedCoupletVerse2 = quoted.RelatedCoupletVerse2,
                    RelatedCoupletVerse2ShouldBeEmphasized = quoted.RelatedCoupletVerse2ShouldBeEmphasized,
                    RelatedCoupletIndex = quoted.RelatedCoupletIndex,
                    Note = quoted.Note,
                    Published = quoted.Published,
                    SamePoemsQuotedCount = quoted.SamePoemsQuotedCount,
                    ClaimedByBothPoets = quoted.ClaimedByBothPoets,
                    PoetId = quoted.PoetId,
                    RelatedPoetId = quoted.RelatedPoetId,
                    IndirectQuotation = quoted.IndirectQuotation,
                    SuggestedById = editingUserId,
                };

                _context.Add(dbQuoted);
                await _context.SaveChangesAsync();

                if (dbQuoted.ClaimedByBothPoets)
                {
                    var poem = await _context.GanjoorPoems.Where(p => p.Id == dbQuoted.PoemId).SingleAsync();
                    if (poem.ClaimedByMultiplePoets == false)
                    {
                        poem.ClaimedByMultiplePoets = true;
                        _context.Update(poem);
                        await _context.SaveChangesAsync();
                    }
                }

                var allRelateds = await _context.GanjoorQuotedPoems.Where(p => p.PoemId == dbQuoted.PoemId && p.RelatedPoemId == dbQuoted.RelatedPoemId && p.Published).ToListAsync();
                foreach (var rel in allRelateds)
                {
                    rel.SamePoemsQuotedCount = allRelateds.Count;
                    _context.Update(rel);
                    await _context.SaveChangesAsync();
                }

                if (dbQuoted.Published && dbQuoted.RelatedPoetId != null)
                {
                    var page = await _context.GanjoorPages.AsNoTracking().Where(p => p.PoetId == dbQuoted.PoetId && p.SecondPoetId == dbQuoted.RelatedPoetId).SingleOrDefaultAsync();
                    if (page != null)
                    {
                        _StartRegeneratingRelatedPoemsPageAsync(editingUserId, dbQuoted.PoetId, (int)dbQuoted.RelatedPoetId);
                    }
                }

                return new RServiceResult<GanjoorQuotedPoemViewModel>(new GanjoorQuotedPoemViewModel(dbQuoted));

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoemViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// update quoted poem
        /// </summary>
        /// <param name="quoted"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateGanjoorQuotedPoemsAsync(GanjoorQuotedPoemViewModel quoted, Guid editingUserId)
        {
            try
            {
                var dbModel = await _context.GanjoorQuotedPoems.Where(q => q.Id == quoted.Id).SingleAsync();
                if (dbModel.PoemId != quoted.PoemId || dbModel.RelatedPoemId != quoted.RelatedPoemId)
                {
                    return new RServiceResult<bool>(false, "dbModel.PoemId !=  quoted.PoemId || dbModel.RelatedPoemId != quoted.RelatedPoemId");
                }
                dbModel.PoetId = quoted.PoetId;
                dbModel.RelatedPoetId = quoted.RelatedPoetId;
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

                var poem = await _context.GanjoorPoems.Where(p => p.Id == quoted.PoemId).SingleAsync();
                bool claimedByMultiple = await _context.GanjoorQuotedPoems.AsNoTracking().Where(q => q.PoemId == quoted.PoemId && q.ClaimedByBothPoets == true).AnyAsync();
                if (poem.ClaimedByMultiplePoets != claimedByMultiple)
                {
                    poem.ClaimedByMultiplePoets = claimedByMultiple;
                    _context.Update(poem);
                    await _context.SaveChangesAsync();
                }

                await _context.SaveChangesAsync();

                var allRelateds = await _context.GanjoorQuotedPoems.Where(p => p.PoemId == quoted.PoemId && p.RelatedPoemId == quoted.RelatedPoemId && p.Published).ToListAsync();
                foreach (var rel in allRelateds)
                {
                    rel.SamePoemsQuotedCount = allRelateds.Count;
                    _context.Update(rel);
                    await _context.SaveChangesAsync();
                }

                if (quoted.Published && quoted.RelatedPoetId != null)
                {
                    var page = await _context.GanjoorPages.AsNoTracking().Where(p => p.PoetId == quoted.PoetId && p.SecondPoetId == quoted.RelatedPoetId).SingleOrDefaultAsync();
                    if (page != null)
                    {
                        _StartRegeneratingRelatedPoemsPageAsync(editingUserId, quoted.PoetId, (int)quoted.RelatedPoetId);
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
        /// delete quoted by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="editingUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteGanjoorQuotedPoemByIdAsync(Guid id, Guid editingUserId)
        {
            try
            {
                var q = await _context.GanjoorQuotedPoems.Where(q => q.Id == id).SingleAsync();
                var poemId = q.PoemId;
                var poetId = q.PoetId;
                var relatedPoetId = q.RelatedPoetId;
                var relatedPoemId = q.RelatedPoemId;
                var published = q.Published;
                _context.Remove(q);
                await _context.SaveChangesAsync();

                var poem = await _context.GanjoorPoems.Where(p => p.Id == poemId).SingleAsync();
                bool claimedByMultiple = await _context.GanjoorQuotedPoems.AsNoTracking().Where(q => q.PoemId == poemId && q.ClaimedByBothPoets == true).AnyAsync();
                if (poem.ClaimedByMultiplePoets != claimedByMultiple)
                {
                    poem.ClaimedByMultiplePoets = claimedByMultiple;
                    _context.Update(poem);
                    await _context.SaveChangesAsync();
                }

                var allRelateds = await _context.GanjoorQuotedPoems.Where(p => p.PoemId == poemId && p.RelatedPoemId == relatedPoetId && p.Published).ToListAsync();
                foreach (var rel in allRelateds)
                {
                    rel.SamePoemsQuotedCount = allRelateds.Count;
                    _context.Update(rel);
                    await _context.SaveChangesAsync();
                }

                if (published && relatedPoetId != null)
                {
                    var page = await _context.GanjoorPages.AsNoTracking().Where(p => p.PoetId == poetId && p.SecondPoetId == relatedPoetId).SingleOrDefaultAsync();
                    if (page != null)
                    {
                        _StartRegeneratingRelatedPoemsPageAsync(editingUserId, poetId, (int)relatedPoetId);
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
        /// get quoted poems
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="relatedPoetId"></param>
        /// <param name="chosen"></param>
        /// <param name="published"></param>
        /// <param name="claimed"></param>
        /// <param name="indirect"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoemViewModel[]>> GetGanjoorQuotedPoemsAsync(int? poetId, int? relatedPoetId, bool? chosen, bool? published, bool? claimed, bool? indirect)
        {
            try
            {
                var source = await
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

                        .ToArrayAsync();
                return new RServiceResult<GanjoorQuotedPoemViewModel[]>(
                    source.Select(r => new GanjoorQuotedPoemViewModel(r)).ToArray()
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoemViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get quoted poems for a poem
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="skip"></param>
        /// <param name="itemsCount"></param>
        /// <param name="onlyClaimedByBothPoets"></param>
        /// <param name="published"></param>
        /// <param name="chosenForMainList"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoemViewModel[]>> GetGanjoorQuotedPoemsForPoemAsync(int poemId, int skip, int itemsCount, bool? onlyClaimedByBothPoets, bool? published, bool? chosenForMainList)
        {
            try
            {
                var source =
                _context.GanjoorQuotedPoems
                         .AsNoTracking()
                        .Where(r => r.PoemId == poemId
                                && (chosenForMainList == null || r.ChosenForMainList == chosenForMainList)
                                && (onlyClaimedByBothPoets == null || r.ClaimedByBothPoets == onlyClaimedByBothPoets)
                                && (published == null || r.Published == published)
                                )
                        .OrderBy(r => r.SortOrder).ThenBy(r => r.CachedRelatedPoemPoetDeathYearInLHijri);
                var selection = itemsCount <= 0 ? await source.ToArrayAsync() : await source.Skip(skip).Take(itemsCount).ToArrayAsync();
                return new RServiceResult<GanjoorQuotedPoemViewModel[]>
                    (
                        selection.Select(r => new GanjoorQuotedPoemViewModel(r)).ToArray()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoemViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// two poems quoted records
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="relatedPoemId"></param>
        /// <param name="published"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorQuotedPoemViewModel[]>> GetGanjoorQuotedPoemsForRelatedAsync(int poemId, int relatedPoemId, bool? published)
        {
            try
            {
                var source = await _context.GanjoorQuotedPoems
                         .AsNoTracking()
                        .Where(r => r.PoemId == poemId && r.RelatedPoemId == relatedPoemId && (published == null || r.Published == published))
                        .OrderBy(r => r.SortOrder).ThenBy(r => r.CachedRelatedPoemPoetDeathYearInLHijri)
                        .ToArrayAsync();
                return new RServiceResult<GanjoorQuotedPoemViewModel[]>
                (
                    source.Select(r => new GanjoorQuotedPoemViewModel(r))
                        .ToArray()
               );


            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorQuotedPoemViewModel[]>(null, exp.ToString());
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
                    var tempTagIndex = dbPage.HtmlText.IndexOf("\"", tagIndex);
                    if (tempTagIndex < closeTagIndex)
                    {
                        tagIndex = tempTagIndex;
                    }


                    string poem2Url = dbPage.HtmlText.Substring("https://ganjoor.net".Length + tagIndex + 1, dbPage.HtmlText.IndexOf("\"", tagIndex + 1) - tagIndex - 1 - 1 /*remove trailing slash*/  - "https://ganjoor.net".Length);
                    var poem2 = await context.GanjoorPoems.AsNoTracking().Where(p => p.FullUrl == poem2Url).SingleOrDefaultAsync();
                    if (poem2 == null)
                    {
                        var redirectedPage = await context.GanjoorPages.AsNoTracking().Where(p => p.RedirectFromFullUrl == poem2Url).SingleOrDefaultAsync();
                        if (redirectedPage == null)
                        {
                            return new RServiceResult<bool>(false, poem2Url);
                        }
                        poem2 = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == redirectedPage.Id).SingleAsync();
                    }

                    var poem2Cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == poem2.CatId).SingleAsync();
                    var poem2Poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poem2Cat.PoetId).SingleAsync();
                    var poet2Cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poem2Poet.Id && c.ParentId == null).SingleAsync();

                    GanjoorQuotedPoem relatedPoem = new GanjoorQuotedPoem()
                    {
                        PoemId = poem1.Id,
                        PoetId = poem1Poet.Id,
                        RelatedPoetId = poem2Poet.Id,
                        RelatedPoemId = poem2.Id,
                        IsPriorToRelated = dbPage.Id != 33166 && dbPage.Id != 39321,
                        ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem1.Id && p.RelatedPoemId == poem2.Id).AnyAsync(),
                        CachedRelatedPoemPoetDeathYearInLHijri = poem2Poet.DeathYearInLHijri,
                        CachedRelatedPoemPoetName = poem2Poet.Nickname,
                        CachedRelatedPoemPoetUrl = poet2Cat.FullUrl,
                        CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{poet2Cat.FullUrl}.gif",
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
                        string searchString = dbPage.Id == 39619 ? "بیت" : "\">";
                        tagIndex = dbPage.HtmlText.IndexOf(searchString, tagIndex);
                        tagIndex += searchString.Length;
                        string closeChar = dbPage.Id == 39619 ? ")" : "<";
                        string bnumstring = PersianNumbersUtils.ToEnglishNumbers(dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf(closeChar, tagIndex) - tagIndex)).Replace("بیت", "").Replace(")", "").Replace(":", "").Trim();
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
                        string searchString = dbPage.Id == 39619 ? "بیت " : "\">";
                        tagIndex = dbPage.HtmlText.IndexOf(searchString, tagIndex);
                        tagIndex += searchString.Length;
                        string closeChar = dbPage.Id == 39619 ? ")" : "<";
                        string bnumstring = PersianNumbersUtils.ToEnglishNumbers(dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf(closeChar, tagIndex) - tagIndex)).Replace("بیت", "").Replace(")", "").Replace(":", "").Trim();
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

                    if (relatedPoem.SamePoemsQuotedCount > 1)
                    {
                        var allRelateds = await context.GanjoorQuotedPoems.Where(p => p.Id != relatedPoem.Id && p.PoemId == relatedPoem.PoemId && p.RelatedPoemId == relatedPoem.RelatedPoemId).ToListAsync();
                        foreach (var rel in allRelateds)
                        {
                            rel.SamePoemsQuotedCount = relatedPoem.SamePoemsQuotedCount;
                        }
                        context.UpdateRange(allRelateds);
                        await context.SaveChangesAsync();
                    }

                    var poet1Cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poem1Poet.Id && c.ParentId == null).SingleAsync();
                    GanjoorQuotedPoem reverseRelation = new GanjoorQuotedPoem()
                    {
                        PoemId = poem2.Id,
                        PoetId = poem2Poet.Id,
                        RelatedPoetId = poem1Poet.Id,
                        RelatedPoemId = poem1.Id,
                        IsPriorToRelated = !relatedPoem.IsPriorToRelated,
                        ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem2.Id && p.RelatedPoemId == poem1.Id).AnyAsync(),
                        CachedRelatedPoemPoetDeathYearInLHijri = poem1Poet.DeathYearInLHijri,
                        CachedRelatedPoemPoetName = poem1Poet.Nickname,
                        CachedRelatedPoemPoetUrl = poet1Cat.FullUrl,
                        CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{poet1Cat.FullUrl}.gif",
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

                    if (reverseRelation.SamePoemsQuotedCount > 1)
                    {
                        var allRelateds = await context.GanjoorQuotedPoems.Where(p => p.Id != reverseRelation.Id && p.PoemId == reverseRelation.PoemId && p.RelatedPoemId == reverseRelation.RelatedPoemId).ToListAsync();
                        foreach (var rel in allRelateds)
                        {
                            rel.SamePoemsQuotedCount = reverseRelation.SamePoemsQuotedCount;
                        }
                        context.UpdateRange(allRelateds);
                        await context.SaveChangesAsync();
                    }

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
