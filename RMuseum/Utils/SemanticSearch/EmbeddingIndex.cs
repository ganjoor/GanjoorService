using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RMuseum.Utils.SemanticSearch
{
    public class EmbeddingIndexMetadata
    {
        public string Model { get; set; }
        public int Dimension { get; set; }
        public string PoolingMethod { get; set; }
        public bool Normalized { get; set; }
        public string GeneratedAtUtc { get; set; }
        public int Count { get; set; }
        public int[] Ids { get; set; }
    }

    /// <summary>
    /// Loads the published embeddings.f32 (raw N x D float32, row-major, sorted by poem id
    /// ascending per the export's own writer) + embeddings-index.json into memory once, and does
    /// brute-force cosine similarity search against them.
    ///
    /// Brute force, not a vector database, is a deliberate choice at this scale — ~130k poems x
    /// 1024 dimensions is ~530MB, comfortably held in RAM, and a full linear scan per query is
    /// fast enough (low tens of milliseconds) that a dedicated vector index/database would be
    /// solving a problem this corpus doesn't actually have. Revisit only if the corpus grows by
    /// an order of magnitude or query volume becomes very high.
    ///
    /// Thread-safety: immutable after Load() — every field is only ever written once, during
    /// loading, before this instance is published to any other thread (e.g. via DI singleton
    /// registration). Safe for concurrent reads (searches) from multiple requests afterward.
    /// </summary>
    public class EmbeddingIndex
    {
        private readonly float[] _vectors; // flat, row-major: vectors[id_index * Dimension + d]
        private readonly int[] _poemIds; // poemIds[id_index] is the poem id for that row
        public EmbeddingIndexMetadata Metadata { get; }

        private EmbeddingIndex(float[] vectors, int[] poemIds, EmbeddingIndexMetadata metadata)
        {
            _vectors = vectors;
            _poemIds = poemIds;
            Metadata = metadata;
        }

        /// <summary>
        /// Loads embeddings.f32 + embeddings-index.json from the given directory. Intended to be
        /// called once at startup (e.g. from a DI factory) — this reads and holds ~530MB in
        /// memory, not something to redo per-request.
        /// </summary>
        public static EmbeddingIndex Load(string embeddingsDirectory)
        {
            string indexPath = Path.Combine(embeddingsDirectory, "embeddings-index.json");
            string binPath = Path.Combine(embeddingsDirectory, "embeddings.f32");

            if (!File.Exists(indexPath))
                throw new FileNotFoundException($"embeddings-index.json not found in '{embeddingsDirectory}'", indexPath);
            if (!File.Exists(binPath))
                throw new FileNotFoundException($"embeddings.f32 not found in '{embeddingsDirectory}'", binPath);

            var metadata = JsonSerializer.Deserialize<EmbeddingIndexMetadata>(
                File.ReadAllText(indexPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (metadata == null || metadata.Count <= 0 || metadata.Dimension <= 0)
                throw new InvalidDataException($"embeddings-index.json in '{embeddingsDirectory}' is missing count/dimension");
            if (metadata.Ids == null || metadata.Ids.Length != metadata.Count)
                throw new InvalidDataException(
                    $"embeddings-index.json declares count={metadata.Count} but ids array has " +
                    $"{metadata.Ids?.Length ?? 0} entries — these must match");

            long expectedBytes = (long)metadata.Count * metadata.Dimension * sizeof(float);
            long actualBytes = new FileInfo(binPath).Length;
            if (actualBytes != expectedBytes)
                throw new InvalidDataException(
                    $"embeddings.f32 in '{embeddingsDirectory}' is {actualBytes} bytes, expected " +
                    $"{expectedBytes} (count={metadata.Count} x dimension={metadata.Dimension} x 4 bytes). " +
                    "The index and binary file don't agree — do not trust this data until re-verified " +
                    "(see verify_embeddings.py in the ganjoor-embeddings tooling).");

            var vectors = new float[metadata.Count * metadata.Dimension];
            using (var stream = File.OpenRead(binPath))
            using (var reader = new BinaryReader(stream))
            {
                var buffer = new byte[vectors.Length * sizeof(float)];
                int totalRead = 0;
                while (totalRead < buffer.Length)
                {
                    int read = reader.Read(buffer, totalRead, buffer.Length - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }
                if (totalRead != buffer.Length)
                    throw new InvalidDataException($"expected to read {buffer.Length} bytes from embeddings.f32, got {totalRead}");
                Buffer.BlockCopy(buffer, 0, vectors, 0, buffer.Length);
            }

            return new EmbeddingIndex(vectors, metadata.Ids, metadata);
        }

        /// <summary>
        /// Returns the topK poem ids most similar to queryVector, ranked descending by cosine
        /// similarity. queryVector must already be the SAME dimension as this index and, for the
        /// score to mean what it claims (a true cosine similarity), should already be
        /// L2-normalized the same way the indexed vectors are — see QueryEmbedder.
        ///
        /// If allowedPoemIds is provided (non-null), only poems in that set are eligible —
        /// everything else is scored as excluded and can never appear in the results, however
        /// similar it might be. Used for scoped search ("در کدام شعر حافظ" -> restrict to
        /// Hafez's poems) — still a full scan either way, just with cheap early-outs for
        /// excluded rows, since this corpus is small enough that a full scan is fast regardless.
        /// </summary>
        public List<(int PoemId, float Score)> FindTopSimilar(ReadOnlySpan<float> queryVector, int topK, ISet<int> allowedPoemIds = null)
        {
            if (queryVector.Length != Metadata.Dimension)
                throw new ArgumentException(
                    $"query vector has {queryVector.Length} dimensions, index has {Metadata.Dimension} — " +
                    "these must match (are the query and index using the same model?)");

            int count = _poemIds.Length;
            var scores = new float[count];

            // dot product of two unit vectors == cosine similarity - the vectors were already
            // L2-normalized at export time (see embeddings-index.json's "normalized": true), so
            // this is genuinely computing cosine similarity, not just a raw dot product
            for (int i = 0; i < count; i++)
            {
                if (allowedPoemIds != null && !allowedPoemIds.Contains(_poemIds[i]))
                {
                    scores[i] = float.NegativeInfinity; // excluded from this search - will never sort into the results
                    continue;
                }

                float dot = 0f;
                int baseIdx = i * Metadata.Dimension;
                for (int d = 0; d < Metadata.Dimension; d++)
                {
                    dot += _vectors[baseIdx + d] * queryVector[d];
                }
                scores[i] = dot;
            }

            var indices = Enumerable.Range(0, count).ToArray();
            // partial sort would be a fair perf optimization at larger scale; a full sort of
            // ~130k floats is already comfortably fast (low tens of ms) and simpler to trust
            Array.Sort(indices, (a, b) => scores[b].CompareTo(scores[a]));

            var results = new List<(int, float)>();
            for (int i = 0; i < count && results.Count < topK; i++)
            {
                int idx = indices[i];
                if (float.IsNegativeInfinity(scores[idx]))
                    break; // sorted descending - everything from here on is also excluded
                results.Add((_poemIds[idx], scores[idx]));
            }
            return results;
        }
    }
}
