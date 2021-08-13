using RSecurityBackend.Models.Auth.Db;
using System;
using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// poem correction
    /// </summary>
    public class GanjoorPoemCorrection
    {
        /// <summary>
        /// Correction Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem Id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// modified verses
        /// </summary>
        public ICollection<GanjoorVerseVOrderText> VerseOrderText { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// original title
        /// </summary>
        public string OriginalTitle { get; set; }

        /// <summary>
        /// rhythm
        /// </summary>
        public string Rhythm { get; set; }

        /// <summary>
        /// original rhythm
        /// </summary>
        public string OriginalRhythm { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// user Id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// reviewed
        /// </summary>
        public bool Reviewed { get; set; }

        /// <summary>
        /// reviewdate
        /// </summary>
        public DateTime ReviewDate { get; set; }

        /// <summary>
        /// review note
        /// </summary>
        public string ReviewNote { get; set; }

        /// <summary>
        /// reviewer id
        /// </summary>
        public Guid? ReviewerUserId { get; set; }

        /// <summary>
        /// reviwer user id
        /// </summary>
        public virtual RAppUser ReviewerUser { get; set; }

        /// <summary>
        /// application order for poem
        /// </summary>
        public int ApplicationOrder { get; set; }

        /// <summary>
        /// review result for title and rhythm
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// review result for rhythm
        /// </summary>
        public CorrectionReviewResult RhythmResult { get; set; }

        /// <summary>
        /// had any effect on poem after moderation? effective in history
        /// </summary>
        public bool AffectedThePoem { get; set; }
    }

    

    
}
