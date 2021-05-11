using GanjooRazor.Models;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class BannersModel : PageModel
    {
        /// <summary>
        /// image
        /// </summary>
        [BindProperty]
        public BannerUploadModel Upload { get; set; }

        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }


        /// <summary>
        /// banners
        /// </summary>
        public GanjoorSiteBannerViewModel[] Banners { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/site/banners");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = await response.Content.ReadAsStringAsync();
                    }

                    response.EnsureSuccessStatusCode();

                    Banners = JsonConvert.DeserializeObject<GanjoorSiteBannerViewModel[]>(await response.Content.ReadAsStringAsync());

                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }

            return Page();
        }

        /// <summary>
        /// edit
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPutEditAsync(int id, string alt, string url, bool active)
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    GanjoorSiteBannerModifyViewModel model = new GanjoorSiteBannerModifyViewModel()
                    {
                        AlternateText = alt,
                        TargetUrl = url,
                        Active = active
                    };
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/site/banner/{id}", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));
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

            return new JsonResult(true);
        }

        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/site/banner?id={id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Redirect($"/login?redirect={Request.Path}&error={await response.Content.ReadAsStringAsync()}");
                    }

                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnPostAsync(BannerUploadModel Upload)
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    MultipartFormDataContent form = new MultipartFormDataContent();

                    using (MemoryStream stream = new MemoryStream())
                    {

                        form.Add(new StringContent(Upload.Alt), "alt");
                        form.Add(new StringContent(Upload.Url), "url");

                        await Upload.Image.CopyToAsync(stream);
                        var fileContent = stream.ToArray();
                        form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), Upload.Image.FileName, Upload.Image.FileName);

                        HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/site/banner", form);
                        if (!response.IsSuccessStatusCode)
                        {
                            LastMessage = await response.Content.ReadAsStringAsync();
                        }

                    }


                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }


            return Page();
        }
    }
}
