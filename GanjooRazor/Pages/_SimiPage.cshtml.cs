using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public partial class IndexModel : LoginPartialEnabledPageModel
    {
        public async Task<IActionResult> OnGetSimilarPoemsPartialAsync(int poemId, int skip, string prosodyMetre, string rhymeLetters, string poemFullUrl, int sectionIndex)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/section/{poemId}/{sectionIndex}/related?skip={skip}&itemsCount=21";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(await ReadErrorMessageAsync(response));
            var relatedSections = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCachedRelatedSection>>();

            return Partial("_SimiPartialView", new _SimiPartialViewModel()
            {
                RelatedSections = relatedSections.ToArray(),
                Rhythm = prosodyMetre,
                RhymeLetters = rhymeLetters,
                Skip = skip,
                PoemId = poemId,
                PoemFullUrl = poemFullUrl,
                SectionIndex = sectionIndex
            });
        }

        public async Task<IActionResult> OnGetSimilarPoemsFromPoetPartialAsync(int poetId, string prosodyMetre, string rhymeLetters, string skipPoemFullUrl1, string skipPoemFullUrl2)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?metre={prosodyMetre}&rhyme={rhymeLetters}&poetId={poetId}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(await ReadErrorMessageAsync(response));

            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
            if (poems.Any(p => p.FullUrl == skipPoemFullUrl1))
                poems.RemoveAll(p => p.FullUrl == skipPoemFullUrl1); //TODO: fix errors here
            if (poems.Any(p => p.FullUrl == skipPoemFullUrl2))
                poems.RemoveAll(p => p.FullUrl == skipPoemFullUrl2);

            foreach (var poem in poems)
            {
                poem.HtmlText = GanjoorPoemTools.GetPoemHtmlExcerpt(poem.HtmlText);
            }

            return Partial("_SimiPartialFromPoetView", new _SimiPartialFromPoetViewModel()
            {
                Poems = poems,
            });
        }
    }
}
