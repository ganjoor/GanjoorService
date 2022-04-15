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
        /// get a section related sections
        /// </summary>
        /// <param name="poemId">poem id</param>
        /// <param name="sectionIndex">section index</param>
        /// <param name="skip"></param>
        /// <param name="itemsCount">if sent 0 or less returns all items</param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorCachedRelatedSection[]>> GetRelatedSections(int poemId, int sectionIndex, int skip, int itemsCount)
        {
            var source =
                 _context.GanjoorCachedRelatedSections
                         .Where(r => r.PoemId == poemId && r.SectionIndex == sectionIndex)
                         .OrderBy(r => r.RelationOrder);

            if (itemsCount <= 0)
                return new RServiceResult<GanjoorCachedRelatedSection[]>(await source.ToArrayAsync());
            return new RServiceResult<GanjoorCachedRelatedSection[]>
                (
                await source.Skip(skip).Take(itemsCount).ToArrayAsync()
                );
        }

        /// <summary>
        /// update related sections info (after metreId or rhyme for one of these sections changes)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metreId"></param>
        /// <param name="rhyme"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _UpdateRelatedSections(RMuseumDbContext context, int metreId, string rhyme)
        {
            try
            {
                var sections = await context.GanjoorPoemSections.AsNoTracking().Where(section => section.GanjoorMetreId == metreId && section.RhymeLetters == rhyme).ToListAsync();
                foreach (var section in sections)
                {
                    await _UpdateSectionRelatedSectionsInfoNoSaveChanges(context, section);
                }
                await context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private async Task _UpdateSectionRelatedSectionsInfoNoSaveChanges(RMuseumDbContext context, GanjoorPoemSection section)
        {
            var oldRelations = await context.GanjoorCachedRelatedSections.Where(r => r.PoemId == section.PoemId && r.SectionIndex == section.Index).ToListAsync();
            context.GanjoorCachedRelatedSections.RemoveRange(oldRelations);

            int metreId = (int)section.GanjoorMetreId;
            string rhyme = section.RhymeLetters;


            var relatedSections = await context.GanjoorPoemSections.AsNoTracking().Include(section => section.Poem).Include(section => section.Poet)
                .Where(s =>
                        s.GanjoorMetreId == metreId
                        &&
                        s.RhymeLetters == rhyme
                        &&
                        s.Id != section.Id
                        )
                .OrderBy(p => p.Poet.BirthYearInLHijri).ThenBy(p => p.PoetId).ThenBy(p => p.SectionType).ToListAsync();

            List<GanjoorCachedRelatedSection> GanjoorCachedRelatedSections = new List<GanjoorCachedRelatedSection>();
            int r = 0;
            int prePoetId = -1;
            foreach (var relatedSection in relatedSections)
            {
                if (prePoetId != relatedSection.PoetId)
                {
                    r++;

                    GanjoorCachedRelatedSection newRelatedPoem = new GanjoorCachedRelatedSection()
                    {
                        PoemId = section.PoemId,
                        SectionIndex = section.Index,
                        PoetId = (int)relatedSection.PoetId,
                        RelationOrder = r,
                        PoetName = relatedSection.Poet.Nickname,
                        PoetImageUrl = $"/api/ganjoor/poet/image{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == relatedSection.PoetId).AsNoTracking().SingleAsync()).FullUrl}.gif",
                        FullTitle = relatedSection.Poem.FullTitle,
                        FullUrl = relatedSection.Poem.FullUrl,
                        PoetMorePoemsLikeThisCount = 0,
                        HtmlExcerpt = GetPoemHtmlExcerpt(relatedSection.HtmlText)
                    };

                    GanjoorCachedRelatedSections.Add(newRelatedPoem);

                    prePoetId = (int)relatedSection.PoetId;
                }
                else
                {
                    GanjoorCachedRelatedSections[GanjoorCachedRelatedSections.Count - 1].PoetMorePoemsLikeThisCount++;
                }
            }
            if (GanjoorCachedRelatedSections.Count > 0)
            {
                context.GanjoorCachedRelatedSections.AddRange(GanjoorCachedRelatedSections);
            }
        }

        /// <summary>
        /// start generating related sections info
        /// </summary>
        /// <param name="regenerate"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartGeneratingRelatedSectionsInfo(bool regenerate)
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
                                    var job = (await jobProgressServiceEF.NewJob("GeneratingRelatedSectionsInfo", "Query")).Result;
                                    int percent = 0;
                                    try
                                    {

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Query");

                                        var sections = await context.GanjoorPoemSections.AsNoTracking().Where(p => !string.IsNullOrEmpty(p.RhymeLetters) && p.GanjoorMetreId != null).ToListAsync();

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Updating Related Sections");

                                        for (int i = 0; i < sections.Count; i++)
                                        {
                                            if (!regenerate)
                                            {
                                                if (await context.GanjoorCachedRelatedSections.AnyAsync(r => r.PoemId == sections[i].PoemId && r.SectionIndex == sections[i].Index))
                                                    continue;
                                            }
                                            if (i * 100 / sections.Count > percent)
                                            {
                                                percent++;
                                                await jobProgressServiceEF.UpdateJob(job.Id, percent);
                                            }

                                            await _UpdateSectionRelatedSectionsInfoNoSaveChanges(context, sections[i]);

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