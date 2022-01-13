using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using GSpotifyProxy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class HistoryModel : PageModel
    {

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// pagination links
        /// </summary>
        public List<NameIdUrlImage> PaginationLinks { get; set; }

        /// <summary>
        /// history items
        /// </summary>
        public List<GanjoorUserBookmarkViewModel> HistoryItems { get; set; }

        /// <summary>
        /// tracking is enabled
        /// </summary>
        public bool TrackingIsEnabled { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            LastError = "";
            using (HttpClient secureClient = new HttpClient())
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/options/KeepHistory");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }
                    TrackingIsEnabled = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()) == true.ToString();

                    int pageNumber = 1;
                    if (!string.IsNullOrEmpty(Request.Query["page"]))
                    {
                        pageNumber = int.Parse(Request.Query["page"]);
                    }
                    var responseHistoryItems = await secureClient.GetAsync($"{APIRoot.Url}/api/tracking/?PageNumber={pageNumber}&PageSize=20");
                    if (!responseHistoryItems.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await responseHistoryItems.Content.ReadAsStringAsync());
                        return Page();
                    }

                    HistoryItems = JArray.Parse(await responseHistoryItems.Content.ReadAsStringAsync()).ToObject<List<GanjoorUserBookmarkViewModel>>();

                    string paginnationMetadata = responseHistoryItems.Headers.GetValues("paging-headers").FirstOrDefault();
                    if (!string.IsNullOrEmpty(paginnationMetadata))
                    {
                        PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);
                        PaginationLinks = new List<NameIdUrlImage>();
                        if (paginationMetadata.totalPages > 1)
                        {
                            if (paginationMetadata.currentPage > 3)
                            {
                                PaginationLinks.Add
                                    (
                                    new NameIdUrlImage()
                                    {
                                        Name = "صفحهٔ اول",
                                        Url = "/User/History/?page=1"
                                    }
                                    );
                            }
                            for (int i = (paginationMetadata.currentPage - 2); i <= (paginationMetadata.currentPage + 2); i++)
                            {
                                if (i >= 1 && i <= paginationMetadata.totalPages)
                                {
                                    if (i == paginationMetadata.currentPage)
                                    {

                                        PaginationLinks.Add
                                           (
                                           new NameIdUrlImage()
                                           {
                                               Name = i.ToPersianNumbers(),
                                           }
                                           );
                                    }
                                    else
                                    {

                                        PaginationLinks.Add
                                            (
                                            new NameIdUrlImage()
                                            {
                                                Name = i.ToPersianNumbers(),
                                                Url = $"/User/History/?page={i}"
                                            }
                                            );
                                    }
                                }
                            }
                            if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                            {

                                PaginationLinks.Add
                                    (
                                    new NameIdUrlImage()
                                    {
                                        Name = "... ",
                                    }
                                    );

                                PaginationLinks.Add
                                   (
                                   new NameIdUrlImage()
                                   {
                                       Name = "صفحهٔ آخر",
                                       Url = $"/User/History/?page={paginationMetadata.totalPages}"
                                   }
                                   );
                            }
                        }
                    }


                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            return Page();
        }

        public async Task<IActionResult> OnDeleteHistoryItem(Guid id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/tracking/{id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnPostStopTracking()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/tracking", new StringContent(JsonConvert.SerializeObject(false), Encoding.UTF8, "application/json"));

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                    if(Request.Cookies["KeepHistory"] != null)
                    {
                        Response.Cookies.Delete("KeepHistory");
                    }
                    var cookieOption = new CookieOptions()
                    {
                        Expires = DateTime.Now.AddDays(365),
                    };
                    Response.Cookies.Append("KeepHistory", $"{false}", cookieOption);

                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnPostStartTracking()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/tracking", new StringContent(JsonConvert.SerializeObject(true), Encoding.UTF8, "application/json"));

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                    
                    if (Request.Cookies["KeepHistory"] != null)
                    {
                        Response.Cookies.Delete("KeepHistory");
                    }
                    var cookieOption = new CookieOptions()
                    {
                        Expires = DateTime.Now.AddDays(365),
                    };
                    Response.Cookies.Append("KeepHistory", $"{true}", cookieOption);

                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }


    }
}
