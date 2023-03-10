using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        private async Task _FindCategoryPoemsRhythmsInternal(int catId, bool retag, string rhythm)
        {
            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
            {
                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                var job = (await jobProgressServiceEF.NewJob($"FindCategoryPoemsRhythms Cat {catId}", "Query data")).Result;
                try
                {
                    var metres = await context.GanjoorMetres.OrderBy(m => m.Rhythm).AsNoTracking().ToArrayAsync();
                    var rhythms = metres.Select(m => m.Rhythm).ToArray();

                    GanjoorMetre preDeterminedMetre = string.IsNullOrEmpty(rhythm) ? null : metres.Where(m => m.Rhythm == rhythm).Single();

                    var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).ToListAsync();

                    int i = 0;
                    List<Tuple<int, string>> updateList = new List<Tuple<int, string>>();
                    using (HttpClient httpClient = new HttpClient())
                    {
                        
                        foreach (var poem in poems)
                        {

                            var sections = await context.GanjoorPoemSections.Where(p => p.PoemId == poem.Id).ToListAsync();
                            foreach (var section in sections.Where(s => s.GanjoorMetreRefSectionIndex == null).ToList())
                            {
                                if (retag || section.GanjoorMetreId == null)
                                {
                                    
                                    if (preDeterminedMetre == null)
                                    {
                                        var res = await _FindSectionRhythm(section, context, httpClient, rhythms);
                                        if (!string.IsNullOrEmpty(res.Result))
                                        {
                                            section.GanjoorMetreId = metres.Where(m => m.Rhythm == res.Result).Single().Id;
                                            context.GanjoorPoemSections.Update(section);
                                        }
                                    }
                                    else
                                    {
                                        section.GanjoorMetreId = preDeterminedMetre.Id;
                                        context.GanjoorPoemSections.Update(section);
                                    }

                                    if(section.GanjoorMetreId != null)
                                    {
                                        var dependentSections = sections.Where(s => s.GanjoorMetreRefSectionIndex == section.Index).ToList();
                                        foreach (var dsection in dependentSections)
                                        {
                                            dsection.GanjoorMetreId = section.GanjoorMetreId;
                                            context.GanjoorPoemSections.Update(dsection);
                                        }
                                    }

                                    if (section.GanjoorMetreId != null && !string.IsNullOrEmpty(section.RhymeLetters))
                                    {
                                        if(!updateList.Any(u  => u.Item1  == section.GanjoorMetreId && u.Item2 == section.RhymeLetters))
                                        {
                                            updateList.Add(new Tuple<int, string>((int)section.GanjoorMetreId, section.RhymeLetters));
                                        }

                                    }

                                    if (section.GanjoorMetreId != null)
                                    {
                                        var dependentSections = sections.Where(s => s.GanjoorMetreRefSectionIndex == section.Index).ToList();
                                        foreach (var dsection in dependentSections)
                                        {
                                            if(dsection.GanjoorMetreId != null && !string.IsNullOrEmpty(dsection.RhymeLetters))
                                            {
                                                if (!updateList.Any(u => u.Item1 == dsection.GanjoorMetreId && u.Item2 == dsection.RhymeLetters))
                                                {
                                                    updateList.Add(new Tuple<int, string>((int)dsection.GanjoorMetreId, dsection.RhymeLetters));
                                                }

                                            }
                                        }
                                    }

                                    await jobProgressServiceEF.UpdateJob(job.Id, i++);
                                }
                            }

                            
                        }
                    }

                    foreach (var item in updateList)
                    {
                        await _UpdateRelatedSections(context, item.Item1, item.Item2, jobProgressServiceEF, job);
                    }

                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                }
                catch (Exception exp)
                {
                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                }
            }
        }

        /// <summary>
        /// find category poem rhymes
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="retag"></param>
        /// <param name="rhythm"></param>
        /// <returns></returns>
        public RServiceResult<bool> FindCategoryPoemsRhythms(int catId, bool retag, string rhythm = "")
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            await _FindCategoryPoemsRhythmsInternal(catId, retag, rhythm);
                        });

            return new RServiceResult<bool>(true);
        }

      
 
        /// <summary>
        /// directly insert generated TOC
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="userId"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DirectInsertGeneratedTableOfContents(int catId, Guid userId, GanjoorTOC options)
        {
            return await _DirectInsertGeneratedTableOfContents(_context, catId, userId, options);
        }

        /// <summary>
        /// generate category TOC
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> GenerateTableOfContents(int catId, GanjoorTOC options)
        {
            return await _GenerateTableOfContents(_context, catId, options);
        }
      

        /// <summary>
        /// make plain text
        /// </summary>
        /// <param name="verses"></param>
        /// <returns></returns>
        private static string PreparePlainText(List<GanjoorVerse> verses)
        {
            string plainText = "";
            foreach (GanjoorVerse verse in verses)
            {
                plainText += $"{LanguageUtils.MakeTextSearchable(verse.Text)}{Environment.NewLine}";//replace zwnj with space
            }
            return plainText.Trim();
        }

        /// <summary>
        /// separate verses in poem.PlainText with  Environment.NewLine instead of SPACE
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public RServiceResult<bool> RegerneratePoemsPlainText(int catId)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
            (
            async token =>
            {
                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                {
                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                    var job = (await jobProgressServiceEF.NewJob($"RegerneratePoemsPlainText {catId}", "Query data")).Result;

                    try
                    {
                        var poems = catId == 0 ? await context.GanjoorPoems.ToArrayAsync() : await context.GanjoorPoems.Where(p => p.CatId == catId).ToArrayAsync();

                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Updating PlainText/Poem Html {catId}");

                        int percent = 0;
                        for (int i = 0; i < poems.Length; i++)
                        {
                            if (i * 100 / poems.Length > percent)
                            {
                                percent++;
                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                            }

                            var poem = poems[i];

                            var verses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).ToListAsync();

                            poem.PlainText = PreparePlainText(verses);
                            poem.HtmlText = PrepareHtmlText(verses);
                        }

                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Finalizing PlainText/Poem Html {catId}");

                        context.GanjoorPoems.UpdateRange(poems);

                        await context.SaveChangesAsync();

                        await jobProgressServiceEF.UpdateJob(job.Id, 50, $"Updating pages HTML {catId}");

                        //the following line always gets timeout, so it is being replaced by a loop
                        //await context.Database.ExecuteSqlRawAsync(
                        //    "UPDATE p SET p.HtmlText = (SELECT poem.HtmlText FROM GanjoorPoems poem WHERE poem.Id = p.Id) FROM GanjoorPages p WHERE p.GanjoorPageType = 3 ");

                        foreach (var poem in poems)
                        {
                            var page = await context.GanjoorPages.Where(p => p.Id == poem.Id).SingleAsync();
                            page.HtmlText = poem.HtmlText;
                            context.GanjoorPages.Update(page);
                        }
                        await context.SaveChangesAsync();

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

        /// <summary>
        /// examine site pages for broken links
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> HealthCheckContents()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
            (
            async token =>
            {
                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                {
                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                    var job = (await jobProgressServiceEF.NewJob("HealthCheckContents", "Query data")).Result;

                    try
                    {
                        var pages = await context.GanjoorPages.ToArrayAsync();

                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Examining Pages");

                        var previousErrors = await context.GanjoorHealthCheckErrors.ToArrayAsync();
                        context.RemoveRange(previousErrors);
                        await context.SaveChangesAsync();
                        int percent = 0;
                        for (int i = 0; i < pages.Length; i++)
                        {
                            if (i * 100 / pages.Length > percent)
                            {
                                percent++;
                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                            }

                            var hrefs = pages[i].HtmlText.Split(new[] { "href=\"" }, StringSplitOptions.RemoveEmptyEntries).Where(o => o.StartsWith("http")).Select(o => o.Substring(0, o.IndexOf("\"")));

                            foreach (string url in hrefs)
                            {
                                if (url == "https://ganjoor.net" || url == "https://ganjoor.net/" || url.IndexOf("https://ganjoor.net/vazn/?") == 0 || url.IndexOf("https://ganjoor.net/simi/?v") == 0)
                                    continue;
                                if (url.IndexOf("http://ganjoor.net") == 0)
                                {
                                    context.GanjoorHealthCheckErrors.Add
                                    (
                                        new GanjoorHealthCheckError()
                                        {
                                            ReferrerPageUrl = pages[i].FullUrl,
                                            TargetUrl = url,
                                            BrokenLink = false,
                                            MulipleTargets = false
                                        }
                                     );

                                    await context.SaveChangesAsync();
                                }
                                else
                                if (url.IndexOf("https://ganjoor.net") == 0)
                                {
                                    var testUrl = url.Substring("https://ganjoor.net".Length);
                                    if (testUrl[testUrl.Length - 1] == '/')
                                        testUrl = testUrl.Substring(0, testUrl.Length - 1);
                                    var pageCount = await context.GanjoorPages.Where(p => p.FullUrl == testUrl).CountAsync();
                                    if (pageCount != 1)
                                    {
                                        context.GanjoorHealthCheckErrors.Add
                                     (
                                         new GanjoorHealthCheckError()
                                         {
                                             ReferrerPageUrl = pages[i].FullUrl,
                                             TargetUrl = url,
                                             BrokenLink = pageCount == 0,
                                             MulipleTargets = pageCount != 0
                                         }
                                      );

                                        await context.SaveChangesAsync();
                                    }
                                }
                            }
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

        private async Task<string> _ProcessCommentHtml(string commentText, RMuseumDbContext context)
        {
            string[] allowedTags = new string[]
            {
                "p",
                "a",
                "br",
                "b",
                "i",
                "strong",
                "img"
            };
            if (commentText.IndexOf("<") != -1)
            {
                int openTagIndex = commentText.IndexOf('<');
                while (openTagIndex != -1)
                {
                    int closeOpenningTagIndex = commentText.IndexOf('>', openTagIndex + 1);
                    if (closeOpenningTagIndex == -1) //an unclosed tag
                    {
                        if (commentText.IndexOf(' ', openTagIndex + 1) != -1)
                        {
                            commentText = commentText.Substring(0, openTagIndex) + commentText.Substring(commentText.IndexOf(' ', openTagIndex + 1));
                        }
                        else
                        {
                            commentText = commentText.Substring(0, openTagIndex);
                        }
                    }
                    else
                    {
                        int anotherOpenTagInBetweenIndex = commentText.IndexOf('<', openTagIndex + 1);
                        if (anotherOpenTagInBetweenIndex != -1 && anotherOpenTagInBetweenIndex < closeOpenningTagIndex)
                        {
                            commentText = commentText.Substring(0, openTagIndex) + commentText.Substring(anotherOpenTagInBetweenIndex);
                        }
                        else
                        {
                            int tagTypeCloseIndex = closeOpenningTagIndex;
                            int spaceAfterOpenningTagIndex = commentText.IndexOf(' ', openTagIndex + 1);
                            if (spaceAfterOpenningTagIndex != -1 && spaceAfterOpenningTagIndex < tagTypeCloseIndex)
                                tagTypeCloseIndex = spaceAfterOpenningTagIndex;


                            string tagType = commentText.Substring(openTagIndex + 1, tagTypeCloseIndex - openTagIndex - 1).ToLower();
                            tagType = tagType.Replace("/", "");//include close tags
                            if (tagType.Length == 0)
                            {
                                if (closeOpenningTagIndex == commentText.Length - 1)
                                    commentText = commentText.Substring(0, openTagIndex);
                                else
                                    commentText = commentText.Substring(0, openTagIndex) + commentText.Substring(closeOpenningTagIndex + 1);
                            }
                            else
                            {
                                if (!allowedTags.Contains(tagType))
                                {
                                    commentText = commentText.Substring(0, openTagIndex) + commentText.Substring(closeOpenningTagIndex + 1);
                                    commentText = commentText.Replace($"</{tagType}>", "");
                                }
                            }
                        }
                    }


                    openTagIndex = commentText.IndexOf("<", openTagIndex + 1);
                }
            }

            if (commentText.IndexOf("href=") == -1 && commentText.IndexOf("http") != -1)
            {
                commentText = _Linkify(commentText);
            }
            int index = commentText.IndexOf("href=");
            while (index != -1)
            {
                index += "href=\"".Length;
                commentText = commentText.Replace("'", "\"");
                if (commentText.IndexOf("\"", index) != -1)
                {
                    int closeIndex = commentText.IndexOf("\"", index);
                    if (closeIndex == -1)
                    {
                        continue;
                    }
                    string url = commentText.Substring(index, closeIndex - index);
                    closeIndex = commentText.IndexOf(">", index);
                    if (closeIndex != -1 && commentText.IndexOf("</a>", closeIndex) != -1)
                    {
                        closeIndex += ">".Length;
                        string urlText = commentText.Substring(closeIndex, commentText.IndexOf("</a>", closeIndex) - closeIndex);
                        if (urlText == url)
                        {
                            bool textFixed = false;
                            if (urlText.IndexOf("http://ganjoor.net") == 0 || urlText.IndexOf("https://ganjoor.net") == 0)
                            {
                                urlText = urlText.Replace("http://ganjoor.net", "").Replace("https://ganjoor.net", "");
                                int coupletNumber = -1;
                                if (urlText.IndexOf("#bn") != -1)
                                {
                                    int coupletStartIndex = urlText.IndexOf("#bn") + "#bn".Length;
                                    if (int.TryParse(urlText.Substring(coupletStartIndex), out coupletNumber))
                                    {
                                        urlText = urlText.Substring(0, urlText.IndexOf("#bn"));
                                    }
                                }
                                if (urlText.Length > 0 && urlText[urlText.Length - 1] == '/')
                                    urlText = urlText.Substring(0, urlText.Length - 1);
                                var page = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl == urlText).FirstOrDefaultAsync();
                                if (page != null)
                                {
                                    if (coupletNumber != -1)
                                    {
                                        string coupletSummary = "";
                                        int coupletIndex = coupletNumber - 1;
                                        var verses = await _context.GanjoorVerses.Where(v => v.PoemId == page.Id).OrderBy(v => v.VOrder).ToListAsync();
                                        int cIndex = -1;
                                        for (int i = 0; i < verses.Count; i++)
                                        {
                                            if (verses[i].VersePosition != VersePosition.Left && verses[i].VersePosition != VersePosition.CenteredVerse2)
                                                cIndex++;
                                            if (cIndex == coupletIndex)
                                            {
                                                coupletSummary = verses[i].Text;
                                                if (verses[i].VersePosition == VersePosition.Right)
                                                {
                                                    if (i < verses.Count - 1)
                                                    {
                                                        coupletSummary += $" {verses[i + 1].Text}";
                                                    }
                                                }
                                                if (verses[i].VersePosition == VersePosition.CenteredVerse1)
                                                {
                                                    if (i < verses.Count - 1)
                                                    {
                                                        if (verses[i + 1].VersePosition == VersePosition.CenteredVerse2)
                                                        {
                                                            coupletSummary += $" {verses[i + 1].Text}";
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(coupletSummary))
                                        {
                                            coupletSummary = _CutSummary(coupletSummary);
                                            commentText = commentText.Substring(0, closeIndex) + page.FullTitle + " » " + coupletSummary + commentText.Substring(commentText.IndexOf("</a>", closeIndex));
                                            textFixed = true;
                                        }
                                        else
                                            coupletNumber = -1;
                                    }
                                    if (coupletNumber == -1)
                                    {
                                        commentText = commentText.Substring(0, closeIndex) + page.FullTitle + commentText.Substring(commentText.IndexOf("</a>", closeIndex));
                                        textFixed = true;
                                    }
                                }

                            }
                            if (!textFixed)
                                commentText = commentText.Substring(0, closeIndex) + "پیوند به وبگاه بیرونی" + commentText.Substring(commentText.IndexOf("</a>", closeIndex));
                        }
                    }
                }
                index = commentText.IndexOf("href=\"", index);
            }
            return commentText;
        }

        private string _Linkify(string SearchText)
        {
            if (SearchText.IndexOf("href") != -1)
                return SearchText;
            int linkIndex = SearchText.IndexOf("http");
            while (linkIndex != -1)
            {
                int linkEndIndex = SearchText.IndexOfAny(new char[] { '\r', '\n', '<', ' ' }, linkIndex);
                if (linkEndIndex == -1)
                    linkEndIndex = SearchText.Length - 1;
                if (linkEndIndex != -1)
                {
                    string link = SearchText.Substring(linkIndex, linkEndIndex - linkIndex);
                    SearchText
                        =
                        SearchText.Substring(0, linkIndex)
                        +
                        "<a href=\""
                        +
                        link
                        +
                        "\" rel=\"nofollow\">"
                        +
                        link
                        +
                        "</a>"
                        +
                        SearchText.Substring(linkEndIndex);
                    linkIndex =
                        (
                        SearchText.Substring(0, linkIndex)
                        +
                        "<a href=\""
                        +
                        link
                        +
                        "\" rel=\"nofollow\">"
                        +
                        link
                        +
                        "</a>"
                        ).Length;
                    linkIndex = SearchText.IndexOf("http", linkIndex);
                }
                else
                    linkIndex = SearchText.IndexOf("http", linkIndex + "http".Length);
            }
            return SearchText;
        }

        /// <summary>
        /// examine comments for long links
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> FindAndFixLongUrlsInComments()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
            (
            async token =>
            {
                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                {
                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                    var job = (await jobProgressServiceEF.NewJob("FindAndFixLongUrlsInComments", "Query data")).Result;

                    try
                    {
                        var comments = await context.GanjoorComments.Where(c => c.HtmlComment.Contains("href=")).ToArrayAsync();

                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Examining {comments.Length} Comments");

                        int percent = 0;
                        for (int i = 0; i < comments.Length; i++)
                        {
                            if (i * 100 / comments.Length > percent)
                            {
                                percent++;
                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                            }

                            var comment = comments[i];

                            string commentText = await _ProcessCommentHtml(comment.HtmlComment, context);

                            if (commentText != comment.HtmlComment)
                            {
                                comment.HtmlComment = commentText;
                                context.Update(comment);
                                await context.SaveChangesAsync();
                            }
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

        /// <summary>
        /// start filling poems couplet indices
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartFillingPoemsCoupletIndices()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
            (
            async token =>
            {
                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                {
                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                    var job = (await jobProgressServiceEF.NewJob("FillingPoemsCoupletIndices", "Query data")).Result;

                    try
                    {
                        var poemIds = await context.GanjoorPoems.AsNoTracking().Select(p => p.Id).ToListAsync();


                        int percent = 0;
                        for (int i = 0; i < poemIds.Count; i++)
                        {
                            if (i * 100 / poemIds.Count > percent)
                            {
                                percent++;
                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                            }

                            await _FillPoemCoupletIndices(context, poemIds[i]);
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

        /// <summary>
        /// regenerate poem full titles to fix an old bug
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> RegeneratePoemsFullTitles()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
            (
            async token =>
            {
                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                {
                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                    var job = (await jobProgressServiceEF.NewJob("RegenratePoemsFullTitles", "Query data")).Result;
                    int percent = 0;
                    try
                    {
                        var poemIds = await context.GanjoorPoems.AsNoTracking().Select(p => p.Id).ToListAsync();
                        
                        for (int i = 0; i < poemIds.Count; i++)
                        {
                            if (i * 100 / poemIds.Count > percent)
                            {
                                percent++;
                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                            }

                            var poem = await context.GanjoorPoems.Where(p => p.Id == poemIds[i]).SingleOrDefaultAsync();
                            var catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == poem.CatId).SingleOrDefaultAsync();
                            if(catPage == null)
                            {
                                catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.CatId == poem.CatId).SingleOrDefaultAsync();
                            }
                            var title = $"{catPage.FullTitle} » {poem.Title}";
                            if(title != poem.FullTitle)
                            {
                                poem.FullTitle = title;
                                context.Update(poem);
                            }                          
                            var page = await context.GanjoorPages.Where(p => p.Id == poem.Id).SingleOrDefaultAsync();
                            if(title != page.FullTitle)
                            {
                                page.FullTitle = title;
                                context.Update(page);
                            }
                        }

                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                    }
                    catch (Exception exp)
                    {
                        await jobProgressServiceEF.UpdateJob(job.Id, percent, "", false, exp.ToString());
                    }

                }
            }
            );

            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// start finding rhymes for single couplets
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartFindingRhymesForSingleCouplets()
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
            (
            async token =>
            {
                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                {
                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                    var job = (await jobProgressServiceEF.NewJob("FindingRhymesForSingleCouplets", "Query data")).Result;

                    try
                    {
                        var sections = await context.GanjoorPoemSections.AsNoTracking().Include(s => s.GanjoorMetre).Where(s => s.SectionType == PoemSectionType.WholePoem && s.GanjoorMetre != null && (s.RhymeLetters.Length >= 30 || s.RhymeLetters.Length < 2)).ToListAsync();


                        int percent = 0;
                        for (int i = 0; i < sections.Count; i++)
                        {
                            if (i * 100 / sections.Count > percent)
                            {
                                percent++;
                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                            }

                            var res = await _FindSectionRhyme(context, sections[i].Id);
                            if(string.IsNullOrEmpty(res.ExceptionString))
                            {
                                if(!string.IsNullOrEmpty(res.Result.Rhyme) && res.Result.Rhyme != sections[i].RhymeLetters)
                                {
                                    var sectionTracked = await context.GanjoorPoemSections.Where(s => s.Id == sections[i].Id).SingleAsync();
                                    var oldRhyme = sectionTracked.RhymeLetters;
                                    sectionTracked.RhymeLetters = res.Result.Rhyme;
                                    context.Update(sectionTracked);
                                    await context.SaveChangesAsync();
                                    await _UpdateRelatedSections(context, (int)sectionTracked.GanjoorMetreId, oldRhyme, jobProgressServiceEF, job, percent);
                                    await _UpdateRelatedSections(context, (int)sectionTracked.GanjoorMetreId, res.Result.Rhyme, jobProgressServiceEF, job, percent);
                                }
                            }
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
    }
}