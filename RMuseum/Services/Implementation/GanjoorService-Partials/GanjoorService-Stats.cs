using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    internal class RhythmCoupletCount
    {
        public int? GanjoorMetreId { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        private async Task _UpdatePageHtmlText(RMuseumDbContext context, Guid userId, GanjoorPage page, string note, string htmlText)
        {
            context.GanjoorPageSnapshots.Add
                           (
                           new GanjoorPageSnapshot()
                           {
                               GanjoorPageId = page.Id,
                               MadeObsoleteByUserId = userId,
                               HtmlText = page.HtmlText,
                               Note = note,
                               RecordDate = DateTime.Now
                           }
                           );
            page.HtmlText = htmlText;
            context.GanjoorPages.Update(page);
            await context.SaveChangesAsync();
        }

        private async Task _UpdatePoetStatsPage(Guid editingUserId, GanjoorPoet poet, List<GanjoorMetre> rhythms, RMuseumDbContext context)
        {
            var wholePoemSections = await context.GanjoorPoemSections.Include(v => v.Poem).ThenInclude(p => p.Cat).ThenInclude(c => c.Poet).AsNoTracking()
                                                .Where(s => s.PoetId == poet.Id && s.Poem.Cat.Poet.Published && s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First)
                                                .Select(s => new { PoemId = s.PoemId, Index = s.Index, GanjoorMetreId = s.GanjoorMetreId })
                                                .ToListAsync();

            Dictionary<int?, int> metreCounts = new Dictionary<int?, int>();
            foreach (var section in wholePoemSections)
            {
                int coupletCount = await context.GanjoorVerses.AsNoTracking()
                    .Where(v =>
                        v.PoemId == section.PoemId
                        &&
                        (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                        &&
                        v.SectionIndex1 == section.Index
                        ).CountAsync();
                var metreId = section.GanjoorMetreId == null ? 0 : (int)section.GanjoorMetreId;
                if (metreCounts.TryGetValue(metreId, out int sectionCoupletCount))
                {
                    sectionCoupletCount += coupletCount;
                }
                else
                {
                    sectionCoupletCount = coupletCount;
                }
                metreCounts[metreId] = sectionCoupletCount;
            }

            List<RhythmCoupletCount> rhythmsCoupletCounts = new List<RhythmCoupletCount>();
            foreach (var metreCount in metreCounts)
            {
                int? metreId = metreCount.Key == 0 ? null : metreCount.Key;
                rhythmsCoupletCounts.Add(new RhythmCoupletCount (){ GanjoorMetreId = metreId, Count = metreCount.Value });
            }
            rhythmsCoupletCounts.Sort((a, b) => b.Count - a.Count);

            /*
            var rhythmsCoupletCounts =
                            await context.GanjoorVerses.Include(v => v.Poem).ThenInclude(p => p.Cat).AsNoTracking()
                            .Where(v => v.Poem.Cat.PoetId == poet.Id && (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1))
                            .GroupBy(v => new { v.Poem.GanjoorMetreId })
                            .Select(g => new { GanjoorMetreId = g.Key.GanjoorMetreId, Count = g.Count() })
                            .ToListAsync();
            rhythmsCoupletCounts.Sort((a, b) => b.Count - a.Count);
            */

            var sumRhythmsCouplets = rhythmsCoupletCounts.Sum(c => c.Count);

            string htmlText = $"<p>این آمار از میان {LanguageUtils.FormatMoney(sumRhythmsCouplets)} بیت شعر موجود در گنجور از {poet.Name} استخراج شده است.</p>{Environment.NewLine}";
            htmlText += $"<p>توجه فرمایید که این آمار به دلایلی از قبیل وجود چند نسخه از آثار شعرا در سایت (مثل آثار خیام) و همینطور یک بیت محسوب شدن مصرع‌های بند قالبهای ترکیبی مثل مخمسها تقریبی و حدودی است و افزونگی دارد.</p>{Environment.NewLine}";
            htmlText += $"<p>آمار همهٔ شعرهای گنجور را <a href=\"/vazn\">اینجا</a> ببینید.</p>{Environment.NewLine}";
            htmlText += $"<p>وزنیابی دستی در بیشتر موارد با ملاحظهٔ تنها یک مصرع از شعر صورت گرفته و امکان وجود اشکال در آن (مخصوصاً اشتباه در تشخیص وزنهای قابل تبدیل از قبیل وزن مثنوی مولوی به جای وزن عروضی سریع مطوی مکشوف) وجود دارد. وزنیابی ماشینی نیز که جدیداً با استفاده از امکانات <a href=\"http://www.sorud.info/\">تارنمای سرود</a> اضافه شده بعضاً خطا دارد. برخی از بخشها شامل اشعاری با بیش از یک وزن هستند که در این صورت عمدتاً وزن ابیات آغازین و برای بعضی منظومه‌ها وزن غالب منظومه به عنوان وزن آن بخش منظور شده است.</p>{Environment.NewLine}";
            htmlText += $"<table>{Environment.NewLine}" +
                                            $"<tr class=\"h\">{Environment.NewLine}" +
                                            $"<td class=\"c1\">ردیف</td>{Environment.NewLine}" +
                                            $"<td class=\"c2\">وزن</td>{Environment.NewLine}" +
                                            $"<td class=\"c3\">تعداد ابیات</td>{Environment.NewLine}" +
                                            $"<td class=\"c4\">درصد از کل</td>{Environment.NewLine}" +
                                            $"</tr>{Environment.NewLine}";

            for (int i = 0; i < rhythmsCoupletCounts.Count; i++)
            {
                if (i % 2 == 0)
                    htmlText += $"<tr class=\"e\">{Environment.NewLine}";
                else
                    htmlText += $"<tr>{Environment.NewLine}";

                htmlText += $"<td class=\"c1\">{(i + 1).ToPersianNumbers()}</td>{Environment.NewLine}";
                string rhythm = rhythmsCoupletCounts[i].GanjoorMetreId == null ? "وزنیابی نشده" :
                                $"<a href=\"/simi/?v={Uri.EscapeDataString(rhythms.Where(r => r.Id == rhythmsCoupletCounts[i].GanjoorMetreId).Single().Rhythm)}&amp;a={poet.Id}\">{rhythms.Where(r => r.Id == rhythmsCoupletCounts[i].GanjoorMetreId).Single().Rhythm}</a>";
                htmlText += $"<td class=\"c2\">{rhythm}</td>{Environment.NewLine}";
                htmlText += $"<td class=\"c3\">{LanguageUtils.FormatMoney(rhythmsCoupletCounts[i].Count)}</td>{Environment.NewLine}";
                htmlText += $"<td class=\"c4\">{(rhythmsCoupletCounts[i].Count * 100.0 / sumRhythmsCouplets).ToString("N2", new CultureInfo("fa-IR")).ToPersianNumbers()}</td>{Environment.NewLine}";

                htmlText += $"</tr>{Environment.NewLine}";
            }
            htmlText += $"</table>{Environment.NewLine}";

            var poetUrl = (await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == poet.Id).SingleAsync()).FullUrl;

            var pageUrl = $"{poetUrl}/vazn";

            var dbPage = await context.GanjoorPages.Where(p => p.FullUrl == pageUrl).SingleOrDefaultAsync();

            if(dbPage != null)
            {
                await _UpdatePageHtmlText(context, editingUserId, dbPage, "به روزرسانی خودکار صفحهٔ آمار وزنها", htmlText);
            }
            else
            {
                var maxPageId = await context.GanjoorPoems.MaxAsync(p => p.Id);
                if (await context.GanjoorPages.MaxAsync(p => p.Id) > maxPageId)
                    maxPageId = await context.GanjoorPages.MaxAsync(p => p.Id);
                var pageId = 1 + maxPageId;
                GanjoorPage dbPoemPage = new GanjoorPage()
                {
                    Id = pageId,
                    GanjoorPageType = GanjoorPageType.ProsodyAndStats,
                    Published = true,
                    PageOrder = -1,
                    Title = "آمار اوزان عروضی",
                    FullTitle = $"{poet.Nickname} » آمار اوزان عروضی",
                    UrlSlug = "vazn",
                    FullUrl = pageUrl,
                    HtmlText = htmlText,
                    PoetId = poet.Id,
                    PostDate = DateTime.Now,
                    ParentId = (await context.GanjoorPages.Where(p => p.FullUrl == poetUrl).SingleAsync()).Id
                };
                context.GanjoorPages.Add(dbPoemPage);
                await context.SaveChangesAsync();
            }


        }

        /// <summary>
        /// start updating stats page
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartUpdatingStatsPage(Guid editingUserId)
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
                                    var job = (await jobProgressServiceEF.NewJob("UpdateStatsPage", "Total Poets Stats")).Result;

                                    try
                                    {
                                        var poetsCoupletCounts =
                                                    await context.GanjoorVerses.Include(v => v.Poem).ThenInclude(p => p.Cat).ThenInclude(c => c.Poet).AsNoTracking()
                                                    .Where(v => 
                                                    v.Poem.Cat.Poet.Published
                                                    &&
                                                    (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1))
                                                    .GroupBy(v => new { v.Poem.Cat.PoetId })
                                                    .Select(g => new { PoetId = g.Key.PoetId, Count = g.Count() })
                                                    .ToListAsync();
                                        poetsCoupletCounts.Sort((a, b) => b.Count - a.Count);
                                        var sumPoetsCouplets = poetsCoupletCounts.Sum(c => c.Count);

                                        await jobProgressServiceEF.UpdateJob(job.Id, 1, "Counting whole sections");

                                        var wholePoemSections = await context.GanjoorPoemSections.Include(v => v.Poem).ThenInclude(p => p.Cat).ThenInclude(c => c.Poet).AsNoTracking()
                                                .Where(s => s.Poem.Cat.Poet.Published && s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First)
                                                .Select(s => new { PoemId = s.PoemId, Index = s.Index, GanjoorMetreId = s.GanjoorMetreId })
                                                .ToListAsync();

                                        Dictionary<int, int> metreCounts = new Dictionary<int, int>();
                                        foreach (var section in wholePoemSections)
                                        {
                                            int coupletCount = await context.GanjoorVerses.AsNoTracking()
                                                .Where(v =>
                                                    v.PoemId == section.PoemId
                                                    &&
                                                    (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                                                    &&
                                                    v.SectionIndex1 == section.Index
                                                    ).CountAsync();
                                            var metreId = section.GanjoorMetreId == null ? 0 : (int)section.GanjoorMetreId;
                                            if (metreCounts.TryGetValue(metreId, out int sectionCoupletCount))
                                            {
                                                sectionCoupletCount += coupletCount;
                                            }
                                            else
                                            {
                                                sectionCoupletCount = coupletCount;
                                            }
                                            metreCounts[metreId] = sectionCoupletCount;
                                        }

                                        List<RhythmCoupletCount> rhythmsCoupletCounts = new List<RhythmCoupletCount>();
                                        foreach (var metreCount in metreCounts)
                                        {
                                            int? metreId = metreCount.Key == 0 ? null : metreCount.Key;
                                            rhythmsCoupletCounts.Add(new RhythmCoupletCount (){ GanjoorMetreId = metreId, Count = metreCount.Value });
                                        }
                                        rhythmsCoupletCounts.Sort((a, b) => b.Count - a.Count);

                                        await jobProgressServiceEF.UpdateJob(job.Id, 2, "Counting whole sections done!");


                                        /*
                                        var rhythmsCoupletCounts =
                                                   await context.GanjoorVerses.Include(v => v.Poem).ThenInclude(p => p.Cat).ThenInclude(c => c.Poet).AsNoTracking()
                                                   .Where(v =>
                                                    v.Poem.Cat.Poet.Published
                                                    &&
                                                    (v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1))
                                                   .GroupBy(v => new { v.Poem.GanjoorMetreId })
                                                   .Select(g => new { GanjoorMetreId = g.Key.GanjoorMetreId, Count = g.Count() })
                                                   .ToListAsync();
                                        rhythmsCoupletCounts.Sort((a, b) => b.Count - a.Count);*/
                                        var sumRhythmsCouplets = rhythmsCoupletCounts.Sum(c => c.Count);

                                        var dbPage = await context.GanjoorPages.Where(p => p.FullUrl == "/vazn").SingleAsync();

                                        var poets = await context.GanjoorPoets.AsNoTracking().ToListAsync();

                                        string htmlText = $"<p>تا تاریخ {LanguageUtils.FormatDate(DateTime.Now)} مجموعاً {LanguageUtils.FormatMoney(sumPoetsCouplets)} بیت شعر از طریق سایت گنجور در دسترس قرار گرفته است. در جدول زیر که شاعران در آنها بر اساس تعداد ابیات اشعارشان به صورت نزولی مرتب شده‌اند با کلیک بر روی نام هر شاعر می‌توانید آمار اوزان اشعار او را مشاهده کنید.</p>{Environment.NewLine}";
                                        htmlText += $"<p>توجه فرمایید که این آمار به دلایلی از قبیل وجود چند نسخه از آثار شعرا در گنجور (مثل آثار خیام)، یک بیت محسوب شدن مصرع‌های بند قالبهای ترکیبی مثل مخمس‌ها و همینطور این که اشعار نقل شده از شاعران دیگر در تذکره‌ها و کتابهایی مانند آن به نام مؤلف نقل‌کنندهٔ شعر ثبت شده تقریبی و حدودی است و افزونگی دارد.</p>{Environment.NewLine}";

                                        htmlText += $"<table>{Environment.NewLine}" +
                                            $"<tr class=\"h\">{Environment.NewLine}" +
                                            $"<td class=\"c1\">ردیف</td>{Environment.NewLine}" +
                                            $"<td class=\"c2\">شاعر</td>{Environment.NewLine}" +
                                            $"<td class=\"c3\">تعداد ابیات</td>{Environment.NewLine}" +
                                            $"<td class=\"c4\">درصد از کل</td>{Environment.NewLine}" +
                                            $"</tr>{Environment.NewLine}";

                                        for (int i = 0; i < poetsCoupletCounts.Count; i++)
                                        {
                                            if (i % 2 == 0)
                                                htmlText += $"<tr class=\"e\">{Environment.NewLine}";
                                            else
                                                htmlText += $"<tr>{Environment.NewLine}";

                                            htmlText += $"<td class=\"c1\">{(i + 1).ToPersianNumbers()}</td>{Environment.NewLine}";
                                            htmlText += $"<td class=\"c2\"><a href=\"{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == poetsCoupletCounts[i].PoetId).SingleAsync()).FullUrl}/vazn\">{poets.Where(p => p.Id == poetsCoupletCounts[i].PoetId).Single().Nickname}</a></td>{Environment.NewLine}";
                                            htmlText += $"<td class=\"c3\">{LanguageUtils.FormatMoney(poetsCoupletCounts[i].Count)}</td>{Environment.NewLine}";
                                            htmlText += $"<td class=\"c4\">{(poetsCoupletCounts[i].Count * 100.0 / sumPoetsCouplets).ToString("N2", new CultureInfo("fa-IR")).ToPersianNumbers()}</td>{Environment.NewLine}";

                                            htmlText += $"</tr>{Environment.NewLine}";
                                        }
                                        htmlText += $"</table>{Environment.NewLine}";

                                       

                                        var rhythms = await context.GanjoorMetres.ToListAsync();

                                        htmlText += $"<p>فهرست زیر نیز آمار اشعار گنجور را از لحاظ اوزان عروضی نشان می‌دهد:</p>{Environment.NewLine}";

                                        htmlText += $"<table>{Environment.NewLine}" +
                                            $"<tr class=\"h\">{Environment.NewLine}" +
                                            $"<td class=\"c1\">ردیف</td>{Environment.NewLine}" +
                                            $"<td class=\"c2\">وزن</td>{Environment.NewLine}" +
                                            $"<td class=\"c3\">تعداد ابیات</td>{Environment.NewLine}" +
                                            $"<td class=\"c4\">درصد از کل</td>{Environment.NewLine}" +
                                            $"</tr>{Environment.NewLine}";

                                        for (int i = 0; i < rhythmsCoupletCounts.Count; i++)
                                        {
                                            if (i % 2 == 0)
                                                htmlText += $"<tr class=\"e\">{Environment.NewLine}";
                                            else
                                                htmlText += $"<tr>{Environment.NewLine}";

                                            htmlText += $"<td class=\"c1\">{(i + 1).ToPersianNumbers()}</td>{Environment.NewLine}";
                                            var rhythm = rhythms.Where(r => r.Id == rhythmsCoupletCounts[i].GanjoorMetreId).SingleOrDefault();
                                            if(rhythm != null)
                                            {
                                                rhythm.VerseCount = rhythmsCoupletCounts[i].Count;
                                                context.Update(rhythm);
                                            }
                                            string rhythmName = rhythmsCoupletCounts[i].GanjoorMetreId == null ? "وزنیابی نشده" :
                                                         $"<a href=\"/simi/?v={Uri.EscapeDataString(rhythm.Rhythm)}\">{rhythms.Where(r => r.Id == rhythmsCoupletCounts[i].GanjoorMetreId).Single().Rhythm}</a>";
                                            htmlText += $"<td class=\"c2\">{rhythmName}</td>{Environment.NewLine}";
                                            htmlText += $"<td class=\"c3\">{LanguageUtils.FormatMoney(rhythmsCoupletCounts[i].Count)}</td>{Environment.NewLine}";
                                            htmlText += $"<td class=\"c4\">{(rhythmsCoupletCounts[i].Count * 100.0 / sumRhythmsCouplets).ToString("N2", new CultureInfo("fa-IR")).ToPersianNumbers()}</td>{Environment.NewLine}";

                                            htmlText += $"</tr>{Environment.NewLine}";
                                        }
                                        htmlText += $"</table>{Environment.NewLine}";

                                        await context.SaveChangesAsync();//store rhythm[s].VerseCount

                                        await _UpdatePageHtmlText(context, editingUserId, dbPage, "به روزرسانی خودکار صفحهٔ آمار وزنها", htmlText);

                                        foreach (var poetInfo in poetsCoupletCounts)
                                        {
                                            var poet = poets.Where(p => p.Id == poetInfo.PoetId).Single();
                                            await jobProgressServiceEF.UpdateJob(job.Id, poetInfo.PoetId, poet.Nickname);
                                            await _UpdatePoetStatsPage(editingUserId, poet, rhythms, context);
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

                
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}