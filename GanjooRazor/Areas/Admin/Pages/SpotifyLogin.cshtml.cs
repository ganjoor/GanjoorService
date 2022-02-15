using System.Net;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class SpotifyLoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public SpotifyLoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
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
                    $"https://accounts.spotify.com/authorize?client_id={SpotifyOptions.Options["client_id"]}&response_type=code&redirect_uri=" +
                        WebUtility.UrlEncode($"{_configuration["SiteUrl"]}/Admin/SpotifyCallback") +
                        "&scope=&state=34fFs29kd09";
            }
        }
    }
}
