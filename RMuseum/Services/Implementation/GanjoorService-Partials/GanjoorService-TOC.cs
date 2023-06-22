using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
        /// regenerate TOCs
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartRegeneratingTOCs(Guid userId)
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
                                   var job = (await jobProgressServiceEF.NewJob("StartRegeneratingTOCs", "Query data")).Result;
                                   try
                                   {
                                       var cats = await context.GanjoorCategories.AsNoTracking().ToListAsync();

                                       foreach (var cat in cats)
                                       {
                                           var page =
                                                    await context.GanjoorPages.AsNoTracking()
                                                    .Where(p => (p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == cat.PoetId && cat.ParentId == null) || (p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == cat.Id)).SingleOrDefaultAsync();
                                           if (page == null) continue;
                                           await jobProgressServiceEF.UpdateJob(job.Id, 0, page.FullTitle);
                                           if(cat.TableOfContentsStyle == GanjoorTOC.Analyse)
                                           {
                                               var hasChild = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).AnyAsync(); 
                                               if (cat.ParentId == null || hasChild)
                                                   cat.TableOfContentsStyle = GanjoorTOC.OnlyTitles;
                                               else
                                                   cat.TableOfContentsStyle = GanjoorTOC.TitlesAndFirstVerse;
                                               var catToUpdate = await context.GanjoorCategories.Where(c => c.Id == cat.Id).SingleAsync();
                                               catToUpdate.TableOfContentsStyle = cat.TableOfContentsStyle;
                                               context.Update(catToUpdate);
                                               await context.SaveChangesAsync();
                                           }
                                           await _DirectInsertGeneratedTableOfContents(context,cat.Id, userId, cat.TableOfContentsStyle);

                                           await context.SaveChangesAsync();
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

        private async Task<RServiceResult<bool>> _DirectInsertGeneratedTableOfContents(RMuseumDbContext context, int catId, Guid userId, GanjoorTOC options)
        {
            try
            {
                var resGeneration = await _GenerateTableOfContents(context, catId, options);
                if (!string.IsNullOrEmpty(resGeneration.ExceptionString))
                {
                    return new RServiceResult<bool>(false, resGeneration.ExceptionString);
                }
                var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                var dbPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == catId).SingleOrDefaultAsync();
                if (dbPage == null)
                {
                    dbPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == cat.PoetId).SingleAsync();
                }

                await _UpdatePageAsync(context, dbPage.Id, userId,
                new GanjoorModifyPageViewModel()
                {
                    Title = dbPage.Title,
                    HtmlText = resGeneration.Result,
                    Note = "تولید فهرست بخش",
                    UrlSlug = dbPage.UrlSlug,
                    NoIndex = dbPage.NoIndex,
                    RedirectFromFullUrl = dbPage.RedirectFromFullUrl,
                    Published = dbPage.Published,
                    CatType = cat.CatType,
                    Description = cat.Description,
                    DescriptionHtml = cat.DescriptionHtml,
                    MixedModeOrder = cat.MixedModeOrder,
                    TableOfContentsStyle = cat.TableOfContentsStyle,
                },
                false
                );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private async Task<string> _AdditionalTableOfContentsAnchorTitleForPoem(string title, RMuseumDbContext context, GanjoorPoem poem, GanjoorTOC options)
        {
            var verses = await context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == poem.Id).OrderBy(v => v.VOrder).ToArrayAsync();

            if (verses.Length > 0)
            {
                if (options == GanjoorTOC.TitlesAndFirstVerse || options == GanjoorTOC.AlphabeticWithFirstVerse || options == GanjoorTOC.AlphabeticWithFirstVerseNotSorted)
                {
                    var excerpt = verses[0].Text;
                    if (
                        (
                        verses[0].VersePosition == VersePosition.Paragraph
                        ||
                        verses[0].VersePosition == VersePosition.Single
                        ||
                        verses[0].VersePosition == VersePosition.Comment
                        ) && excerpt.Length > 100)
                    {
                        excerpt = excerpt.Substring(0, 50);
                        int n = excerpt.LastIndexOf(' ');
                        if (n >= 0)
                        {
                            excerpt = excerpt.Substring(0, n) + " ...";
                        }
                        else
                        {
                            excerpt += "...";
                        }
                    }
                    title += $": {excerpt}";
                }
                else
                if (options == GanjoorTOC.AlphabeticWithSecondVerse || options == GanjoorTOC.AlphabeticWithSecondVerseNotSorted || options == GanjoorTOC.TitlesAndSecondVerse)
                {
                    if (verses.Length > 1)
                    {
                        title += $": {verses[1].Text}";
                    }
                    else
                    {
                        title += $": {verses[0].Text}";
                    }
                }
                else
                if (options == GanjoorTOC.AlphabeticWithFirstCouplet || options == GanjoorTOC.AlphabeticWithFirstCoupletNotSorted || options == GanjoorTOC.TitlesAndFirstCouplet)
                {
                    if (verses.Length > 1)
                    {
                        title += $": {verses[0].Text} - {verses[1].Text}";
                    }
                    else
                    {
                        title += $": {verses[0].Text}";
                    }
                }
                else
                if (options == GanjoorTOC.TitlesAndFirstCenteredVerse)
                {
                    if (verses.Where(v => v.VersePosition == VersePosition.CenteredVerse1).Any())
                    {
                        title += $": {verses.Where(v => v.VersePosition == VersePosition.CenteredVerse1).First().Text}";
                    }
                    else
                    {
                        title += $": {verses[0].Text}";
                    }
                }
                else
                if (options == GanjoorTOC.TitlesAndFirstCenteredCouplet)
                {
                    if (
                        verses.Where(v => v.VersePosition == VersePosition.CenteredVerse1).Any()
                        &&
                        verses.Where(v => v.VersePosition == VersePosition.CenteredVerse2).Any()
                        )
                    {
                        title += $": {verses.Where(v => v.VersePosition == VersePosition.CenteredVerse1).First().Text} - {verses.Where(v => v.VersePosition == VersePosition.CenteredVerse2).First().Text}";
                    }
                    else
                    {
                        title += $": {verses[0].Text}";
                    }
                }
            }
            return title
                       .Replace("ّ", "")//tashdid
                       .Replace("َ", "")//a
                       .Replace("ِ", "")//e
                       .Replace("ُ", "")//o
                       .Replace("ً", "")//an
                       .Replace("ٍ", "")//en
                       .Replace("ٌ", "")//on
                       .Replace("ْ", "")//sokoon
                       .Replace("ٔ", "")//ye
                       ;
        }
        private async Task<RServiceResult<string>> _GenerateTableOfContents(RMuseumDbContext context, int catId, GanjoorTOC options)
        {
            try
            {
                var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                string html = string.IsNullOrEmpty(cat.DescriptionHtml) ? "" : $"{cat.DescriptionHtml.Replace("../", "https://ganjoor.net/") }{Environment.NewLine}";
                var subCats = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == catId).OrderBy(c => c.MixedModeOrder).ThenBy(c => c.Id).ToListAsync();
                var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).OrderBy(p => p.MixedModeOrder).ThenBy(p => p.Id).ToListAsync();

                if (options == GanjoorTOC.OnlyTitles || (options == GanjoorTOC.Analyse && (cat.ParentId == null || subCats.Where(c => c.MixedModeOrder != 0).Any() || poems.Count == 0 || poems.Where(p => p.MixedModeOrder != 0).Any())))//ignore options parameter
                {
                    if((subCats.Count + poems.Count) > 7)
                    {
                        html += $"<div class=\"clear-both\">{Environment.NewLine}";
                        html += $"<input type=\"text\" id=\"findpoet\" placeholder=\"جستجو در عناوین\" size=\"35\" value=\"\" oninput=\"onInlineSearch(this.value, 'found-items', 'part-title-block')\">{Environment.NewLine}";
                        html += $"<div class=\"spacer\" id=\"found-items\"></div>{Environment.NewLine}";
                        html += $"</div>{Environment.NewLine}";

                        html += $"<br>{Environment.NewLine}";
                    }
                    int nMixedModeOrder = 1;
                    while (subCats.Where(c => c.MixedModeOrder == nMixedModeOrder).Any() || poems.Where(p => p.MixedModeOrder == nMixedModeOrder).Any())
                    {
                        var subCatWithThisMixedOrder = subCats.Where(c => c.MixedModeOrder == nMixedModeOrder).ToArray();
                        foreach (var subCat in subCatWithThisMixedOrder)
                        {
                            html += $"<div class=\"part-title-block\" data-value=\"{subCat.Title}\" id=\"cat-{subCat.Id}\">{Environment.NewLine}";
                            html += $"<a href=\"{subCat.FullUrl}\">{subCat.Title}</a>{Environment.NewLine}";
                            html += $"</div>{Environment.NewLine}";
                        }

                        var poemsWithThisMixedOrder = poems.Where(c => c.MixedModeOrder == nMixedModeOrder).ToArray();
                        foreach (var poem in poemsWithThisMixedOrder)
                        {
                            html += $"<div class=\"part-title-block\" data-value=\"{await _AdditionalTableOfContentsAnchorTitleForPoem("", context, poem, options)}\" id=\"poem-{poem.Id}\">{Environment.NewLine}";
                            html += $"<a href=\"{poem.FullUrl}\">{poem.Title}";
                            if (
                             options == GanjoorTOC.TitlesAndFirstVerse
                             ||
                             options == GanjoorTOC.TitlesAndSecondVerse
                             ||
                             options == GanjoorTOC.TitlesAndFirstCouplet
                             ||
                             options == GanjoorTOC.TitlesAndFirstCenteredVerse
                             )
                            {
                                html = await _AdditionalTableOfContentsAnchorTitleForPoem(html, context, poem, options);
                            }
                            html += $"</a>{Environment.NewLine}";
                            html += $"</div>{Environment.NewLine}";
                        }

                        nMixedModeOrder++;
                    }

                    nMixedModeOrder = 0;
                    if (subCats.Where(c => c.MixedModeOrder == nMixedModeOrder).Any() || poems.Where(p => p.MixedModeOrder == nMixedModeOrder).Any())
                    {
                        var subCatWithThisMixedOrder = subCats.Where(c => c.MixedModeOrder == nMixedModeOrder).ToArray();
                        foreach (var subCat in subCatWithThisMixedOrder)
                        {
                            html += $"<div class=\"part-title-block\"  data-value=\"{subCat.Title}\" id=\"cat-{subCat.Id}\">{Environment.NewLine}";
                            html += $"<a href=\"{subCat.FullUrl}\">{subCat.Title}</a>{Environment.NewLine}";
                            html += $"</div>{Environment.NewLine}";
                        }

                        var poemsWithThisMixedOrder = poems.Where(c => c.MixedModeOrder == nMixedModeOrder).ToArray();
                        foreach (var poem in poemsWithThisMixedOrder)
                        {
                            html += $"<div class=\"part-title-block\" data-value=\"{await _AdditionalTableOfContentsAnchorTitleForPoem("", context, poem, options)}\" id=\"poem-{poem.Id}\">{Environment.NewLine}";
                            html += $"<a href=\"{poem.FullUrl}\">{poem.Title}";
                            if (
                             options == GanjoorTOC.TitlesAndFirstVerse
                             ||
                             options == GanjoorTOC.TitlesAndSecondVerse
                             ||
                             options == GanjoorTOC.TitlesAndFirstCouplet
                             ||
                             options == GanjoorTOC.TitlesAndFirstCenteredVerse
                             )
                            {
                                html = await _AdditionalTableOfContentsAnchorTitleForPoem(html, context, poem, options);
                            }
                            html += $"</a>{Environment.NewLine}";
                            html += $"</div>{Environment.NewLine}";
                        }
                    }

                    if (cat.ParentId == null)
                    {
                        //poet page
                        var poetPage = await context.GanjoorPages.AsNoTracking().Where(p => p.ParentId == null && p.PoetId == cat.PoetId && p.GanjoorPageType == GanjoorPageType.PoetPage).SingleAsync();
                        var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == cat.PoetId).SingleAsync();
                        html += $"<p>دیگر صفحات مرتبط با {poet.Nickname} در این پایگاه:</p>{Environment.NewLine}";
                        var statsPage = await context.GanjoorPages.AsNoTracking()
                                .Where(p => p.FullUrl == $"{poetPage.FullUrl}/vazn").SingleOrDefaultAsync();
                        if (statsPage != null)
                        {
                            html += $"<div class=\"part-title-block-alt\" id=\"page-{statsPage.Id}\">{Environment.NewLine}";
                            html += $"<a href=\"{statsPage.FullUrl}\">اوزان اشعار {poet.Nickname}</a>{Environment.NewLine}";
                            html += $"</div>{Environment.NewLine}";
                        }
                        var thisPoetsSimilars = await context.GanjoorPages.AsNoTracking()
                               .Where(p => p.GanjoorPageType == GanjoorPageType.ProsodySimilars && p.PoetId == poet.Id).ToListAsync();

                        foreach (var childPage in thisPoetsSimilars)
                        {
                            html += $"<div class=\"part-title-block-alt\" id=\"page-{childPage.Id}\">{Environment.NewLine}";
                            html += $"<a href=\"{childPage.FullUrl}\">{childPage.Title}</a>{Environment.NewLine}";
                            html += $"</div>{Environment.NewLine}";
                        }

                        var otherPoetsSimilars = await context.GanjoorPages.AsNoTracking()
                                .Where(p => p.GanjoorPageType == GanjoorPageType.ProsodySimilars && p.SecondPoetId == poet.Id).ToListAsync();
                        foreach (var childPage in otherPoetsSimilars)
                        {
                            html += $"<div class=\"part-title-block-alt\" id=\"page-{childPage.Id}\">{Environment.NewLine}";
                            html += $"<a href=\"{childPage.FullUrl}\">{childPage.Title}</a>{Environment.NewLine}";
                            html += $"</div>{Environment.NewLine}";
                        }

                        html += $"<div class=\"part-title-block-alt\" id=\"photos-{poet.Id}\">{Environment.NewLine}";
                        html += $"<a href=\"/photos?p={cat.UrlSlug}\">تصاویر پیشنهادی برای {poet.Nickname}</a>{Environment.NewLine}";
                        html += $"</div>{Environment.NewLine}";
                    }

                    return new RServiceResult<string>(html);
                }

                foreach (var subCat in subCats)
                {
                    html += $"<div class=\"part-title-block\" id=\"cat-{subCat.Id}\">{Environment.NewLine}";
                    html += $"<a href=\"{subCat.FullUrl}\">{subCat.Title}</a>{Environment.NewLine}";
                    html += $"</div>{Environment.NewLine}";
                }

                var poemIds = poems.Select(p => p.Id).ToArray();

                var mainPoemSections = await context.GanjoorPoemSections.Where(s => poemIds.Contains(s.PoemId) && s.SectionType == PoemSectionType.WholePoem && s.VerseType == VersePoemSectionType.First).ToListAsync();

                foreach (var poem in poems)
                {
                    var section = mainPoemSections.Where(s => s.PoemId == poem.Id).FirstOrDefault();
                    if (section != null)
                        poem.RhymeLetters = section.RhymeLetters;
                }

                if (poems.Count > 0)
                {
                    if (options == GanjoorTOC.Analyse)
                    {
                        if (poems.Where(p => !string.IsNullOrEmpty(p.RhymeLetters)).Count() * 100 / poems.Count > 50)
                        {
                            options = GanjoorTOC.AlphabeticWithFirstVerse;
                        }
                        else
                        {
                            options = GanjoorTOC.TitlesAndFirstVerse;
                        }
                    }
                }

                bool separatorForInlineSearch = true;
                List<string> foundLastChars = new List<string>();
                if
                    (
                    options == GanjoorTOC.AlphabeticWithFirstCouplet
                    ||
                    options == GanjoorTOC.AlphabeticWithFirstVerse
                    ||
                    options == GanjoorTOC.AlphabeticWithSecondVerse
                    ||
                    options == GanjoorTOC.AlphabeticWithFirstCoupletNotSorted
                    ||
                    options == GanjoorTOC.AlphabeticWithFirstVerseNotSorted
                    ||
                    options == GanjoorTOC.AlphabeticWithSecondVerseNotSorted
                    )
                {
                    var taggedPoems = poems.Where(p => !string.IsNullOrEmpty(p.RhymeLetters)).ToArray();
                    if (taggedPoems.Length > 0)
                    {
                        separatorForInlineSearch = false;
                        html += $"<div class=\"notice\"><p>فهرست شعرها به ترتیب آخر حرف قافیه گردآوری شده است. برای پیدا کردن یک شعر کافی است حرف آخر قافیهٔ آن را در نظر بگیرید تا بتوانید آن  را پیدا کنید.</p>{Environment.NewLine}";
                        var randomPoem = taggedPoems[new Random(DateTime.Now.Millisecond).Next(taggedPoems.Length)];
                        var randomPoemVerses = await context.GanjoorVerses.AsNoTracking().Where(p => p.PoemId == randomPoem.Id).OrderBy(v => v.VOrder).ToArrayAsync();
                        if (randomPoemVerses.Length > 1)
                        {
                            string versePosition = options == GanjoorTOC.AlphabeticWithFirstVerse || options == GanjoorTOC.AlphabeticWithFirstVerseNotSorted ? "اول" : "دوم";
                            string sampleVerse = options == GanjoorTOC.AlphabeticWithFirstVerse || options == GanjoorTOC.AlphabeticWithFirstVerseNotSorted ? randomPoemVerses[0].Text : randomPoemVerses[1].Text;
                            html += $"<p>مثلاً برای پیدا کردن شعری که مصرع «<em>{sampleVerse}</em>» مصرع {versePosition} یکی از بیتهای آن است باید شعرهایی را نگاه کنید که آخر حرف قافیهٔ آنها «<em><a href=\"#{ GPersianTextSync.UniquelyFarglisize(randomPoem.RhymeLetters.Substring(randomPoem.RhymeLetters.Length - 1)) }\">{randomPoem.RhymeLetters.Substring(randomPoem.RhymeLetters.Length - 1)}</a></em>» است.</p>{Environment.NewLine}";
                        }

                        html += $"</div>{Environment.NewLine}";

                        html += $"<h3><a id=\"index\">حرف آخر قافیه</a></h3>{Environment.NewLine}";
                        string lastChar = "";

                        foreach (var poem in taggedPoems)
                        {
                            string poemLastChar = poem.RhymeLetters.Substring(poem.RhymeLetters.Length - 1);
                            if (poemLastChar == "!")
                                continue;
                            if (poemLastChar != lastChar)
                            {
                                if (foundLastChars.IndexOf(poemLastChar) == -1)
                                {
                                    foundLastChars.Add(poemLastChar);
                                }
                            }
                        }
                        if(
                            options == GanjoorTOC.AlphabeticWithFirstCouplet
                            ||
                            options == GanjoorTOC.AlphabeticWithFirstVerse
                            ||
                            options == GanjoorTOC.AlphabeticWithSecondVerse
                          )
                        {
                            var fa = new CultureInfo("fa-IR");
                            if (foundLastChars.Contains("و") && foundLastChars.Contains("ی") && foundLastChars.IndexOf("ه") == (foundLastChars.IndexOf("و") - 1))
                            {
                                foundLastChars.Sort((a, b) => a == "ه" && b == "و" ? -1 : a == "و" && b == "ه" ? 1 : fa.CompareInfo.Compare(a, b));
                            }
                            else
                            {
                                foundLastChars.Sort((a, b) => fa.CompareInfo.Compare(a, b));
                            }
                        }
                        
                        
                        foreach (var poemLastChar in foundLastChars)
                        {
                            string rep = poemLastChar == "ا" ? "الف" : poemLastChar;
                            html += $"<a href=\"#{GPersianTextSync.UniquelyFarglisize(poemLastChar)}\"><div class=\"circled-number\">{rep}</div></a>";
                            lastChar = poemLastChar;
                        }
                    }
                }

                if (poems.Count > 0)
                {
                    html += $"<div class=\"clear-both\">{Environment.NewLine}";
                    html += $"<input type=\"text\" id=\"findpoet\" placeholder=\"جستجو در عناوین\" size=\"35\" value=\"\" oninput=\"onInlineSearch(this.value, 'found-items', 'poem-excerpt')\">{Environment.NewLine}";
                    html += $"<div class=\"spacer\" id=\"found-items\"></div>{Environment.NewLine}";
                    html += $"</div>{Environment.NewLine}";

                    if (separatorForInlineSearch)
                    {
                        html += $"<br>{Environment.NewLine}";
                    }
                }

                string waitingForChar = foundLastChars.Count == 0 ? "!@#" : foundLastChars[0];
                int nLastIndex = 0;
                foreach (var poem in poems)
                {
                    if
                    (
                    options == GanjoorTOC.AlphabeticWithFirstCouplet
                    ||
                    options == GanjoorTOC.AlphabeticWithFirstVerse
                    ||
                    options == GanjoorTOC.AlphabeticWithSecondVerse
                     ||
                    options == GanjoorTOC.AlphabeticWithFirstCoupletNotSorted
                    ||
                    options == GanjoorTOC.AlphabeticWithFirstVerseNotSorted
                    ||
                    options == GanjoorTOC.AlphabeticWithSecondVerseNotSorted
                    )
                    {
                        if (!string.IsNullOrEmpty(poem.RhymeLetters))
                        {
                            string poemLast = poem.RhymeLetters.Substring(poem.RhymeLetters.Length - 1);
                            if (poemLast == waitingForChar)
                            {
                                string rep = poemLast == "ا" ? "الف" : poemLast;
                                html += $"<div class=\"part-title-block\" id=\"{GPersianTextSync.UniquelyFarglisize(poemLast)}\"><a href=\"#index\">{rep}</a></div>{Environment.NewLine}";

                                nLastIndex++;
                                if (nLastIndex < foundLastChars.Count)
                                {
                                    waitingForChar = foundLastChars[nLastIndex];
                                }
                                else
                                {
                                    waitingForChar = "!@#";
                                }
                            }
                        }
                    }

                    html += $"<p class=\"poem-excerpt\" data-value=\"{@poem.Title} { await _AdditionalTableOfContentsAnchorTitleForPoem("", context, poem, options)}\"><a href=\"{poem.FullUrl}\">{poem.Title}</a>";

                    html = await _AdditionalTableOfContentsAnchorTitleForPoem(html, context, poem, options);

                    html += $"</p>{Environment.NewLine}";
                }
                return new RServiceResult<string>(html);
            }
            catch (Exception exp)
            {
                return new RServiceResult<string>(null, exp.ToString());
            }
        }

        private async Task _GeneratingSubCatsTOC(Guid userId, RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job, int catId)
        {
            foreach (var cat in await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == catId).ToListAsync())
            {
                await jobProgressServiceEF.UpdateJob(job.Id, cat.Id);
                var page = await context.GanjoorPages.Where(p => p.FullUrl == cat.FullUrl).SingleAsync();

                context.GanjoorPageSnapshots.Add
                           (
                           new GanjoorPageSnapshot()
                           {
                               GanjoorPageId = page.Id,
                               MadeObsoleteByUserId = userId,
                               HtmlText = page.HtmlText,
                               Note = "تولید گروهی فهرستهای زیربخشها",
                               RecordDate = DateTime.Now
                           }
                           );

                page.HtmlText = (await _GenerateTableOfContents(context, cat.Id, GanjoorTOC.TitlesAndFirstCouplet)).Result;
                context.GanjoorPages.Update(page);
                await context.SaveChangesAsync();

                await _GeneratingSubCatsTOC(userId, context, jobProgressServiceEF, job, cat.Id);
            }
        }

        /// <summary>
        /// start generating sub cats TOC
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartGeneratingSubCatsTOC(Guid userId, int catId)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                var job = (await jobProgressServiceEF.NewJob($"GeneratingSubCatsTOC {catId}", "Query data")).Result;
                                try
                                {
                                    await _GeneratingSubCatsTOC(userId, context, jobProgressServiceEF, job, catId);
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


    }
}