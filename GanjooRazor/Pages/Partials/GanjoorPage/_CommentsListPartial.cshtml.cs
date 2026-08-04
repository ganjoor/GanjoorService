using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// Model for the comments list + count message shared between the initial page render
    /// (_PoemPagePartial.cshtml) and the AJAX sort-toggle refresh (OnGetCommentsPartialAsync).
    /// </summary>
    public class _CommentsListPartialModel : PageModel
    {
        public GanjoorCommentSummaryViewModel[] Comments { get; set; }
        public int PoemId { get; set; }
        public bool LoggedIn { get; set; }

        public _CommentPartialModel GetCommentModel(GanjoorCommentSummaryViewModel comment)
        {
            return new _CommentPartialModel()
            {
                Comment = comment,
                Error = "",
                InReplyTo = null,
                LoggedIn = LoggedIn,
                DivSuffix = "",
                PoemId = PoemId,
            };
        }
    }
}
