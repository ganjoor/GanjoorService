using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class TranslateModel : PageModel
    {
        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        /// <summary>
        /// translations
        /// </summary>
        public GanjoorLanguage[] Languages { get; set; }

        /// <summary>
        /// translations
        /// </summary>
        public GanjoorPoemTranslationViewModel[] Translations { get; set; }

        /// <summary>
        /// translation
        /// </summary>
        public GanjoorPoemTranslationViewModel Translation { get; set; }

        /// <summary>
        /// user info
        /// </summary>
        public PublicRAppUser UserInfo { get; set; }

        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        public string GetVersePosition(GanjoorVerseViewModel verse)
        {
            switch (verse.VersePosition)
            {
                case VersePosition.Right:
                    return "مصرع اول";
                case VersePosition.Left:
                    return "مصرع دوم";
                case VersePosition.CenteredVerse1:
                    return "مصرع اول بند";
                case VersePosition.CenteredVerse2:
                    return "مصرع دوم بند";
                case VersePosition.Paragraph:
                    return "پاراگراف نثر";
                case VersePosition.Single:
                    return "نیمایی یا آزاد";
                case VersePosition.Comment:
                    return "توضیح";
            }
            return "نامعتبر";
        }

        public int GetVerseCoupletNumber(GanjoorVerseViewModel verse)
        {
            int n = 1;
            VersePosition pre = VersePosition.Right;
            foreach (var v in PageInformation.Poem.Verses)
            {
                if (v.Id == verse.Id)
                {
                    if (pre == VersePosition.CenteredVerse1 && v.VersePosition != VersePosition.CenteredVerse2)
                        n++;
                    return n;
                }
                if (v.VersePosition == VersePosition.Left || v.VersePosition == VersePosition.CenteredVerse2
                    || v.VersePosition == VersePosition.Single || v.VersePosition == VersePosition.Paragraph)
                    n++;
                else
                if (pre == VersePosition.CenteredVerse1)
                    n++;
                pre = v.VersePosition;
            }
            return -1;
        }

        

        /// <summary>
        /// get
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {

                    await GanjoorSessionChecker.ApplyPermissionsToViewData(Request, Response, ViewData);
                    if (!ViewData.ContainsKey($"{RMuseumSecurableItem.GanjoorEntityShortName}-{RMuseumSecurableItem.Translations}"))
                    {
                        FatalError = "حساب کاربری شما به این بخش دسترسی ندارد.";
                        return Page();
                    }

                    int poemId = int.Parse(Request.Query["id"]);

                    int transId = string.IsNullOrEmpty(Request.Query["t"]) ? -1 : int.Parse(Request.Query["t"]);

                    int langId = string.IsNullOrEmpty(Request.Query["lang"]) ? -1 : int.Parse(Request.Query["lang"]);

                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/translations/poem/{poemId}/language/{langId}");
                    if (!response.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }

                    var translations = JsonConvert.DeserializeObject<GanjoorPoemTranslationViewModel[]>(await response.Content.ReadAsStringAsync());

                    var userInfoResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/users/{Request.Cookies["UserId"]}");
                    if (!response.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }
                    UserInfo = JsonConvert.DeserializeObject<PublicRAppUser>(await userInfoResponse.Content.ReadAsStringAsync());

                    if(transId != -1)
                    {
                        Translation = translations.Where(t => t.Id == transId).Single();
                        langId = Translation.Language.Id;
                    }

                    if (langId == -1)
                    {
                        var lastLangResp = await secureClient.GetAsync($"{APIRoot.Url}/api/translations/user/lastlanguage");
                        if (response.StatusCode != HttpStatusCode.NotFound)
                        {
                            var lang = JsonConvert.DeserializeObject<GanjoorLanguage>(await lastLangResp.Content.ReadAsStringAsync());
                            if(lang != null)
                            {
                                langId = lang.Id;
                            }
                        }
                    }

                    if (langId != -1 && Translation == null)
                    {
                        Translations = translations;
                        foreach (var translation in translations)
                            if (translation.ContributerId == UserInfo.Id && langId == translation.Language.Id)
                            {
                                Translation = translation;
                                break;
                            }
                    }
                    else if (translations.Length > 0 && Translation == null)
                    {
                        foreach (var translation in translations)
                            if (translation.ContributerId == UserInfo.Id)
                            {
                                Translation = translation;
                                langId = Translation.Language.Id;
                                break;
                            }
                        if (langId == -1)
                        {
                            Translation = translations[0];
                            langId = Translation.Language.Id;
                        }

                    }

                   

                    HttpResponseMessage responseLanguages = await secureClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
                    if (!responseLanguages.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await responseLanguages.Content.ReadAsStringAsync());
                        return Page();
                    }

                    
                    Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await responseLanguages.Content.ReadAsStringAsync());

                    if(Languages.Length == 0)
                    {
                        FatalError = "<a role=\"button\" target=\"_blank\" href=\"/User/Languages\" class=\"actionlink\">معرفی زبان‌ها و نویسش‌ها</a>";
                        return Page();
                    }

                    if (langId == -1)
                    {
                        langId = Languages[0].Id;
                    }


                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={poemId}");
                    if (!pageUrlResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                        return Page();
                    }
                    var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                    var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                    if (!pageQuery.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                        return Page();
                    }
                    PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                    if(Translation == null)
                    {
                       
                        Translation = new GanjoorPoemTranslationViewModel()
                        {
                            Language = Languages.Where(l => l.Id == langId).Single(),
                            PoemId = PageInformation.Id,
                            Title = "",
                            Published = false,
                            Description = "",
                            ContributerName = UserInfo.NickName == null ? UserInfo.Id.ToString() : UserInfo.NickName,
                            TranslatedVerses = PageInformation.Poem.Verses.Select(v =>
                            new GanjoorVerseTranslationViewModel()
                            {
                                Verse = v,
                                TText = ""
                            }
                        ).ToArray()
                        };

                        Translations = new GanjoorPoemTranslationViewModel[] { Translation };
                    }

                    if(langId != -1)
                    {
                        Translations = translations.Where(t => t.Language.Id == langId).ToArray();
                    }

                    
                }
                else
                {
                    FatalError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }


        public async Task<IActionResult> OnPostSendPoemTranslationAsync(int poemid, int langid, string[] translations, bool published, string note)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    string title = null;
                    List<GanjoorVerseTranslationViewModel> verses = new List<GanjoorVerseTranslationViewModel>();
                    foreach (string v in translations)
                    {
                        var vParts = v.Split("TextSeparator", System.StringSplitOptions.RemoveEmptyEntries);
                        int vOrder = int.Parse(vParts[0]);
                        if (vOrder == 0)
                        {
                            if(vParts.Length > 1)
                                title = vParts[1];
                        }
                        else
                        {
                            verses.Add
                                (
                                new GanjoorVerseTranslationViewModel()
                                {
                                    Verse = new GanjoorVerseViewModel() { VOrder = vOrder },
                                    TText = vParts.Length > 1 ? vParts[1] : null
                                }
                                ); ;
                        }
                    }

                    if (string.IsNullOrEmpty(title) && verses.Where(v => !string.IsNullOrEmpty(v.TText) ).FirstOrDefault() == null)
                        return new BadRequestObjectResult("شما هیچ متنی را وارد نکرده‌اید!");

                    var translation = new GanjoorPoemTranslationViewModel()
                    {
                        Language = new GanjoorLanguage() { Id = langid },
                        PoemId = poemid,
                        Title = title,
                        Published = published,
                        Description = note,
                        TranslatedVerses = verses.ToArray()
                    };

                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/translations",
                        new StringContent(JsonConvert.SerializeObject(translation),
                        Encoding.UTF8,
                        "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }
                    Translation = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPoemTranslationViewModel>();
                    return new OkObjectResult(Translation.Id);
                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }
    }
}
