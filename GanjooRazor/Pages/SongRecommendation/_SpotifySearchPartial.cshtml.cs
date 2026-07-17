using GSpotifyProxy.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GanjooRazor.Pages
{
    public class _SpotifySearchPartialModel : PageModel
    {
        public NameIdUrlImage[] Artists { get; set; }

        public TrackQueryResult[] Tracks { get; set; }

        public void OnGet()
        {
        }
    }
}
