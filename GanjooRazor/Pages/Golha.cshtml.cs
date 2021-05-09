using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class GolhaModel : PageModel
    {

        private readonly IGanjoorService _ganjoorService;

        private readonly IMusicCatalogueService _musicCatalogue;

       /// <summary>
       /// constructor
       /// </summary>
       /// <param name="ganjoorService"></param>
       /// <param name="musicCatalogue"></param>
        public GolhaModel(
            IGanjoorService ganjoorService,
            IMusicCatalogueService musicCatalogue
            )
        {
            _ganjoorService = ganjoorService;
            _musicCatalogue = musicCatalogue;
        }
        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        /// <summary>
        /// PoemId
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// Post Success
        /// </summary>
        public bool PostSuccess { get; set; }

        /// <summary>
        /// Inserted song Id
        /// </summary>
        public int InsertedSongId { get; set; }

        /// <summary>
        /// suggested (unapproved) songs
        /// </summary>
        public PoemMusicTrackViewModel[] SuggestedSongs { get; set; }

        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public PoemMusicTrackViewModel PoemMusicTrackViewModel { get; set; }

        private async Task _GetSuggestedSongs()
        {
            var resSuggested = await _ganjoorService.GetPoemSongs(PoemId, false, PoemMusicTrackType.Golha);
            SuggestedSongs = resSuggested.Result;
        }

        public async Task OnGetAsync()
        {
            PostSuccess = false;
            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            InsertedSongId = 0;
            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                PoemId = int.Parse(Request.Query["p"]);
            }
            else
            {
                PoemId = 0;
            }

            await _GetSuggestedSongs();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            PostSuccess = false;
            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            PoemId = PoemMusicTrackViewModel.PoemId = int.Parse(Request.Query["p"]);
            PoemMusicTrackViewModel.TrackType = PoemMusicTrackType.Golha;
            InsertedSongId = 0;

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var stringContent = new StringContent(JsonConvert.SerializeObject(PoemMusicTrackViewModel), Encoding.UTF8, "application/json");
                    var methodUrl = $"{APIRoot.Url}/api/ganjoor/song";
                    var response = await secureClient.PostAsync(methodUrl, stringContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        InsertedSongId = JsonConvert.DeserializeObject<PoemMusicTrackViewModel>(await response.Content.ReadAsStringAsync()).Id;

                        PostSuccess = true;
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
                

                await _GetSuggestedSongs();
            }

            return Page();
        }

        /// <summary>
        /// fill collection programs
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostFillProgramsAsync(int collection)
        {
            var res = await _musicCatalogue.GetGolhaCollectionPrograms(collection);
            return new OkObjectResult(res.Result);
        }

        /// <summary>
        /// fill program tracks
        /// </summary>
        /// <param name="program"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostFillTracksAsync(int program)
        {
            var res = await _musicCatalogue.GetGolhaProgramTracks(program);
            return new OkObjectResult(res.Result);
        }
    }
}
