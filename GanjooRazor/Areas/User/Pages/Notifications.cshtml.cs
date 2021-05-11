using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using GSpotifyProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Notification.ViewModels;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class NotificationsModel : PageModel
    {
        // <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// pagination links
        /// </summary>
        public List<NameIdUrlImage> PaginationLinks { get; set; }

        /// <summary>
        /// Notifications
        /// </summary>
        public List<RUserNotificationViewModel> Notifications { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastError = "";
            using (HttpClient secureClient = new HttpClient())
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    {
                        int pageNumber = 1;
                        if (!string.IsNullOrEmpty(Request.Query["page"]))
                        {
                            pageNumber = int.Parse(Request.Query["page"]);
                        }
                        var response = await secureClient.GetAsync($"{APIRoot.Url}/api/notifications/paginated?PageNumber={pageNumber}&PageSize=20");
                        if (!response.IsSuccessStatusCode)
                        {
                            LastError = await response.Content.ReadAsStringAsync();
                            return Page();
                        }
                        response.EnsureSuccessStatusCode();

                        Notifications = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<RUserNotificationViewModel>>();

                        string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
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
                                            Url = "/User/Notifications/?page=1"
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
                                                    Url = $"/User/Notifications/?page={i}"
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
                                           Url = $"/User/Notifications/?page={paginationMetadata.totalPages}"
                                       }
                                       );
                                }
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

        public async Task<IActionResult> OnDeleteNotification(Guid id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/notifications/{id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Redirect($"/login?redirect={Request.Path}&error={await response.Content.ReadAsStringAsync()}");
                    }

                }
            }
            return new JsonResult(true);
        }
    }
}
