using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// parse related pages
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ParseRelatedPageAsync(RMuseumDbContext context, int pageId)
        {
            try
            {
                var dbPage = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == pageId).SingleAsync();
                int endIndex = dbPage.HtmlText.IndexOf("در بخش دوم");
                if(endIndex == -1)
                {
                    return new RServiceResult<bool>(false, "endIndex == -1");
                }
                int index = dbPage.HtmlText.IndexOf("<li>");
                while(index != -1 && index < endIndex)
                {
                    int closeTagIndex = dbPage.HtmlText.IndexOf("</li>", index);
                    int tagIndex = dbPage.HtmlText.IndexOf("\"", index);
                    string poem1Url = dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("\"", index + 1) - tagIndex - 1 - 1 /*remove trailing slash*/ );
                    var poem1 = await context.GanjoorPoems.AsNoTracking().Where(p => p.FullUrl == poem1Url).SingleOrDefaultAsync();
                    if(poem1 == null)
                    {
                        var redirectedPage = await context.GanjoorPages.AsNoTracking().Where(p => p.RedirectFromFullUrl == poem1Url).SingleAsync();
                        poem1 = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == redirectedPage.Id).SingleAsync();
                    }
                    var poem1Cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == poem1.CatId).SingleAsync();
                    var poem1Poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poem1Cat.PoetId).SingleAsync();

                    index += poem1Url.Length;
                    tagIndex = dbPage.HtmlText.IndexOf("\"", index);
                    string poem2Url = dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("\"", index + 1) - tagIndex - 1 - 1 /*remove trailing slash*/ );
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
                        RelatedPoemId = poem2.Id,
                        IsPriorToRelated = true,
                        ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem1.Id && p.CachedRelatedPoemPoetUrl == poem2Cat.FullUrl).AnyAsync(),
                        CachedRelatedPoemPoetDeathYearInLHijri = poem2Poet.DeathYearInLHijri,
                        CachedRelatedPoemPoetName = poem2Poet.Name,
                        CachedRelatedPoemPoetUrl = poem2Cat.FullUrl,
                        CachedRelatedPoemPoetImage =  $"/api/ganjoor/poet/image{poem2Cat.FullUrl}.gif",
                        CachedRelatedPoemFullTitle = poem2.FullTitle,
                        CachedRelatedPoemFullUrl = poem2.FullUrl,
                        Description = "",
                        Published = true
                    };

                    //first couplet:
                    relatedPoem.Couplet1PoetName = poem1Poet.Name;
                    tagIndex = dbPage.HtmlText.IndexOf("<p>", tagIndex);
                    if(tagIndex != -1 && tagIndex < closeTagIndex)
                    {
                        tagIndex = dbPage.HtmlText.IndexOf("\">", tagIndex);
                        tagIndex += "\">".Length;
                        relatedPoem.Couplet1Index = -1 + int.Parse(PersianNumbersUtils.ToEnglishNumbers(dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("<", tagIndex) - tagIndex - 1)));
                        
                        tagIndex = dbPage.HtmlText.IndexOf(":", tagIndex) + 1;

                        relatedPoem.Couplet1Verse1 = dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("-", tagIndex) - 1).Replace("\r", "").Replace("\n", "");
                        relatedPoem.Couplet1Verse1IsMainPart = false;
                        if (relatedPoem.Couplet1Verse1.Contains("strong"))
                        {
                            relatedPoem.Couplet1Verse1IsMainPart = true;
                            relatedPoem.Couplet1Verse1 = relatedPoem.Couplet1Verse1.Replace("<strong>", "").Replace("</strong>", "");
                        }

                        relatedPoem.Couplet1Verse2 = dbPage.HtmlText.Substring(dbPage.HtmlText.IndexOf("-", tagIndex), dbPage.HtmlText.IndexOf("</p>", tagIndex) - 1).Replace("\r", "").Replace("\n", "");
                        relatedPoem.Couplet1Verse2IsMainPart = false;
                        if (relatedPoem.Couplet1Verse2.Contains("strong"))
                        {
                            relatedPoem.Couplet1Verse2IsMainPart = true;
                            relatedPoem.Couplet1Verse2 = relatedPoem.Couplet1Verse2.Replace("<strong>", "").Replace("</strong>", "");
                        }
                    }

                    tagIndex = dbPage.HtmlText.IndexOf("<p>", tagIndex);
                    if (tagIndex != -1 && tagIndex < closeTagIndex)
                    {
                        tagIndex = dbPage.HtmlText.IndexOf("\">", tagIndex);
                        tagIndex += "\">".Length;
                        relatedPoem.RelatedCouplet1Index = -1 + int.Parse(PersianNumbersUtils.ToEnglishNumbers(dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("<", tagIndex) - tagIndex - 1)));

                        tagIndex = dbPage.HtmlText.IndexOf(":", tagIndex) + 1;

                        relatedPoem.RelatedCouplet1Verse1 = dbPage.HtmlText.Substring(tagIndex, dbPage.HtmlText.IndexOf("-", tagIndex) - 1).Replace("\r", "").Replace("\n", "");
                        relatedPoem.RelatedCouplet1Verse1IsMainPart = false;
                        if (relatedPoem.RelatedCouplet1Verse1.Contains("strong"))
                        {
                            relatedPoem.RelatedCouplet1Verse1IsMainPart = true;
                            relatedPoem.RelatedCouplet1Verse1 = relatedPoem.RelatedCouplet1Verse1.Replace("<strong>", "").Replace("</strong>", "");
                        }

                        relatedPoem.RelatedCouplet1Verse2 = dbPage.HtmlText.Substring(dbPage.HtmlText.IndexOf("-", tagIndex), dbPage.HtmlText.IndexOf("</p>", tagIndex) - 1).Replace("\r", "").Replace("\n", "");
                        relatedPoem.RelatedCouplet1Verse2IsMainPart = false;
                        if (relatedPoem.RelatedCouplet1Verse2.Contains("strong"))
                        {
                            relatedPoem.RelatedCouplet1Verse2IsMainPart = true;
                            relatedPoem.RelatedCouplet1Verse2 = relatedPoem.RelatedCouplet1Verse2.Replace("<strong>", "").Replace("</strong>", "");
                        }
                    }



                    context.Add(relatedPoem);
                    await context.SaveChangesAsync();


                    GanjoorQuotedPoem reverseRelation = new GanjoorQuotedPoem()
                    {
                        PoemId = poem2.Id,
                        RelatedPoemId = poem1.Id,
                        IsPriorToRelated = false,
                        ChosenForMainList = false == await context.GanjoorQuotedPoems.AsNoTracking().Where(p => p.PoemId == poem2.Id && p.CachedRelatedPoemPoetUrl == poem1Cat.FullUrl).AnyAsync(),
                        CachedRelatedPoemPoetDeathYearInLHijri = poem1Poet.DeathYearInLHijri,
                        CachedRelatedPoemPoetName = poem1Poet.Name,
                        CachedRelatedPoemPoetUrl = poem1Cat.FullUrl,
                        CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{poem1Cat.FullUrl}.gif",
                        CachedRelatedPoemFullTitle = poem1.FullTitle,
                        CachedRelatedPoemFullUrl = poem1.FullUrl,
                        Description = "",
                        Published = true,
                        Couplet1PoetName = relatedPoem.Couplet1PoetName,
                        Couplet1Verse1 = relatedPoem.Couplet1Verse1,
                        Couplet1Verse1IsMainPart = relatedPoem.Couplet1Verse1IsMainPart,
                        Couplet1Verse2 = relatedPoem.Couplet1Verse2,
                        Couplet1Verse2IsMainPart = relatedPoem.Couplet1Verse2IsMainPart,
                        Couplet1Index = relatedPoem.Couplet1Index,
                        RelatedCouplet1PoetName = relatedPoem.RelatedCouplet1PoetName,
                        RelatedCouplet1Verse1 = relatedPoem.RelatedCouplet1Verse1,
                        RelatedCouplet1Verse1IsMainPart = relatedPoem.RelatedCouplet1Verse1IsMainPart,
                        RelatedCouplet1Verse2 = relatedPoem.RelatedCouplet1Verse2,
                        RelatedCouplet1Verse2IsMainPart = relatedPoem.RelatedCouplet1Verse2IsMainPart,
                        RelatedCouplet1Index = relatedPoem.RelatedCouplet1Index,
                    };
                    context.Add(reverseRelation);
                    await context.SaveChangesAsync();

                    index = dbPage.HtmlText.IndexOf("<li>", index);
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
