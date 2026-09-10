using Microsoft.Extensions.Logging;
using RMuseum.DbContext;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RMuseum.Utils.SemanticSearch
{
    /// <summary>
    /// Same "load once, never throw" pattern as LazySemanticSearchResources, but deliberately a
    /// SEPARATE singleton with its own independent failure domain: if the poet/category name
    /// lookup fails to load for any reason, that must only disable scope auto-detection
    /// ("در کدام شعر حافظ" -> unscoped, searches everything) — it must never affect the embedding
    /// index/model loading or plain (unscoped) search, which are a completely different concern.
    ///
    /// Uses a SemaphoreSlim rather than a plain `lock`, since the actual load is async (DB
    /// queries) and you can't `await` inside a `lock` block.
    /// </summary>
    public class LazyQueryScopeIndex
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private QueryScopeIndex _index;
        private bool _attempted;
        private readonly ILogger<LazyQueryScopeIndex> _logger;

        public LazyQueryScopeIndex(ILogger<LazyQueryScopeIndex> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads (once) and returns the scope index, or null if loading failed or hasn't
        /// succeeded yet — NEVER throws. The passed-in context is only actually used on the
        /// first call that does real work; a context created by the caller for its own
        /// SearchAsync call is reused here rather than this class creating its own, since it's
        /// only needed for the one-time load.
        /// </summary>
        public async Task<QueryScopeIndex> TryGetIndexAsync(RMuseumDbContext context)
        {
            if (_attempted)
                return _index;

            await _semaphore.WaitAsync();
            try
            {
                if (_attempted)
                    return _index;

                try
                {
                    _index = await QueryScopeIndex.LoadAsync(context);
                    _logger.LogInformation("Query scope index loaded.");
                }
                catch (Exception exp)
                {
                    _index = null;
                    _logger.LogError(exp,
                        "Query scope index failed to load — poet/category auto-detection will be " +
                        "unavailable, but this must not affect plain search.");
                }
                finally
                {
                    _attempted = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return _index;
        }
    }
}
