using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.GanjoorAudio.ViewModels;

namespace GanjooRazor.Pages
{
    public class _AudioPlayerPartialModel : PageModel
    {
        public bool LoggedIn { get; set; }
        public PublicRecitationViewModel[] Recitations { get; set; }
        public bool ShowAllRecitaions { get; set; }
        public bool CategoryMode { get; set; }

        public string getAudioDesc(PublicRecitationViewModel recitation, bool contributionLink = false)
        {
            string audiodesc = "به خوانش ";
            if (!string.IsNullOrEmpty(recitation.AudioArtistUrl) && recitation.AudioArtistUrl.StartsWith("http"))
            {
                audiodesc += $"<a href='{recitation.AudioArtistUrl}'>{recitation.AudioArtist}</a>";
            }
            else
            {
                audiodesc += $"{recitation.AudioArtist}";
            }

            if (!string.IsNullOrEmpty(recitation.AudioSrc) && !string.IsNullOrEmpty(recitation.AudioSrcUrl) && recitation.AudioSrcUrl.StartsWith("http"))
            {
                audiodesc += $" نقل از <a href='{recitation.AudioSrcUrl}'>{recitation.AudioSrc}</a>";
            }

            if (contributionLink)
            {
                audiodesc += "<br> <small>می‌خواهید شما بخوانید؟ <a href='http://ava.ganjoor.net/about/'>اینجا</a> را ببینید.</small>";
            }

            return audiodesc;
        }

        public string CSSClass(int recitationIndex)
        {
            return ShowAllRecitaions || (recitationIndex < 5) ? "audio-player" : "hidden-recitation";
        }
    }
}
