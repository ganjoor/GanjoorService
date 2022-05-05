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
        /// <param name="onlyPoemsWithRhymes"></param>
        /// <param name="poemsNum"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartFindingMissingRhythms(bool onlyPoemsWithRhymes, int poemsNum = 1000)
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
                                    var poemIds = await context.GanjoorPoems.AsNoTracking()
                                            .Where(p =>
                                                p.GanjoorMetreId == null && (onlyPoemsWithRhymes == false || !string.IsNullOrEmpty(p.RhymeLetters))
                                                &&
                                                false == (context.GanjoorVerses.Where(v => v.PoemId == p.Id && (v.VersePosition == VersePosition.Paragraph || v.VersePosition == VersePosition.Single)).Any())
                                                &&
                                                false == (context.GanjoorPoemProbableMetres.Where(r => r.PoemId == p.Id).Any())
                                                )
                                            .Take(poemsNum)
                                            .Select(p => p.Id)
                                            .ToArrayAsync();
                                    await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Total: {poemIds.Length}");
                                    var metres = await context.GanjoorMetres.OrderBy(m => m.Rhythm).AsNoTracking().Select(m => m.Rhythm).ToArrayAsync();

                                    using (HttpClient httpClient = new HttpClient())
                                    {
                                        for (int i = 0; i < poemIds.Length; i++)
                                        {
                                            var id = poemIds[i];
                                            var res = await _FindPoemMainSectionRhythm(id, context, httpClient, metres, true);
                                            if (res.Result == null)
                                                res.Result = "";

                                            GanjoorPoemProbableMetre prometre = new GanjoorPoemProbableMetre()
                                            {
                                                PoemId = id,
                                                Metre = res.Result
                                            };

                                            context.GanjoorPoemProbableMetres.Add(prometre);

                                            await jobProgressServiceEF.UpdateJob(job.Id, i);
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
        public async Task<RServiceResult<GanjoorPoemCompleteViewModel>> GetNextGanjoorPoemProbableMetre()
        {
            var next = await _context.GanjoorPoemProbableMetres.Where(p => p.Metre != "dismissed").AsNoTracking().FirstOrDefaultAsync();
            if (next == null)
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null);
            var res = await GetPoemById(next.PoemId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null, res.ExceptionString);
            if (res.Result == null)
                return new RServiceResult<GanjoorPoemCompleteViewModel>(null, "poem does not exist!");
            res.Result.GanjoorMetre = new GanjoorMetre()
            {
                Id = next.Id,
                Rhythm = next.Metre
            };
            return res;
        }

        /// <summary>
        /// get a list of ganjoor poems probable metres
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>> GetUnreviewedGanjoorPoemProbableMetres(PagingParameterModel paging)
        {
            try
            {
                var source = from probable in _context.GanjoorPoemProbableMetres.AsNoTracking() where probable.Metre != "dismissed" select probable;
                (PaginationMetadata PagingMeta, GanjoorPoemProbableMetre[] Items) paginatedResult =
                    await QueryablePaginator<GanjoorPoemProbableMetre>.Paginate(source, paging);
                List<GanjoorPoemCompleteViewModel> poems = new List<GanjoorPoemCompleteViewModel>();
                foreach (var next in paginatedResult.Items)
                {
                    var res = await GetPoemById(next.PoemId);
                    var poem = res.Result;
                    poem.GanjoorMetre = new GanjoorMetre()
                    {
                        Id = next.Id,
                        Rhythm = next.Metre
                    };
                    poems.Add(poem);
                }
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>((paginatedResult.PagingMeta, poems.ToArray()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, GanjoorPoemCompleteViewModel[] Items)>((null, null), exp.ToString());
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
                var poem = await _context.GanjoorPoems.Where(p => p.Id == item.PoemId).SingleAsync();
                int? oldMetreId = poem.GanjoorMetreId;
                poem.GanjoorMetreId = rhythm.Id;
                _context.Update(poem);
                _context.Remove(item);
                await _context.SaveChangesAsync();
                _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                if (oldMetreId != null && !string.IsNullOrEmpty(poem.RhymeLetters))
                                {
                                    await _UpdateRelatedPoems(context, (int)oldMetreId, poem.RhymeLetters);
                                    await context.SaveChangesAsync();
                                }

                                if (poem.GanjoorMetreId != null && !string.IsNullOrEmpty(poem.RhymeLetters))
                                {
                                    await _UpdateRelatedPoems(context, (int)poem.GanjoorMetreId, poem.RhymeLetters);
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