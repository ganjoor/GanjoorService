using Dapper;
using DNTPersianUtils.Core;
using FluentFTP;
using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
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
                                                        DownloadUrl = $"https://i.ganjoor.net/android/gdb/{Path.GetFileName(zipFile)}",
                                                        BlogUrl = "",
                                                        FileExt = ".zip",
                                                        ImageUrl = (hasImage ? $"https://i.ganjoor.net/android/img/{poet.Id}.png" : ""),
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


                                        if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                                        {
                                            var ftpClient = new AsyncFtpClient
                                            (
                                                Configuration.GetSection("ExternalFTPServer")["Host"],
                                                Configuration.GetSection("ExternalFTPServer")["Username"],
                                                Configuration.GetSection("ExternalFTPServer")["Password"]
                                            );
                                            ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                                            await ftpClient.AutoConnect();
                                            ftpClient.Config.RetryAttempts = 3;

                                            string dir = Path.GetDirectoryName(xmlFile);

                                            string rootDirName = Path.GetFileName(dir);
                                            string gdbDirName = Path.GetFileName(Path.GetDirectoryName(outDir));

                                            foreach (var localFilePath in Directory.GetFiles(Path.Combine(dir, gdbDirName), "*.zip"))
                                            {
                                                var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/{rootDirName}/{gdbDirName}/{Path.GetFileName(localFilePath)}";
                                                await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);
                                            }

                                            string imgDirName = Path.GetFileName(Path.GetDirectoryName(imgDir));
                                            foreach (var localFilePath in Directory.GetFiles(Path.Combine(dir, imgDirName), "*.png"))
                                            {

                                                var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/{rootDirName}/{imgDirName}/{Path.GetFileName(localFilePath)}";
                                                await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);
                                            }

                                            await ftpClient.UploadFile(xmlFile, $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/{rootDirName}/{Path.GetFileName(xmlFile)}", createRemoteDir: true);

                                            await ftpClient.Disconnect();
                                        }

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

    }
}
