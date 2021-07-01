using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        private async Task _UpdateMundexPage(Guid editingUserId, RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            var poemMusicTracks =
                                        await context.GanjoorPoemMusicTracks
                                        .Where(m =>
                                            m.TrackType == PoemMusicTrackType.BeepTunesOrKhosousi
                                            &&
                                            m.Approved)
                                            .ToListAsync();

            var dbPage = await context.GanjoorPages.Where(p => p.FullUrl == "/mundex").SingleAsync();


            string htmlText = $"<p>در این صفحه فهرست اشعار استفاده شده در آلبومهای موسیقی را با استفاده از اطلاعات جمع‌آوری شده در <a href=\"http://blog.ganjoor.net/1395/06/28/bptags/\">این پروژه</a> به تفکیک خواننده و به ترتیب نزولی تعداد قطعات مرتبط گرد آورده‌ایم." +
            $" تا تاریخ {LanguageUtils.FormatDate(DateTime.Now)} ارتباط {poemMusicTracks.Count.ToPersianNumbers()} قطعهٔ موسیقی از {poemMusicTracks.GroupBy(m => m.ArtistName).Count().ToPersianNumbers()} هنرمند با {poemMusicTracks.GroupBy(m => m.PoemId).Count().ToPersianNumbers()} شعر در پایگاه گنجور ثبت و تأیید شده است.  </p>{Environment.NewLine}";
            htmlText += $"<p>جهت مشاهدهٔ این اطلاعات به تفکیک شاعران <small>(به همراه اطلاعات مجموعهٔ گلها و سایت اسپاتیفای)</small> <a href=\"/mundex/bypoet/\" > این صفحه</a> را ببینید.</p>{Environment.NewLine}";
            htmlText += $"<p>جهت کمک به تکمیل این مجموعه <a href=\"http://blog.ganjoor.net/1395/06/28/bptags/\">این مطلب</a> را مطالعه بفرمایید و <a href=\"http://www.aparat.com/v/kxGre\">این فیلم</a> را مشاهده کنید.</p>{Environment.NewLine}";

            var singers = poemMusicTracks.GroupBy(m => new { m.ArtistName, m.ArtistUrl })
                            .Select(g => new { ArtistName = g.Key.ArtistName, ArtistUrl = g.Key.ArtistUrl, TrackCount = g.Count() })
                            .OrderByDescending(g => g.TrackCount)
                            .ToList();
            using (HttpClient httpClient = new HttpClient())
                for (int nSinger = 0; nSinger < singers.Count; nSinger++)
                {
                    var singer = singers[nSinger];

                    var tracks = poemMusicTracks.
                                        Where(m => m.ArtistName == singer.ArtistName && m.ArtistUrl == singer.ArtistUrl)
                                        .OrderBy(m => m.PoemId)
                                        .ToList();
                    if (tracks.Count != singer.TrackCount)
                        continue;//!!! a weird situration I cannot figure out now!

                    htmlText += $"<p><br style=\"clear: both;\" /></p>{Environment.NewLine}";
                    htmlText += $"<h2>{(nSinger + 1).ToPersianNumbers()}. <a href=\"{singer.ArtistUrl}\">";
                    htmlText += $"{singer.ArtistName} ({singer.TrackCount.ToPersianNumbers()} قطعه)</a></h2>{Environment.NewLine}";
                    htmlText += "<div class=\"spacer\">&nbsp;</div>";


                    if (singer.ArtistUrl.Contains("beeptunes.com/artist/"))
                    {
                        var bUrl = singer.ArtistUrl;
                        if (bUrl.LastIndexOf('/') == (bUrl.Length - 1))
                            bUrl = bUrl.Substring(0, (bUrl.Length - 1));
                        var beepId = bUrl.Substring(bUrl.LastIndexOf("/") + 1);
                        var response = await httpClient.GetAsync($"https://newapi.beeptunes.com/public/artist/info/?artistId={beepId}");
                        if (response.IsSuccessStatusCode)
                        {
                            dynamic bpArtist = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                            if (bpArtist.artistImage != null)
                            {
                                htmlText += $"<div style=\"width:240px;margin:auto\">{Environment.NewLine}" +
                                $"<a href=\"{singer.ArtistUrl}\">{Environment.NewLine}" +
                                $"<img src=\"{bpArtist.artistImage}\" alt=\"{singer.ArtistName}\"/>{Environment.NewLine}" +
                                $"</a>{Environment.NewLine}" +
                                $"</div>{Environment.NewLine}";
                            }
                        }
                    }

                    htmlText += $"<ol>{Environment.NewLine}";

                    foreach (var song in tracks)
                    {
                        htmlText += $"<li><p>{Environment.NewLine}";
                        var poem = await context.GanjoorPoems.Where(p => p.Id == song.PoemId).SingleAsync();
                        htmlText += $"<a href=\"{poem.FullUrl}\">{poem.FullTitle}</a> در ";
                        htmlText += $"<a href=\"{song.AlbumUrl}\">{song.AlbumName.ToPersianNumbers()}</a> » <a href=\"{song.TrackUrl}\">{song.TrackName.ToPersianNumbers()}</a></p></li>{Environment.NewLine}";
                    }

                    htmlText += $"</ol>{Environment.NewLine}";
                }


            await _UpdatePageHtmlText(context, editingUserId, dbPage, "به روزرسانی خودکار صفحهٔ نمایهٔ موسیقی", htmlText);
        }

        private async Task _UpdateMundexByPoetPage(Guid editingUserId, RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            var poemMusicTracks =
                                await context.GanjoorPoemMusicTracks.Include(m => m.Poem).ThenInclude(p => p.Cat)
                                .Where(m =>
                                    m.Approved)
                                    .ToListAsync();

            var dbPage = await context.GanjoorPages.Where(p => p.FullUrl == "/mundex/bypoet").SingleAsync();


            string htmlText = $"<p>در این صفحه فهرست اشعار استفاده شده در آلبومهای موسیقی را با استفاده از اطلاعات جمع‌آوری شده در <a href=\"http://blog.ganjoor.net/1395/06/28/bptags/\">این پروژه</a> به تفکیک شاعر و به ترتیب نزولی تعداد اشعار مرتبط گرد آورده‌ایم." +
            $" تا تاریخ {LanguageUtils.FormatDate(DateTime.Now)} ارتباط {poemMusicTracks.GroupBy(m => m.PoemId).Count().ToPersianNumbers()} شعر از {poemMusicTracks.GroupBy(m => m.Poem.Cat.PoetId).Count().ToPersianNumbers()} شاعر با {poemMusicTracks.Count.ToPersianNumbers()} قطعهٔ موسیقی در پایگاه گنجور ثبت و تأیید شده است.  </p>{Environment.NewLine}";
            htmlText += $"<p>جهت مشاهدهٔ این اطلاعات به تفکیک هنرمندان <small>(بدون اطلاعات مجموعهٔ گلها، سایت اسپاتیفای و اجراهای خصوصی)</small> <a href=\"/mundex/\" > این صفحه</a> را ببینید.</p>{Environment.NewLine}";
            htmlText += $"<p>جهت کمک به تکمیل این مجموعه <a href=\"http://blog.ganjoor.net/1395/06/28/bptags/\">این مطلب</a> را مطالعه بفرمایید و <a href=\"http://www.aparat.com/v/kxGre\">این فیلم</a> را مشاهده کنید.</p>{Environment.NewLine}";

            var poetIdsAndTrackCounts = poemMusicTracks.GroupBy(m => new { m.Poem.Cat.PoetId })
                            .Select(g => new { PoetId = g.Key.PoetId, TrackCount = g.Count() })
                            .OrderByDescending(g => g.TrackCount)
                            .ToList();
            using (HttpClient httpClient = new HttpClient())
                for (int nPoetIndex = 0; nPoetIndex < poetIdsAndTrackCounts.Count; nPoetIndex++)
                {
                    var poetIdAndTrackCount = poetIdsAndTrackCounts[nPoetIndex];

                    var tracks = poemMusicTracks.
                                        Where(m => m.Poem.Cat.PoetId == poetIdAndTrackCount.PoetId)
                                        .OrderBy(m => m.PoemId)
                                        .ToList();
                    if (tracks.Count != poetIdAndTrackCount.TrackCount)
                        continue;//!!! a weird situration I cannot figure out now!
                    var poet = await context.GanjoorPoets.Where(p => p.Id == poetIdAndTrackCount.PoetId).AsNoTracking().SingleAsync();
                    var poetCat = await context.GanjoorCategories.Where(c => c.PoetId == poetIdAndTrackCount.PoetId && c.ParentId == null).AsNoTracking().SingleAsync();

                    htmlText += $"<p><br style=\"clear: both;\" /></p>{Environment.NewLine}";
                    htmlText += $"<h2>{nPoetIndex+1}. <a href=\"{poetCat.FullUrl}\">{poet.Nickname}</a></h2>{Environment.NewLine}";
                    htmlText += $"<div class=\"spacer\">&nbsp;</div>{Environment.NewLine}";
                    htmlText += $"<div style=\"width:82px;margin:auto\"><a href=\"{poetCat.FullUrl}\"><img src=\"https://ganjgah.ir/api/ganjoor/poet/image/{poetCat.UrlSlug}.gif\" alt=\"{poet.Nickname}\" /></a></div>{Environment.NewLine}";
                    htmlText += $"<div style=\"width:100%;margin:auto\"><a href=\"/{poetCat.FullUrl}\" >{poet.Nickname}</a> ({tracks.Count.ToPersianNumbers()} قطعه)</div>{Environment.NewLine}" +
                        $"<div class=\"spacer\">&nbsp;</div>{Environment.NewLine}";

                    htmlText += $"<ol>{Environment.NewLine}";

                    foreach (var song in tracks)
                    {
                        htmlText += $"<li><p>{Environment.NewLine}";
                        var poem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == song.PoemId).SingleAsync();
                        htmlText += $"<a href=\"{poem.FullUrl}\">{poem.FullTitle}</a> در ";

                        if(song.TrackType == PoemMusicTrackType.Golha)
                        {
                            htmlText += "گلها  » ";
                        }
                        else
                        {
                            htmlText += $"{song.ArtistName}  » ";
                        }

                        htmlText += $"{song.AlbumName.ToPersianNumbers()} » <a href=\"{song.TrackUrl}\">{song.TrackName.ToPersianNumbers()}</a></p></li>{Environment.NewLine}";
                    }

                    htmlText += $"</ol>{Environment.NewLine}";
                }


            await _UpdatePageHtmlText(context, editingUserId, dbPage, "به روزرسانی خودکار صفحهٔ شاعران به روایت آهنگها", htmlText);
        }


        /// <summary>
        /// start updating stats page
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> StartUpdatingMundexPage(Guid editingUserId)
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
                                    var job = (await jobProgressServiceEF.NewJob("UpdateMundexPage", "Mundex Page")).Result;

                                    try
                                    {

                                        await _UpdateMundexPage(editingUserId, context, jobProgressServiceEF, job);
                                        await jobProgressServiceEF.UpdateJob(job.Id, 50, "Mundex by poet page");
                                        await _UpdateMundexByPoetPage(editingUserId, context, jobProgressServiceEF, job);

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
    }
}