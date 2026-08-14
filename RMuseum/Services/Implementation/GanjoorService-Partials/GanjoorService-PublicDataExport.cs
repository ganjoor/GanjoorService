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
        /// how many ids are grouped into each id-index shard file — kept small enough that a
        /// shard stays a cheap single fetch, large enough that the id-space doesn't produce an
        /// unreasonable number of tiny files. 2000 ids/shard means ~500 shard files for Ganjoor's
        /// current poem count.
        /// </summary>
        private const int IdIndexShardSize = 2000;

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
                                    IdIndexShardSize = IdIndexShardSize,
                                };

                                var poetIdIndex = new Dictionary<int, string>();
                                var catIdIndex = new Dictionary<int, string>();
                                var poemIdIndex = new Dictionary<int, string>();

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
                                    poetIdIndex[poet.Id] = catPoet.FullUrl;

                                    int poemCount = await ExportCatTreeToJson(context, repoRoot, catPoet, catIdIndex, poemIdIndex);
                                    manifest.PoemsCount += poemCount;

                                    manifest.Poets.Add(new PublicExportManifestPoetEntryDto
                                    {
                                        Id = poet.Id,
                                        Nickname = poet.Nickname,
                                        FullUrl = catPoet.FullUrl,
                                    });
                                }

                                manifest.PoetsCount = manifest.Poets.Count;

                                await jobProgressServiceEF.UpdateJob(job.Id, 98, "Writing id indexes");
                                await IdIndexWriter.WriteFlatIndexAsync(repoRoot, "index/poets-by-id.json", poetIdIndex);
                                await IdIndexWriter.WriteShardedIndexAsync(repoRoot, "cats", catIdIndex, IdIndexShardSize);
                                await IdIndexWriter.WriteShardedIndexAsync(repoRoot, "poems", poemIdIndex, IdIndexShardSize);

                                await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "manifest.json"), manifest);
                                await TextFileWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "API.md"), BuildApiMarkdown(manifest));

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
        private async Task<int> ExportCatTreeToJson(RMuseumDbContext context, string repoRoot, GanjoorCat cat,
            Dictionary<int, string> catIdIndex, Dictionary<int, string> poemIdIndex)
        {
            var childCats = await context.GanjoorCategories.AsNoTracking()
                                    .Where(c => c.ParentId == cat.Id && c.Published)
                                    .OrderBy(c => c.Id)
                                    .ToListAsync();

            var poems = await context.GanjoorPoems.AsNoTracking()
                                    .Where(p => p.CatId == cat.Id && p.Published)
                                    .OrderBy(p => p.Id)
                                    .ToListAsync();

            catIdIndex[cat.Id] = cat.FullUrl;

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
                poemIdIndex[poem.Id] = poem.FullUrl;
            }

            foreach (var childCat in childCats)
            {
                poemCount += await ExportCatTreeToJson(context, repoRoot, childCat, catIdIndex, poemIdIndex);
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

        /// <summary>
        /// generates the repo-root API.md every run, so the docs can never drift out of sync with
        /// UrlTemplates/IdIndexShardSize in manifest.json. Not hand-edited — if you want to add
        /// prose, add it here, not in the generated file.
        /// </summary>
        private static string BuildApiMarkdown(PublicExportManifestDto manifest)
        {
            var t = manifest.UrlTemplates;
            return
$@"# Ganjoor public data — static API

This repository is served as a static API through jsDelivr's GitHub CDN. There is no server —
every ""endpoint"" below is a file in this repo, fetched over plain HTTPS with CORS enabled.

Base URL (tracks the latest commit on `main`):

    https://cdn.jsdelivr.net/gh/ganjoor/ganjoor-data@main/

Note: since this currently tracks `@main` rather than tagged releases, jsDelivr's edge cache
(up to ~7 days) means a fetch can lag behind the newest commit. If you need a frozen snapshot,
pin to an exact commit instead of `@main`:

    https://cdn.jsdelivr.net/gh/ganjoor/ganjoor-data@<commit-sha>/

## Discovery

`GET manifest.json` — schema version, generation timestamp, poet/poem counts, the list of poets
with their paths, and the URL templates below (`manifest.json` is the source of truth for this
file; if they ever disagree, trust `manifest.json`).

## Content

- `GET {t.Poet}` — poet biography
- `GET {t.Category}` — a category/collection: title, description, ordered child categories and poems
- `GET {t.Poem}` — a poem: metre, rhyme, sections, verses

`{{poetSlug}}`/`{{catPath}}`/`{{poemSlug}}` are exactly the path segments of the poem's Ganjoor
URL, e.g. the poem at ganjoor.net/hafez/ghazal/sh1 is at `poets/hafez/ghazal/sh1.json`.

## Resolving a numeric id

If you have a poet/category/poem id (not a slug), resolve it via the id index instead of
guessing a path:

- `GET {t.PoetIdIndex}` — small enough to fetch whole: `{{ ""1"": ""/hafez"", ... }}`
- `GET {t.CatIdIndexShard}` / `GET {t.PoemIdIndexShard}` — bucketed. The shard file for id `X` is
  `bucket = X / {manifest.IdIndexShardSize}` (integer division), so e.g. poem id 4321 with the
  current shard size of {manifest.IdIndexShardSize} lives in shard `{4321 / manifest.IdIndexShardSize}`,
  i.e. `index/poems-by-id/{4321 / manifest.IdIndexShardSize}.json`.
  Each shard maps ids in its range to a `FullUrl` you then fetch with the `{t.Poem}` pattern above.

## Not included

No comments, bookmarks, reading history, edit/correction history, or any other user-account-linked
data — see the repo this data set is exported from for details. Only `Published` poets/categories/
poems are included.

## Search

Not available as a static endpoint in this data set yet.
";
        }
    }
}
