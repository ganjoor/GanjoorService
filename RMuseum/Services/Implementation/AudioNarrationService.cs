using Audit.WebApi;
using ganjoor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NAudio.Wave;
using OggVorbisEncoder;
using RMuseum.DbContext;
using RMuseum.Migrations;
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
                                        string soundFilesFolder = Configuration.GetSection("AudioUploadService")["StoragePath"];
                                        string targetPathForAudioFiles = Path.Combine(Configuration.GetSection("LocalAudioRepositoryPath")["StoragePath"], Configuration.GetSection("AudioUploadService")["StoragePath"]);
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
                                        File.Exists(Path.Combine(targetPathForAudioFiles, $"{fileNameWithoutExtension}.ogg"))
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
                                        if(mp3file == null)
                                        {
                                            session.UploadedFiles.Where(f => f.Id == file.Id).SingleOrDefault().ProcessResultMsg = "فایل mp3 متناظر یافت نشد (توجه فرمایید که همنامی اهمیت ندارد و فایل mp3 ارسالی باید دقیقاً همان فایلی باشد که همگامی با آن صورت گرفته است. اگر بعداً آن را جایگزین کرده‌اید مشخصات آن با مشخصات درج شده در فایل xml همسان نخواهد بود).";
                                            context.UploadSessions.Update(session);
                                        }
                                        else
                                        {
                                            File.Move(mp3file.FilePath, localMp3FilePath);

                                            //here we should produce and save ogg file
                                            byte[] mp3bytes = File.ReadAllBytes(Path.Combine(targetPathForAudioFiles, $"{fileNameWithoutExtension}.mp3"));
                                            int mp3fileSize = mp3bytes.Length;
                                            int oggfileSize;
                                            using (MemoryStream ms = new MemoryStream(mp3bytes))                                            
                                            using (Mp3FileReader mp3FileReader = new Mp3FileReader(ms))
                                            {
                                                byte[] samples = new byte[mp3FileReader.Length];
                                                await mp3FileReader.ReadAsync(samples, 0, (int)mp3FileReader.Length);
                                                var oggBytes = ConvertRawPCMFile(mp3FileReader.Mp3WaveFormat.SampleRate, mp3FileReader.Mp3WaveFormat.Channels, samples, PCMSample.EightBit, mp3FileReader.Mp3WaveFormat.SampleRate, mp3FileReader.Mp3WaveFormat.Channels);
                                                oggfileSize = oggBytes.Length;
                                                File.WriteAllBytes(Path.Combine(targetPathForAudioFiles, $"{fileNameWithoutExtension}.ogg"), oggBytes);
                                            }

                                            

                                            Guid legacyAudioGuid = audio.SyncGuid;
                                            while(
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
                                                SoundFilesFolder = soundFilesFolder,
                                                AudioTitle = audio.PoemTitle,
                                                AudioArtist = defProfile.ArtistName,
                                                AudioArtistUrl = defProfile.ArtistUrl,
                                                AudioSrc = defProfile.AudioSrc, 
                                                AudioSrcUrl = defProfile.AudioSrcUrl, 
                                                LegacyAudioGuid = legacyAudioGuid,
                                                Mp3FileCheckSum = audio.FileCheckSum,
                                                Mp3SizeInBytes = mp3fileSize,
                                                OggSizeInBytes = oggfileSize,
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

        enum PCMSample : int
        {
            EightBit = 1,
            SixteenBit = 2
        }

        private static byte[] ConvertRawPCMFile(int OutputSampleRate, int OutputChannels, byte[] PCMSamples, PCMSample PCMSampleSize, int PCMSampleRate, int PCMChannels)
        {
            int NumPCMSamples = (PCMSamples.Length / (int)PCMSampleSize / PCMChannels);
            float PCMDuraton = NumPCMSamples / (float)PCMSampleRate;

            int NumOutputSamples = (int)(PCMDuraton * OutputSampleRate);
            //Ensure that samble buffer is aligned to write chunk size
            NumOutputSamples = (NumOutputSamples / WriteBufferSize) * WriteBufferSize;

            float[][] OutSamples = new float[OutputChannels][];

            for (int ch = 0; ch < OutputChannels; ch++)
                OutSamples[ch] = new float[NumOutputSamples];

            for (int sampleNumber = 0; sampleNumber < NumOutputSamples; sampleNumber++)
            {
                float rawSample = 0.0f;

                for (int ch = 0; ch < OutputChannels; ch++)
                {
                    int sampleIndex = (sampleNumber * PCMChannels) * (int)PCMSampleSize;

                    if (ch < PCMChannels) sampleIndex += (ch * (int)PCMSampleSize);

                    switch (PCMSampleSize)
                    {
                        case PCMSample.EightBit:
                            rawSample = ByteToSample(PCMSamples[sampleIndex]);
                            break;
                        case PCMSample.SixteenBit:
                            rawSample = ShortToSample((short)(PCMSamples[sampleIndex + 1] << 8 | PCMSamples[sampleIndex]));
                            break;
                    }

                    OutSamples[ch][sampleNumber] = rawSample;
                }
            }

            return GenerateFile(OutSamples, OutputSampleRate, OutputChannels);
        }

        private static float ByteToSample(short pcmValue)
        {
            return pcmValue / 128f;
        }

        private static float ShortToSample(short pcmValue)
        {
            return pcmValue / 32768f;
        }

        private static byte[] GenerateFile(float[][] FloatSamples, int SampleRate, int Channels)
        {
            using (MemoryStream outputData = new MemoryStream())
            {
                // Stores all the static vorbis bitstream settings
                var info = VorbisInfo.InitVariableBitRate(Channels, SampleRate, 0.5f);

                // set up our packet->stream encoder
                var serial = new Random().Next();
                var oggStream = new OggStream(serial);

                // =========================================================
                // HEADER
                // =========================================================
                // Vorbis streams begin with three headers; the initial header (with
                // most of the codec setup parameters) which is mandated by the Ogg
                // bitstream spec.  The second header holds any comment fields.  The
                // third header holds the bitstream codebook.
                var headerBuilder = new HeaderPacketBuilder();

                var comments = new Comments();
                comments.AddTag("ARTIST", "TEST");

                var infoPacket = headerBuilder.BuildInfoPacket(info);
                var commentsPacket = headerBuilder.BuildCommentsPacket(comments);
                var booksPacket = headerBuilder.BuildBooksPacket(info);

                oggStream.PacketIn(infoPacket);
                oggStream.PacketIn(commentsPacket);
                oggStream.PacketIn(booksPacket);

                // Flush to force audio data onto its own page per the spec
                FlushPages(oggStream, outputData, true);

                // =========================================================
                // BODY (Audio Data)
                // =========================================================
                var processingState = ProcessingState.Create(info);

                processingState.WriteData(FloatSamples, FloatSamples[0].Length);
                for (int readIndex = 0; readIndex <= FloatSamples[0].Length; readIndex += WriteBufferSize)
                {
                   
                    OggPacket packet;
                    while (!oggStream.Finished
                            && processingState.PacketOut(out packet))
                    {
                        oggStream.PacketIn(packet);

                        FlushPages(oggStream, outputData, false);
                    }
                }

                FlushPages(oggStream, outputData, true);

                return outputData.ToArray();
            }
        }

        private static void FlushPages(OggStream oggStream, Stream Output, bool Force)
        {
            OggPage page;
            while (oggStream.PageOut(out page, Force))
            {
                Output.Write(page.Header, 0, page.Header.Length);
                Output.Write(page.Body, 0, page.Body.Length);
            }
        }

        private static int WriteBufferSize = 512;
        private static readonly int[] SampleRates = { 8000, 11025, 16000, 22050, 32000, 44100 };



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
