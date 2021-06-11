using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
      
        /// <summary>
        /// import from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public RServiceResult<bool> ImportFromSqlite(int poetId, string filePath)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                            (
                            async token =>
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                                {
                                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                    var job = (await jobProgressServiceEF.NewJob("ImportFromSqlite", "Query data")).Result;

                                    try
                                    {
                                        SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();
                                        connectionStringBuilder.DataSource = filePath;
                                        using (SqliteConnection sqliteConnection = new SqliteConnection(connectionStringBuilder.ToString()))
                                        {
                                            await sqliteConnection.OpenAsync();
                                            IDbConnection sqlite = sqliteConnection;
                                            var poets = (await sqlite.QueryAsync("SELECT * FROM poet")).ToList();
                                            if (poets.Count != 1)
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "poets count in sqlite db is not equal to 1");
                                            }


                                            var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync();
                                            var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poetId && c.ParentId == null).SingleAsync();

                                            await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Importing");

                                            await _ImportSQLiteCatChildren(context, sqlite, poetId, 0, cat, poet.Nickname);

                                            await context.SaveChangesAsync();

                                            await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                        }
                                    }
                                    catch (Exception exp)
                                    {
                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                    }
                                }
                            }
                            );
            return new RServiceResult<bool>(true);
        }
        private async Task _ImportSQLiteCatChildren(RMuseumDbContext context, IDbConnection sqlite, int poetId, int catId, GanjoorCat parentCat, string parentFullTitle)
        {
            string catHtmlText = "";
            foreach (var cat in await sqlite.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} AND parent_id = {catId} ORDER BY id"))
            {
                var poetCatId = 1 + await context.GanjoorCategories.MaxAsync(c => c.Id);

                string url = GPersianTextSync.Farglisize(cat.text);

                GanjoorCat dbCat = new GanjoorCat()
                {
                    Id = poetCatId,
                    PoetId = poetId,
                    Title = cat.text,
                    UrlSlug = url,
                    FullUrl = $"{parentCat.FullUrl}/{url}"
                };
                context.GanjoorCategories.Add(dbCat);

                var catPageId = 1 + await context.GanjoorPages.MaxAsync(p => p.Id);

                GanjoorPage dbPageCat = new GanjoorPage()
                {
                    Id = catPageId,
                    GanjoorPageType = GanjoorPageType.CatPage,
                    Published = false,
                    PageOrder = -1,
                    Title = dbCat.Title,
                    FullTitle = $"{parentFullTitle} » {dbCat.Title}",
                    UrlSlug = dbCat.UrlSlug,
                    FullUrl = dbCat.FullUrl,
                    HtmlText = "",
                    PoetId = poetId,
                    CatId = poetCatId,
                    PostDate = DateTime.Now
                };

                context.GanjoorPages.Add(dbPageCat);

                catHtmlText += $"<p><a href=\"{dbCat.FullUrl}\">{dbCat.Title}</a></p>{Environment.NewLine}";

                await _ImportSQLiteCatChildren(context, sqlite, poetId, (int)cat.id, dbCat, $"{parentFullTitle} » {dbCat.Title}");
            }

            var poemId = 1 + await context.GanjoorPoems.MaxAsync(p => p.Id);
            while (await context.GanjoorPages.Where(p => p.Id == poemId).AnyAsync())
                poemId++;

            int poemNumber = 0;
            foreach (var poem in await sqlite.QueryAsync($"SELECT * FROM poem WHERE cat_id = {catId} ORDER BY id"))
            {
                poemNumber++;
                GanjoorPoem dbPoem = new GanjoorPoem()
                {
                    Id = poemId,
                    CatId = parentCat.Id,
                    Title = poem.title,
                    UrlSlug = $"sh{poemNumber}",
                    FullTitle = $"{parentFullTitle} » {poem.title}",
                    FullUrl = $"{parentCat.FullUrl}/sh{poemNumber}",
                };

                List<GanjoorVerse> poemVerses = new List<GanjoorVerse>();
                foreach (var verse in await sqlite.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poem.id} ORDER BY vorder"))
                {
                    GanjoorVerse dbVerse = new GanjoorVerse()
                    {
                        PoemId = poemId,
                        VOrder = verse.vorder,
                        VersePosition = verse.position,
                        Text = verse.text.Replace("ـ", "").Replace("  ", " ").ApplyCorrectYeKe().Trim()
                    };
                    poemVerses.Add(dbVerse);
                }

                dbPoem.PlainText = PreparePlainText(poemVerses);
                dbPoem.HtmlText = PrepareHtmlText(poemVerses);

                context.GanjoorPoems.Add(dbPoem);

                foreach (var dbVerse in poemVerses)
                {
                    context.GanjoorVerses.Add(dbVerse);
                    await context.SaveChangesAsync();//id set should be in order
                }

                GanjoorPage dbPoemPage = new GanjoorPage()
                {
                    Id = poemId,
                    GanjoorPageType = GanjoorPageType.PoemPage,
                    Published = false,
                    PageOrder = -1,
                    Title = dbPoem.Title,
                    FullTitle = dbPoem.FullTitle,
                    UrlSlug = dbPoem.UrlSlug,
                    FullUrl = dbPoem.FullUrl,
                    HtmlText = dbPoem.HtmlText,
                    PoetId = poemId,
                    CatId = parentCat.Id,
                    PostDate = DateTime.Now
                };

                _context.GanjoorPages.Add(dbPoemPage);

                catHtmlText += $"<p><a href=\"{dbPoemPage.FullUrl}\">{dbPoemPage.Title}</a></p>{Environment.NewLine}";


                poemId++;
            }

            if (!string.IsNullOrEmpty(catHtmlText))
            {
                var parentCatPage = await context.GanjoorPages.Where(p => p.FullUrl == parentCat.FullUrl && p.GanjoorPageType == GanjoorPageType.CatPage && p.PoetId == poetId).SingleAsync();
                parentCatPage.HtmlText += catHtmlText;
                context.GanjoorPages.Update(parentCatPage);
            }

            await context.SaveChangesAsync();

        }



        /// <summary>
        /// make html text
        /// </summary>
        /// <param name="verses"></param>
        /// <returns></returns>
        private static string PrepareHtmlText(List<GanjoorVerse> verses)
        {
            string htmlText = "";
            for (int vIndex = 0; vIndex < verses.Count; vIndex++)
            {
                GanjoorVerse v = verses[vIndex];
                if (v.VersePosition == VersePosition.CenteredVerse1)
                {
                    if (((vIndex + 1) < verses.Count) && (verses[vIndex + 1].VersePosition == VersePosition.CenteredVerse2))
                    {
                        htmlText += $"<div class=\"b2\"><p>{v.Text}</p>{Environment.NewLine}";
                    }
                    else
                    {
                        htmlText += $"<div class=\"b2\"><p>{v.Text}</p></div>{Environment.NewLine}";

                    }
                }
                else
                if (v.VersePosition == VersePosition.CenteredVerse2)
                {
                    htmlText += $"<p>{v.Text}</p></div>{Environment.NewLine}";
                }
                else

                if (v.VersePosition == VersePosition.Right)
                {
                    htmlText += $"<div class=\"b\"><div class=\"m1\"><p>{v.Text}</p></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Left)
                {
                    htmlText += $"<div class=\"m2\"><p>{v.Text}</p></div></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Paragraph || v.VersePosition == VersePosition.Single)
                {

                    string[] lines = v.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    if (lines.Length != 0)
                    {
                        if (v.Text.Length / lines.Length < 150)
                        {
                            htmlText += $"<div class=\"n\"><p>{v.Text.Replace("\r\n", " ")}</p></div>{Environment.NewLine}";
                        }
                        else
                        {
                            foreach (string line in lines)
                                htmlText += $"<div class=\"n\"><p>{line}</p></div>{Environment.NewLine}";
                        }
                    }
                }
            }
            return htmlText.Trim();
        }
    }
}
