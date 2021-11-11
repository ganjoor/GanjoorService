using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Accounting;
using RMuseum.Models.Accounting.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ExpenseEditModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public ExpenseEditModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [BindProperty]
        public UpdateDateDescriptionViewModel Expense { get; set; }

        public string LastMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/donations/expense/{Request.Query["id"]}");

            if(!response.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            var expense = JsonConvert.DeserializeObject<GanjoorExpense>(await response.Content.ReadAsStringAsync());

            Expense = new UpdateDateDescriptionViewModel()
            {
                Date = expense.ExpenseDate,
                Description = expense.Description
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/donations/expense/{Request.Query["id"]}", new StringContent(JsonConvert.SerializeObject(Expense), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }
                return Redirect("/Admin/Expenses");
            }
        }
    }
}
