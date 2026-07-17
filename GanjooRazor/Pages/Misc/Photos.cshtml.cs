using GanjooRazor.Models;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PhotosModel : LoginPartialEnabledPageModel
    {
        public string LastError { get; set; }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public GanjoorPoetViewModel Poet { get; set; }

        public List<GanjoorPoetSuggestedSpecLineViewModel> SpecLines { get; set; }

        public List<GanjoorPoetSuggestedPictureViewModel> Photos { get; set; }

        [BindProperty]
        public PoetPhotoSuggestionUploadModel Upload { get; set; }

        public GanjoorPoetSuggestedPictureViewModel UploadedPhoto { get; set; }

        public bool ModeratePoetPhotos { get; set; }

        // Kept local rather than PoetCacheService: unlike every other page that fetches the poet
        // list, this one prefixes each poet's ImageUrl with APIRoot.InternetUrl before use, which
        // the shared service intentionally doesn't do (no other caller needed it).
        private async Task<List<GanjoorPoetViewModel>> _PreparePoets()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return new List<GanjoorPoetViewModel>();
            }
            var poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();

            foreach (var poet in poets)
            {
                poet.ImageUrl = $"{APIRoot.InternetUrl}{poet.ImageUrl}";
            }

            return poets;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            InitializeCommonPageState();

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet?url=/{Request.Query["p"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(response);
                    return Page();
                }
                Poet = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>().Poet;
                Poet.ImageUrl = $"{APIRoot.InternetUrl}{Poet.ImageUrl}";

                var responseLines = await _httpClient.GetAsync($"{APIRoot.Url}/api/poetspecs/poet/{Poet.Id}");
                if (!responseLines.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(responseLines);
                    return Page();
                }
                SpecLines = JArray.Parse(await responseLines.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetSuggestedSpecLineViewModel>>();

                var responsePhotos = await _httpClient.GetAsync($"{APIRoot.Url}/api/poetphotos/poet/{Poet.Id}");
                if (!responsePhotos.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(responsePhotos);
                    return Page();
                }
                Photos = JArray.Parse(await responsePhotos.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetSuggestedPictureViewModel>>();

                foreach (var photo in Photos)
                {
                    photo.ImageUrl = $"{APIRoot.InternetUrl}/{photo.ImageUrl}";
                }

                if (LoggedIn)
                {
                    await GanjoorSessionChecker.ApplyPermissionsToViewData(Request, Response, ViewData);
                    ModeratePoetPhotos = ViewData.ContainsKey($"{RMuseumSecurableItem.GanjoorEntityShortName}-{RMuseumSecurableItem.ModeratePoetPhotos}");
                }

            }
            else
            {
                Poets = await _PreparePoets();
            }

            ViewData["Title"] = Poet == null ? "پیشنهاد تصویر برای سخنوران" : $"پیشنهاد تصویر برای {Poet.Nickname}";

            return Page();
        }

        private IActionResult SpecLineErrorPartial(string error)
        {
            return Partial("_PoetSpecLinePartial", new _PoetSpecLinePartialModel()
            {
                Line = new GanjoorPoetSuggestedSpecLineViewModel()
                {
                    Id = 0,
                    Contents = error
                }
            });
        }

        public Task<IActionResult> OnPostSuggestAsync(int poetId, string contents)
        {
            if (string.IsNullOrEmpty(contents))
            {
                return Task.FromResult(SpecLineErrorPartial("متن خالی است."));
            }

            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/poetspecs",
                    new StringContent(
                    JsonConvert.SerializeObject
                    (
                        new GanjoorPoetSuggestedSpecLineViewModel()
                        {
                            PoetId = poetId,
                            Contents = contents,
                        }
                    ),
                    Encoding.UTF8, "application/json")
                    );
                if (response.IsSuccessStatusCode)
                {
                    var line = JsonConvert.DeserializeObject<GanjoorPoetSuggestedSpecLineViewModel>(await response.Content.ReadAsStringAsync());
                    return Partial("_PoetSpecLinePartial", new _PoetSpecLinePartialModel()
                    {
                        Line = line
                    });
                }

                return SpecLineErrorPartial(await ReadErrorMessageAsync(response));
            }, SpecLineErrorPartial(NotLoggedInMessage));
        }

        public async Task<IActionResult> OnPostAsync(PoetPhotoSuggestionUploadModel Upload)
        {
            if (string.IsNullOrEmpty(Upload.Title))
                LastError = "عنوان خالی است.";
            else
               if (string.IsNullOrEmpty(Upload.Description))
                LastError = "توضیح خالی است.";
            else
               if (Upload.Image == null)
                LastError = "تصویر انتخاب نشده است.";
            else
                // Kept as its own using/PrepareClient block rather than WithSecureClientAsync: this
                // handler needs to fall through to OnGetAsync() regardless of outcome (success,
                // upload failure, or auth failure), which doesn't fit the early-return helper shape.
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        MultipartFormDataContent form = new MultipartFormDataContent();

                        using (MemoryStream stream = new MemoryStream())
                        {
                            form.Add(new StringContent(Upload.PoetId.ToString()), "poetId");
                            form.Add(new StringContent(Upload.Title), "title");
                            form.Add(new StringContent(Upload.Description), "description");
                            form.Add(new StringContent(""), "srcUrl");


                            await Upload.Image.CopyToAsync(stream);
                            var fileContent = stream.ToArray();
                            form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), Upload.Image.FileName, Upload.Image.FileName);

                            HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/poetphotos", form);
                            if (!response.IsSuccessStatusCode)
                            {
                                LastError = await ReadErrorMessageAsync(response);
                            }
                            else
                            {
                                UploadedPhoto = JsonConvert.DeserializeObject<GanjoorPoetSuggestedPictureViewModel>(await response.Content.ReadAsStringAsync());

                                UploadedPhoto.ImageUrl = $"{APIRoot.InternetUrl}/{UploadedPhoto.ImageUrl}";
                            }
                        }
                    }
                    else
                    {
                        LastError = NotLoggedInMessage;
                    }

                }


            return await OnGetAsync();
        }

        public Task<IActionResult> OnPutChoosePhotoAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var responsePhoto = await secureClient.GetAsync($"{APIRoot.Url}/api/poetphotos/{id}");
                if (!responsePhoto.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(responsePhoto);
                    return new BadRequestObjectResult(LastError);
                }
                var photo = JsonConvert.DeserializeObject<GanjoorPoetSuggestedPictureViewModel>(await responsePhoto.Content.ReadAsStringAsync());
                photo.ChosenOne = true;
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/poetphotos", new StringContent(JsonConvert.SerializeObject(photo), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkResult();
            });
        }

        public Task<IActionResult> OnDeleteAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/poetphotos/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkResult();
            });
        }

        public Task<IActionResult> OnDeleteSpecLineAsync(int id)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/poetspecs/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkResult();
            });
        }

        public PhotosModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }
    }
}
