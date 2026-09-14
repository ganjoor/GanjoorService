using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RMuseum.Utils
{
    /// <summary>
    /// a legal music streaming/store platform accepted for poem music track links
    /// </summary>
    public class MusicPlatform
    {
        /// <summary>
        /// stable identifier
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// display name (Persian)
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// 16px icon path, null when no artwork is available yet
        /// </summary>
        public string IconUrl { get; init; }

        /// <summary>
        /// 32px icon path, null when no artwork is available yet
        /// </summary>
        public string LargeIconUrl { get; init; }

        /// <summary>
        /// oEmbed endpoint accepting a ?url= parameter, null when the platform has none
        /// </summary>
        public string OEmbedEndpoint { get; init; }

        /// <summary>
        /// accepted hosts, each matching itself and its subdomains
        /// </summary>
        public string[] Hosts { get; init; }

        /// <summary>
        /// accepted host pattern, used where the host varies by country
        /// </summary>
        public Regex HostPattern { get; init; }

        /// <summary>
        /// does the given lowercased host belong to this platform?
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public bool MatchesHost(string host)
        {
            if (HostPattern != null && HostPattern.IsMatch(host))
                return true;
            if (Hosts == null)
                return false;
            foreach (var acceptedHost in Hosts)
            {
                if (host == acceptedHost || host.EndsWith($".{acceptedHost}", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// validates user supplied music links against an allow-list of legal music platforms
    /// </summary>
    public static class MusicUrlValidator
    {
        /// <summary>
        /// maximum accepted url length
        /// </summary>
        public const int MaxUrlLength = 1024;

        /// <summary>
        /// accepted platforms, ordered so that more specific hosts win (YouTube Music before YouTube)
        /// </summary>
        /// <remarks>
        /// IconUrl/LargeIconUrl are only set for platforms whose artwork already exists under
        /// GanjooRazor/wwwroot/image; drop a {Key}16.png/{Key}.png pair in image/music and fill them in.
        /// </remarks>
        public static readonly IReadOnlyList<MusicPlatform> Platforms = new List<MusicPlatform>()
        {
            new MusicPlatform()
            {
                Key = "spotify",
                Name = "اسپاتیفای",
                Hosts = new[] { "open.spotify.com" },
                IconUrl = "/image/sp16.png",
                LargeIconUrl = "/image/spotify.png",
                OEmbedEndpoint = "https://open.spotify.com/oembed",
            },
            new MusicPlatform()
            {
                Key = "applemusic",
                Name = "اپل موزیک",
                Hosts = new[] { "music.apple.com", "itunes.apple.com" },
            },
            new MusicPlatform()
            {
                Key = "amazonmusic",
                Name = "آمازون موزیک",
                HostPattern = new Regex(@"^music\.amazon\.(com|com\.au|com\.br|com\.mx|com\.tr|co\.uk|co\.jp|ca|de|fr|it|es|nl|se|pl|in|sg|ae)$", RegexOptions.Compiled),
            },
            new MusicPlatform()
            {
                Key = "youtubemusic",
                Name = "یوتیوب موزیک",
                Hosts = new[] { "music.youtube.com" },
                OEmbedEndpoint = "https://www.youtube.com/oembed",
            },
            new MusicPlatform()
            {
                Key = "youtube",
                Name = "یوتیوب",
                Hosts = new[] { "youtube.com", "youtu.be" },
                OEmbedEndpoint = "https://www.youtube.com/oembed",
            },
            new MusicPlatform()
            {
                Key = "soundcloud",
                Name = "ساندکلاود",
                Hosts = new[] { "soundcloud.com" },
                OEmbedEndpoint = "https://soundcloud.com/oembed",
            },
            new MusicPlatform()
            {
                Key = "iheart",
                Name = "آی‌هارت رادیو",
                Hosts = new[] { "iheart.com" },
            },
            new MusicPlatform()
            {
                Key = "radiojavan",
                Name = "رادیو جوان",
                Hosts = new[] { "radiojavan.com" },
            },
            new MusicPlatform()
            {
                Key = "beeptunes",
                Name = "بیپ‌تونز",
                Hosts = new[] { "beeptunes.com" },
                LargeIconUrl = "/image/beeptunes.png",
            },
            new MusicPlatform()
            {
                Key = "khosousi",
                Name = "خصوصی",
                Hosts = new[] { "khosousi.com" },
            },
        };

        /// <summary>
        /// query parameters dropped during normalization so that the same track always yields the same url
        /// </summary>
        private static readonly string[] _trackingParameters = new[]
        {
            "si", "nd", "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content",
            "feature", "pp", "context", "_branch_match_id", "_branch_referrer", "ref", "referrer"
        };

        /// <summary>
        /// comma separated platform names, for user facing messages
        /// </summary>
        public static string PlatformNames
        {
            get
            {
                return string.Join("، ", Platforms.Select(p => p.Name));
            }
        }

        /// <summary>
        /// validate a user supplied music url and rewrite it to its canonical form
        /// </summary>
        /// <param name="url"></param>
        /// <param name="normalizedUrl"></param>
        /// <param name="platform"></param>
        /// <param name="error">Persian error message when validation fails</param>
        /// <returns></returns>
        public static bool TryNormalize(string url, out string normalizedUrl, out MusicPlatform platform, out string error)
        {
            normalizedUrl = null;
            platform = null;
            error = null;

            if (string.IsNullOrWhiteSpace(url))
            {
                error = "نشانی آهنگ خالی است.";
                return false;
            }

            url = url.Trim();

            if (url.Length > MaxUrlLength)
            {
                error = "نشانی آهنگ بیش از حد طولانی است.";
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                error = "نشانی آهنگ معتبر نیست.";
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                error = "نشانی آهنگ باید با https:// آغاز شود.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                error = "نشانی آهنگ معتبر نیست.";
                return false;
            }

            if (!uri.IsDefaultPort)
            {
                error = "نشانی آهنگ معتبر نیست.";
                return false;
            }

            string host = uri.IdnHost.ToLowerInvariant();

            platform = Platforms.FirstOrDefault(p => p.MatchesHost(host));

            if (platform == null)
            {
                error = $"تنها نشانی آهنگ از این سرویس‌ها پذیرفته می‌شود: {PlatformNames}.";
                return false;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("https://");
            builder.Append(host);
            builder.Append(uri.AbsolutePath);

            string query = _StripTrackingParameters(uri.Query);
            if (!string.IsNullOrEmpty(query))
            {
                builder.Append('?');
                builder.Append(query);
            }

            normalizedUrl = builder.ToString();

            return true;
        }

        /// <summary>
        /// platform of an already stored url, null when it belongs to none of the accepted platforms
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static MusicPlatform Detect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri))
                return null;
            string host = uri.IdnHost.ToLowerInvariant();
            return Platforms.FirstOrDefault(p => p.MatchesHost(host));
        }

        private static string _StripTrackingParameters(string query)
        {
            if (string.IsNullOrEmpty(query))
                return "";

            List<string> kept = new List<string>();
            foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int separatorIndex = pair.IndexOf('=');
                string key = separatorIndex < 0 ? pair : pair.Substring(0, separatorIndex);
                if (_trackingParameters.Contains(key.ToLowerInvariant()))
                    continue;
                kept.Add(pair);
            }

            return string.Join("&", kept);
        }
    }
}
