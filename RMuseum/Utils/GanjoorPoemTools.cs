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
            textWithoutTags = HttpUtility.HtmlDecode(textWithoutTags);

            //it seems some codes are not correctly converted:
            textWithoutTags = textWithoutTags.Replace("&zwnj;", "‌");
            textWithoutTags = textWithoutTags.Replace("&laquo;", "»");
            textWithoutTags = textWithoutTags.Replace("&raquo;", "«");

            return textWithoutTags;
        }
    }
}
