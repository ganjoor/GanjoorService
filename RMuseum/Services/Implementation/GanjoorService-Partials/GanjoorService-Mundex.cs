using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Data;
using System.Linq;
using System.Net.Http;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {

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
                                    var job = (await jobProgressServiceEF.NewJob("UpdateMundexPage", "Query data")).Result;

                                    try
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
                                        using(HttpClient httpClient = new HttpClient())
                                        for (int nSinger = 0; nSinger < singers.Count; nSinger++)
                                        {
                                            var singer = singers[nSinger];

                                           

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
                                                if(response.IsSuccessStatusCode)
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

                                            foreach (var song in poemMusicTracks.Where(m => m.ArtistName == singer.ArtistName && m.ArtistUrl == singer.ArtistUrl).OrderBy(m => m.PoemId).ToList())
                                            {
                                                htmlText += $"<li><p>{Environment.NewLine}";
                                                var poem = await context.GanjoorPoems.Where(p => p.Id == song.PoemId).SingleAsync();
                                                htmlText += $"<a href=\"{poem.FullUrl}\">{poem.FullTitle}</a> در ";
                                                htmlText += $"<a href=\"{song.AlbumUrl}\">{song.AlbumName.ToPersianNumbers()}</a> » <a href=\"{song.TrackUrl}\">{song.TrackName.ToPersianNumbers()}</a></p></li>{Environment.NewLine}";
                                            }

                                            htmlText += $"</ol>{Environment.NewLine}";
                                        }


                                        await _UpdatePageHtmlText(context, editingUserId, dbPage, "به روزرسانی خودکار صفحهٔ نمایهٔ موسیقی", htmlText);

                                        

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