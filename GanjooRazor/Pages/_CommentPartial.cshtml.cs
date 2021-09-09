using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _CommentPartialModel : PageModel
    {
        public GanjoorCommentSummaryViewModel Comment { get; set; }

        public string Error { get; set; }

        public GanjoorCommentSummaryViewModel InReplyTo { get; set; }

        public bool LoggedIn { get; set; }

        public string DivSuffix { get; set; }

        public string Wrote
        {
            get
            {
                return InReplyTo == null ? "‰Ê‘ Â" : "Å«”Œ œ«œÂ";
            }
        }

        public _CommentPartialModel GetCommentModel(GanjoorCommentSummaryViewModel comment)
        {
            return new _CommentPartialModel()
            {
                Comment = comment,
                Error = "",
                InReplyTo = Comment,
                LoggedIn = LoggedIn,
                DivSuffix = comment.CoupletIndex == -1 ? "" : $"-{comment.CoupletIndex.ToString()}"
            };
        }
       
    }
}
