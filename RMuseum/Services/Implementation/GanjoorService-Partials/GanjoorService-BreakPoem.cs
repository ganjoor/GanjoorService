using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.DbContext;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        public async Task<RServiceResult<int>> _BreakPoemAsync(RMuseumDbContext context, int poemId, int vOrder, Guid userId, GanjoorPoemCompleteViewModel poem, GanjoorPage parentPage, string poemTitleStaticPart)
        {
            await context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleOrDefaultAsync();//waring killer
            return new RServiceResult<int>(0, "_BreakPoemAsync is disabled for now because of dependence to _UpdatePageAsync");
            /*
            try
            {
                if (poem.Next == null)
                {
                    return await _BreakLastPoemInItsCategoryAsync(context, poemId, vOrder, userId, poem, parentPage, poemTitleStaticPart);
                }
                var dbMainPoem = await context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleOrDefaultAsync();
                var dbPage = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == poemId).SingleOrDefaultAsync();

                GanjoorModifyPageViewModel pageViewModel = new GanjoorModifyPageViewModel()
                {
                    Title = dbMainPoem.Title,
                    UrlSlug = dbMainPoem.UrlSlug,
                    OldTag = dbMainPoem.OldTag,
                    OldTagPageUrl = dbMainPoem.OldTagPageUrl,
                    RhymeLetters = dbMainPoem.RhymeLetters,
                    Rhythm = dbMainPoem.GanjoorMetre == null ? null : dbMainPoem.GanjoorMetre.Rhythm,
                    HtmlText = dbMainPoem.HtmlText,
                    SourceName = dbMainPoem.SourceName,
                    SourceUrlSlug = dbMainPoem.SourceUrlSlug,
                    RedirectFromFullUrl = dbPage.RedirectFromFullUrl,
                    NoIndex = dbPage.NoIndex,
                    Language = dbMainPoem.Language,
                    MixedModeOrder = dbMainPoem.MixedModeOrder,
                    Published = dbMainPoem.Published,
                    Note = "گام اول شکستن شعر به اشعار مجزا"
                };

                var res = await _UpdatePageAsync(context, poemId, userId, pageViewModel, false);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return new RServiceResult<int>(0, res.ExceptionString);

                var poemList = await context.GanjoorPoems.AsNoTracking()
                                                          .Where(p => p.CatId == dbMainPoem.CatId && p.Id > dbMainPoem.Id).OrderBy(p => p.Id).ToListAsync();

                var lastPoemInCategory = poemList.Last();

                if (!int.TryParse(lastPoemInCategory.UrlSlug.Substring("sh".Length), out int newPoemSlugNumber))
                    return new RServiceResult<int>(-1, $"slug error for the last poem in the category: {lastPoemInCategory.UrlSlug}");

                string nextPoemUrlSluf = $"sh{newPoemSlugNumber + 1}";

                var maxPoemId = await context.GanjoorPoems.MaxAsync(p => p.Id);
                if (await context.GanjoorPages.MaxAsync(p => p.Id) > maxPoemId)
                    maxPoemId = await context.GanjoorPages.MaxAsync(p => p.Id);
                var nextPoemId = 1 + maxPoemId;

                string nextPoemTitle = $"{poemTitleStaticPart} {(newPoemSlugNumber + 1).ToPersianNumbers()}";

                GanjoorPoem dbNewPoem = new GanjoorPoem()
                {
                    Id = nextPoemId,
                    CatId = poem.Category.Cat.Id,
                    Title = nextPoemTitle,
                    UrlSlug = nextPoemUrlSluf,
                    FullTitle = $"{parentPage.FullTitle} » {nextPoemTitle}",
                    FullUrl = $"{parentPage.FullUrl}/{nextPoemUrlSluf}",
                    SourceName = poem.SourceName,
                    SourceUrlSlug = poem.SourceUrlSlug,
                    Language = dbMainPoem.Language,
                    MixedModeOrder = dbMainPoem.MixedModeOrder,
                    Published = dbMainPoem.Published,
                };

                GanjoorPage dbPoemNewPage = new GanjoorPage()
                {
                    Id = nextPoemId,
                    GanjoorPageType = GanjoorPageType.PoemPage,
                    Published = true,
                    PageOrder = -1,
                    Title = dbNewPoem.Title,
                    FullTitle = dbNewPoem.FullTitle,
                    UrlSlug = dbNewPoem.UrlSlug,
                    FullUrl = dbNewPoem.FullUrl,
                    HtmlText = dbNewPoem.HtmlText,
                    PoetId = parentPage.PoetId,
                    CatId = poem.Category.Cat.Id,
                    PoemId = nextPoemId,
                    PostDate = DateTime.Now,
                    ParentId = parentPage.Id,
                };
                context.GanjoorPoems.Add(dbNewPoem);
                context.GanjoorPages.Add(dbPoemNewPage);
                await context.SaveChangesAsync();

                int targetPoemId = dbPoemNewPage.Id;

                //now copy each poem to its next sibling in their category
                for (int nPoemIndex = poemList.Count - 1; nPoemIndex >= 0; nPoemIndex--)
                {

                    var sourcePoem = poemList[nPoemIndex];


                    var targetPoem = await context.GanjoorPoems.Where(p => p.Id == targetPoemId).SingleAsync();
                    //copy everything but url and title:
                    targetPoem.HtmlText = sourcePoem.HtmlText;
                    targetPoem.PlainText = sourcePoem.PlainText;
                    targetPoem.GanjoorMetreId = sourcePoem.GanjoorMetreId;
                    targetPoem.RhymeLetters = sourcePoem.RhymeLetters;
                    targetPoem.SourceName = sourcePoem.SourceName;
                    targetPoem.SourceUrlSlug = sourcePoem.SourceUrlSlug;
                    targetPoem.OldTag = sourcePoem.OldTag;
                    targetPoem.OldTagPageUrl = sourcePoem.OldTagPageUrl;
                    targetPoem.MixedModeOrder = sourcePoem.MixedModeOrder;
                    targetPoem.Published = sourcePoem.Published;
                    targetPoem.Language = sourcePoem.Language;
                    context.Update(targetPoem);


                    var sourcePage = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == sourcePoem.Id).SingleAsync();
                    var targetPage = await context.GanjoorPages.Where(p => p.Id == targetPoemId).SingleAsync();
                    //copy everything but url and title:
                    targetPage.Published = sourcePage.Published;
                    targetPage.PageOrder = sourcePage.PageOrder;
                    targetPage.ParentId = sourcePage.ParentId;
                    targetPage.PoetId = sourcePage.PoetId;
                    targetPage.CatId = sourcePage.CatId;
                    targetPage.PostDate = sourcePage.PostDate;
                    targetPage.NoIndex = sourcePage.NoIndex;
                    targetPage.HtmlText = sourcePage.HtmlText; 
                    context.Update(targetPage);


                    var poemVerses = await context.GanjoorVerses.Where(v => v.PoemId == sourcePoem.Id).OrderBy(v => v.VOrder).ToListAsync();
                    for (int i = 0; i < poemVerses.Count; i++)
                    {
                        poemVerses[i].PoemId = targetPoemId;
                    }
                    context.GanjoorVerses.UpdateRange(poemVerses);
                    

                    var recitaions = await context.Recitations.Where(r => r.GanjoorPostId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < recitaions.Count; i++)
                    {
                        recitaions[i].GanjoorPostId = targetPoemId;
                    }
                    context.UpdateRange(recitaions);
                    

                    var tracks = await context.GanjoorPoemMusicTracks.Where(t => t.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        tracks[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(tracks);
                    

                    var comments = await context.GanjoorComments.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < comments.Count; i++)
                    {
                        comments[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(comments);
                    

                    var pageSnapshots = await context.GanjoorPageSnapshots.Where(c => c.GanjoorPageId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < pageSnapshots.Count; i++)
                    {
                        pageSnapshots[i].GanjoorPageId = targetPoemId;
                    }
                    context.UpdateRange(pageSnapshots);
                    

                    var poemCorrections = await context.GanjoorPoemCorrections.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < poemCorrections.Count; i++)
                    {
                        poemCorrections[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(poemCorrections);
                    

                    var userBookmarks = await context.GanjoorUserBookmarks.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < userBookmarks.Count; i++)
                    {
                        userBookmarks[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(userBookmarks);
                    

                    var verseNumberings = await context.GanjoorVerseNumbers.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < verseNumberings.Count; i++)
                    {
                        verseNumberings[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(verseNumberings);
                    

                    var relatedPoems = await context.GanjoorCachedRelatedPoems.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < relatedPoems.Count; i++)
                    {
                        relatedPoems[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(relatedPoems);
                    

                    var probables = await context.GanjoorPoemProbableMetres.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < probables.Count; i++)
                    {
                        probables[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(probables);
                    

                    var visits = await context.GanjoorUserPoemVisits.Where(c => c.PoemId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < visits.Count; i++)
                    {
                        visits[i].PoemId = targetPoemId;
                    }
                    context.UpdateRange(visits);
                    

                    var links = await context.GanjoorLinks.Where(l => l.GanjoorPostId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < links.Count; i++)
                    {
                        links[i].GanjoorPostId = targetPoemId;
                        links[i].GanjoorUrl = $"https://ganjoor.net{targetPoem.FullUrl}";
                        links[i].GanjoorTitle = targetPoem.FullTitle;
                    }
                    context.UpdateRange(links);
                    

                    var pinterests = await context.PinterestLinks.Where(l => l.GanjoorPostId == sourcePoem.Id).ToListAsync();
                    for (int i = 0; i < pinterests.Count; i++)
                    {
                        pinterests[i].GanjoorPostId = targetPoemId;
                        pinterests[i].GanjoorUrl = $"https://ganjoor.net{targetPoem.FullUrl}";
                        pinterests[i].GanjoorTitle = targetPoem.FullTitle;
                    }
                    context.UpdateRange(pinterests);
                    



                    targetPoemId = sourcePoem.Id;
                }

                await context.SaveChangesAsync();



                var dbLastTargetPoem = await context.GanjoorPoems.Where(p => p.Id == targetPoemId).SingleAsync();

                var mainPoemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder >= vOrder).OrderBy(v => v.VOrder).ToListAsync();
                for (int i = 0; i < mainPoemVerses.Count; i++)
                {
                    mainPoemVerses[i].VOrder = i + 1;
                    mainPoemVerses[i].PoemId = targetPoemId;
                }
                dbLastTargetPoem.PlainText = PreparePlainText(mainPoemVerses);
                dbLastTargetPoem.HtmlText = PrepareHtmlText(mainPoemVerses);
                dbLastTargetPoem.GanjoorMetreId = dbMainPoem.GanjoorMetreId;
                dbLastTargetPoem.RhymeLetters = dbMainPoem.RhymeLetters;
                dbLastTargetPoem.SourceName = dbMainPoem.SourceName;
                dbLastTargetPoem.SourceUrlSlug = dbMainPoem.SourceUrlSlug;
                dbLastTargetPoem.OldTag = dbMainPoem.OldTag;
                dbLastTargetPoem.OldTagPageUrl = dbMainPoem.OldTagPageUrl;
                dbLastTargetPoem.MixedModeOrder = dbMainPoem.MixedModeOrder;
                dbLastTargetPoem.Published = dbMainPoem.Published;
                dbLastTargetPoem.Language = dbMainPoem.Language;

                try
                {
                    var poemRhymeLettersRes = LanguageUtils.FindRhyme(mainPoemVerses);
                    if (!string.IsNullOrEmpty(poemRhymeLettersRes.Rhyme))
                    {
                        dbLastTargetPoem.RhymeLetters = poemRhymeLettersRes.Rhyme;
                    }
                }
                catch
                {

                }

                var dbLastTargetPage = await context.GanjoorPages.Where(p => p.Id == targetPoemId).SingleAsync();
                dbLastTargetPage.Published = dbPage.Published;
                dbLastTargetPage.PageOrder = dbPage.PageOrder;
                dbLastTargetPage.ParentId = dbPage.ParentId;
                dbLastTargetPage.PoetId = dbPage.PoetId;
                dbLastTargetPage.CatId = dbPage.CatId;
                dbLastTargetPage.PostDate = dbPage.PostDate;
                dbLastTargetPage.NoIndex = dbPage.NoIndex;
                dbLastTargetPage.HtmlText = dbLastTargetPoem.HtmlText;
                context.Update(dbLastTargetPage);


                context.Update(dbLastTargetPoem);
                context.GanjoorVerses.UpdateRange(mainPoemVerses);



                await context.SaveChangesAsync();


                GanjoorModifyPageViewModel afterUpdatePageViewModel = new GanjoorModifyPageViewModel()
                {
                    Title = dbMainPoem.Title,
                    UrlSlug = dbMainPoem.UrlSlug,
                    OldTag = dbMainPoem.OldTag,
                    OldTagPageUrl = dbMainPoem.OldTagPageUrl,
                    RhymeLetters = dbMainPoem.RhymeLetters,
                    Rhythm = dbMainPoem.GanjoorMetre == null ? null : dbMainPoem.GanjoorMetre.Rhythm,
                    HtmlText = dbMainPoem.HtmlText,
                    SourceName = dbMainPoem.SourceName,
                    SourceUrlSlug = dbMainPoem.SourceUrlSlug,
                    RedirectFromFullUrl = dbPage.RedirectFromFullUrl,
                    NoIndex = dbPage.NoIndex,
                    Language = dbMainPoem.Language,
                    MixedModeOrder = dbMainPoem.MixedModeOrder,
                    Published = dbMainPoem.Published,
                    Note = "گام نهایی شکستن شعر به اشعار مجزا"
                };

                var afterUpdatePoemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId).OrderBy(v => v.VOrder).ToListAsync();
                afterUpdatePageViewModel.HtmlText = PrepareHtmlText(afterUpdatePoemVerses);
                try
                {
                    var newRhyme = LanguageUtils.FindRhyme(afterUpdatePoemVerses);
                    if (!string.IsNullOrEmpty(newRhyme.Rhyme) &&
                        (string.IsNullOrEmpty(dbMainPoem.RhymeLetters) || (!string.IsNullOrEmpty(dbMainPoem.RhymeLetters) && newRhyme.Rhyme.Length > dbMainPoem.RhymeLetters.Length))
                        )
                    {
                        afterUpdatePageViewModel.RhymeLetters = newRhyme.Rhyme;
                    }
                }
                catch
                {

                }

                var res2 = await _UpdatePageAsync(context, poemId, userId, afterUpdatePageViewModel, false);
                if (!string.IsNullOrEmpty(res2.ExceptionString))
                    return new RServiceResult<int>(0, res2.ExceptionString);
                return new RServiceResult<int>(targetPoemId);
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
            */
        }


        private async Task<RServiceResult<int>> _BreakLastPoemInItsCategoryAsync(RMuseumDbContext context, int poemId, int vOrder, Guid userId, GanjoorPoemCompleteViewModel poem, GanjoorPage parentPage, string poemTitleStaticPart)
        {
            if (poem.UrlSlug.IndexOf("sh") != 0)
            {
                return new RServiceResult<int>(-1, "poem.UrlSlug.IndexOf(\"sh\") != 0");
            }

            try
            {
                var dbMainPoem = await context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleOrDefaultAsync();
                var dbPage = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == poemId).SingleOrDefaultAsync();

                GanjoorModifyPageViewModel pageViewModel = new GanjoorModifyPageViewModel()
                {
                    Title = dbMainPoem.Title,
                    UrlSlug = dbMainPoem.UrlSlug,
                    OldTag = dbMainPoem.OldTag,
                    OldTagPageUrl = dbMainPoem.OldTagPageUrl,
                    RhymeLetters = dbMainPoem.RhymeLetters,
                    Rhythm = dbMainPoem.GanjoorMetre == null ? null : dbMainPoem.GanjoorMetre.Rhythm,
                    HtmlText = dbMainPoem.HtmlText,
                    SourceName = dbMainPoem.SourceName,
                    SourceUrlSlug = dbMainPoem.SourceUrlSlug,
                    RedirectFromFullUrl = dbPage.RedirectFromFullUrl,
                    NoIndex = dbPage.NoIndex,
                    Language = dbMainPoem.Language,
                    MixedModeOrder = dbMainPoem.MixedModeOrder,
                    Published = dbMainPoem.Published,
                    Note = "گام اول شکستن شعر به اشعار مجزا"
                };

                var res = await _UpdatePageAsync(context, poemId, userId, pageViewModel, false);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return new RServiceResult<int>(0, res.ExceptionString);

               

                if (!int.TryParse(poem.UrlSlug.Substring("sh".Length), out int slugNumber))
                    return new RServiceResult<int>(-1, $"slug error: {poem.UrlSlug}");

                var poemPage = await context.GanjoorPages.AsNoTracking().Where(p => p.Id == poem.Id).SingleAsync();
                

                string nextPoemUrlSluf = $"sh{slugNumber + 1}";

                var maxPoemId = await context.GanjoorPoems.MaxAsync(p => p.Id);
                if (await context.GanjoorPages.MaxAsync(p => p.Id) > maxPoemId)
                    maxPoemId = await context.GanjoorPages.MaxAsync(p => p.Id);
                var nextPoemId = 1 + maxPoemId;

                string nextPoemTitle = $"{poemTitleStaticPart} {(slugNumber + 1).ToPersianNumbers()}";

                GanjoorPoem dbNewPoem = new GanjoorPoem()
                {
                    Id = nextPoemId,
                    CatId = poem.Category.Cat.Id,
                    Title = nextPoemTitle,
                    UrlSlug = nextPoemUrlSluf,
                    FullTitle = $"{parentPage.FullTitle} » {nextPoemTitle}",
                    FullUrl = $"{parentPage.FullUrl}/{nextPoemUrlSluf}",
                    GanjoorMetreId = poem.GanjoorMetre == null ? null : poem.GanjoorMetre.Id,
                    SourceName = poem.SourceName,
                    SourceUrlSlug = poem.SourceUrlSlug,
                    Language = dbMainPoem.Language,
                    MixedModeOrder = dbMainPoem.MixedModeOrder,
                    Published = dbMainPoem.Published,
                };



                var poemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder >= vOrder).OrderBy(v => v.VOrder).ToListAsync();

                for (int i = 0; i < poemVerses.Count; i++)
                {
                    poemVerses[i].VOrder = i + 1;
                    poemVerses[i].PoemId = nextPoemId;
                }

                dbNewPoem.PlainText = PreparePlainText(poemVerses);
                dbNewPoem.HtmlText = PrepareHtmlText(poemVerses);

                try
                {
                    var poemRhymeLettersRes = LanguageUtils.FindRhyme(poemVerses);
                    if (!string.IsNullOrEmpty(poemRhymeLettersRes.Rhyme))
                    {
                        dbNewPoem.RhymeLetters = poemRhymeLettersRes.Rhyme;
                    }
                }
                catch
                {

                }


                context.GanjoorPoems.Add(dbNewPoem);
                context.GanjoorVerses.UpdateRange(poemVerses);

                GanjoorPage dbPoemNewPage = new GanjoorPage()
                {
                    Id = nextPoemId,
                    GanjoorPageType = GanjoorPageType.PoemPage,
                    Published = true,
                    PageOrder = -1,
                    Title = dbNewPoem.Title,
                    FullTitle = dbNewPoem.FullTitle,
                    UrlSlug = dbNewPoem.UrlSlug,
                    FullUrl = dbNewPoem.FullUrl,
                    HtmlText = dbNewPoem.HtmlText,
                    PoetId = parentPage.PoetId,
                    CatId = poem.Category.Cat.Id,
                    PoemId = nextPoemId,
                    PostDate = DateTime.Now,
                    ParentId = parentPage.Id,
                };

                context.GanjoorPages.Add(dbPoemNewPage);
                await context.SaveChangesAsync();


                GanjoorModifyPageViewModel afterUpdatePageViewModel = new GanjoorModifyPageViewModel()
                {
                    Title = dbMainPoem.Title,
                    UrlSlug = dbMainPoem.UrlSlug,
                    OldTag = dbMainPoem.OldTag,
                    OldTagPageUrl = dbMainPoem.OldTagPageUrl,
                    RhymeLetters = dbMainPoem.RhymeLetters,
                    Rhythm = dbMainPoem.GanjoorMetre == null ? null : dbMainPoem.GanjoorMetre.Rhythm,
                    HtmlText = dbMainPoem.HtmlText,
                    SourceName = dbMainPoem.SourceName,
                    SourceUrlSlug = dbMainPoem.SourceUrlSlug,
                    RedirectFromFullUrl = dbPage.RedirectFromFullUrl,
                    NoIndex = dbPage.NoIndex,
                    Language = dbMainPoem.Language,
                    MixedModeOrder = dbMainPoem.MixedModeOrder,
                    Published = dbMainPoem.Published,
                    Note = "گام نهایی شکستن شعر به اشعار مجزا"
                };

                var afterUpdatePoemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId).OrderBy(v => v.VOrder).ToListAsync();
                afterUpdatePageViewModel.HtmlText = PrepareHtmlText(afterUpdatePoemVerses);
                try
                {
                    var newRhyme = LanguageUtils.FindRhyme(afterUpdatePoemVerses);
                    if (!string.IsNullOrEmpty(newRhyme.Rhyme) &&
                        (string.IsNullOrEmpty(dbMainPoem.RhymeLetters) || (!string.IsNullOrEmpty(dbMainPoem.RhymeLetters) && newRhyme.Rhyme.Length > dbMainPoem.RhymeLetters.Length))
                        )
                    {
                        afterUpdatePageViewModel.RhymeLetters = newRhyme.Rhyme;
                    }
                }
                catch
                {

                }

                var res2 = await _UpdatePageAsync(context, poemId, userId, afterUpdatePageViewModel, false);
                if (!string.IsNullOrEmpty(res2.ExceptionString))
                    return new RServiceResult<int>(0, res2.ExceptionString);



                return new RServiceResult<int>(nextPoemId);
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }
    }
}