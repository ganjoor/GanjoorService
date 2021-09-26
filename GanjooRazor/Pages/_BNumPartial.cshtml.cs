using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;

namespace GanjooRazor.Pages
{
    public class _BNumPartialModel : PageModel
    {
        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// is bookmarked
        /// </summary>
        public bool IsBookmarked { get; set; }

        /// <summary>
        /// bookmaring text
        /// </summary>
        public string BookmarkingText
        {
            get
            {
                return IsBookmarked ? "حذف نشانه" : "نشانه‌گذاری";
            }
        }

        /// <summary>
        /// bookmarking icon
        /// </summary>
        public string BookmarkingIcon
        {
            get
            {
                return IsBookmarked ? "star" : "star_border";
            }
        }

        /// <summary>
        /// comments
        /// </summary>
        public List<GanjoorCommentSummaryViewModel> Comments { get; set; }

        public _CommentPartialModel GetCommentModel(GanjoorCommentSummaryViewModel comment)
        {
            return new _CommentPartialModel()
            {
                Comment = comment,
                Error = "",
                InReplyTo = null,
                LoggedIn = LoggedIn,
                DivSuffix = $"-{comment.CoupletIndex}"
            };
        }



    }
}
