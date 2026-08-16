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
        /// Exports the Tajik (Cyrillic transliteration) overlay data — GanjoorTajikPoet/Cat/Poem/Verse
        /// — to its own git-tracked JSON repository, separate from the main ganjoor-data export.
        /// Kept separate deliberately: Tajik translation coverage is sparse (most poets/categories/
        /// poems have no Tajik counterpart at all), the consumer audience is different (Cyrillic
        /// Tajik apps, not general Ganjoor API consumers), and update cadence is independent of the
        /// main Persian corpus. This export only ever writes entries that actually have a Tajik
        /// database record; everything else is skipped. Ids and FullUrls match the main
        /// ganjoor-data repo exactly, so a consumer resolves structure/metre/sections for a poem by
        /// fetching the same id/path from there and merging verses by VOrder.
        /// </summary>
        public RServiceResult<bool> StartBatchExportTajikPublicGitData()
        {
            bool acquiredGuard = false;
            try
            {
                // scans every DTO in RMuseum.Models.Ganjoor.PublicExport, which now includes the
                // Tajik DTOs too — no separate call needed, they're covered automatically
                PublicExportSafetyGuard.AssertSafe();

                if (!TryStartExclusiveExportJob("TajikPublicDataExport"))
                {
                    return new RServiceResult<bool>(false,
                        "A Tajik public data export is already running (check the Jobs page) — wait for it to finish before starting another.");
                }
                acquiredGuard = true;

                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                    async token =>
                    {
                        try
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                            {
                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                var job = (await jobProgressServiceEF.NewJob("TajikPublicDataExport", "Preparing working copy")).Result;

                                try
                                {
                                var options = ReadTajikPublicDataExportOptions();
                                var publisher = new GitRepoPublisher(options);
                                publisher.EnsureWorkingCopyUpToDate();

                                string repoRoot = options.LocalWorkingCopyPath;

                                var manifest = new TajikPublicExportManifestDto
                                {
                                    GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                                    IdIndexShardSize = IdIndexShardSize,
                                };

                                var poetIdIndex = new Dictionary<int, string>();
                                var catIdIndex = new Dictionary<int, string>();
                                var poemIdIndex = new Dictionary<int, string>();

                                var tajikPoets = await context.TajikPoets.AsNoTracking().OrderBy(p => p.Id).ToListAsync();

                                int poetIndex = 0;
                                foreach (var tajikPoet in tajikPoets)
                                {
                                    poetIndex++;
                                    await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * poetIndex / Math.Max(1, tajikPoets.Count)), $"Exporting {tajikPoet.TajikNickname}");

                                    // the Tajik tables have no FullUrl of their own - poet/category/
                                    // poem paths are read from the main tables, since ids match 1:1
                                    var catPoet = await context.GanjoorCategories.AsNoTracking()
                                                        .Where(c => c.PoetId == tajikPoet.Id && c.ParentId == null)
                                                        .SingleOrDefaultAsync();
                                    if (catPoet == null)
                                        continue;

                                    await ExportTajikPoetToJson(repoRoot, tajikPoet, catPoet);
                                    poetIdIndex[tajikPoet.Id] = catPoet.FullUrl;

                                    int poemCount = await ExportTajikPoetContent(context, repoRoot, tajikPoet.Id, catPoet, catIdIndex, poemIdIndex);
                                    manifest.PoemsCount += poemCount;

                                    manifest.Poets.Add(new PublicExportManifestPoetEntryDto
                                    {
                                        Id = tajikPoet.Id,
                                        Nickname = tajikPoet.TajikNickname,
                                        FullUrl = catPoet.FullUrl,
                                    });
                                }

                                manifest.PoetsCount = manifest.Poets.Count;

                                await jobProgressServiceEF.UpdateJob(job.Id, 98, "Writing id indexes");
                                await IdIndexWriter.WriteFlatIndexAsync(repoRoot, "index/poets-by-id.json", poetIdIndex);
                                await IdIndexWriter.WriteShardedIndexAsync(repoRoot, "cats", catIdIndex, IdIndexShardSize);
                                await IdIndexWriter.WriteShardedIndexAsync(repoRoot, "poems", poemIdIndex, IdIndexShardSize);

                                await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "manifest.json"), manifest);
                                await TextFileWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "API.md"), BuildTajikApiMarkdown(manifest));
                                await TextFileWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "README.md"), BuildTajikReadmeMarkdown(manifest));

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
                        finally
                        {
                            EndExclusiveExportJob("TajikPublicDataExport");
                        }
                    }
                );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                if (acquiredGuard)
                {
                    EndExclusiveExportJob("TajikPublicDataExport"); // queueing itself threw after we'd already acquired the guard
                }
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private GitRepoPublisherOptions ReadTajikPublicDataExportOptions()
        {
            var section = Configuration.GetSection("TajikPublicDataExport");
            return new GitRepoPublisherOptions
            {
                LocalWorkingCopyPath = section["LocalWorkingCopyPath"],
                RemoteUrl = section["RemoteUrl"],
                Branch = section["Branch"] ?? "main",
                CommitAuthorName = section["CommitAuthorName"] ?? "Ganjoor Tajik Export Bot",
                CommitAuthorEmail = section["CommitAuthorEmail"] ?? "bot@ganjoor.net",
                PushEnabled = bool.TryParse(section["PushEnabled"], out var push) && push,
                GitUserName = section["GitUserName"],
                GitToken = section["GitToken"],
                GitExecutablePath = section["GitExecutablePath"],
                CommandTimeoutMinutes = int.TryParse(section["CommandTimeoutMinutes"], out var timeout) ? timeout : 45,
            };
        }

        private async Task ExportTajikPoetToJson(string repoRoot, GanjoorTajikPoet tajikPoet, GanjoorCat catPoet)
        {
            var dto = new TajikPoetPublicDto
            {
                Id = tajikPoet.Id,
                TajikNickname = tajikPoet.TajikNickname,
                TajikDescription = tajikPoet.TajikDescription,
                BirthYearInLHijri = tajikPoet.BirthYearInLHijri,
                FullUrl = catPoet.FullUrl,
            };

            string path = Path.Combine(repoRoot, "poets", TrimLeadingSlash(catPoet.FullUrl), "poet.json");
            await DeterministicJsonWriter.WriteIfChangedAsync(path, dto);
        }

        /// <summary>
        /// batch-loads everything Tajik for this poet (mirrors the same "load once per poet, not
        /// once per poem" fix applied to the main export) before walking the tree
        /// </summary>
        private async Task<int> ExportTajikPoetContent(RMuseumDbContext context, string repoRoot, int poetId, GanjoorCat catPoet,
            Dictionary<int, string> catIdIndex, Dictionary<int, string> poemIdIndex)
        {
            var tajikCatsById = (await context.TajikCats.AsNoTracking().Where(c => c.PoetId == poetId).ToListAsync())
                                    .ToDictionary(c => c.Id);

            if (tajikCatsById.Count == 0)
                return 0; // nothing translated for this poet at all

            var catIds = tajikCatsById.Keys.ToList();

            var tajikPoemsById = (await context.TajikPoems.AsNoTracking().Where(p => catIds.Contains(p.CatId)).ToListAsync())
                                    .ToDictionary(p => p.Id);

            var poemIds = tajikPoemsById.Keys.ToList();

            var tajikVersesByPoem = (await context.TajikVerses.AsNoTracking()
                                            .Where(v => poemIds.Contains(v.PoemId))
                                            .OrderBy(v => v.VOrder)
                                            .ToListAsync())
                                        .GroupBy(v => v.PoemId)
                                        .ToDictionary(g => g.Key, g => g.ToList());

            return await ExportTajikCatTreeToJson(context, repoRoot, catPoet, tajikCatsById, tajikPoemsById, tajikVersesByPoem, catIdIndex, poemIdIndex);
        }

        /// <summary>
        /// walks the *main* category tree (for structure/ordering — Tajik tables have no tree
        /// shape of their own) but only ever writes a file where a Tajik counterpart actually
        /// exists; still recurses into every child regardless, since a deeper category can have a
        /// translation even when its parent doesn't.
        /// </summary>
        private async Task<int> ExportTajikCatTreeToJson(RMuseumDbContext context, string repoRoot, GanjoorCat cat,
            Dictionary<int, GanjoorTajikCat> tajikCatsById, Dictionary<int, GanjoorTajikPoem> tajikPoemsById,
            Dictionary<int, List<GanjoorTajikVerse>> tajikVersesByPoem,
            Dictionary<int, string> catIdIndex, Dictionary<int, string> poemIdIndex)
        {
            var childCats = await context.GanjoorCategories.AsNoTracking()
                                    .Where(c => c.ParentId == cat.Id)
                                    .OrderBy(c => c.Id)
                                    .ToListAsync();

            int poemCount = 0;

            if (tajikCatsById.TryGetValue(cat.Id, out var tajikCat))
            {
                var poems = await context.GanjoorPoems.AsNoTracking()
                                    .Where(p => p.CatId == cat.Id)
                                    .OrderBy(p => p.Id)
                                    .ToListAsync();

                var translatedChildCats = childCats.Where(c => tajikCatsById.ContainsKey(c.Id)).ToList();
                var translatedPoems = poems.Where(p => tajikPoemsById.ContainsKey(p.Id)).ToList();

                var catDto = new TajikCatPublicDto
                {
                    Id = cat.Id,
                    PoetId = cat.PoetId,
                    ParentId = cat.ParentId,
                    TajikTitle = tajikCat.TajikTitle,
                    TajikDescription = tajikCat.TajikDescription,
                    FullUrl = cat.FullUrl,
                    ChildCats = translatedChildCats.Select(c => new CatChildRefDto { Id = c.Id, Title = tajikCatsById[c.Id].TajikTitle, FullUrl = c.FullUrl }).ToList(),
                    Poems = translatedPoems.Select(p => new PoemChildRefDto { Id = p.Id, Title = tajikPoemsById[p.Id].TajikTitle, FullUrl = p.FullUrl }).ToList(),
                };

                string catDir = Path.Combine(repoRoot, "poets", TrimLeadingSlash(cat.FullUrl));
                await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(catDir, "_cat.json"), catDto);
                catIdIndex[cat.Id] = cat.FullUrl;

                foreach (var poem in translatedPoems)
                {
                    var tajikPoem = tajikPoemsById[poem.Id];
                    tajikVersesByPoem.TryGetValue(poem.Id, out var verses);
                    await ExportTajikPoemToJson(repoRoot, poem, tajikPoem, verses ?? new List<GanjoorTajikVerse>());
                    poemIdIndex[poem.Id] = poem.FullUrl;
                    poemCount++;
                }
            }

            foreach (var childCat in childCats)
            {
                poemCount += await ExportTajikCatTreeToJson(context, repoRoot, childCat, tajikCatsById, tajikPoemsById, tajikVersesByPoem, catIdIndex, poemIdIndex);
            }

            return poemCount;
        }

        private async Task ExportTajikPoemToJson(string repoRoot, GanjoorPoem poem, GanjoorTajikPoem tajikPoem, List<GanjoorTajikVerse> verses)
        {
            var dto = new TajikPoemPublicDto
            {
                Id = poem.Id,
                CatId = poem.CatId,
                TajikTitle = tajikPoem.TajikTitle,
                FullTitle = tajikPoem.FullTitle,
                FullUrl = poem.FullUrl,
                TajikPlainText = tajikPoem.TajikPlainText,
                Verses = verses.OrderBy(v => v.VOrder).Select(v => new TajikVersePublicDto { VOrder = v.VOrder, TajikText = v.TajikText }).ToList(),
            };

            string path = Path.Combine(repoRoot, "poets", TrimLeadingSlash(poem.FullUrl) + ".json");
            await DeterministicJsonWriter.WriteIfChangedAsync(path, dto);
        }

        private static string BuildTajikReadmeMarkdown(TajikPublicExportManifestDto manifest)
        {
            return
$@"# ganjoor-tajik-data

**[▶ Live demo](https://ganjoor.github.io/tjmini/)** — Мини-Ганҷур, a minimal reading app built
on this data, running client-side in your browser with no server of its own.

Cyrillic Tajik transliteration/translation overlay for [Ganjoor](https://ganjoor.net)'s poetry
content. This is a **separate, sparser** data set from
[ganjoor-data](https://github.com/ganjoor/ganjoor-data) — Tajik coverage only exists for some
poets/categories/poems, not the full corpus, so this repo only ever contains entries that
actually have a Tajik translation.

Ids and paths in this repo match [ganjoor-data]({manifest.MainDataRepo}) exactly (same underlying
poet/category/poem). This repo carries only the Tajik title/text; for metre, rhyme, verse
structure, and sections, fetch the same id/path from `ganjoor-data` and merge verses by `vOrder`.

Currently tracks **{manifest.PoetsCount} poets** / **{manifest.PoemsCount} poems** with Tajik
content, generated {manifest.GeneratedAtUtc}.

## Where do I start?

- **[`manifest.json`](manifest.json)** — the list of poets that have any Tajik content, plus URL
  templates for every other file kind here.
- **[`API.md`](API.md)** — how to fetch any of this over plain HTTPS, and how to cross-reference
  `ganjoor-data` for structural data this repo doesn't duplicate.
";
        }

        private static string BuildTajikApiMarkdown(TajikPublicExportManifestDto manifest)
        {
            var t = manifest.UrlTemplates;
            return
$@"# ganjoor-tajik-data — static API

Same static-API-via-jsDelivr approach as [ganjoor-data]({manifest.MainDataRepo}) — no server,
every ""endpoint"" below is a plain HTTPS file fetch:

    https://cdn.jsdelivr.net/gh/ganjoor/ganjoor-tajik-data@main/

## Content

- `GET {t.Poet}` — Tajik nickname/description for a poet
- `GET {t.Category}` — Tajik title/description for a category, plus only the children that
  themselves have Tajik content
- `GET {t.Poem}` — Tajik title and per-verse text (`vOrder` + `tajikText` only)

## This repo does not stand alone

A poem file here has no metre, rhyme, or verse-position/section data — those live in
[ganjoor-data]({manifest.MainDataRepo}) under the *same* id and path. To render a Tajik poem
fully: fetch this repo's poem file for the Tajik text, fetch `ganjoor-data`'s poem file (same
`{t.Poem}` path) for structure, and merge the two verse lists by `vOrder`.

## Resolving a numeric id

Same scheme as `ganjoor-data`: `{t.PoetIdIndex}` (flat), `{t.CatIdIndexShard}` /
`{t.PoemIdIndexShard}` (bucketed, `bucket = id / {manifest.IdIndexShardSize}`). A missing id here
just means that poet/category/poem has no Tajik content yet — check `ganjoor-data`'s own id index
to confirm the id is valid at all.

## Not included

No comments, bookmarks, reading history, edit/correction history, or any other data linked to a
user account — same allowlist-only approach as `ganjoor-data`.
";
        }
    }
}
