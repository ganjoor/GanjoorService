using Microsoft.Extensions.Configuration;

namespace RMuseum.Utils
{
    /// <summary>
    /// Base address of the host that serves static assets (recitation mp3/ogg/xml files, GDB packages,
    /// logos, ...). Configurable through the "ExternalAssets:RootUrl" appsettings value; the
    /// default is the historical https://i.ganjoor.net.
    /// </summary>
    /// <remarks>
    /// Static on purpose: it is read by model classes (e.g. Recitation.Mp3Url) that have no access to
    /// dependency injection. Startup calls <see cref="Configure"/> once at application start.
    /// </remarks>
    public static class ExternalAssets
    {
        /// <summary>
        /// Default value used when nothing is configured
        /// </summary>
        public const string DefaultRootUrl = "https://i.ganjoor.net";

        private static string _rootUrl = DefaultRootUrl;

        /// <summary>
        /// Root url without a trailing slash, e.g. https://i.ganjoor.net
        /// </summary>
        public static string RootUrl => _rootUrl;

        /// <summary>
        /// Reads "ExternalAssets:RootUrl" from configuration (falls back to the default when empty)
        /// </summary>
        /// <param name="configuration"></param>
        public static void Configure(IConfiguration configuration)
        {
            string value = configuration?.GetSection("ExternalAssets")["RootUrl"];
            _rootUrl = string.IsNullOrWhiteSpace(value) ? DefaultRootUrl : value.Trim().TrimEnd('/');
        }
    }
}
