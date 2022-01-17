using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Poet Specification
    /// </summary>
    public class GanjoorPoetSuggestedSpecLine
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Poet Id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// Poet
        /// </summary>
        public GanjoorPoet Poet { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// suggested by id
        /// </summary>
        public Guid? SuggestedById { get; set; }

        /// <summary>
        /// suggested by
        /// </summary>
        public virtual RAppUser SuggestedBy { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int LineOrder { get; set; }

    }
}
