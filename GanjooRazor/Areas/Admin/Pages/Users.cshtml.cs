using GanjooRazor.Utils;
using GSpotifyProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class UsersModel : PageModel
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        public PublicRAppUser[] Users { get; set; }

        public PaginationMetadata Paging { get; set; }

        public List<NameIdUrlImage> PaginationLinks { get; set; } = new List<NameIdUrlImage>();

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 15;

        public string FilterByEmail { get; set; }

        public string FilterByNickName { get; set; }

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1, int pageSize = 15, string filterByEmail = null, string filterByNickName = null)
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize;
            FilterByEmail = filterByEmail;
            FilterByNickName = filterByNickName;

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var url = $"{APIRoot.Url}/api/users?PageNumber={PageNumber}&PageSize={PageSize}";
                    if (!string.IsNullOrEmpty(FilterByEmail))
                        url += $"&filterByEmail={Uri.EscapeDataString(FilterByEmail)}";
                    if (!string.IsNullOrEmpty(FilterByNickName))
                        url += $"&filterByNickName={Uri.EscapeDataString(FilterByNickName)}";

                    var response = await secureClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        Users = JsonConvert.DeserializeObject<PublicRAppUser[]>(await response.Content.ReadAsStringAsync());
                        if (response.Headers.TryGetValues("paging-headers", out var values))
                        {
                            Paging = JsonConvert.DeserializeObject<PaginationMetadata>(values.FirstOrDefault());
                            if (Paging != null)
                            {
                                for (int p = 1; p <= Paging.totalPages; p++)
                                {
                                    var pageUrl = $"?pageNumber={p}&pageSize={PageSize}";
                                    if (!string.IsNullOrEmpty(FilterByEmail))
                                        pageUrl += $"&filterByEmail={Uri.EscapeDataString(FilterByEmail)}";
                                    if (!string.IsNullOrEmpty(FilterByNickName))
                                        pageUrl += $"&filterByNickName={Uri.EscapeDataString(FilterByNickName)}";
                                    PaginationLinks.Add(new NameIdUrlImage()
                                    {
                                        Name = p.ToString(),
                                        Url = p == PageNumber ? "" : pageUrl
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        LastError = await response.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    LastError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }

            return Page();
        }

        /// <summary>
        /// role names for a specific user - ?handler=Roles&id=...
        /// </summary>
        public async Task<IActionResult> OnGetRolesAsync(Guid id)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var response = await secureClient.GetAsync($"{APIRoot.Url}/api/users/{id}/roles");
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                var roles = JsonConvert.DeserializeObject<string[]>(await response.Content.ReadAsStringAsync());
                return new JsonResult(roles);
            }
        }

        /// <summary>
        /// all defined role names, for populating the "add role" picker - ?handler=AllRoles
        /// </summary>
        public async Task<IActionResult> OnGetAllRolesAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var response = await secureClient.GetAsync($"{APIRoot.Url}/api/roles");
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                var roles = JsonConvert.DeserializeObject<RAppRole[]>(await response.Content.ReadAsStringAsync());
                return new JsonResult(roles.Select(r => r.Name).ToArray());
            }
        }

        /// <summary>
        /// add a role to a user - ?handler=AddRole
        /// </summary>
        public async Task<IActionResult> OnPostAddRoleAsync(Guid id, string role)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/users/{id}/roles/{Uri.EscapeDataString(role)}", null);
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                return new JsonResult(true);
            }
        }

        /// <summary>
        /// remove a role from a user - ?handler=Role (DELETE)
        /// </summary>
        public async Task<IActionResult> OnDeleteRoleAsync(Guid id, string role)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/users/{id}/roles/{Uri.EscapeDataString(role)}");
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                return new JsonResult(true);
            }
        }

        /// <summary>
        /// toggle a user's active/inactive status - ?handler=ToggleStatus
        /// </summary>
        public async Task<IActionResult> OnPostToggleStatusAsync(Guid id, bool active)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var userInfoResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/users/{id}");
                if (!userInfoResponse.IsSuccessStatusCode)
                    return BadRequest(await userInfoResponse.Content.ReadAsStringAsync());

                var userInfo = JsonConvert.DeserializeObject<PublicRAppUser>(await userInfoResponse.Content.ReadAsStringAsync());

                // ModifyUser rejects the request unless IsAdmin exactly matches the user's actual
                // current admin-role membership (this endpoint cannot itself change admin status),
                // so we need their real membership, not a guess.
                var rolesResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/users/{id}/roles");
                if (!rolesResponse.IsSuccessStatusCode)
                    return BadRequest(await rolesResponse.Content.ReadAsStringAsync());
                var currentRoles = JsonConvert.DeserializeObject<string[]>(await rolesResponse.Content.ReadAsStringAsync());
                bool isCurrentlyAdmin = currentRoles.Contains("Administrator");

                var updateModel = new RegisterRAppUser()
                {
                    Password = "",
                    IsAdmin = isCurrentlyAdmin,
                    Id = userInfo.Id,
                    Username = userInfo.Username,
                    Email = userInfo.Email,
                    PhoneNumber = userInfo.PhoneNumber,
                    FirstName = userInfo.FirstName,
                    SurName = userInfo.SurName,
                    Status = active ? RAppUserStatus.Active : RAppUserStatus.Inactive,
                    RImageId = userInfo.RImageId,
                    NickName = userInfo.NickName,
                    Bio = userInfo.Bio,
                    Website = userInfo.Website
                };

                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/users/{id}", new StringContent(JsonConvert.SerializeObject(updateModel), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                return new JsonResult(true);
            }
        }
    }
}
