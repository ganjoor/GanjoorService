using GanjooRazor.Models;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        private async Task<List<GanjoorPoetViewModel>> _PreparePoets()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
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
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

           
            
            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet?url=/{Request.Query["p"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }
                Poet = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>().Poet;
                Poet.ImageUrl = $"{APIRoot.InternetUrl}{Poet.ImageUrl}";

                var responseLines = await _httpClient.GetAsync($"{APIRoot.Url}/api/poetspecs/poet/{Poet.Id}");
                if (!responseLines.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await responseLines.Content.ReadAsStringAsync());
                    return Page();
                }
                SpecLines = JArray.Parse(await responseLines.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetSuggestedSpecLineViewModel>>();

                var responsePhotos = await _httpClient.GetAsync($"{APIRoot.Url}/api/poetphotos/poet/{Poet.Id}");
                if (!responsePhotos.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await responsePhotos.Content.ReadAsStringAsync());
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

            ViewData["Title"] = Poet == null ? "پیشنهاد تصویر برای شاعران" : $"پیشنهاد تصویر برای {Poet.Nickname}";

            return Page();
        }

        public async Task<ActionResult> OnPostSuggestAsync(int poetId, string contents)
        {
            string error = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
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
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var line = JsonConvert.DeserializeObject<GanjoorPoetSuggestedSpecLineViewModel>(await response.Content.ReadAsStringAsync());
                        return new PartialViewResult()
                        {
                            ViewName = "_PoetSpecLinePartial",
                            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                            {
                                Model = new _PoetSpecLinePartialModel()
                                {
                                    Line = line
                                }
                            }
                        };
                    }
                    else
                    {
                        error = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                }
                else
                {
                    error = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return new PartialViewResult()
            {
                ViewName = "_PoetSpecLinePartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _PoetSpecLinePartialModel()
                    {
                        Line = new GanjoorPoetSuggestedSpecLineViewModel()
                        {
                            Id = 0,
                            Contents = error
                        }
                    }
                }
            };
        }

        public async Task<IActionResult> OnPostAsync(PoetPhotoSuggestionUploadModel Upload)
        {
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
                            LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            UploadedPhoto = JsonConvert.DeserializeObject<GanjoorPoetSuggestedPictureViewModel>(await response.Content.ReadAsStringAsync());

                            UploadedPhoto.ImageUrl =  $"{APIRoot.InternetUrl}/{UploadedPhoto.ImageUrl}";
                        }
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }


            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPutChoosePhotoAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var responsePhoto = await secureClient.GetAsync($"{APIRoot.Url}/api/poetphotos/{id}");
                    if (!responsePhoto.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await responsePhoto.Content.ReadAsStringAsync());
                        return new BadRequestObjectResult(LastError);
                    }
                    var photo = JsonConvert.DeserializeObject<GanjoorPoetSuggestedPictureViewModel>(await responsePhoto.Content.ReadAsStringAsync());
                    photo.ChosenOne = true;
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/poetphotos", new StringContent(JsonConvert.SerializeObject(photo), Encoding.UTF8, "application/json"));
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }

            return new OkResult();
        }

        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/poetphotos/{id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }

            return new OkResult();
        }

        public PhotosModel(HttpClient httpClient) : base(httpClient)
        {

        }
    }
}
