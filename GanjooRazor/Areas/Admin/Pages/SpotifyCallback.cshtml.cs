using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SpotifyCallbackModel : PageModel
    {
        public SpotifyCallbackModel(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            Configuration = configuration;
        }
        public IActionResult OnGet(string code = "code", string state = "none")
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            Code = code;
            State = state;
            return Page();
        }

        public async Task<ActionResult> OnPostAsync(string code)
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            var nvc = new List<KeyValuePair<string, string>>();
            nvc.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            nvc.Add(new KeyValuePair<string, string>("code", code));

            string callbackUrl = $"{Configuration["SiteUrl"]}/Admin/SpotifyCallback";

            nvc.Add(new KeyValuePair<string, string>("redirect_uri", callbackUrl));

            var formContent = new FormUrlEncodedContent(nvc);
            var client = _clientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
            "https://accounts.spotify.com/api/token");
            request.Content = formContent;
            string authValue = Convert.ToBase64String(new ASCIIEncoding().GetBytes($"{Configuration.GetSection("Spotify")["client_id"]}:{Configuration.GetSection("Spotify")["client_secret"]}"));
            request.Headers.Add("Authorization", $"Basic {authValue}");
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                access_token = parsed.SelectToken("access_token").Value<string>();
                refresh_token = parsed.SelectToken("refresh_token").Value<string>();
                token_type = parsed.SelectToken("token_type").Value<string>();
                expires_in = parsed.SelectToken("expires_in").Value<string>();

                string encryptedAccessToken = EncDecUtil.Encrypt(access_token, Configuration.GetSection("Spotify")["Salt"]);
                string encryptedRefreshToken = EncDecUtil.Encrypt(refresh_token, Configuration.GetSection("Spotify")["Salt"]);

                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var responseSaveOption = await secureClient.PutAsync($"{APIRoot.Url}/api/options/global/SpotifyAccessToken", new StringContent(JsonConvert.SerializeObject(encryptedAccessToken), Encoding.UTF8, "application/json"));
                        if (!responseSaveOption.IsSuccessStatusCode)
                        {
                            Error = JsonConvert.DeserializeObject<string>(await responseSaveOption.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            responseSaveOption = await secureClient.PutAsync($"{APIRoot.Url}/api/options/global/SpotifyRefreshToken", new StringContent(JsonConvert.SerializeObject(encryptedRefreshToken), Encoding.UTF8, "application/json"));
                            if (!responseSaveOption.IsSuccessStatusCode)
                            {
                                Error = JsonConvert.DeserializeObject<string>(await responseSaveOption.Content.ReadAsStringAsync());
                            }
                        }
                    }
                    else
                    {
                        Error = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                    }
                }

            }
            else
            {
                Error = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
            }
            return new OkResult();
        }

        public string Code { get; set; }
        public string State { get; set; }
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string refresh_token { get; set; }

        public string Error { get; set; }

        private readonly IHttpClientFactory _clientFactory;

        private readonly IConfiguration Configuration;
    }
}
