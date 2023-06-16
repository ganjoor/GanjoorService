namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    ///  a person refered by a poem
    /// </summary>
    public class GanjoorRelatedPerson
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
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// wikipedia url
        /// </summary>
        public string WikiUrl { get; set; }

        /// <summary>
        /// birth year in lunar hijri
        /// </summary>
        public int BirthYearInLHijri { get; set; }

        /// <summary>
        /// death year in lunar hijri
        /// </summary>
        public int DeathYearInLHijri { get; set; }

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
