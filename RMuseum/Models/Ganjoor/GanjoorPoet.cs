using RSecurityBackend.Models.Image;
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

        /// <summary>
        /// birth year in lunar hijri
        /// </summary>
        public int BirthYearInLHijri { get; set; }

        /// <summary>
        /// death year in lunar hijri
        /// </summary>
        public int DeathYearInLHijri { get; set; }

        /// <summary>
        /// Home page pin order (zero means not pinned)
        /// </summary>
        public int PinOrder { get; set; }

        /// <summary>
        /// BirthYearInLHijri, for some poets it is only indicator of their century of birth and should not be relied as their valid birth date
        /// </summary>
        public bool ValidBirthDate { get; set; }

        /// <summary>
        /// DeathYearInLHijri, for some poets it is only indicator of their century of death and should not be relied as their valid death date
        /// </summary>
        public bool ValidDeathDate { get; set; }

        /// <summary>
        /// birth location id
        /// </summary>
        public int? BirthLocationId { get; set; }

        /// <summary>
        /// birth location
        /// </summary>
        public virtual GanjoorGeoLocation BirthLocation { get; set; }

        /// <summary>
        /// death location id
        /// </summary>
        public int? DeathLocationId { get; set; }

        /// <summary>
        /// death location
        /// </summary>
        public virtual GanjoorGeoLocation DeathLocation { get; set; }
    }
}
