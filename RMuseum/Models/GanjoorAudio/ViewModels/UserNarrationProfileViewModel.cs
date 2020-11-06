using RSecurityBackend.Models.Auth.ViewModels;
using System;

namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// UserNarrationProfile View Model
    /// </summary>
    public class UserNarrationProfileViewModel
    {
        /// <summary>
        /// parameterless constructor
        /// </summary>
        public UserNarrationProfileViewModel()
        {

        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="src"></param>
        public UserNarrationProfileViewModel(UserNarrationProfile src)
        {
            Id = src.Id;
            User = src.User == null ? null : new PublicRAppUser(src.User);
            UserId = src.UserId;
            Name = src.Name;
            FileSuffixWithoutDash = src.FileSuffixWithoutDash;
            ArtistName = src.ArtistName;
            ArtistUrl = src.ArtistUrl;
            AudioSrc = src.AudioSrc;
            AudioSrcUrl = src.AudioSrcUrl;
            IsDefault = src.IsDefault;

        }
        /// <summary>
        /// Id
        /// </summary>
        /// <remarks>
        /// Do not fill it in POST api
        /// </remarks>
        public Guid Id { get; set; }

        /// <summary>
        /// User
        /// </summary>
        /// <remarks>
        /// Do not fill it in POST api
        /// </remarks>
        public PublicRAppUser User { get; set; }

        /// <summary>
        /// UserId
        /// </summary>
        /// <remarks>
        /// Do not fill it in POST api
        /// </remarks>
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
