using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Models.MuseumLink;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class FinalizeImagesModel : PageModel
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// total count
        /// </summary>
        public int TotalCount { get; set; }

        public GanjoorLinkViewModel MainImage { get; set; }

        public GanjoorLinkViewModel PreviouslyLinkedImage { get; set; }

        [BindProperty]
        public ImageLinkFinalizeModel ImageLinkFinalizeModel { get; set; }

        public async Task OnGetAsync()
        {
            LastError = "";
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            MainImage = null;
            PreviouslyLinkedImage = null;
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var imageResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/artifacts/ganjoor/nextunsychedimage?skip={Skip}");
                    if (!imageResponse.IsSuccessStatusCode)
                    {
                        LastError = await imageResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        string paginnationMetadata = imageResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                        if (!string.IsNullOrEmpty(paginnationMetadata))
                        {
                            TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata).totalCount;
                        }

                        var images = JsonConvert.DeserializeObject<GanjoorLinkViewModel[]>(await imageResponse.Content.ReadAsStringAsync());

                        if(images != null)
                        {
                            if(images.Length >= 1)
                            {   
                                MainImage = images[0];

                                ImageLinkFinalizeModel = new ImageLinkFinalizeModel()
                                {
                                    Id = MainImage.Id,
                                    DisplayOnPage = false
                                };
                            }

                            if (images.Length >= 2)
                            {
                                PreviouslyLinkedImage = images[1];
                            }
                        }
                        
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastError = "";
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var imageResponse = await secureClient.PutAsync($"{APIRoot.Url}/api/artifacts/ganjoor/sync/{ImageLinkFinalizeModel.Id}/{ImageLinkFinalizeModel.DisplayOnPage}", null);
                    if (!imageResponse.IsSuccessStatusCode)
                    {
                        LastError = await imageResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return Redirect($"/User/FinalizeImages/?skip={Skip}");
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }
            return Page();
        }
    }
}
