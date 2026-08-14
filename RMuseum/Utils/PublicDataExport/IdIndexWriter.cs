using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Utils.PublicDataExport
{
    /// <summary>
    /// A bare numeric id (poem id, category id, ...) is meaningless to a static file tree unless
    /// something maps it to a path. Writing one giant id-&gt;path file doesn't scale to Ganjoor's
    /// poem count, so ids are bucketed by <c>id / shardSize</c> into small shard files a client can
    /// compute the name of directly — no lookup-before-the-lookup needed.
    /// </summary>
    public static class IdIndexWriter
    {
        /// <summary>
        /// Writes every id in <paramref name="idToPath"/> to poets-by-id.json (or configured file
        /// name) with no sharding — for tables small enough that one file is fine (e.g. poets: a
        /// few hundred rows).
        /// </summary>
        public static async Task WriteFlatIndexAsync(string repoRoot, string relativeFilePath, Dictionary<int, string> idToPath)
        {
            var ordered = idToPath.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            await DeterministicJsonWriter.WriteIfChangedAsync(Path.Combine(repoRoot, relativeFilePath), ordered);
        }

        /// <summary>
        /// Writes <paramref name="idToPath"/> as bucketed shard files under
        /// <paramref name="repoRoot"/>/index/{category}-by-id/{bucket}.json, where
        /// bucket = id / shardSize. Only buckets that actually contain ids get a file — an empty
        /// bucket produces no request-able file, which is fine since a client only ever asks for
        /// the bucket of an id it already has.
        /// </summary>
        public static async Task WriteShardedIndexAsync(string repoRoot, string category, Dictionary<int, string> idToPath, int shardSize)
        {
            var byBucket = idToPath.GroupBy(kv => kv.Key / shardSize);
            foreach (var bucket in byBucket)
            {
                var ordered = bucket.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
                string path = Path.Combine(repoRoot, "index", $"{category}-by-id", $"{bucket.Key}.json");
                await DeterministicJsonWriter.WriteIfChangedAsync(path, ordered);
            }
        }
    }
}
