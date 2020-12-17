using RMuseum.Models.GanjoorAudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// a more complete GanjoorPoem View Model
    /// </summary>
    public class GanjoorPoemCompleteViewModel
    {
        /// <summary>
        /// basic poem info
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// Recitations
        /// </summary>
        public PublicRecitationViewModel[] Recitations { get; set; }
    }
}
