using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        private void WriteSitemap(string filePath, List<string> urls)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xsiNs = "http://www.w3.org/2001/XMLSchema-instance";

            XDocument xDoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "no"),
                new XElement(ns + "urlset",
                new XAttribute(XNamespace.Xmlns + "xsi", xsiNs),
                new XAttribute(xsiNs + "schemaLocation",
                    "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),
                from url in urls
                select new XElement(ns + "url",
                    new XElement(ns + "loc", $"https://ganjoor.net{url}"))
                )
            );

            xDoc.Save(filePath);
        }

        /// <summary>
        /// build sitemap
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartBuildingSitemap()
        {
            try
            {
                
                _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob("BuildSitemap", "Query data")).Result;
                                   try
                                   {
                                       string xmlSitemap = Configuration.GetSection("Ganjoor")["SitemapLocation"];
                                       if (File.Exists(xmlSitemap))
                                           File.Delete(xmlSitemap);

                                       string dir = Path.GetDirectoryName(xmlSitemap);

                                       await jobProgressServiceEF.UpdateJob(job.Id, 0, "", false);

                                       List<string> sitemaps = new List<string>();

                                       string firstSitemap = Path.Combine(dir, $"1.xml");

                                       sitemaps.Add(firstSitemap);

                                       var urls = await context.GanjoorPages.Where(p => p.PoetId == null).OrderBy(p => p.Id).Select(p => p.FullUrl).ToListAsync();
                                       urls.Add("/map");
                                       urls.Add("/photos");
                                       urls.Add("/faq");

                                       urls.Remove("/audioclip");
                                       urls.Remove("/simi");
                                       urls.Remove("/tags");
                                       urls.Remove("/amar");
                                       urls.Remove("/tools");
                                       urls.Remove("/embed");
                                       urls.Remove("/hashieha");

                                       WriteSitemap(firstSitemap,
                                              urls
                                              );

                                       foreach (var poet in await context.GanjoorPoets.Where(p => p.Published).ToListAsync())
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, poet.Id, "", false);

                                           string poetSitemap = Path.Combine(dir, $"{poet.Id}.xml");

                                           WriteSitemap(poetSitemap,
                                               await context.GanjoorPages.Where(p => p.PoetId == poet.Id).OrderBy(p => p.Id).Select(p => p.FullUrl).ToListAsync()
                                               );

                                           sitemaps.Add(poetSitemap);
                                       }

                                       XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                                       XNamespace xsiNs = "http://www.w3.org/2001/XMLSchema-instance";
                                       XDocument xDoc = new XDocument(
                                            new XDeclaration("1.0", "UTF-8", "no"),
                                            new XElement(ns + "sitemapindex",
                                            new XAttribute(XNamespace.Xmlns + "xsi", xsiNs),
                                            new XAttribute(xsiNs + "schemaLocation",
                                                "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),
                                            from sitemap in sitemaps
                                            select new XElement(ns + "sitemap",
                                                new XElement(ns + "loc", $"https://ganjoor.net/{Path.GetFileName(sitemap)}"))
                                            )
                                        );

                                       xDoc.Save(xmlSitemap);

                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                   }
                                   catch(Exception exp)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                   }
                                   
                               }
                           });
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}