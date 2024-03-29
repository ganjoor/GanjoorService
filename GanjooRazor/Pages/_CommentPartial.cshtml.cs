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
        public int PoemId { get; set; }
        public string Wrote
        {
            get
            {
                return InReplyTo == null ? "�����" : "���� ����";
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
