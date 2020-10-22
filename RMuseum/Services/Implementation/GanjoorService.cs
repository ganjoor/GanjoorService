using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
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
    public class GanjoorService : IGanjoorService
    {
        #region SQLite import
        /// <summary>
        /// imports unimported poem data from a locally accessible ganjoor SqlLite database
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ImportLocalSQLiteDb()
        {
            try
            {
                SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();
                connectionStringBuilder.DataSource = @"C:\Users\moham\AppData\Local\ganjoor\ganjoor.s3db";
                using (SqliteConnection sqliteConnection = new SqliteConnection(connectionStringBuilder.ToString()))
                {
                    await sqliteConnection.OpenAsync();

                    IDbConnection dapper = sqliteConnection;

                    using (var sqlConnection = _context.Database.GetDbConnection())
                    {
                        await sqlConnection.OpenAsync();
                        using (var command = sqlConnection.CreateCommand())
                        {
                            foreach (var poet in await dapper.QueryAsync("SELECT * FROM poet ORDER BY id"))
                            {
                                int poetId = (int)poet.id;
                                if ((await _context.GanjoorPoets.Where(p => p.Id == poetId).FirstOrDefaultAsync()) != null)
                                {
                                    continue;
                                }
                                command.CommandText =
                                    $"INSERT INTO GanjoorPoets (Id, Name, Description) VALUES (${poet.id}, N'{poet.name}', N'{poet.description}')";
                                await command.ExecuteNonQueryAsync();
                                                              
                                
                                await _ImportSQLiteCatChildren(command, dapper, poetId, 0, poet.name);
                            }
                        }

                    }


                }
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            
            return new RServiceResult<bool>(true);
        }

        

        private async Task _ImportSQLiteCatChildren(DbCommand command, IDbConnection dapper, int poetId, int catId, string itemFullTitle)
        {
           
            foreach(var cat in await dapper.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} AND parent_id = {catId} ORDER BY id"))
            {
                if(catId == 0)
                {
                    command.CommandText =
                                  $"INSERT INTO GanjoorCategories (Id, PoetId, Title, UrlSlug) VALUES (${cat.id}, {poetId}, N'{cat.text}', '{ _ExtractUrlSlug(cat.url)}')";
                }
                else
                {
                    command.CommandText =
                                  $"INSERT INTO GanjoorCategories (Id, PoetId, Title, ParentId, UrlSlug) VALUES (${cat.id}, {poetId}, N'{cat.text}', {catId}, '{ _ExtractUrlSlug(cat.url)}')";
                }
               
                await command.ExecuteNonQueryAsync();


                foreach (var poem in await dapper.QueryAsync($"SELECT * FROM poem WHERE cat_id = {cat.id} ORDER BY id"))
                {
                    command.CommandText =
                               $"INSERT INTO GanjoorPoems (Id, CatId, Title, UrlSlug, FullTitle) VALUES (${poem.id}, {cat.id}, N'{poem.title}', '{ _ExtractUrlSlug(poem.url)}', N'{$"{itemFullTitle} » {cat.text} » {poem.title}"}')";
                    await command.ExecuteNonQueryAsync();

                    foreach (var verse in await dapper.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poem.id} ORDER BY vorder"))
                    {
                        command.CommandText =
                              $"INSERT INTO GanjoorVerses (PoemId, VOrder, VersePosition, Text) VALUES (${poem.id}, {verse.vorder}, {verse.position}, N'{verse.text}')";
                        await command.ExecuteNonQueryAsync();
                    }

                }


                await _ImportSQLiteCatChildren(command, dapper, poetId, (int)cat.id, $"{itemFullTitle} » {cat.text}");
                

            }
        }

        #endregion

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

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public GanjoorService(RMuseumDbContext context, IConfiguration configuration)
        {
            _context = context;
            Configuration = configuration;
        }
    }
}
