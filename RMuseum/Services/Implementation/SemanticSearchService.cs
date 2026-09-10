using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor.SemanticSearch;
using RMuseum.Utils.SemanticSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    public interface ISemanticSearchService
    {
        Task<SemanticSearchResponseDto> SearchAsync(SemanticSearchRequestDto request);
    }

    /// <summary>
    /// Deliberately NOT a GanjoorService partial, unlike everything else in this project.
    /// EmbeddingIndex (~530MB in memory) and QueryEmbedder (a loaded ONNX model) both need to be
    /// true singletons — constructed once at startup, never per-request — while GanjoorService
    /// and RMuseumDbContext are scoped per-request throughout this codebase. Injecting a
    /// singleton's dependencies into a per-request class (or vice versa) is a real DI lifetime
    /// bug, not just an inconsistency, so this stays a separate service registered as a
    /// singleton itself (see INTEGRATION.md for the exact registration).
    ///
    /// Depends on LazySemanticSearchResources rather than EmbeddingIndex/QueryEmbedder directly —
    /// deliberately, after a production incident where eager, throwing DI factories for those two
    /// meant a load failure (wrong/missing file paths) prevented GanjoorController itself from
    /// being constructed, taking down every endpoint under /api/ganjoor with a 503, not just
    /// semantic search. This class must never let a resource-loading failure become an unhandled
    /// exception that propagates past SearchAsync — see the catch below.
    ///
    /// LazyQueryScopeIndex is a SEPARATE lazy singleton from LazySemanticSearchResources on
    /// purpose — poet/category name detection ("در کدام شعر حافظ") is a genuinely independent
    /// concern from the embedding/model loading, with its own independent failure mode; a bug in
    /// one must not be able to disable the other.
    /// </summary>
    public class SemanticSearchService : ISemanticSearchService
    {
        private const int DefaultTopK = 10;
        private const int MaxTopK = 50;
        private const int PreviewVerseCount = 4; // ~2 couplets - enough for a result-card preview, not the whole poem

        private readonly LazySemanticSearchResources _resources;
        private readonly LazyQueryScopeIndex _queryScopeIndex;

        public SemanticSearchService(LazySemanticSearchResources resources, LazyQueryScopeIndex queryScopeIndex)
        {
            _resources = resources;
            _queryScopeIndex = queryScopeIndex;
        }

        public async Task<SemanticSearchResponseDto> SearchAsync(SemanticSearchRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
                throw new ArgumentException("query must not be empty", nameof(request));

            if (!_resources.TryGetResources(out var embeddingIndex, out var queryEmbedder, out var error))
            {
                throw new SemanticSearchUnavailableException(
                    "Semantic search is not available right now" + (error != null ? $": {error}" : "."));
            }

            int k = request.TopK.GetValueOrDefault(DefaultTopK);
            if (k <= 0 || k > MaxTopK)
                k = DefaultTopK;

            var response = new SemanticSearchResponseDto { Query = request.Query };

            // a fresh, short-lived DbContext per call - same pattern GanjoorService's background
            // jobs already use throughout this codebase, since a scoped/request DbContext can't
            // be injected into this singleton service
            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
            {
                // An explicit request.PoetId/CatId (from a future UI, e.g. a poet picker) always
                // wins; auto-detection only fills in whatever wasn't already specified. A
                // detection failure here is swallowed on top of LazyQueryScopeIndex's own
                // try/catch, as a second safety net - this is a nice-to-have, never worth
                // failing the whole search over.
                int? scopePoetId = request.PoetId;
                int? scopeCatId = request.CatId;
                try
                {
                    var scopeIndex = await _queryScopeIndex.TryGetIndexAsync(context);
                    if (scopeIndex != null)
                    {
                        var detected = scopeIndex.DetectScope(request.Query);
                        if (detected.HasAny)
                        {
                            scopePoetId = scopePoetId ?? detected.PoetId;
                            scopeCatId = scopeCatId ?? detected.CatId;
                            response.DetectedPoetName = detected.PoetName;
                            response.DetectedCategoryName = detected.CategoryName;
                        }
                    }
                }
                catch (Exception)
                {
                    // scope detection is best-effort only - fall through with no scope rather
                    // than fail the search
                }

                HashSet<int> allowedPoemIds = null;
                if (scopeCatId.HasValue)
                {
                    var ids = await context.GanjoorPoems.AsNoTracking()
                                        .Where(p => p.CatId == scopeCatId.Value)
                                        .Select(p => p.Id)
                                        .ToListAsync();
                    allowedPoemIds = new HashSet<int>(ids);
                }
                else if (scopePoetId.HasValue)
                {
                    var ids = await context.GanjoorPoems.AsNoTracking()
                                        .Where(p => p.Cat.PoetId == scopePoetId.Value)
                                        .Select(p => p.Id)
                                        .ToListAsync();
                    allowedPoemIds = new HashSet<int>(ids);
                }

                float[] queryVector = queryEmbedder.EmbedQuery(request.Query);
                List<(int PoemId, float Score)> topMatches = embeddingIndex.FindTopSimilar(queryVector, k, allowedPoemIds);

                var poemIds = topMatches.Select(m => m.PoemId).ToList();
                var poemsById = await context.GanjoorPoems.AsNoTracking()
                                        .Where(p => poemIds.Contains(p.Id))
                                        .ToDictionaryAsync(p => p.Id);

                foreach (var match in topMatches)
                {
                    // a poem id present in the embedding index but missing from the live DB
                    // (e.g. deleted/unpublished since the embeddings were generated) is silently
                    // skipped rather than failing the whole search for everyone else's results
                    if (!poemsById.TryGetValue(match.PoemId, out var poem))
                        continue;

                    // one small, bounded query per result (topK is capped at 50) rather than
                    // loading every verse of every matched poem just to keep the first few - a
                    // poem can have hundreds of verses, no reason to pull all of them over a
                    // preview snippet
                    var verses = await context.GanjoorVerses.AsNoTracking()
                                        .Where(v => v.PoemId == poem.Id)
                                        .OrderBy(v => v.VOrder)
                                        .Take(PreviewVerseCount)
                                        .ToListAsync();

                    response.Results.Add(new SemanticSearchResultDto
                    {
                        PoemId = poem.Id,
                        Title = poem.Title,
                        FullTitle = poem.FullTitle,
                        FullUrl = poem.FullUrl,
                        PoemSummary = poem.PoemSummary,
                        Score = match.Score,
                        Verses = verses.Select(v => new SemanticSearchVerseDto
                        {
                            VOrder = v.VOrder,
                            Position = v.VersePosition.ToString(),
                            Text = v.Text,
                        }).ToList(),
                    });
                }
            }

            return response;
        }
    }
}
