using Dapper;
using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using RMuseum.DbContext;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
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
        /// return selected narration information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemNarrationViewModel>> Get(Guid id)
        {
            try
            {
                var narration = await _context.AudioFiles.Where(a => a.Id == id).SingleOrDefaultAsync();
                if(narration == null)
                {
                    return new RServiceResult<PoemNarrationViewModel>(null);
                }
                return new RServiceResult<PoemNarrationViewModel>(new PoemNarrationViewModel(narration));
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemNarrationViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// updates metadata for narration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdatePoemNarration(Guid id, PoemNarrationUpdateViewModel metadata)
        {
            try
            {
                PoemNarration narration =  await _context.AudioFiles.Where(a => a.Id == id).SingleOrDefaultAsync();
                if(narration == null)
                    return new RServiceResult<bool>(false, "404");
                narration.AudioTitle = metadata.AudioTitle;
                narration.AudioArtist = metadata.AudioArtist;
                narration.AudioArtistUrl = metadata.AudioArtistUrl;
                narration.AudioSrc = metadata.AudioSrc;
                narration.AudioSrcUrl = metadata.AudioSrcUrl;
                narration.ReviewStatus = metadata.ReviewStatus;
                _context.AudioFiles.Update(narration);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
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
                UserNarrationProfile defProfile = await _context.UserNarrationProfiles.Where(p => p.UserId == userId && p.IsDefault == true).FirstOrDefaultAsync();
                if (defProfile == null)
                {
                    return new RServiceResult<UploadSession>(null, "نمایهٔ پیش‌فرض شما مشخص نیست. لطفا پیش از ارسال خوانش نمایهٔ پیش‌فرض خود را تعریف کنید.");
                }

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

                            UserNarrationProfile defProfile = await context.UserNarrationProfiles.Where(p => p.UserId == session.UseId && p.IsDefault == true).FirstOrDefaultAsync(); //this should not be null

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
                                                    GanjoorPostId = audio.PoemId,
                                                    OwnerId = session.UseId,
                                                    GanjoorAudioId = 1 + await context.AudioFiles.OrderByDescending(a => a.GanjoorAudioId).Select(a => a.GanjoorAudioId).FirstOrDefaultAsync(),
                                                    AudioOrder = 1 + await context.AudioFiles.Where(a => a.GanjoorPostId == audio.PoemId).OrderByDescending(a => a.GanjoorAudioId).Select(a => a.GanjoorAudioId).FirstOrDefaultAsync(),
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

                            await _notificationService.PushNotification
                            (
                                session.UseId,
                                "پایان پردازش خوانش بارگذاری شده",
                                $"پردازش خوانشهای بارگذاری شدهٔ اخیر شما تکمیل شده است.{Environment.NewLine}" +
                                $"می‌توانید با مراجعه به این صفحه TODO: client url وضعیت آنها را بررسی و ذر صورت عدم وجود خطا تقاضای بررسی آنها توسط ناظران را ثبت کنید."
                            );
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
        public async Task<RServiceResult<bool>> ModeratePoemNarration(Guid id, Guid moderatorId, PoemNarrationModerateViewModel model)
        {
            try
            {
                PoemNarration narration = await _context.AudioFiles.Where(a => a.Id == id).SingleOrDefaultAsync();
                if (narration == null)
                    return new RServiceResult<bool>(false, "404");
                if (narration.ReviewStatus != AudioReviewStatus.Pending)
                    return new RServiceResult<bool>(false, "خوانش می‌بایست در وضعیت در انتظار بازبینی باشد.");
                narration.ReviewDate = DateTime.Now;
                narration.ReviewerId = moderatorId;
                if(model.Result != PoemNarrationModerationResult.MetadataNeedsFixation)
                {
                    narration.ReviewStatus = model.Result == PoemNarrationModerationResult.Approve ? AudioReviewStatus.Approved : AudioReviewStatus.Rejected;
                }
                if (narration.ReviewStatus == AudioReviewStatus.Rejected)
                {
                    narration.AudioSyncStatus = (int)AudioSyncStatus.SynchronizedOrRejected;
                    //TODO: delete rejected items files passed a certain period of time in a maintenance job
                }
                narration.ReviewMsg = model.Message;
                _context.AudioFiles.Update(narration);
                await _context.SaveChangesAsync();

                if (model.Result == PoemNarrationModerationResult.MetadataNeedsFixation)
                {
                    await _notificationService.PushNotification
                         (
                             narration.OwnerId,
                             "نیاز به بررسی خوانش ارسالی",
                             $"خوانش شما بررسی شده و نیاز به اعمال تغییرات دارد.{Environment.NewLine}" +
                             $"می‌توانید با مراجعه به این صفحه TODO: client url وضعیت آن را بررسی کنید."
                         );
                }
                else
                if (narration.ReviewStatus == AudioReviewStatus.Rejected) 
                {
                    await _notificationService.PushNotification
                         (
                             narration.OwnerId,
                             "عدم پذیرش خوانش ارسالی",
                             $"خوانش ارسالی شما قابل پذیرش نبود.{Environment.NewLine}" +
                             $"می‌توانید با مراجعه به این صفحه TODO: client url وضعیت آن را بررسی کنید."
                         );
                }
                else //approved:
                {
                    _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                    async token =>
                    {
                        using var client = new SftpClient
                        (
                            Configuration.GetSection("AudioSFPServer")["Host"],
                            int.Parse(Configuration.GetSection("AudioSFPServer")["Port"]),
                            Configuration.GetSection("AudioSFPServer")["Username"],
                            Configuration.GetSection("AudioSFPServer")["Password"]
                            );
                        try
                        {
                            client.Connect();

                            using var x = File.OpenRead(narration.LocalXmlFilePath);
                            client.UploadFile(x, $"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteXMLFilePath}", true);

                            using var s = File.OpenRead(narration.LocalMp3FilePath);
                            client.UploadFile(s, $"{Configuration.GetSection("AudioSFPServer")["RootPath"]}{narration.RemoteMp3FilePath}", true);

                            string sql = $"INSERT INTO ganja_gaudio (audio_post_ID,audio_order,audio_xml,audio_ogg,audio_mp3,audio_title,audio_artist," +
                                    $"audio_artist_url,audio_src,audio_src_url, audio_guid, audio_fchecksum, audio_mp3bsize, audio_oggbsize, audio_date) VALUES " +
                                    $"({narration.GanjoorPostId},{narration.AudioOrder},'{narration.RemoteXMLFilePath}', '', '{narration.Mp3Url}', '{narration.AudioTitle}', '{narration.AudioArtist}', " +
                                    $"'{narration.AudioArtistUrl}', '{narration.AudioSrc}', '{narration.AudioSrcUrl}', '{narration.LegacyAudioGuid}', '{narration.Mp3FileCheckSum}', {narration.Mp3SizeInBytes}, 0, '{$"{narration.ReviewDate:0:u}".Replace("Z", "")}')";

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

                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration)) //this is long running job, so _context might be already been freed/collected by GC
                            {
                                narration.AudioSyncStatus = (int)AudioSyncStatus.SynchronizedOrRejected;
                                context.AudioFiles.Update(narration);
                                await context.SaveChangesAsync();
                            }

                         await _notificationService.PushNotification
                         (
                             narration.OwnerId,
                             "انتشار خوانش ارسالی",
                             $"خوانش ارسالی شما منتشر شد.{Environment.NewLine}" +
                             $"می‌توانید با مراجعه به این صفحه TODO: client url وضعیت آن را بررسی کنید."
                         );


                        }
                        catch(Exception exp)
                        {
                            //if an error occurs, narration.AudioSyncStatus is not updated and narration can be idetified later to do "retry" attempts
                            await _notificationService.PushNotification
                        (
                            narration.OwnerId,
                            "خطا در پردازش نهایی",
                            $"{exp}{Environment.NewLine}" +
                            $"می‌توانید با مراجعه به این صفحه TODO: client url وضعیت آن را بررسی کنید."
                        );
                        }
                        finally
                        {
                            client.Disconnect();
                        }
                       
                    });
                }

                


                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
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
        /// validating narration profile
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static string GetUserProfileValidationError(UserNarrationProfile p)
        {
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

            string s = LanguageUtils.GetFirstNotMatchingCharacter(p.ArtistName, LanguageUtils.PersianAlphabet, " ");
            if (s != "")
            {
                return  $"نام فقط باید شامل حروف فارسی و فاصله باشد. اولین حرف غیرمجاز = {s}";
            }

            s = LanguageUtils.GetFirstNotMatchingCharacter(p.ArtistUrl, LanguageUtils.EnglishLowerCaseAlphabet, LanguageUtils.EnglishLowerCaseAlphabet.ToUpper() + @":/.");

            if (s != "")
            {
                return $"نشانی وب خوانشگر شامل حروف غیر مجاز است. اولین حرف غیرمجاز = {s}";
            }

            s = LanguageUtils.GetFirstNotMatchingCharacter(p.AudioSrcUrl, LanguageUtils.EnglishLowerCaseAlphabet, LanguageUtils.EnglishLowerCaseAlphabet.ToUpper() + @":/.");
            if (s != "")
            {
                return $"نشانی وب منبع شامل حروف غیر مجاز است. اولین حرف غیرمجاز = {s}";
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
        public async Task<RServiceResult<UserNarrationProfileViewModel>> AddUserNarrationProfiles(UserNarrationProfileViewModel profile)
        {
            try
            {
                var p =  new UserNarrationProfile()
                {
                    UserId = profile.UserId,
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
                    return new RServiceResult<UserNarrationProfileViewModel>(null, error);
                }

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

                p.ArtistName = profile.ArtistName.Trim();
                p.ArtistUrl = profile.ArtistUrl.Trim();
                p.AudioSrc = profile.AudioSrc.Trim();
                p.AudioSrcUrl = profile.AudioSrcUrl.Trim();
                p.FileSuffixWithoutDash = profile.FileSuffixWithoutDash.Trim();
                p.IsDefault = profile.IsDefault;

                string error = GetUserProfileValidationError(p);
                if (error != "")
                {
                    return new RServiceResult<UserNarrationProfileViewModel>(null, error);
                }

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
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, dynamic[] Items)>> GetUploads(PagingParameterModel paging, Guid userId)
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
                    select new
                    { file.FileName, file.ProcessResult, file.ProcessResultMsg, session.UploadEndTime, session.User.UserName, session.ProcessStartTime, session.ProcessProgress, session.ProcessEndTime }
                    ).AsQueryable();
                    
                (PaginationMetadata PagingMeta, dynamic[] Items) paginatedResult =
                    await QueryablePaginator<dynamic>.Paginate(source, paging);

               

                return new RServiceResult<(PaginationMetadata PagingMeta, dynamic[] Items)>((paginatedResult.PagingMeta, paginatedResult.Items));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, dynamic[] Items)>((PagingMeta: null, Items: null), exp.ToString());
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
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="notificationService"></param>
        public AudioNarrationService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue, IRNotificationService notificationService)
        {
            _context = context;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
            _notificationService = notificationService;
        }
    }
}
