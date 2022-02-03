using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    public class SuggestedPoetSpecLinesModel : PageModel
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public GanjoorPoetSuggestedSpecLineViewModel Suggestion { get; set; }

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

        public async Task OnGetAsync()
        {
            LastError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var suggestionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/poetspecs/unpublished/next?skip={Skip}");
                    if (!suggestionResponse.IsSuccessStatusCode)
                    {
                        if(suggestionResponse.StatusCode == System.Net.HttpStatusCode.NotFound )
                        {
                            LastError = "مورد دیگری وجود ندارد.";
                        }
                        LastError = JsonConvert.DeserializeObject<string>(await suggestionResponse.Content.ReadAsStringAsync());
                        return;
                    }
                    else
                    {
                        string paginnationMetadata = suggestionResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                        if (!string.IsNullOrEmpty(paginnationMetadata))
                        {
                            TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata).totalCount;
                        }


                        Suggestion = JsonConvert.DeserializeObject<GanjoorPoetSuggestedSpecLineViewModel>(await suggestionResponse.Content.ReadAsStringAsync());

                        var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{Suggestion.PoetId}");
                        if (!response.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                            return;
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
        }
    }
}
