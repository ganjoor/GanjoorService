using Microsoft.EntityFrameworkCore;
using System;
using RMuseum.Models.Ganjoor;
using System.Threading.Tasks;
using RSecurityBackend.Models.Generic;
using System.Linq;
using RSecurityBackend.Services.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using RMuseum.DbContext;
using System.Data;
using System.IO;
using Dapper;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        public async Task<RServiceResult<bool>> TajikImportFromSqlite(int poetId, string filePath)
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
                                    var job = (await jobProgressServiceEF.NewJob("ImportFromSqlite", "Query data")).Result;

                                    try
                                    {
                                        SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();
                                        connectionStringBuilder.DataSource = filePath;
                                        using (SqliteConnection sqliteConnection = new SqliteConnection(connectionStringBuilder.ToString()))
                                        {
                                            await sqliteConnection.OpenAsync();
                                            IDbConnection sqlite = sqliteConnection;
                                            var poets = (await sqlite.QueryAsync("SELECT * FROM poet ORDER BY id")).ToList();
                                            if (poetId != 0 && poets.Count != 1)
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "poets count in sqlite db is not equal to 1");
                                            }

                                            foreach (var poet in poets)
                                            {
                                                int poetId = (int)poet.id;
                                                await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId}");
                                                if (await context.GanjoorPoets.AnyAsync(p => p.Id == poetId) == false) continue;
                                                var cats = (await sqlite.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} ORDER BY id")).ToList();
                                                if(cats.Count == 0) continue;
                                                if (await context.TajikPoets.AnyAsync(p => p.Id == poetId) == false)
                                                {
                                                    var tajikPoet = new GanjoorTajikPoet()
                                                    {
                                                        Id = poetId,
                                                        TajikNickname = poet.name,
                                                        TajikDescription = poet.description,
                                                        BirthYearInLHijri = (await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync()).BirthYearInLHijri
                                                    };
                                                    context.Add(tajikPoet);
                                                    await context.SaveChangesAsync();
                                                }
                                                
                                                foreach (var cat in cats)
                                                {
                                                    int catId = (int)cat.id;
                                                    await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId} - cat: {catId}");
                                                    if (await context.GanjoorCategories.AnyAsync(c => c.Id == catId) == false) continue;
                                                    if(await context.TajikCats.AnyAsync(c => c.Id == catId) == false)
                                                    {
                                                        var tajikCat = new GanjoorTajikCat()
                                                        {
                                                            Id = catId,
                                                            PoetId = poetId,
                                                            TajikTitle = cat.text
                                                        };
                                                        context.Add(tajikCat);
                                                        await context.SaveChangesAsync();
                                                    }
                                                    var poems = (await sqlite.QueryAsync($"SELECT * FROM poem WHERE cat_id = {catId} ORDER BY id")).ToList();
                                                    foreach (var poem in poems)
                                                    {
                                                        int poemId = (int)poem.id;
                                                        if (await context.GanjoorPoems.AnyAsync(p => p.Id == poemId) == false) continue;
                                                        if(await context.TajikPoems.AnyAsync(p => p.Id == poemId) == false)
                                                        {
                                                            var verses = (await sqlite.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poemId} ORDER BY vorder")).ToList();
                                                            var ganjoorVerses = await context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == poemId).ToListAsync();

                                                            if (verses.Count != ganjoorVerses.Count) continue;

                                                            await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId} - cat: {catId} - poem: {poemId}");
                                                            int currentCatId = catId;
                                                            string fullTitle = poem.title;
                                                            while (currentCatId != 0)
                                                            {
                                                                var tajikCat = await context.TajikCats.AsNoTracking().Where(c => c.Id == currentCatId).SingleAsync();
                                                                var gCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.Id == tajikCat.Id).SingleAsync();

                                                                fullTitle = tajikCat.TajikTitle + " - " + fullTitle;
                                                                currentCatId = gCat.ParentId ?? 0;
                                                            }

                                                            var ganjoorPoem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == poemId).SingleAsync();
                                                            var tajikPoem = new GanjoorTajikPoem()
                                                            {
                                                                Id = poemId,
                                                                CatId = catId,
                                                                FullUrl = ganjoorPoem.FullUrl,
                                                                FullTitle = fullTitle,
                                                                TajikTitle = poem.title,
                                                                TajikPlainText = "",
                                                            };
                                                            context.Add(tajikPoem);
                                                            await context.SaveChangesAsync();
                                                            await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId} - cat: {catId} - poem: {poemId} - query verses");
                                                            string plaintText = "";
                                                            foreach (var verse in verses)
                                                            {
                                                                await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId} - cat: {catId} - poem: {poemId} - verse: {verse.vorder}");
                                                                var ganjoorVerse = ganjoorVerses.Where(v => v.VOrder == (int)verse.vorder).SingleOrDefault();
                                                                if (ganjoorVerse == null) continue;
                                                                var tajikVerse = new GanjoorTajikVerse()
                                                                {
                                                                    Id = ganjoorVerse.Id,
                                                                    PoemId = poemId,
                                                                    TajikText = verse.text,
                                                                    VOrder = (int)verse.vorder,
                                                                };
                                                                context.Add(tajikVerse);
                                                                plaintText += tajikVerse.TajikText;
                                                                plaintText += "\r\n";
                                                            }
                                                            await context.SaveChangesAsync();
                                                            tajikPoem.TajikPlainText = plaintText;
                                                            context.Update(tajikPoem);
                                                            await context.SaveChangesAsync();

                                                            await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId} - cat: {catId} - poem: {poemId} - page");
                                                            if (await context.TajikPages.AnyAsync(p => p.Id == poemId) == false)
                                                            {
                                                                GanjoorTajikPage page = new GanjoorTajikPage()
                                                                {
                                                                    Id = poemId,
                                                                    TajikHtmlText = PrepareHtmlText
                                                                    (
                                                                        
                                                                        verses.Select(s => new GanjoorVerse()
                                                                        {
                                                                            VOrder = (int)s.vorder,
                                                                            Text = s.text,
                                                                            VersePosition = (VersePosition) ((int)s.position)
                                                                        }
                                                                        ).ToList()
                                                                    ),
                                                                };
                                                                context.Add(page);
                                                                await context.SaveChangesAsync();
                                                            }

                                                        }

                                                    }
                                                }

                                                foreach (var cat in cats)
                                                {
                                                    int catId = (int)cat.id;
                                                    await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId} - cat: {catId} - page");
                                                    var catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == catId).SingleAsync();
                                                    if (await context.TajikPages.AnyAsync(p => p.Id == catPage.Id) == false)
                                                    {
                                                        var tajikCat = await context.TajikCats.AsNoTracking().Where(c => c.Id == catId).SingleAsync();
                                                        GanjoorTajikPage page = new GanjoorTajikPage()
                                                        {
                                                            Id = catPage.Id,
                                                            TajikHtmlText = await PrepareTajikCatHtmlTextAsync(context, tajikCat),
                                                        };
                                                        context.Add(page);
                                                        await context.SaveChangesAsync();
                                                    }
                                                }

                                                var poetPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == poetId).SingleAsync();
                                                if (await context.TajikPages.AnyAsync(p => p.Id == poetPage.Id) == false)
                                                {
                                                    await jobProgressServiceEF.UpdateJob(job.Id, poetId, $"poet: {poetId} - page");
                                                    var tajikPoet = await context.TajikPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync();
                                                    GanjoorTajikPage page = new GanjoorTajikPage()
                                                    {
                                                        Id = poetPage.Id,
                                                        TajikHtmlText = await PrepareTajikPoetHtmlTextAsync(context, tajikPoet),
                                                    };
                                                    context.Add(page);
                                                    await context.SaveChangesAsync();
                                                }
                                            }
                                        }
                                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
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