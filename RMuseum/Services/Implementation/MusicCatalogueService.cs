using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using RMuseum.DbContext;
using RMuseum.Models.MusicCatalogue;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// Music Catalgue Service Implementation
    /// </summary>
    public class MusicCatalogueService
    {
        /// <summary>
        /// import catalogue from ganjoor.net MySql db
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ImportFromMySql(LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            try
            {
                

                using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                {
                    using (MySqlConnection connection = new MySqlConnection
                                           (
                                           $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["SongsUsername"]};pwd={Configuration.GetSection("AudioMySqlServer")["SongsPassword"]};database={Configuration.GetSection("AudioMySqlServer")["SongsDatabase"]};charset=utf8;convert zero datetime=True"
                                           ))
                    {
                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 4 - import golha data - pre open connection")).Result;

                        connection.Open();

                        using (MySqlDataAdapter src = new MySqlDataAdapter(
                            "SELECT col_id, name FROM golha_collections ORDER BY col_id",
                            connection))
                        {
                            using (DataTable data = new DataTable())
                            {
                                await src.FillAsync(data);

                                foreach (DataRow row in data.Rows)
                                {


                                    GolhaCollection collection = new GolhaCollection()
                                    {
                                        Id = int.Parse(row["col_id"].ToString()),
                                        Name = row["name"].ToString(),
                                        Programs = new List<GolhaProgram>()
                                    };

                                    job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"phase 4 - import golha data - golha_collections: {collection.Id}")).Result;


                                    using (MySqlDataAdapter srcPrograms = new MySqlDataAdapter(
                                        $"SELECT program_id, title, progarm_order, url, mp3 FROM golha_programs WHERE col_id = {collection.Id} ORDER BY program_id",
                                        connection))
                                    {
                                        using (DataTable programData = new DataTable())
                                        {
                                            await srcPrograms.FillAsync(programData);


                                            foreach (DataRow golhaProgram in programData.Rows)
                                            {
                                                GolhaProgram program = new GolhaProgram()
                                                {
                                                    Id = int.Parse(golhaProgram["program_id"].ToString()),
                                                    Title = golhaProgram["title"].ToString(),
                                                    ProgramOrder = int.Parse(golhaProgram["progarm_order"].ToString()),
                                                    Url = golhaProgram["url"].ToString(),
                                                    Mp3 = golhaProgram["mp3"].ToString(),
                                                    Tracks = new List<GolhaTrack>()
                                                };

                                                using (MySqlDataAdapter srcTracks = new MySqlDataAdapter(
                                               $"SELECT track_no, timing, title FROM golha_tracks WHERE program_id = {program.Id} ORDER BY track_no",
                                               connection))
                                                {
                                                    using (DataTable trackData = new DataTable())
                                                    {
                                                        await srcTracks.FillAsync(trackData);

                                                        foreach (DataRow golhaTrack in trackData.Rows)
                                                        {
                                                            program.Tracks.Add
                                                                (
                                                                new GolhaTrack()
                                                                {
                                                                    TrackNo = int.Parse(golhaTrack["track_no"].ToString()),
                                                                    Timing = golhaTrack["timing"].ToString(),
                                                                    Title = golhaTrack["title"].ToString(),
                                                                    Blocked = false,
                                                                }
                                                                );
                                                        }
                                                    }

                                                }
                                                collection.Programs.Add(program);

                                            }

                                        }
                                    }
                                    context.GolhaCollections.Add(collection);
                                    await context.SaveChangesAsync();
                                }



                            }
                        }


                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 4 - import singers data")).Result;

                        using (MySqlDataAdapter src = new MySqlDataAdapter(
                            "SELECT artist_id, artist_name, artist_beeptunesurl FROM ganja_artists ORDER BY artist_id",
                            connection))
                        {
                            using (DataTable data = new DataTable())
                            {
                                await src.FillAsync(data);

                                foreach (DataRow row in data.Rows)
                                {
                                    int artistId = int.Parse(row["artist_id"].ToString());

                                    GanjoorSinger singer = new GanjoorSinger()
                                    {
                                        Name = row["artist_name"].ToString(),
                                        Url = row["artist_beeptunesurl"].ToString(),
                                        Albums = new List<GanjoorAlbum>()
                                    };

                                    using (MySqlDataAdapter srcAlbums = new MySqlDataAdapter(
                                    $"SELECT album_id, album_name, album_beeptunesurl FROM ganja_albums WHERE album_artistid = {artistId} ORDER BY album_id",
                                    connection))
                                    {
                                        using (DataTable dataAlbums = new DataTable())
                                        {
                                            await srcAlbums.FillAsync(dataAlbums);

                                            foreach (DataRow rowAlbum in dataAlbums.Rows)
                                            {

                                                int albumId = int.Parse(rowAlbum["album_id"].ToString());

                                                GanjoorAlbum album = new GanjoorAlbum()
                                                {
                                                    Name = rowAlbum["album_name"].ToString(),
                                                    Url = rowAlbum["album_beeptunesurl"].ToString(),
                                                    Tracks = new List<GanjoorTrack>()
                                                };

                                                using (MySqlDataAdapter srcTracks = new MySqlDataAdapter(
                                                $"SELECT track_name, track_beeptunesurl FROM ganja_tracks WHERE album_id = {albumId} ORDER BY track_id",
                                                connection))
                                                {
                                                    using (DataTable dataTracks = new DataTable())
                                                    {
                                                        await srcTracks.FillAsync(dataTracks);

                                                        foreach (DataRow rowTrack in dataTracks.Rows)
                                                        {
                                                            album.Tracks.Add
                                                                (
                                                                new GanjoorTrack()
                                                                {
                                                                    Name = rowTrack["track_name"].ToString(),
                                                                    Url = rowTrack["track_beeptunesurl"].ToString(),
                                                                    Blocked = false
                                                                }
                                                                );
                                                        }
                                                    }
                                                }

                                                singer.Albums.Add(album);

                                            }
                                        }
                                    }




                                    context.Singers.Add(singer);

                                    job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"phase 4 - import singers data - {singer.Name}")).Result;

                                }
                            }
                        }

                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 4 - finalizing singers data")).Result;

                        await context.SaveChangesAsync();
                    }
                }

                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, job.Progress, "", false, exp.ToString());

                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }
       
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        public MusicCatalogueService(IConfiguration configuration)
        {
            Configuration = configuration;
        }

    }
}
