using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        /// <summary>
        /// build sitemap
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateStatsPage(Guid editingUserId)
        {
            try
            {

                var poetsCoupletCounts =
                                        await _context.GanjoorVerses.Include(v => v.Poem).ThenInclude(p => p.Cat).AsNoTracking()
                                        .Where(v => v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1)
                                        .GroupBy(v => new { v.Poem.Cat.PoetId })
                                        .Select(g => new { PoetId = g.Key.PoetId, Count = g.Count() })
                                        .ToListAsync();

                poetsCoupletCounts.Sort((a, b) => b.Count - a.Count);

                var dbPage = await _context.GanjoorPages.Where(p => p.FullUrl == "/vazn").SingleAsync();

                var poets = await _context.GanjoorPoets.ToListAsync();

                var sum = poetsCoupletCounts.Sum(c => c.Count);

                string htmlText = $"<p>تا تاریخ {LanguageUtils.FormatDate(DateTime.Now)} مجموعاً {LanguageUtils.FormatMoney(sum)} بیت شعر از طریق سایت گنجور در دسترس قرار گرفته است. در جدول زیر با کلیک بر روی نام شاعران -که بر اساس تعداد ابیات اشعار آنها به صورت نزولی مرتب شده- می‌توانید آمار اوزان دیوان آنها را مشاهده کنید.</p>{Environment.NewLine}";

                htmlText += $"<table>{Environment.NewLine}" +
                    $"<tr class=\"h\">{Environment.NewLine}" +
                    $"<td class=\"c1\">ردیف</td>{Environment.NewLine}" +
                    $"<td class=\"c2\">شاعر</td>{Environment.NewLine}" +
                    $"<td class=\"c3\">تعداد ابیات</td>{Environment.NewLine}" +
                    $"<td class=\"c4\">درصد از کل</td>{Environment.NewLine}" +
                    $"</tr>{Environment.NewLine}";

                for (int i = 0; i < poetsCoupletCounts.Count; i++)
                {
                    if(i%2 == 0)
                        htmlText += $"<tr class=\"e\">{Environment.NewLine}";
                    else
                        htmlText += $"<tr>{Environment.NewLine}";

                    htmlText += $"<td class=\"c1\">{(i + 1).ToPersianNumbers()}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"c2\">{poets.Where(p => p.Id == poetsCoupletCounts[i].PoetId).Single().Nickname}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"c3\">{LanguageUtils.FormatMoney(poetsCoupletCounts[i].Count)}</td>{Environment.NewLine}";
                    htmlText += $"<td class=\"c4\">{LanguageUtils.FormatMoney(poetsCoupletCounts[i].Count * 100 / sum)}</td>{Environment.NewLine}";

                    htmlText += $"</tr>{Environment.NewLine}";
                }
                htmlText += $"</table>{Environment.NewLine}";

                await UpdatePageAsync(dbPage.Id, editingUserId,
                 new GanjoorModifyPageViewModel()
                 {
                     Title = dbPage.Title,
                     HtmlText = htmlText,
                     Note = "به روزرسانی خودکار صفحهٔ آمار وزنها",
                     UrlSlug = dbPage.UrlSlug,
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