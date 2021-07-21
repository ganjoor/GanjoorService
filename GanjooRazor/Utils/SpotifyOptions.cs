using System.Collections.Generic;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// spotify options
    /// </summary>
    public class SpotifyOptions
    {
        private const string OptionFilePath = @"C:\Tools\NewSpotifyOptions\config.txt";

        /// <summary>
        /// options
        /// </summary>
        public static Dictionary<string, string> Options
        {
            get
            {
                if (_cachedOptions != null)
                    return _cachedOptions;
                Dictionary<string, string> options = new Dictionary<string, string>();
                options.Add("access_token", "");
                options.Add("refresh_token", "");
                options.Add("client_id", "");
                options.Add("client_secret", "");
                if (System.IO.File.Exists(OptionFilePath))
                {
                    string[] lines = System.IO.File.ReadAllLines(OptionFilePath);
                    int i = 0;
                    while (i < lines.Length)
                    {
                        if (options.ContainsKey(lines[i]))
                        {
                            if ((i + 1) < lines.Length)
                            {
                                options[lines[i]] = lines[i + 1];
                            }
                            i += 2;
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
                _cachedOptions = options;
                return options;
            }
            set
            {
                _cachedOptions = null;
                List<string> lines = new List<string>();
                foreach (string key in value.Keys)
                {
                    lines.Add(key);
                    lines.Add(value[key]);
                }
                System.IO.File.WriteAllLines(OptionFilePath, lines);
            }
        }

        private static Dictionary<string, string> _cachedOptions = null;
    }
}
