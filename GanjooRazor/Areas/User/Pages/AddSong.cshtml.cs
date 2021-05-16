using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;

namespace GanjooRazor.Areas.User.Pages
{
    public class AddSongModel : PageModel
    {
        /// ganjoor service
        /// </summary>
        private readonly IGanjoorService _ganjoorService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ganjoorService"></param>
        public AddSongModel(IGanjoorService ganjoorService)
        {
            _ganjoorService = ganjoorService;
        }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public PoemMusicTrackViewModel PoemMusicTrackViewModel { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        public void OnGet()
        {
            PoemMusicTrackViewModel = new PoemMusicTrackViewModel()
            {
                TrackType = RMuseum.Models.Ganjoor.PoemMusicTrackType.BeepTunesOrKhosousi,
                PoemId = 0,
                ArtistName = "محمدرضا شجریان",
                ArtistUrl = "http://beeptunes.com/artist/3403349",
                AlbumName = "اجراهای خصوصی",
                AlbumUrl = "http://khosousi.com",
                TrackName = "",
                TrackUrl = "",
                Approved = false,
                Rejected = false,
                BrokenLink = false,
                Description = ""
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var putResponse = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/song/add", new StringContent(JsonConvert.SerializeObject(PoemMusicTrackViewModel), Encoding.UTF8, "application/json"));
                    if (!putResponse.IsSuccessStatusCode)
                    {
                        LastError = await putResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        await _ganjoorService.CacheCleanForPageById(PoemMusicTrackViewModel.PoemId);
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }


            if (!string.IsNullOrEmpty(LastError))
            {
                return Page();
            }

            return Redirect("/User/AddSong");
        }
    }
}
