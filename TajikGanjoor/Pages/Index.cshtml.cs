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

        protected readonly IConfiguration Configuration;

        protected readonly HttpClient _httpClient;
        public IndexModel(IConfiguration configuration, HttpClient httpClient)
        {
            Configuration = configuration;
            _httpClient = httpClient;
        }
    }
}
