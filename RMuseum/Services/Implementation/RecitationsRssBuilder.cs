using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using RMuseum.Models.GanjoorAudio.ViewModels;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
        public static async Task<string> Build(PublicRecitationViewModel[] recitations)
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true, Indent = true }))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.WriteTitle("خوانش‌های گنجور");
                await writer.WriteDescription("روایتهای صوتی اشعار سایت گنجور");
                await writer.Write(new SyndicationLink(new Uri("http://ava.ganjoor.net")));
                await writer.WriteLastBuildDate(recitations.Length > 0 ? recitations[0].PublishDate.ToUniversalTime() : DateTimeOffset.UtcNow);
                await writer.WriteLanguage(new System.Globalization.CultureInfo("fa-IR"));
                await writer.WriteGenerator("https://ganjgah.ir");

                foreach(PublicRecitationViewModel recitation in recitations)
                {
                    var item = new SyndicationItem()
                    {
                        Id = recitation.LegacyAudioGuid.ToString(),
                        Title = $"{recitation.PoemFullTitle} با خوانش {recitation.AudioArtist}",
                        Description = recitation.HtmlText == null ? "" : recitation.HtmlText
                                                .Replace("</div>", "")
                                                .Replace("<div class=\"b\">", "")
                                                .Replace("<div class=\"b2\">", "")
                                                .Replace("<div class=\"m1\">", "")
                                                .Replace("<div class=\"m2\">", "")
                                                .Replace("<div class=\"n\">", ""),
                        Published = recitation.PublishDate.ToUniversalTime(),
                    };

                    item.AddLink(new SyndicationLink(new Uri($"https://ava.ganjoor.net/#/{recitation.Id}")));
                    item.AddCategory(new SyndicationCategory("شعرخوانی"));
                    item.AddCategory(new SyndicationCategory("ادبیات"));

                    await writer.Write(item);
                }

                xmlWriter.Flush();
            }


            return sw.ToString();
        }
    }

    class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            this._encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }
    }
}
