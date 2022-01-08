using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// ganjoor user history track item (stored by his or her choice)
    /// </summary>
    public class GanjoorUserPoemVisit
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// user id (nullable to prevent a db cascade relation)
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public virtual RAppUser User { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// date/time
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
