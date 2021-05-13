using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Services;

namespace GanjooRazor.Pages
{
    public class AudioClipModel : PageModel
    {

        private readonly IGanjoorService _ganjoorService;


        private readonly IRecitationService _recitationService;

        public AudioClipModel(IGanjoorService ganjoorService, IRecitationService recitationService)
        {
            _ganjoorService = ganjoorService;
            _recitationService = recitationService;
        }

        /// <summary>
        /// recitation
        /// </summary>
        public PublicRecitationViewModel Recitation { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoemCompleteViewModel Poem { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var res = await _recitationService.GetPublishedRecitationById(int.Parse(Request.Query["a"]));


            Recitation = res.Result;

            var resPoem = await _ganjoorService.GetPoemById(Recitation.PoemId, true, false, false, false, false, false, false, true, false);

            Poem = resPoem.Result;

            return Page();

        }
    }
}
