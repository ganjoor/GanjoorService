using RSecurityBackend.Models.Auth.Db;
using System;

namespace RSecurityBackend.Models.Generic.Db
{
    /// <summary>
    /// generic option
    /// </summary>
    public class RGenericOption
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid? RAppUserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public virtual RAppUser RAppUser { get; set; }
    }
}
