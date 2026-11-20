using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.PublicExport;
using RMuseum.Utils.PublicDataImport;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService

    {
        /// <summary>
        /// https://x.com/itsArmanj/status/2123434482757996948
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartImportAISummariesFromPublicDataRepo()
        {
            try
            {
                var options = ReadPublicDataExportOptions();
                string location = options.LocalWorkingCopyPath;

                if (string.IsNullOrWhiteSpace(location))
                    return new RServiceResult<bool>(false, "location is required");

                
                

                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                    async token =>
                    {
                        
                        using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                        {
                            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                            var job = (await jobProgressServiceEF.NewJob("ImportAISummariesFromPublicDataRepo", "Reading manifest")).Result;

                            int changedVerses = 0;
                            try
                            {
                                IPublicDataSource source =
                                     new LocalFileSystemPublicDataSource(location);

                                string manifestJson = await source.ReadTextAsync("manifest.json");
                                if (manifestJson == null)
                                    throw new Exception($"manifest.json not found at '{location}' — check the path/URL");

                                var manifest = JsonSerializer.Deserialize<PublicExportManifestDto>(manifestJson, _importJsonOptions);

                                var poetsToImport = manifest.Poets;



                                int poetIndex = 0;
                                foreach (var poetEntry in poetsToImport)
                                {
                                    poetIndex++;
                                    await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * poetIndex / Math.Max(1, poetsToImport.Count)), $"Importing {poetEntry.Nickname} - changedVerses : {changedVerses}");
                                    changedVerses += await ImportPoetAISummariesFromPublicData(context, source, poetEntry.Id, poetEntry.FullUrl);
                                }

                                await jobProgressServiceEF.UpdateJob(job.Id, 100, $"changedVerses : {changedVerses}", true);
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

        private async Task<int> ImportPoetAISummariesFromPublicData(RMuseumDbContext context, IPublicDataSource source, int poetId, string poetFullUrl)
        {
            string nickname = (await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync()).Nickname;

            // the poet's root category IS the poet's landing page (e.g. /hafez) — imported the
            // same way as any category, just flagged as the tree's root for page-type purposes
            return await ImportCatTreeAISummariesFromPublicData(context, source, poetId, poetFullUrl, nickname, isRoot: true);
        }

        private async Task<int> ImportCatTreeAISummariesFromPublicData(RMuseumDbContext context, IPublicDataSource source, int poetId,
    string catFullUrl, string parentFullTitle, bool isRoot)
        {
            string catJson = await source.ReadTextAsync($"poets{catFullUrl}/_cat.json");
            if (catJson == null)
                return;

            var catDto = JsonSerializer.Deserialize<CatPublicDto>(catJson, _importJsonOptions);
            string fullTitle = isRoot ? parentFullTitle : $"{parentFullTitle} » {catDto.Title}";

            int changedVerses = 0;

            foreach (var poemRef in catDto.Poems)
            {
                changedVerses += await ImportPoemAISummariesFromPublicData(context, source, poetId, catDto.Id, poemRef.Id, poemRef.FullUrl, fullTitle);
            }

            foreach (var childRef in catDto.ChildCats)
            {
                changedVerses += await ImportCatTreeAISummariesFromPublicData(context, source, poetId, childRef.FullUrl, fullTitle, isRoot: false);
            }
            return changedVerses;
        }

        private async Task<int> ImportPoemAISummariesFromPublicData(RMuseumDbContext context, IPublicDataSource source, int poetId, int catId,
    int poemId, string poemFullUrl, int parentPageId, string parentFullTitle)
        {
            string poemJson = await source.ReadTextAsync($"poets{poemFullUrl}.json");
            if (poemJson == null)
                return 0;

            var poemDto = JsonSerializer.Deserialize<PoemPublicDto>(poemJson, _importJsonOptions);

            var verseDtoArray = poemDto.Verses.Select(v => new GanjoorVerse
            {
                PoemId = poemId,
                VOrder = v.VOrder,
                VersePosition = Enum.Parse<VersePosition>(v.Position),
                Text = v.Text,
                CoupletIndex = v.CoupletIndex,
                SectionIndex1 = v.SectionIndex1,
                SectionIndex2 = v.SectionIndex2,
                SectionIndex3 = v.SectionIndex3,
                SectionIndex4 = v.SectionIndex4,
                CoupletSummary = v.CoupletSummary,
            }).ToList();

            

            bool needsSave = false;

            var editablePoem = await context.GanjoorPoems.Where(p => p.Id == poemId).SingleOrDefaultAsync();
            if (editablePoem == null)
                return;

            if(!string.IsNullOrEmpty(poemDto.PoemSummary) && editablePoem.PoemSummary.StartsWith(AISummaryPrefix))
            {
                editablePoem.PoemSummary = poemDto.PoemSummary;
                context.Update(editablePoem);
                needsSave = true;
            }

            int changedVerses = 0;
            var editableVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId).ToArrayAsync();

            foreach (var verseDto in verseDtoArray)
            {
                if(!string.IsNullOrEmpty(verseDto.CoupletSummary))
                {
                    var editableVerse = editableVerses.Where(v => v.Id == verseDto.Id).SingleOrDefault();
                    if (editableVerse != null)
                    {
                        if (editableVerse.CoupletSummary.StartsWith(AISummaryPrefix))
                        {
                            editableVerse.CoupletSummary = verseDto.CoupletSummary;
                            context.Update(editableVerse);
                            changedVerses++;
                            needsSave = true;
                        }
                    }

                }
            }

            if(needsSave)
            {
                await context.SaveChangesAsync();
            }
            
            context.ChangeTracker.Clear(); // everything above is saved; keeps a full-corpus run from tracking every poem
            return changedVerses;
        }


    }
}
