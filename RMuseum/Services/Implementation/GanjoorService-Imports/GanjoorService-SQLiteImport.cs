using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Data;
using System.Data.Common;
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
                                            IDbConnection dapper = sqliteConnection;
                                            using (var sqlConnection = _context.Database.GetDbConnection())
                                            {
                                                await sqlConnection.OpenAsync();

                                                var poets = (await dapper.QueryAsync("SELECT * FROM poet")).ToList();
                                                if (poets.Count != 1)
                                                {
                                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "poets count in sqlite db is not equal to 1");
                                                }


                                                var poet = await context.GanjoorPoets.AsNoTracking().Where(p => p.Id == poetId).SingleAsync();

                                                await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Importing");



                                                await context.SaveChangesAsync();

                                                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);

                                            }
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
        public async Task _ImportSQLiteCatChildren(DbCommand command, IDbConnection sqlite, int poetId, int catId, string itemFullTitle, string itemFullSlug)
        {

            foreach (var cat in await sqlite.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} AND parent_id = {catId} ORDER BY id"))
            {

                if (catId == 0)
                {
                    command.CommandText =
                                  $"INSERT INTO GanjoorCategories (Id, PoetId, Title, UrlSlug, FullUrl) VALUES (${cat.id}, {poetId}, N'{cat.text}', '{ _ExtractUrlSlug(cat.url)}', '{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}')";
                }
                else
                {
                    command.CommandText =
                                  $"INSERT INTO GanjoorCategories (Id, PoetId, Title, ParentId, UrlSlug, FullUrl) VALUES (${cat.id}, {poetId}, N'{cat.text}', {catId}, '{ _ExtractUrlSlug(cat.url)}', '{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}')";
                }

                await command.ExecuteNonQueryAsync();


                foreach (var poem in await sqlite.QueryAsync($"SELECT * FROM poem WHERE cat_id = {cat.id} ORDER BY id"))
                {
                    command.CommandText =
                               $"INSERT INTO GanjoorPoems (Id, CatId, Title, UrlSlug, FullTitle, FullUrl) VALUES (${poem.id}, {cat.id}, N'{poem.title}', '{ _ExtractUrlSlug(poem.url)}', N'{$"{itemFullTitle}{cat.text} » {poem.title}"}', '{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}/{_ExtractUrlSlug(poem.url)}')";
                    await command.ExecuteNonQueryAsync();

                    foreach (var verse in await sqlite.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poem.id} ORDER BY vorder"))
                    {
                        command.CommandText =
                              $"INSERT INTO GanjoorVerses (PoemId, VOrder, VersePosition, Text) VALUES (${poem.id}, {verse.vorder}, {verse.position}, N'{verse.text}')";
                        await command.ExecuteNonQueryAsync();
                    }

                }


                await _ImportSQLiteCatChildren(command, sqlite, poetId, (int)cat.id, $"{itemFullTitle}{cat.text} » ", $"{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}");


            }
        }
        private static string _ExtractUrlSlug(string slug)
        {
            if (!string.IsNullOrEmpty(slug))
            {
                //sample: https://ganjoor.net/hafez
                if (slug.LastIndexOf('/') != -1)
                {
                    // => result = hafez
                    slug = slug.Substring(slug.LastIndexOf('/') + 1);
                }
            }
            return slug;
        }
    }
}
