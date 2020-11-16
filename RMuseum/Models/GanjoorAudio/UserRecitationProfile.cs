using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// Narration Profiles for users
    /// </summary>
    public class UserRecitationProfile
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// UserId
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Profile Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// this would be appended to audio files names prefixed by a dash to make them unique and specfic to user
        /// filenames usually would look like {GanjoorPostId}-{FileSuffixWithoutDash}.{ext}
        /// for example 2200-hrm.xml
        /// </summary>
        public string FileSuffixWithoutDash { get; set; }

        /// <summary>
        /// artist name
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        /// artist url
        /// </summary>
        public string ArtistUrl { get; set; }

        /// <summary>
        /// audio src
        /// </summary>
        public string AudioSrc { get; set; }

        /// <summary>
        /// audio src url
        /// </summary>
        public string AudioSrcUrl { get; set; }

        /// <summary>
        /// is default
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
