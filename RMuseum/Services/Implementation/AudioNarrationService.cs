using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using RMuseum.DbContext;
using RMuseum.Migrations;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
using RMuseum.Models.UploadSession.ViewModels;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{

    /// <summary>
    /// Audio Narration Service Implementation
    /// </summary>
    public class AudioNarrationService : IAudioNarrationService
    {
        /// <summary>
        /// returns list of narrations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filteredUserId">send Guid.Empty if you want all narrations</param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, PoemNarrationViewModel[] Items)>> GetAll(PagingParameterModel paging, Guid filteredUserId, AudioReviewStatus status)
        {
            try
            {
                var source =
                    
                     _context.AudioFiles
                     .Include(a => a.Owner)
                     .Where(a => 
                            (filteredUserId == Guid.Empty || a.OwnerId == filteredUserId)
                            &&
                            (status == AudioReviewStatus.All || a.ReviewStatus == status)
                     )
                    .OrderByDescending(a => a.UploadDate)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, PoemNarration[] Items) paginatedResult =
                    await QueryablePaginator<PoemNarration>.Paginate(source, paging);

                List<PoemNarrationViewModel> res = new List<PoemNarrationViewModel>();
                foreach(PoemNarration audio in paginatedResult.Items)
                {
                    res.Add(new PoemNarrationViewModel(audio));
                }

                return new RServiceResult<(PaginationMetadata PagingMeta, PoemNarrationViewModel[] Items)>((paginatedResult.PagingMeta, res.ToArray()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, PoemNarrationViewModel[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
        }

        /// <summary>
        /// imports data from ganjoor MySql database
        /// </summary>
        /// <param name="OwnrRAppUserId">User Id which becomes owner of imported data</param>
        public async Task<RServiceResult<bool>> OneTimeImport(Guid OwnrRAppUserId)
        {
            try
            {
                PoemNarration sampleCheck = await _context.AudioFiles.FirstOrDefaultAsync();
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
                    using(MySqlDataAdapter src = new MySqlDataAdapter(
                        "SELECT audio_ID, audio_post_ID, audio_order, audio_xml, audio_ogg, audio_mp3, " +
                        "audio_title,  audio_artist, audio_artist_url, audio_src,  audio_src_url, audio_guid, " +
                        "audio_fchecksum,  audio_mp3bsize,  audio_oggbsize,  audio_date " +
                        "FROM adab.ganja_gaudio ORDER BY audio_ID",
                        connection
                        ))
                    {
                        using(DataTable srcData = new DataTable())
                        {
                            await src.FillAsync(srcData);

                            int audioSyncStatus = (int)AudioSyncStatus.SynchronizedOrRejected;
                            foreach (DataRow row in srcData.Rows)
                            {
                                PoemNarration newRecord = new PoemNarration()
                                {
                                    OwnerId = OwnrRAppUserId,
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
                                    AudioSyncStatus = audioSyncStatus,
                                    ReviewStatus = AudioReviewStatus.Approved
                                };
                                newRecord.ReviewDate = newRecord.UploadDate;
                                string audio_xml = row["audio_xml"].ToString();
                                //sample audio_xml value: /i/a/x/11876-Simorgh.xml
                                audio_xml = audio_xml.Substring("/i/".Length); // /i/a/x/11876-Simorgh.xml -> a/x/11876-Simorgh.xml
                                newRecord.SoundFilesFolder = audio_xml.Substring(0, audio_xml.IndexOf('/')); //(a)
                                newRecord.FileNameWithoutExtension = Path.GetFileNameWithoutExtension(audio_xml.Substring(audio_xml.LastIndexOf('/') + 1)); //(11876-Simorgh)

                                _context.AudioFiles.Add(newRecord);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
           
        }

        /// <summary>
        /// Initiate New Upload Session for audio
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UploadSession>> InitiateNewUploadSession(Guid userId)
        {
            try
            {
                UploadSession session = new UploadSession()
                {
                    SessionType = UploadSessionType.Audio,
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
                    ProcessResultMsg = "پردازش نشده (فایلهای mp3‌ که مشخصات آنها در فایلهای xml ارسالی یافت نشود پردازش نمی‌شوند)."
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
                            List<UploadSessionFile> mp3files = new List<UploadSessionFile>();
                            foreach (UploadSessionFile file in session.UploadedFiles.Where(file => Path.GetExtension(file.FilePath) == ".mp3").ToList())
                            {
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

                            UserNarrationProfile defProfile = await context.UserNarrationProfiles.Where(p => p.UserId == session.UseId && p.IsDefault == true).FirstOrDefaultAsync();
                            if (defProfile == null)
                            {
                                defProfile = new UserNarrationProfile()
                                {
                                    FileSuffixWithoutDash = !string.IsNullOrEmpty(session.User.FirstName) ? !string.IsNullOrEmpty(session.User.SureName) ?
                                                            GPersianTextSync.Farglisize($"{session.User.FirstName[0]}{session.User.SureName[0]}")
                                                            :
                                                            GPersianTextSync.Farglisize($"{session.User.FirstName[0]}") : $"{session.User.UserName[0]}",
                                    ArtistName = $"{session.User.FirstName} {session.User.SureName}",
                                    ArtistUrl = "",
                                    AudioSrc = "",
                                    AudioSrcUrl = ""

                                };
                            }

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
                                        if( await context.AudioFiles.Where(a => a.Mp3FileCheckSum == audio.FileCheckSum).SingleOrDefaultAsync() != null)
                                        {
                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "فایل صوتیی همسان با فایل ارسالی پیشتر آپلود شده است.";
                                            context.UploadSessions.Update(session);
                                        }
                                        else
                                        {
                                            string soundFilesFolder = Configuration.GetSection("AudioUploadService")["TempUploadPath"];
                                            string targetPathForAudioFiles = Configuration.GetSection("AudioUploadService")["LocalAudioRepositoryPath"];
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
                                            }
                                            else
                                            {
                                                File.Move(mp3file.FilePath, localMp3FilePath);
                                                int mp3fileSize = File.ReadAllBytes(localMp3FilePath).Length;



                                                Guid legacyAudioGuid = audio.SyncGuid;
                                                while (
                                                    (await context.AudioFiles.Where(a => a.LegacyAudioGuid == legacyAudioGuid).FirstOrDefaultAsync()) != null
                                                    )
                                                {
                                                    legacyAudioGuid = Guid.NewGuid();
                                                }


                                                PoemNarration narration = new PoemNarration()
                                                {
                                                    OwnerId = session.UseId,
                                                    GanjoorAudioId = 1 + await context.AudioFiles.OrderByDescending(a => a.GanjoorAudioId).Select(a => a.GanjoorAudioId).FirstOrDefaultAsync(),
                                                    AudioOrder = 1 + await context.AudioFiles.Where(a => a.GanjoorPostId == audio.PoemId).OrderByDescending(a => a.GanjoorAudioId).Select(a => a.GanjoorAudioId).FirstOrDefaultAsync(),
                                                    FileNameWithoutExtension = fileNameWithoutExtension,
                                                    SoundFilesFolder = Configuration.GetSection("AudioUploadService")["CurrentSoundFilesFolder"],
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
                                                    LocalMp3FilePath = localMp3FilePath,
                                                    LocalXmlFilePath = localXmlFilePath,
                                                    AudioSyncStatus = (int)AudioSyncStatus.NewItem,
                                                    ReviewStatus = AudioReviewStatus.Draft
                                                };

                                                context.AudioFiles.Add(narration);


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
                                }
                            }

                            //remove session files (house keeping)
                            foreach(UploadSessionFile file in session.UploadedFiles)
                            {
                                if(!file.ProcessResult)
                                {
                                    file.ProcessResultMsg = "فایل xml یا mp3 متناظر این فایل یافت نشد.";
                                    file.ProcessResult = true;
                                    context.Update(file);
                                    await context.SaveChangesAsync();
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
        /// <returns></returns>
        public async Task<RServiceResult<UserNarrationProfileViewModel[]>> GetUserNarrationProfiles(Guid userId)
        {
            try
            {
                List<UserNarrationProfileViewModel> profiles = new List<UserNarrationProfileViewModel>();
                
                foreach(UserNarrationProfile p in (await _context.UserNarrationProfiles.Include(p => p.User).Where(p => p.UserId == userId).ToArrayAsync()))
                {
                    profiles.Add(new UserNarrationProfileViewModel(p));
                }
                return new RServiceResult<UserNarrationProfileViewModel[]>(profiles.ToArray());

            }
            catch (Exception exp)
            {
                return new RServiceResult<UserNarrationProfileViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Add a narration profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserNarrationProfileViewModel>> AddUserNarrationProfiles(UserNarrationProfileViewModel profile)
        {
            try
            {
                var p =  new UserNarrationProfile()
                {
                    UserId = profile.UserId,
                    ArtistName = profile.ArtistName,
                    ArtistUrl = profile.ArtistUrl,
                    AudioSrc = profile.AudioSrc,
                    AudioSrcUrl = profile.AudioSrcUrl,
                    FileSuffixWithoutDash = profile.FileSuffixWithoutDash,
                    IsDefault = profile.IsDefault
                };
                await _context.UserNarrationProfiles.AddAsync(p);
                   
                await _context.SaveChangesAsync();
                if(p.IsDefault)
                {
                    foreach(var o in _context.UserNarrationProfiles.Where(o => o.Id != p.Id && o.IsDefault).Select(o => o))
                    {
                        o.IsDefault = false;
                        _context.UserNarrationProfiles.Update(o);
                    }
                    await _context.SaveChangesAsync();
                }
                return new RServiceResult<UserNarrationProfileViewModel>(new UserNarrationProfileViewModel(p));
            }
            catch (Exception exp)
            {
                return new RServiceResult<UserNarrationProfileViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Update a narration profile 
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<RServiceResult<UserNarrationProfileViewModel>> UpdateUserNarrationProfiles(UserNarrationProfileViewModel profile)
        {
            try
            {

                var p = await _context.UserNarrationProfiles.Where(p => p.Id == profile.Id).SingleOrDefaultAsync();

                if (p.UserId != profile.UserId)
                    return new RServiceResult<UserNarrationProfileViewModel>(null, "permission error");

                p.ArtistName = profile.ArtistName;
                p.ArtistUrl = profile.ArtistUrl;
                p.AudioSrc = profile.AudioSrc;
                p.AudioSrcUrl = profile.AudioSrcUrl;
                p.FileSuffixWithoutDash = profile.FileSuffixWithoutDash;
                p.IsDefault = profile.IsDefault;

                _context.UserNarrationProfiles.Update(p);

                await _context.SaveChangesAsync();
                if (p.IsDefault)
                {
                    foreach (var o in _context.UserNarrationProfiles.Where(o => o.Id != p.Id && o.IsDefault).Select(o => o))
                    {
                        o.IsDefault = false;
                        _context.UserNarrationProfiles.Update(o);
                    }
                    await _context.SaveChangesAsync();
                }
                return new RServiceResult<UserNarrationProfileViewModel>(new UserNarrationProfileViewModel(p));
            }
            catch (Exception exp)
            {
                return new RServiceResult<UserNarrationProfileViewModel>(null, exp.ToString());
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

                var p = await _context.UserNarrationProfiles.Where(p => p.Id == id).SingleOrDefaultAsync();

                if (p.UserId != userId)
                    return new RServiceResult<bool>(false);

                _context.UserNarrationProfiles.Remove(p);

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
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, UploadSessionViewModel[] Items)>> GetUploads(PagingParameterModel paging, Guid userId)
        {
            try
            {
                var source =

                     _context.UploadSessions
                     .Include(u => u.User)
                     .Include(u => u.UploadedFiles)
                     .Where(u => userId == Guid.Empty || u.UseId == userId )
                    .OrderByDescending(u => u.UploadEndTime)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, UploadSession[] Items) paginatedResult =
                    await QueryablePaginator<UploadSession>.Paginate(source, paging);

                List<UploadSessionViewModel> res = new List<UploadSessionViewModel>();
                foreach (UploadSession upload in paginatedResult.Items)
                {
                    res.Add(new UploadSessionViewModel(upload));
                }

                return new RServiceResult<(PaginationMetadata PagingMeta, UploadSessionViewModel[] Items)>((paginatedResult.PagingMeta, res.ToArray()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, UploadSessionViewModel[] Items)>((PagingMeta: null, Items: null), exp.ToString());
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
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        public AudioNarrationService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _context = context;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
        }
    }
}
