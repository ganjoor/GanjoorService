using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using RMuseum.Models.Ganjoor.ViewModels;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// break a poem from a verse forward
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="vOrder"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> BreakPoemAsync(int poemId, int vOrder, Guid userId)
        {
            try
            {
                var poem = (await GetPoemById(poemId, true, false, true, false, false, false, false, true, true)).Result;
                var parentPage = await _context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == poem.Category.Cat.Id).SingleAsync();
                var poemTitleStaticPart = poem.Title.Replace("۰", "").Replace("۱", "1").Replace("۲", "").Replace("۳", "").Replace("۴", "").Replace("۵", "").Replace("۶", "").Replace("۷", "").Replace("۸", "1").Replace("۹", "").Trim();
                if (poem.Next == null)
                {
                    return await _BreakLastPoemInItsCategoryAsync(poemId, vOrder, userId, poem, parentPage, poemTitleStaticPart);
                }
                var dbMainPoem = await _context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleOrDefaultAsync();
                var poemList = await _context.GanjoorPoems.AsNoTracking()
                                                          .Where(p => p.CatId == dbMainPoem.CatId && p.Id > dbMainPoem.Id).OrderBy(p => p.Id).ToListAsync();

                var lastPoemInCategory = poemList.Last();

                if (!int.TryParse(lastPoemInCategory.UrlSlug.Substring("sh".Length), out int newPoemSlugNumber))
                    return new RServiceResult<int>(-1, $"slug error for the last poem in the category: {lastPoemInCategory.UrlSlug}");

                string nextPoemUrlSluf = $"sh{newPoemSlugNumber + 1}";

                var maxPoemId = await _context.GanjoorPoems.MaxAsync(p => p.Id);
                if (await _context.GanjoorPages.MaxAsync(p => p.Id) > maxPoemId)
                    maxPoemId = await _context.GanjoorPages.MaxAsync(p => p.Id);
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
                _context.GanjoorPoems.Add(dbNewPoem);
                _context.GanjoorPages.Add(dbPoemNewPage);
                await _context.SaveChangesAsync();

                var targetPoem = dbNewPoem;
                var targetPage = dbPoemNewPage;

                //now copy each poem to its next sibling in their category


                return new RServiceResult<int>(0, "poem.Next != null");
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
           
        }


        private async Task<RServiceResult<int>> _BreakLastPoemInItsCategoryAsync(int poemId, int vOrder, Guid userId, GanjoorPoemCompleteViewModel poem, GanjoorPage parentPage, string poemTitleStaticPart)
        {
            if (poem.UrlSlug.IndexOf("sh") != 0)
            {
                return new RServiceResult<int>(-1, "poem.UrlSlug.IndexOf(\"sh\") != 0");
            }

            try
            {
                var dbMainPoem = await _context.GanjoorPoems.Include(p => p.GanjoorMetre).Where(p => p.Id == poemId).SingleOrDefaultAsync();
                var dbPage = await _context.GanjoorPages.AsNoTracking().Where(p => p.Id == poemId).SingleOrDefaultAsync();

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

                var res = await UpdatePageAsync(poemId, userId, pageViewModel);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return new RServiceResult<int>(0, res.ExceptionString);

               

                if (!int.TryParse(poem.UrlSlug.Substring("sh".Length), out int slugNumber))
                    return new RServiceResult<int>(-1, $"slug error: {poem.UrlSlug}");

                var poemPage = await _context.GanjoorPages.AsNoTracking().Where(p => p.Id == poem.Id).SingleAsync();
                

                string nextPoemUrlSluf = $"sh{slugNumber + 1}";

                var maxPoemId = await _context.GanjoorPoems.MaxAsync(p => p.Id);
                if (await _context.GanjoorPages.MaxAsync(p => p.Id) > maxPoemId)
                    maxPoemId = await _context.GanjoorPages.MaxAsync(p => p.Id);
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



                var poemVerses = await _context.GanjoorVerses.Where(v => v.PoemId == poemId && v.VOrder >= vOrder).OrderBy(v => v.VOrder).ToListAsync();

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


                _context.GanjoorPoems.Add(dbNewPoem);
                _context.GanjoorVerses.UpdateRange(poemVerses);

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

                _context.GanjoorPages.Add(dbPoemNewPage);
                await _context.SaveChangesAsync();


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

                var afterUpdatePoemVerses = await _context.GanjoorVerses.Where(v => v.PoemId == poemId).OrderBy(v => v.VOrder).ToListAsync();
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

                var res2 = await UpdatePageAsync(poemId, userId, afterUpdatePageViewModel);
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