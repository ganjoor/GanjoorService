using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Areas.Admin.Pages
{
    /// <summary>
    /// moderation of literary devices (آرایه‌ها) suggested by users and management of literary concepts (مفاهیم)
    /// </summary>
    public class LiteraryDevicesModel : PageModel
    {
        public string LastMessage { get; set; }

        public LiteraryDevice[] SuggestedDevices { get; set; } = Array.Empty<LiteraryDevice>();

        public LiteraryDevice[] ApprovedDevices { get; set; } = Array.Empty<LiteraryDevice>();

        private static JsonSerializerSettings JsonSettings =>
            new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

        public string SuggestedDevicesJson => JsonConvert.SerializeObject(SuggestedDevices, JsonSettings);

        public string ApprovedDevicesJson => JsonConvert.SerializeObject(ApprovedDevices, JsonSettings);

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var suggested = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/devices/suggested");
                    if (!suggested.IsSuccessStatusCode)
                    {
                        LastMessage = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(suggested);
                        return Page();
                    }
                    SuggestedDevices = JsonConvert.DeserializeObject<LiteraryDevice[]>(await suggested.Content.ReadAsStringAsync());

                    var approved = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/devices");
                    if (!approved.IsSuccessStatusCode)
                    {
                        LastMessage = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(approved);
                        return Page();
                    }
                    ApprovedDevices = JsonConvert.DeserializeObject<LiteraryDevice[]>(await approved.Content.ReadAsStringAsync());
                }
                else
                {
                    LastMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        /// <summary>
        /// approve (with the moderator's edits) or reject a device: Status of the posted device is the decision
        /// </summary>
        public async Task<IActionResult> OnPostModerateDeviceAsync([FromBody] LiteraryDevice device)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/literary/device/moderate",
                        new StringContent(JsonConvert.SerializeObject(device), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        /// <summary>
        /// adds the core devices that are missing
        /// </summary>
        public async Task<IActionResult> OnPostSeedCoreAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/literary/devices/seed-core", new StringContent("", Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    return new OkObjectResult(JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync()));
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        /// <summary>
        /// concept search (with parent names)
        /// </summary>
        public async Task<IActionResult> OnGetConceptsAsync(string query)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/concepts?query={Uri.EscapeDataString(query ?? "")}&take=30");
                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    var concepts = JsonConvert.DeserializeObject<LiteraryConcept[]>(await response.Content.ReadAsStringAsync());

                    var parentNames = new Dictionary<int, string>();
                    var parentIds = concepts.Where(c => c.ParentId != null).Select(c => c.ParentId.Value).Distinct().ToArray();
                    if (parentIds.Length > 0)
                    {
                        var pr = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/concepts/by-ids?ids={string.Join(",", parentIds)}");
                        if (pr.IsSuccessStatusCode)
                            foreach (var p in JsonConvert.DeserializeObject<LiteraryConcept[]>(await pr.Content.ReadAsStringAsync()))
                                parentNames[p.Id] = p.Name;
                    }
                    return new JsonResult(concepts.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        aliases = c.Aliases,
                        parentId = c.ParentId,
                        parentName = c.ParentId != null && parentNames.ContainsKey(c.ParentId.Value) ? parentNames[c.ParentId.Value] : null
                    }).ToArray());
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        public async Task<IActionResult> OnPutConceptAsync([FromBody] LiteraryConcept concept)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/literary/concept",
                        new StringContent(JsonConvert.SerializeObject(concept), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }

        public async Task<IActionResult> OnPostMergeAsync(int sourceId, int targetId)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/literary/concept/merge/{sourceId}/{targetId}", new StringContent("", Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                        return new BadRequestObjectResult(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    return new OkObjectResult(true);
                }
            }
            return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
        }
    }
}
