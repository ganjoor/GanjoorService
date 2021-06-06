using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Accounting;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ExpensesModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public ExpensesModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }
        [BindProperty]
        public GanjoorExpense Expense { get; set; }

        /// <summary>
        /// donations
        /// </summary>
        public GanjoorExpense[] Expenses { get; set; }

        /// <summary>
        /// show account info
        /// </summary>
        public string ShowAccountInfo { get; set; }

        private async Task ReadExpenses()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/donations/expense");
            if (!response.IsSuccessStatusCode)
            {
                LastMessage = await response.Content.ReadAsStringAsync();
            }

            response.EnsureSuccessStatusCode();

            Expenses = JsonConvert.DeserializeObject<GanjoorExpense[]>(await response.Content.ReadAsStringAsync());

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage resAccountInfo = await secureClient.GetAsync($"{APIRoot.Url}/api/donations/accountinfo/visible");
                    if (!resAccountInfo.IsSuccessStatusCode)
                    {
                        LastMessage = await resAccountInfo.Content.ReadAsStringAsync();
                    }

                    resAccountInfo.EnsureSuccessStatusCode();

                    ShowAccountInfo = JsonConvert.DeserializeObject<bool>(await resAccountInfo.Content.ReadAsStringAsync()) ? "نمایش حساب فعال است." : "نمایش حساب غیرفعال است.";
                }
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";

            Expense = new GanjoorExpense()
            {
                ExpenseDate = DateTime.Now.Date,
                Unit = "تومان",
            };

            await ReadExpenses();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(GanjoorExpense Expense)
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {

                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/donations/expense", new StringContent(JsonConvert.SerializeObject(Expense), Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = await response.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }

            return Page();
        }

        public async Task<IActionResult> OnPostRebuildPageAsync()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/donations/page", null);
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/donations/expense/{id}");
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }
    }
}
