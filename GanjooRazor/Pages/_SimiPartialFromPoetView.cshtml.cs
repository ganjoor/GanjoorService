using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _SimiPartialFromPoetViewModel : PageModel
    {
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
    }
}
