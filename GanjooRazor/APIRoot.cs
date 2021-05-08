using Microsoft.Extensions.Configuration;
using System.IO;

namespace GanjooRazor
{
    /// <summary>
    /// API Root
    /// </summary>
    public class APIRoot
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
    }
}
