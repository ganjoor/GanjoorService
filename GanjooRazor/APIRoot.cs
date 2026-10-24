using Microsoft.Extensions.Configuration;
using System.IO;

namespace GanjooRazor
{
    /// <summary>
    /// API Root
    /// </summary>
    public static class APIRoot
    {
        /// <summary>
        /// url
        /// </summary>
        public static string Url
        {
            get
            {
                if (!string.IsNullOrEmpty(_url))
                    return _url;
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                    .Build();
                _url = configuration["APIRoot"];
                return _url;
            }
        }

        private static string _url = "";

        private static string _InternetUrl = "";

        /// <summary>
        /// internet accessible end point
        /// </summary>
        public static string InternetUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(_InternetUrl))
                    return _InternetUrl;
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                    .Build();
                _InternetUrl = configuration["GlobalAPIRoot"];
                return _InternetUrl;
            }
        }

        private static string _imagesUrl = "";

        /// <summary>
        /// Static assets host (breadcrumb icons etc.), configurable through the "ImagesRoot"
        /// appsettings value; defaults to https://i.ganjoor.net
        /// </summary>
        public static string ImagesUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(_imagesUrl))
                    return _imagesUrl;
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                    .Build();
                string value = configuration["ImagesRoot"];
                _imagesUrl = string.IsNullOrWhiteSpace(value) ? "https://i.ganjoor.net" : value.Trim().TrimEnd('/');
                return _imagesUrl;
            }
        }

        /// <summary>
        /// The host stored in image urls kept in the database (thumbnails, covers, ...)
        /// </summary>
        public const string StoredImagesHost = "https://i.ganjoor.net";

        private static bool? _offlineMode;

        /// <summary>
        /// "OfflineMode" appsettings flag: true when this instance is an offline copy of the site
        /// </summary>
        public static bool OfflineMode
        {
            get
            {
                if (_offlineMode.HasValue)
                    return _offlineMode.Value;
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                    .Build();
                _offlineMode = bool.TryParse(configuration["OfflineMode"], out var value) && value;
                return _offlineMode.Value;
            }
        }

        /// <summary>
        /// Image urls received from the service (ThumbnailImageUrl, CoverThumbnailImageUrl, ...) start
        /// with <see cref="StoredImagesHost"/> because that is what is stored in the database. In
        /// offline mode this swaps that host for the configured "ImagesRoot" value, so an offline
        /// copy can serve the images itself. Outside offline mode the url is returned unchanged.
        /// </summary>
        public static string OfflineImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url) || !OfflineMode)
                return url;
            return url.Replace(StoredImagesHost, ImagesUrl, System.StringComparison.Ordinal);
        }

        private static string _semanticSearchUrl = "";

        /// <summary>
        /// Semantic search API endpoint — deliberately configurable separately from
        /// Url/InternetUrl. Currently points at a physically separate domain/app pool
        /// (ganjgah.ir) hosting the same RMuseum codebase, so that a problem with this one
        /// feature (a native ONNX Runtime crash, a performance issue, anything) can't affect
        /// api.ganjoor.net or the main site at all — proven necessary by an actual production
        /// incident, not a hypothetical precaution. Point this at the main API root instead once
        /// the feature has run stably on its own domain for a while.
        /// </summary>
        public static string SemanticSearchUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(_semanticSearchUrl))
                    return _semanticSearchUrl;
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                    .Build();
                _semanticSearchUrl = configuration["SemanticSearchAPIRoot"];
                return _semanticSearchUrl;
            }
        }
    }
}
