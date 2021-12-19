
using RMuseum.Models.GanjoorAudio.ViewModels;
using System;
using System.Text;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// creates an RSS feed from recitations
    /// </summary>
    public static class RecitationsRssBuilder
    {
        /// <summary>
        /// build rss
        /// </summary>
        /// <param name="recitations"></param>
        /// <returns></returns>
        public static string Build(PublicRecitationViewModel[] recitations)
        {

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            builder.AppendLine("<rss version=\"2.0\"");
            builder.AppendLine("    xmlns:content=\"http://purl.org/rss/1.0/modules/content/\"");
            builder.AppendLine("    xmlns:wfw=\"http://wellformedweb.org/CommentAPI/\"");
            builder.AppendLine("    xmlns:dc=\"http://purl.org/dc/elements/1.1/\"");
            builder.AppendLine("    xmlns:atom=\"http://www.w3.org/2005/Atom\"");
            builder.AppendLine("    xmlns:sy=\"http://purl.org/rss/1.0/modules/syndication/\"");
            builder.AppendLine("    xmlns:slash=\"http://purl.org/rss/1.0/modules/slash/\"");
            builder.AppendLine("    xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\"");
            builder.AppendLine(">");

            builder.AppendLine("<channel>");

            builder.AppendLine("    <title>خوانش‌های گنجور</title>");
            builder.AppendLine($"    <atom:link href=\"{WebServiceUrl.Url}/api/audio/published/rss\" rel=\"self\" type=\"application/rss+xml\" />");
            builder.AppendLine("    <link>http://ava.ganjoor.net</link>");
            builder.AppendLine("    <description>دکلمه‌های صوتی اشعار گنجور</description>");

            DateTime dtLastUpdate = recitations.Length > 0 ? recitations[0].PublishDate : DateTime.Now;

            builder.AppendLine($"    <lastBuildDate>{dtLastUpdate:r}</lastBuildDate>");
            builder.AppendLine("    <language>fa-IR</language>");
            builder.AppendLine("    <sy:updatePeriod>hourly</sy:updatePeriod>");
            builder.AppendLine("    <sy:updateFrequency>1</sy:updateFrequency>");
            builder.AppendLine("    <image>");
            builder.AppendLine("        <url>https://ganjoor.net/image/rss.png</url>");
            builder.AppendLine("        <title>خوانش‌های گنجور</title>");
            builder.AppendLine("        <link>http://ava.ganjoor.net</link>");
            builder.AppendLine("    </image>");
            builder.AppendLine("    <itunes:category text=\"Arts\">");
            builder.AppendLine("        <itunes:category text=\"Books\"/>");
            builder.AppendLine("    </itunes:category>");
            builder.AppendLine("    <itunes:explicit>clean</itunes:explicit>");
            builder.AppendLine("    <itunes:owner><itunes:name>گنجور</itunes:name><itunes:email>ganjoor+avarss@ganjoor.net</itunes:email></itunes:owner>");
            builder.AppendLine("    <itunes:image href=\"https://ganjoor.net/image/rss.png\" />");

            foreach (PublicRecitationViewModel recitation in recitations)
            {
                string poemDescription = recitation.PoemFullTitle;
                if (recitation.PoemFullTitle.IndexOf("»") != -1)
                {
                    string poemCat = recitation.PoemFullTitle.Substring(0, recitation.PoemFullTitle.LastIndexOf("»")).Trim();
                    string poemTitle = recitation.PoemFullTitle.Substring(recitation.PoemFullTitle.LastIndexOf("»") + 1).Trim();

                    poemDescription = $"{poemTitle} از ({poemCat})";
                }

                string artist = recitation.AudioArtist;
                if(!string.IsNullOrEmpty(recitation.AudioArtistUrl))
                {
                    artist = $"<a href=\"{recitation.AudioArtistUrl}\">{recitation.AudioArtist}</a>";
                }

                builder.AppendLine("    <item>");
                builder.AppendLine($"       <title>{recitation.PoemFullTitle} با خوانش {recitation.AudioArtist}</title>");
                builder.AppendLine($"       <link>http://ava.ganjoor.net/#/{recitation.Id}</link>");
                builder.AppendLine($"       <pubDate>{recitation.PublishDate:r}</pubDate>");
                builder.AppendLine($"       <guid isPermaLink=\"false\">{recitation.LegacyAudioGuid}</guid>");
                builder.AppendLine($"       <dc:creator>{recitation.AudioArtist}</dc:creator>");
                builder.AppendLine($"       <description><![CDATA[{poemDescription} را با خوانش {recitation.AudioArtist} بشنوید.]]></description>");
                string htmlText = recitation.HtmlText == null ? "" : recitation.HtmlText
                                                    .Replace("</div>", $"{Environment.NewLine}")
                                                    .Replace("<div class=\"b\">", "")
                                                    .Replace("<div class=\"b2\">", "")
                                                    .Replace("<div class=\"m1\">", "")
                                                    .Replace("<div class=\"m2\">", "")
                                                    .Replace("<div class=\"n\">", "");
                builder.AppendLine($"       <content:encoded><![CDATA[<p><a href=\"https://ganjoor.net{recitation.PoemFullUrl}\">{poemDescription}</a> را با خوانش {artist} بشنوید.</p>{Environment.NewLine}" +
                                   $"           <p>فایل صوتی متناظر را می‌توانید در قالب mp3 از <a href=\"{recitation.Mp3Url}\">این نشانی</a> (اندازه {(recitation.Mp3SizeInBytes / (1024 * 1024.0f)) : 0.00} مگابایت) دریافت کنید.</p>{Environment.NewLine}" +
                                   $"           <p>متن خوانش:</p>{Environment.NewLine}" +
                                   $"           {htmlText}]]></content:encoded>");
                builder.AppendLine($"       <enclosure url=\"{recitation.Mp3Url}\" length=\"{recitation.Mp3SizeInBytes}\" type=\"audio/mpeg\" />");
                builder.AppendLine("    </item>");
            }

            builder.AppendLine("</channel>");
            builder.AppendLine("</rss>");

            return builder.ToString();

                
        }
    }

    
}
