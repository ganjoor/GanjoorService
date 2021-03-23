using System;

namespace RMuseum.Models.Auth.ViewModel
{
    /// <summary>
    /// ganjoor user public profile
    /// </summary>
    public class GanjoorUserPublicProfile
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }


        /// <summary>
        /// user image
        /// </summary>
        public Guid? RImageId { get; set; }

        /// <summary>
        /// nick name
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// biography
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// web site
        /// </summary>
        public string Website { get; set; }

    }
}
