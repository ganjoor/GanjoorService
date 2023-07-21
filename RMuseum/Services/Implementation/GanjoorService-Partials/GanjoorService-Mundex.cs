using DNTPersianUtils.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.MusicCatalogue;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services.Implementation;
using System;
using System.Data;
using System.IO;
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
        private async Task _PerformMundexHouseKeepingAndPreparation(RMuseumDbContext context, LongRunningJobProgressServiceEF jobProgressServiceEF, RLongRunningJobStatus job)
        {
            var singers = await context.GanjoorSingers.ToListAsync();
            foreach(var singer in singers)
                if(!string.IsNullOrEmpty(singer.Url) && singer.Url.Length > 0 && singer.Url[singer.Url.Length - 1] == '/')
                {
                    singer.Url = singer.Url.Substring(0, singer.Url.Length - 1);
                    context.GanjoorSingers.Update(singer);
                }
            await context.SaveChangesAsync();
            await jobProgressServiceEF.UpdateJob(job.Id, 2);

            var poemMusicTracks =
                                await context.GanjoorPoemMusicTracks
                                .Where(m =>
                                    m.TrackType == PoemMusicTrackType.BeepTunesOrKhosousi
                                    &&
                                    m.Approved)
                                    .ToListAsync();

            IConfigurationRoot configuration = new ConfigurationBuilder()
                                          .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                                          .Build();
            ImageFileServiceEF imageFileService = new ImageFileServiceEF(context, configuration);

            using (HttpClient httpClient = new HttpClient())
                foreach (var poemMusicTrack in poemMusicTracks)
                {
                    if (!string.IsNullOrEmpty(poemMusicTrack.ArtistUrl) && poemMusicTrack.ArtistUrl.Length > 0 && poemMusicTrack.ArtistUrl[poemMusicTrack.ArtistUrl.Length - 1] == '/')
                    {
                        poemMusicTrack.ArtistUrl = poemMusicTrack.ArtistUrl.Substring(0, poemMusicTrack.ArtistUrl.Length - 1);
                        context.GanjoorPoemMusicTracks.Update(poemMusicTrack);
                    }

                    var singer =
                        poemMusicTrack.SingerId != null ?
                        context.GanjoorSingers.Where(s => s.Id == poemMusicTrack.SingerId).FirstOrDefault()
                        :
                        context.GanjoorSingers.Where(s => s.Url == poemMusicTrack.ArtistUrl).FirstOrDefault();
                    if (singer == null)
                    {
                        singer = new GanjoorSinger()
                        {
                            Name = poemMusicTrack.ArtistName,
                            Url = poemMusicTrack.ArtistUrl
                        };
                        context.GanjoorSingers.Add(singer);
                        await context.SaveChangesAsync();
                    }

                    if(poemMusicTrack.SingerId == null)
                    {
                        poemMusicTrack.SingerId = singer.Id;
                        context.GanjoorPoemMusicTracks.Update(poemMusicTrack);
                    }

                    //singer image:
                    if (singer.Url.Contains("beeptunes.com/artist/"))
                    {
                        if (singer.RImageId == null)
                        {
                            var bUrl = singer.Url;
                            var beepId = bUrl.Substring(bUrl.LastIndexOf("/") + 1);
                            var response = await httpClient.GetAsync($"https://newapi.beeptunes.com/public/artist/info/?artistId={beepId}");
                            if (response.IsSuccessStatusCode)
                            {
                                dynamic bpArtist = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                                if (bpArtist.artistImage != null)
                                {
                                    try
                                    {
                                        var imageResult = await httpClient.GetAsync(bpArtist.artistImage.ToString());
                                        if (imageResult.IsSuccessStatusCode)
                                        {
                                            using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                            {
                                                RServiceResult<RImage> image = await imageFileService.Add(null, imageStream, $"{beepId}.jpg", Path.Combine(configuration.GetSection("PictureFileService")["StoragePath"], "SingerImages"), true);
                                                if (string.IsNullOrEmpty(image.ExceptionString))
                                                {
                                                    image = await imageFileService.Store(image.Result);
                                                    if (string.IsNullOrEmpty(image.ExceptionString))
                                                    {
                                                        singer.RImageId = image.Result.Id;
                                                        context.GanjoorSingers.Update(singer);
                                                        await context.SaveChangesAsync();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            await context.SaveChangesAsync();
            await jobProgressServiceEF.UpdateJob(job.Id, 3);

        }
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
            
            
            htmlText += $"<div class=\"part-title-block\"><a href=\"/mundex/bypoet/\">مشاهدهٔ نمایهٔ موسیقی به تفکیک سخنوران (به همراه اطلاعات مجموعهٔ گلها و سایت اسپاتیفای)</a></div>{Environment.NewLine}";
            htmlText += $"<p>جهت کمک به تکمیل این مجموعه <a href=\"http://blog.ganjoor.net/1395/06/28/bptags/\">این مطلب</a> را مطالعه بفرمایید و <a href=\"http://www.aparat.com/v/kxGre\">این فیلم</a> را مشاهده کنید.</p>{Environment.NewLine}";

            var singers = poemMusicTracks.GroupBy(m => new { m.SingerId })
                            .Select(g => new { SingerId = g.Key.SingerId, TrackCount = g.Count() })
                            .OrderByDescending(g => g.TrackCount)
                            .ToList();
            using (HttpClient httpClient = new HttpClient())
                for (int nSinger = 0; nSinger < singers.Count; nSinger++)
                {
                    var singer = singers[nSinger];

                    var dbSinger = await context.GanjoorSingers.Where(s => s.Id == singer.SingerId).SingleAsync();

                    var tracks = poemMusicTracks.
                                        Where(m => m.SingerId == singer.SingerId)
                                        .OrderBy(m => m.PoemId)
                                        .ToList();
                    if (tracks.Count != singer.TrackCount)
                        continue;//!!! a weird situration I cannot figure out now!


                    htmlText += $"<p><br style=\"clear: both;\" /></p>{Environment.NewLine}";
                    htmlText += $"<h2>{(nSinger + 1).ToPersianNumbers()}. <a href=\"{dbSinger.Url}\">";
                    htmlText += $"{dbSinger.Name}</a></h2>{Environment.NewLine}";
                    htmlText += "<div class=\"spacer\">&nbsp;</div>";


                    if (dbSinger.RImageId != null)
                    {
                        var imageUrl = $"{WebServiceUrl.Url}/api/rimages/{dbSinger.RImageId}.jpg";
                        htmlText += $"<div style=\"width:240px;margin:auto\">{Environment.NewLine}" +
                            $"<a href=\"{dbSinger.Url}\">{Environment.NewLine}" +
                            $"<img src=\"{imageUrl}\" loading=\"lazy\" alt=\"{dbSinger.Name}\"/>{Environment.NewLine}" +
                            $"</a>{Environment.NewLine}" +
                            $"</div>{Environment.NewLine}";
                    }

                    htmlText += $"<div class=\"century\">{Environment.NewLine}";
                    htmlText += $"{singer.TrackCount.ToPersianNumbers()} قطعه";
                    htmlText += $"<a role=\"button\" class=\"w3tooltip cursor-pointer\" onclick=\"switch_section('item-section-{nSinger}', 'item-collapse-button-{nSinger}')\"><i class=\"info-buttons collapse_circle_down\" id=\"item-collapse-button-{nSinger}\"></i><span class=\"w3tooltiptext\">جمع شود / باز شود</span></a>";
                    htmlText += $"</div>{Environment.NewLine}";
                    htmlText += $"<div id=\"item-section-{nSinger}\" style=\"display:none\">{Environment.NewLine}";
                    htmlText += $"<ol>{Environment.NewLine}";

                    foreach (var song in tracks)
                    {
                        htmlText += $"<li><p>{Environment.NewLine}";
                        var poem = await context.GanjoorPoems.Where(p => p.Id == song.PoemId).SingleAsync();
                        htmlText += $"<a href=\"{poem.FullUrl}\">{poem.FullTitle}</a> در ";
                        htmlText += $"<a href=\"{song.AlbumUrl}\">{song.AlbumName.ToPersianNumbers()}</a> » <a href=\"{song.TrackUrl}\">{song.TrackName.ToPersianNumbers()}</a></p></li>{Environment.NewLine}";
                    }

                    htmlText += $"</ol>{Environment.NewLine}";
                    htmlText += $"</div>{Environment.NewLine}";
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


            string htmlText = $"<p>در این صفحه فهرست اشعار استفاده شده در آلبومهای موسیقی را با استفاده از اطلاعات جمع‌آوری شده در <a href=\"http://blog.ganjoor.net/1395/06/28/bptags/\">این پروژه</a> به تفکیک سخنور و به ترتیب نزولی تعداد اشعار مرتبط گرد آورده‌ایم." +
            $" تا تاریخ {LanguageUtils.FormatDate(DateTime.Now)} ارتباط {poemMusicTracks.GroupBy(m => m.PoemId).Count().ToPersianNumbers()} شعر از {poemMusicTracks.GroupBy(m => m.Poem.Cat.PoetId).Count().ToPersianNumbers()} سخنور با {poemMusicTracks.Count.ToPersianNumbers()} قطعهٔ موسیقی در پایگاه گنجور ثبت و تأیید شده است.  </p>{Environment.NewLine}";

            htmlText += $"<div class=\"part-title-block\"><a href=\"/mundex/\">مشاهدهٔ نمایهٔ موسیقی به تفکیک هنرمندان (بدون اطلاعات مجموعهٔ گلها و سایت اسپاتیفای)</a></div>{Environment.NewLine}";
            
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
                    htmlText += $"<h2>{(nPoetIndex+1).ToPersianNumbers()}. <a href=\"{poetCat.FullUrl}\">{poet.Nickname}</a></h2>{Environment.NewLine}";
                    htmlText += $"<div class=\"spacer\">&nbsp;</div>{Environment.NewLine}";
                    htmlText += $"<div style=\"width:82px;margin:auto\"><a href=\"{poetCat.FullUrl}\"><img src=\"{WebServiceUrl.Url}/api/ganjoor/poet/image/{poetCat.UrlSlug}.gif\" alt=\"{poet.Nickname}\" /></a></div>{Environment.NewLine}";

                    htmlText += $"<div class=\"century\">{Environment.NewLine}";
                    htmlText += $"{tracks.Count.ToPersianNumbers()} قطعه";
                    htmlText += $"<a role=\"button\" class=\"w3tooltip cursor-pointer\" onclick=\"switch_section('item-section-{nPoetIndex}', 'item-collapse-button-{nPoetIndex}')\"><i class=\"info-buttons collapse_circle_down\" id=\"item-collapse-button-{nPoetIndex}\"></i><span class=\"w3tooltiptext\">جمع شود / باز شود</span></a>";
                    htmlText += $"</div>{Environment.NewLine}";
                    htmlText += $"<div id=\"item-section-{nPoetIndex}\" style=\"display:none\">{Environment.NewLine}";

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
                    htmlText += $"</div>{Environment.NewLine}";
                }


            await _UpdatePageHtmlText(context, editingUserId, dbPage, "به روزرسانی خودکار صفحهٔ سخنوران به روایت آهنگها", htmlText);
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
                                    var job = (await jobProgressServiceEF.NewJob("UpdateMundexPage", "House Keeping")).Result;

                                    try
                                    {
                                        await _PerformMundexHouseKeepingAndPreparation(context, jobProgressServiceEF, job);
                                        await jobProgressServiceEF.UpdateJob(job.Id, 10, "Mundex Page");
                                        await _UpdateMundexPage(editingUserId, context, jobProgressServiceEF, job);
                                        await jobProgressServiceEF.UpdateJob(job.Id, 60, "Mundex by poet page");
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