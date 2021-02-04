using Dapper;
using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using RMuseum.DbContext;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
using RMuseum.Models.UploadSession.ViewModels;
using RMuseum.Services.Implementation;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
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
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RecitationViewModel[] Items)>> SecureGetAll(PagingParameterModel paging, Guid filteredUserId, AudioReviewStatus status, string searchTerm)
        {
            try
            {
                //whenever I had not a reference to audio.Owner in the final selection it became null, so this strange arrangement is not all because of my stupidity!
                var source =
                     from audio in _context.Recitations.Include(a => a.Owner)
                     join poem in _context.GanjoorPoems
                     on audio.GanjoorPostId equals poem.Id
                     where 
                     (filteredUserId == Guid.Empty || audio.OwnerId == filteredUserId)
                     &&
                     (status == AudioReviewStatus.All || audio.ReviewStatus == status)
                     &&
                     (string.IsNullOrEmpty(searchTerm) ||
                     (!string.IsNullOrEmpty(searchTerm) && (audio.AudioArtist.Contains(searchTerm) || audio.AudioTitle.Contains(searchTerm) || poem.FullTitle.Contains(searchTerm) ))
                     )
                     orderby audio.UploadDate descending
                     select new RecitationViewModel(audio, audio.Owner, poem);

                (PaginationMetadata PagingMeta, RecitationViewModel[] Items) paginatedResult =
                    await QueryablePaginator<RecitationViewModel>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, RecitationViewModel[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RecitationViewModel[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
        }

        /// <summary>
        /// returns list of publish recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>> GetPublishedRecitations(PagingParameterModel paging, string searchTerm)
        {
            try
            {
                var source =
                     from audio in _context.Recitations
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
                         Mp3Url = $"https://ganjgah.ir/api/audio/file/{audio.Id}.mp3",
                         XmlText = $"https://ganjgah.ir/api/audio/xml/{audio.Id}",
                         PlainText = poem.PlainText,
                         HtmlText = poem.HtmlText
                     };

                (PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items) paginatedResult =
                    await QueryablePaginator<PublicRecitationViewModel>.Paginate(source, paging);

                return new RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, PublicRecitationViewModel[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
        }

        /// <summary>
        /// get published recitation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PublicRecitationViewModel>> GetPublishedRecitationById(int id)
        {
            try
            {
                var source =
                     from audio in _context.Recitations
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
                         Mp3Url = $"https://ganjgah.ir/api/audio/file/{audio.Id}.mp3",
                         XmlText = $"https://ganjgah.ir/api/audio/xml/{audio.Id}",
                         PlainText = poem.PlainText,
                         HtmlText = poem.HtmlText
                     };

                return new RServiceResult<PublicRecitationViewModel>(await source.SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PublicRecitationViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// return selected narration information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationViewModel>> Get(int id)
        {
            try
            {
                //whenever I had not a reference to audio.Owner in the final selection it became null, so this strange arrangement is not all because of my stupidity!
                var source =
                     from audio in _context.Recitations
                     .Include(a => a.Owner)
                     .Where(a => a.Id == id)
                     join poem in _context.GanjoorPoems
                     on audio.GanjoorPostId equals poem.Id
                     select new RecitationViewModel(audio, audio.Owner, poem);

                var narration = await source.SingleOrDefaultAsync();
                return new RServiceResult<RecitationViewModel>(narration);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RecitationViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Delete recitation
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> Delete(int id, Guid userId)
        {
            try
            {
                Recitation recitation = await _context.Recitations.Where(a => a.Id == id && a.OwnerId == userId).FirstOrDefaultAsync();
                if(recitation == null)
                {
                    return new RServiceResult<bool>(false, "404");
                }

               

                if (recitation.ReviewStatus == AudioReviewStatus.Approved)
                {
                    recitation.AudioSyncStatus = AudioSyncStatus.Deleted;
                    _context.Recitations.Update(recitation);
                    await _context.SaveChangesAsync();

                    _backgroundTaskQueue.QueueBackgroundWorkItem
                  (
                  async token =>
                  {
                      using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
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
                }
                else
                {
                    await _FinalizeDelete(_context, recitation);
                }


                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
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
                
                if(!rejected)
                {
                    if (File.Exists(mp3))
                        File.Delete(mp3);
                    if (File.Exists(xml))
                        File.Delete(xml);
                }

                return "";
            }
            catch(Exception exp)
            {
                return exp.ToString();
            }
        }


        /// <summary>
        /// Gets Verse Sync Range Information
        /// </summary>
        /// <param name="id">narration id</param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationVerseSync[]>> GetPoemNarrationVerseSyncArray(int id)
        {
            try
            {
                var narration = await _context.Recitations.Where(a => a.Id == id).SingleOrDefaultAsync();
                var verses = await _context.GanjoorVerses.Where(v => v.PoemId == narration.GanjoorPostId).OrderBy(v => v.VOrder).ToListAsync();

                string xml = File.ReadAllText(narration.LocalXmlFilePath);

                List<RecitationVerseSync> verseSyncs = new List<RecitationVerseSync>();

                XElement elObject = XDocument.Parse(xml).Root;
                float oneSecond = 1;
                if(elObject.Element("PoemAudio").Elements("OneSecondBugFix").Count() == 0)
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
            catch (Exception exp)
            {
                return new RServiceResult<RecitationVerseSync[]>(null, exp.ToString());
            }
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

            if(!string.IsNullOrEmpty(p.AudioSrcUrl))
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
            try
            {

                metadata.AudioTitle = metadata.AudioTitle.Trim();
                metadata.AudioArtist = metadata.AudioArtist.Trim();
                metadata.AudioArtistUrl = metadata.AudioArtistUrl.Trim();
                metadata.AudioSrc = metadata.AudioSrc.Trim();
                metadata.AudioSrcUrl = metadata.AudioSrcUrl.Trim();

                string err = GetPoemNarrationValidationError(metadata);
                if(!string.IsNullOrEmpty(err))
                {
                    return new RServiceResult<RecitationViewModel>(null, err);
                }

                

                Recitation narration =  await _context.Recitations.Include(a => a.Owner).Where(a => a.Id == id).SingleOrDefaultAsync();
                if(narration == null)
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

                if(bNewPendingRequest)
                {
                    var moderators = await _userService.GetUsersHavingPermission(RMuseumSecurableItem.AudioRecitationEntityShortName, RMuseumSecurableItem.ModerateOperationShortName);
                    if(string.IsNullOrEmpty(moderators.ExceptionString)) //if not, do nothing!
                    {
                        foreach(var moderator in moderators.Result)
                        {
                            await new RNotificationService(_context).PushNotification
                                            (
                                                (Guid)moderator.Id ,
                                                "درخواست بررسی خوانش",
                                                $"درخواستی برای بررسی خوانشی از «{narration.AudioArtist}» ثبت شده است. در صورت تمایل به بررسی، بخش «خوانش‌های در انتظار تأیید» را ببینید.{ Environment.NewLine}" +
                                                $"توجه فرمایید که اگر کاربر دیگری که دارای مجوز بررسی خوانش‌هاست پیش از شما به آن رسیدگی کرده باشد آن را در صف نخواهید دید."
                                            );
                        }
                    }
                }


                if(narration.ReviewStatus == AudioReviewStatus.Approved)
                {
                    narration.AudioSyncStatus = AudioSyncStatus.MetadataChanged;
                    _context.Recitations.Update(narration);
                    await _context.SaveChangesAsync();

                    _backgroundTaskQueue.QueueBackgroundWorkItem
                   (
                   async token =>
                   {
                       using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
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

                }
                return new RServiceResult<RecitationViewModel>(new RecitationViewModel(narration, narration.Owner, await _context.GanjoorPoems.Where(p => p.Id == narration.GanjoorPostId).SingleOrDefaultAsync()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<RecitationViewModel>(null, exp.ToString());
            }
        }

 


        /// <summary>
        /// imports data from ganjoor MySql database
        /// </summary>
        /// <param name="ownerRAppUserId">User Id which becomes owner of imported data</param>
        public async Task<RServiceResult<bool>> OneTimeImport(Guid ownerRAppUserId)
        {
            try
            {
                Recitation sampleCheck = await _context.Recitations.FirstOrDefaultAsync();
                if(sampleCheck != null)
                {
                    return new RServiceResult<bool>(false, "OneTimeImport is a one time operation and cannot be called multiple times.");
                }
                using (MySqlConnection connection = new MySqlConnection
                    (
                    $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8"
                    ))
                {
                    connection.Open();
                    //I thought that result Id fields would become corresponant to order of selection (and later insertions) but it is not
                    //the case in batch insertion, so this ORDER BY clause is useless unless we do save every time we insert a record
                    //which I guess might take much longer
                    using(MySqlDataAdapter src = new MySqlDataAdapter(
                        "SELECT audio_ID, audio_post_ID, audio_order, audio_xml, audio_ogg, audio_mp3, " +
                        "audio_title,  audio_artist, audio_artist_url, audio_src,  audio_src_url, audio_guid, " +
                        "audio_fchecksum,  audio_mp3bsize,  audio_oggbsize,  audio_date " +
                        "FROM ganja_gaudio ORDER BY audio_date",
                        connection
                        ))
                    {
                        using(DataTable srcData = new DataTable())
                        {
                            await src.FillAsync(srcData);


                            foreach (DataRow row in srcData.Rows)
                            {
                                Recitation newRecord = new Recitation()
                                {
                                    OwnerId = ownerRAppUserId,
                                    GanjoorAudioId = int.Parse(row["audio_ID"].ToString()),
                                    GanjoorPostId = (int)row["audio_post_ID"],
                                    AudioOrder = (int)row["audio_order"],
                                    AudioTitle = row["audio_title"].ToString(),
                                    AudioArtist = row["audio_artist"].ToString(),
                                    AudioArtistUrl = row["audio_artist_url"].ToString(),
                                    AudioSrc = row["audio_src"].ToString(),
                                    AudioSrcUrl = row["audio_src_url"].ToString(),
                                    LegacyAudioGuid = new Guid(row["audio_guid"].ToString()),
                                    Mp3FileCheckSum = row["audio_fchecksum"].ToString(),
                                    Mp3SizeInBytes = (int)row["audio_mp3bsize"],
                                    OggSizeInBytes = (int)row["audio_oggbsize"],
                                    UploadDate = (DateTime)row["audio_date"],
                                    AudioSyncStatus = AudioSyncStatus.SynchronizedOrRejected,
                                    ReviewStatus = AudioReviewStatus.Approved
                                };
                                newRecord.FileLastUpdated = newRecord.UploadDate;
                                newRecord.ReviewDate = newRecord.UploadDate;
                                string audio_xml = row["audio_xml"].ToString();
                                //sample audio_xml value: /i/a/x/11876-Simorgh.xml
                                audio_xml = audio_xml.Substring("/i/".Length); // /i/a/x/11876-Simorgh.xml -> a/x/11876-Simorgh.xml
                                newRecord.SoundFilesFolder = audio_xml.Substring(0, audio_xml.IndexOf('/')); //(a)
                                string targetForAudioFile = Path.Combine(Configuration.GetSection("AudioUploadService")["LocalAudioRepositoryPath"], newRecord.SoundFilesFolder);
                                string targetForXmlAudioFile = Path.Combine(targetForAudioFile, "x");
                               
                                newRecord.FileNameWithoutExtension = Path.GetFileNameWithoutExtension(audio_xml.Substring(audio_xml.LastIndexOf('/') + 1)); //(11876-Simorgh)
                                newRecord.LocalMp3FilePath = Path.Combine(targetForAudioFile, $"{newRecord.FileNameWithoutExtension}.mp3");
                                newRecord.LocalXmlFilePath = Path.Combine(targetForXmlAudioFile, $"{newRecord.FileNameWithoutExtension}.xml");

                                _context.Recitations.Add(newRecord);
                                await _context.SaveChangesAsync(); //this logically should be outside this loop, 
                                                                   //but it messes with the order of records so I decided 
                                                                   //to wait a little longer and have an ordered set of records
                            }
                        }
                       
                    }
                }
                string err = await BuildProfilesFromExistingData(ownerRAppUserId);
                if(!string.IsNullOrEmpty(err))
                    return new RServiceResult<bool>(false, err);
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// build profiles from exisng narrations data
        /// </summary>
        /// <param name="ownerRAppUserId">User Id which becomes owner of imported data</param>
        /// <returns>error string if occurs</returns>
        public async Task<string> BuildProfilesFromExistingData(Guid ownerRAppUserId)
        {
            try
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
                foreach(UserRecitationProfile profile in profiles)
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
                        ext = narration.FileNameWithoutExtension.Substring(narration.FileNameWithoutExtension.IndexOf('-') + 1);
                    }
                    if(ext.Length < 2)
                    {
                        string[] parts = profile.ArtistName.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        if(parts.Length < 2)
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
                    while((await _context.UserRecitationProfiles.Where(p => p.UserId == ownerRAppUserId && p.Name == profile.Name).SingleOrDefaultAsync())!=null)
                    {
                        pIndex++;
                        profile.Name = $"{profile.ArtistName} {GPersianTextSync.Sync(pIndex.ToString())}";
                    }
                    _context.UserRecitationProfiles.Add(profile);
                    await _context.SaveChangesAsync(); //this logically should be outside this loop, 
                                                       //but it messes with the order of records so I decided 
                                                       //to wait a little longer and have an ordered set of records
                }
                return "";
            }
            catch (Exception exp)
            {
                return exp.ToString();
            }
        }

        /// <summary>
        /// Initiate New Upload Session for audio
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSession>> InitiateNewUploadSession(Guid userId, bool replace)
        {
            try
            {
                UserRecitationProfile defProfile = await _context.UserRecitationProfiles.Where(p => p.UserId == userId && p.IsDefault == true).FirstOrDefaultAsync();
                if (defProfile == null)
                {
                    return new RServiceResult<UploadSession>(null, "نمایهٔ پیش‌فرض شما مشخص نیست. لطفا پیش از ارسال خوانش نمایهٔ پیش‌فرض خود را تعریف کنید.");
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
            catch(Exception exp)
            {
                return new RServiceResult<UploadSession>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Save uploaded file
        /// </summary>
        /// <param name="uploadedFile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSessionFile>> SaveUploadedFile(IFormFile uploadedFile)
        {
            try
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
                if(ext != ".mp3" && ext != ".xml" && ext != ".ogg")
                {
                    file.ProcessResultMsg = "تنها فایلهای با پسوند mp3، xml و ogg قابل قبول هستند.";
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
                    while(File.Exists(filePath))
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
            catch (Exception exp)
            {
                return new RServiceResult<UploadSessionFile>(null, exp.ToString());
            }
        }

        /// <summary>
        /// finalize upload session (add files)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSession>> FinalizeNewUploadSession(UploadSession session, UploadSessionFile[] files)
        {
            try
            {
                session.UploadedFiles = files;
                session.UploadEndTime = DateTime.Now;

                _context.UploadSessions.Update(session);
                await _context.SaveChangesAsync();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                    async token =>
                    {
                        using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                        {
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
                                        if( await context.Recitations.Where(a => a.Mp3FileCheckSum == audio.FileCheckSum && a.ReviewStatus != AudioReviewStatus.Rejected).SingleOrDefaultAsync() != null)
                                        {
                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "فایل صوتیی همسان با فایل ارسالی پیشتر آپلود شده است.";
                                            context.UploadSessions.Update(session);

                                            await new RNotificationService(context).PushNotification
                                             (
                                                 session.UseId,
                                                 "خطا در پردازش فایل ارسالی",
                                                 $"فایل صوتیی همسان با فایل ارسالی پیشتر آپلود شده است.{ Environment.NewLine}" +
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

                                            string fileNameWithoutExtension = $"{audio.PoemId}-{defProfile.FileSuffixWithoutDash}";
                                            int tmp = 1;
                                            while
                                            (
                                            File.Exists(Path.Combine(targetPathForAudioFiles, $"{fileNameWithoutExtension}.mp3"))
                                            ||
                                            File.Exists(Path.Combine(targetPathForXmlFiles, $"{fileNameWithoutExtension}.xml"))
                                            )
                                            {
                                                fileNameWithoutExtension = $"{audio.PoemId}-{defProfile.FileSuffixWithoutDash}{tmp}";
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
                                                 $"فایل mp3 متناظر یافت نشد(توجه فرمایید که همنامی اهمیت ندارد و فایل mp3 ارسالی باید دقیقاً همان فایلی باشد که همگامی با آن صورت گرفته است.اگر بعداً آن را جایگزین کرده‌اید مشخصات آن با مشخصات درج شده در فایل xml همسان نخواهد بود).{ Environment.NewLine}" +
                                                 $"{file.FileName}"
                                             );
                                            }
                                            else
                                            {
                                                File.Move(mp3file.FilePath, localMp3FilePath);
                                                int mp3fileSize = File.ReadAllBytes(localMp3FilePath).Length;

                                                bool replace = false;
                                                if(session.SessionType == UploadSessionType.ReplaceAudio)
                                                {
                                                    Recitation existing =  await context.Recitations.Where(r => r.OwnerId == session.UseId && r.GanjoorPostId == audio.PoemId && r.AudioArtist == defProfile.ArtistName).FirstOrDefaultAsync();
                                                    if(existing != null)
                                                    {
                                                        replace = true;

                                                        File.Move(localXmlFilePath, existing.LocalXmlFilePath, true);
                                                        File.Move(localMp3FilePath, existing.LocalMp3FilePath, true);
                                                        existing.Mp3FileCheckSum = audio.FileCheckSum;
                                                        existing.Mp3SizeInBytes = mp3fileSize;
                                                        existing.FileLastUpdated = session.UploadEndTime;
                                                        existing.AudioSyncStatus = AudioSyncStatus.SoundOrXMLFilesChanged;

                                                        context.Recitations.Update(existing);

                                                        await context.SaveChangesAsync();

                                                        _backgroundTaskQueue.QueueBackgroundWorkItem
                                                            (
                                                            async token =>
                                                            {
                                                            using (RMuseumDbContext publishcontext = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
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


                                                if(!replace)
                                                {
                                                    Guid legacyAudioGuid = audio.SyncGuid;
                                                    while (
                                                        (await context.Recitations.Where(a => a.LegacyAudioGuid == legacyAudioGuid).FirstOrDefaultAsync()) != null
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
                                                 $"{exp}{ Environment.NewLine}" +
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
                                if(!file.ProcessResult && string.IsNullOrEmpty(file.ProcessResultMsg))
                                {
                                    file.ProcessResultMsg = "فایل xml یا mp3 متناظر این فایل یافت نشد.";
                                    context.Update(file);

                                    await new RNotificationService(context).PushNotification
                                             (
                                                 session.UseId,
                                                 "خطا در پردازش فایل ارسالی",
                                                 $"فایل mp3 متناظر یافت نشد(توجه فرمایید که همنامی اهمیت ندارد و فایل mp3 ارسالی باید دقیقاً همان فایلی باشد که همگامی با آن صورت گرفته است.اگر بعداً آن را جایگزین کرده‌اید مشخصات آن با مشخصات درج شده در فایل xml همسان نخواهد بود).{ Environment.NewLine}" +
                                                 $"{file.FileName}"
                                             );

                                }
                                if(File.Exists(file.FilePath))
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
                       
                    }
                    );

                   

                return new RServiceResult<UploadSession>(session);

            }
            catch (Exception exp)
            {
                return new RServiceResult<UploadSession>(null, exp.ToString());
            }
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
            try
            {
                Recitation narration = await _context.Recitations.Include(a => a.Owner).Where(a => a.Id == id).SingleOrDefaultAsync();
                if (narration == null)
                    return new RServiceResult<RecitationViewModel>(null, "404");
                if (narration.ReviewStatus != AudioReviewStatus.Draft && narration.ReviewStatus != AudioReviewStatus.Pending)
                    return new RServiceResult<RecitationViewModel>(null, "خوانش می‌بایست در وضعیت پیش‌نویس یا در انتظار بازبینی باشد.");
                narration.ReviewDate = DateTime.Now;
                narration.ReviewerId = moderatorId;
                if(model.Result != PoemNarrationModerationResult.MetadataNeedsFixation)
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
                        using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
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
                }

                


                return new RServiceResult<RecitationViewModel>(new RecitationViewModel(narration, narration.Owner, await _context.GanjoorPoems.Where(p => p.Id == narration.GanjoorPostId).SingleOrDefaultAsync()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<RecitationViewModel>(null, exp.ToString());
            }
        }

        private SftpClient _client = null;

        private void _ensureSftpClientConnection()
        {
            if (_client == null)
            {
                _client = new SftpClient
                            (
                                Configuration.GetSection("AudioSFPServer")["Host"],
                                int.Parse(Configuration.GetSection("AudioSFPServer")["Port"]),
                                Configuration.GetSection("AudioSFPServer")["Username"],
                                Configuration.GetSection("AudioSFPServer")["Password"]
                                );
                _client.Connect();
            }
            else
            if(!_client.IsConnected)
            {
                _client.Connect();
            }
        }

        #region Remote Update
        private async Task _PublishNarration(Recitation narration, RecitationPublishingTracker tracker, RMuseumDbContext context)
        {
            
            try
            {
                bool replace = narration.AudioSyncStatus == AudioSyncStatus.SoundOrXMLFilesChanged;

                _ensureSftpClientConnection();

                using var x = File.OpenRead(narration.LocalXmlFilePath);
                _client.UploadFile(x, $"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteXMLFilePath}", true);

                tracker.XmlFileCopied = true;
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                using var s = File.OpenRead(narration.LocalMp3FilePath);
                _client.UploadFile(s, $"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteMp3FilePath}", true);

                tracker.Mp3FileCopied = true;
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();


                if (!replace)
                {
                    string sql = $"INSERT INTO ganja_gaudio (audio_post_ID,audio_order,audio_xml,audio_ogg,audio_mp3,audio_title,audio_artist," +
                    $"audio_artist_url,audio_src,audio_src_url, audio_guid, audio_fchecksum, audio_mp3bsize, audio_oggbsize, audio_date) VALUES " +
                    $"({narration.GanjoorPostId},{narration.AudioOrder},'{narration.RemoteXMLFilePath}', '', '{narration.Mp3Url}', '{narration.AudioTitle}', '{narration.AudioArtist}', " +
                    $"'{narration.AudioArtistUrl}', '{narration.AudioSrc}', '{narration.AudioSrcUrl}', '{narration.LegacyAudioGuid}', '{narration.Mp3FileCheckSum}', {narration.Mp3SizeInBytes}, 0, NOW())";

                    using (MySqlConnection connection = new MySqlConnection
                    (
                    $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8"
                    ))
                    {
                        await connection.OpenAsync();

                        using (MySqlDataAdapter src = new MySqlDataAdapter(
                        $"SELECT * FROM ganja_gaudio WHERE audio_post_ID = {narration.GanjoorPostId} AND audio_guid = '{narration.LegacyAudioGuid}'",
                        connection
                        ))
                        {
                            using (DataTable srcData = new DataTable())
                            {
                                await src.FillAsync(srcData);
                                if(srcData.Rows.Count == 0)//prevent duplicated insertions (this process might have caused an exception previously and caused the record to become existing prior)
                                {
                                    using (MySqlCommand cmd = new MySqlCommand(sql, connection))
                                    {
                                        await cmd.ExecuteNonQueryAsync();
                                        int AudioId = (int)cmd.LastInsertedId;
                                        narration.GanjoorAudioId = AudioId;
                                        context.Recitations.Update(narration);
                                        await context.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        
                    }

                    tracker.FirstDbUpdated = true;
                    context.RecitationPublishingTrackers.Update(tracker);
                    await context.SaveChangesAsync();

                    //We are using two database for different purposes on the remote
                    using (MySqlConnection connection = new MySqlConnection
                    (
                    $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["2ndUsername"]};pwd={Configuration.GetSection("AudioMySqlServer")["2ndPassword"]};database={Configuration.GetSection("AudioMySqlServer")["2ndDatabase"]};charset=utf8"
                    ))
                    {
                        await connection.OpenAsync();

                        using (MySqlDataAdapter src = new MySqlDataAdapter(
                        $"SELECT * FROM ganja_gaudio WHERE audio_post_ID = {narration.GanjoorPostId} AND audio_guid = '{narration.LegacyAudioGuid}'",
                        connection
                        ))
                        {
                            using (DataTable srcData = new DataTable())
                            {
                                await src.FillAsync(srcData);
                                if (srcData.Rows.Count == 0)//prevent duplicated insertions
                                {
                                    await connection.ExecuteAsync(sql);
                                }
                            }
                        }
                    }

                    tracker.SecondDbUpdated = true;
                    context.RecitationPublishingTrackers.Update(tracker);
                    await context.SaveChangesAsync();
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
                    "انتشار نهایی خوانش ارسالی",
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
                   "خطا در انتشار نهایی خوانش ارسالی",
                   $"انتشار خوانش ارسالی {narration.AudioTitle} با خطا مواجه شد.{Environment.NewLine}" +
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
                                            "خطا در انتشار نهایی خوانش ارسالی",
                                            $"لطفا صف انتظار را بررسی کنید.{ Environment.NewLine}"
                                        );
                    }
                }
            }

        }

        private async Task _DeleteNarrationFromRemote(Recitation narration, RecitationPublishingTracker tracker, RMuseumDbContext context)
        {

            try
            {
                _ensureSftpClientConnection();
                if(_client.Exists($"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteXMLFilePath}"))
                {
                    _client.DeleteFile($"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteXMLFilePath}");
                }
                
                if(_client.Exists($"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteMp3FilePath}"))
                {
                    _client.DeleteFile($"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteMp3FilePath}");
                }

                string sql = $"DELETE FROM ganja_gaudio WHERE audio_post_ID = {narration.GanjoorPostId} AND audio_guid = '{narration.LegacyAudioGuid}'";

                using (MySqlConnection connection = new MySqlConnection
                (
                $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8"
                ))
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync(sql);
                }

                //We are using two database for different purposes on the remote
                using (MySqlConnection connection = new MySqlConnection
                (
                $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["2ndUsername"]};pwd={Configuration.GetSection("AudioMySqlServer")["2ndPassword"]};database={Configuration.GetSection("AudioMySqlServer")["2ndDatabase"]};charset=utf8"
                ))
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync(sql);
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
                    $"لطفا توجه فرمایید که ممکن است ظاهر شدن تأثیر تغییرات روی سایت به دلیل تنظیمات حفظ کارایی گنجور تا یک روز طول بکشد.{Environment.NewLine}" +
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
                                            $"لطفا صف انتظار را بررسی کنید.{ Environment.NewLine}"
                                        );
                    }
                }
            }

        }

        private async Task _UpdateRemoteRecitations(Recitation narration, RecitationPublishingTracker tracker, RMuseumDbContext context, bool notify)
        {

            try
            {
                string sql = $"UPDATE ganja_gaudio SET audio_title = '{narration.AudioTitle}',audio_artist = '{narration.AudioArtist}', " +
                     $"audio_artist_url = '{narration.AudioArtistUrl}',audio_src = '{narration.AudioSrc}',audio_src_url = '{narration.AudioSrcUrl}', audio_order = {narration.AudioOrder} " +
                     $" WHERE audio_post_ID = {narration.GanjoorPostId} AND audio_guid = '{narration.LegacyAudioGuid}'";


                using (MySqlConnection connection = new MySqlConnection
                (
                $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["Username"]};pwd={Configuration.GetSection("AudioMySqlServer")["Password"]};database={Configuration.GetSection("AudioMySqlServer")["Database"]};charset=utf8"
                ))
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync(sql);
                }

                //We are using two database for different purposes on the remote
                using (MySqlConnection connection = new MySqlConnection
                (
                $"server={Configuration.GetSection("AudioMySqlServer")["Server"]};uid={Configuration.GetSection("AudioMySqlServer")["2ndUsername"]};pwd={Configuration.GetSection("AudioMySqlServer")["2ndPassword"]};database={Configuration.GetSection("AudioMySqlServer")["2ndDatabase"]};charset=utf8"
                ))
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync(sql);
                }


                narration.AudioSyncStatus = AudioSyncStatus.SynchronizedOrRejected;
                context.Recitations.Update(narration);
                await context.SaveChangesAsync();

                tracker.Finished = true;
                tracker.FinishDate = DateTime.Now;
                context.RecitationPublishingTrackers.Update(tracker);
                await context.SaveChangesAsync();

                if(notify)
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

                if(notify)
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
                                            $"لطفا صف انتظار را بررسی کنید.{ Environment.NewLine}"
                                        );
                    }
                }
            }

        }

        #endregion



        /// <summary>
        /// retry publish unpublished narrations
        /// </summary>
        public async Task RetryPublish()
        {
            if (_backgroundTaskQueue.Count > 0)
                return;

            var unpublishedQueue =  await _context.RecitationPublishingTrackers.ToArrayAsync();
            if (unpublishedQueue.Length > 0)
            {
                _context.RecitationPublishingTrackers.RemoveRange(unpublishedQueue);
                await _context.SaveChangesAsync();
            }

            _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                    async token =>
                    {
                        using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
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


        /// <summary>
        /// Get Upload Session (including files)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSession>> GetUploadSession(Guid id)
        {
            try
            {
                return new RServiceResult<UploadSession>
                    (
                    await _context.UploadSessions.Include(s => s.UploadedFiles).FirstOrDefaultAsync(s => s.Id == id)
                    );

            }
            catch (Exception exp)
            {
                return new RServiceResult<UploadSession>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get User Profiles
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="artistName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel[]>> GetUserNarrationProfiles(Guid userId, string artistName)
        {
            try
            {
                List<UserRecitationProfileViewModel> profiles = new List<UserRecitationProfileViewModel>();

                foreach (UserRecitationProfile p in 
                    (
                    await _context.UserRecitationProfiles.Include(p => p.User)
                    .Where(p => p.UserId == userId && p.IsDefault == true && 
                    (string.IsNullOrEmpty(artistName) || (!string.IsNullOrEmpty(artistName) && p.ArtistName.Contains(artistName)))
                    ).ToArrayAsync())
                    )
                {
                    profiles.Add(new UserRecitationProfileViewModel(p));
                }

                foreach (UserRecitationProfile p in (await _context.UserRecitationProfiles.Include(p => p.User).Where(p => p.UserId == userId && p.IsDefault == false
                &&
                    (string.IsNullOrEmpty(artistName) || (!string.IsNullOrEmpty(artistName) && p.ArtistName.Contains(artistName)))
                ).ToArrayAsync()))
                {
                    profiles.Add(new UserRecitationProfileViewModel(p));
                }
                return new RServiceResult<UserRecitationProfileViewModel[]>(profiles.ToArray());

            }
            catch (Exception exp)
            {
                return new RServiceResult<UserRecitationProfileViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get User Default Profile
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel>> GetUserDefProfile(Guid userId)
        {
            try
            {
                var defProfile = await _context.UserRecitationProfiles.Include(p => p.User).Where(p => p.UserId == userId && p.IsDefault == true).FirstOrDefaultAsync();
                if (defProfile == null)
                    return new RServiceResult<UserRecitationProfileViewModel>(null);
                return new RServiceResult<UserRecitationProfileViewModel>(new UserRecitationProfileViewModel(defProfile));
            }
            catch (Exception exp)
            {
                return new RServiceResult<UserRecitationProfileViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// validating narration profile
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static string GetUserProfileValidationError(UserRecitationProfile p)
        {
            if(string.IsNullOrEmpty(p.Name))
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
                return  $"نام خوانشگر فقط باید شامل حروف فارسی و فاصله باشد. اولین حرف غیرمجاز = {s}";
            }

            s = LanguageUtils.GetFirstNotMatchingCharacter(p.ArtistUrl, LanguageUtils.EnglishLowerCaseAlphabet, LanguageUtils.EnglishLowerCaseAlphabet.ToUpper() + @":/._-0123456789%");

            if(!string.IsNullOrEmpty(p.ArtistUrl))
            {
                bool result = Uri.TryCreate(p.ArtistUrl, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (!result)
                {
                    return $"نشانی وب خوانشگر نامعتبر است.";
                }
            }
            

            s = LanguageUtils.GetFirstNotMatchingCharacter(p.AudioSrcUrl, LanguageUtils.EnglishLowerCaseAlphabet, LanguageUtils.EnglishLowerCaseAlphabet.ToUpper() + @":/._-0123456789%");
            
            if(!string.IsNullOrEmpty(p.AudioSrcUrl))
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




        /// <summary>
        /// Add a narration profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel>> AddUserNarrationProfiles(UserRecitationProfileViewModel profile)
        {
            try
            {
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
                if(error != "")
                {
                    return new RServiceResult<UserRecitationProfileViewModel>(null, error);
                }

                if((await _context.UserRecitationProfiles.Where(e => e.UserId == p.Id && e.Name == p.Name).SingleOrDefaultAsync())!=null)
                {
                    return new RServiceResult<UserRecitationProfileViewModel>(null, "شما نمایهٔ دیگری با همین نام دارید.");
                }

                await _context.UserRecitationProfiles.AddAsync(p);
                   
                await _context.SaveChangesAsync();
                if(p.IsDefault)
                {
                    foreach(var o in _context.UserRecitationProfiles.Where(o => o.Id != p.Id && o.UserId == p.UserId && o.IsDefault).Select(o => o))
                    {
                        o.IsDefault = false;
                        _context.UserRecitationProfiles.Update(o);
                    }
                    await _context.SaveChangesAsync();
                }
                return new RServiceResult<UserRecitationProfileViewModel>(new UserRecitationProfileViewModel(p));
            }
            catch (Exception exp)
            {
                return new RServiceResult<UserRecitationProfileViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Update a narration profile 
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserRecitationProfileViewModel>> UpdateUserNarrationProfiles(UserRecitationProfileViewModel profile)
        {
            try
            {

                var p = await _context.UserRecitationProfiles.Where(p => p.Id == profile.Id).SingleOrDefaultAsync();

                if (p.UserId != profile.UserId)
                    return new RServiceResult<UserRecitationProfileViewModel>(null, "permission error");

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
                return new RServiceResult<UserRecitationProfileViewModel>(new UserRecitationProfileViewModel(p));
            }
            catch (Exception exp)
            {
                return new RServiceResult<UserRecitationProfileViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Delete a narration profile 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteUserNarrationProfiles(Guid id, Guid userId)
        {
            try
            {

                var p = await _context.UserRecitationProfiles.Where(p => p.Id == id).SingleOrDefaultAsync();

                if (p.UserId != userId)
                    return new RServiceResult<bool>(false);

                _context.UserRecitationProfiles.Remove(p);

                await _context.SaveChangesAsync();
                
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Get uploads descending by upload time
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId">if userId is empty all user uploads would be returned</param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, UploadedItemViewModel[] Items)>> GetUploads(PagingParameterModel paging, Guid userId)
        {
            try
            {
                var source =
                    (
                    from file in _context.UploadedFiles
                    join session in _context.UploadSessions.Include(s => s.User)
                    on file.UploadSessionId equals session.Id
                    where userId == Guid.Empty || session.UseId == userId
                    orderby session.UploadEndTime descending
                    select new UploadedItemViewModel()
                    { FileName = file.FileName, ProcessResult = file.ProcessResult, ProcessResultMsg = file.ProcessResultMsg, UploadEndTime = session.UploadEndTime, UserName = session.User.UserName, ProcessStartTime = session.ProcessStartTime, ProcessProgress = session.ProcessProgress, ProcessEndTime = session.ProcessEndTime }
                    ).AsQueryable();
                    
                (PaginationMetadata PagingMeta, UploadedItemViewModel[] Items) paginatedResult =
                    await QueryablePaginator<UploadedItemViewModel>.Paginate(source, paging);

               

                return new RServiceResult<(PaginationMetadata PagingMeta, UploadedItemViewModel[] Items)>((paginatedResult.PagingMeta, paginatedResult.Items));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, UploadedItemViewModel[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
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
            try
            {
                var source =
                      from tracker in _context.RecitationPublishingTrackers
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
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RecitationPublishingTrackerViewModel[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
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
            try
            {
                var recitations = await _context.Recitations.Where(r => r.OwnerId == currentOwenerId && r.AudioArtist == artistName && r.ReviewStatus == AudioReviewStatus.Approved).ToListAsync();
                foreach(Recitation recitation in recitations)
                {
                    recitation.OwnerId = newOwnerId;
                }
                _context.Recitations.UpdateRange(recitations);
                var profiles = await _context.UserRecitationProfiles.Where(r => r.UserId == currentOwenerId && r.ArtistName == artistName).ToListAsync();
                foreach(UserRecitationProfile profile in profiles)
                {
                    profile.IsDefault = false;
                    profile.UserId = newOwnerId;
                }
                _context.UserRecitationProfiles.UpdateRange(profiles);
                await _context.SaveChangesAsync();

                var defProfile = await _context.UserRecitationProfiles.Where(r => r.UserId == newOwnerId && r.IsDefault == true).FirstOrDefaultAsync();
                if(defProfile == null)
                {
                    var firstProfile = await _context.UserRecitationProfiles.Where(r => r.UserId == newOwnerId).FirstOrDefaultAsync();
                    if(firstProfile != null)
                    {
                        firstProfile.IsDefault = true;
                        _context.UserRecitationProfiles.Update(firstProfile);
                        await _context.SaveChangesAsync();
                    }
                }

                var user = await _userService.GetUserInformation(currentOwenerId);

                await new RNotificationService(_context).PushNotification
                                            (
                                                newOwnerId,
                                                "انتقال مالکیت خوانش‌ها به شما",
                                                $"مالکیت {recitations.Count} خوانش تأیید شده از خوانشگری به نام «{artistName}» توسط کاربری با پست الکترونیکی «{user.Result.Email}» به شما منتقل شد.{ Environment.NewLine}"
                                            );

                return new RServiceResult<int>(recitations.Count);
            }
            catch(Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// move recitaions of an artist to the first position
        /// </summary>
        /// <param name="artistName"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> MakeArtistRecitationsFirst(string artistName)
        {
            try
            {
                var recitations = await _context.Recitations.Where(r => r.AudioArtist == artistName && r.AudioOrder != 1).ToListAsync();
                foreach (Recitation recitation in recitations)
                {
                    var otherRecitaions = await _context.Recitations.Where(r => r.GanjoorPostId == recitation.GanjoorPostId && r.AudioArtist != artistName).ToListAsync();

                    foreach(Recitation other in otherRecitaions)
                    {
                        other.AudioOrder = other.AudioOrder + 1;
                        other.AudioSyncStatus = AudioSyncStatus.MetadataChanged;
                        _context.Recitations.Update(other);

                        RecitationPublishingTracker tracker = new RecitationPublishingTracker()
                        {
                            PoemNarrationId = other.Id,
                            StartDate = DateTime.Now,
                            XmlFileCopied = false,
                            Mp3FileCopied = false,
                            FirstDbUpdated = false,
                            SecondDbUpdated = false,
                        };
                        _context.RecitationPublishingTrackers.Add(tracker);

                        await _context.SaveChangesAsync();

                        await _UpdateRemoteRecitations(other, tracker, _context, false /* owner of this recitation did nothing to expect any notifications*/);
                    }

                    recitation.AudioOrder = 1;
                    recitation.AudioSyncStatus = AudioSyncStatus.MetadataChanged;
                    _context.Recitations.Update(recitation);

                    RecitationPublishingTracker trackerMain = new RecitationPublishingTracker()
                    {
                        PoemNarrationId = recitation.Id,
                        StartDate = DateTime.Now,
                        XmlFileCopied = false,
                        Mp3FileCopied = false,
                        FirstDbUpdated = false,
                        SecondDbUpdated = false,
                    };
                    _context.RecitationPublishingTrackers.Add(trackerMain);

                    await _context.SaveChangesAsync();

                    await _UpdateRemoteRecitations(recitation, trackerMain, _context, true);
                }
                

                return new RServiceResult<int>(recitations.Count);
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// Synchronization Queue
        /// </summary>
        /// <param name="filteredUserId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RecitationViewModel[]>> GetSynchronizationQueue(Guid filteredUserId)
        {
            try
            {
                var source =
                     from audio in _context.Recitations.Include(a => a.Owner)
                     join poem in _context.GanjoorPoems
                     on audio.GanjoorPostId equals poem.Id
                     where
                     (filteredUserId == Guid.Empty || audio.OwnerId == filteredUserId)
                     &&
                     audio.ReviewStatus == AudioReviewStatus.Approved
                     &&
                     audio.AudioSyncStatus != AudioSyncStatus.SynchronizedOrRejected
                     orderby audio.UploadDate descending
                     select new RecitationViewModel(audio, audio.Owner, poem);

                return new RServiceResult<RecitationViewModel[]>(await source.ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RecitationViewModel[]>(null, exp.ToString());
            }
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
                    return bool.Parse(Configuration.GetSection("AudioUploadService")["Enabled"]);
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
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="notificationService"></param>
        /// <param name="userService"></param>
        public RecitationService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue, IRNotificationService notificationService, IAppUserService userService)
        {
            _context = context;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
            _notificationService = notificationService;
            _userService = userService;
        }
    }
}
