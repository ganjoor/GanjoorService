using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.MusicCatalogue;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using RSecurityBackend.Services.Implementation;
using RMuseum.Models.Auth.Memory;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// get poem related songs
        /// </summary>
        /// <param name="id"></param>
        /// <param name="approved"></param>
        /// <param name="trackType"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel[]>> GetPoemSongs(int id, bool approved, PoemMusicTrackType trackType)
        {
            return new RServiceResult<PoemMusicTrackViewModel[]>
                (
                await _context.GanjoorPoemMusicTracks
                                                .Where
                                                (
                                                    t => t.PoemId == id
                                                    &&
                                                    t.Approved == approved
                                                    &&
                                                    t.Rejected == false
                                                    &&
                                                    (trackType == PoemMusicTrackType.All || t.TrackType == trackType)
                                                )
                                                .OrderBy(t => t.SongOrder)
                                                .Select
                                                (
                                                 t => new PoemMusicTrackViewModel()
                                                 {
                                                     Id = t.Id,
                                                     PoemId = t.PoemId,
                                                     TrackType = t.TrackType,
                                                     ArtistName = t.ArtistName,
                                                     ArtistUrl = t.ArtistUrl,
                                                     AlbumName = t.AlbumName,
                                                     AlbumUrl = t.AlbumUrl,
                                                     TrackName = t.TrackName,
                                                     TrackUrl = t.TrackUrl,
                                                     Description = t.Description,
                                                     BrokenLink = t.BrokenLink,
                                                     GolhaTrackId = t.GolhaTrackId == null ? 0 : (int)t.GolhaTrackId,
                                                     Approved = t.Approved,
                                                     Rejected = t.Rejected,
                                                     RejectionCause = t.RejectionCause

                                                 }
                                                ).AsNoTracking().ToArrayAsync()
                );
        }

        /// <summary>
        /// user suggested songs
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PoemMusicTrackViewModel[] Items)>> GetUserSuggestedSongs(Guid userId, PagingParameterModel paging)
        {
            var source = from track in
                             _context.GanjoorPoemMusicTracks
                         where track.SuggestedById == userId
                         orderby track.SongOrder
                         select
                            new PoemMusicTrackViewModel()
                            {
                                Id = track.Id,
                                PoemId = track.PoemId,
                                TrackType = track.TrackType,
                                ArtistName = track.ArtistName,
                                ArtistUrl = track.ArtistUrl,
                                AlbumName = track.AlbumName,
                                AlbumUrl = track.AlbumUrl,
                                TrackName = track.TrackName,
                                TrackUrl = track.TrackUrl,
                                Description = track.Description,
                                BrokenLink = track.BrokenLink,
                                GolhaTrackId = track.GolhaTrackId == null ? 0 : (int)track.GolhaTrackId,
                                Approved = track.Approved,
                                Rejected = track.Rejected,
                                RejectionCause = track.RejectionCause

                            };
            return new RServiceResult<(PaginationMetadata, PoemMusicTrackViewModel[])>
                (await QueryablePaginator<PoemMusicTrackViewModel>.Paginate(source, paging));
        }


        /// <summary>
        /// suggest song
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> SuggestSong(Guid userId, PoemMusicTrackViewModel song)
        {
            song.Approved = false;
            song.Rejected = false;
            song.RejectionCause = "";
            song.BrokenLink = false;
            if (song.TrackType == PoemMusicTrackType.Golha)
            {
                var golhaTrack = await _context.GolhaTracks.Include(g => g.GolhaProgram).ThenInclude(p => p.GolhaCollection).Where(g => g.Id == song.GolhaTrackId).FirstOrDefaultAsync();
                if (golhaTrack == null)
                {
                    return new RServiceResult<PoemMusicTrackViewModel>(null, "مشخصات قطعهٔ گلها درست نیست.");
                }
                var alreadySuggestedSong = await _context.GanjoorPoemMusicTracks.AsNoTracking().Where(t => t.PoemId == song.PoemId && t.TrackType == song.TrackType && t.GolhaTrackId == song.GolhaTrackId && (t.Approved || (!t.Approved && !t.Rejected))).FirstOrDefaultAsync();
                if (alreadySuggestedSong != null)
                {
                    return new RServiceResult<PoemMusicTrackViewModel>(null, "این آهنگ پیشتر برای این شعر پیشنهاد داده شده است.");
                }

                song.ArtistName = "";
                song.ArtistUrl = "";
                song.AlbumName = $"{golhaTrack.GolhaProgram.GolhaCollection.Name} » شمارهٔ {golhaTrack.GolhaProgram.Title.ToPersianNumbers().ApplyCorrectYeKe()}";
                song.AlbumUrl = "";
                song.TrackName = $"{golhaTrack.Timing.ToPersianNumbers().ApplyCorrectYeKe()} {golhaTrack.Title}";
                song.TrackUrl = golhaTrack.GolhaProgram.Url;
            }
            else
            {
                var alreadySuggestedSong = await _context.GanjoorPoemMusicTracks.AsNoTracking().Where(t => t.PoemId == song.PoemId && t.TrackType == song.TrackType && (t.TrackUrl == song.TrackUrl || t.TrackUrl == song.TrackUrl.Replace("https", "http")) && (t.Approved || (!t.Approved && !t.Rejected))).FirstOrDefaultAsync();
                if (alreadySuggestedSong != null)
                {
                    return new RServiceResult<PoemMusicTrackViewModel>(null, "این آهنگ پیشتر برای این شعر پیشنهاد داده شده است.");
                }

            }
            var sug =
                new PoemMusicTrack()
                {
                    TrackType = song.TrackType,
                    PoemId = song.PoemId,
                    ArtistName = song.ArtistName,
                    ArtistUrl = song.ArtistUrl,
                    AlbumName = song.AlbumName,
                    AlbumUrl = song.AlbumUrl,
                    TrackName = song.TrackName,
                    TrackUrl = song.TrackUrl,
                    SuggestedById = userId,
                    Description = song.Description,
                    GolhaTrackId = song.TrackType == PoemMusicTrackType.Golha ? song.GolhaTrackId : (int?)null,
                    Approved = false,
                    Rejected = false,
                    RejectionCause = ""
                };

            GanjoorSinger singer = await _context.GanjoorSingers.Where(s => s.Url == song.ArtistUrl).FirstOrDefaultAsync();
            if (singer != null)
            {
                sug.SingerId = singer.Id;
            }

            _context.GanjoorPoemMusicTracks.Add
                (
                sug
                );

            await _context.SaveChangesAsync();
            sug.SongOrder = sug.Id;
            _context.GanjoorPoemMusicTracks.Update(sug);
            await _context.SaveChangesAsync();
            song.Id = sug.Id;

            var moderators = await _appUserService.GetUsersHavingPermission(RMuseumSecurableItem.GanjoorEntityShortName, RMuseumSecurableItem.ReviewSongs);
            if (string.IsNullOrEmpty(moderators.ExceptionString)) //if not, do nothing!
            {
                foreach (var moderator in moderators.Result)
                {
                    await _notificationService.PushNotification
                                    (
                                        (Guid)moderator.Id,
                                        "پیشنهاد آهنگ",
                                        $"کاربری آهنگ مرتبط جدیدی را برای یک شعر پیشنهاد داده است. لطفاً بخش <a href=\"/User/ReviewSongs\">آهنگ‌های پیشنهادی</a> را بررسی فرمایید.{Environment.NewLine}" +
                                        $"توجه فرمایید که اگر کاربر دیگری که دارای مجوز بررسی آهنگ‌های پیشنهادی است پیش از شما به آن رسیدگی کرده باشد آن را در صف نخواهید دید."
                                    );
                }
            }

            return new RServiceResult<PoemMusicTrackViewModel>(song);
        }

        /// <summary>
        /// get unreviewed count
        /// </summary>
        /// <param name="suggestedById"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreviewedSongsCount(Guid suggestedById)
        {
            return new RServiceResult<int>(await _context.GanjoorPoemMusicTracks
               .Where(p => p.Approved == false && p.Rejected == false && (suggestedById == Guid.Empty || p.SuggestedById == suggestedById))
               .CountAsync());
        }


        /// <summary>
        /// next unreviewed track
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="suggestedById"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> GetNextUnreviewedSong(int skip, Guid suggestedById)
        {
            var song = await _context.GanjoorPoemMusicTracks.Include(song => song.SuggestedBy)
                .Where(p => p.Approved == false && p.Rejected == false && (suggestedById == Guid.Empty || p.SuggestedById == suggestedById))
                .OrderBy(p => p.Id).Skip(skip).AsNoTracking().FirstOrDefaultAsync();
            if (song != null)
            {
                return new RServiceResult<PoemMusicTrackViewModel>
                    (
                    new PoemMusicTrackViewModel()
                    {
                        Id = song.Id,
                        TrackType = song.TrackType,
                        PoemId = song.PoemId,
                        ArtistName = song.ArtistName,
                        ArtistUrl = song.ArtistUrl,
                        AlbumName = song.AlbumName,
                        AlbumUrl = song.AlbumUrl,
                        TrackName = song.TrackName,
                        TrackUrl = song.TrackUrl,
                        Description = song.Description,
                        GolhaTrackId = song.TrackType == PoemMusicTrackType.Golha ? (int)song.GolhaTrackId : 0,
                        BrokenLink = song.BrokenLink,
                        Approved = song.Approved,
                        Rejected = song.Rejected,
                        RejectionCause = song.RejectionCause,
                        SuggestedById = song.SuggestedById,
                        SuggestedByNickName = string.IsNullOrEmpty(song.SuggestedBy.NickName) ? song.SuggestedBy.Id.ToString() : song.SuggestedBy.NickName
                    }
                    );
            }
            return new RServiceResult<PoemMusicTrackViewModel>(null); //not found
        }

        /// <summary>
        /// get track of user song suggestions
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserSongSuggestionsHistory>> GetUserSongsSuggestionsStatistics(Guid userId)
        {
            return new RServiceResult<UserSongSuggestionsHistory>
                (
                new UserSongSuggestionsHistory()
                {
                    Approved = await _context.GanjoorPoemMusicTracks.Where(p => p.SuggestedById == userId && p.Approved == true).CountAsync(),
                    Rejected = await _context.GanjoorPoemMusicTracks.Where(p => p.SuggestedById == userId && p.Rejected == true).CountAsync()
                }
                );
        }

        /// <summary>
        /// review song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> ReviewSong(PoemMusicTrackViewModel song)
        {
            if (song.Approved && song.Rejected)
                return new RServiceResult<PoemMusicTrackViewModel>(null, "song.Approved && song.Rejected");

            if (song.Approved)
            {
                if (song.TrackType == PoemMusicTrackType.Golha)
                {
                    var alreadySuggestedSong = await _context.GanjoorPoemMusicTracks.AsNoTracking().Where(t => t.PoemId == song.PoemId && t.TrackType == song.TrackType && t.GolhaTrackId == song.GolhaTrackId && t.Approved).FirstOrDefaultAsync();
                    if (alreadySuggestedSong != null)
                    {
                        return new RServiceResult<PoemMusicTrackViewModel>(null, "این آهنگ پیشتر برای این شعر تأیید شده است.");
                    }
                }
                else
                {
                    var alreadySuggestedSong = await _context.GanjoorPoemMusicTracks.AsNoTracking().Where(t => t.PoemId == song.PoemId && t.TrackType == song.TrackType && (t.TrackUrl == song.TrackUrl || t.TrackUrl == song.TrackUrl.Replace("https", "http")) && t.Approved).FirstOrDefaultAsync();
                    if (alreadySuggestedSong != null)
                    {
                        return new RServiceResult<PoemMusicTrackViewModel>(null, "این آهنگ پیشتر برای این شعر تأیید شده است.");
                    }
                }
            }


            var track = await _context.GanjoorPoemMusicTracks.Where(t => t.Id == song.Id).SingleOrDefaultAsync();

            track.TrackType = song.TrackType;
            track.ArtistName = song.ArtistName;
            track.ArtistUrl = song.ArtistUrl;
            track.AlbumName = song.AlbumName;
            track.AlbumUrl = song.AlbumUrl;
            track.TrackName = song.TrackName;
            track.TrackUrl = song.TrackUrl;
            if (!track.Approved && song.Approved)
            {
                track.ApprovalDate = DateTime.Now;
            }
            track.Approved = song.Approved;
            track.Rejected = song.Rejected;
            track.RejectionCause = song.RejectionCause;
            track.BrokenLink = song.BrokenLink;
            if (track.TrackType == PoemMusicTrackType.Golha)
            {
                track.GolhaTrackId = song.GolhaTrackId;
            }


            GanjoorSinger singer = await _context.GanjoorSingers.AsNoTracking().Where(s => s.Url == track.ArtistUrl).FirstOrDefaultAsync();
            if (singer != null)
            {
                track.SingerId = singer.Id;
            }

            _context.GanjoorPoemMusicTracks.Update(track);

            await _context.SaveChangesAsync();

            if (track.Approved)
            {
                await CacheCleanForPageById(track.PoemId);
            }

            var poem = await _context.GanjoorPoems.AsNoTracking().Where(p => p.Id == track.PoemId).SingleAsync();

            if (track.Approved)
            {
                await _notificationService.PushNotification(
                    (Guid)track.SuggestedById,
                                  "تأیید آهنگ پیشنهادی",
                                  $"آهنگ پیشنهادی شما («{track.TrackName}» برای «<a href='{poem.FullUrl}'>{poem.FullTitle}</a>») تأیید شد.  {Environment.NewLine}" +
                                  $"از این که به تکمیل اطلاعات گنجور کمک کردید سپاسگزاریم."
                                  );
            }
            else if (track.Rejected)
            {
                await _notificationService.PushNotification(
                    (Guid)track.SuggestedById,
                                  "رد آهنگ پیشنهادی",
                                  $"آهنگ پیشنهادی شما («{track.TrackName}» برای «<a href='{poem.FullUrl}'>{poem.FullTitle}</a>») تأیید نشد. {Environment.NewLine}" +
                                  $"علت عدم تأیید: {Environment.NewLine}" +
                                  $"«{track.RejectionCause}» {Environment.NewLine}" +
                                  $"توجه کنید که در پیشنهاد آهنگ می‌بایست دقیقا قطعه‌ای را مشخص کنید که شعر در آن خوانده شده و پیشنهاد خواننده یا آلبوم یا برنامهٔ گلها به طور کلی فایده‌ای ندارد.{Environment.NewLine}" +
                                  $"اگر تصور می‌کنید اشتباهی رخ داده لطفا مجددا آهنگ را پیشنهاد دهید و در بخش توضیحات دلیل خود را بنویسید.{Environment.NewLine}با سپاس"
                                  );
            }


            return new RServiceResult<PoemMusicTrackViewModel>(song);
        }

        /// <summary>
        /// direct insert song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> DirectInsertSong(PoemMusicTrackViewModel song)
        {
            var poem = await _context.GanjoorPoems.Where(p => p.Id == song.PoemId).SingleOrDefaultAsync();
            if (poem == null)
                return new RServiceResult<PoemMusicTrackViewModel>(null, "poem == null");

            if
                (
                string.IsNullOrEmpty(song.ArtistName)
                ||
                string.IsNullOrEmpty(song.ArtistUrl)
                ||
                string.IsNullOrEmpty(song.AlbumName)
                ||
                string.IsNullOrEmpty(song.AlbumUrl)
                ||
                string.IsNullOrEmpty(song.TrackName)
                ||
                string.IsNullOrEmpty(song.TrackUrl)
                ||
                song.TrackType != PoemMusicTrackType.BeepTunesOrKhosousi
                )
            {
                return new RServiceResult<PoemMusicTrackViewModel>(null, "data validation err");
            }

            var duplicated = await _context.GanjoorPoemMusicTracks.Where(m => m.PoemId == song.PoemId && m.TrackUrl == song.TrackUrl).FirstOrDefaultAsync();
            if (duplicated != null)
            {
                return new RServiceResult<PoemMusicTrackViewModel>(null, "duplicated song url for this poem");
            }


            PoemMusicTrack track = new PoemMusicTrack();

            track.PoemId = song.PoemId;
            track.TrackType = song.TrackType;
            track.ArtistName = song.ArtistName;
            track.ArtistUrl = song.ArtistUrl;
            track.AlbumName = song.AlbumName;
            track.AlbumUrl = song.AlbumUrl;
            track.TrackName = song.TrackName;
            track.TrackUrl = song.TrackUrl;
            track.ApprovalDate = DateTime.Now;
            track.Approved = true;
            track.Rejected = false;
            track.BrokenLink = song.BrokenLink;

            GanjoorSinger singer = await _context.GanjoorSingers.Where(s => s.Url == track.ArtistUrl).FirstOrDefaultAsync();
            if (singer != null)
            {
                track.SingerId = singer.Id;
            }

            _context.GanjoorPoemMusicTracks.Add(track);

            await _context.SaveChangesAsync();

            track.SongOrder = track.Id;
            song.Id = track.Id;
            _context.GanjoorPoemMusicTracks.Update(track);
            await _context.SaveChangesAsync();

            await CacheCleanForPageById(track.PoemId);

            return new RServiceResult<PoemMusicTrackViewModel>(song);
        }

        /// <summary>
        /// get song by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        public async Task<RServiceResult<PoemMusicTrackViewModel>> GetPoemSongById(int id)
        {
            return new RServiceResult<PoemMusicTrackViewModel>
                (
                await _context.GanjoorPoemMusicTracks
                                                .Where
                                                (
                                                   t => t.Id == id
                                                )
                                                .OrderBy(t => t.SongOrder)
                                                .Select
                                                (
                                                 t => new PoemMusicTrackViewModel()
                                                 {
                                                     Id = t.Id,
                                                     PoemId = t.PoemId,
                                                     TrackType = t.TrackType,
                                                     ArtistName = t.ArtistName,
                                                     ArtistUrl = t.ArtistUrl,
                                                     AlbumName = t.AlbumName,
                                                     AlbumUrl = t.AlbumUrl,
                                                     TrackName = t.TrackName,
                                                     TrackUrl = t.TrackUrl,
                                                     Description = t.Description,
                                                     BrokenLink = t.BrokenLink,
                                                     GolhaTrackId = t.GolhaTrackId == null ? 0 : (int)t.GolhaTrackId,
                                                     Approved = t.Approved,
                                                     Rejected = t.Rejected,
                                                     RejectionCause = t.RejectionCause

                                                 }
                                                ).AsNoTracking().SingleOrDefaultAsync()
                );
        }

        /// <summary>
        /// modify a published song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemMusicTrackViewModel>> ModifyPublishedSong(PoemMusicTrackViewModel song)
        {
            if (!song.Approved)
                return new RServiceResult<PoemMusicTrackViewModel>(null, "!song.Approved ");


            var track = await _context.GanjoorPoemMusicTracks.Where(t => t.Id == song.Id).SingleOrDefaultAsync();

            track.TrackType = song.TrackType;
            track.ArtistName = song.ArtistName;
            track.ArtistUrl = song.ArtistUrl;
            track.AlbumName = song.AlbumName;
            track.AlbumUrl = song.AlbumUrl;
            track.TrackName = song.TrackName;
            track.TrackUrl = song.TrackUrl;
            track.BrokenLink = song.BrokenLink;

            GanjoorSinger singer = await _context.GanjoorSingers.AsNoTracking().Where(s => s.Url == track.ArtistUrl).FirstOrDefaultAsync();
            if (singer != null)
            {
                track.SingerId = singer.Id;
            }

            _context.GanjoorPoemMusicTracks.Update(track);

            await _context.SaveChangesAsync();

            await CacheCleanForPageById(track.PoemId);

            return new RServiceResult<PoemMusicTrackViewModel>(song);
        }

        /// <summary>
        /// delete a poem song by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeletePoemSongById(int id)
        {
            try
            {
                var song = await _context.GanjoorPoemMusicTracks.Where(t => t.Id == id).SingleAsync();
                _context.Remove(song);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }



    }
}
