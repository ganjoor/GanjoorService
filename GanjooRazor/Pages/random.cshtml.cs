using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace GanjooRazor.Pages
{
    public class RandomModel : PageModel
    {
        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        public RandomModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            ViewData["TrackingScript"] = _configuration["TrackingScript"];
        }
    }
}
