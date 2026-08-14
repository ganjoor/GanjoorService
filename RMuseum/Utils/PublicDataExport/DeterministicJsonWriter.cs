using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace RMuseum.Utils.PublicDataExport
{
    /// <summary>
    /// Writes JSON in a way that is stable across runs: fixed indentation, LF line endings,
    /// UTF-8 without BOM, a single trailing newline, and Persian/Arabic text left unescaped
    /// (the default encoder \u-escapes non-ASCII, which is both hard to review in a diff and
    /// bloats file size for a mostly-Persian dataset).
    ///
    /// Callers are responsible for sorting any collections before serializing — this class only
    /// guarantees stable *encoding*, not stable *ordering*, since ordering is a data decision.
    ///
    /// Files are only actually written to disk when the content differs from what's already
    /// there, so an unmodified poem never shows up in `git status` even if the whole tree is
    /// regenerated every run.
    /// </summary>
    public static class DeterministicJsonWriter
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private static readonly UTF8Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Serializes <paramref name="value"/> and writes it to <paramref name="path"/> only if the
        /// resulting bytes differ from the file's current content. Creates parent directories as needed.
        /// Returns true if the file was created or changed, false if it was already up to date.
        /// </summary>
        public static async Task<bool> WriteIfChangedAsync<T>(string path, T value)
        {
            string json = JsonSerializer.Serialize(value, _options);
            // normalize to LF and ensure exactly one trailing newline, regardless of host OS
            json = json.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
            byte[] newBytes = _utf8NoBom.GetBytes(json);

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(path))
            {
                byte[] existingBytes = await File.ReadAllBytesAsync(path);
                if (BytesEqual(existingBytes, newBytes))
                {
                    return false;
                }
            }

            await File.WriteAllBytesAsync(path, newBytes);
            return true;
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
