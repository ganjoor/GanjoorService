using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// Role Permissions (a set of defined permision policies which could be assigned to users)
    /// </summary>
    public class RAppRole : IdentityRole<Guid>
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public RAppRole() : base()
        {

        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="roleName"></param>
        public RAppRole(string roleName) : base(roleName)
        {
        }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Permissions
        /// </summary>
        public ICollection<RPermission> Permissions { get; set; }
    }
}
