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
        public async Task<RServiceResult<bool>> TajikImportFromSqlite(int poetId, IFormFile file)
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
                                            var poets = (await sqlite.QueryAsync("SELECT * FROM poet ORDER BY id")).ToList();
                                            if (poetId != 0 && poets.Count != 1)
                                            {
                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "poets count in sqlite db is not equal to 1");
                                            }

                                            foreach (var poet in poets)
                                            {
                                                int poetId = poet.id;
                                                if (await context.GanjoorPoets.AnyAsync(p => p.Id == poetId) == false) continue;
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
                                                    var poetPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.PoetPage && p.PoetId == poetId).SingleAsync();
                                                    if (await context.TajikPages.AnyAsync(p => p.Id == poetPage.Id) == false)
                                                    {
                                                        GanjoorTajikPage page = new GanjoorTajikPage()
                                                        {
                                                            Id = poetPage.Id,
                                                            TajikHtmlText = await PrepareTajikPoetHtmlTextAsync(context, tajikPoet),
                                                        };
                                                        context.Add(page);
                                                        await context.SaveChangesAsync();
                                                    }
                                                }
                                                var cats = (await sqlite.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} ORDER BY id")).ToList();
                                                foreach (var cat in cats)
                                                {
                                                    int catId = cat.Id;
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

                                                        var catPage = await context.GanjoorPages.AsNoTracking().Where(p => p.GanjoorPageType == GanjoorPageType.CatPage && p.CatId == catId).SingleAsync();
                                                        if (await context.TajikPages.AnyAsync(p => p.Id == catPage.Id) == false)
                                                        {
                                                            GanjoorTajikPage page = new GanjoorTajikPage()
                                                            {
                                                                Id = catPage.Id,
                                                                TajikHtmlText = await PrepareTajikCatHtmlTextAsync(context, tajikCat),
                                                            };
                                                            context.Add(page);
                                                            await context.SaveChangesAsync();
                                                        }
                                                       
                                                    }
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