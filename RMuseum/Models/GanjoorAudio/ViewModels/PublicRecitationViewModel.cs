using System;

namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Public (no user specific data) for publish recitations
    /// </summary>
    public class PublicRecitationViewModel
    {
        /// <summary>
        /// Audio Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ganjoor Post Id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// Poem Full Title
        /// </summary>
        public string PoemFullTitle { get; set; }

        /// <summary>
        /// Poem Full Url (without domain name)
        /// </summary>
        public string PoemFullUrl { get; set; }

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
        /// Publish Date
        /// </summary>
        public DateTime PublishDate { get; set; }

        /// <summary>
        /// File Last Updated
        /// </summary>
        public DateTime FileLastUpdated { get; set; }

        /// <summary>
        /// Mp3 Url
        /// </summary>
        public string Mp3Url { get; set; }

        /// <summary>
        /// Xml Text, xml file url is {WebServiceUrl.Url}/api/audio/file/{audio.Id}.xml
        /// </summary>
        public string XmlText { get; set; }

        /// <summary>
        /// Poem Plain Text
        /// </summary>
        public string PlainText { get; set; }

        /// <summary>
        /// Poem Html Text
        /// </summary>
        public string HtmlText { get; set; }

        /// <summary>
        /// mistakes
        /// </summary>
        public RecitationMistakeViewModel[] Mistakes { get; set; }

        /// <summary>
        /// This determines where an audio is displayed between a list of sounds related to a specfic poem
        /// </summary>
        public int AudioOrder { get; set; }

        /// <summary>
        /// upvoted by current user (filled at client)
        /// </summary>
        public bool UpVotedByUser { get; set; }

    }
}
