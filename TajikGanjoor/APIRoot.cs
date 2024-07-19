namespace TajikGanjoor
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
                _url = configuration["APIRoot"] ?? "https://api.ganjoor.net";
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
                _InternetUrl = configuration["GlobalAPIRoot"] ?? "https://api.ganjoor.net";
                return _InternetUrl;
            }
        }
    }
}
