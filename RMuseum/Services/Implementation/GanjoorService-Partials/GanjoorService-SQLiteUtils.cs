using Dapper;
using DNTPersianUtils.Core;
using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.Ganjoor;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
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
        /// start generating gdb files
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartBatchGenerateGDBFiles()
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
                                    var job = (await jobProgressServiceEF.NewJob("BatchGenerateGDBFiles", "Query Data")).Result;

                                    try
                                    {
                                        string outDir = Configuration.GetSection("Ganjoor")["GDBStorage"];
                                        string imgDir = Configuration.GetSection("Ganjoor")["GDBStorageImageSource"];
                                        string xmlFile = Configuration.GetSection("Ganjoor")["GDBListXMLFile"];
                                        string preExisitingListXMLFile = Configuration.GetSection("Ganjoor")["GDBPreExisitingListXMLFile"];
                                        List<GDBInfo> programList = new List<GDBInfo>();
                                        if (!string.IsNullOrEmpty(preExisitingListXMLFile))
                                        {
                                            if (File.Exists(preExisitingListXMLFile))
                                            {
                                                programList = GDBListProcessor.RetrieveListFromFile(preExisitingListXMLFile, out string exception);
                                                if (programList == null)
                                                {
                                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "GDBListProcessor.RetrieveListFromFile", false, exception);
                                                    return;
                                                }
                                            }
                                        }

                                        var poets = await context.GanjoorPoets.AsNoTracking().ToListAsync();

                                        List<GDBInfo> lstFiles = new List<GDBInfo>();

                                        foreach (var poet in poets)
                                        {
                                            if (!await context.GanjoorPoems
                                                                        .Include(p => p.Cat)
                                                                        .Where(p => p.Cat.PoetId == poet.Id).AnyAsync())
                                                continue;

                                            await jobProgressServiceEF.UpdateJob(job.Id, poet.Id);

                                            var gdbGeneration = await _ExportToSqlite(context, poet.Id, outDir, null, false);
                                            if(!string.IsNullOrEmpty(gdbGeneration.ExceptionString))
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, gdbGeneration.ExceptionString);
                                                return;
                                            }
                                            string gdbFile = gdbGeneration.Result;
                                            string pngFile = Path.Combine(imgDir, $"{poet.Id}.png");
                                            bool hasImage = File.Exists(pngFile);

                                            using (var archiveStream = new MemoryStream())
                                            {
                                                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                                                {
                                                    var zipGDBFileEntry = archive.CreateEntry(Path.GetFileName(gdbFile), CompressionLevel.Optimal);
                                                    using (var zipStream = zipGDBFileEntry.Open())
                                                    {
                                                        var gdbBytes = File.ReadAllBytes(gdbFile);
                                                        zipStream.Write(gdbBytes, 0, gdbBytes.Length);
                                                    }

                                                    
                                                    if (hasImage)
                                                    {
                                                        var zipImgFileEntry = archive.CreateEntry(Path.GetFileName(pngFile), CompressionLevel.Optimal);
                                                        using (var zipStream = zipImgFileEntry.Open())
                                                        {
                                                            var pngBytes = File.ReadAllBytes(pngFile);
                                                            zipStream.Write(pngBytes, 0, pngBytes.Length);
                                                        }
                                                    }
                                                }

                                                string zipFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(gdbFile) + ".zip");
                                                if (File.Exists(zipFile))
                                                    File.Delete(zipFile);

                                                byte[] zipArray = archiveStream.ToArray();

                                                File.WriteAllBytes(zipFile, zipArray);

                                                var catPoet = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();

                                                var lowestPoemID = await context.GanjoorPoems
                                                                        .Include(p => p.Cat)
                                                                        .Where(p => p.Cat.PoetId == poet.Id)
                                                                        .MinAsync(p => p.Id);

                                                lstFiles.Add
                                                (
                                                    new GDBInfo()
                                                    {
                                                        CatName = poet.Nickname,
                                                        CatID = catPoet.Id,
                                                        PoetID = poet.Id,
                                                        DownloadUrl = $"http://i.ganjoor.net/android/gdb/{Path.GetFileName(zipFile)}",
                                                        BlogUrl = "",
                                                        FileExt = ".zip",
                                                        ImageUrl = (hasImage ? $"http://i.ganjoor.net/android/img/{poet.Id}.png" : ""),
                                                        FileSizeInByte = zipArray.Length,
                                                        LowestPoemID = lowestPoemID,
                                                        PubDate = DateTime.Now
                                                    }
                                                );

                                            }
                                            File.Delete(gdbFile);

                                            
                                        }

                                        if (File.Exists(xmlFile))
                                            File.Delete(xmlFile);

                                        if(programList.Count > 0)
                                        {
                                            lstFiles.AddRange(programList);
                                        }

                                        lstFiles.Sort((a, b) => a.CatName.CompareTo(b.CatName));

                                        GDBListProcessor.Save(xmlFile, "مجموعه‌های قابل دریافت از گنجور", "", "", lstFiles);

                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// export to sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> ExportToSqlite(int poetId)
        {
            return await _ExportToSqlite(_context, poetId, Path.Combine($"{Configuration.GetSection("PictureFileService")["StoragePath"]}", "SQLiteExports"));
        }
        
        private async Task<RServiceResult<string>> _ExportToSqlite(RMuseumDbContext context, int poetId, string dir, string fileName = null, bool ignoreBio = false)
        {
            try
            {
                var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync();
                var catPoet = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poetId && c.ParentId == null).SingleAsync();

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if(fileName == null)
                {
                    fileName = catPoet.UrlSlug;
                }

                string filePath = Path.Combine(dir, $"{fileName}.gdb");

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
                    string bio = poet.Description;
                    bio = bio.Replace("\"", "").Replace("'", "");
                    await sqliteConnection.ExecuteAsync($"INSERT INTO poet (id, name, cat_id, description) VALUES ({poet.Id}, '{poet.Nickname}', {catPoet.Id}, '{(ignoreBio ? "" : bio)}');");
                    await ExportCatToSqlite(context, sqliteConnection, catPoet);
                    await sqliteConnection.ExecuteAsync("COMMIT;");
                }
                SqliteConnection.ClearAllPools();

                return new RServiceResult<string>(filePath);
            }
            catch(Exception exp)
            {
                return new RServiceResult<string>(null, exp.ToString());
            }
        }

        private async Task ExportCatToSqlite(RMuseumDbContext context, SqliteConnection sqliteConnection, GanjoorCat cat)
        {
            int parentId = cat.ParentId == null ? 0 : (int)cat.ParentId;
            await sqliteConnection.ExecuteAsync($"INSERT INTO cat (id, poet_id, text, parent_id, url) VALUES ({cat.Id}, {cat.PoetId}, '{cat.Title}', {parentId}, 'https://ganjoor.net{cat.FullUrl}');");
            
            var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == cat.Id).ToListAsync();
            foreach(var poem in poems)
            {
                await sqliteConnection.ExecuteAsync($"INSERT INTO poem (id, cat_id, title, url) VALUES ({poem.Id}, {poem.CatId}, '{poem.Title}', 'https://ganjoor.net{poem.FullUrl}');");
                foreach (var verse in await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).ToListAsync())
                    await sqliteConnection.ExecuteAsync($"INSERT INTO verse (poem_id, vorder, position, text) VALUES ({poem.Id}, {verse.VOrder}, {(int)verse.VersePosition}, '{verse.Text}');");
            }

            foreach (var child in await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).ToListAsync())
                await ExportCatToSqlite(context, sqliteConnection, child);
                    
        }

        /// <summary>
        /// Apply corrections from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="file"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ApplyCorrectionsFromSqlite(int poetId, IFormFile file, string note)
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

                string email = $"{Configuration.GetSection("Ganjoor")["SystemEmail"]}";
                var userId = (await _appUserService.FindUserByEmail(email)).Result.Id;

                _backgroundTaskQueue.QueueBackgroundWorkItem
                            (
                            async token =>
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                                {
                                    LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                    var job = (await jobProgressServiceEF.NewJob("ApplyCorrectionsFromSqlite", "Query data")).Result;

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

                                            int poemNumber = 0;
                                            foreach (var poem in await sqlite.QueryAsync($"SELECT * FROM poem ORDER BY id"))
                                            {
                                                poemNumber++;
                                                await jobProgressServiceEF.UpdateJob(job.Id, poemNumber, "", false);

                                                int poemId = (int)poem.id;

                                                GanjoorPoem dbPoem = await context.GanjoorPoems.Include(p => p.Cat).Where(p => p.Id == poemId).SingleOrDefaultAsync();

                                                if (dbPoem == null)
                                                    continue;

                                                if (dbPoem.Cat.PoetId != poetId)
                                                    continue;

                                                string comment = $"<p>تغییرات حاصل از پردازش {note}</p>{Environment.NewLine}";
                                                bool anyChanges = false;

                                                var dbPage = await context.GanjoorPages.Where(p => p.Id == poemId).SingleOrDefaultAsync();

                                                GanjoorPageSnapshot snapshot = new GanjoorPageSnapshot()
                                                {
                                                    GanjoorPageId = poemId,
                                                    MadeObsoleteByUserId = (Guid)userId,
                                                    RecordDate = DateTime.Now,
                                                    Note = note,
                                                    Title = dbPage.Title,
                                                    UrlSlug = dbPage.UrlSlug,
                                                    HtmlText = dbPage.HtmlText,
                                                };

                                                string poemTitle = poem.title;
                                                if(poemTitle != dbPoem.Title)
                                                {
                                                    anyChanges = true;
                                                    comment += $"<p>تغییر عنوان از «{dbPoem.Title}» به «{poemTitle}»</p>{Environment.NewLine}";
                                                    dbPoem.Title = poemTitle;
                                                    dbPoem.FullTitle = $"{dbPoem.Cat.FullUrl} » {dbPoem.Title}";
                                                    context.GanjoorPoems.Update(dbPoem);
                                                }


                                                var sqliteVerses = new List<dynamic>(await sqlite.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poem.id} ORDER BY vorder"));
                                                var dbVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId).OrderBy(v => v.VOrder).ToListAsync();

                                                int vIndex = 0;
                                                while(vIndex < sqliteVerses.Count && vIndex < dbVerses.Count)
                                                {
                                                    if (sqliteVerses[vIndex].vorder != dbVerses[vIndex].VOrder)
                                                    {
                                                        vIndex = -1;
                                                        break;
                                                    }

                                                    string text = sqliteVerses[vIndex].text;
                                                    text = text.Replace("ـ", "").Replace("  ", " ").ApplyCorrectYeKe().Trim();

                                                    if (text == dbVerses[vIndex].Text)
                                                    {
                                                        vIndex++;
                                                        continue;
                                                    }

                                                    comment += $"<p>تغییر مصرع {vIndex + 1} از «{dbVerses[vIndex].Text}» به «{text}»</p>{Environment.NewLine}".ToPersianNumbers();

                                                    dbVerses[vIndex].Text = text;

                                                    context.GanjoorVerses.Update(dbVerses[vIndex]);

                                                    anyChanges = true;
                                                    vIndex++;
                                                }

                                                if(vIndex != -1)
                                                {
                                                    while (vIndex < dbVerses.Count)
                                                    {
                                                        comment += $"<p>حذف مصرع {vIndex + 1} با متن «{dbVerses[vIndex].Text}»</p>{Environment.NewLine}".ToPersianNumbers();
                                                        context.GanjoorVerses.Remove(dbVerses[vIndex]);
                                                        vIndex++;
                                                        anyChanges = true;
                                                    }

                                                    while (vIndex < sqliteVerses.Count)
                                                    {
                                                        string text = sqliteVerses[vIndex].text;
                                                        text = text.Replace("ـ", "").Replace("  ", " ").ApplyCorrectYeKe().Trim();
                                                        int vOrder = int.Parse(sqliteVerses[vIndex].vorder.ToString());
                                                        int position = int.Parse(sqliteVerses[vIndex].position.ToString());
                                                        comment += $"<p>اضافه شدن مصرع {vIndex + 1} با متن «{text}»</p>{Environment.NewLine}".ToPersianNumbers();
                                                        context.GanjoorVerses.Add
                                                        (
                                                            new GanjoorVerse()
                                                            {
                                                                PoemId = poemId,
                                                                VOrder = vOrder,
                                                                VersePosition = (VersePosition)position,
                                                                Text = text
                                                            }
                                                        );
                                                        vIndex++;
                                                        anyChanges = true;
                                                    }

                                                    

                                                    if (anyChanges)
                                                    {
                                                        await _FillPoemCoupletIndices(context, poemId);
                                                        GanjoorComment sysComment = new GanjoorComment()
                                                        {
                                                            UserId = userId,
                                                            AuthorIpAddress = "127.0.0.1",
                                                            CommentDate = DateTime.Now,
                                                            HtmlComment = comment,
                                                            PoemId = poemId,
                                                            Status = PublishStatus.Published,
                                                        };
                                                        context.GanjoorComments.Add(sysComment);

                                                        context.GanjoorPageSnapshots.Add(snapshot);
                                                        
                                                        await context.SaveChangesAsync();

                                                        var poemVerses = await context.GanjoorVerses.Where(v => v.PoemId == poemId).OrderBy(v => v.VOrder).ToListAsync();

                                                        bool needsNewVOrder = false;
                                                        for (int i = 0; i < poemVerses.Count; i++)
                                                        {
                                                            if(poemVerses[i].VOrder != (i + 1))
                                                            {
                                                                poemVerses[i].VOrder = i + 1;
                                                                needsNewVOrder = true;
                                                            }
                                                        }
                                                        if(needsNewVOrder)
                                                        {
                                                            context.GanjoorVerses.UpdateRange(poemVerses);
                                                        }

                                                        dbPoem.PlainText = PreparePlainText(poemVerses);
                                                        dbPoem.HtmlText = PrepareHtmlText(poemVerses);
                                                        dbPage.HtmlText = dbPoem.HtmlText;
                                                        dbPage.Title = dbPoem.Title;
                                                        dbPage.FullTitle = dbPoem.FullTitle;

                                                        try
                                                        {
                                                            var poemRhymeLettersRes = LanguageUtils.FindRhyme(poemVerses);
                                                            if (!string.IsNullOrEmpty(poemRhymeLettersRes.Rhyme))
                                                            {
                                                                dbPoem.RhymeLetters = poemRhymeLettersRes.Rhyme;
                                                            }
                                                        }
                                                        catch
                                                        {

                                                        }
                                                        

                                                        context.GanjoorPoems.Update(dbPoem);
                                                        context.GanjoorPages.Update(dbPage);

                                                        await context.SaveChangesAsync();

                                                        

                                                    }
                                                }

                                            }

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
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }



            return new RServiceResult<bool>(true);
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

                                            var resImport = await _ImportSQLiteCatChildren(context, sqlite, poetId, await sqlite.QuerySingleAsync<int>($"SELECT id FROM cat WHERE parent_id = 0") , cat, poet.Nickname, jobProgressServiceEF, job, catPage.Id);

                                            if(string.IsNullOrEmpty(resImport))
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

                    string catTitle = cat.text;

                    string url = GPersianTextSync.Farglisize(catTitle);
                    if(catTitle.IndexOf('|') != -1)
                    {
                        string[] catParts = catTitle.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        if(catParts.Length == 2)
                        {
                            catTitle = catParts[0].Trim();
                            url = catParts[1].Trim();
                        }
                    }
                    else
                    switch(catTitle)
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
                        ParentId = parentCat.Id
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

                    await _FillPoemCoupletIndices(context, poemId);

                    try
                    {
                        var poemRhymeLettersRes = LanguageUtils.FindRhyme(poemVerses);
                        if (!string.IsNullOrEmpty(poemRhymeLettersRes.Rhyme))
                        {
                            dbPoem.RhymeLetters = poemRhymeLettersRes.Rhyme;
                            context.GanjoorPoems.Update(dbPoem);
                        }
                    }
                    catch
                    {

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
                if(v.VersePosition == VersePosition.Comment)
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
                        if(string.IsNullOrEmpty(v.Text))
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
