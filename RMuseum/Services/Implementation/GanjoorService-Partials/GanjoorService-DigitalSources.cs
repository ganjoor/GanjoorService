using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;
using DNTPersianUtils.Core;
using System.Globalization;
using System.Threading.Tasks;
using RSecurityBackend.Models.Generic;
using System.Security.Cryptography.X509Certificates;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// get digital source from tag
        /// </summary>
        /// <param name="sourceUrlSlug"></param>
        /// <returns></returns>
        public async Task<RServiceResult<DigitalSource>> GetDigitalSourceFromTagAsync(string sourceUrlSlug)
        {
            try
            {
                return new RServiceResult<DigitalSource>(await _context.DigitalSources.AsNoTracking().Where(d => d.UrlSlug == sourceUrlSlug).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<DigitalSource>(null, exp.ToString());
            }
        }
        /// <summary>
        /// tag with sources
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="source"></param>
        public void TagCategoryWithSource(int catId, DigitalSource source)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob($"TagCategoryWithSource(catId: {catId} - source:{source.UrlSlug})", "Query data")).Result;
                                   try
                                   {
                                       var pages = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl.StartsWith("/sources/")).ToListAsync();
                                       foreach (var page in pages)
                                       {
                                           if (false == await context.DigitalSources.Where(d => d.UrlSlug == page.UrlSlug).AnyAsync())
                                           {
                                               string shortName = page.Title;
                                               switch (page.UrlSlug)
                                               {
                                                   case "wikidorj":
                                                       shortName = "ویکی‌درج";
                                                       break;
                                                   case "frankfurt":
                                                       shortName = "دانشگاه فرانکفورت";
                                                       break;
                                                   case "tariqmo":
                                                       shortName = "طریق التحقیق دکتر مؤذنی";
                                                       break;
                                                   case "tebyan":
                                                       shortName = "تبیان";
                                                       break;
                                               }
                                               context.DigitalSources.Add
                                               (
                                                   new DigitalSource()
                                                   {
                                                       UrlSlug = page.UrlSlug,
                                                       ShortName = shortName,
                                                       FullName = page.Title,
                                                       SourceType = "",
                                                       CoupletsCount = 0,
                                                   }
                                               );
                                               await context.SaveChangesAsync();
                                           }
                                       }

                                       var digitalSource = await context.DigitalSources.Where(p => p.UrlSlug == source.UrlSlug).SingleOrDefaultAsync();
                                       if (digitalSource == null)
                                       {
                                           context.DigitalSources.Add
                                               (
                                                   new DigitalSource()
                                                   {
                                                       UrlSlug = source.UrlSlug,
                                                       ShortName = source.ShortName,
                                                       FullName = source.FullName,
                                                       SourceType = source.SourceType,
                                                       CoupletsCount = 0,
                                                   }
                                               );

                                           var parentPage = await context.GanjoorPages.AsNoTracking().Where(p => p.UrlSlug == "sources").SingleAsync();
                                           var newPageId = 1 + await context.GanjoorPages.MaxAsync(p => p.Id);
                                           while (await context.GanjoorPoems.Where(p => p.Id == newPageId).AnyAsync())
                                               newPageId++;
                                           context.GanjoorPages.Add
                                           (
                                               new GanjoorPage()
                                               {
                                                   Id = newPageId,
                                                   GanjoorPageType = GanjoorPageType.None,
                                                   Published = true,
                                                   PageOrder = -1,
                                                   Title = source.FullName,
                                                   FullTitle = $"{parentPage.FullTitle} » {source.FullName}",
                                                   FullUrl = $"{parentPage.FullUrl}/{source.UrlSlug}",
                                                   UrlSlug = source.UrlSlug,
                                                   HtmlText = $"<p>{source.FullName}</p>",
                                                   PostDate = DateTime.Now,
                                                   ParentId = parentPage.Id,
                                               }
                                           );
                                           
                                           await context.SaveChangesAsync();
                                           digitalSource = await context.DigitalSources.Where(p => p.UrlSlug == source.UrlSlug).SingleAsync();
                                       }

                                       List<int> catIdList = new List<int>
                                       {
                                           catId
                                       };
                                       await _populateCategoryChildren(context, catId, catIdList);
                                       int poemCount = 0;
                                       int progress = 0;
                                       foreach (int catId in catIdList)
                                       {
                                           var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).ToListAsync();
                                           poemCount += poems.Count;
                                           foreach (var poem in poems)
                                           {
                                               poem.SourceName = digitalSource.ShortName;
                                               poem.SourceUrlSlug = source.UrlSlug;
                                               context.Update(poem);

                                               int coupletCount = await context.GanjoorVerses.AsNoTracking()
                                                .Where(v =>
                                                    v.PoemId == poem.Id
                                                    &&
                                                    (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                                                    ).CountAsync();
                                               digitalSource.CoupletsCount += coupletCount;
                                               await jobProgressServiceEF.UpdateJob(job.Id, progress, $"{progress} از {poemCount}");
                                           }
                                       }

                                       context.Update(digitalSource);
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
        /// update digital sources stats
        /// </summary>
        /// <param name="editingUserId"></param>
        public void UpdateDigitalSourcesStats(Guid editingUserId)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob($"UpdateDigitalSourcesStats", "Query data")).Result;
                                   try
                                   {
                                       var pages = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl.StartsWith("/sources/")).ToListAsync();
                                       foreach (var page in pages)
                                       {
                                           if (false == await context.DigitalSources.Where(d => d.UrlSlug == page.UrlSlug).AnyAsync())
                                           {
                                               string shortName = page.Title;
                                               switch (page.UrlSlug)
                                               {
                                                   case "wikidorj":
                                                       shortName = "ویکی‌درج";
                                                       break;
                                                   case "frankfurt":
                                                       shortName = "دانشگاه فرانکفورت";
                                                       break;
                                                   case "tariqmo":
                                                       shortName = "طریق التحقیق دکتر مؤذنی";
                                                       break;
                                                   case "tebyan":
                                                       shortName = "تبیان";
                                                       break;
                                               }
                                               context.DigitalSources.Add
                                               (
                                                   new DigitalSource()
                                                   {
                                                       UrlSlug = page.UrlSlug,
                                                       ShortName = shortName,
                                                       FullName = page.Title,
                                                       SourceType = "",
                                                       CoupletsCount = 0,
                                                   }
                                               );
                                               await context.SaveChangesAsync();
                                           }
                                       }

                                       var digitalSources = await context.DigitalSources.ToArrayAsync();
                                       int totalCoupletsCount = 0;
                                       foreach (var digitalSource in digitalSources)
                                       {
                                           digitalSource.CoupletsCount = 0;
                                           var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.SourceUrlSlug == digitalSource.UrlSlug).ToArrayAsync();
                                           foreach (var poem in poems)
                                           {
                                               int coupletCount = await context.GanjoorVerses.AsNoTracking()
                                                .Where(v =>
                                                    v.PoemId == poem.Id
                                                    &&
                                                    (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                                                    ).CountAsync();
                                               digitalSource.CoupletsCount += coupletCount;
                                           }
                                           totalCoupletsCount += digitalSource.CoupletsCount;
                                           context.Update(digitalSource);
                                           await jobProgressServiceEF.UpdateJob(job.Id, 50, digitalSource.FullName);
                                       }

                                       var noSourceUrlPoems = await context.GanjoorPoems.AsNoTracking().Where(p => string.IsNullOrEmpty(p.SourceUrlSlug)).ToArrayAsync();
                                       int untaggaed = 0;
                                       foreach (var noSourceUrlPoem in noSourceUrlPoems)
                                       {
                                           int coupletCount = await context.GanjoorVerses.AsNoTracking()
                                            .Where(v =>
                                                v.PoemId == noSourceUrlPoem.Id
                                                &&
                                                (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                                                ).CountAsync();
                                           totalCoupletsCount += coupletCount;
                                           untaggaed += coupletCount;
                                       }

                                       //now rebuild /sources page contents
                                       await jobProgressServiceEF.UpdateJob(job.Id, 75, "Rebuild Sources Page");

                                       var sources = await context.DigitalSources.AsNoTracking().Where(d => d.CoupletsCount > 0).OrderByDescending(d => d.CoupletsCount).ToListAsync();

                                       int totalCount = sources.Sum(s => s.CoupletsCount);
                                       string htmlText = $"<p>بخش عمدهٔ محتوای گنجور که در حال حاضر شامل {LanguageUtils.FormatMoney(totalCount)} بیت شعر است از نرم‌افزارها و وبگاه‌های دیگر وارد شده است و در این زمینه علاقمندان ادبیات مدیون بزرگوارانی هستند که پیش و بیش از گنجور روی دیجیتالی کردن میراث ادب ایران‌زمین سرمایه‌گذاری کرده‌اند. آمار زیر نشان دهندهٔ ترکیب محتوای گنجور بر اساس نوع منبع دیجیتالی شدن آنهاست.</p>{Environment.NewLine}";
                                       htmlText += $"<table>{Environment.NewLine}" +
                                            $"<tr class=\"h\">{Environment.NewLine}" +
                                            $"<td class=\"c1\">ردیف</td>{Environment.NewLine}" +
                                            $"<td class=\"c2\">نوع منبع</td>{Environment.NewLine}" +
                                            $"<td class=\"c3\">تعداد ابیات</td>{Environment.NewLine}" +
                                            $"<td class=\"c4\">درصد از کل</td>{Environment.NewLine}" +
                                            $"</tr>{Environment.NewLine}";
                                       var sourcesByType = await context.DigitalSources.AsNoTracking().Where(d => d.CoupletsCount > 0).GroupBy(d => d.SourceType).Select(d => new { SourceType = d.Key, CoupletsCount = d.Sum(s => s.CoupletsCount) }).OrderByDescending(d => d.CoupletsCount).ToListAsync();
                                       for (int i = 0; i < sourcesByType.Count; i++)
                                       {
                                           if (i % 2 == 0)
                                               htmlText += $"<tr class=\"e\">{Environment.NewLine}";
                                           else
                                               htmlText += $"<tr>{Environment.NewLine}";

                                           htmlText += $"<td class=\"c1\">{(i + 1).ToPersianNumbers()}</td>{Environment.NewLine}";
                                           htmlText += $"<td class=\"c2\">{sourcesByType[i].SourceType}</td>{Environment.NewLine}";
                                           htmlText += $"<td class=\"c3\">{LanguageUtils.FormatMoney(sourcesByType[i].CoupletsCount)}</td>{Environment.NewLine}";
                                           htmlText += $"<td class=\"c4\">{(sourcesByType[i].CoupletsCount * 100.0 / totalCount).ToString("N2", new CultureInfo("fa-IR")).ToPersianNumbers()}</td>{Environment.NewLine}";

                                           htmlText += $"</tr>{Environment.NewLine}";
                                       }
                                       htmlText += $"</table>{Environment.NewLine}";

                                       htmlText += $"<p>جدول زیر نشان‌دهندهٔ آمار ریز منابع دیجیتال گنجور است:</p>{Environment.NewLine}";
                                       htmlText += $"<table>{Environment.NewLine}" +
                                            $"<tr class=\"h\">{Environment.NewLine}" +
                                            $"<td class=\"c1\">ردیف</td>{Environment.NewLine}" +
                                            $"<td class=\"c2\">نام منبع</td>{Environment.NewLine}" +
                                            $"<td class=\"c3\">تعداد ابیات</td>{Environment.NewLine}" +
                                            $"<td class=\"c4\">درصد از کل</td>{Environment.NewLine}" +
                                            $"</tr>{Environment.NewLine}";
                                       
                                       for (int i = 0; i < sources.Count; i++)
                                       {
                                           if (i % 2 == 0)
                                               htmlText += $"<tr class=\"e\">{Environment.NewLine}";
                                           else
                                               htmlText += $"<tr>{Environment.NewLine}";

                                           htmlText += $"<td class=\"c1\">{(i + 1).ToPersianNumbers()}</td>{Environment.NewLine}";
                                           htmlText += $"<td class=\"c2\"><a href=\"/sources/{sources[i].UrlSlug}\">{sources[i].FullName}</a></td>{Environment.NewLine}";
                                           htmlText += $"<td class=\"c3\">{LanguageUtils.FormatMoney(sources[i].CoupletsCount)}</td>{Environment.NewLine}";
                                           htmlText += $"<td class=\"c4\">{(sources[i].CoupletsCount * 100.0 / totalCount).ToString("N2", new CultureInfo("fa-IR")).ToPersianNumbers()}</td>{Environment.NewLine}";

                                           htmlText += $"</tr>{Environment.NewLine}";
                                       }
                                       htmlText += $"</table>{Environment.NewLine}";

                                       var dbPage = await context.GanjoorPages.Where(p => p.FullUrl == "/sources").SingleAsync();
                                       await _UpdatePageHtmlText(context, editingUserId, dbPage, "به روزرسانی خودکار صفحهٔ آمار منابع", htmlText);


                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, $"total: {totalCoupletsCount} - untagged: {untaggaed}", true);
                                   }
                                   catch (Exception exp)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                   }

                               }
                           });
        }
    }


}