using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// poem section correction
    /// </summary>
    public class GanjoorPoemSectionCorrection
    {
        /// <summary>
        /// Correction Id
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// section id
        /// </summary>
        public int SectionId { get; set; }

        /// <summary>
        /// section
        /// </summary>
        public GanjoorPoemSection Section { get; set; }

        /// <summary>
        /// rhythm
        /// </summary>
        public string Rhythm { get; set; }

        /// <summary>
        /// original rhythm
        /// </summary>
        public string OriginalRhythm { get; set; }

        /// <summary>
        /// review result for rhythm
        /// </summary>
        public CorrectionReviewResult RhythmResult { get; set; }

        /// <summary>
        /// rhythm 2
        /// </summary>
        public string Rhythm2 { get; set; }

        /// <summary>
        /// original rhythm 2
        /// </summary>
        public string OriginalRhythm2 { get; set; }

        /// <summary>
        /// review result for rhythm2
        /// </summary>
        public CorrectionReviewResult RhythmResult2 { get; set; }

        /// <summary>
        /// break from verse1 VOrder
        /// </summary>
        public int? BreakFromVerse1VOrder { get; set; }

        /// <summary>
        /// review result for break from verse1 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse1VOrderResult { get; set; }

        /// <summary>
        /// break from verse2 VOrder
        /// </summary>
        public int? BreakFromVerse2VOrder { get; set; }

        /// <summary>
        /// review result for break from verse2 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse2VOrderResult { get; set; }

        /// <summary>
        /// break from verse3 VOrder
        /// </summary>
        public int? BreakFromVerse3VOrder { get; set; }

        /// <summary>
        /// review result for break from verse3 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse3VOrderResult { get; set; }

        /// <summary>
        /// break from verse4 VOrder
        /// </summary>
        public int? BreakFromVerse4VOrder { get; set; }

        /// <summary>
        /// review result for break from verse4 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse4VOrderResult { get; set; }

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
        /// had any effect on poem after moderation? effective in history
        /// </summary>
        public bool AffectedThePoem { get; set; }

        /// <summary>
        /// break from verse5 VOrder
        /// </summary>
        public int? BreakFromVerse5VOrder { get; set; }

        /// <summary>
        /// review result for break from verse5 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse5VOrderResult { get; set; }

        /// <summary>
        /// break from verse6 VOrder
        /// </summary>
        public int? BreakFromVerse6VOrder { get; set; }

        /// <summary>
        /// review result for break from verse6 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse6VOrderResult { get; set; }


        /// <summary>
        /// break from verse7 VOrder
        /// </summary>
        public int? BreakFromVerse7VOrder { get; set; }

        /// <summary>
        /// review result for break from verse7 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse7VOrderResult { get; set; }


        /// <summary>
        /// break from verse8 VOrder
        /// </summary>
        public int? BreakFromVerse8VOrder { get; set; }

        /// <summary>
        /// review result for break from verse8 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse8VOrderResult { get; set; }


        /// <summary>
        /// break from verse9 VOrder
        /// </summary>
        public int? BreakFromVerse9VOrder { get; set; }

        /// <summary>
        /// review result for break from verse9 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse9VOrderResult { get; set; }

        /// <summary>
        /// break from verse10 VOrder
        /// </summary>
        public int? BreakFromVerse10VOrder { get; set; }

        /// <summary>
        /// review result for break from verse10 VOrder
        /// </summary>
        public CorrectionReviewResult BreakFromVerse10VOrderResult { get; set; }

        /// <summary>
        /// rhyme letters
        /// </summary>
        public string RhymeLetters { get; set; }

        /// <summary>
        /// original rhyme letters
        /// </summary>
        public string OriginalRhymeLetters { get; set; }

        /// <summary>
        /// rhyme letters review result
        /// </summary>
        public CorrectionReviewResult RhymeLettersReviewResult { get; set; }

        /// <summary>
        /// language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// original language
        /// </summary>
        public string OriginalLanguage { get; set; }

        /// <summary>
        /// language review result
        /// </summary>
        public CorrectionReviewResult LanguageReviewResult { get; set; }

        /// <summary>
        /// poem format
        /// </summary>
        public GanjoorPoemFormat? PoemFormat { get; set; }

        /// <summary>
        /// original poem format
        /// </summary>
        public GanjoorPoemFormat? OriginalPoemFormat { get; set; }

        /// <summary>
        /// poem format review result
        /// </summary>
        public CorrectionReviewResult PoemFormatReviewResult { get; set; }

        /// <summary>
        /// hide the editors name
        /// </summary>
        public bool HideMyName { get; set; }
    }
}
