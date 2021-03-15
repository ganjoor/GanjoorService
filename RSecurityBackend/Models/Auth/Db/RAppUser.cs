using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using RSecurityBackend.Models.Image;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// Ganjoor Application User
    /// </summary>
    public class RAppUser : IdentityUser<Guid>
    {   
        /// <summary>
        /// First Name
        /// </summary>
        [MinLength(1)]
        public string FirstName { get; set; }

        /// <summary>
        /// Sure Name
        /// </summary>
        [MinLength(1)]
        public string SureName { get; set; }

        /// <summary>
        /// User Creation Date
        /// </summary>
        [Required]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// user status
        /// </summary>
        [Required]
        public RAppUserStatus Status { get; set; }

        /// <summary>
        /// user image
        /// </summary>
        public virtual RImage RImage { get; set; }

        /// <summary>
        /// user image id
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
