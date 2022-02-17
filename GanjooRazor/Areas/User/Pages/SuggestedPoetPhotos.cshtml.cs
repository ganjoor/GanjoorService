﻿using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SuggestedPoetPhotosModel : PageModel
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public GanjoorPoetSuggestedPictureViewModel Suggestion { get; set; }

        /// <summary>
        /// poet
        /// </summary>
        public GanjoorPoetViewModel Poet { get; set; }

        /// <summary>
        /// skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// total count
        /// </summary>
        public int TotalCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {

                    if(!string.IsNullOrEmpty(Request.Query["id"]) && !string.IsNullOrEmpty(Request.Query["id"]))
                    {
                        //modify mode:
                        int id = int.Parse(Request.Query["id"]);
                        int poetId = int.Parse(Request.Query["poetId"]);

                        var responsePhotos = await secureClient.GetAsync($"{APIRoot.Url}/api/poetphotos/poet/{poetId}");
                        if (!responsePhotos.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await responsePhotos.Content.ReadAsStringAsync());
                            return Page();
                        }
                        var photos = JArray.Parse(await responsePhotos.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetSuggestedPictureViewModel>>();

                        Suggestion = photos.Where(p => p.Id == id).FirstOrDefault();
                        if (Suggestion == null)
                        {
                            LastError = "تصویری با شناسهٔ ارسالی یافت نشد.";
                            return Page();
                        }
                    }
                    else
                    {
                        //moderate mode
                        var suggestionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/poetphotos/unpublished/next?skip={Skip}");
                        if (!suggestionResponse.IsSuccessStatusCode)
                        {
                            if (suggestionResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                LastError = "پیشنهادی وجود ندارد.";
                            }
                            else
                            {
                                LastError = JsonConvert.DeserializeObject<string>(await suggestionResponse.Content.ReadAsStringAsync());
                            }
                            return Page();
                        }
                        else
                        {
                            string paginnationMetadata = suggestionResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                            if (!string.IsNullOrEmpty(paginnationMetadata))
                            {
                                TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata).totalCount;
                            }
                            Suggestion = JsonConvert.DeserializeObject<GanjoorPoetSuggestedPictureViewModel>(await suggestionResponse.Content.ReadAsStringAsync());
                        }
                    }

                    if (Suggestion != null)
                    {
                        Suggestion.ImageUrl = $"{APIRoot.InternetUrl}/{Suggestion.ImageUrl}";

                        var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{Suggestion.PoetId}");
                        if (!response.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                            return Page();
                        }

                        var poet = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());
                        Poet = poet.Poet;
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);

            if (Request.Form["next"].Count == 1)
            {
                return Redirect($"/User/SuggestedPoetPhotos/?skip={Skip + 1}");
            }

            Suggestion.Published = Request.Form["approve"].Count == 1;

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    if (Suggestion.Published)
                    {
                        var putResponse = await secureClient.PutAsync($"{APIRoot.Url}/api/poetphotos", new StringContent(JsonConvert.SerializeObject(Suggestion), Encoding.UTF8, "application/json"));
                        if (!putResponse.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await putResponse.Content.ReadAsStringAsync());
                        }
                    }
                    else
                    {
                        var deleteResponse = await secureClient.DeleteAsync($"{APIRoot.Url}/api/poetphotos/{Suggestion.Id}");
                        if (!deleteResponse.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await deleteResponse.Content.ReadAsStringAsync());
                        }
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

            return Redirect($"/User/SuggestedPoetPhotos/?skip={Skip}");

        }
    }
}
