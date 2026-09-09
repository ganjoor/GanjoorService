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
    /// </summary>
    public class SemanticSearchService : ISemanticSearchService
    {
        private const int DefaultTopK = 10;
        private const int MaxTopK = 50;

        private readonly EmbeddingIndex _embeddingIndex;
        private readonly QueryEmbedder _queryEmbedder;

        public SemanticSearchService(EmbeddingIndex embeddingIndex, QueryEmbedder queryEmbedder)
        {
            _embeddingIndex = embeddingIndex;
            _queryEmbedder = queryEmbedder;
        }

        public async Task<SemanticSearchResponseDto> SearchAsync(string query, int? topK)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("query must not be empty", nameof(query));

            int k = topK.GetValueOrDefault(DefaultTopK);
            if (k <= 0 || k > MaxTopK)
                k = DefaultTopK;

            float[] queryVector = _queryEmbedder.EmbedQuery(query);
            List<(int PoemId, float Score)> topMatches = _embeddingIndex.FindTopSimilar(queryVector, k);

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
