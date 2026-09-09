using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RMuseum.Utils.SemanticSearch
{
    /// <summary>
    /// Embeds a single user-typed search query using the SAME ONNX model + tokenizer as
    /// scripts/generate_embeddings.py (the Python side that indexed the poem corpus) — this is
    /// non-negotiable: a query embedded in a different space than the documents it's being
    /// compared against produces meaningless similarity scores, silently (no error, just bad
    /// results), not something that shows up as a crash.
    ///
    /// UNTESTED — flagging this more strongly than usual, because this file stacks two risks
    /// that weren't present anywhere else in this project:
    ///   1. This environment has no .NET SDK and no network access to onnxruntime/the model, so
    ///      none of this has actually been compiled or run.
    ///   2. Even once it compiles, BpeTokenizer.Create(vocab, merges) builds a plain BPE
    ///      tokenizer from vocab.json + merges.txt — it may not capture every detail of Qwen's
    ///      full tokenizer.json spec (custom pre-tokenizer regex, added/special tokens). A
    ///      mismatch here wouldn't error either — it would silently tokenize text differently
    ///      than the Python `tokenizers` library did at index time, degrading search quality
    ///      without any visible failure.
    /// See VERIFICATION.md for a paired Python/C# test to run BEFORE trusting search results —
    /// do this before anything else in this phase.
    ///
    /// Query-time embedding is always exactly one query per call (a person typing into a search
    /// box), never a batch — unlike the Python indexing script, which batched many poems
    /// together and needed padding logic. That simplifies this class: no padding, no
    /// batch-dimension complexity, last-token pooling reduces to simply the final sequence
    /// position (no attention-mask lookup needed, since there's no padding to skip past).
    /// </summary>
    public class QueryEmbedder : IDisposable
    {
        // Confirmed via `python3 scripts/generate_embeddings.py --inspect-only` against the real
        // Qwen/Qwen3-Embedding-0.6B ONNX export (onnx-community/Qwen3-Embedding-0.6B-ONNX),
        // 2026-09-05. If the model is ever swapped for a different export/architecture, these
        // MUST be re-confirmed the same way (re-run --inspect-only, read off the real shapes) —
        // hardcoding them here (rather than trying to introspect symbolic dimension names from
        // .NET's ONNX Runtime API, whose support for that is less certain than Python's) is a
        // deliberate tradeoff: reliable given what we've already confirmed, but silently wrong
        // if the model changes without updating these.
        private const int NumLayers = 28;
        private const int NumKvHeads = 8;
        private const int HeadDim = 128;

        /// <summary>
        /// Per Qwen3-Embedding's documented convention, QUERIES (unlike documents) benefit from
        /// an instruction prefix. generate_embeddings.py deliberately does NOT add one to
        /// documents (poem summaries) — this asymmetry is the model's own documented design, not
        /// an inconsistency. If this template ever changes, or if it's ever added to/removed
        /// from the document side, both sides must be updated together, or queries and documents
        /// drift into subtly different embedding spaces. This exact wording hasn't been
        /// benchmarked for Persian poetry specifically — reasonable to A/B test once the basic
        /// pipeline is confirmed working (see VERIFICATION.md), not something to treat as final.
        /// </summary>
        private const string InstructionTemplate =
            "Instruct: Given a search query, retrieve Persian poems whose meaning matches it\nQuery: {0}";

        private readonly InferenceSession _session;
        private readonly Tokenizer _tokenizer;
        private readonly int _dimension;

        public QueryEmbedder(string modelPath, string vocabPath, string mergesPath, int dimension = 1024)
        {
            _session = new InferenceSession(modelPath);

            using (var vocabStream = File.OpenRead(vocabPath))
            using (var mergesStream = File.OpenRead(mergesPath))
            {
                _tokenizer = BpeTokenizer.Create(vocabStream, mergesStream);
            }

            _dimension = dimension;
        }

        /// <summary>
        /// Returns an L2-normalized embedding for queryText, comparable via dot product against
        /// EmbeddingIndex's vectors (which are normalized the same way).
        /// </summary>
        public float[] EmbedQuery(string queryText)
        {
            string instructedText = string.Format(InstructionTemplate, queryText);

            IReadOnlyList<int> tokenIds = _tokenizer.EncodeToIds(instructedText);
            int seqLen = tokenIds.Count;
            if (seqLen == 0)
                throw new ArgumentException("query tokenized to zero tokens — empty or whitespace-only query?", nameof(queryText));

            var inputIds = new DenseTensor<long>(new[] { 1, seqLen });
            var attentionMask = new DenseTensor<long>(new[] { 1, seqLen });
            var positionIds = new DenseTensor<long>(new[] { 1, seqLen });
            for (int i = 0; i < seqLen; i++)
            {
                inputIds[0, i] = tokenIds[i];
                attentionMask[0, i] = 1;
                positionIds[0, i] = i;
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                NamedOnnxValue.CreateFromTensor("position_ids", positionIds),
            };

            // empty (zero-length) KV cache for every layer - a single uncached forward pass over
            // the whole query, same reasoning as build_extra_inputs() in generate_embeddings.py
            for (int layer = 0; layer < NumLayers; layer++)
            {
                var emptyKey = new DenseTensor<float>(new[] { 1, NumKvHeads, 0, HeadDim });
                var emptyValue = new DenseTensor<float>(new[] { 1, NumKvHeads, 0, HeadDim });
                inputs.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{layer}.key", emptyKey));
                inputs.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{layer}.value", emptyValue));
            }

            using (var outputs = _session.Run(inputs, new[] { "last_hidden_state" }))
            {
                var hiddenStates = outputs.First(o => o.Name == "last_hidden_state").AsTensor<float>();
                // hiddenStates shape: (1, seqLen, _dimension)

                // last-token pooling: batch size 1, no padding, so the last real token is simply
                // the final sequence position - see the class docstring for why this doesn't
                // need the attention-mask lookup the Python (batched, padded) version does
                var pooled = new float[_dimension];
                for (int d = 0; d < _dimension; d++)
                {
                    pooled[d] = hiddenStates[0, seqLen - 1, d];
                }

                return L2Normalize(pooled);
            }
        }

        private static float[] L2Normalize(float[] vector)
        {
            double sumSquares = 0;
            foreach (var v in vector) sumSquares += (double)v * v;
            double norm = Math.Sqrt(sumSquares);
            if (norm < 1e-12) norm = 1e-12; // guard against a degenerate all-zero vector

            var result = new float[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                result[i] = (float)(vector[i] / norm);
            }
            return result;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
