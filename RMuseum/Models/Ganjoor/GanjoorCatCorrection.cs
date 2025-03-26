using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    public class GanjoorCatCorrection
    {
        /// <summary>
        /// correct id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// category id
        /// </summary>
        public int CatId { get; set; }

        /// <summary>
        /// category
        /// </summary>
        public GanjoorCat Cat { get; set; }

        /// <summary>
        /// additional description or note
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// html mode of additional description or note
        /// </summary>
        public string DescriptionHtml { get; set; }

        /// <summary>
        /// additional description or note
        /// </summary>
        public string OriginalDescription { get; set; }

        /// <summary>
        /// html mode of additional description or note
        /// </summary>
        public string OriginalDescriptionHtml { get; set; }

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
        /// application order for category
        /// </summary>
        public int ApplicationOrder { get; set; }

        /// <summary>
        /// review result for description
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// had any effect on category after moderation? effective in history
        /// </summary>
        public bool AffectedTheCat { get; set; }

        /// <summary>
        /// hide the editors name
        /// </summary>
        public bool HideMyName { get; set; }

        /// <summary>
        /// page id
        /// </summary>
        public int PageId { get; set; }
    }
}
