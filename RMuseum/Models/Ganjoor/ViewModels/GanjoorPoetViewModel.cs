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
        /// death year in lunar hijri
        /// </summary>
        public int DeathYearInLHijri { get; set; }

        /// <summary>
        /// Home page pin order (zero means not pinned)
        /// </summary>
        public int PinOrder { get; set; }

        public override string ToString()
        {
            return Nickname;
        }
    }
}
