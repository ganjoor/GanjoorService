namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Geo + date tags for poems
    /// </summary>
    public class PoemGeoDateTag
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Poem Id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// Poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// location
        /// </summary>
        public int? LocationId { get; set; }

        /// <summary>
        /// geo location
        /// </summary>
        public virtual GanjoorGeoLocation Location { get; set; }

        /// <summary>
        /// Hijri Ghamari year
        /// </summary>
        public int? LunarYear { get; set; }

        /// <summary>
        /// Hijri Ghamari month
        /// </summary>
        public int? LunarMonth { get; set; }

        /// <summary>
        /// Hijri Ghamari day
        /// </summary>
        public int? LunarDay{ get; set; }

        /// <summary>
        /// sample: 14440101, would be used in sorting events
        /// </summary>
        public int? LunarDateTotalNumber { get; set; }

        /// <summary>
        /// verified date
        /// </summary>
        public bool VerifiedDate { get; set; }

        /// <summary>
        /// ignore in category
        /// </summary>
        public bool IgnoreInCategory { get; set; }

        /// <summary>
        /// related person id
        /// </summary>
        public int? PersonId { get; set; }

        /// <summary>
        /// related person
        /// </summary>
        public virtual GanjoorRelatedPerson Person { get; set; }
    }
}
