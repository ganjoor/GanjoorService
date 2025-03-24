using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RMuseum.Utils
{
    /// <summary>
    /// ganjoor poem tools
    /// </summary>
    public static class GanjoorPoemTools
    {
        /// <summary>
        /// get an excerpt for the poem
        /// </summary>
        /// <param name="poemHtml"></param>
        /// <returns></returns>
        public static string GetPoemHtmlExcerpt(string poemHtml)
        {
            while (poemHtml.IndexOf("id=\"bn") != -1)
            {
                int idxbn1 = poemHtml.IndexOf(" id=\"bn");
                int idxbn2 = poemHtml.IndexOf("\"", idxbn1 + " id=\"bn".Length);
                poemHtml = poemHtml.Substring(0, idxbn1) + poemHtml.Substring(idxbn2 + 1);
            }

            poemHtml = poemHtml.Replace("<div class=\"b\">", "").Replace("<div class=\"b2\">", "").Replace("<div class=\"m1\">", "").Replace("<div class=\"m2\">", "").Replace("</div>", "");

            int index = poemHtml.IndexOf("<p>");
            int count = 0;
            while (index != -1 && count < 5)
            {
                index = poemHtml.IndexOf("<p>", index + 1);
                count++;
            }

            if (index != -1)
            {
                poemHtml = poemHtml.Substring(0, index);
                poemHtml += "<p>[...]</p>";
            }

            return poemHtml;
        }

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
