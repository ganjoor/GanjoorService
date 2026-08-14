using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RMuseum.Utils.PublicDataExport
{
    /// <summary>
    /// Same no-op-if-unchanged, LF/UTF-8-no-BOM behavior as <see cref="DeterministicJsonWriter"/>,
    /// for plain-text files (currently just the generated API.md) rather than JSON.
    /// </summary>
    public static class TextFileWriter
    {
        private static readonly UTF8Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static async Task<bool> WriteIfChangedAsync(string path, string content)
        {
            content = content.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
            byte[] newBytes = _utf8NoBom.GetBytes(content);

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(path))
            {
                byte[] existingBytes = await File.ReadAllBytesAsync(path);
                if (existingBytes.Length == newBytes.Length)
                {
                    bool same = true;
                    for (int i = 0; i < existingBytes.Length; i++)
                    {
                        if (existingBytes[i] != newBytes[i]) { same = false; break; }
                    }
                    if (same) return false;
                }
            }

            await File.WriteAllBytesAsync(path, newBytes);
            return true;
        }
    }
}
