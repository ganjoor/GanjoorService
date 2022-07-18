using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Utils;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
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
        /// regenerate category related sections
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartRegeneratingCateoryRelatedSections(int catId)
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
                                   var job = (await jobProgressServiceEF.NewJob($"StartRegeneratingCateoryRelatedSections - {catId}", "Query data")).Result;
                                   try
                                   {
                                       var updatePoemListId = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == catId).Select(p => p.Id).ToListAsync();
                                       for (int i = 0; i<updatePoemListId.Count; i++)
                                       {
                                           var updatePoemId = updatePoemListId[i];
                                           var sections = await context.GanjoorPoemSections.AsNoTracking().Where(s => s.PoemId == updatePoemId && s.GanjoorMetreId != null && !string.IsNullOrEmpty(s.RhymeLetters)).ToListAsync();
                                           for (int j = 0; j < sections.Count; j++)
                                           {
                                               var section = sections[j];
                                               await _UpdateRelatedSections(context, (int)section.GanjoorMetreId, section.RhymeLetters, jobProgressServiceEF, job, (i * 10 + j) * 100 / (10 * (updatePoemListId.Count + 1)));
                                           }
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



        /// <summary>
        /// update related sections info (after metreId or rhyme for one of these sections changes)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metreId"></param>
        /// <param name="rhyme"></param>
        /// <param name="jobProgressServiceEF"></param>
        /// <param name="job"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _UpdateRelatedSections(RMuseumDbContext context, int metreId, string rhyme, LongRunningJobProgressServiceEF jobProgressServiceEF = null, RLongRunningJobStatus job = null , int progress = 0)
        {
            try
            {
                if(jobProgressServiceEF != null)
                {
                    await jobProgressServiceEF.UpdateJob(job.Id, progress, $"M: {metreId}, G: {rhyme}");
                }
                if (await context.UpdatingRelSectsLogs.AsNoTracking()
                    .Where(l => l.MeterId == metreId && l.RhymeLettes == rhyme && l.DateTime > DateTime.Now.AddMinutes(-10)).AnyAsync())
                    return new RServiceResult<bool>(true);//prevent parallel updates for same data

                var log = new UpdatingRelSectsLog()
                {
                    MeterId = metreId,
                    RhymeLettes = rhyme,
                    DateTime = DateTime.Now
                };
                context.Add(log);
                await context.SaveChangesAsync();

                var sections = await context.GanjoorPoemSections.AsNoTracking()
                    .Where(section => section.GanjoorMetreId == metreId && section.RhymeLetters == rhyme && section.SectionType == PoemSectionType.WholePoem)
                    .ToListAsync();
                Dictionary<int, string> poetsImagesUrls = new Dictionary<int, string>();
                foreach (var section in sections)
                {
                    await _UpdateSectionRelatedSectionsInfoNoSaveChanges(context, section, poetsImagesUrls, true);
                }

                var logs = await context.UpdatingRelSectsLogs
                    .Where(l => l.MeterId == metreId && l.RhymeLettes == rhyme).ToListAsync();
                context.RemoveRange(logs);
                await context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                var logs = await context.UpdatingRelSectsLogs
                    .Where(l => l.MeterId == metreId && l.RhymeLettes == rhyme).ToListAsync();
                context.RemoveRange(logs);
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private async Task _UpdateSectionRelatedSectionsInfoNoSaveChanges(RMuseumDbContext context, GanjoorPoemSection section, Dictionary<int, string> poetsImagesUrls, bool needsClearance)
        {
            if(needsClearance)
            {
                var oldRelations = await context.GanjoorCachedRelatedSections.Where(r => r.PoemId == section.PoemId && r.SectionIndex == section.Index).ToListAsync();
                context.GanjoorCachedRelatedSections.RemoveRange(oldRelations);
            }

            int metreId = (int)section.GanjoorMetreId;
            string rhyme = section.RhymeLetters;


            var relatedSections = await context.GanjoorPoemSections.AsNoTracking().Include(section => section.Poem).Include(section => section.Poet)
                .Where(s =>
                        s.GanjoorMetreId == metreId
                        &&
                        s.RhymeLetters == rhyme
                        ).ToListAsync();

            relatedSections = relatedSections.OrderBy(p => p.Poet.BirthYearInLHijri).ThenBy(p => p.PoetId).ThenBy(p => p.SectionType).ToList();

            List<GanjoorCachedRelatedSection> GanjoorCachedRelatedSections = new List<GanjoorCachedRelatedSection>();
            int relationOrder = 0;
            int prePoetId = -1;
            foreach (var relatedSection in relatedSections)
            {
                if (relatedSection.Id == section.Id)
                    continue;
                if (prePoetId != relatedSection.PoetId)
                {
                    relationOrder++;

                    var fullUrl = relatedSection.Poem.FullUrl;
                    if(relatedSection.CachedFirstCoupletIndex > 0)
                    {
                        fullUrl += $"#bn{relatedSection.CachedFirstCoupletIndex + 1}";
                    }

                    if(!poetsImagesUrls.TryGetValue((int)relatedSection.PoetId, out string imgUrl))
                    {
                        imgUrl = $"/api/ganjoor/poet/image{(await context.GanjoorCategories.Where(c => c.ParentId == null && c.PoetId == relatedSection.PoetId).AsNoTracking().SingleAsync()).FullUrl}.gif";
                        poetsImagesUrls[(int)relatedSection.PoetId] = imgUrl;
                    }

                    GanjoorCachedRelatedSection newRelatedPoem = new GanjoorCachedRelatedSection()
                    {
                        PoemId = section.PoemId,
                        SectionIndex = section.Index,
                        PoetId = (int)relatedSection.PoetId,
                        RelationOrder = relationOrder,
                        PoetName = relatedSection.Poet.Nickname,
                        PoetImageUrl = imgUrl,
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
                                    var job = (await jobProgressServiceEF.NewJob($"GeneratingRelatedSectionsInfo - Whole Poems", "Query")).Result;
                                    int number = 0;
                                    try
                                    {

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Query");

                                        var sectionsInfo = 
                                            await context.GanjoorPoemSections.AsNoTracking()
                                            .Where(p => p.SectionType == PoemSectionType.WholePoem 
                                            && !string.IsNullOrEmpty(p.RhymeLetters) && p.GanjoorMetreId != null)
                                            .Select(s => new { s.Id, s.PoemId, s.Index })
                                            .ToListAsync();

                                        await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Updating Related Sections for {sectionsInfo.Count} sections");
                                        Dictionary<int, string> poetsImagesUrls = new Dictionary<int, string>();
                                        for (int i = 0; i < sectionsInfo.Count; i++)
                                        {
                                            number++;
                                            
                                            if (!regenerate)
                                            {
                                                if (await context.GanjoorCachedRelatedSections.AnyAsync(r => r.PoemId == sectionsInfo[i].PoemId && r.SectionIndex == sectionsInfo[i].Index))
                                                    continue;
                                            }
                                            var section = await context.GanjoorPoemSections.AsNoTracking().SingleAsync(s => s.Id == sectionsInfo[i].Id);
                                            await _UpdateSectionRelatedSectionsInfoNoSaveChanges(context, section, poetsImagesUrls, !regenerate);

                                           
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