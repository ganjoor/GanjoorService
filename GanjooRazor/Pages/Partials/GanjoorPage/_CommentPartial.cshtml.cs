using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _CommentPartialModel : PageModel
    {
        public GanjoorCommentSummaryViewModel Comment { get; set; }
        public string Error { get; set; }

        /// <summary>
        /// set (alongside Error) when Error is the "sanitizing had to drop real text" case -
        /// the plain text that would remain, so client JS can diff it against what the user
        /// actually typed and show them exactly what got dropped. Null/empty for any other error.
        /// </summary>
        public string SanitizerRemainingText { get; set; }
        public GanjoorCommentSummaryViewModel InReplyTo { get; set; }
        public bool LoggedIn { get; set; }
        public string DivSuffix { get; set; }
        public int PoemId { get; set; }
        public string Wrote
        {
            get
            {
                return InReplyTo == null ? "نوشته" : "پاسخ داده";
            }
        }
        public bool Bookmarked { get; set; }
        public _CommentPartialModel GetCommentModel(GanjoorCommentSummaryViewModel comment)
        {
            return new _CommentPartialModel()
            {
                Comment = comment,
                Error = "",
                InReplyTo = Comment,
                LoggedIn = LoggedIn,
                DivSuffix = DivSuffix,
                PoemId = PoemId,
                Bookmarked = Bookmarked,
            };
        }
       
    }
}
