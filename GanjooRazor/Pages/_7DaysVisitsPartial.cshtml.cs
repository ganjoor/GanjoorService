using KontorService.Models.Reporting.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GanjooRazor.Pages
{
    public class _7DaysVisitsPartialModel : PageModel
    {
        public DateRangeVisitsViewModel[] SevenDaysVisits { get; set; }
    }
}
