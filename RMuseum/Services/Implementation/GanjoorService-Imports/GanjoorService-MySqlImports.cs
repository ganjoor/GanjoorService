using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.MusicCatalogue;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

        /// <summary>
        /// import GanjoorPage entity data from MySql
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> ImportFromMySql()
        {
            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                async token =>
                {

                    using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                    using (RMuseumDbContext contextReport = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                    {
                        LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(contextReport);

                        var job = (await jobProgressServiceEF.NewJob("GanjoorService:ImportFromMySql", "pre open connection")).Result;


                        MusicCatalogueService catalogueService = new MusicCatalogueService(Configuration, context);
                        RServiceResult<bool> musicCatalogueRes = await catalogueService.ImportFromMySql("MusicCatalogueImportFromMySql", jobProgressServiceEF, job);

                        if (!musicCatalogueRes.Result)
                            return;


                        try
                        {

                            using (MySqlConnection connection = new MySqlConnection
                                            (
                                            $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8;convert zero datetime=True"
                                            ))
                            {
                                connection.Open();
                                using (MySqlDataAdapter src = new MySqlDataAdapter(
                                    "SELECT ID, post_author, post_date, post_date_gmt, post_content, post_title, post_category, post_excerpt, post_status, comment_status, ping_status, post_password, post_name, to_ping, pinged, post_modified, post_modified_gmt, post_content_filtered, post_parent, guid, menu_order, post_type, post_mime_type, comment_count, " +
                                    "COALESCE((SELECT meta_value FROM ganja_postmeta WHERE post_id = ID AND meta_key='_wp_page_template'), '') AS template," +
                                    "(SELECT meta_value FROM ganja_postmeta WHERE post_id = ID AND meta_key='otherpoetid') AS other_poet_id " +
                                    "FROM ganja_posts",
                                    connection))
                                {
                                    using (DataTable srcData = new DataTable())
                                    {
                                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 1 - mysql 1")).Result;

                                        await src.FillAsync(srcData);

                                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 1 - processing mysql data")).Result;


                                        foreach (DataRow row in srcData.Rows)
                                        {
                                            GanjoorPageType pageType =
                                                row["post_type"].ToString() == "post" && row["comment_status"].ToString() != "closed" ?
                                                        GanjoorPageType.PoemPage
                                                        :
                                                        row["template"].ToString() == "comspage.php" ?
                                                        GanjoorPageType.AllComments
                                                        :
                                                        row["template"].ToString() == "relations.php" ?
                                                        GanjoorPageType.ProsodySimilars
                                                        :
                                                        row["template"].ToString() == "vazn.php" ?
                                                        GanjoorPageType.ProsodyAndStats
                                                        :
                                                        GanjoorPageType.None;

                                            int? poetId = row["post_author"].ToString() == "1" ? (int?)null : int.Parse(row["post_author"].ToString());
                                            if (poetId == 36)//رشحه
                                            {
                                                continue;
                                            }

                                            if (poetId != null)
                                            {
                                                if (!await context.GanjoorPoets.Where(poet => poet.Id == poetId).AnyAsync())
                                                {
                                                    continue;
                                                }
                                            }

                                            GanjoorPage page = new GanjoorPage()
                                            {
                                                Id = int.Parse(row["ID"].ToString()),
                                                GanjoorPageType = pageType,
                                                Published = true,
                                                PageOrder = -1,
                                                Title = row["post_title"].ToString(),
                                                UrlSlug = row["post_name"].ToString(),
                                                HtmlText = row["post_content"].ToString(),
                                                ParentId = row["post_parent"].ToString() == "0" ? (int?)null : int.Parse(row["post_parent"].ToString()),
                                                PoetId = poetId,
                                                SecondPoetId = row["other_poet_id"] == DBNull.Value ? (int?)null : int.Parse(row["other_poet_id"].ToString()),
                                                PostDate = (DateTime)row["post_date"]
                                            };



                                            if (pageType == GanjoorPageType.PoemPage)
                                            {
                                                var poem = await context.GanjoorPoems.Where(p => p.Id == page.Id).FirstOrDefaultAsync();
                                                if (poem == null)
                                                    continue;
                                                page.PoemId = poem.Id;
                                            }
                                            if (poetId != null && pageType == GanjoorPageType.None)
                                            {
                                                GanjoorCat cat = await context.GanjoorCategories.Where(c => c.PoetId == poetId && c.ParentId == null && c.UrlSlug == page.UrlSlug).SingleOrDefaultAsync();
                                                if (cat != null)
                                                {
                                                    page.GanjoorPageType = GanjoorPageType.PoetPage;
                                                    page.CatId = cat.Id;
                                                }
                                                else
                                                {
                                                    cat = await context.GanjoorCategories.Where(c => c.PoetId == poetId && c.ParentId != null && c.UrlSlug == page.UrlSlug).SingleOrDefaultAsync();
                                                    if (cat != null)
                                                    {
                                                        page.GanjoorPageType = GanjoorPageType.CatPage;
                                                        page.CatId = cat.Id;
                                                    }
                                                }
                                            }

                                            context.GanjoorPages.Add(page);

                                        }
                                    }
                                }
                            }
                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 1 - finalizing")).Result;
                            await context.SaveChangesAsync();



                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 2 - pre fetch data")).Result;

                            var orphanPages = await context.GanjoorPages.Include(p => p.Poem).Where(p => p.FullUrl == null).ToListAsync();

                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 2 - post fetch data")).Result;

                            double count = orphanPages.Count;
                            int i = 0;
                            foreach (var page in orphanPages)
                            {

                                job = (await jobProgressServiceEF.UpdateJob(job.Id, i++, "phase 2")).Result;

                                string fullUrl = page.UrlSlug;
                                string fullTitle = page.Title;

                                if (page.GanjoorPageType == GanjoorPageType.PoemPage)
                                {
                                    fullTitle = page.Poem.FullTitle;
                                    fullUrl = page.Poem.FullUrl;
                                }
                                else
                                {
                                    if (page.ParentId != null)
                                    {
                                        GanjoorPage parent = await context.GanjoorPages.Where(p => p.Id == page.ParentId).SingleAsync();
                                        while (parent != null)
                                        {
                                            fullUrl = parent.UrlSlug + "/" + fullUrl;
                                            fullTitle = parent.Title + " » " + fullTitle;
                                            parent = parent.ParentId == null ? null : await context.GanjoorPages.Where(p => p.Id == parent.ParentId).SingleAsync();
                                        }
                                    }
                                    else
                                    {
                                        GanjoorCat cat = await context.GanjoorCategories.Where(c => c.PoetId == page.PoetId && c.UrlSlug == page.UrlSlug).SingleOrDefaultAsync();
                                        if (cat != null)
                                        {
                                            fullUrl = cat.FullUrl;
                                            while (cat.ParentId != null)
                                            {
                                                cat = await context.GanjoorCategories.Where(c => c.Id == cat.ParentId).SingleOrDefaultAsync();
                                                if (cat != null)
                                                {
                                                    fullTitle = cat.Title + " » " + fullTitle;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            cat = await context.GanjoorCategories.Where(c => c.PoetId == page.PoetId && c.ParentId == null).SingleOrDefaultAsync();
                                            if (cat != null)
                                            {
                                                fullUrl = $"{cat.UrlSlug}/{page.UrlSlug}";
                                            }
                                        }
                                    }

                                }
                                if (!string.IsNullOrEmpty(fullUrl) && fullUrl.IndexOf('/') != 0)
                                    fullUrl = $"/{fullUrl}";
                                page.FullUrl = fullUrl;
                                page.FullTitle = fullTitle;

                                context.Update(page);
                            }

                            job = (await jobProgressServiceEF.UpdateJob(job.Id, job.Progress, "phase 2 - finalizing")).Result;

                            await context.SaveChangesAsync();

                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 3 - pre mysql data fetch")).Result;

                            using (MySqlConnection connection = new MySqlConnection
                                            (
                                            $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8;convert zero datetime=True"
                                            ))
                            {
                                connection.Open();
                                using (MySqlDataAdapter src = new MySqlDataAdapter(
                                    "SELECT meta_key, post_id, meta_value FROM ganja_postmeta WHERE meta_key IN ( 'vazn', 'ravi', 'src', 'srcslug' )",
                                    connection))
                                {
                                    job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 3 - mysql 2")).Result;
                                    using (DataTable srcData = new DataTable())
                                    {
                                        await src.FillAsync(srcData);

                                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 3 - processing meta data")).Result;

                                        int r = 0;
                                        foreach (DataRow row in srcData.Rows)
                                        {
                                            job = (await jobProgressServiceEF.UpdateJob(job.Id, r++, "phase 3 - processing meta data")).Result;

                                            int poemId = int.Parse(row["post_id"].ToString());
                                            var poem = await context.GanjoorPoems.Where(p => p.Id == poemId).FirstOrDefaultAsync();
                                            if (poem == null)
                                                continue;

                                            string metaKey = row["meta_key"].ToString();
                                            string metaValue = row["meta_value"].ToString();
                                            switch (metaKey)
                                            {
                                                case "vazn":
                                                    {
                                                        GanjoorMetre metre = await context.GanjoorMetres.Where(m => m.Rhythm == metaValue).SingleOrDefaultAsync();
                                                        if (metre == null)
                                                        {
                                                            metre = new GanjoorMetre()
                                                            {
                                                                Rhythm = metaValue,
                                                                VerseCount = 0
                                                            };
                                                            context.GanjoorMetres.Add(metre);
                                                            await context.SaveChangesAsync();
                                                        }

                                                        poem.GanjoorMetreId = metre.Id;
                                                    }
                                                    break;
                                                case "ravi":
                                                    poem.RhymeLetters = metaValue;
                                                    break;
                                                case "src":
                                                    poem.SourceName = metaValue;
                                                    break;
                                                case "srcslug":
                                                    poem.SourceUrlSlug = metaValue;
                                                    break;
                                            }

                                            context.GanjoorPoems.Update(poem);
                                        }
                                    }
                                }
                            }
                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase 3 - finalizing meta data")).Result;
                            await context.SaveChangesAsync();

                            var resApprovedPoemSongs = await _ImportPoemSongsDataFromMySql("_ImportPoemSongsDataFromMySql", context, jobProgressServiceEF, job, true);
                            if (!resApprovedPoemSongs.Result)
                            {
                                return;
                            }

                            var resPendingPoemSongs = await _ImportPoemSongsDataFromMySql("_ImportPoemSongsDataFromMySql", context, jobProgressServiceEF, job, false);
                            if (!resPendingPoemSongs.Result)
                            {
                                return;
                            }

                            using (MySqlConnection connection = new MySqlConnection
                                            (
                                            $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8;convert zero datetime=True"
                                            ))
                            {
                                connection.Open();
                                using (MySqlDataAdapter src = new MySqlDataAdapter(
                                    "SELECT poem_id, mimage_id FROM ganja_mimages",
                                    connection))
                                {
                                    job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase N - mysql N")).Result;
                                    using (DataTable srcData = new DataTable())
                                    {
                                        await src.FillAsync(srcData);

                                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase N - processing meta data")).Result;

                                        int r = 0;
                                        foreach (DataRow row in srcData.Rows)
                                        {
                                            job = (await jobProgressServiceEF.UpdateJob(job.Id, r++, "phase N - processing meta data")).Result;

                                            int poemId = int.Parse(row["poem_id"].ToString());
                                            Guid imageId = Guid.Parse(row["mimage_id"].ToString());

                                            var link = await context.GanjoorLinks.Include(l => l.Item).ThenInclude(i => i.Images).
                                                    Where(l => l.GanjoorPostId == poemId && l.Item.Images.First().Id == imageId)
                                                    .FirstOrDefaultAsync();
                                            if (link != null)
                                            {
                                                link.DisplayOnPage = true;
                                                context.GanjoorLinks.Update(link);
                                            }
                                        }
                                    }
                                }
                            }
                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, "phase N - finalizing meta data")).Result;
                            await context.SaveChangesAsync();


                           
                        }
                        catch (Exception jobExp)
                        {
                            await jobProgressServiceEF.UpdateJob(job.Id, job.Progress, "", false, jobExp.ToString());
                        }
                        var resComments = await _ImportCommentsDataFromMySql("_ImportCommentsDataFromMySql", context, jobProgressServiceEF, job);
                        if (!resComments.Result)
                        {
                            return;
                        }

                        await jobProgressServiceEF.UpdateJob(job.Id, 100, "Finished", true);
                    }
                });


                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private async Task<RServiceResult<bool>> _ImportPoemSongsDataFromMySql(string jobName, RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job, bool approved)
        {
            try
            {
                job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - pre mysql data fetch")).Result;

                string connectionString =
                    approved ?
                    $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8;convert zero datetime=True"
                    :
                    $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["SongsUsername"]};pwd={Configuration.GetSection("AudioMySqlServer")["SongsPassword"]};database={Configuration.GetSection("AudioMySqlServer")["SongsDatabase"]};charset=utf8;convert zero datetime=True";

                using (MySqlConnection connection = new MySqlConnection
                                (
                                connectionString
                                ))
                {
                    connection.Open();
                    using (MySqlDataAdapter src = new MySqlDataAdapter(
                        "SELECT poem_id, artist_name, artist_beeptunesurl, album_name, album_beeptunesurl, track_name, track_beeptunesurl, ptrack_typeid FROM ganja_ptracks ORDER BY id",
                        connection))
                    {
                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - mysql")).Result;
                        using (DataTable data = new DataTable())
                        {
                            await src.FillAsync(data);

                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - processing approved poem songs")).Result;

                            foreach (DataRow row in data.Rows)
                            {
                                PoemMusicTrack track = new PoemMusicTrack()
                                {
                                    TrackType = (PoemMusicTrackType)int.Parse(row["ptrack_typeid"].ToString()),
                                    PoemId = int.Parse(row["poem_id"].ToString()),
                                    ArtistName = row["artist_name"].ToString(),
                                    ArtistUrl = row["artist_beeptunesurl"].ToString(),
                                    AlbumName = row["album_name"].ToString(),
                                    AlbumUrl = row["album_beeptunesurl"].ToString(),
                                    TrackName = row["track_name"].ToString(),
                                    TrackUrl = row["track_beeptunesurl"].ToString(),
                                    ApprovalDate = DateTime.Now,
                                    Description = "",
                                    Approved = approved
                                };

                                var poem = await context.GanjoorPoems.Where(p => p.Id == track.PoemId).SingleOrDefaultAsync();
                                if (poem == null)
                                    continue;

                                switch (track.TrackType)
                                {
                                    case PoemMusicTrackType.BeepTunesOrKhosousi:
                                    case PoemMusicTrackType.iTunes:
                                        {
                                            GanjoorTrack catalogueTrack = await context.GanjoorMusicCatalogueTracks.Where(m => m.Url == track.TrackUrl).FirstOrDefaultAsync();
                                            if (catalogueTrack != null)
                                            {
                                                track.GanjoorTrackId = catalogueTrack.Id;
                                            }

                                            GanjoorSinger singer = await context.GanjoorSingers.Where(s => s.Url == track.ArtistUrl).FirstOrDefaultAsync();
                                            if (singer != null)
                                            {
                                                track.SingerId = singer.Id;
                                            }
                                        }
                                        break;
                                    case PoemMusicTrackType.Golha:
                                        {
                                            track.AlbumName = $"{track.ArtistName} » {track.AlbumName}";
                                            track.ArtistName = "";

                                            track.GolhaTrackId = int.Parse(track.ArtistUrl);
                                            track.ArtistUrl = "";
                                        }
                                        break;
                                }

                                context.GanjoorPoemMusicTracks.Add(track);

                            }
                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - finalizing approved poem songs data")).Result;

                            await context.SaveChangesAsync();

                        }

                    }
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, job.Progress, "", false, exp.ToString());
                return new RServiceResult<bool>(false, exp.ToString());
            }

        }

        private async Task<RServiceResult<bool>> _ImportCommentsDataFromMySql(string jobName, RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            try
            {
                job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - pre mysql data fetch")).Result;

                string connectionString =
                    $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8;convert zero datetime=True";

                using (MySqlConnection connection = new MySqlConnection
                                (
                                connectionString
                                ))
                {
                    connection.Open();
                    using (MySqlDataAdapter src = new MySqlDataAdapter(
                        "SELECT comment_post_ID, comment_author, comment_author_email, comment_author_url, comment_author_IP, comment_date, comment_content, comment_approved FROM ganja_comments WHERE comment_type <> 'pingback' ORDER BY comment_ID",
                        connection))
                    {
                        job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - mysql")).Result;
                        using (DataTable data = new DataTable())
                        {
                            await src.FillAsync(data);

                            job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - processing approved poem songs")).Result;

                            int count = data.Rows.Count;
                            int i = 0;

                            int percent = -1;

                            foreach (DataRow row in data.Rows)
                            {
                                GanjoorComment comment = new GanjoorComment()
                                {
                                    PoemId = int.Parse(row["comment_post_ID"].ToString()),
                                    AuthorName = row["comment_author"].ToString(),
                                    AuthorEmail = row["comment_author_email"].ToString(),
                                    AuthorUrl = row["comment_author_url"].ToString(),
                                    AuthorIpAddress = row["comment_author_IP"].ToString(),
                                    CommentDate = (DateTime)row["comment_date"],
                                    HtmlComment = _PrepareCommentHtml(row["comment_content"].ToString()),
                                    Status = row["comment_approved"].ToString() == "1" ? PublishStatus.Published : PublishStatus.Awaiting
                                };

                                var poem = await context.GanjoorPoems.Where(p => p.Id == comment.PoemId).SingleOrDefaultAsync();
                                if (poem == null)
                                    continue;


                                context.GanjoorComments.Add(comment);


                                i++;

                                if(i * 100 / count > percent)
                                {
                                    percent = i * 100 / count;

                                    job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - {i} of {count}")).Result;
                                }
                            }

                            await context.SaveChangesAsync();

                        }

                    }
                }

                job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - assigning comments to users")).Result;
                foreach (var user in await context.Users.ToListAsync())
                {
                    foreach(var comment in await context.GanjoorComments.Where(u => u.AuthorEmail == user.Email).ToListAsync())
                    {
                        comment.UserId = user.Id;
                        context.GanjoorComments.Update(comment);
                    }
                }
                await context.SaveChangesAsync();


                job = (await jobProgressServiceEF.UpdateJob(job.Id, 0, $"{jobName} - finished")).Result;


                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, job.Progress, "", false, exp.ToString());
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

       

        private List<GanjoorVerse> _extractVersesFromPoemHtmlText(string poemtext)
        {
            List<GanjoorVerse> verses = new List<GanjoorVerse>();

            //this spagetti code has been imported from my internal utilities:
            while (poemtext.IndexOf("<a href") != -1)
            {
                int ahrefStart = poemtext.IndexOf("<a href");
                string part1 = poemtext.Substring(0, ahrefStart);
                string part2 = poemtext.Substring(poemtext.IndexOf(">", ahrefStart) + 1, poemtext.IndexOf("</a>") - (poemtext.IndexOf(">", ahrefStart) + 1));
                poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("</a>") + 4, poemtext.Length - (poemtext.IndexOf("</a>") + 4));
            }
            while (poemtext.IndexOf("<acronym") != -1)
            {
                int acroStart = poemtext.IndexOf("<acronym");
                string part1 = poemtext.Substring(0, acroStart);
                string part2;
                try
                {
                    part2 = poemtext.Substring(poemtext.IndexOf(">", acroStart) + 1, poemtext.IndexOf("</acronym>") - (poemtext.IndexOf(">", acroStart) + 1));
                    poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("</acronym>") + 10, poemtext.Length - (poemtext.IndexOf("</acronym>") + 10));
                }
                catch
                {
                    part2 = poemtext.Substring(poemtext.IndexOf(">", acroStart) + 1, poemtext.IndexOf("<acronym>") - (poemtext.IndexOf(">", acroStart) + 1));
                    poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("<acronym>") + 10, poemtext.Length - (poemtext.IndexOf("<acronym>") + 10));
                }

            }

            while (poemtext.IndexOf("<sup>") != -1)
            {
                string part1 = poemtext.Substring(0, poemtext.IndexOf("<sup>"));
                try
                {
                    poemtext = part1 + poemtext.Substring(poemtext.IndexOf("</sup>") + 6, poemtext.Length - (poemtext.IndexOf("</sup>") + 6));
                    poemtext = poemtext.Replace("  ", " ");
                }
                catch
                {
                    throw new Exception($"poemtext.IndexOf(\"<sup>\": {poemtext}");
                }

            }


            poemtext = poemtext.Replace("Adaptation du milieu", "یییییییییییییییییییی");
            poemtext = poemtext.Replace("Empirique", "ببببببببب");


            poemtext = poemtext.Replace("<div class=\"b\" style=\"width:750px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:660px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:680px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:650px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:690px\">", "<div class=\"b\">").Replace("<p style=\"color:#911\">", "<p>").Replace("<p style=\"color:#191\">", "<p>").Replace("<div class=\"spacer\">", "").Replace("&nbsp;", "").Replace("<div class=\"spacer\" />", "").Replace("<div class=\"b\" style=\"width:700px\">", "<div class=\"b\">");
            poemtext = poemtext.Replace("<em>", "").Replace("</em>", "");
            poemtext = poemtext.Replace("<em>", "").Replace("</em>", "").Replace("<small>", "").Replace("</small>", "");
            poemtext = poemtext.Replace("<b>", "").Replace("</b>", "").Replace("<strong>", "").Replace("</strong>", "");
            poemtext = poemtext.Replace("<p><br style=\"clear:both;\"/></p>", "").Replace("<br style=\"clear:both;\"/>", "");
            if (poemtext.IndexOf("\r\n") == 0)
                poemtext = poemtext.Substring(2);
            poemtext = poemtext.Replace("\r", "").Replace("\n", "");
            poemtext = poemtext.Replace("</div>", "").Replace("</p>", "");
            poemtext = poemtext.Replace("<div class=\"b2\">", "a");
            poemtext = poemtext.Replace("<div class=\"b\">", "b");
            poemtext = poemtext.Replace("<div class=\"m1\">", "m");
            poemtext = poemtext.Replace("<div class=\"m2\">", "n");
            poemtext = poemtext.Replace("<div class=\"n\">", "");
            poemtext = poemtext.Replace("<p>", "p");
            poemtext = poemtext.Replace("bmp", "b");
            poemtext = poemtext.Replace("np", "n");
            poemtext = poemtext.Replace("ap", "a");
            poemtext = poemtext.Replace("\"", "").Replace("'", "");
            if (poemtext.IndexOfAny(new char[] { '<', '>' }) != -1)
                throw new Exception($"Invalid Characteres: {poemtext}");
            if (poemtext.IndexOf("mp") != -1)
                throw new Exception($"مصرع اول بدون مصرع دوم: {poemtext}");

            if (poemtext.Length > 0)
            {

                int idx = poemtext.IndexOfAny(new char[] { 'a', 'b', 'm', 'n', 'p' });
                bool preWasBand = false;
                while (idx != -1)
                {
                    GanjoorVerse verse = new GanjoorVerse();
                    verse.VOrder = verses.Count + 1;
                    switch (poemtext[idx])
                    {
                        case 'p':
                            if (preWasBand)
                                verse.VersePosition = VersePosition.CenteredVerse2;
                            else
                                verse.VersePosition = VersePosition.Paragraph;
                            preWasBand = false;
                            break;
                        case 'b':
                            verse.VersePosition = VersePosition.Right;
                            preWasBand = false;
                            break;
                        case 'n':
                            verse.VersePosition = VersePosition.Left;
                            preWasBand = false;
                            break;
                        case 'a':
                            verse.VersePosition = VersePosition.CenteredVerse1;
                            preWasBand = true;
                            break;
                    }
                    int nextIdx = poemtext.IndexOfAny(new char[] { 'a', 'b', 'm', 'n', 'p' }, idx + 1);
                    if (nextIdx == -1)
                    {
                        verse.Text = poemtext.Substring(idx + 1).Replace("یییییییییییییییییییی", "Adaptation du milieu").Replace("ببببببببب", "Empirique");
                    }
                    else
                    {
                        verse.Text = poemtext.Substring(idx + 1, nextIdx - idx - 1).Replace("یییییییییییییییییییی", "Adaptation du milieu").Replace("ببببببببب", "Empirique");
                    }

                    verses.Add(verse);

                    idx = nextIdx;
                }
            }

            return verses;
        }

        private string _Linkify(string SearchText)
        {
            // this will find links like:
            // http://www.mysite.com
            // as well as any links with other characters directly in front of it like:
            // href="http://www.mysite.com"
            // you can then use your own logic to determine which links to linkify
            Regex regx = new Regex(@"\b(((\S+)?)(@|mailto\:|(news|(ht|f)tp(s?))\://)\S+)\b", RegexOptions.IgnoreCase);
            SearchText = SearchText.Replace("&nbsp;", " ");
            MatchCollection matches = regx.Matches(SearchText);

            foreach (Match match in matches)
            {
                if (match.Value.StartsWith("http"))
                { // if it starts with anything else then dont linkify -- may already be linked!
                    SearchText = SearchText.Replace(match.Value, "<a href='" + match.Value + "'>" + match.Value + "</a>");
                }
            }

            return SearchText;
        }

        private string _PrepareCommentHtml(string comment)
        {
            comment = comment.ToPersianNumbers().ApplyCorrectYeKe();
            comment = _Linkify(comment);
            comment = $"<p>{comment.Replace("\r\n", "\n").Replace("\n\n", "\n").Replace("\n", "<br />")}</p>";

            return comment;
        }
    }
}
