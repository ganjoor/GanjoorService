using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using System;

namespace RMuseum.Models.GanjoorIntegration.ViewModels
{
    /// <summary>
    /// Ganjoor Link View Model
    /// </summary>
    public class GanjoorLinkViewModel
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public GanjoorLinkViewModel()
        {

        }

        /// <summary>
        /// parameterized constructor
        /// </summary>
        /// <param name="src"></param>
        /// <param name="entityName"></param>
        /// <param name="entityFriendlyUrl"></param>
        /// <param name="entityImageId"></param>
        public GanjoorLinkViewModel(GanjoorLink src, string entityName, string entityFriendlyUrl, Guid entityImageId)
        {
            Id = src.Id;
            GanjoorPostId = src.GanjoorPostId;
            GanjoorUrl = src.GanjoorUrl;
            GanjoorTitle = src.GanjoorTitle;
            EntityName = entityName;
            EntityFriendlyUrl = entityFriendlyUrl;
            EntityImageId = entityImageId;
            ReviewResult = src.ReviewResult;
            Synchronized = src.Synchronized;
            RAppUser appUser = src.SuggestedBy;
            SuggestedBy =new PublicRAppUser()
            {
                Id = appUser.Id,
                Username = appUser.UserName,
                Email = appUser.Email,
                FirstName = appUser.FirstName,
                SureName = appUser.SureName,
                PhoneNumber = appUser.PhoneNumber,
                RImageId = appUser.RImageId,
                Status = appUser.Status
            };
        }

        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Ganjoor Post Id
        /// </summary>
        public int GanjoorPostId { get; set; }

        /// <summary>
        /// ganjoor url
        /// </summary>
        public string GanjoorUrl { get; set; }

        /// <summary>
        /// ganjoor title
        /// </summary>
        public string GanjoorTitle { get; set; }

        /// <summary>
        /// entity name
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// entity friendly url
        /// </summary>
        public string EntityFriendlyUrl { get; set; }

        /// <summary>
        /// entity image id
        /// </summary>
        public Guid EntityImageId { get; set; }

        /// <summary>
        /// review result
        /// </summary>
        public ReviewResult ReviewResult { get; set; }

        /// <summary>
        /// Synchronized with ganjoor
        /// </summary>
        public bool Synchronized { get; set; }

        /// <summary>
        /// suggested by
        /// </summary>
        public PublicRAppUser SuggestedBy { get; set; }
    }
}
