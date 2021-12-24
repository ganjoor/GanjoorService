using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using System;

namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    ///  Poem Narration view model
    /// </summary>
    public class RecitationViewModel
    {
        /// <summary>
        /// parameterless constructor for deserialization support
        /// </summary>
        public RecitationViewModel()
        {

        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="src"></param>
        /// <param name="appUser"></param>
        /// <param name="poem"></param>
        /// <param name="mistake"></param>
        public RecitationViewModel(Recitation src, RAppUser appUser, GanjoorPoem poem, string mistake)
        {
            Id = src.Id;
            Owner = new PublicRAppUser()
            {
                Id = appUser.Id,
                Username = appUser.UserName,
                Email = appUser.Email,
                FirstName = appUser.FirstName,
                SureName = appUser.SureName,
                PhoneNumber = appUser.PhoneNumber,
                RImageId = appUser.RImageId,
                Status = appUser.Status,
                NickName = appUser.NickName,
                Website = appUser.Website,
                Bio = appUser.Bio,
                EmailConfirmed = appUser.EmailConfirmed
            };
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
            UploadDate = src.UploadDate;
            FileLastUpdated = src.FileLastUpdated;
            ReviewDate = src.ReviewDate;
            if(poem != null)
            {
                PoemFullTitle = poem.FullTitle;
                PoemFullUrl = poem.FullUrl;
            }
            AudioSyncStatus = src.AudioSyncStatus;
            if (string.IsNullOrEmpty(mistake))
                ReviewMsg = src.ReviewMsg;
            else
                ReviewMsg = mistake;
        }

        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

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
        /// Poem Full Title
        /// </summary>
        public string PoemFullTitle { get; set; }

        /// <summary>
        /// Poem Full Url (without domain name)
        /// </summary>
        public string PoemFullUrl { get; set; }

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
        /// File Last Updated
        /// </summary>
        public DateTime FileLastUpdated { get; set; }

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
        public AudioSyncStatus AudioSyncStatus { get; set; }

        /// <summary>
        /// Review Status
        /// </summary>
        public AudioReviewStatus ReviewStatus { get; set; }

        /// <summary>
        /// Review Message
        /// </summary>
        public string ReviewMsg { get; set; }
    }
}
