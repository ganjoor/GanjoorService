using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;


namespace GanjooRazor.Models
{
    public class InlineSimilarPoems
    {
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public List<int> PoetMorePoemsLikeThisCount { get; set; }
    }
}
