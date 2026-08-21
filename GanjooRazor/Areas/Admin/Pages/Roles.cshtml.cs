using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class RolesModel : PageModel
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        public RAppRole[] Roles { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/roles");
                    if (response.IsSuccessStatusCode)
                    {
                        Roles = JsonConvert.DeserializeObject<RAppRole[]>(await response.Content.ReadAsStringAsync());
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
        /// create a new role - ?handler=NewRole
        /// </summary>
        public async Task<IActionResult> OnPostNewRoleAsync(string name, string description)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var newRole = new { name = name, description = description };
                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/roles", new StringContent(JsonConvert.SerializeObject(newRole), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                return new JsonResult(true);
            }
        }

        /// <summary>
        /// delete a role - ?handler=Role (DELETE)
        /// </summary>
        public async Task<IActionResult> OnDeleteRoleAsync(string roleName)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/roles/{roleName}");
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                return new JsonResult(true);
            }
        }

        /// <summary>
        /// securable items tree annotated with this role's current permission status - ?handler=Permissions&roleName=...
        /// </summary>
        public async Task<IActionResult> OnGetPermissionsAsync(string roleName)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                var response = await secureClient.GetAsync($"{APIRoot.Url}/api/roles/permissions/{roleName}");
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                var items = JsonConvert.DeserializeObject<SecurableItem[]>(await response.Content.ReadAsStringAsync());
                return new JsonResult(items);
            }
        }

        /// <summary>
        /// save this role's permissions - ?handler=Permissions&roleName=... (PUT)
        /// </summary>
        public async Task<IActionResult> OnPutPermissionsAsync(string roleName)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    return BadRequest("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");

                string body;
                using (var reader = new StreamReader(Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }

                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/roles/permissions/{roleName}", new StringContent(body, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                    return BadRequest(await response.Content.ReadAsStringAsync());

                return new JsonResult(true);
            }
        }
    }
}
