using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
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
        /// update related poems info (after metreId or rhyme for one of these poems changes)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metreId"></param>
        /// <param name="rhyme"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _UpdateRelatedPoems(RMuseumDbContext context, int metreId, string rhyme)
        {
            try
            {
                var poemIds = await context.GanjoorPoems.AsNoTracking().Where(p => p.GanjoorMetreId == metreId && p.RhymeLetters == rhyme).Select(p => p.Id).ToListAsync();
                foreach (var id in poemIds)
                {
                    await _UpdatePoemRelatedPoemsInfoNoSaveChanges(context, id);
                }
                await context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            

        }
        private async Task _UpdatePoemRelatedPoemsInfoNoSaveChanges(RMuseumDbContext context, int poemId)
        {
            var poem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == poemId).SingleAsync();
            var oldRelations = await context.GanjoorCachedRelatedPoems.Where(r => r.PoemId == poemId).ToListAsync();
            context.GanjoorCachedRelatedPoems.RemoveRange(oldRelations);

            if (poem.GanjoorMetreId == null || string.IsNullOrEmpty(poem.RhymeLetters))
                return;

            int metreId = (int)poem.GanjoorMetreId;
            string rhyme = poem.RhymeLetters;

            var relatedPoems = await context.GanjoorPoems.AsNoTracking().Include(p => p.Cat).ThenInclude(c => c.Poet)
                .Where(p =>
                        p.GanjoorMetreId == metreId
                        &&
                        p.RhymeLetters == rhyme
                        &&
                        p.Id != poemId
                        )
                .OrderBy(p => p.Cat.Poet.BirthYearInLHijri).ThenBy(p => p.Cat.PoetId).ToListAsync();

            List<GanjoorCachedRelatedPoem> ganjoorCachedRelatedPoems = new List<GanjoorCachedRelatedPoem>();
            int r = 0;
            int prePoetId = -1;
            foreach (var relatedPoem in relatedPoems)
            {
                if(prePoetId != relatedPoem.Cat.PoetId)
                {
                    r++;

                    GanjoorCachedRelatedPoem newRelatedPoem = new GanjoorCachedRelatedPoem()
                    {
                        PoemId = poemId,
                        PoetId = relatedPoem.Cat.PoetId,
                        RelationOrder = r,
                        PoetName = relatedPoem.Cat.Poet.Nickname,
                        PoetImageUrl = $"/api/ganjoor/poet/image{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == relatedPoem.Cat.PoetId).AsNoTracking().SingleAsync()).FullUrl}.gif",
                        FullTitle = relatedPoem.FullTitle,
                        FullUrl = relatedPoem.FullTitle,
                        PoetMorePoemsLikeThisCount = 0,
                        HtmlExcerpt = GetPoemHtmlExcerpt(relatedPoem.HtmlText)
                    };

                    ganjoorCachedRelatedPoems.Add(newRelatedPoem);

                    prePoetId = relatedPoem.Cat.PoetId;
                }
                else
                {
                    ganjoorCachedRelatedPoems[ganjoorCachedRelatedPoems.Count - 1].PoetMorePoemsLikeThisCount++;
                }
            }
            if(ganjoorCachedRelatedPoems.Count > 0)
            {
                context.GanjoorCachedRelatedPoems.AddRange(ganjoorCachedRelatedPoems);
            }
        }

        /// <summary>
        /// get an excerpt for the poem
        /// </summary>
        /// <param name="poemHtml"></param>
        /// <returns></returns>
        public static string GetPoemHtmlExcerpt(string poemHtml)
        {
            while (poemHtml.IndexOf("id=\"bn") != -1)
            {
                int idxbn1 = poemHtml.IndexOf(" id=\"bn");
                int idxbn2 = poemHtml.IndexOf("\"", idxbn1 + " id=\"bn".Length);
                poemHtml = poemHtml.Substring(0, idxbn1) + poemHtml.Substring(idxbn2 + 1);
            }

            poemHtml = poemHtml.Replace("<div class=\"b\">", "").Replace("<div class=\"b2\">", "").Replace("<div class=\"m1\">", "").Replace("<div class=\"m2\">", "").Replace("</div>", "");

            int index = poemHtml.IndexOf("<p>");
            int count = 0;
            while (index != -1 && count < 5)
            {
                index = poemHtml.IndexOf("<p>", index + 1);
                count++;
            }

            if (index != -1)
            {
                poemHtml = poemHtml.Substring(0, index);
                poemHtml += "<p>[...]</p>";
            }

            return poemHtml;
        }

        /// <summary>
        /// start generating related poems info
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartGeneratingRelatedPoemsInfo()
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
                                    var job = (await jobProgressServiceEF.NewJob("GeneratingRelatedPoemsInfo", "Query")).Result;

                                    try
                                    {

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Query");

                                        var poemIds = await context.GanjoorPoems.AsNoTracking().Where(p => !string.IsNullOrEmpty(p.RhymeLetters) && p.GanjoorMetreId != null).Select(p => p.Id).ToListAsync();

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Updating Related Poems");
                                        int percent = 0;
                                        for (int i = 0; i < poemIds.Count; i++)
                                        {
                                            if (i * 100 / poemIds.Count > percent)
                                            {
                                                percent++;
                                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                                            }

                                            await _UpdatePoemRelatedPoemsInfoNoSaveChanges(context, poemIds[i]);

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