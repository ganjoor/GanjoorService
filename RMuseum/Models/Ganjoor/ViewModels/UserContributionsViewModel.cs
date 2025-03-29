using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// user contributions
    /// </summary>
    public class UserContributionsViewModel
    {
        /// <summary>
        /// user id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// poem corrections
        /// </summary>
        public int PoemCorrections { get; set; }

        /// <summary>
        /// section corrections
        /// </summary>
        public int SectionCorrections { get; set; }

        /// <summary>
        /// cat edit corrections
        /// </summary>
        public int CatCorrections { get; set; }

        /// <summary>
        /// suggested songs
        /// </summary>
        public int SuggestedSongs { get; set; }

        /// <summary>
        /// quoted poems
        /// </summary>
        public int QuotedPoems { get; set; }

        /// <summary>
        /// comments
        /// </summary>
        public int Comments { get; set; }

        /// <summary>
        /// recitations
        /// </summary>
        public int Recitations { get; set; }

        /// <summary>
        /// museum links
        /// </summary>
        public int MuseumLinks { get; set; }

        /// <summary>
        /// pinterest links
        /// </summary>
        public int PinterestLinks { get; set; }

        /// <summary>
        /// poet spec links
        /// </summary>
        public int PoetSpecLines { get; set; }

        /// <summary>
        /// poet pictures
        /// </summary>
        public int PoetPictures { get; set; }

        /// <summary>
        /// public user notes
        /// </summary>
        public int PublicUserNotes { get; set; }
    }
}
