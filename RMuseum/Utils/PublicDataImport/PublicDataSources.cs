using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace RMuseum.Utils.PublicDataImport
{
    /// <summary>
    /// Abstracts "read a relative path from the public data export" so the importer doesn't care
    /// whether it's reading a local `git clone` or fetching over HTTP from a CDN.
    /// </summary>
    public interface IPublicDataSource
    {
        /// <summary>
        /// Returns the file's text content, or null if it doesn't exist at this path
        /// (a missing file is a normal, expected outcome — e.g. a leaf category has no children).
        /// </summary>
        Task<string> ReadTextAsync(string relativePath);
    }

    /// <summary>
    /// Reads from a local folder — the expected path for a developer who already ran
    /// `git clone` on the public data repo, which is both the faster option and the one that
    /// doesn't put load on jsDelivr/GitHub for a full-corpus import.
    /// </summary>
    public class LocalFileSystemPublicDataSource : IPublicDataSource
    {
        private readonly string _rootPath;

        public LocalFileSystemPublicDataSource(string rootPath)
        {
            _rootPath = rootPath;
        }

        public async Task<string> ReadTextAsync(string relativePath)
        {
            string path = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                return null;
            return await File.ReadAllTextAsync(path);
        }
    }

    /// <summary>
    /// Fetches over HTTP — point <paramref name="baseUrl"/> at either the jsDelivr CDN URL
    /// (https://cdn.jsdelivr.net/gh/ORG/REPO@main/) or raw.githubusercontent.com. A missing file
    /// (404) is treated the same as "doesn't exist", not an error.
    /// </summary>
    public class HttpPublicDataSource : IPublicDataSource
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public HttpPublicDataSource(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl.TrimEnd('/') + "/";
        }

        public async Task<string> ReadTextAsync(string relativePath)
        {
            var response = await _httpClient.GetAsync(new Uri(_baseUrl + relativePath.TrimStart('/')));
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadAsStringAsync();
        }
    }
}
