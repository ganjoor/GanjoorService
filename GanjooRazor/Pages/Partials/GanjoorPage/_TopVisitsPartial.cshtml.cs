using KontorService.Models.Reporting.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GanjooRazor.Pages
{
    public class _TopVisitsPartialModel : PageModel
    {
        public PageVisitsViewModel[] Visits { get; set; }
    }
}
