using Microsoft.Data.Sqlite;
using RMuseum.DbContext;
using RSecurityBackend.Models.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using System;
using Microsoft.Extensions.Configuration;

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
                using (SqliteConnection connection = new SqliteConnection(connectionStringBuilder.ToString()))
                {
                    await connection.OpenAsync();

                    IDbConnection dapper = connection;

                    foreach (var poet in await dapper.QueryAsync("SELECT * FROM poet ORDER BY id"))
                    {
                        int poetId = (int)poet.id;
                        using(RMuseumDbContext context = new RMuseumDbContext(Configuration))
                        {
                            if ((await context.GanjoorPoets.Where(p => p.Id == poetId).FirstOrDefaultAsync()) != null)
                            {
                                continue;
                            }

                            context.GanjoorPoets.Add
                                (
                                new GanjoorPoet()
                                {
                                    Id = poetId,
                                    Name = poet.name,
                                    Description = poet.description
                                }
                                );

                            await context.SaveChangesAsync();

                            
                        }

                        await _ImportSQLiteCatChildren(dapper, poetId, 0, poet.name);



                    }

                }
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            
            return new RServiceResult<bool>(true);
        }

        

        private async Task _ImportSQLiteCatChildren(IDbConnection dapper, int poetId, int catId, string itemFullTitle)
        {
           
            foreach(var cat in await dapper.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} AND parent_id = {catId} ORDER BY id"))
            {
                using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                {
                    context.GanjoorCategories.Add
                    (
                    new GanjoorCat()
                    {
                        Id = (int)cat.id,
                        PoetId = poetId,
                        Title = cat.text,
                        UrlSlug = _ExtractUrlSlug(cat.url)
                    }
                    );

                    foreach (var poem in await dapper.QueryAsync($"SELECT * FROM poem WHERE cat_id = {cat.id} ORDER BY id"))
                    {
                        context.GanjoorPoems.Add
                            (
                            new GanjoorPoem()
                            {
                                Id = (int)poem.id,
                                CatId = (int)cat.id,
                                Title = poem.title,
                                UrlSlug = _ExtractUrlSlug(poem.url),
                                FullTitle = $"{itemFullTitle} » {cat.text} » {poem.title}"
                            }
                            );

                        foreach (var verse in await dapper.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poem.id} ORDER BY vorder"))
                        {
                            context.GanjoorVerses.Add
                                (
                                new GanjoorVerse()
                                {
                                    PoemId = (int)poem.id,
                                    VOrder = (int)verse.vorder,
                                    VersePosition = (VersePosition)verse.position,
                                    Text = verse.text
                                }
                                );
                        }

                        await context.SaveChangesAsync();
                    }
                

                    await _ImportSQLiteCatChildren(dapper, poetId, (int)cat.id, $"{itemFullTitle} » {cat.text}");
                }


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
