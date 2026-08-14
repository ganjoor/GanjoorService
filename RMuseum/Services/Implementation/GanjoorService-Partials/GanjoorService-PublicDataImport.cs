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
        /// Category (and root-poet) pages don't carry a real production page id in the public
        /// export — GanjoorPage isn't part of that data set — so this importer mints one
        /// deterministically from the category id, kept well clear of any real id range so it can
        /// never collide with an actual production GanjoorPage/GanjoorPoem id. Poem pages don't
        /// need this: they reuse the poem's own id, matching the convention already used by
        /// _ImportSQLiteCatChildren (see GanjoorService-SQLiteImport.cs).
        /// </summary>
        private static int SyntheticCatPageId(int catId) => 900_000_000 + catId;

        private static readonly JsonSerializerOptions _importJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// (Re)builds Ganjoor content — poets, categories, poems, verses, sections, and their
        /// GanjoorPage routing entries — from a public data export tree, read either from a local
        /// `git clone` or fetched over HTTP. Safe to run against an empty database (bootstrap) or
        /// one that already has some content (merge): every entity is looked up by its id first
        /// and only inserted if missing, so re-running never duplicates or overwrites anything —
        /// including content a developer may have hand-edited locally after a previous import.
        /// </summary>
        /// <param name="useHttp">true: fetch over HTTP (location is a base URL). false: read from a local folder (location is a path).</param>
        /// <param name="location">base URL or local folder path of the exported data tree</param>
        /// <param name="poetId">0 imports every poet in the export's manifest; a specific id imports only that poet — useful on a slow connection, or when a developer only needs one poet's data for local testing</param>
        public RServiceResult<bool> StartImportFromPublicDataRepo(bool useHttp, string location, int poetId = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(location))
                    return new RServiceResult<bool>(false, "location is required");

                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                    async token =>
                    {
                        using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                        {
                            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                            var job = (await jobProgressServiceEF.NewJob("ImportFromPublicDataRepo", "Reading manifest")).Result;

                            try
                            {
                                IPublicDataSource source = useHttp
                                    ? new HttpPublicDataSource(_httpClient, location)
                                    : new LocalFileSystemPublicDataSource(location);

                                string manifestJson = await source.ReadTextAsync("manifest.json");
                                if (manifestJson == null)
                                    throw new Exception($"manifest.json not found at '{location}' — check the path/URL");

                                var manifest = JsonSerializer.Deserialize<PublicExportManifestDto>(manifestJson, _importJsonOptions);

                                var poetsToImport = manifest.Poets;
                                if (poetId != 0)
                                {
                                    poetsToImport = manifest.Poets.Where(p => p.Id == poetId).ToList();
                                    if (poetsToImport.Count == 0)
                                        throw new Exception($"poet id {poetId} was not found in manifest.json");
                                }

                                int poetIndex = 0;
                                foreach (var poetEntry in poetsToImport)
                                {
                                    poetIndex++;
                                    await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * poetIndex / Math.Max(1, poetsToImport.Count)), $"Importing {poetEntry.Nickname}");
                                    await ImportPoetFromPublicData(context, source, poetEntry.Id, poetEntry.FullUrl);
                                }

                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
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

        private async Task ImportPoetFromPublicData(RMuseumDbContext context, IPublicDataSource source, int poetId, string poetFullUrl)
        {
            string nickname = poetFullUrl;

            if (!await context.GanjoorPoets.AnyAsync(p => p.Id == poetId))
            {
                string poetJson = await source.ReadTextAsync($"poets{poetFullUrl}/poet.json");
                if (poetJson == null)
                    return; // referenced in manifest but file missing — skip rather than fail the whole run

                var poetDto = JsonSerializer.Deserialize<PoetPublicDto>(poetJson, _importJsonOptions);
                nickname = poetDto.Nickname;

                context.GanjoorPoets.Add(new GanjoorPoet
                {
                    Id = poetDto.Id,
                    Name = poetDto.Name,
                    Nickname = poetDto.Nickname,
                    Description = poetDto.Description,
                    Published = true,
                    BirthYearInLHijri = poetDto.BirthYearInLHijri,
                    ValidBirthDate = poetDto.ValidBirthDate,
                    DeathYearInLHijri = poetDto.DeathYearInLHijri,
                    ValidDeathDate = poetDto.ValidDeathDate,
                });
                await context.SaveChangesAsync();
            }
            else
            {
                nickname = (await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync()).Nickname;
            }

            // the poet's root category IS the poet's landing page (e.g. /hafez) — imported the
            // same way as any category, just flagged as the tree's root for page-type purposes
            await ImportCatTreeFromPublicData(context, source, poetId, poetFullUrl, null, nickname, isRoot: true);
        }

        private async Task ImportCatTreeFromPublicData(RMuseumDbContext context, IPublicDataSource source, int poetId,
            string catFullUrl, int? parentPageId, string parentFullTitle, bool isRoot)
        {
            string catJson = await source.ReadTextAsync($"poets{catFullUrl}/_cat.json");
            if (catJson == null)
                return;

            var catDto = JsonSerializer.Deserialize<CatPublicDto>(catJson, _importJsonOptions);
            string fullTitle = isRoot ? parentFullTitle : $"{parentFullTitle} » {catDto.Title}";
            int catPageId = SyntheticCatPageId(catDto.Id);

            if (!await context.GanjoorCategories.AnyAsync(c => c.Id == catDto.Id))
            {
                context.GanjoorCategories.Add(new GanjoorCat
                {
                    Id = catDto.Id,
                    PoetId = catDto.PoetId,
                    ParentId = catDto.ParentId,
                    Title = catDto.Title,
                    UrlSlug = LastUrlSegment(catDto.FullUrl),
                    FullUrl = catDto.FullUrl,
                    Description = catDto.Description,
                    DescriptionHtml = catDto.DescriptionHtml,
                    BookName = catDto.BookName,
                    Published = true,
                    TableOfContentsStyle = GanjoorTOC.Analyse,
                });

                context.GanjoorPages.Add(new GanjoorPage
                {
                    Id = catPageId,
                    GanjoorPageType = isRoot ? GanjoorPageType.PoetPage : GanjoorPageType.CatPage,
                    Published = true,
                    PageOrder = -1,
                    Title = catDto.Title,
                    FullTitle = fullTitle,
                    UrlSlug = LastUrlSegment(catDto.FullUrl),
                    FullUrl = catDto.FullUrl,
                    HtmlText = "",
                    PoetId = poetId,
                    CatId = catDto.Id,
                    PostDate = DateTime.Now,
                    ParentId = parentPageId,
                });

                await context.SaveChangesAsync();
            }

            foreach (var poemRef in catDto.Poems)
            {
                if (await context.GanjoorPoems.AnyAsync(p => p.Id == poemRef.Id))
                    continue; // already imported — never re-fetch or overwrite

                await ImportPoemFromPublicData(context, source, poetId, catDto.Id, poemRef.Id, poemRef.FullUrl, catPageId, fullTitle);
            }

            foreach (var childRef in catDto.ChildCats)
            {
                await ImportCatTreeFromPublicData(context, source, poetId, childRef.FullUrl, catPageId, fullTitle, isRoot: false);
            }
        }

        private async Task ImportPoemFromPublicData(RMuseumDbContext context, IPublicDataSource source, int poetId, int catId,
            int poemId, string poemFullUrl, int parentPageId, string parentFullTitle)
        {
            string poemJson = await source.ReadTextAsync($"poets{poemFullUrl}.json");
            if (poemJson == null)
                return;

            var poemDto = JsonSerializer.Deserialize<PoemPublicDto>(poemJson, _importJsonOptions);

            var verses = poemDto.Verses.Select(v => new GanjoorVerse
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
            }).ToList();

            // HtmlText/PlainText aren't duplicated in the export — regenerated here with the same
            // formatting helpers the app itself uses (see GanjoorService-SQLiteImport.cs), so a
            // locally-imported poem renders exactly the way the current codebase renders it rather
            // than however it happened to render at export time.
            string htmlText = PrepareHtmlText(verses);
            string plainText = PreparePlainText(verses);

            var dbPoem = new GanjoorPoem
            {
                Id = poemId,
                CatId = catId,
                Title = poemDto.Title,
                FullTitle = poemDto.FullTitle,
                UrlSlug = LastUrlSegment(poemDto.FullUrl),
                FullUrl = poemDto.FullUrl,
                PlainText = plainText,
                HtmlText = htmlText,
                GanjoorMetreId = poemDto.Metre?.Id,
                RhymeLetters = poemDto.RhymeLetters,
                SourceName = poemDto.SourceName,
                SourceUrlSlug = poemDto.SourceUrlSlug,
                Language = poemDto.Language,
                PoemSummary = poemDto.PoemSummary,
                Published = true,
            };
            context.GanjoorPoems.Add(dbPoem);
            await context.SaveChangesAsync();

            foreach (var verse in verses)
            {
                context.GanjoorVerses.Add(verse);
            }
            await context.SaveChangesAsync();

            foreach (var section in poemDto.Sections)
            {
                context.GanjoorPoemSections.Add(new GanjoorPoemSection
                {
                    PoemId = poemId,
                    PoetId = poetId,
                    Index = section.Index,
                    Number = section.Number,
                    SectionType = Enum.Parse<PoemSectionType>(section.SectionType),
                    VerseType = Enum.Parse<VersePoemSectionType>(section.VerseType),
                    GanjoorMetreId = poemDto.Metre?.Id,
                    RhymeLetters = section.RhymeLetters,
                    PlainText = section.PlainText,
                    HtmlText = section.HtmlText,
                    PoemFormat = string.IsNullOrEmpty(section.PoemFormat) ? (GanjoorPoemFormat?)null : Enum.Parse<GanjoorPoemFormat>(section.PoemFormat),
                    Language = section.Language,
                    CoupletsCount = section.CoupletsCount,
                });
            }
            await context.SaveChangesAsync();

            context.GanjoorPages.Add(new GanjoorPage
            {
                // matches production convention: a poem's page id equals the poem's own id
                // (see GanjoorService-SQLiteImport.cs, dbPoemPage.Id = poemId)
                Id = poemId,
                GanjoorPageType = GanjoorPageType.PoemPage,
                Published = true,
                PageOrder = -1,
                Title = dbPoem.Title,
                FullTitle = dbPoem.FullTitle,
                UrlSlug = dbPoem.UrlSlug,
                FullUrl = dbPoem.FullUrl,
                HtmlText = dbPoem.HtmlText,
                PoetId = poetId,
                CatId = catId,
                PoemId = poemId,
                PostDate = DateTime.Now,
                ParentId = parentPageId,
            });
            await context.SaveChangesAsync();
        }

        private static string LastUrlSegment(string fullUrl)
        {
            if (string.IsNullOrEmpty(fullUrl)) return fullUrl;
            string trimmed = fullUrl.TrimEnd('/');
            int idx = trimmed.LastIndexOf('/');
            return idx == -1 ? trimmed : trimmed.Substring(idx + 1);
        }
    }
}
