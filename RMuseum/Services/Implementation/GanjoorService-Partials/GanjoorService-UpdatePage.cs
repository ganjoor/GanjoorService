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

        private async Task<RServiceResult<GanjoorPageCompleteViewModel>> _UpdatePageAsync(RMuseumDbContext context, int id, Guid editingUserId, GanjoorModifyPageViewModel pageData, bool needsReturn)
        {
            try
            {
                var dbPage = await context.GanjoorPages.Where(p => p.Id == id).SingleOrDefaultAsync();
                if (dbPage == null)
                    return new RServiceResult<GanjoorPageCompleteViewModel>(null);//not found



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

                GanjoorPoem dbPoem = null;

                if (dbPage.GanjoorPageType == GanjoorPageType.PoemPage)
                {
                    dbPoem = await context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == id).SingleOrDefaultAsync();

                    snapshot.SourceName = dbPoem.SourceName;
                    snapshot.SourceUrlSlug = dbPoem.SourceUrlSlug;
                    snapshot.Rhythm = dbPoem.GanjoorMetre == null ? null : dbPoem.GanjoorMetre.Rhythm;
                    snapshot.RhymeLetters = dbPoem.RhymeLetters;
                    snapshot.OldTag = dbPoem.OldTag;
                    snapshot.OldTagPageUrl = dbPoem.OldTagPageUrl;
                }

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

                if (dbPoem != null)
                {
                    dbPoem.SourceName = pageData.SourceName;
                    dbPoem.SourceUrlSlug = pageData.SourceUrlSlug;
                    int? oldMetreId = dbPoem.GanjoorMetreId;
                    string oldRhymeLetters = dbPoem.RhymeLetters;
                    if (string.IsNullOrEmpty(pageData.Rhythm))
                    {
                        dbPoem.GanjoorMetreId = null;
                    }
                    else
                    {
                        var metre = await context.GanjoorMetres.Where(m => m.Rhythm == pageData.Rhythm).SingleOrDefaultAsync();
                        if (metre == null)
                        {
                            metre = new GanjoorMetre()
                            {
                                Rhythm = pageData.Rhythm,
                                VerseCount = 0
                            };
                            context.GanjoorMetres.Add(metre);
                            await context.SaveChangesAsync();
                        }
                        dbPoem.GanjoorMetreId = metre.Id;
                    }
                    dbPoem.RhymeLetters = pageData.RhymeLetters;
                    dbPoem.OldTag = pageData.OldTag;
                    dbPoem.OldTagPageUrl = pageData.OldTagPageUrl;
                    dbPoem.MixedModeOrder = pageData.MixedModeOrder;
                    dbPoem.Language = pageData.Language;
                    dbPoem.Published = pageData.Published;


                    dbPoem.HtmlText = pageData.HtmlText;
                    dbPoem.Title = pageData.Title;
                    dbPoem.UrlSlug = pageData.UrlSlug;
                    dbPoem.FullUrl = dbPage.FullUrl;
                    dbPoem.FullTitle = dbPoem.FullTitle;

                    List<GanjoorVerse> verses = _extractVersesFromPoemHtmlText(id, pageData.HtmlText);

                    dbPoem.PlainText = PreparePlainText(verses);

                    context.GanjoorPoems.Update(dbPoem);


                    var oldVerses = await context.GanjoorVerses.Where(v => v.PoemId == id).ToListAsync();

                    if (oldVerses.Count <= verses.Count)
                    {
                        for (int v = 0; v < oldVerses.Count; v++)
                        {
                            oldVerses[v].Text = verses[v].Text;
                            oldVerses[v].VersePosition = verses[v].VersePosition;
                            oldVerses[v].VOrder = verses[v].VOrder;
                            context.GanjoorVerses.Update(oldVerses[v]);
                        }

                        for (int v = oldVerses.Count; v < verses.Count; v++)
                        {
                            context.GanjoorVerses.Add(verses[v]);
                        }
                    }
                    else
                    {
                        for (int v = 0; v < verses.Count; v++)
                        {
                            oldVerses[v].Text = verses[v].Text;
                            oldVerses[v].VersePosition = verses[v].VersePosition;
                            oldVerses[v].VOrder = verses[v].VOrder;
                            context.GanjoorVerses.Update(oldVerses[v]);
                        }

                        for (int v = verses.Count; v < oldVerses.Count; v++)
                        {
                            context.GanjoorVerses.Remove(oldVerses[v]);
                        }
                    }

                    await _FillPoemCoupletIndices(context, id);

                    var excerptsInRelatedCaches = await context.GanjoorCachedRelatedPoems.Where(p => p.FullUrl == dbPoem.FullUrl).ToListAsync();
                    if (excerptsInRelatedCaches.Count > 0)
                    {
                        var newExcerpt = GetPoemHtmlExcerpt(dbPoem.HtmlText);
                        foreach (var excerptsInRelatedCache in excerptsInRelatedCaches)
                        {
                            excerptsInRelatedCache.HtmlExcerpt = newExcerpt;
                        }
                        context.GanjoorCachedRelatedPoems.UpdateRange(excerptsInRelatedCaches);
                    }

                    if (oldMetreId != dbPoem.GanjoorMetreId || oldRhymeLetters != dbPoem.RhymeLetters)
                    {
                        await context.SaveChangesAsync();

                        _backgroundTaskQueue.QueueBackgroundWorkItem
                            (
                            async token =>
                            {
                                using (RMuseumDbContext inlineContext = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so context might be already been freed/collected by GC
                                {
                                    if (oldMetreId != null && !string.IsNullOrEmpty(oldRhymeLetters))
                                    {
                                        await _UpdateRelatedPoems(inlineContext, (int)oldMetreId, oldRhymeLetters);
                                        await inlineContext.SaveChangesAsync();
                                    }

                                    if (dbPoem.GanjoorMetreId != null && !string.IsNullOrEmpty(dbPoem.RhymeLetters))
                                    {
                                        await _UpdateRelatedPoems(inlineContext, (int)dbPoem.GanjoorMetreId, dbPoem.RhymeLetters);
                                        await inlineContext.SaveChangesAsync();
                                    }
                                }
                            });
                    }
                }
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
