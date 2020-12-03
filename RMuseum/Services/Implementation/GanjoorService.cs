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
                connectionStringBuilder.DataSource = Configuration.GetSection("LocalSqliteImport")["FilePath"];
                using (SqliteConnection sqliteConnection = new SqliteConnection(connectionStringBuilder.ToString()))
                {
                    await sqliteConnection.OpenAsync();
                    IDbConnection dapper = sqliteConnection;
                    using (var sqlConnection = _context.Database.GetDbConnection())
                    {
                        await sqlConnection.OpenAsync();
                        
                            foreach (var poet in await dapper.QueryAsync("SELECT * FROM poet ORDER BY id"))
                            {
                                int poetId = (int)poet.id;
                                if ((await _context.GanjoorPoets.Where(p => p.Id == poetId).FirstOrDefaultAsync()) != null)
                                {
                                    continue;
                                }

                                DbTransaction transaction = await sqlConnection.BeginTransactionAsync();
                                try
                                {
                                    using (var command = sqlConnection.CreateCommand())
                                    {
                                        command.Transaction = transaction;
                                        command.CommandText =
                                            $"INSERT INTO GanjoorPoets (Id, Name, Description) VALUES (${poet.id}, N'{poet.name}', N'{poet.description}')";
                                        await command.ExecuteNonQueryAsync();
                                        await _ImportSQLiteCatChildren(command, dapper, poetId, 0, "", "");
                                        await transaction.CommitAsync();
                                    }
                                }
                                catch (Exception exp2)
                                {
                                    await transaction.RollbackAsync();
                                    return new RServiceResult<bool>(false, exp2.ToString());
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

        

        private async Task _ImportSQLiteCatChildren(DbCommand command, IDbConnection dapper, int poetId, int catId, string itemFullTitle, string itemFullSlug)
        {
           
            foreach(var cat in await dapper.QueryAsync($"SELECT * FROM cat WHERE poet_id = {poetId} AND parent_id = {catId} ORDER BY id"))
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


                foreach (var poem in await dapper.QueryAsync($"SELECT * FROM poem WHERE cat_id = {cat.id} ORDER BY id"))
                {
                    command.CommandText =
                               $"INSERT INTO GanjoorPoems (Id, CatId, Title, UrlSlug, FullTitle, FullUrl) VALUES (${poem.id}, {cat.id}, N'{poem.title}', '{ _ExtractUrlSlug(poem.url)}', N'{$"{itemFullTitle}{cat.text} » {poem.title}"}', '{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}/{_ExtractUrlSlug(poem.url)}')";
                    await command.ExecuteNonQueryAsync();

                    foreach (var verse in await dapper.QueryAsync($"SELECT * FROM verse WHERE poem_id = {poem.id} ORDER BY vorder"))
                    {
                        command.CommandText =
                              $"INSERT INTO GanjoorVerses (PoemId, VOrder, VersePosition, Text) VALUES (${poem.id}, {verse.vorder}, {verse.position}, N'{verse.text}')";
                        await command.ExecuteNonQueryAsync();
                    }

                }


                await _ImportSQLiteCatChildren(command, dapper, poetId, (int)cat.id, $"{itemFullTitle}{cat.text} » ", $"{itemFullSlug}/{ _ExtractUrlSlug(cat.url)}");
                

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
        /// updates poems text
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> UpdatePoemsText()
        {
            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                async token =>
                {
                    using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                    {
                        var poems = await context.GanjoorPoems.Where(p => p.PlainText == null).ToListAsync();
                        foreach (GanjoorPoem poem in poems)
                        {
                            var verses = await context.GanjoorVerses.Where(v => v.PoemId == poem.Id).OrderBy(v => v.VOrder).ToListAsync();
                            string plainText = "";
                            string htmlText = "";
                            for (int vIndex = 0; vIndex < verses.Count; vIndex++)
                            {
                                GanjoorVerse v = verses[vIndex];

                                if (string.IsNullOrEmpty(plainText))
                                    plainText = v.Text;
                                else
                                {
                                    plainText += $" {v.Text}";
                                }

                                switch (v.VersePosition)
                                {
                                    case VersePosition.CenteredVerse1:
                                        if (((vIndex + 1) < verses.Count) && (verses[vIndex + 1].VersePosition == VersePosition.CenteredVerse2))
                                        {
                                            htmlText += $"<div class=\"b2\"><p>{v.Text.Replace("ـ", "").Trim()}</p>{Environment.NewLine}"; //div is not closed
                                        }
                                        else
                                        {
                                            htmlText += $"<div class=\"b2\"><p>{v.Text.Replace("ـ", "").Trim()}</p></div>{Environment.NewLine}";
                                        }
                                        break;
                                    case VersePosition.CenteredVerse2:
                                        htmlText += $"<p>{v.Text.Replace("ـ", "").Trim()}</p></div>{Environment.NewLine}";
                                        break;
                                    case VersePosition.Right:
                                        htmlText += $"<div class=\"b\"><div class=\"m1\"><p>{v.Text.Replace("ـ", "").Trim()}</p></div>{Environment.NewLine}";
                                        break;
                                    case VersePosition.Left:
                                        htmlText += $"<div class=\"m2\"><p>{v.Text.Replace("ـ", "").Trim()}</p></div></div>{Environment.NewLine}";
                                        break;
                                    case VersePosition.Paragraph:
                                    case VersePosition.Single:
                                        {
                                            string[] lines = v.Text.Replace("ـ", "").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                                            if (lines.Length != 0)
                                            {
                                                if (v.Text.Replace("ـ", "").Length / lines.Length < 150)
                                                {
                                                    htmlText += $"<div class=\"n\"><p>{v.Text.Replace("ـ", "").Replace("\r\n", " ")}</p></div>{Environment.NewLine}";
                                                }
                                                else
                                                {
                                                    foreach (string line in lines)
                                                        htmlText += $"<div class=\"n\"><p>{line}</p></div>{Environment.NewLine}";
                                                }
                                            }
                                        }
                                        break;
                                }

                                poem.PlainText = plainText;
                                poem.HtmlText = htmlText;

                                context.Update(poem);

                                await context.SaveChangesAsync();

                            }
                        }
                        

                    }


                });


                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }

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
        /// Background Task Queue Instance
        /// </summary>
        protected readonly IBackgroundTaskQueue _backgroundTaskQueue;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        public GanjoorService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _context = context;
            _backgroundTaskQueue = backgroundTaskQueue;
            Configuration = configuration;
        }
    }
}
