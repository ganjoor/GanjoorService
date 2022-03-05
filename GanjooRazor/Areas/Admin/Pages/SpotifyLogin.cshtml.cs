using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class SpotifyLoginModel : PageModel
    {
        private readonly IConfiguration Configuration;

        public SpotifyLoginModel(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            return Page();
        }

        /// <summary>
        /// url
        /// </summary>
        public string SpotifyUrl
        {
            get
            {
                return
                    $"https://accounts.spotify.com/authorize?client_id={Configuration.GetSection("Spotify")["client_id"]}&response_type=code&redirect_uri=" +
                        WebUtility.UrlEncode($"{Configuration["SiteUrl"]}/Admin/SpotifyCallback") +
                        "&scope=&state=34fFs29kd09";
            }
        }
    }
}
