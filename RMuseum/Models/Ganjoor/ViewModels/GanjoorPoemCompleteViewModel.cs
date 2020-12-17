using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.GanjoorIntegration.ViewModels;

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

        /// <summary>
        /// Images
        /// </summary>
        public GanjoorLinkViewModel[] Images { get; set; }
    }
}
