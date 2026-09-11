using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace RMuseum.Utils.SemanticSearch
{
    /// <summary>
    /// Wraps EmbeddingIndex + QueryEmbedder loading so a failure (missing files, wrong paths,
    /// corrupt data) can NEVER take down anything else in the app.
    ///
    /// The original design registered EmbeddingIndex/QueryEmbedder as singletons whose DI
    /// factories called EmbeddingIndex.Load(...)/`new QueryEmbedder(...)` directly — both of
    /// which throw on failure. Because GanjoorController's constructor (indirectly, through
    /// ISemanticSearchService) depended on them, a load failure meant the controller itself
    /// couldn't be constructed — taking down EVERY endpoint under /api/ganjoor, not just semantic
    /// search, with a 503. That's exactly what happened in production. This class exists so that
    /// can't happen again: the actual load is deferred to first real use (not app/controller
    /// construction), attempted at most once, and a failure is caught, logged, and remembered —
    /// SearchAsync() then reports "search unavailable" as an ordinary result, not an exception
    /// that propagates into breaking anything else.
    ///
    /// Also worth knowing if this server runs multiple IIS worker processes for the same app
    /// pool: each process gets its own instance of this (and everything it loads) — "singleton"
    /// only means one instance per process, not per server. See the migration notes for the
    /// memory math this implies at scale.
    ///
    /// SemanticSearch:Enabled gates ALL of the above, checked before anything else runs — this
    /// is what actually keeps a disabled instance (e.g. api.ganjoor.net, where third-party apps
    /// shouldn't get this feature) from ever loading the ~530MB embeddings file or the ONNX model
    /// into memory at all, not just from serving requests. Defaults to disabled (fail-closed) if
    /// the key is missing entirely — deliberately, since the whole point of this flag is
    /// preventing resource loading on instances that shouldn't have it; an instance that's
    /// SUPPOSED to have it (ganjgah.ir) needs the key explicitly set to "True", not left implicit.
    /// </summary>
    public class LazySemanticSearchResources
    {
        private readonly object _lock = new object();
        private EmbeddingIndex _embeddingIndex;
        private QueryEmbedder _queryEmbedder;
        private string _loadError;
        private bool _attempted;

        private readonly bool _enabled;
        private readonly string _embeddingsDirectory;
        private readonly string _modelPath;
        private readonly string _vocabPath;
        private readonly string _mergesPath;
        private readonly int _dimension;
        private readonly ILogger<LazySemanticSearchResources> _logger;

        /// <summary>
        /// A gentle re-ranking multiplier applied to results whose PoemSummary is still
        /// AI-generated and un-reviewed (detected by the "هوش مصنوعی:" prefix ganjoor-data's own
        /// editing workflow requires removing once a human has reviewed/edited a summary — see
        /// SemanticSearchService for how this is actually applied). A soft nudge, not a filter:
        /// ~95% of summaries currently carry this prefix, so excluding them outright would gut
        /// coverage for most queries. Configurable (SemanticSearch:AiSummaryScorePenalty) rather
        /// than hardcoded, since the right effect size here is a judgment call worth tuning
        /// without a redeploy. Read here (not directly in SemanticSearchService) purely to reuse
        /// the config-reading this class already does — this value has nothing to do with the
        /// lazy-loaded embeddings/model themselves and is available even when Enabled is false.
        /// </summary>
        public float AiSummaryScorePenalty { get; }

        public LazySemanticSearchResources(IConfiguration configuration, ILogger<LazySemanticSearchResources> logger)
        {
            _enabled = string.Equals(configuration["SemanticSearch:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
            _embeddingsDirectory = configuration["SemanticSearch:EmbeddingsDirectory"];
            _modelPath = configuration["SemanticSearch:ModelPath"];
            _vocabPath = configuration["SemanticSearch:VocabPath"];
            _mergesPath = configuration["SemanticSearch:MergesPath"];
            _dimension = int.TryParse(configuration["SemanticSearch:Dimension"], out var d) ? d : 1024;
            AiSummaryScorePenalty = float.TryParse(configuration["SemanticSearch:AiSummaryScorePenalty"], out var p) ? p : 0.97f;
            _logger = logger;

            if (!_enabled)
            {
                _logger.LogInformation("Semantic search is disabled on this instance (SemanticSearch:Enabled is not \"True\") — resources will never be loaded.");
            }
        }

        /// <summary>
        /// Attempts to load the resources on first call (subsequent calls reuse the same result,
        /// success or failure — this never retries automatically; a fresh app start is required
        /// to try again after a config/file fix, which is the expected deploy-and-restart flow
        /// anyway). Returns true and populates both out parameters if available; returns false
        /// and populates <paramref name="error"/> otherwise. NEVER THROWS — that guarantee is the
        /// entire point of this class.
        ///
        /// If SemanticSearch:Enabled isn't "True", this returns false immediately, every call,
        /// without ever touching EmbeddingIndex.Load/QueryEmbedder's constructor — the actual
        /// mechanism that keeps a disabled instance from loading anything into memory at all.
        /// </summary>
        public bool TryGetResources(out EmbeddingIndex embeddingIndex, out QueryEmbedder queryEmbedder, out string error)
        {
            if (!_enabled)
            {
                embeddingIndex = null;
                queryEmbedder = null;
                error = "Semantic search is disabled on this instance.";
                return false;
            }

            if (!_attempted)
            {
                lock (_lock)
                {
                    if (!_attempted)
                    {
                        try
                        {
                            _embeddingIndex = EmbeddingIndex.Load(_embeddingsDirectory);
                            _queryEmbedder = new QueryEmbedder(_modelPath, _vocabPath, _mergesPath, _dimension);
                            _logger.LogInformation(
                                "Semantic search resources loaded: {Count} poems, dimension {Dimension}.",
                                _embeddingIndex.Metadata.Count, _embeddingIndex.Metadata.Dimension);
                        }
                        catch (Exception exp)
                        {
                            _loadError = exp.Message;
                            _embeddingIndex = null;
                            _queryEmbedder = null;
                            _logger.LogError(exp,
                                "Semantic search resources failed to load from '{EmbeddingsDirectory}' / '{ModelPath}' " +
                                "— semantic search will report unavailable, but this must not affect anything else.",
                                _embeddingsDirectory, _modelPath);
                        }
                        finally
                        {
                            _attempted = true;
                        }
                    }
                }
            }

            embeddingIndex = _embeddingIndex;
            queryEmbedder = _queryEmbedder;
            error = _loadError;
            return _embeddingIndex != null && _queryEmbedder != null;
        }
    }

    /// <summary>
    /// Thrown by SemanticSearchService when the underlying resources aren't available — the
    /// controller catches this specifically and returns HTTP 503 with the message, distinct from
    /// a plain 400/500, so a client (or a person checking logs) can tell "this feature isn't
    /// configured/loaded right now" apart from "the query itself was bad" or "something crashed".
    /// </summary>
    public class SemanticSearchUnavailableException : Exception
    {
        public SemanticSearchUnavailableException(string message) : base(message) { }
    }
}
