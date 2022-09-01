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

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        private async Task<RServiceResult<GanjoorPageCompleteViewModel>> _UpdatePoemAsync(RMuseumDbContext context, int id, Guid editingUserId, GanjoorModifyPageViewModel pageData, bool needsReturn)
        {
            try
            {
                var dbPage = await context.GanjoorPages.Where(p => p.Id == id).SingleOrDefaultAsync();
                if (dbPage == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null);//not found
                if (dbPage.GanjoorPageType != GanjoorPageType.PoemPage)
                {
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, "از _UpdatePageAsync استفاده کنید.");
                }
                dbPage.NoIndex = pageData.NoIndex;
                dbPage.RedirectFromFullUrl = string.IsNullOrEmpty(pageData.RedirectFromFullUrl) ? null : pageData.RedirectFromFullUrl;
                context.GanjoorPages.Update(dbPage);

                var dbPoem = await context.GanjoorPoems.Where(p => p.Id == id).SingleOrDefaultAsync();
                dbPoem.MixedModeOrder = pageData.MixedModeOrder;
                context.Update(dbPoem);

                await context.SaveChangesAsync();
                CacheCleanForPageByUrl(dbPage.FullUrl);

                if (needsReturn)
                {
                    return await GetPageByUrl(dbPage.FullUrl);
                }
                return new RServiceResult<GanjoorPageCompleteViewModel>(null);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPageCompleteViewModel>(null, exp.ToString());
            }
        }
        private async Task<RServiceResult<GanjoorPageCompleteViewModel>> _UpdatePageAsync(RMuseumDbContext context, int id, Guid editingUserId, GanjoorModifyPageViewModel pageData, bool needsReturn)
        {
            try
            {
                var dbPage = await context.GanjoorPages.Where(p => p.Id == id).SingleOrDefaultAsync();
                if (dbPage == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null);//not found

                if (dbPage.GanjoorPageType == GanjoorPageType.PoemPage)
                {
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null, "به‌روزرسانی متن شعر از طریق _UpdatePageAsync غیرفعال شده است.");
                }


                GanjoorPageSnapshot snapshot = new GanjoorPageSnapshot()
                {
                    GanjoorPageId = id,
                    MadeObsoleteByUserId = editingUserId,
                    RecordDate = DateTime.Now,
                    Note = pageData.Note,
                    Title = dbPage.Title,
                    UrlSlug = dbPage.UrlSlug,
                    HtmlText = dbPage.HtmlText,
                };


                context.GanjoorPageSnapshots.Add(snapshot);
                await context.SaveChangesAsync();

                dbPage.HtmlText = pageData.HtmlText;
                dbPage.NoIndex = pageData.NoIndex;
                dbPage.RedirectFromFullUrl = string.IsNullOrEmpty(pageData.RedirectFromFullUrl) ? null : pageData.RedirectFromFullUrl;
                bool messWithTitles = dbPage.Title != pageData.Title;
                bool messWithUrls = dbPage.UrlSlug != pageData.UrlSlug;

                if (dbPage.GanjoorPageType == GanjoorPageType.CatPage || dbPage.GanjoorPageType == GanjoorPageType.PoetPage)
                {
                    GanjoorCat cat = await context.GanjoorCategories.Where(c => c.Id == dbPage.CatId).SingleAsync();
                    cat.Published = pageData.Published;
                    cat.MixedModeOrder = pageData.MixedModeOrder;
                    cat.TableOfContentsStyle = pageData.TableOfContentsStyle;
                    cat.CatType = pageData.CatType;
                    cat.Description = pageData.Description;
                    cat.DescriptionHtml = pageData.DescriptionHtml;

                    context.GanjoorCategories.Update(cat);
                    await context.SaveChangesAsync();

                    if (dbPage.GanjoorPageType == GanjoorPageType.PoetPage)
                    {
                        var poet = await context.GanjoorPoets.Where(p => p.Id == dbPage.PoetId).SingleAsync();
                        poet.Description = cat.Description;
                        context.Update(poet);
                        await context.SaveChangesAsync();
                    }
                }

                if (messWithTitles || messWithUrls)
                {

                    dbPage.Title = pageData.Title;
                    dbPage.UrlSlug = pageData.UrlSlug;

                    if (dbPage.ParentId != null)
                    {
                        GanjoorPage parent = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == dbPage.ParentId).SingleAsync();
                        if (messWithUrls)
                        {
                            dbPage.FullUrl = parent.FullUrl + "/" + pageData.UrlSlug;
                        }
                        if (messWithTitles)
                        {
                            dbPage.FullTitle = parent.FullTitle + " » " + pageData.Title;
                        }
                    }
                    else
                    {
                        if (messWithUrls)
                        {
                            dbPage.FullUrl = "/" + pageData.UrlSlug;
                        }

                        if (messWithTitles)
                        {
                            dbPage.FullTitle = pageData.Title;
                        }

                    }

                    switch (dbPage.GanjoorPageType)
                    {
                        case GanjoorPageType.CatPage:
                            {
                                GanjoorCat cat = await context.GanjoorCategories.Where(c => c.Id == dbPage.CatId).SingleAsync();
                                if (messWithTitles)
                                    cat.Title = dbPage.Title;
                                if (messWithUrls)
                                {
                                    cat.UrlSlug = dbPage.UrlSlug;
                                    cat.FullUrl = dbPage.FullUrl;
                                }

                                context.GanjoorCategories.Update(cat);
                                await context.SaveChangesAsync();
                            }
                            break;
                    }
                    _backgroundTaskQueue.QueueBackgroundWorkItem
                       (
                       async token =>
                       {
                           using (RMuseumDbContext inlineContext = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so context might be already been freed/collected by GC
                           {
                               LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(inlineContext);
                               var job = (await jobProgressServiceEF.NewJob($"Updating PageChildren for {dbPage.Id}", "Updating")).Result;
                               try
                               {


                                   await _UpdatePageChildrenTitleAndUrl(inlineContext, dbPage, messWithTitles, messWithUrls);

                                   await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                               }
                               catch (Exception expUpdateBatch)
                               {
                                   await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, expUpdateBatch.ToString());
                               }
                           }

                       }
                       );

                }

                if (dbPage.GanjoorPageType == GanjoorPageType.PoetPage && (messWithTitles || messWithUrls))
                {
                    if (messWithTitles)
                    {
                        GanjoorPoet poet = await context.GanjoorPoets.Where(p => p.Id == dbPage.PoetId).SingleAsync();
                        poet.Nickname = dbPage.Title;
                        context.GanjoorPoets.Update(poet);
                    }


                    GanjoorCat cat = await context.GanjoorCategories.Where(c => c.Id == dbPage.CatId).SingleAsync();
                    if (messWithTitles)
                    {
                        cat.Title = dbPage.Title;
                    }
                    if (messWithUrls)
                    {
                        cat.UrlSlug = dbPage.UrlSlug;
                        cat.FullUrl = dbPage.FullUrl;
                    }


                    context.GanjoorCategories.Update(cat);

                    await context.SaveChangesAsync();

                    CleanPoetCache((int)dbPage.PoetId);
                }

                context.GanjoorPages.Update(dbPage);
                
                await context.SaveChangesAsync();
                CacheCleanForPageByUrl(dbPage.FullUrl);

                if(needsReturn)
                {
                    return await GetPageByUrl(dbPage.FullUrl);
                }
                return new RServiceResult<GanjoorPageCompleteViewModel>(null);
               
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPageCompleteViewModel>(null, exp.ToString());
            }
        }
    }
}
