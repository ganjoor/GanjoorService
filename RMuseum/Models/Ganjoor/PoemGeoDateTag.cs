using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>
        /// AI generated
        /// </summary>
        public bool MachineGenerated { get; set; }

        /// <summary>
        /// optional explanatory note for this tag (e.g. why this couplet is relevant to the
        /// tagged person - carried over from the suggestion's SuggestionNote when a moderator
        /// approves it, and editable afterwards like the tag's other fields)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// the tagged couplet's own text (both مصرع of the verse at PoemId/CoupletIndex),
        /// filled in on read by whichever service method needs to show it (e.g.
        /// GanjoorRelatedPersonService.GetPoemsByPersonAsync) - not a mapped database column
        /// </summary>
        [NotMapped]
        public string CoupletText { get; set; }
    }
}
