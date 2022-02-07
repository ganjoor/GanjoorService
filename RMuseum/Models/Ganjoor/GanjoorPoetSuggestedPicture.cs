using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Poet Picture
    /// </summary>
    public class GanjoorPoetSuggestedPicture
    {
        /// <summary>
        /// id
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
        /// order
        /// </summary>
        public int PicOrder { get; set; }

        /// <summary>
        /// picture id
        /// </summary>
        public Guid? PictureId { get; set; }

        /// <summary>
        /// picture
        /// </summary>
        public virtual RPictureFile Picture { get; set; }

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
        /// selected
        /// </summary>
        public bool ChosenOne { get; set; }

    }
}
