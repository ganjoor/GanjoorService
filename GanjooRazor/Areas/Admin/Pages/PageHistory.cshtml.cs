using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class PageHistoryModel : PageModel
    {
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }
        public void OnGet()
        {
        }
    }
}
