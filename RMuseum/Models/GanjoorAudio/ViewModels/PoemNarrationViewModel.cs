using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Collections.Generic;

namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    ///  Poem Narration view model
    /// </summary>
    public class PoemNarrationViewModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="src"></param>
        public PoemNarrationViewModel(PoemNarration src)
        {
            Id = src.Id;
            Owner = new PublicRAppUser(src.Owner);
            GanjoorAudioId = src.GanjoorAudioId;
            GanjoorPostId = src.GanjoorPostId;
            AudioOrder = src.AudioOrder;
            FileNameWithoutExtension = src.FileNameWithoutExtension;
            SoundFilesFolder = src.SoundFilesFolder;
            AudioTitle = src.AudioTitle;
            AudioArtist = src.AudioArtist;
            AudioArtistUrl = src.AudioArtistUrl;
            AudioSrc = src.AudioSrc;
            AudioSrcUrl = src.AudioSrcUrl;
            LegacyAudioGuid = src.LegacyAudioGuid;
            Mp3FileCheckSum = src.Mp3FileCheckSum;
            Mp3SizeInBytes = src.Mp3SizeInBytes;
            OggSizeInBytes = src.OggSizeInBytes;
            LocalMp3FilePath = src.LocalMp3FilePath;
            LocalXmlFilePath = src.LocalXmlFilePath;            
            ReviewStatus = src.ReviewStatus;
            List<AudioSyncStatus> lst = new List<AudioSyncStatus>();
            foreach(AudioSyncStatus status in 
                new AudioSyncStatus[] 
                {
                    AudioSyncStatus.SynchronizedOrRejected,
                    AudioSyncStatus.NewItem,
                    AudioSyncStatus.MetadataChanged,
                    AudioSyncStatus.SoundFilesChanged,
                    AudioSyncStatus.XmlFileChanged,
                    AudioSyncStatus.Deleted
                })
            {
                if(
                    src.AudioSyncStatus == (( (int)status )| src.AudioSyncStatus)
                   )
                {
                    lst.Add(status);
                }               
            }
            AudioSyncStatusArray = lst.ToArray();
        }

        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Owner User
        /// </summary>
        public PublicRAppUser Owner { get; set; }

        /// <summary>
        /// Final data is actually exported to a MySQL database which this auto increment field is its key
        /// </summary>
        public int GanjoorAudioId { get; set; }

        /// <summary>
        /// Ganjoor Post Id
        /// </summary>
        public int GanjoorPostId { get; set; }

        /// <summary>
        /// This determines where an audio is displayed between a list of sounds related to a specfic poem
        /// </summary>
        public int AudioOrder { get; set; }

        /// <summary>
        /// Using this field content you would determine xml, mp3 and ogg file names
        /// </summary>
        public string FileNameWithoutExtension { get; set; }

        /// <summary>
        /// combining this with <see cref="FileNameWithoutExtension"/> + .ext would preduce relative path of sound files on our file server the full path would become [HOMEPath] + '/i/' + this value
        /// </summary>
        /// <sample>
        /// "a2"
        /// </sample>
        /// <remarks>
        /// We had previously used different pathes because when a directory became overcrowded our connection time would increase unbearably due to IRAN internet problems, so it is sensible to store it per record and not as a global option value
        /// </remarks>
        public string SoundFilesFolder { get; set; }

        /// <summary>
        /// MP3 File Path
        /// </summary>
        public string RemoteMp3FilePath { get { return $"/i/{SoundFilesFolder}/{FileNameWithoutExtension}.mp3"; } }

        /// <summary>
        /// MP3 url
        /// </summary>
        public string Mp3Url { get { return $"https://i.ganjoor.net/{SoundFilesFolder}/{FileNameWithoutExtension}.mp3"; } }


        /// <summary>
        /// OGG File Path
        /// </summary>
        public string RemoteOggFilePath { get { return $"/i/{SoundFilesFolder}/{FileNameWithoutExtension}.ogg"; } }

        /// <summary>
        /// OGG url
        /// </summary>
        public string OggUrl { get { return $"https://i.ganjoor.net/{SoundFilesFolder}/{FileNameWithoutExtension}.ogg"; } }

        /// <summary>
        /// This is also a legacy field
        /// </summary>
        /// <sample>
        /// "/i/a2/x"
        /// </sample>
        /// <remarks>
        /// XML Schema is defined based on Desktop Ganjoor (https://github.com/ganjoor/ganjoor) code, for
        /// more information take a look at this code:
        /// https://github.com/ganjoor/ganjoor/blob/master/ganjoor/Audio%20Support/PoemAudioListProcessor.cs
        /// </remarks>
        public string RemoteXMLFilePath { get { return $"/i/{SoundFilesFolder}/x/{FileNameWithoutExtension}.xml"; } }

        /// <summary>
        /// XML url
        /// </summary>
        public string XMLUrl { get { return $"https://i.ganjoor.net/{SoundFilesFolder}/x/{FileNameWithoutExtension}.xml"; } }

        /// <summary>
        /// Audio Title
        /// </summary>
        public string AudioTitle { get; set; }

        /// <summary>
        /// Audio Artist
        /// </summary>
        public string AudioArtist { get; set; }

        /// <summary>
        /// Audio Artist Url
        /// </summary>
        public string AudioArtistUrl { get; set; }

        /// <summary>
        /// Audio Source
        /// </summary>
        public string AudioSrc { get; set; }

        /// <summary>
        /// Audio Src Url
        /// </summary>
        public string AudioSrcUrl { get; set; }

        /// <summary>
        /// Legacy Audio Guid
        /// </summary>
        public Guid LegacyAudioGuid { get; set; }

        /// <summary>
        /// Audio File CheckSum
        /// </summary>
        public string Mp3FileCheckSum { get; set; }

        /// <summary>
        /// mp3 size in bytes
        /// </summary>
        public int Mp3SizeInBytes { get; set; }

        /// <summary>
        /// ogg size in bytes
        /// </summary>
        public int OggSizeInBytes { get; set; }

        /// <summary>
        /// Upload Date
        /// </summary>
        public DateTime UploadDate { get; set; }

        /// <summary>
        /// Review Date (Approve or Reject)
        /// </summary>
        public DateTime ReviewDate { get; set; }

        /// <summary>
        /// MP3 File local path on Windows Server (if item is not rejected probably it is not valid and it is deleted)
        /// </summary>
        public string LocalMp3FilePath { get; set; }

        /// <summary>
        /// XML File local path on Windows Server (if item is not rejected probably it is not valid and it is deleted)
        /// </summary>
        public string LocalXmlFilePath { get; set; }

        /// <summary>
        /// Value is one or a combination of <see cref="RMuseum.Models.GanjoorAudio.AudioSyncStatus"/>
        /// </summary>
        public AudioSyncStatus[] AudioSyncStatusArray { get; set; }

        /// <summary>
        /// Review Status
        /// </summary>
        public AudioReviewStatus ReviewStatus { get; set; }
    }
}
