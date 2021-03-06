﻿using RSecurityBackend.Models.Image;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Poet
    /// </summary>
    /// <remarks>
    /// cat_id field is removed, it is retrievable by querying <see cref="GanjoorCat"/> 
    /// where <see cref="GanjoorCat.PoetId"/> == <see cref="Id"/> and
    /// <see cref="GanjoorCat.Parent"/> == null
    /// </remarks>
    public class GanjoorPoet
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// short name
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// poet image
        /// </summary>
        public virtual RImage RImage { get; set; }

        /// <summary>
        /// user image id
        /// </summary>
        public Guid? RImageId { get; set; }

        /// <summary>
        /// published on website
        /// </summary>
        public bool Published { get; set; }
    }
}
