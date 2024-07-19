using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace TajikGanjoor.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (bool.Parse(Configuration["MaintenanceMode"] ?? false.ToString()))
            {
                return StatusCode(503);
            }

            return Page();
        }

        private readonly IConfiguration Configuration;
        public IndexModel(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
