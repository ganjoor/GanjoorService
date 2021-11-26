using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.GanjoorAudio.ViewModels;

namespace GanjooRazor.Pages
{
    public class _AudioPlayerPartialModel : PageModel
    {
        public PublicRecitationViewModel[] Recitations { get; set; }
    }
}
