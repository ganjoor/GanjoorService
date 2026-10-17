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
        /// how many person ids are grouped into each index/person-tags shard (people are far fewer
        /// than poems, but each person can have thousands of tagged couplets)
        /// </summary>
        private const int PersonIndexShardSize = 500;

        /// <summary>
        /// collects the tag-derived flat files while the per-poem files are being written, so the
        /// tags only have to be loaded once (per poet) from the database
        /// </summary>
        private class PublicExportTagCollector
        {
            public List<GeoTagPublicDto> GeoTags { get; } = new List<GeoTagPublicDto>();
            public Dictionary<int, List<PersonTagRefPublicDto>> PersonTags { get; } = new Dictionary<int, List<PersonTagRefPublicDto>>();

            /// <summary>
            /// literary tags of the poet being exported, by poem id (cleared after each poet)
            /// </summary>
            public Dictionary<int, List<PoemLiteraryTag>> LiteraryByPoem { get; } = new Dictionary<int, List<PoemLiteraryTag>>();

            /// <summary>
            /// all literary tags by device id, for index/literary-tags
            /// </summary>
            public Dictionary<int, List<LiteraryTagIndexEntryPublicDto>> LiteraryByDevice { get; } = new Dictionary<int, List<LiteraryTagIndexEntryPublicDto>>();
        }

        /// <summary>
        /// Guards against two runs of the same export job (keyed by job name — "PublicDataExport"
        /// / "TajikPublicDataExport") ever executing concurrently against the same git working
        /// copy. This exists because deleting a job's row on the admin Jobs page only removes its
        /// database record — it does not stop the background Task (or its child git.exe process)
        /// that's still actually running. Without this guard, triggering the job again while a
        /// previous run hadn't finished starts a second concurrent git process against the same
        /// folder, which is exactly what produces a ".git/index.lock: File exists" failure.
        /// </summary>
        private static readonly object _publicDataExportRunningLock = new object();
        private static readonly HashSet<string> _publicDataExportRunningJobs = new HashSet<string>();

        /// <summary>
        /// Returns true (and marks <paramref name="jobName"/> as running) if no run of this job
        /// was already in progress; false if one was, in which case the caller should refuse to
        /// start a second one rather than queueing a background task that would race the first.
        /// </summary>
        private static bool TryStartExclusiveExportJob(string jobName)
        {
            lock (_publicDataExportRunningLock)
            {
                if (_publicDataExportRunningJobs.Contains(jobName))
                    return false;
                _publicDataExportRunningJobs.Add(jobName);
                return true;
            }
        }

        private static void EndExclusiveExportJob(string jobName)
        {
            lock (_publicDataExportRunningLock)
            {
                _publicDataExportRunningJobs.Remove(jobName);
            }
        }

        /// <summary>
        /// start exporting all Ganjoor data (poets/categories/poems/verses) belonging to
        /// published poets to a
        /// git-tracked JSON tree and pushing it to the configured remote. User-linked tables
        /// (comments, bookmarks, visits, corrections, accounting, ...) are never touched by
        /// this code path — see RMuseum.Models.Ganjoor.PublicExport for the allowlisted shape
        /// of what actually gets written.
        /// </summary>
        public RServiceResult<bool> StartBatchExportPublicGitData()
        {
            bool acquiredGuard = false;
            try
            {
                PublicExportSafetyGuard.AssertSafe();

                if (!TryStartExclusiveExportJob("PublicDataExport"))
                {
                    return new RServiceResult<bool>(false,
                        "A public data export is already running (check the Jobs page) — wait for it to finish before starting another.");
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
                                var job = (await jobProgressServiceEF.NewJob("PublicDataExport", "Preparing working copy")).Result;

                                try
                                {
                                var options = ReadPublicDataExportOptions();
                                var publisher = new GitRepoPublisher(options);
                                publisher.EnsureWorkingCopyUpToDate();

                                string repoRoot = options.LocalWorkingCopyPath;

                                await jobProgressServiceEF.UpdateJob(job.Id, 1, "Writing shared lookup tables");
                                await ExportSharedLookupTables(context, repoRoot);

                                // loaded once for the whole run instead of once per poem (was the
                                // single biggest cost in this job: ~3 round-trips per poem, tens of
                                // thousands of poems, on every run regardless of what changed) — see
                                // ExportPoetContent for how sections/verses are batched per-poet.
                                var metresById = (await context.GanjoorMetres.AsNoTracking().ToListAsync())
                                                    .ToDictionary(m => m.Id);

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
                                var tagCollector = new PublicExportTagCollector();

                                int poetIndex = 0;
                                foreach (var poet in poets)
                                {
                                    poetIndex++;
                                    await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * poetIndex / Math.Max(1, poets.Count)), $"Exporting {poet.Nickname}");

                                    var catPoet = await context.GanjoorCategories.AsNoTracking()
                                                        .Where(c => c.PoetId == poet.Id && c.ParentId == null)
                                                        .SingleOrDefaultAsync();
                                    if (catPoet == null)
                                        continue;

                                    await ExportPoetToJson(context, repoRoot, poet, catPoet);
                                    poetIdIndex[poet.Id] = catPoet.FullUrl;

                                    int poemCount = await ExportPoetContent(context, repoRoot, poet, catPoet, metresById, catIdIndex, poemIdIndex, tagCollector);
                                    manifest.PoemsCount += poemCount;

                                    manifest.Poets.Add(new PublicExportManifestPoetEntryDto
                                    {
                                        Id = poet.Id,
                                        Nickname = poet.Nickname,
                                        FullUrl = catPoet.FullUrl,
                                    });
                                }

                                manifest.PoetsCount = manifest.Poets.Count;

                                await jobProgressServiceEF.UpdateJob(job.Id, 97, "Writing people, places and tags");
                                await ExportPeopleAndTags(context, repoRoot, manifest, poemIdIndex, tagCollector);

                                await jobProgressServiceEF.UpdateJob(job.Id, 98, "Writing id indexes");
                                await IdIndexWriter.WriteFlatIndexAsync(repoRoot, "index/poets-by-id.json", poetIdIndex);
                                await IdIndexWriter.WriteShardedIndexAsync(repoRoot, "cats", catIdIndex, IdIndexShardSize);
                                await IdIndexWriter.WriteShardedIndexAsync(repoRoot, "poems", poemIdIndex, IdIndexShardSize);

                                await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "manifest.json"), manifest);
                                await TextFileWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "API.md"), BuildApiMarkdown(manifest));
                                await TextFileWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "README.md"), BuildReadmeMarkdown(manifest));

                                await jobProgressServiceEF.UpdateJob(job.Id, 99, "Committing and pushing");
                                int changed = publisher.CommitAndPush($"data: export {manifest.PoetsCount} poets / {manifest.PoemsCount} poems / {manifest.PeopleCount} people / {manifest.PersonTagsCount + manifest.GeoTagsCount} tags / {manifest.LiteraryTagsCount} literary tags — {DateTime.UtcNow:yyyy-MM-dd}");

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
                            EndExclusiveExportJob("PublicDataExport");
                        }
                    }
                );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                if (acquiredGuard)
                {
                    EndExclusiveExportJob("PublicDataExport"); // queueing itself threw after we'd already acquired the guard
                }
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
                GitExecutablePath = section["GitExecutablePath"],
                CommandTimeoutMinutes = int.TryParse(section["CommandTimeoutMinutes"], out var timeout) ? timeout : 45,
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
                BirthLocationId = poet.BirthLocationId,
                DeathLocationId = poet.DeathLocationId,
                PinOrder = poet.PinOrder,
            };

            string path = Path.Combine(repoRoot, "poets", TrimLeadingSlash(catPoet.FullUrl), "poet.json");
            await DeterministicJsonWriter.WriteIfChangedAsync(path, dto);
        }

        /// <summary>
        /// Batch-loads this poet's entire sections/verses in two queries (instead of the
        /// two-per-poem queries the old per-poem approach ran), then walks the poet's category
        /// tree writing everything from memory. This is the fix for the export consistently
        /// taking about as long on every run regardless of how much content changed — the
        /// "skip unchanged files" logic in DeterministicJsonWriter only ever saved disk writes,
        /// never the DB round-trips, which were the actual dominant cost (roughly 3 sequential
        /// queries per poem — tens of thousands of round-trips for the full corpus, every run).
        /// </summary>
        private async Task<int> ExportPoetContent(RMuseumDbContext context, string repoRoot, GanjoorPoet poet, GanjoorCat catPoet,
            Dictionary<int, GanjoorMetre> metresById, Dictionary<int, string> catIdIndex, Dictionary<int, string> poemIdIndex,
            PublicExportTagCollector tagCollector)
        {
            var sectionsByPoem = (await context.GanjoorPoemSections.AsNoTracking()
                                        .Where(s => s.Poem.Cat.PoetId == poet.Id)
                                        .OrderBy(s => s.Index)
                                        .ToListAsync())
                                    .GroupBy(s => s.PoemId)
                                    .ToDictionary(g => g.Key, g => g.ToList());

            var versesByPoem = (await context.GanjoorVerses.AsNoTracking()
                                        .Where(v => v.Poem.Cat.PoetId == poet.Id)
                                        .OrderBy(v => v.VOrder)
                                        .ToListAsync())
                                    .GroupBy(v => v.PoemId)
                                    .ToDictionary(g => g.Key, g => g.ToList());

            // approved tags only: pending suggestions live in the correction tables, which this
            // export never reads
            var tagsByPoem = (await context.PoemGeoDateTags.AsNoTracking()
                                        .Where(t => t.Poem.Cat.PoetId == poet.Id)
                                        .OrderBy(t => t.CoupletIndex).ThenBy(t => t.LocationId).ThenBy(t => t.PersonId).ThenBy(t => t.Id)
                                        .ToListAsync())
                                    .GroupBy(t => t.PoemId)
                                    .ToDictionary(g => g.Key, g => g.ToList());

            //literary tags (approved by definition: pending suggestions only exist in the correction tables)
            tagCollector.LiteraryByPoem.Clear();
            foreach (var group in (await context.PoemLiteraryTags.AsNoTracking()
                                        .Include(t => t.Parts)
                                        .Where(t => t.Poem.Cat.PoetId == poet.Id)
                                        .OrderBy(t => t.CoupletIndex).ThenBy(t => t.DeviceId).ThenBy(t => t.Id)
                                        .ToListAsync())
                                    .GroupBy(t => t.PoemId))
            {
                tagCollector.LiteraryByPoem[group.Key] = group.ToList();
            }

            int exported = await ExportCatTreeToJson(context, repoRoot, catPoet, metresById, sectionsByPoem, versesByPoem, tagsByPoem, catIdIndex, poemIdIndex, tagCollector);
            tagCollector.LiteraryByPoem.Clear();
            return exported;
        }

        /// <summary>
        /// public form of a literary tag: no author, no dates, and imported tags are traced back to their source
        /// </summary>
        private static LiteraryTagPublicDto ToPublicLiteraryTag(PoemLiteraryTag t)
        {
            return new LiteraryTagPublicDto
            {
                CoupletIndex = t.CoupletIndex,
                DeviceId = t.DeviceId,
                Uncertain = t.Uncertain,
                Note = t.Note,
                Origin = t.Origin.ToString(),
                SourceKey = t.Origin == LiteraryTagOrigin.AcademicCorpus ? t.OriginRef : null,
                MachineGenerated = t.Origin == LiteraryTagOrigin.AI || t.Origin == LiteraryTagOrigin.Algorithm,
                Parts = (t.Parts ?? new List<PoemLiteraryTagPart>()).OrderBy(p => p.Order).ThenBy(p => p.Id)
                            .Select(p => new LiteraryTagPartPublicDto
                            {
                                SlotId = p.SlotId,
                                CoupletIndex = p.CoupletIndex,
                                Misra = p.Misra,
                                WordStart = p.WordStart,
                                WordEnd = p.WordEnd,
                                ConceptId = p.ConceptId,
                                Text = p.Text,
                            }).ToList(),
            };
        }

        /// <summary>
        /// recursively writes _cat.json for <paramref name="cat"/> and every poem directly
        /// under it, then recurses into child categories. Returns the number of poems written
        /// in this subtree (for manifest counts). Only the poet-level Published flag is a real
        /// visibility gate in this codebase (see GetPoets) — GanjoorCat.Published and
        /// GanjoorPoem.Published are not checked anywhere the live site actually serves content
        /// (GetCatByUrl/GetPoemByUrl ignore them entirely), so this export doesn't filter on them
        /// either; every category/poem under a published poet is exported.
        /// </summary>
        private async Task<int> ExportCatTreeToJson(RMuseumDbContext context, string repoRoot, GanjoorCat cat,
            Dictionary<int, GanjoorMetre> metresById,
            Dictionary<int, List<GanjoorPoemSection>> sectionsByPoem, Dictionary<int, List<GanjoorVerse>> versesByPoem,
            Dictionary<int, List<PoemGeoDateTag>> tagsByPoem,
            Dictionary<int, string> catIdIndex, Dictionary<int, string> poemIdIndex, PublicExportTagCollector tagCollector)
        {
            var childCats = await context.GanjoorCategories.AsNoTracking()
                                    .Where(c => c.ParentId == cat.Id)
                                    .OrderBy(c => c.Id)
                                    .ToListAsync();

            var poems = await context.GanjoorPoems.AsNoTracking()
                                    .Where(p => p.CatId == cat.Id)
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
                CatType = cat.CatType.ToString(),
                TableOfContentsStyle = cat.TableOfContentsStyle.ToString(),
                MixedModeOrder = cat.MixedModeOrder,
                SumUpSubsGeoLocations = cat.SumUpSubsGeoLocations,
                MapName = cat.MapName,
                ChildCats = childCats.Select(c => new CatChildRefDto { Id = c.Id, Title = c.Title, FullUrl = c.FullUrl }).ToList(),
                Poems = poems.Select(p => new PoemChildRefDto { Id = p.Id, Title = p.Title, FullUrl = p.FullUrl }).ToList(),
            };

            string catDir = Path.Combine(repoRoot, "poets", TrimLeadingSlash(cat.FullUrl));
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(catDir, "_cat.json"), catDto);

            int poemCount = poems.Count;

            foreach (var poem in poems)
            {
                sectionsByPoem.TryGetValue(poem.Id, out var sections);
                versesByPoem.TryGetValue(poem.Id, out var verses);
                GanjoorMetre metre = poem.GanjoorMetreId != null && metresById.TryGetValue(poem.GanjoorMetreId.Value, out var m) ? m : null;

                tagsByPoem.TryGetValue(poem.Id, out var tags);

                await ExportPoemToJson(repoRoot, poem, metre, sections ?? new List<GanjoorPoemSection>(), verses ?? new List<GanjoorVerse>(),
                    tags ?? new List<PoemGeoDateTag>(), tagCollector);
                poemIdIndex[poem.Id] = poem.FullUrl;
            }

            foreach (var childCat in childCats)
            {
                poemCount += await ExportCatTreeToJson(context, repoRoot, childCat, metresById, sectionsByPoem, versesByPoem, tagsByPoem, catIdIndex, poemIdIndex, tagCollector);
            }

            return poemCount;
        }

        /// <summary>
        /// Pure in-memory write — no DB access. Sections/verses/metre are pre-loaded by the caller
        /// (see <see cref="ExportPoetContent"/>) instead of queried per poem.
        /// </summary>
        private async Task ExportPoemToJson(string repoRoot, GanjoorPoem poem, GanjoorMetre metre,
            List<GanjoorPoemSection> sections, List<GanjoorVerse> verses, List<PoemGeoDateTag> tags, PublicExportTagCollector tagCollector)
        {
            foreach (var tag in tags)
            {
                if (tag.LocationId != null)
                {
                    tagCollector.GeoTags.Add(new GeoTagPublicDto
                    {
                        PoemId = tag.PoemId,
                        CoupletIndex = tag.CoupletIndex,
                        LocationId = tag.LocationId.Value,
                        LunarYear = tag.LunarYear,
                        LunarMonth = tag.LunarMonth,
                        LunarDay = tag.LunarDay,
                        VerifiedDate = tag.VerifiedDate,
                        IgnoreInCategory = tag.IgnoreInCategory,
                        MachineGenerated = tag.MachineGenerated,
                    });
                }

                if (tag.PersonId != null)
                {
                    if (!tagCollector.PersonTags.TryGetValue(tag.PersonId.Value, out var personTags))
                    {
                        personTags = new List<PersonTagRefPublicDto>();
                        tagCollector.PersonTags[tag.PersonId.Value] = personTags;
                    }
                    personTags.Add(new PersonTagRefPublicDto
                    {
                        PoemId = tag.PoemId,
                        CoupletIndex = tag.CoupletIndex,
                        PersonMention = tag.PersonMention.ToString(),
                    });
                }
            }

            List<LiteraryTagPublicDto> literaryTags = null;
            if (tagCollector.LiteraryByPoem.TryGetValue(poem.Id, out var poemLiteraryTags) && poemLiteraryTags.Count > 0)
            {
                literaryTags = poemLiteraryTags.Select(ToPublicLiteraryTag).ToList();
                foreach (var lt in literaryTags)
                {
                    if (!tagCollector.LiteraryByDevice.TryGetValue(lt.DeviceId, out var byDevice))
                    {
                        byDevice = new List<LiteraryTagIndexEntryPublicDto>();
                        tagCollector.LiteraryByDevice[lt.DeviceId] = byDevice;
                    }
                    byDevice.Add(new LiteraryTagIndexEntryPublicDto
                    {
                        PoemId = poem.Id,
                        CoupletIndex = lt.CoupletIndex,
                        DeviceId = lt.DeviceId,
                        Uncertain = lt.Uncertain,
                        Note = lt.Note,
                        Origin = lt.Origin,
                        SourceKey = lt.SourceKey,
                        MachineGenerated = lt.MachineGenerated,
                        Parts = lt.Parts,
                    });
                }
            }

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
                    MetreId = s.GanjoorMetreId,
                    MetreRefSectionIndex = s.GanjoorMetreRefSectionIndex,
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
                    CoupletSummary = v.CoupletSummary,
                }).ToList(),
                Tags = tags.Count == 0 ? null : tags.Select(t => new PoemTagPublicDto
                {
                    CoupletIndex = t.CoupletIndex,
                    LocationId = t.LocationId,
                    PersonId = t.PersonId,
                    PersonMention = t.PersonId == null ? null : t.PersonMention.ToString(),
                    LunarYear = t.LunarYear,
                    LunarMonth = t.LunarMonth,
                    LunarDay = t.LunarDay,
                    VerifiedDate = t.VerifiedDate,
                    IgnoreInCategory = t.IgnoreInCategory,
                    MachineGenerated = t.MachineGenerated,
                    Note = t.Note,
                }).ToList(),
                LiteraryTags = literaryTags,
            };

            string path = Path.Combine(repoRoot, "poets", TrimLeadingSlash(poem.FullUrl) + ".json");
            await DeterministicJsonWriter.WriteIfChangedAsync(path, dto);
        }

        /// <summary>
        /// writes the shared people/place files and the tag-derived flat files: locations.json,
        /// people.json, person-relations.json, person-affiliations.json, geotags.json and the
        /// sharded index/person-tags. Runs after all poems are written because relation evidence
        /// is limited to couplets of poems that are part of the export (poemIdIndex). Who added
        /// a piece of evidence or when is never exported.
        /// </summary>
        private async Task ExportPeopleAndTags(RMuseumDbContext context, string repoRoot, PublicExportManifestDto manifest,
            Dictionary<int, string> poemIdIndex, PublicExportTagCollector tagCollector)
        {
            var locations = await context.GanjoorGeoLocations.AsNoTracking()
                                .OrderBy(l => l.Id)
                                .Select(l => new LocationPublicDto
                                {
                                    Id = l.Id,
                                    Name = l.Name,
                                    Latitude = l.Latitude,
                                    Longitude = l.Longitude,
                                    MachineGenerated = l.MachineGenerated,
                                })
                                .ToListAsync();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "locations.json"), locations);
            manifest.LocationsCount = locations.Count;

            var people = (await context.GanjoorRelatedPersons.AsNoTracking()
                                .OrderBy(p => p.Id)
                                .ToListAsync())
                            .Select(p => new PersonPublicDto
                            {
                                Id = p.Id,
                                Name = p.Name,
                                Description = p.Description,
                                Aliases = p.Aliases,
                                WikiUrl = p.WikiUrl,
                                Gender = p.Gender.ToString(),
                                Importance = p.Importance.ToString(),
                                BirthYearInLHijri = p.BirthYearInLHijri,
                                ValidBirthDate = p.ValidBirthDate,
                                DeathYearInLHijri = p.DeathYearInLHijri,
                                ValidDeathDate = p.ValidDeathDate,
                                BirthLocationId = p.BirthLocationId,
                                DeathLocationId = p.DeathLocationId,
                                FamilyTreeCaption = p.FamilyTreeCaption,
                                MachineGenerated = p.MachineGenerated,
                            })
                            .ToList();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "people.json"), people);
            manifest.PeopleCount = people.Count;

            var relationEvidences = (await context.GanjoorPersonRelationEvidences.AsNoTracking()
                                        .OrderBy(e => e.PoemId).ThenBy(e => e.CoupletIndex).ThenBy(e => e.Id)
                                        .Select(e => new { e.RelationId, e.PoemId, e.CoupletIndex, e.MasterCatId, e.Inferred })
                                        .ToListAsync())
                                    .Where(e => poemIdIndex.ContainsKey(e.PoemId))
                                    .GroupBy(e => e.RelationId)
                                    .ToDictionary(g => g.Key, g => g.Select(e => new PersonEvidencePublicDto
                                    {
                                        PoemId = e.PoemId,
                                        CoupletIndex = e.CoupletIndex,
                                        MasterCatId = e.MasterCatId,
                                        Inferred = e.Inferred ? true : (bool?)null,
                                    }).ToList());
            var relations = (await context.GanjoorPersonRelations.AsNoTracking().OrderBy(r => r.Id).ToListAsync())
                                .Select(r => new PersonRelationPublicDto
                                {
                                    Id = r.Id,
                                    Person1Id = r.Person1Id,
                                    Person2Id = r.Person2Id,
                                    RelationType = r.RelationType.ToString(),
                                    DegreeHint = r.DegreeHint,
                                    Note = r.Note,
                                    Evidence = relationEvidences.TryGetValue(r.Id, out var re) ? re : new List<PersonEvidencePublicDto>(),
                                })
                                .ToList();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "person-relations.json"), relations);

            var affiliationEvidences = (await context.GanjoorPersonAffiliationEvidences.AsNoTracking()
                                        .OrderBy(e => e.PoemId).ThenBy(e => e.CoupletIndex).ThenBy(e => e.Id)
                                        .Select(e => new { e.AffiliationId, e.PoemId, e.CoupletIndex, e.MasterCatId })
                                        .ToListAsync())
                                    .Where(e => poemIdIndex.ContainsKey(e.PoemId))
                                    .GroupBy(e => e.AffiliationId)
                                    .ToDictionary(g => g.Key, g => g.Select(e => new PersonEvidencePublicDto
                                    {
                                        PoemId = e.PoemId,
                                        CoupletIndex = e.CoupletIndex,
                                        MasterCatId = e.MasterCatId,
                                    }).ToList());
            var affiliations = (await context.GanjoorPersonAffiliations.AsNoTracking().OrderBy(a => a.Id).ToListAsync())
                                .Select(a => new PersonAffiliationPublicDto
                                {
                                    Id = a.Id,
                                    Person1Id = a.Person1Id,
                                    Person2Id = a.Person2Id,
                                    AffiliationType = a.AffiliationType.ToString(),
                                    Note = a.Note,
                                    Evidence = affiliationEvidences.TryGetValue(a.Id, out var ae) ? ae : new List<PersonEvidencePublicDto>(),
                                })
                                .ToList();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "person-affiliations.json"), affiliations);

            // poets are exported in poet order, not poem order: sort so the file is stable
            var geoTags = tagCollector.GeoTags
                            .OrderBy(t => t.PoemId).ThenBy(t => t.CoupletIndex).ThenBy(t => t.LocationId)
                            .ThenBy(t => t.LunarYear).ThenBy(t => t.LunarMonth).ThenBy(t => t.LunarDay)
                            .ToList();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "geotags.json"), geoTags);
            manifest.GeoTagsCount = geoTags.Count;

            int personTagsCount = 0;
            foreach (var bucket in tagCollector.PersonTags.GroupBy(kv => kv.Key / PersonIndexShardSize))
            {
                var shard = bucket.OrderBy(kv => kv.Key).ToDictionary(
                    kv => kv.Key.ToString(),
                    kv => kv.Value.OrderBy(t => t.PoemId).ThenBy(t => t.CoupletIndex).ThenBy(t => t.PersonMention).ToList());
                personTagsCount += shard.Values.Sum(v => v.Count);
                await DeterministicJsonWriter.WriteIfChangedAsync(
                    Path.Combine(repoRoot, "index", "person-tags", $"{bucket.Key}.json"), shard);
            }
            manifest.PersonTagsCount = personTagsCount;
            manifest.PersonIndexShardSize = PersonIndexShardSize;

            await ExportLiteraryData(context, repoRoot, manifest, tagCollector);
        }

        /// <summary>
        /// writes literary-devices.json (approved devices with their slots), literary-concepts.json (approved
        /// concepts), literary-sources.json (origin and license of imported tags) and one
        /// index/literary-tags/{deviceId}.json per device. Who suggested or approved a device, concept or tag,
        /// and when, is never exported; neither is a device's Source field.
        /// </summary>
        private async Task ExportLiteraryData(RMuseumDbContext context, string repoRoot, PublicExportManifestDto manifest, PublicExportTagCollector tagCollector)
        {
            var devices = (await context.LiteraryDevices.AsNoTracking().Include(d => d.Slots)
                                .Where(d => d.Status == LiteraryEntityStatus.Approved)
                                .OrderBy(d => d.Id)
                                .ToListAsync())
                            .Select(d => new LiteraryDevicePublicDto
                            {
                                Id = d.Id,
                                Key = d.Key,
                                Name = d.Name,
                                Category = d.Category.ToString(),
                                Definition = d.Definition,
                                Example = d.Example,
                                IsCore = d.IsCore,
                                Slots = d.Slots.OrderBy(s => s.Order).ThenBy(s => s.Id).Select(s => new LiteraryDeviceSlotPublicDto
                                {
                                    Id = s.Id,
                                    Key = s.Key,
                                    Name = s.Name,
                                    Kind = s.Kind.ToString(),
                                    Required = s.Required,
                                    AllowMultiple = s.AllowMultiple,
                                    Order = s.Order,
                                }).ToList(),
                            })
                            .ToList();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "literary-devices.json"), devices);
            manifest.LiteraryDevicesCount = devices.Count;

            var concepts = await context.LiteraryConcepts.AsNoTracking()
                                .Where(c => c.Status == LiteraryEntityStatus.Approved)
                                .OrderBy(c => c.Id)
                                .Select(c => new LiteraryConceptPublicDto { Id = c.Id, Name = c.Name, Aliases = c.Aliases, ParentId = c.ParentId })
                                .ToListAsync();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "literary-concepts.json"), concepts);
            manifest.LiteraryConceptsCount = concepts.Count;

            var sources = await context.LiteraryTagSources.AsNoTracking()
                                .OrderBy(s => s.Key)
                                .Select(s => new LiteraryTagSourcePublicDto
                                {
                                    Key = s.Key,
                                    Name = s.Name,
                                    Title = s.Title,
                                    Authors = s.Authors,
                                    Url = s.Url,
                                    Description = s.Description,
                                    License = s.License,
                                    LicenseText = s.LicenseText,
                                })
                                .ToListAsync();
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, "literary-sources.json"), sources);

            int literaryTagsCount = 0;
            foreach (var device in devices)
            {
                tagCollector.LiteraryByDevice.TryGetValue(device.Id, out var entries);
                var sorted = (entries ?? new List<LiteraryTagIndexEntryPublicDto>())
                                .OrderBy(e => e.PoemId).ThenBy(e => e.CoupletIndex)
                                .ThenBy(e => e.Parts.Count > 0 ? e.Parts[0].CoupletIndex : 0)
                                .ThenBy(e => e.Parts.Count > 0 ? e.Parts[0].Misra : 0)
                                .ThenBy(e => e.Parts.Count > 0 ? e.Parts[0].WordStart ?? 0 : 0)
                                .ToList();
                literaryTagsCount += sorted.Count;
                await DeterministicJsonWriter.WriteIfChangedAsync(
                    Path.Combine(repoRoot, "index", "literary-tags", $"{device.Id}.json"), sorted);
            }
            manifest.LiteraryTagsCount = literaryTagsCount;
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
- `GET {t.Poem}` — a poem: metre, rhyme, sections, verses, and `Tags` (see below) when it has any

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

## People, places and tagged couplets

Couplets are tagged with places, dates and people (the people may be historical or legendary
figures, not only poets). Only approved tags are exported, with no information about who suggested
or approved them. A couplet is addressed by its poem id and `CoupletIndex` (the same value as in
the poem file's `Verses`).

- `GET {t.Locations}` — places: id, name, latitude, longitude
- `GET {t.People}` — people: names, aliases, description, gender, life dates, birth/death place ids
- `GET {t.PersonRelations}` — kinship (Parent, Sibling, Spouse, Ancestor). For Parent/Ancestor,
  `Person1Id` is the parent/ancestor. `Evidence` lists the attesting couplets; `MasterCatId` is
  the category id of the *book* in which a couplet attests the relation — the same two people can
  be related differently in different books (e.g. a different father in another recension), so a
  family tree should be built per book from the evidence, not from the bare relation list.
- `GET {t.PersonAffiliations}` — non-family ties (minister, patron, panegyrized, ...), same
  evidence shape
- `GET {t.GeoTags}` — every place/date tag in one file, for maps and timelines
- `GET {t.PersonTagsIndexShard}` — where a person is tagged: person id X is in shard
  `{{X / {PersonIndexShardSize}}}` (integer division) and maps to the list of poem id / couplet index
  pairs; `PersonMention` is `Participant` (part of what the couplet tells) or `Allusion` (a passing
  reference)
- the per-poem file also carries the same tags in its `Tags` list, with `LocationId` / `PersonId`
  pointing into the files above

Many tags and relations are created with AI assistance and reviewed by a human moderator;
`MachineGenerated` marks tags and records that were created by the AI.

## Literary devices (آرایه‌های ادبی)

Poems are annotated with literary devices such as تشبیه, استعاره or جناس. Only approved devices and
tags are exported, with no information about who suggested or approved them.

- `GET {t.LiteraryDevices}` — the devices: id, key, name, category, definition, example and `Slots`.
  A tag is made of one value per slot (e.g. a تشبیه has «مشبه» and «مشبه‌به»); a slot's `Kind` says what
  its value holds: `Span` (words of the poem), `SpanConcept` (words and the concept they stand for),
  `Concept` or `Text`. `Required` / `AllowMultiple` tell whether the slot must be filled / can repeat.
- `GET {t.LiteraryConcepts}` — the concepts slot values refer to (e.g. «شمع», «زلف»): id, name,
  aliases (separated by `|`) and an optional `ParentId`
- `GET {t.LiteraryTagSources}` — origin and license of imported annotations. Tags with
  `Origin` = `AcademicCorpus` name their entry in `SourceKey`; redistributing those tags means
  keeping that attribution and license
- `GET {t.LiteraryTagsByDevice}` — every tag of one device across all poems, each with its `PoemId`
- the per-poem file carries its own tags in `LiteraryTags`

In a tag, `CoupletIndex` is the 1-based number of the main couplet as numbered on ganjoor.net (`0`
means the whole poem). A part located in the text has its own `CoupletIndex`, `Misra` (1 or 2) and
`WordStart` / `WordEnd` (1-based, inclusive; the words of a hemistich split at whitespace).
`Uncertain` marks tags the annotator was not sure about. `Origin` is `Editor`, `AcademicCorpus`,
`Algorithm` or `AI`; `MachineGenerated` is true for the last two.

## Not included

No comments, bookmarks, reading history, edit/correction history, or any other user-account-linked
data — see the repo this data set is exported from for details. Only `Published` poets/categories/
poems are included. Tags and relations carry no information about who suggested or approved them.

## Search

Not available as a static endpoint in this data set yet.
";
        }

        /// <summary>
        /// generates the repo-root README.md every run — the landing document GitHub shows by
        /// default, so this is where a first-time visitor's "where do I even start?" question
        /// needs to be answered in the first few lines, before they'd ever think to look for
        /// API.md.
        /// </summary>
        private static string BuildReadmeMarkdown(PublicExportManifestDto manifest)
        {
            return
$@"# ganjoor-data

**[▶ Live demo](https://ganjoor.github.io/mini/)** — مین‌گنجور, a minimal reading app built
entirely on this data, running client-side in your browser with no server of its own.

Public, git-tracked export of [Ganjoor](https://ganjoor.net)'s poetry content — poets,
categories, and poems — plus the people, places and date/person/place tags attached to couplets —
allowlisted to contain none of the site's user-account-linked data
(comments, bookmarks, edit history, etc.). Generated from
[GanjoorService](https://github.com/ganjoor/GanjoorService); see that repo if you're looking for
the application this data set is exported from, or want to run your own copy of Ganjoor locally
using this data.

Currently tracks **{manifest.PoetsCount} poets** / **{manifest.PoemsCount} poems**, generated {manifest.GeneratedAtUtc}.

## Where do I start?

- **[`manifest.json`](manifest.json)** — the full list of poets (id, nickname, and path), plus the
  schema version and URL templates for every other file kind in this repo. This is the list to
  start from if you're building a client and need ""which poets exist and what are their ids"".
- **[`people.json`](people.json)**, **[`locations.json`](locations.json)**,
  **[`person-relations.json`](person-relations.json)**, **[`geotags.json`](geotags.json)** — the
  people, places, family relations (attested per book) and tagged couplets; see `API.md`.
- **[`literary-devices.json`](literary-devices.json)**, **[`literary-concepts.json`](literary-concepts.json)**
  and **[`literary-sources.json`](literary-sources.json)** — the literary devices (آرایه‌های ادبی) with
  their slots, the concepts they refer to, and the license of imported annotations; the tags themselves
  are in each poem file and in `index/literary-tags/`.
- **[`API.md`](API.md)** — how to fetch any of this over plain HTTPS (no server needed, via
  jsDelivr), including how to resolve a bare numeric id to its path.

## Not included

No comments, bookmarks, reading history, edit/correction history, or any other data linked to a
user account. See `API.md` for the full list of what each file kind does and doesn't contain.
";
        }
    }
}
