using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// "find a poem about..." chatbox. Deliberately calls APIRoot.SemanticSearchUrl, NOT
    /// APIRoot.Url/InternetUrl — see the comment on that property in APIRoot.cs. Currently a
    /// physically separate domain/app pool (ganjgah.ir) from api.ganjoor.net, so a problem with
    /// this one feature can't affect the main site or its API, a precaution proven necessary by
    /// an actual production incident during development, not a hypothetical one.
    /// </summary>
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SemanticSearchModel : PageModel
    {
        public void OnGet()
        {
        }

        /// <summary>
        /// proxies the query to the semantic search API and passes its JSON straight back to the
        /// page's own JS — no local re-modeling of the response, so there's no risk of a shape
        /// mismatch between the two sides silently dropping a field
        /// </summary>
        public async Task<IActionResult> OnPostSearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("لطفاً عبارتی برای جست‌وجو وارد کنید.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30); // query-time ONNX inference is not instant - give it real room, but not unbounded

                var requestBody = "{\"query\":" + System.Text.Json.JsonSerializer.Serialize(query) + ",\"topK\":10}";
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync($"{APIRoot.SemanticSearchUrl}/api/ganjoor/search/semantic", content);
                }
                catch (Exception)
                {
                    // a network-level failure reaching the isolated semantic-search domain -
                    // exactly the kind of failure this domain-level isolation is meant to
                    // contain to just this feature, so report it plainly rather than let an
                    // unhandled exception propagate
                    return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                        "در حال حاضر جست‌وجوی معنایی در دسترس نیست. لطفاً کمی بعد دوباره امتحان کنید.");
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, responseBody);
                }

                return Content(responseBody, "application/json");
            }
        }
    }
}
