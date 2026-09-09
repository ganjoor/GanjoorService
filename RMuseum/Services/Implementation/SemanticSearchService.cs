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
        Task<SemanticSearchResponseDto> SearchAsync(string query, int? topK);
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
    /// </summary>
    public class SemanticSearchService : ISemanticSearchService
    {
        private const int DefaultTopK = 10;
        private const int MaxTopK = 50;

        private readonly LazySemanticSearchResources _resources;

        public SemanticSearchService(LazySemanticSearchResources resources)
        {
            _resources = resources;
        }

        public async Task<SemanticSearchResponseDto> SearchAsync(string query, int? topK)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("query must not be empty", nameof(query));

            if (!_resources.TryGetResources(out var embeddingIndex, out var queryEmbedder, out var error))
            {
                throw new SemanticSearchUnavailableException(
                    "Semantic search is not available right now" + (error != null ? $": {error}" : "."));
            }

            int k = topK.GetValueOrDefault(DefaultTopK);
            if (k <= 0 || k > MaxTopK)
                k = DefaultTopK;

            float[] queryVector = queryEmbedder.EmbedQuery(query);
            List<(int PoemId, float Score)> topMatches = embeddingIndex.FindTopSimilar(queryVector, k);

            var response = new SemanticSearchResponseDto { Query = query };

            // a fresh, short-lived DbContext per call - same pattern GanjoorService's background
            // jobs already use throughout this codebase, since a scoped/request DbContext can't
            // be injected into this singleton service
            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
            {
                var poemIds = topMatches.Select(m => m.PoemId).ToList();
                var poemsById = await context.GanjoorPoems.AsNoTracking()
                                        .Where(p => poemIds.Contains(p.Id))
                                        .ToDictionaryAsync(p => p.Id);

                foreach (var match in topMatches)
                {
                    // a poem id present in the embedding index but missing from the live DB
                    // (e.g. deleted/unpublished since the embeddings were generated) is silently
                    // skipped rather than failing the whole search for everyone else's results
                    if (poemsById.TryGetValue(match.PoemId, out var poem))
                    {
                        response.Results.Add(new SemanticSearchResultDto
                        {
                            PoemId = poem.Id,
                            Title = poem.Title,
                            FullTitle = poem.FullTitle,
                            FullUrl = poem.FullUrl,
                            PoemSummary = poem.PoemSummary,
                            Score = match.Score,
                        });
                    }
                }
            }

            return response;
        }
    }
}
