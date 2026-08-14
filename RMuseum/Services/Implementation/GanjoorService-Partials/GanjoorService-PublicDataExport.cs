using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.PublicExport;
using RMuseum.Utils.PublicDataExport;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// start exporting all published Ganjoor data (poets/categories/poems/verses) to a
        /// git-tracked JSON tree and pushing it to the configured remote. User-linked tables
        /// (comments, bookmarks, visits, corrections, accounting, ...) are never touched by
        /// this code path — see RMuseum.Models.Ganjoor.PublicExport for the allowlisted shape
        /// of what actually gets written.
        /// </summary>
        public RServiceResult<bool> StartBatchExportPublicGitData()
        {
            try
            {
                PublicExportSafetyGuard.AssertSafe();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                    async token =>
                    {
                        using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                        {
                            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                            var job = (await jobProgressServiceEF.NewJob("PublicDataExport", "Preparing working copy")).Result;

                            try
                            {
                                var options = ReadPublicDataExportOptions();
                                var publisher = new GitRepoPublisher(options);
                                publisher.EnsureWorkingCopyUpToDate();

                                string repoRoot = options.LocalWorkingCopyPath;

                                await jobProgressServiceEF.UpdateJob(job.Id, 1, "Writing shared lookup tables");
                                await ExportSharedLookupTables(context, repoRoot);

                                var poets = await context.GanjoorPoets.AsNoTracking()
                                                    .Include(p => p.BirthLocation)
                                                    .Include(p => p.DeathLocation)
                                                    .Where(p => p.Published)
                                                    .OrderBy(p => p.Id)
                                                    .ToListAsync();

                                var manifest = new PublicExportManifestDto
                                {
                                    GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                                };

                                int poetIndex = 0;
                                foreach (var poet in poets)
                                {
                                    poetIndex++;
                                    await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * poetIndex / Math.Max(1, poets.Count)), $"Exporting {poet.Nickname}");

                                    var catPoet = await context.GanjoorCategories.AsNoTracking()
                                                        .Where(c => c.PoetId == poet.Id && c.ParentId == null)
                                                        .SingleOrDefaultAsync();
                                    if (catPoet == null || !catPoet.Published)
                                        continue;

                                    await ExportPoetToJson(context, repoRoot, poet, catPoet);

                                    int poemCount = await ExportCatTreeToJson(context, repoRoot, catPoet);
                                    manifest.PoemsCount += poemCount;

                                    manifest.Poets.Add(new PublicExportManifestPoetEntryDto
                                    {
                                        Id = poet.Id,
                                        Nickname = poet.Nickname,
                                        FullUrl = catPoet.FullUrl,
                                    });
                                }

                                manifest.PoetsCount = manifest.Poets.Count;

                                await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "manifest.json"), manifest);

                                await jobProgressServiceEF.UpdateJob(job.Id, 99, "Committing and pushing");
                                int changed = publisher.CommitAndPush($"data: export {manifest.PoetsCount} poets / {manifest.PoemsCount} poems — {DateTime.UtcNow:yyyy-MM-dd}");

                                await jobProgressServiceEF.UpdateJob(job.Id, 100, changed == 0 ? "No changes" : $"{changed} files changed", true);
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

        private GitRepoPublisherOptions ReadPublicDataExportOptions()
        {
            var section = Configuration.GetSection("PublicDataExport");
            return new GitRepoPublisherOptions
            {
                LocalWorkingCopyPath = section["LocalWorkingCopyPath"],
                RemoteUrl = section["RemoteUrl"],
                Branch = section["Branch"] ?? "main",
                CommitAuthorName = section["CommitAuthorName"] ?? "Ganjoor Export Bot",
                CommitAuthorEmail = section["CommitAuthorEmail"] ?? "bot@ganjoor.net",
                PushEnabled = bool.TryParse(section["PushEnabled"], out var push) && push,
                GitUserName = section["GitUserName"],
                GitToken = section["GitToken"],
            };
        }

        private async Task ExportSharedLookupTables(RMuseumDbContext context, string repoRoot)
        {
            var metres = await context.GanjoorMetres.AsNoTracking()
                                .OrderBy(m => m.Id)
                                .Select(m => new MetrePublicDto
                                {
                                    Id = m.Id,
                                    UrlSlug = m.UrlSlug,
                                    Rhythm = m.Rhythm,
                                    Name = m.Name,
                                    Description = m.Description,
                                })
                                .ToListAsync();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "metres.json"), metres);

            var languages = await context.GanjoorLanguages.AsNoTracking()
                                .OrderBy(l => l.Id)
                                .Select(l => new LanguagePublicDto
                                {
                                    Id = l.Id,
                                    Name = l.Name,
                                    Code = l.Code,
                                    NativeName = l.NativeName,
                                    RightToLeft = l.RightToLeft,
                                })
                                .ToListAsync();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "languages.json"), languages);
        }

        private async Task ExportPoetToJson(RMuseumDbContext context, string repoRoot, GanjoorPoet poet, GanjoorCat catPoet)
        {
            var dto = new PoetPublicDto
            {
                Id = poet.Id,
                Name = poet.Name,
                Nickname = poet.Nickname,
                Description = poet.Description,
                FullUrl = catPoet.FullUrl,
                ImageUrl = poet.RImageId == null ? null : $"https://ganjoor.net/api/ganjoor/poet/image{catPoet.FullUrl}.gif",
                BirthYearInLHijri = poet.BirthYearInLHijri,
                ValidBirthDate = poet.ValidBirthDate,
                DeathYearInLHijri = poet.DeathYearInLHijri,
                ValidDeathDate = poet.ValidDeathDate,
                BirthPlace = poet.BirthLocation?.Name,
                DeathPlace = poet.DeathLocation?.Name,
            };

            string path = Path.Combine(repoRoot, "poets", TrimLeadingSlash(catPoet.FullUrl), "poet.json");
            await DeterministicJsonWriter.WriteIfChangedAsync(path, dto);
        }

        /// <summary>
        /// recursively writes _cat.json for <paramref name="cat"/> and every published poem directly
        /// under it, then recurses into published child categories. Returns the number of poems written
        /// in this subtree (for manifest counts).
        /// </summary>
        private async Task<int> ExportCatTreeToJson(RMuseumDbContext context, string repoRoot, GanjoorCat cat)
        {
            var childCats = await context.GanjoorCategories.AsNoTracking()
                                    .Where(c => c.ParentId == cat.Id && c.Published)
                                    .OrderBy(c => c.Id)
                                    .ToListAsync();

            var poems = await context.GanjoorPoems.AsNoTracking()
                                    .Where(p => p.CatId == cat.Id && p.Published)
                                    .OrderBy(p => p.Id)
                                    .ToListAsync();

            var catDto = new CatPublicDto
            {
                Id = cat.Id,
                PoetId = cat.PoetId,
                ParentId = cat.ParentId,
                Title = cat.Title,
                FullUrl = cat.FullUrl,
                Description = cat.Description,
                DescriptionHtml = cat.DescriptionHtml,
                BookName = cat.BookName,
                ChildCats = childCats.Select(c => new CatChildRefDto { Id = c.Id, Title = c.Title, FullUrl = c.FullUrl }).ToList(),
                Poems = poems.Select(p => new PoemChildRefDto { Id = p.Id, Title = p.Title, FullUrl = p.FullUrl }).ToList(),
            };

            string catDir = Path.Combine(repoRoot, "poets", TrimLeadingSlash(cat.FullUrl));
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(catDir, "_cat.json"), catDto);

            int poemCount = poems.Count;

            foreach (var poem in poems)
            {
                await ExportPoemToJson(context, repoRoot, poem);
            }

            foreach (var childCat in childCats)
            {
                poemCount += await ExportCatTreeToJson(context, repoRoot, childCat);
            }

            return poemCount;
        }

        private async Task ExportPoemToJson(RMuseumDbContext context, string repoRoot, GanjoorPoem poem)
        {
            var metre = poem.GanjoorMetreId == null
                ? null
                : await context.GanjoorMetres.AsNoTracking().Where(m => m.Id == poem.GanjoorMetreId).SingleOrDefaultAsync();

            var sections = await context.GanjoorPoemSections.AsNoTracking()
                                .Where(s => s.PoemId == poem.Id)
                                .OrderBy(s => s.Index)
                                .ToListAsync();

            var verses = await context.GanjoorVerses.AsNoTracking()
                                .Where(v => v.PoemId == poem.Id)
                                .OrderBy(v => v.VOrder)
                                .ToListAsync();

            var dto = new PoemPublicDto
            {
                Id = poem.Id,
                CatId = poem.CatId,
                Title = poem.Title,
                FullTitle = poem.FullTitle,
                FullUrl = poem.FullUrl,
                RhymeLetters = poem.RhymeLetters,
                SourceName = poem.SourceName,
                SourceUrlSlug = poem.SourceUrlSlug,
                Language = poem.Language,
                PoemSummary = poem.PoemSummary,
                Metre = metre == null ? null : new MetreRefDto { Id = metre.Id, Rhythm = metre.Rhythm, Name = metre.Name },
                Sections = sections.Select(s => new PoemSectionPublicDto
                {
                    Index = s.Index,
                    Number = s.Number,
                    SectionType = s.SectionType.ToString(),
                    VerseType = s.VerseType.ToString(),
                    RhymeLetters = s.RhymeLetters,
                    PlainText = s.PlainText,
                    HtmlText = s.HtmlText,
                    PoemFormat = s.PoemFormat?.ToString(),
                    Language = s.Language,
                    CoupletsCount = s.CoupletsCount,
                }).ToList(),
                Verses = verses.Select(v => new VersePublicDto
                {
                    VOrder = v.VOrder,
                    Position = v.VersePosition.ToString(),
                    Text = v.Text,
                    CoupletIndex = v.CoupletIndex,
                    SectionIndex1 = v.SectionIndex1,
                    SectionIndex2 = v.SectionIndex2,
                    SectionIndex3 = v.SectionIndex3,
                    SectionIndex4 = v.SectionIndex4,
                }).ToList(),
            };

            string path = Path.Combine(repoRoot, "poets", TrimLeadingSlash(poem.FullUrl) + ".json");
            await DeterministicJsonWriter.WriteIfChangedAsync(path, dto);
        }

        private static string TrimLeadingSlash(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            url = url.Replace('/', Path.DirectorySeparatorChar);
            return url.TrimStart(Path.DirectorySeparatorChar);
        }
    }
}
