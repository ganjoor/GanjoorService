using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    /// <summary>
    /// data problems in the kinship relations and affiliations - loops in the Parent/Ancestor graph (which
    /// the family tree chart cannot draw), suspicious parents, duplicates, incompatible relations between
    /// the same two people. Read-only: each row links to the person window where it can be dealt with.
    /// </summary>
    public class RelationProblemsModel : PageModel
    {
        /// <summary>
        /// what was found
        /// </summary>
        public GanjoorRelationProblemsViewModel Problems { get; set; }

        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        /// <summary>
        /// get
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/people/relations/problems");
                    if (!response.IsSuccessStatusCode)
                    {
                        FatalError = await ApiErrorReader.ReadErrorAsync(response);
                        return Page();
                    }
                    Problems = JsonConvert.DeserializeObject<GanjoorRelationProblemsViewModel>(await response.Content.ReadAsStringAsync());
                    return Page();
                }
                return Redirect("/");
            }
        }
    }
}
