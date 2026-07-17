using GanjooRazor.Models.BeepTunes;
using GanjooRazor.Utils;
using GSpotifyProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class BpModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// configuration
        /// </summary>
        private readonly IConfiguration Configuration;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public BpModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            Configuration = configuration;
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
        /// readonly mode
        /// </summary>
        public bool ReadOnlyMode
        {
            get
            {
                return bool.Parse(Configuration["ReadOnlyMode"]);
            }
        }

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
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{PoemId}/songs/?approved=false&trackType={(int)PoemMusicTrackType.BeepTunesOrKhosousi}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                SuggestedSongs = JsonConvert.DeserializeObject<PoemMusicTrackViewModel[]>(await response.Content.ReadAsStringAsync());
            }
            else
            {
                SuggestedSongs = new PoemMusicTrackViewModel[] { };
            }
        }

        public async Task OnGetAsync()
        {
            PostSuccess = false;
            LastError = "";
            InsertedSongId = 0;
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                PoemId = int.Parse(Request.Query["p"]);
            }
            else
            {
                PoemMusicTrackViewModel.PoemId = 0;
            }

            await _GetSuggestedSongs();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            PostSuccess = false;
            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            PoemId = PoemMusicTrackViewModel.PoemId = int.Parse(Request.Query["p"]);
            PoemMusicTrackViewModel.TrackType = PoemMusicTrackType.BeepTunesOrKhosousi;
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
                        LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        InsertedSongId = JsonConvert.DeserializeObject<PoemMusicTrackViewModel>(await response.Content.ReadAsStringAsync()).Id;

                        PostSuccess = true;
                    }
                }
                else
                {
                    LastError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
               

                
            }

            await _GetSuggestedSongs();

            return Page();
        }


        /// <summary>
        /// search by artists name
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostSearchByArtistNameAsync(string search)
        {
            var response = await _httpClient.GetAsync($"https://newapi.beeptunes.com/public/search?albumCount=0&artistCount=100&text={search}&trackCount=0");

            List<NameIdUrlImage> artists = new List<NameIdUrlImage>();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                BpSearchResponseModel bpResponse = JsonConvert.DeserializeObject<BpSearchResponseModel>(await response.Content.ReadAsStringAsync());
                foreach (var artist in bpResponse.Artists)
                {
                    artists.Add
                        (
                        new NameIdUrlImage()
                        {
                            Id = artist.Id,
                            Name = artist.ArtisticName,
                            Url = artist.Url,
                            Image = artist.Picture
                        }
                        );
                }

            }


            return new PartialViewResult()
            {
                ViewName = "_SpotifySearchPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SpotifySearchPartialModel()
                    {
                        Artists = artists.ToArray()
                    }
                }
            };
        }

        /// <summary>
        /// fill artist albums
        /// </summary>
        /// <param name="artist">is an ID</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostFillAlbumsAsync(string artist)
        {
            var response = await _httpClient.GetAsync($"https://newapi.beeptunes.com/public/artist/albums?artistId={artist}&begin=0&size=1000");

            NameIdUrlImage[] albums = new NameIdUrlImage[] { };

            if (response.StatusCode == HttpStatusCode.OK)
            {
                albums = JsonConvert.DeserializeObject<NameIdUrlImage[]>(await response.Content.ReadAsStringAsync());
                foreach (var album in albums)
                {
                    album.Url = $"https://beeptunes.com/album/{album.Id}";
                }
            }
            return new OkObjectResult(albums);
        }

        /// <summary>
        /// fill album tracks
        /// </summary>
        /// <param name="album">is an ID</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostFillTracksAsync(string album)
        {
            var response = await _httpClient.GetAsync($"https://newapi.beeptunes.com/public/album/list-tracks/?albumId={album}");

            NameIdUrlImage[] tracks = new NameIdUrlImage[] { };

            if (response.StatusCode == HttpStatusCode.OK)
            {
                tracks = JsonConvert.DeserializeObject<NameIdUrlImage[]>(await response.Content.ReadAsStringAsync());
                foreach (var track in tracks)
                {
                    track.Url = $"https://beeptunes.com/track/{track.Id}";
                }
            }
            return new OkObjectResult(tracks);
        }

        /// <summary>
        /// search by track title
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostSearchByTrackTitleAsync(string search)
        {
            var response = await _httpClient.GetAsync($"https://newapi.beeptunes.com/public/search?albumCount=0&artistCount=0&text={search}&trackCount=100");

            List<TrackQueryResult> tracks = new List<TrackQueryResult>();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                BpSearchResponseModel bpResponse = JsonConvert.DeserializeObject<BpSearchResponseModel>(await response.Content.ReadAsStringAsync());
                foreach (var track in bpResponse.Tracks)
                {
                    if (track.FirstArtists != null && track.FirstArtists.Length > 0)
                    {
                        string albumName = "";
                        var responseAlbum = await _httpClient.GetAsync($"https://newapi.beeptunes.com/public/album/info/?albumId={track.Album_Id}");
                        if (responseAlbum.StatusCode == HttpStatusCode.OK)
                        {
                            NameIdUrlImage nameIdUrl = JsonConvert.DeserializeObject<NameIdUrlImage>(await responseAlbum.Content.ReadAsStringAsync());
                            albumName = nameIdUrl.Name;
                        }

                        tracks.Add
                            (
                            new TrackQueryResult()
                            {
                                Id = track.Id,
                                Name = track.Name,
                                Url = track.Url,
                                AlbumId = track.Album_Id,
                                AlbumName = albumName,
                                AlbunUrl = $"https://beeptunes.com/album/{track.Album_Id}",
                                ArtistId = track.FirstArtists[0].Id,
                                ArtistName = track.FirstArtists[0].ArtisticName,
                                ArtistUrl = track.FirstArtists[0].Url,
                                Image = track.PrimaryImage
                            }
                            );
                    }
                }
            }

            return new PartialViewResult()
            {
                ViewName = "_SpotifySearchPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SpotifySearchPartialModel()
                    {
                        Tracks = tracks.ToArray()
                    }
                }
            };
        }
    }
}
