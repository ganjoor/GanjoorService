using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class MusicLinkModel : PageModel
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
        public MusicLinkModel(HttpClient httpClient, IConfiguration configuration)
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

        /// <summary>
        /// every pending suggestion of the poem is listed, whatever source it came from
        /// </summary>
        private async Task _GetSuggestedSongs()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{PoemId}/songs/?approved=false&trackType={(int)PoemMusicTrackType.All}");

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
            PoemMusicTrackViewModel.TrackType = PoemMusicTrackType.MusicUrl;
            InsertedSongId = 0;

            if (!MusicUrlValidator.TryNormalize(PoemMusicTrackViewModel.TrackUrl, out string normalizedUrl, out _, out string urlError))
            {
                LastError = urlError;
                await _GetSuggestedSongs();
                return Page();
            }

            PoemMusicTrackViewModel.TrackUrl = normalizedUrl;

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var stringContent = new StringContent(JsonConvert.SerializeObject(PoemMusicTrackViewModel), Encoding.UTF8, "application/json");
                    var methodUrl = $"{APIRoot.Url}/api/ganjoor/song";
                    var response = await secureClient.PostAsync(methodUrl, stringContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = await _ReadErrorAsync(response);
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
        /// the API returns its errors as a JSON string, but 401s and framework level failures
        /// come back as an empty or non-JSON body
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static async Task<string> _ReadErrorAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                return "نشست شما معتبر نیست. لطفاً از گنجور خارج و مجدداً وارد شوید.";

            string body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return $"خطا در ارتباط با گنجور ({(int)response.StatusCode}).";

            try
            {
                return JsonConvert.DeserializeObject<string>(body) ?? body;
            }
            catch (JsonException)
            {
                return body;
            }
        }

        /// <summary>
        /// best effort track title/artist lookup through the platform's public oEmbed endpoint
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostFetchMetadataAsync(string url)        {
            if (!MusicUrlValidator.TryNormalize(url, out string normalizedUrl, out MusicPlatform platform, out string error))
            {
                return new BadRequestObjectResult(error);
            }

            if (string.IsNullOrEmpty(platform.OEmbedEndpoint))
            {
                return new JsonResult(new { trackName = "", artistName = "" });
            }

            try
            {
                // the pasted url is only ever handed to the platform's own fixed endpoint, and redirects
                // are refused, so this cannot be steered at an arbitrary host
                using var handler = new HttpClientHandler() { AllowAutoRedirect = false };
                using var oEmbedClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5), MaxResponseContentBufferSize = 128 * 1024 };

                var response = await oEmbedClient.GetAsync($"{platform.OEmbedEndpoint}?url={WebUtility.UrlEncode(normalizedUrl)}&format=json");
                if (!response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { trackName = "", artistName = "" });
                }

                var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());

                return new JsonResult
                    (
                    new
                    {
                        trackName = parsed.SelectToken("title")?.Value<string>() ?? "",
                        artistName = parsed.SelectToken("author_name")?.Value<string>() ?? ""
                    }
                    );
            }
            catch
            {
                return new JsonResult(new { trackName = "", artistName = "" });
            }
        }
    }
}
