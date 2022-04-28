using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Utils;
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
                var sectionIds = await context.GanjoorPoemSections.AsNoTracking()
                    .Where(section => section.GanjoorMetreId == metreId && section.RhymeLetters == rhyme)
                    .OrderBy(section => section.SectionType)
                    .Select(section => section.Id)
                    .ToListAsync();
                foreach (var sectionId in sectionIds)
                {
                    var section = await context.GanjoorPoemSections.AsNoTracking().SingleAsync(s => s.Id == sectionId);
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

                    var fullUrl = relatedSection.Poem.FullUrl;
                    if(relatedSection.CachedFirstCoupletIndex > 0)
                    {
                        fullUrl += $"#bn{relatedSection.CachedFirstCoupletIndex + 1}";
                    }

                    GanjoorCachedRelatedSection newRelatedPoem = new GanjoorCachedRelatedSection()
                    {
                        PoemId = section.PoemId,
                        SectionIndex = section.Index,
                        PoetId = (int)relatedSection.PoetId,
                        RelationOrder = r,
                        PoetName = relatedSection.Poet.Nickname,
                        PoetImageUrl = $"/api/ganjoor/poet/image{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == relatedSection.PoetId).AsNoTracking().SingleAsync()).FullUrl}.gif",
                        FullTitle = relatedSection.Poem.FullTitle,
                        FullUrl = fullUrl,
                        PoetMorePoemsLikeThisCount = 0,
                        HtmlExcerpt = GanjoorPoemTools.GetPoemHtmlExcerpt(relatedSection.HtmlText),
                        TargetPoemId = relatedSection.PoemId,
                        TargetSectionIndex = relatedSection.Index,
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
        /// <param name="wholepoems"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartGeneratingRelatedSectionsInfo(bool regenerate, bool wholepoems)
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
                                    var job = (await jobProgressServiceEF.NewJob($"GeneratingRelatedSectionsInfo - {wholepoems}", "Query")).Result;
                                    int number = 0;
                                    try
                                    {

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Query");

                                        var sectionIds = 
                                            await context.GanjoorPoemSections.AsNoTracking()
                                            .Where(p => ((wholepoems && p.SectionType == PoemSectionType.WholePoem) || (!wholepoems && p.SectionType != PoemSectionType.WholePoem)) 
                                            && !string.IsNullOrEmpty(p.RhymeLetters) && p.GanjoorMetreId != null).Select(s => s.Id).ToListAsync();

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Updating Related Sections for {sectionIds.Count} sections");

                                        for (int i = 0; i < sectionIds.Count; i++)
                                        {
                                            var section = await context.GanjoorPoemSections.AsNoTracking().SingleAsync(s => s.Id == sectionIds[i]);
                                            if (!regenerate)
                                            {
                                                if (await context.GanjoorCachedRelatedSections.AnyAsync(r => r.PoemId == section.PoemId && r.SectionIndex == section.Index))
                                                    continue;
                                            }

                                            await _UpdateSectionRelatedSectionsInfoNoSaveChanges(context, section);

                                            number++;
                                            if(number % 100 == 0)
                                                await jobProgressServiceEF.UpdateJob(job.Id, number);
                                        }

                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                    }
                                    catch (Exception exp)
                                    {
                                        await jobProgressServiceEF.UpdateJob(job.Id, number, "", false, exp.ToString());
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