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
                    

                    int anchorIndex = dbPage.HtmlText.IndexOf("\"", index);
                    string poem1Url = dbPage.HtmlText.Substring(anchorIndex, dbPage.HtmlText.IndexOf("\"", index + 1) - anchorIndex - 1 - 1 /*remove trailing slash*/ );
                    var poem1 = await context.GanjoorPoems.AsNoTracking().Where(p => p.FullUrl == poem1Url).SingleOrDefaultAsync();
                    if(poem1 == null)
                    {
                        var redirectedPage = await context.GanjoorPages.AsNoTracking().Where(p => p.RedirectFromFullUrl == poem1Url).SingleAsync();
                        poem1 = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == redirectedPage.Id).SingleAsync();
                    }


                    index += poem1Url.Length;
                    anchorIndex = dbPage.HtmlText.IndexOf("\"", index);
                    string poem2Url = dbPage.HtmlText.Substring(anchorIndex, dbPage.HtmlText.IndexOf("\"", index + 1) - anchorIndex - 1 - 1 /*remove trailing slash*/ );
                    var poem2 = await context.GanjoorPoems.AsNoTracking().Where(p => p.FullUrl == poem2Url).SingleOrDefaultAsync();
                    if (poem2 == null)
                    {
                        var redirectedPage = await context.GanjoorPages.AsNoTracking().Where(p => p.RedirectFromFullUrl == poem2Url).SingleAsync();
                        poem2 = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == redirectedPage.Id).SingleAsync();
                    }

                    var poem2Cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == poem2.CatId).SingleAsync();
                    var poem2Poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poem2Cat.PoetId).SingleAsync();

                    GanjoorRelatedPoem relatedPoem = new GanjoorRelatedPoem()
                    {
                        PoemId = poem1.Id,
                        RelatedPoemId = poem2.Id,
                        IsPriorToRelated = true,
                        RelatedPoemPoetDeathYearInLHijri = poem2Poet.DeathYearInLHijri,
                        RelatePoemFullTitle = poem2.FullTitle,
                        RelatedPoemFullUrl = poem2.FullUrl,
                        Description = "",
                        Published = true
                    };


                    context.Add(relatedPoem);
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
