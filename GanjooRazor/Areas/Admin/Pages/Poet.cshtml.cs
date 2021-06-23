using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PoetModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public PoetModel(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// last result
        /// </summary>
        public string LastResult { get; set; }


        [BindProperty]
        public GanjoorPoetViewModel Poet { get; set; }

        [BindProperty]
        public IFormFile Image { get; set; }

        [BindProperty]
        public IFormFile SQLiteDb { get; set; }


        public class SQLiteCorrecionDbModel
        {
            public IFormFile Db { get; set; }

            public string Note { get; set; }
        }

        [BindProperty]
        public SQLiteCorrecionDbModel CorrecionDbModel { get; set; }


        private async Task PreparePoet()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{Request.Query["id"]}");

            response.EnsureSuccessStatusCode();

            var poet = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());


            Poet = poet.Poet;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LastResult = "";
            if(string.IsNullOrEmpty(Request.Query["id"]))
            {
                Poet = new GanjoorPoetViewModel()
                {
                    Nickname = "",
                    Name = "",
                    FullUrl = "",
                    Description = "",
                    Published = false
                };
            }
            else
            {
                await PreparePoet();
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostEditPoetAsync(GanjoorPoetViewModel Poet)
        {
            LastResult = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                if (Request.Query["id"].ToString() == "0")
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/poet", new StringContent(JsonConvert.SerializeObject(Poet), Encoding.UTF8, "application/json"));
                    response.EnsureSuccessStatusCode();

                    var poet = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());

                    var cacheKey1 = $"/api/ganjoor/poets";
                    if (_memoryCache.TryGetValue(cacheKey1, out List<GanjoorPoetViewModel> poets))
                    {
                        _memoryCache.Remove(cacheKey1);
                    }


                    return Redirect($"/Admin/Poet?id={poet.Poet.Id}");
                }
                else
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/poet/{Request.Query["id"]}", new StringContent(JsonConvert.SerializeObject(Poet), Encoding.UTF8, "application/json"));
                    response.EnsureSuccessStatusCode();

                    var cacheKey1 = $"/api/ganjoor/poets";
                    if (_memoryCache.TryGetValue(cacheKey1, out List<GanjoorPoetViewModel> poets))
                    {
                        _memoryCache.Remove(cacheKey1);
                    }

                    var cacheKey2 = $"/api/ganjoor/poet/{Request.Query["id"]}";
                    if (_memoryCache.TryGetValue(cacheKey2, out GanjoorPoetCompleteViewModel poet))
                    {
                        _memoryCache.Remove(cacheKey2);
                    }

                    LastResult = $"ویرایش انجام شد. <a role=\"button\" href=\"/Admin/Poet?id={Request.Query["id"]}\" class=\"actionlink\">برگشت به صفحهٔ ویرایش شاعر</a>";

                    await PreparePoet();

                    return Page();
                }
                
            }
        }

        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile Image)
        {
            LastResult = "";
            await PreparePoet();


            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                
                MultipartFormDataContent form = new MultipartFormDataContent();

                using (MemoryStream stream = new MemoryStream())
                {
                    await Image.CopyToAsync(stream);
                    var fileContent = stream.ToArray();
                    form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), Poet.Nickname, Image.FileName);

                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/poet/image/{Request.Query["id"]}", form);
                    response.EnsureSuccessStatusCode();

                    LastResult = $"تصویر بارگذاری شد. <a role=\"button\" href=\"/Admin/Poet?id={Request.Query["id"]}\" class=\"actionlink\">برگشت به صفحهٔ ویرایش شاعر</a>";

                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUploadDbAsync(IFormFile SQLiteDb)
        {
            LastResult = "";
            await PreparePoet();


            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                MultipartFormDataContent form = new MultipartFormDataContent();

                using (MemoryStream stream = new MemoryStream())
                {
                    await SQLiteDb.CopyToAsync(stream);
                    var fileContent = stream.ToArray();
                    form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), Poet.Nickname, SQLiteDb.FileName);

                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sqlite/import/{Request.Query["id"]}", form);
                    response.EnsureSuccessStatusCode();

                    LastResult = $"پایگاه داده‌ها بارگذاری شد. <a role=\"button\" href=\"/Admin/Poet?id={Request.Query["id"]}\" class=\"actionlink\">برگشت به صفحهٔ ویرایش شاعر</a>";

                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUploadCorrectionDbAsync(SQLiteCorrecionDbModel CorrecionDbModel)
        {
            LastResult = "";
            await PreparePoet();


            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                MultipartFormDataContent form = new MultipartFormDataContent();

                using (MemoryStream stream = new MemoryStream())
                {
                    await CorrecionDbModel.Db.CopyToAsync(stream);
                    var fileContent = stream.ToArray();
                    form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), Poet.Nickname, CorrecionDbModel.Db.FileName);

                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sqlite/update/{Request.Query["id"]}?note={CorrecionDbModel.Note}", form);
                    response.EnsureSuccessStatusCode();

                    LastResult = $"پایگاه داده‌های اصلاحی بارگذاری شد. <a role=\"button\" href=\"/Admin/Poet?id={Request.Query["id"]}\" class=\"actionlink\">برگشت به صفحهٔ ویرایش شاعر</a>";

                }
            }

            return Page();
        }
    }
}
