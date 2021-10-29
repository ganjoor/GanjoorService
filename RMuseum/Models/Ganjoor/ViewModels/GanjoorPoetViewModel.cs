namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Poet
    /// </summary>
    public class GanjoorPoetViewModel
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
        /// urlslug
        /// </summary>
        public string FullUrl { get; set; }

        /// <summary>
        /// root cat id
        /// </summary>
        public int RootCatId { get; set; }

        /// <summary>
        /// short name
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// published on website
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// image url
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// birth year in lunar hijri
        /// </summary>
        public int BirthYearInLHijri { get; set; }

        /// <summary>
        /// BirthYearInLHijri, for some poets it is only indicator of their century of birth and should not be relied as their valid birth date
        /// </summary>
        public bool ValidBirthDate { get; set; }

        /// <summary>
        /// death year in lunar hijri
        /// </summary>
        public int DeathYearInLHijri { get; set; }

        /// <summary>
        /// DeathYearInLHijri, for some poets it is only indicator of their century of death and should not be relied as their valid death date
        /// </summary>
        public bool ValidDeathDate { get; set; }

        /// <summary>
        /// Home page pin order (zero means not pinned)
        /// </summary>
        public int PinOrder { get; set; }

        /// <summary>
        /// birth place
        /// </summary>
        public string BirthPlace { get; set; }

        /// <summary>
        /// birth place latitude
        /// </summary>
        public double BirthPlaceLatitude { get; set; }

        /// <summary>
        /// birth place longitude
        /// </summary>
        public double BirthPlaceLongitude { get; set; }

        /// <summary>
        /// death place
        /// </summary>
        public string DeathPlace { get; set; }

        /// <summary>
        /// death place latitude
        /// </summary>
        public double DeathPlaceLatitude { get; set; }

        /// <summary>
        /// death place longitude
        /// </summary>
        public double DeathPlaceLongitude { get; set; }

        public override string ToString()
        {
            return Nickname;
        }
    }
}
