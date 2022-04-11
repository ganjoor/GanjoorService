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
        /// get a poem related poems
        /// </summary>
        /// <param name="id">poem id</param>
        /// <param name="skip"></param>
        /// <param name="itemsCount">if sent 0 or less returns all items</param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCachedRelatedPoem[]>> GetRelatedPoems(int id, int skip, int itemsCount)
        {
            var source =
                 _context.GanjoorCachedRelatedPoems
                         .Where(r => r.PoemId == id)
                         .OrderBy(r => r.RelationOrder);

            if (itemsCount <= 0)
                return new RServiceResult<GanjoorCachedRelatedPoem[]>(await source.ToArrayAsync());
            return new RServiceResult<GanjoorCachedRelatedPoem[]>
                (
                await source.Skip(skip).Take(itemsCount).ToArrayAsync()
                );
        }


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
                var poemIds = await context.GanjoorPoemSections.AsNoTracking().Where(section => section.GanjoorMetreId == metreId && section.RhymeLetters == rhyme).Select(p => p.PoemId).ToListAsync();
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

            var section = await context.GanjoorPoemSections.AsNoTracking().Where(
                section => section.PoemId == poemId 
                && section.SectionType != PoemSectionType.Couplet && section.SectionType != PoemSectionType.Band
                && section.GanjoorMetreId != null && !string.IsNullOrEmpty(section.RhymeLetters)
                ).OrderBy(section => section.VerseType).FirstOrDefaultAsync();

            if (section == null)
                return;

            int metreId = (int)section.GanjoorMetreId;
            string rhyme = section.RhymeLetters;


            var relatedSections = await context.GanjoorPoemSections.AsNoTracking().Include(section => section.Poem).Include(section => section.Poet)
                .Where(section =>
                        section.GanjoorMetreId == metreId
                        &&
                        section.RhymeLetters == rhyme
                        &&
                        section.PoemId != poemId
                        )
                .OrderBy(p => p.Poet.BirthYearInLHijri).ThenBy(p => p.PoetId).ToListAsync();

            List<GanjoorCachedRelatedPoem> ganjoorCachedRelatedPoems = new List<GanjoorCachedRelatedPoem>();
            int r = 0;
            int prePoetId = -1;
            foreach (var relatedSection in relatedSections)
            {
                if(prePoetId != relatedSection.PoetId)
                {
                    r++;

                    GanjoorCachedRelatedPoem newRelatedPoem = new GanjoorCachedRelatedPoem()
                    {
                        PoemId = poemId,
                        PoetId = (int)relatedSection.PoetId,
                        RelationOrder = r,
                        PoetName = relatedSection.Poet.Nickname,
                        PoetImageUrl = $"/api/ganjoor/poet/image{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == relatedSection.PoetId).AsNoTracking().SingleAsync()).FullUrl}.gif",
                        FullTitle = relatedSection.Poem.FullTitle,
                        FullUrl = relatedSection.Poem.FullUrl,
                        PoetMorePoemsLikeThisCount = 0,
                        HtmlExcerpt = GetPoemHtmlExcerpt(relatedSection.HtmlText)
                    };

                    ganjoorCachedRelatedPoems.Add(newRelatedPoem);

                    prePoetId = (int)relatedSection.PoetId;
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
        /// <param name="regenerate"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartGeneratingRelatedPoemsInfo(bool regenerate)
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
                                    int percent = 0;
                                    try
                                    {

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Query");

                                        var poemIds = await context.GanjoorPoems.AsNoTracking().Where(p => !string.IsNullOrEmpty(p.RhymeLetters) && p.GanjoorMetreId != null).Select(p => p.Id).ToListAsync();

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Updating Related Poems");
                                        
                                        for (int i = 0; i < poemIds.Count; i++)
                                        {
                                            if(!regenerate)
                                            {
                                                if (await context.GanjoorCachedRelatedPoems.AnyAsync(r => r.PoemId == poemIds[i]))
                                                    continue;
                                            }
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
                                        await jobProgressServiceEF.UpdateJob(job.Id, percent, "", false, exp.ToString());
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