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
        /// <summary>
        /// Start finding missing rhthms
        /// </summary>
        /// <param name="systemUserId"></param>
        /// <param name="deletedUserId"></param>
        /// <param name="onlyPoemsWithRhymes"></param>
        /// <param name="poemsNum"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartFindingMissingRhythms(Guid systemUserId, Guid deletedUserId, bool onlyPoemsWithRhymes, int poemsNum = 1000)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                var job = (await jobProgressServiceEF.NewJob($"StartFindingMissingRhythms", "Query data")).Result;
                                try
                                {
                                 
                                    var sectionIds = await context.GanjoorPoemSections.AsNoTracking()
                                            .Where(p =>
                                                p.GanjoorMetreId == null && (onlyPoemsWithRhymes == false || !string.IsNullOrEmpty(p.RhymeLetters))
                                                &&
                                                p.SectionType == PoemSectionType.WholePoem
                                                &&
                                                false == context.GanjoorPoemProbableMetres.Where(r => r.SectionId == p.Id).Any()
                                                )
                                            .Take(poemsNum)
                                            .Select(p => p.Id)
                                            .ToArrayAsync();
                                    await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Total: {sectionIds.Length}");
                                    var metres = await context.GanjoorMetres.OrderBy(m => m.Rhythm).AsNoTracking().Select(m => m.Rhythm).ToArrayAsync();

                                    using (HttpClient httpClient = new HttpClient())
                                    {
                                        for (int i = 0; i < sectionIds.Length; i++)
                                        {
                                            var id = sectionIds[i];
                                            var section = await context.GanjoorPoemSections.AsNoTracking().Where(s => s.Id == id).SingleOrDefaultAsync();
                                            if(section != null)
                                            {
                                                var res = await _FindSectionRhythm(section, context, httpClient, metres, true);
                                                if (res.Result == null)
                                                    res.Result = "";

                                                GanjoorPoemProbableMetre prometre = new GanjoorPoemProbableMetre()
                                                {
                                                    PoemId = section.PoemId,
                                                    SectionId = id,
                                                    Metre = res.Result
                                                };

                                                context.GanjoorPoemProbableMetres.Add(prometre);

                                                if (res.Result == "paragraph")
                                                    continue;

                                                var userId = !string.IsNullOrEmpty(res.Result) && res.Result != "dismissed" ? systemUserId : deletedUserId;
                                                if(string.IsNullOrEmpty(res.Result) || res.Result == "dismissed")
                                                {
                                                    res.Result = "فاعلاتن فاعلاتن فاعلاتن فاعلن (رمل مثمن محذوف)";
                                                }


                                                GanjoorPoemSectionCorrection dbCorrection = new GanjoorPoemSectionCorrection()
                                                {
                                                    SectionId = section.Id,
                                                    UserId = userId,
                                                    Rhythm = res.Result,
                                                    Note = "وزن‌یابی سیستمی",
                                                    Date = DateTime.Now,
                                                    RhythmResult = CorrectionReviewResult.NotReviewed,
                                                    Reviewed = false,
                                                    AffectedThePoem = false,
                                                    RhymeLettersReviewResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse1VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse2VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse3VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse4VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse5VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse6VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse7VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse8VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse9VOrderResult = CorrectionReviewResult.NotReviewed,
                                                    BreakFromVerse10VOrderResult = CorrectionReviewResult.NotReviewed,
                                                };
                                                context.GanjoorPoemSectionCorrections.Add(dbCorrection);
                                                await jobProgressServiceEF.UpdateJob(job.Id, i);
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
                        });

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// get next ganjoor poem probable metre
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPoemSection>> GetNextGanjoorPoemProbableMetre()
        {
            var next = await _context.GanjoorPoemProbableMetres.Where(p => p.Metre != "dismissed").AsNoTracking().FirstOrDefaultAsync();
            if (next == null)
                return new RServiceResult<GanjoorPoemSection>(null);
            var res = await _context.GanjoorPoemSections.AsNoTracking().Where(s => s.Id == next.SectionId).SingleOrDefaultAsync();
           
            if (res == null)
                return new RServiceResult<GanjoorPoemSection>(null, "poem section does not exist!");
            res.GanjoorMetre = new GanjoorMetre()
            {
                Id = next.Id,
                Rhythm = next.Metre
            };
            return new RServiceResult<GanjoorPoemSection>(res);
        }

        /// <summary>
        /// get a list of ganjoor poems probable metres
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemSection[] Items)>> GetUnreviewedGanjoorPoemProbableMetres(PagingParameterModel paging)
        {
            try
            {
                var source = from probable in _context.GanjoorPoemProbableMetres.AsNoTracking() where probable.Metre != "dismissed" select probable;
                (PaginationMetadata PagingMeta, GanjoorPoemProbableMetre[] Items) paginatedResult =
                    await QueryablePaginator<GanjoorPoemProbableMetre>.Paginate(source, paging);
                List<GanjoorPoemSection> sections = new List<GanjoorPoemSection>();
                foreach (var next in paginatedResult.Items)
                {
                    var section = await _context.GanjoorPoemSections.AsNoTracking().Where(s => s.Id == next.SectionId).SingleOrDefaultAsync();
                    section.GanjoorMetre = new GanjoorMetre()
                    {
                        Id = next.Id,
                        Rhythm = next.Metre
                    };
                    sections.Add(section);
                }
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemSection[] Items)>((paginatedResult.PagingMeta, sections.ToArray()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemSection[] Items)>((null, null), exp.ToString());
            }
        }

        /// <summary>
        /// save ganjoor poem probable metre
        /// </summary>
        /// <param name="id">problable metre id</param>
        /// <param name="metre"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SaveGanjoorPoemProbableMetre(int id, string metre)
        {
            try
            {
                var item = await _context.GanjoorPoemProbableMetres.Where(p => p.Id == id).SingleAsync();
                metre = metre.Trim();
                if (string.IsNullOrEmpty(metre))
                    metre = "dismissed";
                if (metre == "dismissed")
                {
                    item.Metre = "dismissed";
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    return new RServiceResult<bool>(true);
                }
                var rhythm = await _context.GanjoorMetres.AsNoTracking().Where(m => m.Rhythm == metre).SingleOrDefaultAsync();
                if(rhythm == null)
                {
                    rhythm = new GanjoorMetre()
                    {
                        Rhythm = metre,
                        VerseCount = 0
                    };
                    _context.GanjoorMetres.Add(rhythm);
                    await _context.SaveChangesAsync();
                }
                var section = await _context.GanjoorPoemSections.Where(p => p.Id == item.SectionId).SingleAsync();
                int? oldMetreId = section.GanjoorMetreId;
                section.GanjoorMetreId = rhythm.Id;
                _context.Update(section);
                _context.Remove(item);
                await _context.SaveChangesAsync();
                _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                if (oldMetreId != null && !string.IsNullOrEmpty(section.RhymeLetters))
                                {
                                    await _UpdateRelatedSections(context, (int)oldMetreId, section.RhymeLetters);
                                    await context.SaveChangesAsync();
                                }

                                if (section.GanjoorMetreId != null && !string.IsNullOrEmpty(section.RhymeLetters))
                                {
                                    await _UpdateRelatedSections(context, (int)section.GanjoorMetreId, section.RhymeLetters);
                                    await context.SaveChangesAsync();
                                }
                            }
                        });
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}