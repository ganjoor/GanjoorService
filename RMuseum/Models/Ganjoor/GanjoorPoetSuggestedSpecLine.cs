﻿namespace RMuseum.Models.Ganjoor
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
        /// order
        /// </summary>
        public int LineOrder { get; set; }

        /// <summary>
        /// Contents
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }


    }
}
