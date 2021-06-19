using Dapper;
using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
        /// export to sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> ExportToSqlite(int poetId)
        {
            try
            {
                var poet = await _context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync();
                var catPoet = await _context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poetId && c.ParentId == null).SingleAsync();

                string dir = Path.Combine($"{_configuration.GetSection("PictureFileService")["StoragePath"]}", "SQLiteExports");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, $"{catPoet.UrlSlug}.gdb");

                if (File.Exists(filePath))
                    File.Delete(filePath);

                SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();
                connectionStringBuilder.DataSource = filePath;
                connectionStringBuilder.Mode = SqliteOpenMode.ReadWriteCreate;

                using (SqliteConnection sqliteConnection = new SqliteConnection(connectionStringBuilder.ToString()))
                {
                    await sqliteConnection.OpenAsync();

                    string q = "BEGIN TRANSACTION;" +
                                "CREATE TABLE [cat] ([id] INTEGER  PRIMARY KEY NOT NULL,[poet_id] INTEGER  NULL,[text] NVARCHAR(100)  NULL,[parent_id] INTEGER  NULL,[url] NVARCHAR(255)  NULL);"
                                +
                                "CREATE TABLE [poem] (id INTEGER PRIMARY KEY, cat_id INTEGER, title NVARCHAR(255), url NVARCHAR(255));"
                                +
                                "CREATE TABLE [poet] ([id] INTEGER  PRIMARY KEY NOT NULL,[name] NVARCHAR(20)  NULL,[cat_id] INTEGER  NULL  NULL, [description] TEXT);"
                                +
                                "CREATE TABLE [verse] ([poem_id] INTEGER  NULL,[vorder] INTEGER  NULL,[position] INTEGER  NULL,[text] TEXT  NULL);"
                                +
                                "COMMIT;";
                    await sqliteConnection.ExecuteAsync(q);
                    await sqliteConnection.ExecuteAsync("BEGIN;");
                    await sqliteConnection.ExecuteAsync($"INSERT INTO poet (id, name, cat_id, description) VALUES ({poet.Id}, '{poet.Nickname}', {catPoet.Id}, '{poet.Description}');");
                    await ExportCarToSqlite(sqliteConnection, catPoet);
                    await sqliteConnection.ExecuteAsync("COMMIT;");

                }


                return new RServiceResult<string>(filePath);
            }
            catch(Exception exp)
            {
                return new RServiceResult<string>(null, exp.ToString());
            }
        }

        private async Task ExportCarToSqlite(SqliteConnection sqliteConnection, GanjoorCat cat)
        {
            int parentId = cat.ParentId == null ? 0 : (int)cat.ParentId;
            await sqliteConnection.ExecuteAsync($"INSERT INTO cat (id, poet_id, text, parent_id, url) VALUES ({cat.Id}, {cat.PoetId}, '{cat.Title}', {parentId}, 'https://ganjoor.net{cat.FullUrl}');");
            
            var poems = await _context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == cat.Id).ToListAsync();
            foreach(var poem in poems)
            {
                await sqliteConnection.ExecuteAsync($"INSERT INTO poem (id, cat_id, title, url) VALUES ({poem.Id}, {poem.CatId}, '{poem.Title}', 'https://ganjoor.net{poem.FullUrl}');");
                foreach (var verse in await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).ToListAsync())
                    await sqliteConnection.ExecuteAsync($"INSERT INTO verse (poem_id, vorder, position, text) VALUES ({poem.Id}, {verse.VOrder}, {(int)verse.VersePosition}, '{verse.Text}');");
            }

            foreach (var child in await _context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).ToListAsync())
                await ExportCarToSqlite(sqliteConnection, child);
                    
        }

        /// <summary>
        /// import from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ImportFromSqlite(int poetId, IFormFile file)
        {
            try
            {
                string dir = Path.Combine($"{_configuration.GetSection("PictureFileService")["StoragePath"]}", "SQLiteImports");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, file.FileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
                using (FileStream fsMain = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fsMain);
                }

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
                                            var catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl == cat.FullUrl).SingleAsync();

                                            await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Importing");

                                            await _ImportSQLiteCatChildren(context, sqlite, poetId, await sqlite.QuerySingleAsync<int>($"SELECT id FROM cat WHERE parent_id = 0") , cat, poet.Nickname, jobProgressServiceEF, job, catPage.Id);

                                            await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                        }
                                    }
                                    catch (Exception exp)
                                    {
                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                    }
                                }

                                File.Delete(filePath);
                            }
                            );
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            

            
            return new RServiceResult<bool>(true);
        }
        private async Task<string> _ImportSQLiteCatChildren(RMuseumDbContext context, IDbConnection sqlite, int poetId, int sqliteParentCatId, GanjoorCat parentCat, string parentFullTitle, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job, int parentPagId)
        {
            try
            {
                string catHtmlText = "";
                foreach (var cat in await sqlite.QueryAsync($"SELECT * FROM cat WHERE parent_id = {sqliteParentCatId} ORDER BY id"))
                {
                    await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Importing - {cat.text}");

                    var poetCatId = 1 + await context.GanjoorCategories.MaxAsync(c => c.Id);

                    string url = GPersianTextSync.Farglisize(cat.text);

                    GanjoorCat dbCat = new GanjoorCat()
                    {
                        Id = poetCatId,
                        PoetId = poetId,
                        Title = cat.text,
                        UrlSlug = url,
                        FullUrl = $"{parentCat.FullUrl}/{url}",
                        ParentId = parentCat.Id
                    };
                    context.GanjoorCategories.Add(dbCat);

                    var catPageId = 1 + await context.GanjoorPages.MaxAsync(p => p.Id);
                    while (await context.GanjoorPoems.Where(p => p.Id == catPageId).AnyAsync())
                        catPageId++;

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
                        PostDate = DateTime.Now,
                        ParentId = parentPagId
                    };

                    context.GanjoorPages.Add(dbPageCat);

                    await context.SaveChangesAsync();

                    catHtmlText += $"<p><a href=\"{dbCat.FullUrl}\">{dbCat.Title}</a></p>{Environment.NewLine}";

                    await _ImportSQLiteCatChildren(context, sqlite, poetId, (int)cat.id, dbCat, $"{parentFullTitle} » {dbCat.Title}", jobProgressServiceEF, job, dbPageCat.Id);
                }

                var poemId = 1 + await context.GanjoorPoems.MaxAsync(p => p.Id);
                while (await context.GanjoorPages.Where(p => p.Id == poemId).AnyAsync())
                    poemId++;

                int poemNumber = 0;
                foreach (var poem in await sqlite.QueryAsync($"SELECT * FROM poem WHERE cat_id = {sqliteParentCatId} ORDER BY id"))
                {
                    poemNumber++;
                    await jobProgressServiceEF.UpdateJob(job.Id, poemNumber, "", false);

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
                        int vOrder = int.Parse(verse.vorder.ToString());
                        int position = int.Parse(verse.position.ToString());
                        string text = verse.text;
                        GanjoorVerse dbVerse = new GanjoorVerse()
                        {
                            PoemId = poemId,
                            VOrder = vOrder,
                            VersePosition = (VersePosition)position,
                            Text = text.Replace("ـ", "").Replace("  ", " ").ApplyCorrectYeKe().Trim()
                        };
                        poemVerses.Add(dbVerse);
                    }

                    if(poemVerses.Count == 0)
                    {
                        poemNumber--;
                        continue;
                    }

                    dbPoem.PlainText = PreparePlainText(poemVerses);
                    dbPoem.HtmlText = PrepareHtmlText(poemVerses);

                    context.GanjoorPoems.Add(dbPoem);
                    await context.SaveChangesAsync();

                    foreach (var dbVerse in poemVerses)
                    {
                        context.GanjoorVerses.Add(dbVerse);
                        await context.SaveChangesAsync();//id set should be in order
                    }


                    var poemRhymeLettersRes = LanguageUtils.FindRhyme(poemVerses);
                    if(!string.IsNullOrEmpty(poemRhymeLettersRes.Rhyme))
                    {
                        dbPoem.RhymeLetters = poemRhymeLettersRes.Rhyme;
                        context.GanjoorPoems.Update(dbPoem);
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
                        PoetId = poetId,
                        CatId = parentCat.Id,
                        PoemId = poemId,
                        PostDate = DateTime.Now,
                        ParentId = parentPagId
                    };

                    context.GanjoorPages.Add(dbPoemPage);
                    await context.SaveChangesAsync();

                    catHtmlText += $"<p><a href=\"{dbPoemPage.FullUrl}\">{dbPoemPage.Title}</a></p>{Environment.NewLine}";

                    poemId++;
                }

                if (!string.IsNullOrEmpty(catHtmlText))
                {
                    var parentCatPage = await context.GanjoorPages.Where(p => p.FullUrl == parentCat.FullUrl).SingleAsync();
                    parentCatPage.HtmlText += catHtmlText;
                    context.GanjoorPages.Update(parentCatPage);
                }

                await context.SaveChangesAsync();
            }
            catch(Exception exp)
            {
                return exp.ToString();
            }
            return "";
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
