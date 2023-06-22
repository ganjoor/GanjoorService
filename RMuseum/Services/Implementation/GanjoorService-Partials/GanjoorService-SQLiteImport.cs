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
        /// import from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ImportFromSqlite(int poetId, IFormFile file)
        {
            try
            {
                string dir = Path.Combine($"{Configuration.GetSection("PictureFileService")["StoragePath"]}", "SQLiteImports");
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

                                            var resImport = await _ImportSQLiteCatChildren(context, sqlite, poetId, await sqlite.QuerySingleAsync<int>($"SELECT id FROM cat WHERE parent_id = 0"), cat, poet.Nickname, jobProgressServiceEF, job, catPage.Id);

                                            if (string.IsNullOrEmpty(resImport))
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                            }
                                            else
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, resImport);
                                            }


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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// import a catgory from sqlite
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ImportCategoryFromSqlite(int catId, IFormFile file)
        {
            try
            {
                string dir = Path.Combine($"{Configuration.GetSection("PictureFileService")["StoragePath"]}", "SQLiteImports");
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

                                            var cat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                                            var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == cat.PoetId).SingleAsync();
                                            var catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.FullUrl == cat.FullUrl).SingleAsync();

                                            await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Importing");

                                            var resImport = await _ImportSQLiteCatChildren(context, sqlite, poet.Id, await sqlite.QuerySingleAsync<int>($"SELECT id FROM cat WHERE parent_id = 0"), cat, catPage.FullTitle, jobProgressServiceEF, job, catPage.Id);

                                            if (string.IsNullOrEmpty(resImport))
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                            }
                                            else
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, resImport);
                                            }


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
            catch (Exception exp)
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

                    string catTitle = cat.text;

                    string url = GPersianTextSync.Farglisize(catTitle);
                    if (catTitle.IndexOf('|') != -1)
                    {
                        string[] catParts = catTitle.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        if (catParts.Length == 2)
                        {
                            catTitle = catParts[0].Trim();
                            url = catParts[1].Trim();
                        }
                    }
                    else
                        switch (catTitle)
                        {
                            case "دیوان اشعار":
                                url = "divan";
                                break;
                            case "قصاید":
                            case "قصائد":
                            case "قصیده":
                            case "قصیده ها":
                                url = "ghaside";
                                break;
                            case "غزلیات":
                            case "غزل":
                            case "غزل ها":
                                url = "ghazal";
                                break;
                            case "قطعات":
                            case "مقطعات":
                            case "قطعه":
                                url = "ghete";
                                break;
                            case "مثنویات":
                            case "مثنوی":
                            case "مثنوی ها":
                                url = "masnavi";
                                break;
                            case "ترکیبات":
                            case "ترکیب بند":
                                url = "tarkib";
                                break;
                            case "ترجیعات":
                            case "ترجیع بند":
                                url = "tarjee";
                                break;
                            case "مسمطات":
                            case "مسمط":
                                url = "mosammat";
                                break;
                            case "مخمسات":
                            case "مخمس":
                                url = "mokhammas";
                                break;
                            case "رباعیات":
                            case "رباعی":
                            case "رباعی ها":
                                url = "robaee";
                                break;
                            case "ملمعات":
                            case "ملمع":
                                url = "molamma";
                                break;
                            case "هجویات":
                            case "هجو":
                                url = "hajv";
                                break;
                            case "هزلیات":
                            case "هزل":
                                url = "hazl";
                                break;
                            case "مراثی":
                            case "مرثیه":
                            case "رثا":
                            case "مرثیه ها":
                                url = "marsie";
                                break;
                            case "مفردات":
                                url = "mofradat";
                                break;
                            case "ملحقات":
                                url = "molhaghat";
                                break;
                            case "اشعار عربی":
                                url = "arabi";
                                break;
                            case "ماده تاریخ‌ها":
                            case "ماده تاریخها":
                            case "ماده تاریخ":
                                url = "tarikh";
                                break;
                            case "معمیات":
                                url = "moammiyat";
                                break;
                            case "چیستان":
                                url = "chistan";
                                break;
                            case "لغز":
                            case "لغزها":
                                url = "loghaz";
                                break;
                        }

                    GanjoorCat dbCat = new GanjoorCat()
                    {
                        Id = poetCatId,
                        PoetId = poetId,
                        Title = catTitle,
                        UrlSlug = url,
                        FullUrl = $"{parentCat.FullUrl}/{url}",
                        ParentId = parentCat.Id,
                        TableOfContentsStyle = GanjoorTOC.Analyse,
                        Published = true,
                    };
                    context.GanjoorCategories.Add(dbCat);

                    var maxPageId = await context.GanjoorPages.MaxAsync(p => p.Id);

                    if (await context.GanjoorPoems.MaxAsync(p => p.Id) > maxPageId)
                        maxPageId = await context.GanjoorPoems.MaxAsync(p => p.Id);

                    var catPageId = 1 + maxPageId;
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

                    var resChild = await _ImportSQLiteCatChildren(context, sqlite, poetId, (int)cat.id, dbCat, $"{parentFullTitle} » {dbCat.Title}", jobProgressServiceEF, job, dbPageCat.Id);
                    if (!string.IsNullOrEmpty(resChild))
                        return resChild;
                }
                var maxPoemId = await context.GanjoorPoems.MaxAsync(p => p.Id);
                if (await context.GanjoorPages.MaxAsync(p => p.Id) > maxPoemId)
                    maxPoemId = await context.GanjoorPages.MaxAsync(p => p.Id);
                var poemId = 1 + maxPoemId;

                int poemNumber = 0;
                if (await context.GanjoorPoems.AsNoTracking().Where(c => c.CatId == parentCat.Id).AnyAsync())
                {
                    poemNumber = await context.GanjoorPoems.AsNoTracking().Where(c => c.CatId == parentCat.Id).CountAsync();
                }
                foreach (var poem in await sqlite.QueryAsync($"SELECT * FROM poem WHERE cat_id = {sqliteParentCatId} ORDER BY id"))
                {
                    poemNumber++;
                    await jobProgressServiceEF.UpdateJob(job.Id, poemNumber, "", false);

                    string title = poem.title;
                    string urlSlug = $"sh{poemNumber}";
                    if (title.IndexOf('|') != -1)
                    {
                        string[] titleParts = title.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        if (titleParts.Length == 2)
                        {
                            title = titleParts[0].Trim();
                            urlSlug = titleParts[1].Trim();
                        }
                    }


                    GanjoorPoem dbPoem = new GanjoorPoem()
                    {
                        Id = poemId,
                        CatId = parentCat.Id,
                        Title = title,
                        UrlSlug = urlSlug,
                        FullTitle = $"{parentFullTitle} » {title}",
                        FullUrl = $"{parentCat.FullUrl}/{urlSlug}",
                        Published = true,
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

                    if (poemVerses.Count == 0)
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

                    await _FillPoemCoupletIndices(context, poemId);

                    
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

                    await _SectionizePoem(context, dbPoem, jobProgressServiceEF, job);

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
            catch (Exception exp)
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
            int coupletIndex = 0;
            for (int vIndex = 0; vIndex < verses.Count; vIndex++)
            {
                GanjoorVerse v = verses[vIndex];
                if (v.VersePosition == VersePosition.CenteredVerse1)
                {
                    coupletIndex++;
                    if (((vIndex + 1) < verses.Count) && (verses[vIndex + 1].VersePosition == VersePosition.CenteredVerse2))
                    {
                        htmlText += $"<div class=\"b2\" id=\"bn{coupletIndex}\"><p>{v.Text}</p>{Environment.NewLine}";
                    }
                    else
                    {
                        htmlText += $"<div class=\"b2\" id=\"bn{coupletIndex}\"><p>{v.Text}</p></div>{Environment.NewLine}";

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
                    coupletIndex++;
                    htmlText += $"<div class=\"b\" id=\"bn{coupletIndex}\"><div class=\"m1\"><p>{v.Text}</p></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Left)
                {
                    htmlText += $"<div class=\"m2\"><p>{v.Text}</p></div></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Comment)
                {
                    htmlText += $"<div class=\"c\"><p>{v.Text}</p></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Paragraph || v.VersePosition == VersePosition.Single)
                {
                    coupletIndex++;
                    string[] lines = v.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    string cssClass = v.VersePosition == VersePosition.Paragraph ? "n" : "l";

                    if (lines.Length != 0)
                    {
                        if (v.Text.Length / lines.Length < 150)
                        {
                            htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>{v.Text.Replace("\r\n", " ")}</p></div>{Environment.NewLine}";
                        }
                        else
                        {
                            foreach (string line in lines)
                                htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>{line}</p></div>{Environment.NewLine}";
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(v.Text))
                        {
                            htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>&nbsp;</p></div>{Environment.NewLine}";//empty line!
                        }
                        else
                        {
                            htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>{v.Text}</p></div>{Environment.NewLine}";//not brave enough to ignore it!
                        }

                    }
                }
            }
            return htmlText.Trim();
        }
    }
}
