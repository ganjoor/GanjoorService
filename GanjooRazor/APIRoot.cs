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
