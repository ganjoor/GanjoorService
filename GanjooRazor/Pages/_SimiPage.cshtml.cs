using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public partial class IndexModel : LoginPartialEnabledPageModel
    {
        public async Task<ActionResult> OnGetSimilarPoemsPartialAsync(int poemId, int skip, string prosodyMetre, string rhymeLetters, string poemFullUrl, int sectionIndex)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/section/{poemId}/{sectionIndex}/related?skip={skip}&itemsCount=21";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            var relatedSections = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCachedRelatedSection>>();

            return new PartialViewResult()
            {
                ViewName = "_SimiPartialView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SimiPartialViewModel()
                    {
                        RelatedSections = relatedSections.ToArray(),
                        Rhythm = prosodyMetre,
                        RhymeLetters = rhymeLetters,
                        Skip = skip,
                        PoemId = poemId,
                        PoemFullUrl = poemFullUrl,
                        SectionIndex = sectionIndex
                    }
                }
            };
        }

        public async Task<ActionResult> OnGetSimilarPoemsFromPoetPartialAsync(int poetId, string prosodyMetre, string rhymeLetters, string skipPoemFullUrl1, string skipPoemFullUrl2)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?metre={prosodyMetre}&rhyme={rhymeLetters}&poetId={poetId}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            List<GanjoorPoemCompleteViewModel> selectedPoems = new List<GanjoorPoemCompleteViewModel>();
            List<int> poetMorePoemsLikeThisCount = new List<int>();
            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
            if (poems.Any(p => p.FullUrl == skipPoemFullUrl1))
                poems.RemoveAll(p => p.FullUrl == skipPoemFullUrl1); //TODO: fix errors here
            if (poems.Any(p => p.FullUrl == skipPoemFullUrl2))
                poems.RemoveAll(p => p.FullUrl == skipPoemFullUrl2);

            foreach (var poem in poems)
            {
                poem.HtmlText = GanjoorPoemTools.GetPoemHtmlExcerpt(poem.HtmlText);
            }

            return new PartialViewResult()
            {
                ViewName = "_SimiPartialFromPoetView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SimiPartialFromPoetViewModel()
                    {
                        Poems = poems,
                    }
                }
            };
        }
    }
}
