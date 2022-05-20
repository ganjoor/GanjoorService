using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Updating related sections logs
    /// </summary>
    public class UpdatingRelSectsLog
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// meter id
        /// </summary>
        public int MeterId { get; set; }

        /// <summary>
        /// rhyme letters
        /// </summary>
        public string RhymeLettes { get; set; }

        /// <summary>
        /// date/time
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
