using GanjooRazor.Utils;
using GSpotifyProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SpotifyModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public SpotifyModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
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
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{PoemId}/songs/?approved=false&trackType={(int)PoemMusicTrackType.Spotify}");

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
            PoemMusicTrackViewModel.TrackType = PoemMusicTrackType.Spotify;
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
        /// search by artists name
        /// </summary>
        /// <param name="search"></param>
        /// <param name="secondtime"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostSearchByArtistNameAsync(string search, bool secondtime = false)
        {
            string spotifyToken = _configuration.GetSection("Spofify")["AccessToken"];//we need to store these values in another place
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q={search}&type=artist");
                request.Headers.Add("Authorization", spotifyToken);
            var response = await _httpClient.SendAsync(request);
            List<NameIdUrlImage> artists = new List<NameIdUrlImage>();
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                
                foreach (JToken artist in parsed.SelectTokens("artists.items[*]"))
                {
                    string imageUrl = "";
                    foreach (JToken image in artist.SelectTokens("images[*].url"))
                    {
                        imageUrl = image.Value<string>();
                        break;
                    }
                    artists.Add(
                        new NameIdUrlImage()
                        {
                            Name = artist.SelectToken("name").Value<string>(),
                            Id = artist.SelectToken("id").Value<string>(),
                            Url = artist.SelectToken("external_urls.spotify").Value<string>(),
                            Image = imageUrl
                        }
                        );
                }

            }
            else
            {
                if (!secondtime)
                {
                    await RefreshToken();
                    return await OnPostSearchByArtistNameAsync(search, true);
                }
                return BadRequest(response.ToString());
            }

            /*
            //Warning: This is a private wrapper around the spotify API, created only for this project and incapable of
            //         responding large number of requests (both server and Spotify user limitations),
            //         so please do not use this proxy in other projects because you will cause this proxy to become unavailable for me
            //         Thanks!
            var response = await _httpClient.GetAsync($"http://spotify.ganjoor.net/spotifyapi/search/artists/{HttpUtility.UrlEncode(search)}");

            NameIdUrlImage[] artists = new NameIdUrlImage[] { };

            if (response.StatusCode == HttpStatusCode.OK)
            {
                artists = JsonConvert.DeserializeObject<NameIdUrlImage[]>(await response.Content.ReadAsStringAsync());
            }
            */


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

        private async Task RefreshToken()
        {
            string refresh_token = _configuration.GetSection("Spofify")["RefreshToken"];//we need to store these values in another place
            var nvc = new List<KeyValuePair<string, string>>();
            nvc.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
            nvc.Add(new KeyValuePair<string, string>("refresh_token", refresh_token));
            var formContent = new FormUrlEncodedContent(nvc);
            var request = new HttpRequestMessage(HttpMethod.Post,
            "https://accounts.spotify.com/api/token");
            request.Content = formContent;
            string authValue = Convert.ToBase64String(new ASCIIEncoding().GetBytes($"{_configuration.GetSection("Spofify")["ClientID"]}:{_configuration.GetSection("Spofify")["ClientSecret"]}"));
            request.Headers.Add("Authorization", $"Basic {authValue}");
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                string access_token = parsed.SelectToken("access_token").Value<string>();


                _configuration.GetSection("Spofify")["AccessToken"] = access_token;
            }
        }

        /// <summary>
        /// fill artist albums
        /// </summary>
        /// <param name="artist">is an ID and consists of numeric and non-numeric characters</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostFillAlbumsAsync(string artist)
        {
            //Warning: This is a private wrapper around the spotify API, created only for this project and incapable of
            //         responding large number of requests (both server and Spotify user limitations),
            //         so please do not use this proxy in other projects because you will cause this proxy to become unavailable for me
            //         Thanks!
            var response = await _httpClient.GetAsync($"http://spotify.ganjoor.net/spotifyapi/artists/{artist}/albums");

            NameIdUrlImage[] albums = new NameIdUrlImage[] { };

            if (response.StatusCode == HttpStatusCode.OK)
            {
                albums = JsonConvert.DeserializeObject<NameIdUrlImage[]>(await response.Content.ReadAsStringAsync());
            }
            return new OkObjectResult(albums);
        }

        /// <summary>
        /// fill album tracks
        /// </summary>
        /// <param name="album">is an ID and consists of numeric and non-numeric characters</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostFillTracksAsync(string album)
        {
            //Warning: This is a private wrapper around the spotify API, created only for this project and incapable of
            //         responding large number of requests (both server and Spotify user limitations),
            //         so please do not use this proxy in other projects because you will cause this proxy to become unavailable for me
            //         Thanks!
            var response = await _httpClient.GetAsync($"http://spotify.ganjoor.net/spotifyapi/albums/{album}/tracks");

            NameIdUrlImage[] tracks = new NameIdUrlImage[] { };

            if (response.StatusCode == HttpStatusCode.OK)
            {
                tracks = JsonConvert.DeserializeObject<NameIdUrlImage[]>(await response.Content.ReadAsStringAsync());
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
            //Warning: This is a private wrapper around the spotify API, created only for this project and incapable of
            //         responding large number of requests (both server and Spotify user limitations),
            //         so please do not use this proxy in other projects because you will cause this proxy to become unavailable for me
            //         Thanks!
            var response = await _httpClient.GetAsync($"http://spotify.ganjoor.net/spotifyapi/search/tracks/{search}");

            TrackQueryResult[] tracks = new TrackQueryResult[] { };

            if (response.StatusCode == HttpStatusCode.OK)
            {
                tracks = JsonConvert.DeserializeObject<TrackQueryResult[]>(await response.Content.ReadAsStringAsync());
            }

            return new PartialViewResult()
            {
                ViewName = "_SpotifySearchPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SpotifySearchPartialModel()
                    {
                        Tracks = tracks
                    }
                }
            };
        }
    }
}
