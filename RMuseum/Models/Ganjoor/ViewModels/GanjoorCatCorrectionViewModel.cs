using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    public class GanjoorCatCorrectionViewModel
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
        /// nickname
        /// </summary>
        public string UserNickname { get; set; }

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
        /// review result for description
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// hide the editors name
        /// </summary>
        public bool HideMyName { get; set; }
    }
}
