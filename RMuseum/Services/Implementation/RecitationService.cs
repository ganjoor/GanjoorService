using DNTPersianUtils.Core;
using FluentFTP;
using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
using RMuseum.Models.UploadSession.ViewModels;
using RMuseum.Services.Implementation;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMuseum.Services.Implementationa
{

    /// <summary>
    /// Audio Narration Service Implementation
    /// </summary>
    public class RecitationService : IRecitationService
    {
        /// <summary>
        /// returns list of narrations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filteredUserId">send Guid.Empty if you want all narrations</param>
        /// <param name="status"></param>
        /// <param name="searchTerm"></param>
        /// <param name="mistakes"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RecitationViewModel[] Items)>> SecureGetAll(PagingParameterModel paging, Guid filteredUserId, AudioReviewStatus status, string searchTerm, bool mistakes)
        {
            if (!mistakes)
            {
                //whenever I had not a reference to audio.Owner in the final selection it became null, so this strange arrangement is not all because of my stupidity!
                var source =
                     from audio in _context.Recitations.AsNoTracking().Include(a => a.Owner)
                     join poem in _context.GanjoorPoems
                     on audio.GanjoorPostId equals poem.Id
                     where
                     (filteredUserId == Guid.Empty || audio.OwnerId == filteredUserId)
                     &&
                     (status == AudioReviewStatus.All || audio.ReviewStatus == status)
                     &&
                     (string.IsNullOrEmpty(searchTerm) ||
                     (!string.IsNullOrEmpty(searchTerm) && (audio.AudioArtist.Contains(searchTerm) || audio.AudioTitle.Contains(searchTerm) || poem.FullTitle.Contains(searchTerm)))
                     )
                     orderby audio.UploadDate descending
                     select new RecitationViewModel(audio, audio.Owner, poem, "");

                (PaginationMetadata PagingMeta, RecitationViewModel[] Items) paginatedResult =
                    await QueryablePaginator<RecitationViewModel>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, RecitationViewModel[] Items)>(paginatedResult);
            }
            else
            {
                //whenever I had not a reference to audio.Owner in the final selection it became null, so this strange arrangement is not all because of my stupidity!
                var source =
                     from mistake in _context.RecitationApprovedMistakes.AsNoTracking()
                     join audio in _context.Recitations.Include(a => a.Owner)
                     on mistake.RecitationId equals audio.Id
                     join poem in _context.GanjoorPoems
                     on audio.GanjoorPostId equals poem.Id
                     where
                     (filteredUserId == Guid.Empty || audio.OwnerId == filteredUserId)
                     &&
                     (status == AudioReviewStatus.All || audio.ReviewStatus == status)
                     &&
                     (string.IsNullOrEmpty(searchTerm) ||
                     (!string.IsNullOrEmpty(searchTerm) && (audio.AudioArtist.Contains(searchTerm) || audio.AudioTitle.Contains(searchTerm) || poem.FullTitle.Contains(searchTerm)))
                     )
                     orderby audio.UploadDate descending
                     select new RecitationViewModel(audio, audio.Owner, poem, mistake.Mistake);

                (PaginationMetadata PagingMeta, RecitationViewModel[] Items) paginatedResult =
                    await QueryablePaginator<RecitationViewModel>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, RecitationViewModel[] Items)>(paginatedResult);
            }

        }

        /// <summary>
        /// returns list of publish recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="searchTerm"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>> GetPublishedRecitations(PagingParameterModel paging, string searchTerm, int poetId, int catId)
        {
            if (poetId == 0 && catId == 0)
            {
                var source =
                                 from audio in _context.Recitations.AsNoTracking()
                                 join poem in _context.GanjoorPoems
                                 on audio.GanjoorPostId equals poem.Id
                                 where
                                 audio.ReviewStatus == AudioReviewStatus.Approved
                                 &&
                                 (
                                 string.IsNullOrEmpty(searchTerm) ||
                                 (
                                 !string.IsNullOrEmpty(searchTerm)
                                 &&
                                 (
                                 audio.AudioArtist.Contains(searchTerm)
                                 ||
                                 audio.AudioTitle.Contains(searchTerm)
                                 ||
                                 poem.FullTitle.Contains(searchTerm)
                                 ||
                                 poem.PlainText.Contains(searchTerm)
                                 ))
                                 )
                                 orderby audio.ReviewDate descending
                                 select new PublicRecitationViewModel()
                                 {
                                     Id = audio.Id,
                                     PoemId = audio.GanjoorPostId,
                                     PoemFullTitle = poem.FullTitle,
                                     PoemFullUrl = poem.FullUrl,
                                     AudioTitle = audio.AudioTitle,
                                     AudioArtist = audio.AudioArtist,
                                     AudioArtistUrl = audio.AudioArtistUrl,
                                     AudioSrc = audio.AudioSrc,
                                     AudioSrcUrl = audio.AudioSrcUrl,
                                     LegacyAudioGuid = audio.LegacyAudioGuid,
                                     Mp3FileCheckSum = audio.Mp3FileCheckSum,
                                     Mp3SizeInBytes = audio.Mp3SizeInBytes,
                                     PublishDate = audio.ReviewDate,
                                     FileLastUpdated = audio.FileLastUpdated,
                                     Mp3Url = $"{WebServiceUrl.Url}/api/audio/file/{audio.Id}.mp3",
                                     XmlText = $"{WebServiceUrl.Url}/api/audio/xml/{audio.Id}",
                                     PlainText = poem.PlainText,
                                     HtmlText = poem.HtmlText,
                                     AudioOrder = audio.AudioOrder,
                                     UpVotedByUser = false,
                                 };

                (PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items) paginatedResult =
                    await QueryablePaginator<PublicRecitationViewModel>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>(paginatedResult);
            }
            else
            if (catId != 0)
            {
                var source =
                                 from audio in _context.Recitations.AsNoTracking()
                                 join poem in _context.GanjoorPoems
                                 on audio.GanjoorPostId equals poem.Id
                                 where
                                 poem.CatId == catId
                                 &&
                                 audio.ReviewStatus == AudioReviewStatus.Approved
                                 &&
                                 (
                                 string.IsNullOrEmpty(searchTerm) ||
                                 (
                                 !string.IsNullOrEmpty(searchTerm)
                                 &&
                                 (
                                 audio.AudioArtist.Contains(searchTerm)
                                 ||
                                 audio.AudioTitle.Contains(searchTerm)
                                 ||
                                 poem.FullTitle.Contains(searchTerm)
                                 ||
                                 poem.PlainText.Contains(searchTerm)
                                 ))
                                 )
                                 orderby poem.Id, audio.ReviewDate
                                 select new PublicRecitationViewModel()
                                 {
                                     Id = audio.Id,
                                     PoemId = audio.GanjoorPostId,
                                     PoemFullTitle = poem.FullTitle,
                                     PoemFullUrl = poem.FullUrl,
                                     AudioTitle = audio.AudioTitle,
                                     AudioArtist = audio.AudioArtist,
                                     AudioArtistUrl = audio.AudioArtistUrl,
                                     AudioSrc = audio.AudioSrc,
                                     AudioSrcUrl = audio.AudioSrcUrl,
                                     LegacyAudioGuid = audio.LegacyAudioGuid,
                                     Mp3FileCheckSum = audio.Mp3FileCheckSum,
                                     Mp3SizeInBytes = audio.Mp3SizeInBytes,
                                     PublishDate = audio.ReviewDate,
                                     FileLastUpdated = audio.FileLastUpdated,
                                     Mp3Url = $"{WebServiceUrl.Url}/api/audio/file/{audio.Id}.mp3",
                                     XmlText = $"{WebServiceUrl.Url}/api/audio/xml/{audio.Id}",
                                     PlainText = poem.PlainText,
                                     HtmlText = poem.HtmlText,
                                     AudioOrder = audio.AudioOrder,
                                     UpVotedByUser = false,
                                 };

                (PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items) paginatedResult =
                    await QueryablePaginator<PublicRecitationViewModel>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>(paginatedResult);
            }
            else //poetId != 0
            {
                var source =
                                 from audio in _context.Recitations.AsNoTracking()
                                 join poem in _context.GanjoorPoems.Include(p => p.Cat)
                                 on audio.GanjoorPostId equals poem.Id
                                 where
                                 poem.Cat.PoetId == poetId
                                 &&
                                 audio.ReviewStatus == AudioReviewStatus.Approved
                                 &&
                                 (
                                 string.IsNullOrEmpty(searchTerm) ||
                                 (
                                 !string.IsNullOrEmpty(searchTerm)
                                 &&
                                 (
                                 audio.AudioArtist.Contains(searchTerm)
                                 ||
                                 audio.AudioTitle.Contains(searchTerm)
                                 ||
                                 poem.FullTitle.Contains(searchTerm)
                                 ||
                                 poem.PlainText.Contains(searchTerm)
                                 ))
                                 )
                                 orderby poem.Id, audio.ReviewDate
                                 select new PublicRecitationViewModel()
                                 {
                                     Id = audio.Id,
                                     PoemId = audio.GanjoorPostId,
                                     PoemFullTitle = poem.FullTitle,
                                     PoemFullUrl = poem.FullUrl,
                                     AudioTitle = audio.AudioTitle,
                                     AudioArtist = audio.AudioArtist,
                                     AudioArtistUrl = audio.AudioArtistUrl,
                                     AudioSrc = audio.AudioSrc,
                                     AudioSrcUrl = audio.AudioSrcUrl,
                                     LegacyAudioGuid = audio.LegacyAudioGuid,
                                     Mp3FileCheckSum = audio.Mp3FileCheckSum,
                                     Mp3SizeInBytes = audio.Mp3SizeInBytes,
                                     PublishDate = audio.ReviewDate,
                                     FileLastUpdated = audio.FileLastUpdated,
                                     Mp3Url = $"{WebServiceUrl.Url}/api/audio/file/{audio.Id}.mp3",
                                     XmlText = $"{WebServiceUrl.Url}/api/audio/xml/{audio.Id}",
                                     PlainText = poem.PlainText,
                                     HtmlText = poem.HtmlText,
                                     AudioOrder = audio.AudioOrder,
                                     UpVotedByUser = false,
                                 };

                (PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items) paginatedResult =
                    await QueryablePaginator<PublicRecitationViewModel>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>(paginatedResult);
            }
        }

        /// <summary>
        /// get category top one recitations
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="includePoemText"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PublicRecitationViewModel[]>> GetPoemCategoryTopRecitations(int catId, bool includePoemText)
        {
            try
            {
                var source = from poem in _context.GanjoorPoems.AsNoTracking()
                             from audio in _context.Recitations
                                               .Where(a => a.GanjoorPostId == poem.Id && a.ReviewStatus == AudioReviewStatus.Approved)
                                               .OrderBy(a => a.AudioOrder)
                                               .Take(1)
                                               .DefaultIfEmpty()
                             where
                             poem.CatId == catId && audio != null
                             orderby poem.Id
                             select new PublicRecitationViewModel()
                             {
                                 Id = audio.Id,
                                 PoemId = audio.GanjoorPostId,
                                 PoemFullTitle = poem.FullTitle,
                                 PoemFullUrl = poem.FullUrl,
                                 AudioTitle = audio.AudioTitle,
                                 AudioArtist = audio.AudioArtist,
                                 AudioArtistUrl = audio.AudioArtistUrl,
                                 AudioSrc = audio.AudioSrc,
                                 AudioSrcUrl = audio.AudioSrcUrl,
                                 LegacyAudioGuid = audio.LegacyAudioGuid,
                                 Mp3FileCheckSum = audio.Mp3FileCheckSum,
                                 Mp3SizeInBytes = audio.Mp3SizeInBytes,
                                 PublishDate = audio.ReviewDate,
                                 FileLastUpdated = audio.FileLastUpdated,
                                 Mp3Url = $"{WebServiceUrl.Url}/api/audio/file/{audio.Id}.mp3",
                                 XmlText = $"{WebServiceUrl.Url}/api/audio/xml/{audio.Id}",
                                 PlainText = includePoemText ? poem.PlainText : "",
                                 HtmlText = includePoemText ? poem.HtmlText : "",
                                 AudioOrder = audio.AudioOrder,
                                 UpVotedByUser = false,
                             };

                return new RServiceResult<PublicRecitationViewModel[]>(await source.ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PublicRecitationViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// check if a category has any recitations
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> GetPoemCategoryHasAnyRecitations(int catId)
        {
            try
            {
                return new RServiceResult<bool>(
                    await _context.Recitations.AsNoTracking().Where(
                    a =>
                    _context.GanjoorPoems.Where(p => p.CatId == catId && p.Id == a.GanjoorPostId
                    &&
                    a.ReviewStatus == AudioReviewStatus.Approved
                    ).Any()
                    ).AnyAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// get published recitation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PublicRecitationViewModel>> GetPublishedRecitationById(int id)
        {
            var source =
                 from audio in _context.Recitations.AsNoTracking()
                 join poem in _context.GanjoorPoems
                 on audio.GanjoorPostId equals poem.Id
                 where
                 audio.ReviewStatus == AudioReviewStatus.Approved && audio.Id == id
                 select new PublicRecitationViewModel()
                 {
                     Id = audio.Id,
                     PoemId = audio.GanjoorPostId,
                     PoemFullTitle = poem.FullTitle,
                     PoemFullUrl = poem.FullUrl,
                     AudioTitle = audio.AudioTitle,
                     AudioArtist = audio.AudioArtist,
                     AudioArtistUrl = audio.AudioArtistUrl,
                     AudioSrc = audio.AudioSrc,
                     AudioSrcUrl = audio.AudioSrcUrl,
                     LegacyAudioGuid = audio.LegacyAudioGuid,
                     Mp3FileCheckSum = audio.Mp3FileCheckSum,
                     Mp3SizeInBytes = audio.Mp3SizeInBytes,
                     PublishDate = audio.ReviewDate,
                     FileLastUpdated = audio.FileLastUpdated,
                     Mp3Url = $"{WebServiceUrl.Url}/api/audio/file/{audio.Id}.mp3",
                     XmlText = $"{WebServiceUrl.Url}/api/audio/xml/{audio.Id}",
                     PlainText = poem.PlainText,
                     HtmlText = poem.HtmlText,
                     AudioOrder = audio.AudioOrder,
                     UpVotedByUser = false,
                 };

            return new RServiceResult<PublicRecitationViewModel>(await source.SingleOrDefaultAsync());
        }

        /// <summary>
        /// return selected narration information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationViewModel>> Get(int id)
        {
            var cachKey = $"RecitationService::Get::{id}";
            if (!_memoryCache.TryGetValue(cachKey, out RecitationViewModel narration))
            {
                //whenever I had not a reference to audio.Owner in the final selection it became null, so this strange arrangement is not all because of my stupidity!
                var source =
                     from audio in _context.Recitations.AsNoTracking()
                     .Include(a => a.Owner)
                     .Where(a => a.Id == id)
                     join poem in _context.GanjoorPoems
                     on audio.GanjoorPostId equals poem.Id
                     select new RecitationViewModel(audio, audio.Owner, poem, "");

                narration = await source.SingleOrDefaultAsync();
                _memoryCache.Set(cachKey, narration);
            }

            return new RServiceResult<RecitationViewModel>(narration);
        }

        /// <summary>
        /// Delete recitation
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> Delete(int id, Guid userId)
        {
            Recitation recitation = await _context.Recitations.Where(a => a.Id == id && a.OwnerId == userId).FirstOrDefaultAsync();
            if (recitation == null)
            {
                return new RServiceResult<bool>(false, "404");
            }

            if (recitation.ReviewStatus == AudioReviewStatus.Approved)
            {

                int GanjoorPostId = recitation.GanjoorPostId;

                recitation.AudioSyncStatus = AudioSyncStatus.Deleted;
                _context.Recitations.Update(recitation);
                await _context.SaveChangesAsync();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                  (
                  async token =>
                  {
                      using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                      {
                          RecitationPublishingTracker tracker = new RecitationPublishingTracker()
                          {
                              PoemNarrationId = recitation.Id,
                              StartDate = DateTime.Now,
                              XmlFileCopied = false,
                              Mp3FileCopied = false,
                              FirstDbUpdated = false,
                              SecondDbUpdated = false,
                          };
                          context.RecitationPublishingTrackers.Add(tracker);
                          await context.SaveChangesAsync();

                          await _DeleteNarrationFromRemote(recitation, tracker, context);
                      }

                  });

                await _ganjoorService.CacheCleanForPageById(GanjoorPostId);
            }
            else
            {
                await _FinalizeDelete(_context, recitation);
            }

            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// Gets Verse Sync Range Information
        /// </summary>
        /// <param name="id">narration id</param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationVerseSync[]>> GetPoemNarrationVerseSyncArray(int id)
        {
            var narration = await _context.Recitations.AsNoTracking().Where(a => a.Id == id).SingleOrDefaultAsync();
            var verses = await _context.GanjoorVerses.AsNoTracking().Where(v => v.PoemId == narration.GanjoorPostId).OrderBy(v => v.VOrder).ToListAsync();

            string xml = File.ReadAllText(narration.LocalXmlFilePath);

            List<RecitationVerseSync> verseSyncs = new List<RecitationVerseSync>();

            XElement elObject = XDocument.Parse(xml).Root;
            float oneSecond = 1;
            if (elObject.Element("PoemAudio").Elements("OneSecondBugFix").Count() == 0)
            {
                oneSecond = 0.5f;
            }
            verseSyncs.Add(new RecitationVerseSync()
            {
                VerseOrder = 0,
                VerseText = (await _context.GanjoorPoems.Where(p => p.Id == narration.GanjoorPostId).FirstOrDefaultAsync()).Title,
                AudioStartMilliseconds = 0
            });
            foreach (var syncInfo in elObject.Element("PoemAudio").Element("SyncArray").Elements("SyncInfo"))
            {
                int verseOrder = int.Parse(syncInfo.Element("VerseOrder").Value);
                if (verseOrder < 0) //this happens, seems to be a bug, I did not trace it yet
                    continue;
                verseOrder++;
                var verse = verses.Where(v => v.VOrder == verseOrder).SingleOrDefault();
                if (verse != null)
                {
                    verseSyncs.Add(new RecitationVerseSync()
                    {
                        VerseOrder = verseOrder,
                        VerseText = verse.Text,
                        AudioStartMilliseconds = (int)(oneSecond * int.Parse(syncInfo.Element("AudioMiliseconds").Value))
                    });
                }

            }

            verseSyncs.Sort((a, b) => a.AudioStartMilliseconds.CompareTo(b.AudioStartMilliseconds));

            return new RServiceResult<RecitationVerseSync[]>(verseSyncs.ToArray());
        }

        /// <summary>
        /// validate PoemNarrationViewModel
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static string GetPoemNarrationValidationError(RecitationViewModel p)
        {
            if (p.AudioArtist.Length < 3)
            {
                return "نام خوانشگر باید حداقل شامل سه نویسه باشد.";
            }

            string s = LanguageUtils.GetFirstNotMatchingCharacter(p.AudioArtist, LanguageUtils.PersianAlphabet, " .‌");
            if (s != "")
            {
                return $"نام فقط باید شامل حروف فارسی و فاصله باشد. اولین حرف غیرمجاز = {s}";
            }

            if (!string.IsNullOrEmpty(p.AudioArtistUrl))
            {
                bool result = Uri.TryCreate(p.AudioArtistUrl, UriKind.Absolute, out Uri uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!result)
                {
                    return $"نشانی وب خوانشگر نامعتبر است.";
                }
            }

            if (!string.IsNullOrEmpty(p.AudioSrcUrl))
            {
                bool result = Uri.TryCreate(p.AudioSrcUrl, UriKind.Absolute, out Uri uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!result)
                {
                    return $"نشانی وب منبع نامعتبر است.";
                }
            }


            return "";
        }

        /// <summary>
        /// updates metadata for narration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationViewModel>> UpdatePoemNarration(int id, RecitationViewModel metadata)
        {
            metadata.AudioTitle = metadata.AudioTitle.Trim();
            metadata.AudioArtist = metadata.AudioArtist.Trim();
            metadata.AudioArtistUrl = metadata.AudioArtistUrl.Trim();
            metadata.AudioSrc = metadata.AudioSrc.Trim();
            metadata.AudioSrcUrl = metadata.AudioSrcUrl.Trim();

            string err = GetPoemNarrationValidationError(metadata);
            if (!string.IsNullOrEmpty(err))
            {
                return new RServiceResult<RecitationViewModel>(null, err);
            }

            Recitation narration = await _context.Recitations.Include(a => a.Owner).Where(a => a.Id == id).SingleOrDefaultAsync();
            if (narration == null)
                return new RServiceResult<RecitationViewModel>(null, "404");

            bool bNewPendingRequest = narration.ReviewStatus == AudioReviewStatus.Draft && metadata.ReviewStatus == AudioReviewStatus.Pending;

            narration.AudioTitle = metadata.AudioTitle;
            narration.AudioArtist = metadata.AudioArtist;
            narration.AudioArtistUrl = metadata.AudioArtistUrl;
            narration.AudioSrc = metadata.AudioSrc;
            narration.AudioSrcUrl = metadata.AudioSrcUrl;
            narration.ReviewStatus = metadata.ReviewStatus;
            _context.Recitations.Update(narration);
            await _context.SaveChangesAsync();

            if (bNewPendingRequest)
            {
                var moderators = await _userService.GetUsersHavingPermission(RMuseumSecurableItem.AudioRecitationEntityShortName, RMuseumSecurableItem.ModerateOperationShortName);
                if (string.IsNullOrEmpty(moderators.ExceptionString)) //if not, do nothing!
                {
                    foreach (var moderator in moderators.Result)
                    {
                        await _notificationService.PushNotification
                                        (
                                            (Guid)moderator.Id,
                                            "درخواست بررسی خوانش",
                                            $"درخواستی برای بررسی خوانشی از «{narration.AudioArtist}» ثبت شده است. در صورت تمایل به بررسی، بخش «خوانش‌های در انتظار تأیید» را ببینید.{Environment.NewLine}" +
                                            $"توجه فرمایید که اگر کاربر دیگری که دارای مجوز بررسی خوانش‌هاست پیش از شما به آن رسیدگی کرده باشد آن را در صف نخواهید دید."
                                        );
                    }
                }
            }


            if (narration.ReviewStatus == AudioReviewStatus.Approved)
            {
                narration.AudioSyncStatus = AudioSyncStatus.MetadataChanged;
                _context.Recitations.Update(narration);
                await _context.SaveChangesAsync();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                  (
                  async token =>
                  {
                      using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                      {
                          RecitationPublishingTracker tracker = new RecitationPublishingTracker()
                          {
                              PoemNarrationId = narration.Id,
                              StartDate = DateTime.Now,
                              XmlFileCopied = false,
                              Mp3FileCopied = false,
                              FirstDbUpdated = false,
                              SecondDbUpdated = false,
                          };
                          context.RecitationPublishingTrackers.Add(tracker);
                          await context.SaveChangesAsync();

                          await _UpdateRemoteRecitations(narration, tracker, context, true);
                      }

                  });

                await _ganjoorService.CacheCleanForPageById(narration.GanjoorPostId);

            }
            return new RServiceResult<RecitationViewModel>(new RecitationViewModel(narration, narration.Owner, await _context.GanjoorPoems.Where(p => p.Id == narration.GanjoorPostId).SingleOrDefaultAsync(), ""));
        }

        /// <summary>
        /// build profiles from exisng narrations data
        /// </summary>
        /// <param name="ownerRAppUserId">User Id which becomes owner of imported data</param>
        /// <returns>error string if occurs</returns>
        public async Task<string> BuildProfilesFromExistingData(Guid ownerRAppUserId)
        {
            List<UserRecitationProfile> profiles =
                 await _context.Recitations
                 .GroupBy(audio => new { audio.AudioArtist, audio.AudioArtistUrl, audio.AudioSrc, audio.AudioSrcUrl })
                 .OrderByDescending(g => g.Count())
                 .Select(g => new UserRecitationProfile()
                 {
                     UserId = ownerRAppUserId,
                     ArtistName = g.Key.AudioArtist,
                     ArtistUrl = g.Key.AudioArtistUrl,
                     AudioSrc = g.Key.AudioSrc,
                     AudioSrcUrl = g.Key.AudioSrcUrl,
                     IsDefault = false
                 }
                 ).ToListAsync();
            foreach (UserRecitationProfile profile in profiles)
            {
                Recitation narration =
                    await _context.Recitations.Where(audio =>
                                            audio.AudioArtist == profile.ArtistName
                                            &&
                                            audio.AudioArtistUrl == profile.ArtistUrl
                                            &&
                                            audio.AudioSrc == profile.AudioSrc
                                            &&
                                            audio.AudioSrcUrl == profile.AudioSrcUrl
                                            //&&
                                            //audio.FileNameWithoutExtension.Contains('-')
                                            ).FirstOrDefaultAsync();
                string ext = "";
                if (narration != null && narration.FileNameWithoutExtension.IndexOf('-') != -1)
                {
                    ext = narration.FileNameWithoutExtension[(narration.FileNameWithoutExtension.IndexOf('-') + 1)..];
                }
                if (ext.Length < 2)
                {
                    string[] parts = profile.ArtistName.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        ext = GPersianTextSync.Farglisize(profile.ArtistName).ToLower();
                        if (ext.Length > 3)
                            ext = ext.Substring(0, 3);
                    }
                    else
                    {
                        ext = "";
                        foreach (string part in parts)
                        {
                            string farglisi = GPersianTextSync.Farglisize(part).ToLower();
                            if (!string.IsNullOrEmpty(farglisi))
                                ext += farglisi[0];
                        }
                    }
                }
                profile.FileSuffixWithoutDash = ext;
                profile.Name = profile.ArtistName;
                int pIndex = 1;
                while ((await _context.UserRecitationProfiles.Where(p => p.UserId == ownerRAppUserId && p.Name == profile.Name).SingleOrDefaultAsync()) != null)
                {
                    pIndex++;
                    profile.Name = $"{profile.ArtistName} {pIndex.ToPersianNumbers()}";
                }
                _context.UserRecitationProfiles.Add(profile);
                await _context.SaveChangesAsync(); //this logically should be outside this loop, 
                                                   //but it messes with the order of records so I decided 
                                                   //to wait a little longer and have an ordered set of records
            }
            return "";
        }

        /// <summary>
        /// Initiate New Upload Session for audio
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSession>> InitiateNewUploadSession(Guid userId, bool replace)
        {
            UserRecitationProfile defProfile = await _context.UserRecitationProfiles.Where(p => p.UserId == userId && p.IsDefault == true).FirstOrDefaultAsync();
            if (defProfile == null)
            {
                return new RServiceResult<UploadSession>(null, "نمایهٔ پیش‌فرض شما مشخص نیست. لطفا پیش از ارسال خوانش نمایهٔ پیش‌فرض خود را تعریف کنید.");
            }

            if (!string.IsNullOrEmpty(defProfile.ArtistUrl))
            {
                if (defProfile.ArtistUrl.ToLower().IndexOf("http") != 0)
                {
                    return new RServiceResult<UploadSession>(null, $"نشانی سایت یا صفحهٔ‌اینستاگرام یا کانال تلگرام نمایهٔ شما باید با http:// یا https:// شروع شود. {defProfile.ArtistUrl} قابل پذیرش نیست.");
                }
            }
            if (!string.IsNullOrEmpty(defProfile.AudioSrcUrl))
            {
                if (defProfile.AudioSrcUrl.ToLower().IndexOf("http") != 0)
                {
                    return new RServiceResult<UploadSession>(null, $"نشانی وب منبع در نمایهٔ شما باید با http:// یا https:// شروع شود. {defProfile.AudioSrcUrl} قابل پذیرش نیست.");
                }
            }

            UploadSession session = new UploadSession()
            {
                SessionType = replace ? UploadSessionType.ReplaceAudio : UploadSessionType.NewAudio,
                UseId = userId,
                UploadStartTime = DateTime.Now,
                Status = UploadSessionProcessStatus.NotStarted,
                ProcessProgress = 0,
                User = await _context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync() //this would be referenced later and is needed
            };
            await _context.UploadSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            return new RServiceResult<UploadSession>(session);
        }

        /// <summary>
        /// Save uploaded file
        /// </summary>
        /// <param name="uploadedFile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSessionFile>> SaveUploadedFile(IFormFile uploadedFile)
        {
            UploadSessionFile file = new UploadSessionFile()
            {
                ContentDisposition = uploadedFile.ContentDisposition,
                ContentType = uploadedFile.ContentType,
                FileName = uploadedFile.FileName,
                Length = uploadedFile.Length,
                Name = uploadedFile.Name,
                ProcessResult = false,
                ProcessResultMsg = "در حال پردازش ..."
            };

            string ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".mp3" && ext != ".xml")
            {
                file.ProcessResultMsg = "تنها فایلهای با پسوند mp3 و xml قابل قبول هستند.";
            }
            else
            {
                if (!Directory.Exists(Configuration.GetSection("AudioUploadService")["TempUploadPath"]))
                {
                    try
                    {
                        Directory.CreateDirectory(Configuration.GetSection("AudioUploadService")["TempUploadPath"]);
                    }
                    catch
                    {
                        return new RServiceResult<UploadSessionFile>(null, $"ProcessImage: create dir failed {Configuration.GetSection("AudioUploadService")["TempUploadPath"]}");
                    }
                }

                string filePath = Path.Combine(Configuration.GetSection("AudioUploadService")["TempUploadPath"], file.FileName);
                while (File.Exists(filePath))
                {
                    filePath = Path.Combine(Configuration.GetSection("AudioUploadService")["TempUploadPath"], Guid.NewGuid().ToString() + ext);
                }
                using (FileStream fsMain = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fsMain);
                }
                file.FilePath = filePath;
            }

            return new RServiceResult<UploadSessionFile>(file);

        }

        /// <summary>
        /// finalize upload session (add files)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSession>> FinalizeNewUploadSession(UploadSession session, UploadSessionFile[] files)
        {
            session.UploadedFiles = files;
            session.UploadEndTime = DateTime.Now;

            _context.UploadSessions.Update(session);
            await _context.SaveChangesAsync();

            _backgroundTaskQueue.QueueBackgroundWorkItem
                (
                async token =>
                {
                    using RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>());
                    session.ProcessStartTime = DateTime.Now;
                    double fileCount = session.UploadedFiles.Count;
                    int processFilesCount = 0;
                    List<UploadSessionFile> mp3files = new List<UploadSessionFile>();
                    foreach (UploadSessionFile file in session.UploadedFiles.Where(file => Path.GetExtension(file.FilePath) == ".mp3").ToList())
                    {
                        processFilesCount++;
                        session.ProcessProgress = (int)(processFilesCount / fileCount * 100.0);
                        try
                        {
                            file.MP3FileCheckSum = PoemAudio.ComputeCheckSum(file.FilePath);
                            mp3files.Add(file);
                        }
                        catch (Exception exp)
                        {
                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = exp.ToString();
                            context.UploadSessions.Update(session);
                            await context.SaveChangesAsync();
                        }
                    }

                    UserRecitationProfile defProfile = await context.UserRecitationProfiles.Where(p => p.UserId == session.UseId && p.IsDefault == true).FirstOrDefaultAsync(); //this should not be null

                    int maxRecitationsPerPoem = int.Parse(Configuration.GetSection("AudioUploadService")["MaxRecitationsPerPoem"]);

                    foreach (UploadSessionFile file in session.UploadedFiles.Where(file => Path.GetExtension(file.FilePath) == ".xml").ToList())
                    {
                        try
                        {
                            //although each xml can theorically contain more than one file information
                            //this assumption was never implemented and used in Desktop Ganjoor which produces this xml file
                            //within the loop code the file is moved somewhere else and if the loop reaches is unexpected second path
                            //the code would fail!
                            foreach (PoemAudio audio in PoemAudioListProcessor.Load(file.FilePath))
                            {
                                var audioPoem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == audio.PoemId).SingleOrDefaultAsync();
                                var preUploadedRecitaion = await context.Recitations.Include(r => r.Owner).AsNoTracking().Where(a => a.Mp3FileCheckSum == audio.FileCheckSum && a.ReviewStatus != AudioReviewStatus.Rejected).SingleOrDefaultAsync();
                                if (audioPoem == null)
                                {
                                    session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg
                                            = $"شعری با این شناسه در گنجور وجود ندارد: {audio.PoemId}.{Environment.NewLine}" +
                                            $"ممکن است شعر مد نظر در گنجور جابجا یا حذف شده باشد. لطفاً از طریق دریافت مجموعه‌ها در گنجور رومیزی آخرین نسخهٔ مجموعهٔ سخنور مد نظر را دریافت کنید.";
                                    context.UploadSessions.Update(session);

                                    await new RNotificationService(context).PushNotification
                                     (
                                         session.UseId,
                                         "خطا در پردازش فایل ارسالی",
                                         $"شعری با این شناسه در گنجور وجود ندارد.{Environment.NewLine}" +
                                         $"{file.FileName}"
                                     );
                                }
                                else
                                if (preUploadedRecitaion != null)
                                {
                                    var preUploadedPoem = await context.GanjoorPoems.AsNoTracking().Where(p => p.Id == preUploadedRecitaion.GanjoorPostId).SingleOrDefaultAsync();
                                    session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg
                                            = $"فایل صوتیی همسان با فایل ارسالی پیشتر بارگذاری شده است.{Environment.NewLine}" +
                                            $"مشخصات فایل موجود: {preUploadedRecitaion.AudioTitle} - {preUploadedRecitaion.AudioArtist} - شناسهٔ شعر: {preUploadedRecitaion.GanjoorPostId} {Environment.NewLine}" +
                                            $"{(preUploadedPoem == null ? "شعر نامشخص" : preUploadedPoem.FullTitle)} - کاربر: {preUploadedRecitaion.Owner.NickName}";
                                    context.UploadSessions.Update(session);

                                    await new RNotificationService(context).PushNotification
                                     (
                                         session.UseId,
                                         "خطا در پردازش فایل ارسالی",
                                         $"فایل صوتیی همسان با فایل ارسالی پیشتر آپلود شده است.{Environment.NewLine}" +
                                         $"{file.FileName}"
                                     );
                                }
                                else
                                {
                                    string soundFilesFolder = Configuration.GetSection("AudioUploadService")["TempUploadPath"];
                                    string currentTargetFolder = Configuration.GetSection("AudioUploadService")["CurrentSoundFilesFolder"];
                                    string targetPathForAudioFiles = Path.Combine(Configuration.GetSection("AudioUploadService")["LocalAudioRepositoryPath"], currentTargetFolder);
                                    if (!Directory.Exists(targetPathForAudioFiles))
                                    {
                                        Directory.CreateDirectory(targetPathForAudioFiles);
                                    }
                                    string targetPathForXmlFiles = Path.Combine(targetPathForAudioFiles, "x");
                                    if (!Directory.Exists(targetPathForXmlFiles))
                                    {
                                        Directory.CreateDirectory(targetPathForXmlFiles);
                                    }

                                    Random rnd = new Random(DateTime.Now.Millisecond);
                                    string fileNameWithoutExtension = $"{audio.PoemId}-{defProfile.FileSuffixWithoutDash}-{rnd.Next()}";
                                    int tmp = 1;
                                    while
                                    (
                                    File.Exists(Path.Combine(targetPathForAudioFiles, $"{fileNameWithoutExtension}.mp3"))
                                    ||
                                    File.Exists(Path.Combine(targetPathForXmlFiles, $"{fileNameWithoutExtension}.xml"))
                                    )
                                    {
                                        fileNameWithoutExtension = $"{audio.PoemId}-{defProfile.FileSuffixWithoutDash}-{rnd.Next()}{tmp}";
                                        tmp++;
                                    }


                                    string localXmlFilePath = Path.Combine(targetPathForXmlFiles, $"{fileNameWithoutExtension}.xml");
                                    File.Move(file.FilePath, localXmlFilePath); //this is the movemnet I talked about earlier


                                    string localMp3FilePath = Path.Combine(targetPathForAudioFiles, $"{fileNameWithoutExtension}.mp3");

                                    UploadSessionFile mp3file = mp3files.Where(mp3 => mp3.MP3FileCheckSum == audio.FileCheckSum).SingleOrDefault();
                                    if (mp3file == null)
                                    {
                                        session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "فایل mp3 متناظر یافت نشد (توجه فرمایید که همنامی اهمیت ندارد و فایل mp3 ارسالی باید دقیقاً همان فایلی باشد که همگامی با آن صورت گرفته است. اگر بعداً آن را جایگزین کرده‌اید مشخصات آن با مشخصات درج شده در فایل xml همسان نخواهد بود).";
                                        context.UploadSessions.Update(session);

                                        await new RNotificationService(context).PushNotification
                                     (
                                         session.UseId,
                                         "خطا در پردازش فایل ارسالی",
                                         $"فایل mp3 متناظر یافت نشد(توجه فرمایید که همنامی اهمیت ندارد و فایل mp3 ارسالی باید دقیقاً همان فایلی باشد که همگامی با آن صورت گرفته است.اگر بعداً آن را جایگزین کرده‌اید مشخصات آن با مشخصات درج شده در فایل xml همسان نخواهد بود).{Environment.NewLine}" +
                                         $"{file.FileName}"
                                     );
                                    }
                                    else
                                    {
                                        bool overCrowdedPoem = false;
                                        if (maxRecitationsPerPoem != 0 && maxRecitationsPerPoem <= (await context.Recitations.AsNoTracking().CountAsync(r => r.GanjoorPostId == audio.PoemId && r.ReviewStatus == AudioReviewStatus.Approved)))
                                        {
                                            if
                                            (
                                            !(
                                            session.SessionType == UploadSessionType.ReplaceAudio
                                            &&
                                            await context.Recitations.Where(r => r.OwnerId == session.UseId && r.GanjoorPostId == audio.PoemId && r.AudioArtist == defProfile.ArtistName).AnyAsync()
                                            )
                                            )
                                            {
                                                overCrowdedPoem = true;
                                            }
                                        }

                                        if (overCrowdedPoem)
                                        {
                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = $"این شعر در حال حاضر دارای دست کم {maxRecitationsPerPoem.ToPersianNumbers()} خوانش است و امکان ارسال خوانش جدید برای آن وجود ندارد. اگر روی ارسال خوانش برای این شعر اصرار دارید لطفاً خوانش‌های موجود آن را بررسی کنید و اشکالاتی که منطقاً خوانش را قابل حذف می‌کند از طریق ابزار گزارش خطا گزارش کنید. راه دیگر آن است که برای اشعار دیگری که تعداد زیادی خوانش ندارند خوانش ارسال کنید.";
                                            context.UploadSessions.Update(session);
                                            await context.SaveChangesAsync();

                                            await new RNotificationService(context).PushNotification
                                                     (
                                                         session.UseId,
                                                         "خطای تعداد زیاد خوانش موجود برای شعر",
                                                         $"متأسفانه این شعر در حال حاضر دارای دست کم {maxRecitationsPerPoem.ToPersianNumbers()} خوانش است و امکان ارسال خوانش جدید برای آن وجود ندارد. اگر روی ارسال خوانش برای این شعر اصرار دارید لطفاً خوانش‌های موجود آن را بررسی کنید و اشکالاتی که منطقاً خوانش را قابل حذف می‌کند از طریق ابزار گزارش خطا گزارش کنید. راه دیگر آن است که برای اشعار دیگری که تعداد زیادی خوانش ندارند خوانش ارسال کنید.{Environment.NewLine}" +
                                                         $"{file.FileName}"
                                                     );
                                        }
                                        else
                                        {
                                            File.Move(mp3file.FilePath, localMp3FilePath);
                                            int mp3fileSize = File.ReadAllBytes(localMp3FilePath).Length;

                                            bool replace = false;
                                            if (session.SessionType == UploadSessionType.ReplaceAudio)
                                            {
                                                Recitation existing = await context.Recitations.Where(r => r.OwnerId == session.UseId && r.GanjoorPostId == audio.PoemId && r.AudioArtist == defProfile.ArtistName).FirstOrDefaultAsync();
                                                if (existing != null)
                                                {
                                                    replace = true;

                                                    File.Move(localXmlFilePath, existing.LocalXmlFilePath, true);
                                                    File.Move(localMp3FilePath, existing.LocalMp3FilePath, true);
                                                    existing.Mp3FileCheckSum = audio.FileCheckSum;
                                                    existing.Mp3SizeInBytes = mp3fileSize;
                                                    existing.FileLastUpdated = session.UploadEndTime;
                                                    existing.AudioSyncStatus = AudioSyncStatus.SynchronizedOrRejected;
                                                    context.Recitations.Update(existing);

                                                    bool recomputeOrders = false;
                                                    var mistakes = await context.RecitationApprovedMistakes.Where(m => m.RecitationId == existing.Id).ToListAsync();
                                                    if (mistakes.Count > 0)
                                                    {
                                                        context.RemoveRange(mistakes);
                                                        recomputeOrders = true;
                                                    }

                                                    await context.SaveChangesAsync();
                                                    if (recomputeOrders)
                                                    {
                                                        await _ComputePoemRecitationsOrdersAsync(context, existing.GanjoorPostId, true);
                                                    }

                                                    _backgroundTaskQueue.QueueBackgroundWorkItem
                                                            (
                                                            async token =>
                                                            {
                                                                using (RMuseumDbContext publishcontext = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                                                                {
                                                                    RecitationPublishingTracker tracker = new RecitationPublishingTracker()
                                                                    {
                                                                        PoemNarrationId = existing.Id,
                                                                        StartDate = DateTime.Now,
                                                                        XmlFileCopied = false,
                                                                        Mp3FileCopied = false,
                                                                        FirstDbUpdated = false,
                                                                        SecondDbUpdated = false,
                                                                    };
                                                                    publishcontext.RecitationPublishingTrackers.Add(tracker);
                                                                    await publishcontext.SaveChangesAsync();

                                                                    await _PublishNarration(existing, tracker, publishcontext);
                                                                }
                                                            });

                                                }
                                            }

                                            if (!replace)
                                            {
                                                Guid legacyAudioGuid = audio.SyncGuid;
                                                while (
                                                    (await context.Recitations.AsNoTracking().Where(a => a.LegacyAudioGuid == legacyAudioGuid).FirstOrDefaultAsync()) != null
                                                    )
                                                {
                                                    legacyAudioGuid = Guid.NewGuid();
                                                }


                                                Recitation narration = new Recitation()
                                                {
                                                    GanjoorPostId = audio.PoemId,
                                                    OwnerId = session.UseId,
                                                    GanjoorAudioId = 1 + await context.Recitations.OrderByDescending(a => a.GanjoorAudioId).Select(a => a.GanjoorAudioId).FirstOrDefaultAsync(),
                                                    AudioOrder = 1 + await context.Recitations.Where(a => a.GanjoorPostId == audio.PoemId).OrderByDescending(a => a.AudioOrder).Select(a => a.AudioOrder).FirstOrDefaultAsync(),
                                                    FileNameWithoutExtension = fileNameWithoutExtension,
                                                    SoundFilesFolder = currentTargetFolder,
                                                    AudioTitle = string.IsNullOrEmpty(audio.PoemTitle) ? audio.Description : audio.PoemTitle,
                                                    AudioArtist = defProfile.ArtistName,
                                                    AudioArtistUrl = defProfile.ArtistUrl,
                                                    AudioSrc = defProfile.AudioSrc,
                                                    AudioSrcUrl = defProfile.AudioSrcUrl,
                                                    LegacyAudioGuid = legacyAudioGuid,
                                                    Mp3FileCheckSum = audio.FileCheckSum,
                                                    Mp3SizeInBytes = mp3fileSize,
                                                    OggSizeInBytes = 0,
                                                    UploadDate = session.UploadEndTime,
                                                    FileLastUpdated = session.UploadEndTime,
                                                    LocalMp3FilePath = localMp3FilePath,
                                                    LocalXmlFilePath = localXmlFilePath,
                                                    AudioSyncStatus = AudioSyncStatus.NewItem,
                                                    ReviewStatus = AudioReviewStatus.Draft
                                                };

                                                if (narration.AudioTitle.IndexOf("فایل صوتی") == 0) //no modification on title
                                                {
                                                    GanjoorPoem poem = await context.GanjoorPoems.Where(p => p.Id == audio.PoemId).SingleOrDefaultAsync();
                                                    if (poem != null)
                                                    {
                                                        narration.AudioTitle = poem.Title;
                                                    }
                                                }
                                                context.Recitations.Add(narration);
                                            }

                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "";
                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResult = true;
                                            session.UploadedFiles.Where(f => f.Id == mp3file.Id).SingleOrDefault().ProcessResultMsg = "";
                                            session.UploadedFiles.Where(f => f.Id == mp3file.Id).SingleOrDefault().ProcessResult = true;
                                        }
                                    }
                                }

                                await context.SaveChangesAsync();


                            }
                        }
                        catch (Exception exp)
                        {
                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "خطا در پس پردازش فایل. اطلاعات بیشتر: " + exp.ToString();
                            context.UploadSessions.Update(session);
                            await context.SaveChangesAsync();

                            await new RNotificationService(context).PushNotification
                                     (
                                         session.UseId,
                                         "خطا در پردازش فایل ارسالی",
                                         $"{exp}{Environment.NewLine}" +
                                         $"{file.FileName}"
                                     );
                        }
                        processFilesCount++;
                        session.ProcessProgress = (int)(processFilesCount / fileCount * 100.0);
                    }

                    session.ProcessEndTime = DateTime.Now;
                    context.Update(session);

                    //remove session files (house keeping)
                    foreach (UploadSessionFile file in session.UploadedFiles)
                    {
                        if (!file.ProcessResult && string.IsNullOrEmpty(file.ProcessResultMsg))
                        {
                            file.ProcessResultMsg = "فایل xml یا mp3 متناظر این فایل یافت نشد.";
                            context.Update(file);

                            await new RNotificationService(context).PushNotification
                                     (
                                         session.UseId,
                                         "خطا در پردازش فایل ارسالی",
                                         $"فایل mp3 متناظر یافت نشد(توجه فرمایید که همنامی اهمیت ندارد و فایل mp3 ارسالی باید دقیقاً همان فایلی باشد که همگامی با آن صورت گرفته است.اگر بعداً آن را جایگزین کرده‌اید مشخصات آن با مشخصات درج شده در فایل xml همسان نخواهد بود).{Environment.NewLine}" +
                                         $"{file.FileName}"
                                     );

                        }
                        if (File.Exists(file.FilePath))
                        {
                            try
                            {
                                File.Delete(file.FilePath);
                            }
                            catch
                            {
                                //there should be a house keeping process somewhere to handle undeletable files
                            }
                        }
                    }
                    await context.SaveChangesAsync();

                }
                );

            return new RServiceResult<UploadSession>(session);
        }

        /// <summary>
        /// Moderate pending narration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="moderatorId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationViewModel>> ModeratePoemNarration(int id, Guid moderatorId, RecitationModerateViewModel model)
        {
            Recitation narration = await _context.Recitations.Include(a => a.Owner).Where(a => a.Id == id).SingleOrDefaultAsync();
            if (narration == null)
                return new RServiceResult<RecitationViewModel>(null, "404");
            if (narration.ReviewStatus != AudioReviewStatus.Draft && narration.ReviewStatus != AudioReviewStatus.Pending)
                return new RServiceResult<RecitationViewModel>(null, "خوانش می‌بایست در وضعیت پیش‌نویس یا در انتظار بازبینی باشد.");
            narration.ReviewDate = DateTime.Now;
            narration.ReviewerId = moderatorId;
            if (model.Result != PoemNarrationModerationResult.MetadataNeedsFixation)
            {
                narration.ReviewStatus = model.Result == PoemNarrationModerationResult.Approve ? AudioReviewStatus.Approved : AudioReviewStatus.Rejected;
            }
            if (narration.ReviewStatus == AudioReviewStatus.Rejected)
            {
                narration.AudioSyncStatus = AudioSyncStatus.SynchronizedOrRejected;
                File.Delete(narration.LocalMp3FilePath);
                File.Delete(narration.LocalXmlFilePath);
            }
            narration.ReviewMsg = model.Message;
            _context.Recitations.Update(narration);
            await _context.SaveChangesAsync();

            if (model.Result == PoemNarrationModerationResult.MetadataNeedsFixation)
            {
                await _notificationService.PushNotification
                     (
                         narration.OwnerId,
                         "نیاز به بررسی خوانش ارسالی",
                         $"خوانش {narration.AudioTitle} بررسی شده و نیاز به اعمال تغییرات دارد.{Environment.NewLine}" +
                         $"می‌توانید با مراجعه به خوانش‌هایتان وضعیت آن را بررسی کنید."
                     );
            }
            else
            if (narration.ReviewStatus == AudioReviewStatus.Rejected)
            {
                await _notificationService.PushNotification
                     (
                         narration.OwnerId,
                         "عدم پذیرش خوانش ارسالی",
                         $"خوانش ارسالی {narration.AudioTitle} قابل پذیرش نبود.{Environment.NewLine}" +
                         $"می‌توانید با مراجعه به  خوانش‌هایتان وضعیت آن را بررسی کنید."
                     );
            }
            else //approved:
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                    async token =>
                    {
                        using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                        {
                            RecitationPublishingTracker tracker = new RecitationPublishingTracker()
                            {
                                PoemNarrationId = narration.Id,
                                StartDate = DateTime.Now,
                                XmlFileCopied = false,
                                Mp3FileCopied = false,
                                FirstDbUpdated = false,
                                SecondDbUpdated = false,
                            };
                            context.RecitationPublishingTrackers.Add(tracker);
                            await context.SaveChangesAsync();

                            await _PublishNarration(narration, tracker, context);
                        }
                    });

                await _ganjoorService.CacheCleanForPageById(narration.GanjoorPostId);
            }

            return new RServiceResult<RecitationViewModel>(new RecitationViewModel(narration, narration.Owner, await _context.GanjoorPoems.Where(p => p.Id == narration.GanjoorPostId).SingleOrDefaultAsync(), ""));
        }


        #region Remote Update

        private void FtpClient_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }
        private async Task _PublishNarration(Recitation narration, RecitationPublishingTracker tracker, RMuseumDbContext context)
        {
            bool replace = narration.AudioSyncStatus == AudioSyncStatus.SoundOrXMLFilesChanged;
            try
            {
                if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                {
                    var ftpClient = new AsyncFtpClient
                    (
                        Configuration.GetSection("ExternalFTPServer")["Host"],
                        Configuration.GetSection("ExternalFTPServer")["Username"],
                        Configuration.GetSection("ExternalFTPServer")["Password"]
                    );
                    ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                    await ftpClient.AutoConnect();
                    ftpClient.Config.RetryAttempts = 3;

                    await ftpClient.UploadFile(narration.LocalXmlFilePath, $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}{narration.RemoteXMLFilePath}", createRemoteDir: true);
                    tracker.XmlFileCopied = true;
                    context.RecitationPublishingTrackers.Update(tracker);
                    await context.SaveChangesAsync();

                    await ftpClient.UploadFile(narration.LocalMp3FilePath, $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}{narration.RemoteMp3FilePath}", createRemoteDir: true);
                    tracker.Mp3FileCopied = true;
                    context.RecitationPublishingTrackers.Update(tracker);
                    await context.SaveChangesAsync();

                    await ftpClient.Disconnect();
                }

                narration.AudioSyncStatus = AudioSyncStatus.SynchronizedOrRejected;
                context.Recitations.Update(narration);
                await context.SaveChangesAsync();

                tracker.Finished = true;
                tracker.FinishDate = DateTime.Now;
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                await new RNotificationService(context).PushNotification
                (
                    narration.OwnerId,
                    replace ? "به‌روزآوری نهایی خوانش ارسالی" : "انتشار نهایی خوانش ارسالی",
                    $"خوانش ارسالی {narration.AudioTitle} منتشر شد.{Environment.NewLine}" +
                    $"لطفا توجه فرمایید که ممکن است ظاهر شدن تأثیر تغییرات روی سایت به دلیل تنظیمات حفظ کارایی گنجور تا یک روز طول بکشد.{Environment.NewLine}" +
                    $"می‌توانید با مراجعه به <a href=\"https://ganjoor.net/?p={narration.GanjoorPostId}\">این صفحه</a> وضعیت آن را بررسی کنید."
                );

            }
            catch (Exception exp)
            {
                //if an error occurs, narration.AudioSyncStatus is not updated and narration can be idetified later to do "retrypublish" attempts  
                tracker.LastException = exp.ToString();
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                await new RNotificationService(context).PushNotification
               (
                   narration.OwnerId,
                   replace ? "خطا در به‌روزآوری نهایی اطلاعات خوانش ارسالی" : "خطا در انتشار نهایی خوانش ارسالی",
                   $"انتشار یا به‌روزآوری خوانش ارسالی {narration.AudioTitle} با خطا مواجه شد.{Environment.NewLine}" +
                   $"لطفاً در صف انتشار گنجور وضعیت آن را بررسی کنید و تلاش مجدد بزنید."
               );

                //I mean admins!
                var importers = await _userService.GetUsersHavingPermission(RMuseumSecurableItem.AudioRecitationEntityShortName, RMuseumSecurableItem.ImportOperationShortName);
                if (string.IsNullOrEmpty(importers.ExceptionString)) //if not, do nothing!
                {
                    foreach (var moderator in importers.Result)
                    {
                        await new RNotificationService(_context).PushNotification
                                        (
                                            (Guid)moderator.Id,
                                            replace ? "خطا در به‌روزآوری نهایی اطلاعات خوانش ارسالی" : "خطا در انتشار نهایی خوانش ارسالی",
                                            $"لطفا صف انتظار را بررسی کنید.{Environment.NewLine}"
                                        );
                    }
                }
            }

        }

        private async Task _DeleteNarrationFromRemote(Recitation narration, RecitationPublishingTracker tracker, RMuseumDbContext context)
        {

            try
            {


                if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                {
                    var ftpClient = new AsyncFtpClient
                    (
                        Configuration.GetSection("ExternalFTPServer")["Host"],
                        Configuration.GetSection("ExternalFTPServer")["Username"],
                        Configuration.GetSection("ExternalFTPServer")["Password"]
                    );
                    ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                    await ftpClient.AutoConnect();
                    ftpClient.Config.RetryAttempts = 3;

                    if (true == await ftpClient.FileExists($"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}{narration.RemoteXMLFilePath}"))
                    {
                        await ftpClient.DeleteFile($"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}{narration.RemoteXMLFilePath}");
                    }

                    if (true == await ftpClient.FileExists($"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}{narration.RemoteXMLFilePath}"))
                    {
                        await ftpClient.DeleteFile($"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}{narration.RemoteXMLFilePath}");
                    }

                    await ftpClient.Disconnect();
                }

                string audioTitle = narration.AudioTitle;
                int GanjoorPostId = narration.GanjoorPostId;
                Guid userId = narration.OwnerId;

                await _FinalizeDelete(context, narration);

                tracker.Finished = true;
                tracker.FinishDate = DateTime.Now;
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                await new RNotificationService(context).PushNotification
                (
                    userId,
                    "حذف نهایی خوانش ارسالی",
                    $"خوانش ارسالی {audioTitle} حذف شد.{Environment.NewLine}" +
                    $"لطفاً توجه فرمایید که ممکن است ظاهر شدن تأثیر تغییرات روی وبگاه به دلیل تنظیمات حفظ کارایی گنجور تا یک روز طول بکشد.{Environment.NewLine}" +
                    $"می‌توانید با مراجعه به <a href=\"https://ganjoor.net/?p={GanjoorPostId}\">این صفحه</a> وضعیت آن را بررسی کنید."
                );


            }
            catch (Exception exp)
            {
                //if an error occurs, narration.AudioSyncStatus is not updated and narration can be idetified later to do "retrypublish" attempts  
                tracker.LastException = exp.ToString();
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                await new RNotificationService(context).PushNotification
               (
                   narration.OwnerId,
                   "خطا در حذف نهایی خوانش ارسالی",
                   $"حذف نهایی خوانش ارسالی {narration.AudioTitle} با خطا مواجه شد.{Environment.NewLine}" +
                   $"لطفا در صف انتشار گنجور وضعیت آن را بررسی کنید و تلاش مجدد بزنید."
               );

                //I mean admins!
                var importers = await _userService.GetUsersHavingPermission(RMuseumSecurableItem.AudioRecitationEntityShortName, RMuseumSecurableItem.ImportOperationShortName);
                if (string.IsNullOrEmpty(importers.ExceptionString)) //if not, do nothing!
                {
                    foreach (var moderator in importers.Result)
                    {
                        await new RNotificationService(_context).PushNotification
                                        (
                                            (Guid)moderator.Id,
                                            "خطا در حذف نهایی خوانش ارسالی",
                                            $"لطفا صف انتظار را بررسی کنید.{Environment.NewLine}"
                                        );
                    }
                }
            }

        }

        private async Task<string> _FinalizeDelete(RMuseumDbContext context, Recitation recitation)
        {
            try
            {
                bool rejected = recitation.ReviewStatus == AudioReviewStatus.Rejected;
                string mp3 = recitation.LocalMp3FilePath;
                string xml = recitation.LocalXmlFilePath;
                context.Recitations.Remove(recitation);
                await context.SaveChangesAsync();

                if (!rejected)
                {
                    if (File.Exists(mp3))
                        File.Delete(mp3);
                    if (File.Exists(xml))
                        File.Delete(xml);
                }

                return "";
            }
            catch (Exception exp)
            {
                return exp.ToString();
            }
        }

        private async Task _UpdateRemoteRecitations(Recitation narration, RecitationPublishingTracker tracker, RMuseumDbContext context, bool notify)
        {

            try
            {
                narration.AudioSyncStatus = AudioSyncStatus.SynchronizedOrRejected;
                context.Recitations.Update(narration);
                await context.SaveChangesAsync();

                tracker.Finished = true;
                tracker.FinishDate = DateTime.Now;
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                if (notify)
                {
                    await new RNotificationService(context).PushNotification
                (
                    narration.OwnerId,
                    "به‌روزآوری نهایی اطلاعات خوانش ارسالی",
                    $"اطلاعات خوانش ارسالی {narration.AudioTitle} به‌روز شد.{Environment.NewLine}" +
                    $"لطفا توجه فرمایید که فایل‌های صوتی معمولاً روی مرورگرها کَش می‌شوند. جهت اطمینان از جایگزینی فایل می‌بایست با مرورگری که تا به حال شعر را با آن ندیده‌اید بررسی بفرمایید.{Environment.NewLine}" +
                    $"می‌توانید با مراجعه به <a href=\"https://ganjoor.net/?p={narration.GanjoorPostId}\">این صفحه</a> وضعیت آن را بررسی کنید."
                );
                }


            }
            catch (Exception exp)
            {
                //if an error occurs, narration.AudioSyncStatus is not updated and narration can be idetified later to do "retrypublish" attempts  
                tracker.LastException = exp.ToString();
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                if (notify)
                {
                    await new RNotificationService(context).PushNotification
                   (
                       narration.OwnerId,
                       "خطا در به‌روزآوری نهایی اطلاعات خوانش ارسالی",
                       $"به‌روزآوری اطلاعات خوانش ارسالی {narration.AudioTitle} با خطا مواجه شد.{Environment.NewLine}" +
                       $"لطفا در صف انتشار گنجور وضعیت آن را بررسی کنید و تلاش مجدد بزنید."
                   );
                }


                //I mean admins!
                var importers = await _userService.GetUsersHavingPermission(RMuseumSecurableItem.AudioRecitationEntityShortName, RMuseumSecurableItem.ImportOperationShortName);
                if (string.IsNullOrEmpty(importers.ExceptionString)) //if not, do nothing!
                {
                    foreach (var moderator in importers.Result)
                    {
                        await new RNotificationService(_context).PushNotification
                                        (
                                            (Guid)moderator.Id,
                                            "خطا در به روزآوری نهایی خوانش ارسالی",
                                            $"لطفا صف انتظار را بررسی کنید.{Environment.NewLine}"
                                        );
                    }
                }
            }

        }

        /// <summary>
        /// retry publish unpublished narrations
        /// </summary>
        public async Task RetryPublish()
        {
            if (_backgroundTaskQueue.Count > 0)
                return;

            var unpublishedQueue = await _context.RecitationPublishingTrackers.ToArrayAsync();
            if (unpublishedQueue.Length > 0)
            {
                _context.RecitationPublishingTrackers.RemoveRange(unpublishedQueue);
                await _context.SaveChangesAsync();
            }

            _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                    async token =>
                    {
                        using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                        {
                            var list = await context.Recitations.Where(a => a.ReviewStatus == AudioReviewStatus.Approved && a.AudioSyncStatus != AudioSyncStatus.SynchronizedOrRejected).ToListAsync();
                            foreach (Recitation narration in list)
                            {
                                RecitationPublishingTracker tracker = new RecitationPublishingTracker()
                                {
                                    PoemNarrationId = narration.Id,
                                    StartDate = DateTime.Now,
                                    XmlFileCopied = false,
                                    Mp3FileCopied = false,
                                    FirstDbUpdated = false,
                                    SecondDbUpdated = false,
                                };
                                context.RecitationPublishingTrackers.Add(tracker);
                                await context.SaveChangesAsync();
                                switch (narration.AudioSyncStatus)
                                {
                                    case AudioSyncStatus.NewItem:
                                    case AudioSyncStatus.SoundOrXMLFilesChanged:
                                        {

                                            await _PublishNarration(narration, tracker, context);
                                        }
                                        break;
                                    case AudioSyncStatus.MetadataChanged:
                                        await _UpdateRemoteRecitations(narration, tracker, context, true);//this might send unexpected notications for users not expecting it in case of reordering recitations
                                        break;
                                    case AudioSyncStatus.Deleted:
                                        await _DeleteNarrationFromRemote(narration, tracker, context);
                                        break;
                                }

                            }
                        }
                    });
        }
        #endregion








        /// <summary>
        /// Get Upload Session (including files)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSession>> GetUploadSession(Guid id)
        {
            return new RServiceResult<UploadSession>
                (
                await _context.UploadSessions.AsNoTracking().Include(s => s.UploadedFiles).FirstOrDefaultAsync(s => s.Id == id)
                );
        }

        /// <summary>
        /// Get User Profiles
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="artistName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel[]>> GetUserNarrationProfiles(Guid userId, string artistName)
        {
            List<UserRecitationProfileViewModel> profiles = new List<UserRecitationProfileViewModel>();

            foreach (UserRecitationProfile p in
                (
                await _context.UserRecitationProfiles.AsNoTracking().Include(p => p.User)
                .Where(p => p.UserId == userId && p.IsDefault == true &&
                (string.IsNullOrEmpty(artistName) || (!string.IsNullOrEmpty(artistName) && p.ArtistName.Contains(artistName)))
                ).ToArrayAsync())
                )
            {
                profiles.Add
                    (
                    new UserRecitationProfileViewModel()
                    {
                        Id = p.Id,
                        User = p.User == null ? null :
                        new PublicRAppUser()
                        {
                            Id = p.User.Id,
                            Username = p.User.UserName,
                            Email = p.User.Email,
                            FirstName = p.User.FirstName,
                            SureName = p.User.SureName,
                            PhoneNumber = p.User.PhoneNumber,
                            RImageId = p.User.RImageId,
                            Status = p.User.Status,
                            NickName = p.User.NickName,
                            Website = p.User.Website,
                            Bio = p.User.Bio,
                            EmailConfirmed = p.User.EmailConfirmed
                        },
                        UserId = p.UserId,
                        Name = p.Name,
                        FileSuffixWithoutDash = p.FileSuffixWithoutDash,
                        ArtistName = p.ArtistName,
                        ArtistUrl = p.ArtistUrl,
                        AudioSrc = p.AudioSrc,
                        AudioSrcUrl = p.AudioSrcUrl,
                        IsDefault = p.IsDefault
                    }
                    );
            }

            foreach (UserRecitationProfile p in (await _context.UserRecitationProfiles.AsNoTracking().Include(p => p.User).Where(p => p.UserId == userId && p.IsDefault == false
            &&
                (string.IsNullOrEmpty(artistName) || (!string.IsNullOrEmpty(artistName) && p.ArtistName.Contains(artistName)))
            ).ToArrayAsync()))
            {
                profiles.Add
                    (
                    new UserRecitationProfileViewModel()
                    {
                        Id = p.Id,
                        User = p.User == null ? null :
                        new PublicRAppUser()
                        {
                            Id = p.User.Id,
                            Username = p.User.UserName,
                            Email = p.User.Email,
                            FirstName = p.User.FirstName,
                            SureName = p.User.SureName,
                            PhoneNumber = p.User.PhoneNumber,
                            RImageId = p.User.RImageId,
                            Status = p.User.Status,
                            NickName = p.User.NickName,
                            Website = p.User.Website,
                            Bio = p.User.Bio,
                            EmailConfirmed = p.User.EmailConfirmed
                        },
                        UserId = p.UserId,
                        Name = p.Name,
                        FileSuffixWithoutDash = p.FileSuffixWithoutDash,
                        ArtistName = p.ArtistName,
                        ArtistUrl = p.ArtistUrl,
                        AudioSrc = p.AudioSrc,
                        AudioSrcUrl = p.AudioSrcUrl,
                        IsDefault = p.IsDefault
                    }
                    );
            }
            return new RServiceResult<UserRecitationProfileViewModel[]>(profiles.ToArray());
        }

        /// <summary>
        /// Get User Default Profile
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel>> GetUserDefProfile(Guid userId)
        {
            var defProfile = await _context.UserRecitationProfiles.AsNoTracking().Include(p => p.User).Where(p => p.UserId == userId && p.IsDefault == true).FirstOrDefaultAsync();
            if (defProfile == null)
                return new RServiceResult<UserRecitationProfileViewModel>(null);
            return new RServiceResult<UserRecitationProfileViewModel>
                (
                 new UserRecitationProfileViewModel()
                 {
                     Id = defProfile.Id,
                     User = defProfile.User == null ? null :
                        new PublicRAppUser()
                        {
                            Id = defProfile.User.Id,
                            Username = defProfile.User.UserName,
                            Email = defProfile.User.Email,
                            FirstName = defProfile.User.FirstName,
                            SureName = defProfile.User.SureName,
                            PhoneNumber = defProfile.User.PhoneNumber,
                            RImageId = defProfile.User.RImageId,
                            Status = defProfile.User.Status,
                            NickName = defProfile.User.NickName,
                            Website = defProfile.User.Website,
                            Bio = defProfile.User.Bio,
                            EmailConfirmed = defProfile.User.EmailConfirmed
                        },
                     UserId = defProfile.UserId,
                     Name = defProfile.Name,
                     FileSuffixWithoutDash = defProfile.FileSuffixWithoutDash,
                     ArtistName = defProfile.ArtistName,
                     ArtistUrl = defProfile.ArtistUrl,
                     AudioSrc = defProfile.AudioSrc,
                     AudioSrcUrl = defProfile.AudioSrcUrl,
                     IsDefault = defProfile.IsDefault
                 }
                );
        }

        /// <summary>
        /// validating narration profile
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static string GetUserProfileValidationError(UserRecitationProfile p)
        {
            if (string.IsNullOrEmpty(p.Name))
            {
                return "نام نمایه نباید خالی باشد.";
            }
            if (p.ArtistName.Length < 3)
            {
                return "نام خوانشگر باید حداقل شامل سه نویسه باشد.";
            }

            if (p.FileSuffixWithoutDash.Length < 2)
            {
                return "طول پسوند قابل پذیرش حداقل دو کاراکتر است.";
            }

            if (p.FileSuffixWithoutDash.Length > 4)
            {
                return "طول پسوند قابل پذیرش حداکثر چهار کاراکتر است.";
            }

            string s = LanguageUtils.GetFirstNotMatchingCharacter(p.ArtistName, LanguageUtils.PersianAlphabet, " .‌");
            if (s != "")
            {
                return $"نام خوانشگر فقط باید شامل حروف فارسی و فاصله باشد. اولین حرف غیرمجاز = {s}";
            }

            s = LanguageUtils.GetFirstNotMatchingCharacter(p.ArtistUrl, LanguageUtils.EnglishLowerCaseAlphabet, LanguageUtils.EnglishLowerCaseAlphabet.ToUpper() + @":/._-0123456789%");
            if (s != "")
            {
                return $"نشانی خوانشگر فقط می‌تواند از حروف کوچک انگلیسی تشکیل شود. اولین حرف غیر مجاز = {s}";
            }

            if (!string.IsNullOrEmpty(p.ArtistUrl))
            {
                bool result = Uri.TryCreate(p.ArtistUrl, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (!result)
                {
                    return $"نشانی وب خوانشگر نامعتبر است.";
                }
            }


            s = LanguageUtils.GetFirstNotMatchingCharacter(p.AudioSrcUrl, LanguageUtils.EnglishLowerCaseAlphabet, LanguageUtils.EnglishLowerCaseAlphabet.ToUpper() + @":/._-0123456789%");
            if (s != "")
            {
                return $"نشانی منبع فقط می‌تواند از حروف کوچک انگلیسی تشکیل شود. اولین حرف غیر مجاز = {s}";
            }


            if (!string.IsNullOrEmpty(p.AudioSrcUrl))
            {
                bool result = Uri.TryCreate(p.AudioSrcUrl, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (!result)
                {
                    return $"نشانی وب منبع نامعتبر است.";
                }
            }

            s = LanguageUtils.GetFirstNotMatchingCharacter(p.FileSuffixWithoutDash, LanguageUtils.EnglishLowerCaseAlphabet);

            if (s != "")
            {
                return $"پسوند فقط می‌تواند از حروف کوچک انگلیسی تشکیل شود. اولین حرف غیر مجاز = {s}";
            }

            return "";
        }


        private string GeneratedProfileFileSuffixWithoutDash(string fileSuffixWithoutDash, string artistName)
        {
            if (string.IsNullOrEmpty(fileSuffixWithoutDash))
            {
                fileSuffixWithoutDash = "";
                foreach (string artistnamePart in GPersianTextSync.Farglisize(artistName).Split("-", StringSplitOptions.RemoveEmptyEntries))
                {
                    fileSuffixWithoutDash += artistnamePart[0];
                }
                fileSuffixWithoutDash = fileSuffixWithoutDash.ToLower();
                while (fileSuffixWithoutDash.Length < 2)
                {
                    fileSuffixWithoutDash += 'a';
                }
                if (fileSuffixWithoutDash.Length > 4)
                {
                    fileSuffixWithoutDash = fileSuffixWithoutDash.Substring(0, 4);
                }
            }
            return fileSuffixWithoutDash;
        }

        /// <summary>
        /// Add a narration profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel>> AddUserNarrationProfiles(UserRecitationProfileViewModel profile)
        {
            if (!string.IsNullOrEmpty(profile.ArtistUrl))
            {
                if (profile.ArtistUrl.ToLower().IndexOf("http") != 0)
                {
                    return new RServiceResult<UserRecitationProfileViewModel>(null, $"نشانی سایت یا صفحهٔ‌اینستاگرام یا کانال تلگرام باید با http:// یا https:// شروع شود. {profile.ArtistUrl} قابل قبول نیست.");
                }
            }
            if (!string.IsNullOrEmpty(profile.AudioSrcUrl))
            {
                if (profile.AudioSrcUrl.ToLower().IndexOf("http") != 0)
                {
                    return new RServiceResult<UserRecitationProfileViewModel>(null, $"نشانی وب منبع باید با http:// یا https:// شروع شود. {profile.AudioSrcUrl} قابل قبول نیست.");
                }
            }
            profile.FileSuffixWithoutDash = GeneratedProfileFileSuffixWithoutDash(profile.FileSuffixWithoutDash, profile.ArtistName);

            var p = new UserRecitationProfile()
            {
                UserId = profile.UserId,
                Name = profile.Name.Trim(),
                ArtistName = profile.ArtistName.Trim(),
                ArtistUrl = profile.ArtistUrl.Trim(),
                AudioSrc = profile.AudioSrc.Trim(),
                AudioSrcUrl = profile.AudioSrcUrl.Trim(),
                FileSuffixWithoutDash = profile.FileSuffixWithoutDash.Trim(),
                IsDefault = profile.IsDefault
            };

            string error = GetUserProfileValidationError(p);
            if (error != "")
            {
                return new RServiceResult<UserRecitationProfileViewModel>(null, error);
            }

            if ((await _context.UserRecitationProfiles.Where(e => e.UserId == p.Id && e.Name == p.Name).SingleOrDefaultAsync()) != null)
            {
                return new RServiceResult<UserRecitationProfileViewModel>(null, "شما نمایهٔ دیگری با همین نام دارید.");
            }

            await _context.UserRecitationProfiles.AddAsync(p);

            await _context.SaveChangesAsync();
            if (p.IsDefault)
            {
                foreach (var o in _context.UserRecitationProfiles.Where(o => o.Id != p.Id && o.UserId == p.UserId && o.IsDefault).Select(o => o))
                {
                    o.IsDefault = false;
                    _context.UserRecitationProfiles.Update(o);
                }
                await _context.SaveChangesAsync();
            }
            return new RServiceResult<UserRecitationProfileViewModel>
                (
                new UserRecitationProfileViewModel()
                {
                    Id = p.Id,
                    User = p.User == null ? null :
                        new PublicRAppUser()
                        {
                            Id = p.User.Id,
                            Username = p.User.UserName,
                            Email = p.User.Email,
                            FirstName = p.User.FirstName,
                            SureName = p.User.SureName,
                            PhoneNumber = p.User.PhoneNumber,
                            RImageId = p.User.RImageId,
                            Status = p.User.Status,
                            NickName = p.User.NickName,
                            Website = p.User.Website,
                            Bio = p.User.Bio,
                            EmailConfirmed = p.User.EmailConfirmed
                        },
                    UserId = p.UserId,
                    Name = p.Name,
                    FileSuffixWithoutDash = p.FileSuffixWithoutDash,
                    ArtistName = p.ArtistName,
                    ArtistUrl = p.ArtistUrl,
                    AudioSrc = p.AudioSrc,
                    AudioSrcUrl = p.AudioSrcUrl,
                    IsDefault = p.IsDefault
                }
                );
        }

        /// <summary>
        /// Update a narration profile 
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel>> UpdateUserNarrationProfiles(UserRecitationProfileViewModel profile)
        {
            var p = await _context.UserRecitationProfiles.Where(p => p.Id == profile.Id).SingleOrDefaultAsync();

            if (p.UserId != profile.UserId)
                return new RServiceResult<UserRecitationProfileViewModel>(null, "permission error");

            if (!string.IsNullOrEmpty(profile.ArtistUrl))
            {
                if (profile.ArtistUrl.ToLower().IndexOf("http") != 0)
                {
                    return new RServiceResult<UserRecitationProfileViewModel>(null, $"نشانی سایت یا صفحهٔ‌اینستاگرام یا کانال تلگرام باید با http:// یا https:// شروع شود. {profile.ArtistUrl} قابل قبول نیست.");
                }
            }
            if (!string.IsNullOrEmpty(profile.AudioSrcUrl))
            {
                if (profile.AudioSrcUrl.ToLower().IndexOf("http") != 0)
                {
                    return new RServiceResult<UserRecitationProfileViewModel>(null, $"نشانی وب منبع باید با http:// یا https:// شروع شود. {profile.AudioSrcUrl} قابل قبول نیست.");
                }
            }

            profile.FileSuffixWithoutDash = GeneratedProfileFileSuffixWithoutDash(profile.FileSuffixWithoutDash, profile.ArtistName);

            p.Name = profile.Name.Trim();
            p.ArtistName = profile.ArtistName.Trim();
            p.ArtistUrl = profile.ArtistUrl.Trim();
            p.AudioSrc = profile.AudioSrc.Trim();
            p.AudioSrcUrl = profile.AudioSrcUrl.Trim();
            p.FileSuffixWithoutDash = profile.FileSuffixWithoutDash.Trim();
            p.IsDefault = profile.IsDefault;

            string error = GetUserProfileValidationError(p);
            if (error != "")
            {
                return new RServiceResult<UserRecitationProfileViewModel>(null, error);
            }

            if ((await _context.UserRecitationProfiles.Where(e => e.UserId == p.Id && e.Name == p.Name && e.Id != p.Id).SingleOrDefaultAsync()) != null)
            {
                return new RServiceResult<UserRecitationProfileViewModel>(null, "شما نمایهٔ دیگری با همین نام دارید.");
            }

            _context.UserRecitationProfiles.Update(p);

            await _context.SaveChangesAsync();
            if (p.IsDefault)
            {
                foreach (var o in _context.UserRecitationProfiles.Where(o => o.Id != p.Id && o.UserId == p.UserId && o.IsDefault).Select(o => o))
                {
                    o.IsDefault = false;
                    _context.UserRecitationProfiles.Update(o);
                }
                await _context.SaveChangesAsync();
            }
            return new RServiceResult<UserRecitationProfileViewModel>
                (
                new UserRecitationProfileViewModel()
                {
                    Id = p.Id,
                    User = p.User == null ? null :
                        new PublicRAppUser()
                        {
                            Id = p.User.Id,
                            Username = p.User.UserName,
                            Email = p.User.Email,
                            FirstName = p.User.FirstName,
                            SureName = p.User.SureName,
                            PhoneNumber = p.User.PhoneNumber,
                            RImageId = p.User.RImageId,
                            Status = p.User.Status,
                            NickName = p.User.NickName,
                            Website = p.User.Website,
                            Bio = p.User.Bio,
                            EmailConfirmed = p.User.EmailConfirmed
                        },
                    UserId = p.UserId,
                    Name = p.Name,
                    FileSuffixWithoutDash = p.FileSuffixWithoutDash,
                    ArtistName = p.ArtistName,
                    ArtistUrl = p.ArtistUrl,
                    AudioSrc = p.AudioSrc,
                    AudioSrcUrl = p.AudioSrcUrl,
                    IsDefault = p.IsDefault
                }
                );
        }

        /// <summary>
        /// Delete a narration profile 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteUserNarrationProfiles(Guid id, Guid userId)
        {
            var p = await _context.UserRecitationProfiles.Where(p => p.Id == id).SingleOrDefaultAsync();
            if (p.UserId != userId)
                return new RServiceResult<bool>(false);
            _context.UserRecitationProfiles.Remove(p);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Get uploads descending by upload time
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId">if userId is empty all user uploads would be returned</param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, UploadedItemViewModel[] Items)>> GetUploads(PagingParameterModel paging, Guid userId)
        {
            var source =
                (
                from file in _context.UploadedFiles.AsNoTracking()
                join session in _context.UploadSessions.Include(s => s.User)
                on file.UploadSessionId equals session.Id
                where userId == Guid.Empty || session.UseId == userId
                orderby session.UploadEndTime descending
                select new UploadedItemViewModel()
                { FileName = file.FileName, ProcessResult = file.ProcessResult, ProcessResultMsg = file.ProcessResultMsg, UploadEndTime = session.UploadEndTime, UserName = session.User.UserName, ProcessStartTime = session.ProcessStartTime, ProcessProgress = session.ProcessProgress, ProcessEndTime = session.ProcessEndTime }
                ).AsQueryable();
            return new RServiceResult<(PaginationMetadata PagingMeta, UploadedItemViewModel[] Items)>(await QueryablePaginator<UploadedItemViewModel>.Paginate(source, paging));
        }

        /// <summary>
        /// publishing tracker data
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="unfinished"></param>
        /// <param name="filteredUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RecitationPublishingTrackerViewModel[] Items)>> GetPublishingQueueStatus(PagingParameterModel paging, bool unfinished, Guid filteredUserId)
        {
            var source =
                  from tracker in _context.RecitationPublishingTrackers.AsNoTracking()
                  join recitation in _context.Recitations.Include(a => a.Owner)
                 on tracker.PoemNarrationId equals recitation.Id
                  join poem in _context.GanjoorPoems
                 on recitation.GanjoorPostId equals poem.Id
                  where
                  (filteredUserId == Guid.Empty || recitation.OwnerId == filteredUserId)
                  &&
                  (!unfinished || (unfinished && !tracker.Finished))
                  orderby tracker.StartDate descending
                  select new RecitationPublishingTrackerViewModel()
                  {
                      UserEmail = recitation.Owner.Email,
                      PoemFullTitle = poem.FullTitle,
                      ArtistName = recitation.AudioArtist,
                      Operation = recitation.AudioSyncStatus == AudioSyncStatus.NewItem ? "جدید" :
                            recitation.AudioSyncStatus == AudioSyncStatus.SoundOrXMLFilesChanged ? "جایگزینی فایل" :
                            recitation.AudioSyncStatus == AudioSyncStatus.MetadataChanged ? "تغییر اطلاعات" :
                            recitation.AudioSyncStatus == AudioSyncStatus.Deleted ? "حذف" :
                            "نامشخص (انجام شده)",
                      InProgress = !tracker.Finished && string.IsNullOrEmpty(tracker.LastException),
                      XmlFileCopied = tracker.XmlFileCopied,
                      Mp3FileCopied = tracker.Mp3FileCopied,
                      FirstDbUpdated = tracker.FirstDbUpdated,
                      SecondDbUpdated = tracker.SecondDbUpdated,
                      Succeeded = tracker.Finished,
                      Error = !string.IsNullOrEmpty(tracker.LastException),
                      LastException = tracker.LastException,
                      StartDate = tracker.StartDate,
                      FinishDate = tracker.FinishDate
                  };

            (PaginationMetadata PagingMeta, RecitationPublishingTrackerViewModel[] Items) paginatedResult =
                await QueryablePaginator<RecitationPublishingTrackerViewModel>.Paginate(source, paging);

            return new RServiceResult<(PaginationMetadata PagingMeta, RecitationPublishingTrackerViewModel[] Items)>(paginatedResult);
        }

        /// <summary>
        /// Transfer Recitations Ownership
        /// </summary>
        /// <param name="currentOwenerId"></param>
        /// <param name="newOwnerId"></param>
        /// <param name="artistName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> TransferRecitationsOwnership(Guid currentOwenerId, Guid newOwnerId, string artistName)
        {
            var recitations = await _context.Recitations.Where(r => r.OwnerId == currentOwenerId && r.AudioArtist == artistName && r.ReviewStatus == AudioReviewStatus.Approved).ToListAsync();
            foreach (Recitation recitation in recitations)
            {
                recitation.OwnerId = newOwnerId;
            }
            _context.Recitations.UpdateRange(recitations);
            var profiles = await _context.UserRecitationProfiles.Where(r => r.UserId == currentOwenerId && r.ArtistName == artistName).ToListAsync();
            foreach (UserRecitationProfile profile in profiles)
            {
                profile.IsDefault = false;
                profile.UserId = newOwnerId;
            }
            _context.UserRecitationProfiles.UpdateRange(profiles);
            await _context.SaveChangesAsync();

            var defProfile = await _context.UserRecitationProfiles.Where(r => r.UserId == newOwnerId && r.IsDefault == true).FirstOrDefaultAsync();
            if (defProfile == null)
            {
                var firstProfile = await _context.UserRecitationProfiles.Where(r => r.UserId == newOwnerId).FirstOrDefaultAsync();
                if (firstProfile != null)
                {
                    firstProfile.IsDefault = true;
                    _context.UserRecitationProfiles.Update(firstProfile);
                    await _context.SaveChangesAsync();
                }
            }

            var user = await _userService.GetUserInformation(currentOwenerId);

            await _notificationService.PushNotification
                                        (
                                            newOwnerId,
                                            "انتقال مالکیت خوانش‌ها به شما",
                                            $"مالکیت {recitations.Count} خوانش تأیید شده از خوانشگری به نام «{artistName}» توسط کاربری با پست الکترونیکی «{user.Result.Email}» به شما منتقل شد.{Environment.NewLine}"
                                        );

            return new RServiceResult<int>(recitations.Count);
        }

        /// <summary>
        /// move recitaions of an artist to the first position
        /// </summary>
        /// <param name="artistName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> MakeArtistRecitationsFirst(string artistName)
        {
            var recitations = await _context.Recitations.Where(r => r.AudioArtist == artistName && r.InitialScore == 0).ToListAsync();
            foreach (Recitation recitation in recitations)
            {
                recitation.InitialScore = 100;
                _context.Update(recitation);
                await _context.SaveChangesAsync();
                await ComputePoemRecitationsOrdersAsync(recitation.GanjoorPostId);
            }
            return new RServiceResult<int>(recitations.Count);
        }

        /// <summary>
        /// Synchronization Queue
        /// </summary>
        /// <param name="filteredUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationViewModel[]>> GetSynchronizationQueue(Guid filteredUserId)
        {
            var source =
                 from audio in _context.Recitations.AsNoTracking().Include(a => a.Owner)
                 join poem in _context.GanjoorPoems
                 on audio.GanjoorPostId equals poem.Id
                 where
                 (filteredUserId == Guid.Empty || audio.OwnerId == filteredUserId)
                 &&
                 audio.ReviewStatus == AudioReviewStatus.Approved
                 &&
                 audio.AudioSyncStatus != AudioSyncStatus.SynchronizedOrRejected
                 orderby audio.UploadDate descending
                 select new RecitationViewModel(audio, audio.Owner, poem, "");

            return new RServiceResult<RecitationViewModel[]>(await source.ToArrayAsync());
        }

        /// <summary>
        /// report an error in a recitation
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="report"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationErrorReportViewModel>> ReportErrorAsync(Guid userId, RecitationErrorReportViewModel report)
        {
            try
            {
                report.ReasonText = report.ReasonText.Trim();
                if (string.IsNullOrEmpty(report.ReasonText))
                {
                    return new RServiceResult<RecitationErrorReportViewModel>(null, "دلیل گزارش مشخص نیست.");
                }
                if (report.NumberOfLinesAffected < 1)
                    report.NumberOfLinesAffected = 1;
                RecitationErrorReport dbModel = new RecitationErrorReport()
                {
                    RecitationId = report.RecitationId,
                    ReasonText = report.ReasonText,
                    ReporterId = userId,
                    DateTime = DateTime.Now,
                    NumberOfLinesAffected = report.NumberOfLinesAffected,
                    CoupletIndex = report.CoupletIndex
                };

                _context.RecitationErrorReports.Add(dbModel);
                await _context.SaveChangesAsync();

                var moderators = await _userService.GetUsersHavingPermission(RMuseumSecurableItem.AudioRecitationEntityShortName, RMuseumSecurableItem.ModerateOperationShortName);
                if (string.IsNullOrEmpty(moderators.ExceptionString)) //if not, do nothing!
                {
                    foreach (var moderator in moderators.Result)
                    {
                        await _notificationService.PushNotification
                                        (
                                            (Guid)moderator.Id,
                                            "گزارش خطا در خوانش",
                                            $"گزارش خطایی برای یک خوانش ثبت شده است. لطفاً خوانش‌های گزارش شده را در پیشخان خوانشگران مشاهده کنید.{Environment.NewLine}" +
                                            $"توجه فرمایید که اگر کاربر دیگری که دارای مجوز بررسی خوانش‌هاست پیش از شما به آن رسیدگی کرده باشد آن را در صف نخواهید دید."
                                        );
                    }
                }

                report.Id = dbModel.Id;
                report.DateTime = dbModel.DateTime;

                return new RServiceResult<RecitationErrorReportViewModel>(report);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RecitationErrorReportViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get errors reported for recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RecitationErrorReportViewModel[] Items)>> GetReportedErrorsAsync(PagingParameterModel paging)
        {
            var source =
                 from report in _context.RecitationErrorReports.Include(r => r.Recitation).Include(r => r.Reporter)
                 join poem in _context.GanjoorPoems
                 on report.Recitation.GanjoorPostId equals poem.Id
                 select
                 new RecitationErrorReportViewModel()
                 {
                     Id = report.Id,
                     ReasonText = report.ReasonText,
                     RecitationId = report.RecitationId,
                     Recitation = new RecitationViewModel(report.Recitation, report.Recitation.Owner, poem, ""),
                     DateTime = report.DateTime,
                     NumberOfLinesAffected = report.NumberOfLinesAffected,
                     CoupletIndex = report.CoupletIndex
                 };

            (PaginationMetadata PagingMeta, RecitationErrorReportViewModel[] Items) paginatedResult =
                await QueryablePaginator<RecitationErrorReportViewModel>.Paginate(source, paging);
            return new RServiceResult<(PaginationMetadata PagingMeta, RecitationErrorReportViewModel[] Items)>(paginatedResult);
        }

        /// <summary>
        /// reject a reported error for recitations and notify the reporter (and deletes the report)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rejectionNote"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RejectReportedErrorAsync(int id, string rejectionNote = "عدم تطابق با معیارهای حذف خوانش")
        {
            try
            {
                var report = await _context.RecitationErrorReports.Where(r => r.Id == id).SingleAsync();
                var recitation = await _context.Recitations.AsNoTracking().Where(r => r.Id == report.RecitationId).SingleAsync();
                var userId = report.ReporterId;
                _context.RecitationErrorReports.Remove(report);
                await _context.SaveChangesAsync();


                if (userId != null)
                {
                    await _notificationService.PushNotification
                (
                    (Guid)userId,
                    "عدم پذیرش گزارش خطای خوانش",
                    $"گزارش خطای ارسالی شما برای خوانش {recitation.AudioTitle} از {recitation.AudioArtist} به دلیل {rejectionNote} مورد پذیرش قرار نگرفت."
                );
                }

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {

                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// accepts a reported error for recitations and notify the reporter and recitation owner (and deletes the report)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AcceptReportedErrorAsync(int id)
        {
            try
            {
                var report = await _context.RecitationErrorReports.Where(r => r.Id == id).SingleAsync();

                var recitation = await _context.Recitations.Where(r => r.Id == report.RecitationId).SingleAsync();
                recitation.ReviewStatus = AudioReviewStatus.RejectedDueToReportedErrors;
                recitation.ReviewMsg = $"اشکال گزارش شده: {report.ReasonText}";
                _context.Recitations.Update(recitation);
                await _context.SaveChangesAsync();

                var reporterUserId = report.ReporterId;
                _context.RecitationErrorReports.Remove(report);
                await _context.SaveChangesAsync();


                if (reporterUserId != null)
                {
                    await _notificationService.PushNotification
                (
                    (Guid)reporterUserId,
                    "پذیرش گزارش خطای خوانش",
                    $"گزارش خطای ارسالی شما برای خوانش {recitation.AudioTitle} از {recitation.AudioArtist} بررسی شد و مورد پذیرش قرار گرفت.{Environment.NewLine}" +
                    $"خوانش یاد شده از حالت انتشار خارج شده است."
                );
                }

                await _notificationService.PushNotification
               (
                   recitation.OwnerId,
                   "خروج خوانش از وضعیت انتشار به دلیل گزارش خطا",
                   $"خوانش {recitation.AudioTitle} از {recitation.AudioArtist} به دلیل تأیید گزارش خطای ارسالی از سوی کاربران از حالت انتشار خارج شده است.{Environment.NewLine}" +
                   $"اشکال گزارش شده: {report.ReasonText}{Environment.NewLine}" +
                   $"لطفاً پس از بررسی مشکل خوانش یاد شده را حذف کنید."
               );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {

                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// accepts a reported error for recitations, add mistake to approve the mistake and notify the reporter and recitation owner (and deletes the report)
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AddReportToTheApprovedMistakesAsync(RecitationErrorReportViewModel report)
        {
            try
            {
                var dbReport = await _context.RecitationErrorReports.Where(r => r.Id == report.Id).SingleAsync();

                var recitation = await _context.Recitations.AsNoTracking().Where(r => r.Id == dbReport.RecitationId).SingleAsync();

                var mistake = new RecitationApprovedMistake()
                {
                    RecitationId = recitation.Id,
                    Mistake = report.ReasonText,
                    NumberOfLinesAffected = report.NumberOfLinesAffected,
                    CoupletIndex = report.CoupletIndex
                };

                _context.RecitationApprovedMistakes.Add(mistake);

                var reporterUserId = dbReport.ReporterId;
                _context.RecitationErrorReports.Remove(dbReport);
                await _context.SaveChangesAsync();

                if (reporterUserId != null)
                {
                    await _notificationService.PushNotification
                (
                    (Guid)reporterUserId,
                    "پذیرش گزارش خطای خوانش",
                    $"گزارش خطای ارسالی شما برای خوانش {recitation.AudioTitle} از {recitation.AudioArtist} بررسی شد و مورد پذیرش قرار گرفت.{Environment.NewLine}" +
                    $"خطای گزارش شده توسط شما روی سایت به کاربران نمایش داده می‌شود."
                );
                }

                await _notificationService.PushNotification
               (
                   recitation.OwnerId,
                   "تأیید خطای خوانش ارسالی",
                   $"خطایی در خوانش {recitation.AudioTitle} از {recitation.AudioArtist} گزارش و تأیید شده است.{Environment.NewLine}" +
                   $"اشکال گزارش شده: {dbReport.ReasonText}{Environment.NewLine}" +
                   $"لطفاً بررسی بفرمایید."
               );

                await ComputePoemRecitationsOrdersAsync(recitation.GanjoorPostId);

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {

                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// compute poem recitations order
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationOrderingViewModel[]>> ComputePoemRecitationsOrdersAsync(int poemId, bool update = true)
        {
            return await _ComputePoemRecitationsOrdersAsync(_context, poemId, update);
        }

        public async Task<RServiceResult<RecitationOrderingViewModel[]>> _ComputePoemRecitationsOrdersAsync(RMuseumDbContext context, int poemId, bool update)
        {
            try
            {
                var recitations =
                    await context.Recitations
                        .Where(r => r.ReviewStatus == AudioReviewStatus.Approved && r.GanjoorPostId == poemId)
                        .OrderBy(r => r.Id) //this causes the oldest recirations to become the first one
                        .ToListAsync();

                List<RecitationOrderingViewModel> scores = new List<RecitationOrderingViewModel>();

                for (var i = 0; i < recitations.Count; i++)
                {
                    var recitation = recitations[i];
                    RecitationOrderingViewModel score = new RecitationOrderingViewModel()
                    {
                        RecitationId = recitation.Id,
                        EarlynessAdvantage = recitations.Count - 1 - i,
                        InitialScore = recitations[i].InitialScore,
                        UpVotes = await context.RecitationUserUpVotes.AsNoTracking().Where(r => r.RecitationId == recitation.Id && r.UserId != recitation.OwnerId)
                        .CountAsync(),
                        Mistakes = await context.RecitationApprovedMistakes.AsNoTracking().Where(m => m.RecitationId == recitation.Id).SumAsync(m => m.NumberOfLinesAffected)
                    };


                    score.TotalScores = score.EarlynessAdvantage
                         + score.InitialScore
                         + score.UpVotes
                         - (5 * score.Mistakes);

                    //audio order is used as a temporary variable in the following line and soon is get replaced by computed value
                    recitation.AudioOrder = score.TotalScores;

                    scores.Add(score);
                }

                recitations.Sort((a, b) => b.AudioOrder.CompareTo(a.AudioOrder));
                for (var i = 0; i < recitations.Count; i++)
                {
                    recitations[i].AudioOrder = i + 1;

                    scores.Where(s => s.RecitationId == recitations[i].Id).Single().ComputedOrder = i + 1;

                    if (update)
                    {
                        context.Update(recitations[i]);
                    }

                }

                scores.Sort((a, b) => a.ComputedOrder.CompareTo(b.ComputedOrder));

                if (update)
                    await context.SaveChangesAsync();


                return new RServiceResult<RecitationOrderingViewModel[]>(scores.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RecitationOrderingViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// up vote a recitation
        /// </summary>
        /// <param name="id">recitation id</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpVoteRecitationAsync(int id, Guid userId)
        {
            try
            {
                var existingVote = await _context.RecitationUserUpVotes.AsNoTracking().Where(r => r.RecitationId == id && r.UserId == userId).SingleOrDefaultAsync();
                if (existingVote != null)
                {
                    return new RServiceResult<bool>(false, "شما پیش‌تر به این خوانش رأی داده‌اید.");
                }
                var recitation = await _context.Recitations.AsNoTracking().Where(r => r.Id == id).SingleOrDefaultAsync();
                if (recitation == null)
                {
                    return new RServiceResult<bool>(false, "خوانشی با این شناسه وجود ندارد.");
                }
                if (recitation.ReviewStatus != AudioReviewStatus.Approved)
                {
                    return new RServiceResult<bool>(false, "امکان رأی دادن به خوانش منتشر نشده وجود ندارد.");
                }

                RecitationUserUpVote vote = new RecitationUserUpVote()
                {
                    RecitationId = id,
                    UserId = userId,
                    DateTime = DateTime.Now
                };

                _context.RecitationUserUpVotes.Add(vote);
                await _context.SaveChangesAsync();

                await ComputePoemRecitationsOrdersAsync(recitation.GanjoorPostId);

                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// revoke recitaion up vote
        /// </summary>
        /// <param name="id">recitaion id</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RevokeUpVoteFromRecitationAsync(int id, Guid userId)
        {
            try
            {
                var vote = await _context.RecitationUserUpVotes.Where(r => r.RecitationId == id && r.UserId == userId).SingleOrDefaultAsync();
                if (vote == null)
                {
                    return new RServiceResult<bool>(false, "شما پیش‌تر به این خوانش رأی نداده‌اید.");
                }
                var recitation = await _context.Recitations.AsNoTracking().Where(r => r.Id == id).SingleOrDefaultAsync();
                _context.Remove(vote);
                await _context.SaveChangesAsync();

                await ComputePoemRecitationsOrdersAsync(recitation.GanjoorPostId);

                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// switches recitation upvote
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns>upvote status</returns>
        public async Task<RServiceResult<bool>> SwitchRecitationUpVoteAsync(int id, Guid userId)
        {
            try
            {
                var vote = await _context.RecitationUserUpVotes.Where(r => r.RecitationId == id && r.UserId == userId).SingleOrDefaultAsync();
                if (vote == null)
                {
                    return await UpVoteRecitationAsync(id, userId);
                }
                else
                {
                    var res = await RevokeUpVoteFromRecitationAsync(id, userId);
                    if (!string.IsNullOrEmpty(res.ExceptionString))
                        return res;
                    if (res.Result)
                        return new RServiceResult<bool>(false);//actually this method should return upvote status, so FALSE is the valid result in this situation
                }

                return new RServiceResult<bool>(true);

            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get user upvoted recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>> GetUserUpvotedRecitationsAsync(PagingParameterModel paging, Guid userId)
        {
            var source =
                 from upvote in _context.RecitationUserUpVotes.AsNoTracking().Include(u => u.Recitation)
                 join poem in _context.GanjoorPoems
                 on upvote.Recitation.GanjoorPostId equals poem.Id
                 where
                 upvote.UserId == userId
                 orderby upvote.DateTime descending
                 select new PublicRecitationViewModel()
                 {
                     Id = upvote.Recitation.Id,
                     PoemId = poem.Id,
                     PoemFullTitle = poem.FullTitle,
                     PoemFullUrl = poem.FullUrl,
                     AudioTitle = upvote.Recitation.AudioTitle,
                     AudioArtist = upvote.Recitation.AudioArtist,
                     AudioArtistUrl = upvote.Recitation.AudioArtistUrl,
                     AudioSrc = upvote.Recitation.AudioSrc,
                     AudioSrcUrl = upvote.Recitation.AudioSrcUrl,
                     LegacyAudioGuid = upvote.Recitation.LegacyAudioGuid,
                     Mp3FileCheckSum = upvote.Recitation.Mp3FileCheckSum,
                     Mp3SizeInBytes = upvote.Recitation.Mp3SizeInBytes,
                     PublishDate = upvote.Recitation.ReviewDate,
                     FileLastUpdated = upvote.Recitation.FileLastUpdated,
                     Mp3Url = $"{WebServiceUrl.Url}/api/audio/file/{upvote.Recitation.Id}.mp3",
                     XmlText = $"{WebServiceUrl.Url}/api/audio/xml/{upvote.Recitation.Id}",
                     PlainText = poem.PlainText,
                     HtmlText = poem.HtmlText,
                     AudioOrder = upvote.Recitation.AudioOrder,
                     UpVotedByUser = true,
                 };

            (PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items) paginatedResult =
                await QueryablePaginator<PublicRecitationViewModel>.Paginate(source, paging);

            return new RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>(paginatedResult);
        }


        /// <summary>
        /// check recitaions with missing files and add them to reported errors list
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartCheckingRecitationsHealthCheck(Guid userId)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
                        (
                        async token =>
                        {
                            using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                var job = (await jobProgressServiceEF.NewJob($"CheckingRecitationsHealthCheck", "Query data")).Result;
                                int currentRecitationId = -1;
                                try
                                {
                                    var recitations = await context.Recitations.Where(s => s.ReviewStatus == AudioReviewStatus.Approved).AsNoTracking().ToArrayAsync();
                                    int progress = 0;
                                    List<string> replacedFiles = new List<string>();
                                    for (int i = 0; i < recitations.Length; i++)
                                    {
                                        var recitation = recitations[i];
                                        currentRecitationId = recitation.Id;
                                        if (!File.Exists(recitation.LocalMp3FilePath) || !File.Exists(recitation.LocalXmlFilePath))
                                        {

                                            RecitationErrorReport error = new RecitationErrorReport()
                                            {
                                                RecitationId = recitation.Id,
                                                CoupletIndex = -1,
                                                NumberOfLinesAffected = 0,
                                                ReporterId = userId,
                                                ReasonText = File.Exists(recitation.LocalMp3FilePath) == false ? "سلامت‌سنجی فایل‌ها: MP3 file missing" : "سلامت‌سنجی فایل‌ها: XML file misssing",
                                                DateTime = DateTime.Now
                                            };

                                            context.RecitationErrorReports.Add(error);
                                        }
                                        else
                                        {
                                            var mp3CheckSum = PoemAudio.ComputeCheckSum(recitation.LocalMp3FilePath);
                                            PoemAudio audio = PoemAudioListProcessor.Load(recitation.LocalXmlFilePath).First();
                                            if (audio.FileCheckSum != mp3CheckSum)
                                            {
                                                string backupFile = recitation.LocalMp3FilePath.Replace("C:\\inetpub\\iganjoor", "C:\\audiobackups-restored");
                                                if (File.Exists(backupFile))
                                                {
                                                    File.Copy(backupFile, recitation.LocalMp3FilePath, true);
                                                    replacedFiles.Add(recitation.LocalMp3FilePath);
                                                }
                                                else
                                                {
                                                    RecitationErrorReport error = new RecitationErrorReport()
                                                    {
                                                        RecitationId = recitation.Id,
                                                        CoupletIndex = -1,
                                                        NumberOfLinesAffected = 0,
                                                        ReporterId = userId,
                                                        ReasonText = "سلامت‌سنجی فایل‌ها: امضای فایل mp3 با امضای ثبت شده در xml متفاوت است. شاید فایل خراب شده باشد.",
                                                        DateTime = DateTime.Now
                                                    };

                                                    context.RecitationErrorReports.Add(error);
                                                }
                                            }
                                        }

                                        if (progress < (i * 100 / recitations.Length))
                                        {
                                            progress++;
                                            await jobProgressServiceEF.UpdateJob(job.Id, progress);
                                        }
                                    }
                                    if (replacedFiles.Count > 0)
                                    {
                                        File.WriteAllLines("C:\\audiobackups-restored\\list.txt", replacedFiles.ToArray());
                                    }
                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                                }
                                catch (Exception exp)
                                {
                                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"recitation Id : {currentRecitationId}, exp : {exp}");
                                }

                            }
                        }
                        );

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Upload Enabled (temporary switch off/on for upload)
        /// </summary>
        public bool UploadEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration.GetSection("AudioUploadService")["Enabled"]) && !bool.Parse(Configuration["ReadOnlyMode"]);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Background Task Queue Instance
        /// </summary>
        protected readonly IBackgroundTaskQueue _backgroundTaskQueue;

        /// <summary>
        /// Messaging service
        /// </summary>
        protected readonly IRNotificationService _notificationService;

        /// <summary>
        /// Users service
        /// </summary>
        protected readonly IAppUserService _userService;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// ganjoor service
        /// </summary>
        private readonly IGanjoorService _ganjoorService;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="notificationService"></param>
        /// <param name="userService"></param>
        /// <param name="memoryCache"></param>
        /// <param name="ganjoorService"></param>
        public RecitationService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue, IRNotificationService notificationService, IAppUserService userService, IMemoryCache memoryCache, IGanjoorService ganjoorService)
        {
            _context = context;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
            _notificationService = notificationService;
            _userService = userService;
            _memoryCache = memoryCache;
            _ganjoorService = ganjoorService;
        }
    }
}
