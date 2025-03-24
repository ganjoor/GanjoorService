using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RMuseum.Utils
{
    public class GanjoorHtmlTools
    {
        public static string StripHtmlTags(string input)
        {
            // Remove HTML tags using Regex
            string textWithoutTags = Regex.Replace(input, "<.*?>", string.Empty);

            // Decode HTML entities (e.g., &amp; → &)
            return HttpUtility.HtmlDecode(textWithoutTags);
        }

        public static List<string> ExtractLinksWithRegex(string html)
        {
            List<string> links = new List<string>();
            string pattern = @"<(a|img)\b[^>]*?\b(href|src)\s*=\s*[""']?([^""' >]+)[""']?";
            MatchCollection matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    string url = match.Groups[3].Value;
                    if (!string.IsNullOrEmpty(url))
                        links.Add(url);
                }
            }

            return links.Distinct().ToList(); // Remove duplicates
        }
    }
}
