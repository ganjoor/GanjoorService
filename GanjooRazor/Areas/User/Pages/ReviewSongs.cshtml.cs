using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    public class ReviewSongsModel : PageModel
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public PoemMusicTrackViewModel PoemMusicTrackViewModel { get; set; }

        /// <summary>
        /// poem model
        /// </summary>
        public GanjoorPoemCompleteViewModel Poem { get; set; }

        /// <summary>
        /// skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// total count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// get
        /// </summary>
        public async Task OnGetAsync()
        {
            LastError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var trackResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/song?skip={Skip}&onlyMine=false");
                    if (!trackResponse.IsSuccessStatusCode)
                    {
                        LastError = await trackResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        string paginnationMetadata =  trackResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                        if (!string.IsNullOrEmpty(paginnationMetadata))
                        {
                            TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata).totalCount;
                        }

                        PoemMusicTrackViewModel = JsonConvert.DeserializeObject<PoemMusicTrackViewModel>(await trackResponse.Content.ReadAsStringAsync());

                        PoemMusicTrackViewModel.ArtistName = PoemMusicTrackViewModel.ArtistName.ToPersianNumbers().ApplyCorrectYeKe();
                        PoemMusicTrackViewModel.AlbumName = PoemMusicTrackViewModel.AlbumName.ToPersianNumbers().ApplyCorrectYeKe();
                        PoemMusicTrackViewModel.TrackName = PoemMusicTrackViewModel.TrackName.ToPersianNumbers().ApplyCorrectYeKe();


                        var poemResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{PoemMusicTrackViewModel.PoemId}?catInfo=false&rhymes=false&recitations=false&images=false&songs=false&comments=false&verseDetails=false&navigation=false");
                        if (poemResponse.IsSuccessStatusCode)
                        {
                            Poem = JsonConvert.DeserializeObject<GanjoorPoemCompleteViewModel>(await poemResponse.Content.ReadAsStringAsync());

                           

                        }
                        else
                        {
                            LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                        }
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }
        }


        public async Task<IActionResult> OnPostAsync()
        {

            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);

            if (Request.Form["next"].Count == 1)
            {
                
                return Redirect($"/User/ReviewSongs/?skip={Skip + 1}");
            }

            PoemMusicTrackViewModel.Approved = Request.Form["approve"].Count == 1;
            PoemMusicTrackViewModel.Rejected = (Request.Form["reject1"].Count + Request.Form["reject2"].Count + Request.Form["reject3"].Count) > 0;
            if(string.IsNullOrEmpty(PoemMusicTrackViewModel.RejectionCause))
            {
                if (Request.Form["reject1"].Count == 1)
                {
                    PoemMusicTrackViewModel.RejectionCause = "در آهنگ این شعر خوانده نشده";
                }
                else
                if (Request.Form["reject2"].Count == 1)
                {
                    PoemMusicTrackViewModel.RejectionCause = "لینک یا اطلاعات آهنگ ایراد دارد";
                }
            }

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var putResponse = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/song", new StringContent(JsonConvert.SerializeObject(PoemMusicTrackViewModel), Encoding.UTF8, "application/json"));
                    if (!putResponse.IsSuccessStatusCode)
                    {
                        LastError = await putResponse.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }


            if(!string.IsNullOrEmpty(LastError))
            {
                return Page();
            }

            return Redirect($"/User/ReviewSongs/?skip={Skip}");

        }
    }
}
