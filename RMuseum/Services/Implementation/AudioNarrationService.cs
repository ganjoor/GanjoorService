using Audit.WebApi;
using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using RMuseum.DbContext;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
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
                    .OrderBy(a => a.UploadDate)
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
                    ProcessProgress = 0
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
                if(ext != ".mp3" && ext != ".xml" && ext != ".zip")
                {
                    file.ProcessResultMsg = "تنها فایلهای با پسوند mp3، xml و zip قابل قبول هستند.";
                }
                else
                {
                    if (!Directory.Exists(Configuration.GetSection("AudioUploadService")["StoragePath"]))
                    {
                        try
                        {
                            Directory.CreateDirectory(Configuration.GetSection("AudioUploadService")["StoragePath"]);
                        }
                        catch
                        {
                            return new RServiceResult<UploadSessionFile>(null, $"ProcessImage: create dir failed {Configuration.GetSection("AudioUploadService")["StoragePath"]}");
                        }
                    }

                    string filePath = Path.Combine(Configuration.GetSection("AudioUploadService")["StoragePath"], file.FileName);
                    while(File.Exists(filePath))
                    {
                        filePath = Path.Combine(Configuration.GetSection("AudioUploadService")["StoragePath"], Guid.NewGuid().ToString() + ext);
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

                            foreach (UploadSessionFile file in session.UploadedFiles.Where(file => Path.GetExtension(file.FilePath) == ".xml").ToList())
                            {
                                try
                                {
                                    foreach (PoemAudio audio in PoemAudioListProcessor.Load(file.FilePath))
                                    {
                                        UploadSessionFile mp3file = mp3files.Where(mp3 => mp3.MP3FileCheckSum == audio.FileCheckSum).SingleOrDefault();
                                        if(mp3file == null)
                                        {
                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "فایل mp3 متناظر یافت نشد (توجه فرمایید که همنامی اهمیت ندارد و فایل mp3 ارسالی باید دقیقاً همان فایلی باشد که همگامی با آن صورت گرفته است. اگر بعداً آن را جایگزین کرده‌اید مشخصات آن با مشخصات درج شده در فایل xml همسان نخواهد بود).";
                                            context.UploadSessions.Update(session);
                                        }
                                        else
                                        {
                                            //here we should produce and save ogg file

                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "";
                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResult = true;
                                            session.UploadedFiles.Where(f => f.Id == mp3file.Id).SingleOrDefault().ProcessResultMsg = "";
                                            session.UploadedFiles.Where(f => f.Id == mp3file.Id).SingleOrDefault().ProcessResult = true;
                                        }
                                        await context.SaveChangesAsync();
                                    }
                                }
                                catch (Exception exp)
                                {
                                    session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "فایل XML نامعتبر است. اطلاعات بیشتر: " + exp.ToString();
                                    context.UploadSessions.Update(session);
                                    await context.SaveChangesAsync();
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
        public AudioNarrationService(RMuseumDbContext context, IConfiguration configuration, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _context = context;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
        }
    }
}
