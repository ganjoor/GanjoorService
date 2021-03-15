using RSecurityBackend.Models.Auth.Db;
using System;
using System.ComponentModel.DataAnnotations;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// user informtaion
    /// </summary>
    /// <remarks>
    /// a safe subset of RAppUser
    /// </remarks>
    public class PublicRAppUser
    {
        /// <summary>
        /// Id
        /// </summary>
        /// 
        /// <example>
        /// </example>
        public Guid? Id { get; set; }

        /// <summary>
        /// User Name
        /// </summary>
        /// <example>
        /// test
        /// </example>
        [MinLength(4)]
        public string Username { get; set; }

        /// <summary>
        /// User Email
        /// </summary>
        /// <example>
        /// test@ganjoor.net
        /// </example>
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// User Mobile Phone Number
        /// </summary>
        /// <example>
        /// +989121234567
        /// </example>
        [Phone]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// First Name
        /// </summary>
        /// <example>
        /// Hamid Reza
        /// </example>
        [MinLength(1)]
        public string FirstName { get; set; }

        /// <summary>
        /// Sure Name
        /// </summary>
        /// <example>
        /// Mohammadi
        /// </example>
        [MinLength(1)]
        public string SureName { get; set; }

        /// <summary>
        /// user status
        /// </summary>
        /// <example>
        /// 1
        /// </example>
        [Required]
        public RAppUserStatus Status { get; set; }

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
