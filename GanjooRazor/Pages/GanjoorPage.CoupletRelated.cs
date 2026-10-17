using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// a person tagged on a couplet
    /// </summary>
    public class CoupletRelatedPerson
    {
        public int Id { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// only alluded to, not a part of the story of the couplet
        /// </summary>
        public bool AllusionOnly { get; set; }
    }

    /// <summary>
    /// a place tagged on a couplet
    /// </summary>
    public class CoupletRelatedLocation
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? LunarYear { get; set; }

        /// <summary>
        /// coordinates for the script call, always with a decimal point whatever the culture of the server is
        /// </summary>
        public string Coordinates => Latitude.ToString(CultureInfo.InvariantCulture) + ", " + Longitude.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// places, people and literary devices of one couplet, shown in its panel
    /// </summary>
    public class _CoupletRelatedPartialModel
    {
        public List<CoupletRelatedLocation> Locations { get; set; } = new List<CoupletRelatedLocation>();
        public List<CoupletRelatedPerson> People { get; set; } = new List<CoupletRelatedPerson>();
        public List<LiteraryTagView> LiteraryTags { get; set; } = new List<LiteraryTagView>();
        public List<LiteraryTagSource> Sources { get; set; } = new List<LiteraryTagSource>();

        public bool Any => Locations.Count > 0 || People.Count > 0 || LiteraryTags.Count > 0;
    }

    public partial class GanjoorPageModel
    {
        /// <summary>
        /// places, people and literary devices of a couplet, requested by the couplet panel after it is shown.
        /// Returns nothing (not an error) if anything fails: the panel is complete without them.
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex">0 based, like the couplet panel's</param>
        public async Task<IActionResult> OnGetCoupletRelatedAsync(int poemId, int coupletIndex)
        {
            var model = new _CoupletRelatedPartialModel();
            try
            {
                // places and people: tags carry the same 0 based couplet index as the verses (-1 is a tag of the whole poem,
                // which belongs to no couplet panel)
                {
                    var geoResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/geotag");
                    if (geoResponse.IsSuccessStatusCode)
                    {
                        var geoTags = (JsonConvert.DeserializeObject<PoemGeoDateTag[]>(await geoResponse.Content.ReadAsStringAsync()) ?? new PoemGeoDateTag[0])
                            .Where(t => t.CoupletIndex == coupletIndex).ToList();
                        foreach (var group in geoTags.Where(t => t.Location != null).GroupBy(t => t.LocationId))
                        {
                            var first = group.First();
                            model.Locations.Add(new CoupletRelatedLocation()
                            {
                                Name = first.Location.Name,
                                Latitude = first.Location.Latitude,
                                Longitude = first.Location.Longitude,
                                LunarYear = group.Select(t => t.LunarYear).FirstOrDefault(y => y != null),
                            });
                        }
                        foreach (var group in geoTags.Where(t => t.Person != null).GroupBy(t => t.PersonId))
                        {
                            model.People.Add(new CoupletRelatedPerson()
                            {
                                Id = group.Key.Value,
                                Name = group.First().Person.Name,
                                AllusionOnly = group.All(t => t.PersonMention == PersonMentionKind.Allusion),
                            });
                        }
                    }
                }

                // literary devices: numbered from 1 over the couplets, which is the verses' index + 1
                var tagsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/literary/poem/{poemId}/tags");
                var devices = await GetLiteraryDevicesCachedAsync();
                if (tagsResponse.IsSuccessStatusCode && devices != null)
                {
                    var tags = (JsonConvert.DeserializeObject<PoemLiteraryTag[]>(await tagsResponse.Content.ReadAsStringAsync()) ?? new PoemLiteraryTag[0])
                        .Where(t => t.CoupletIndex == coupletIndex + 1).ToList();
                    if (tags.Count > 0)
                    {
                        var versesResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/verses?coupletIndex={coupletIndex}");
                        if (versesResponse.IsSuccessStatusCode)
                        {
                            var verses = JArray.Parse(await versesResponse.Content.ReadAsStringAsync()).ToObject<List<GanjoorVerseViewModel>>();
                            string hemistich1 = verses.Where(v => v.VersePosition == VersePosition.Right || v.VersePosition == VersePosition.CenteredVerse1
                                    || v.VersePosition == VersePosition.Single || v.VersePosition == VersePosition.Paragraph).Select(v => v.Text).FirstOrDefault();
                            string hemistich2 = verses.Where(v => v.VersePosition == VersePosition.Left || v.VersePosition == VersePosition.CenteredVerse2)
                                .Select(v => v.Text).FirstOrDefault();
                            var couplet = new LiteraryCoupletView() { Number = coupletIndex + 1 };
                            FillLiteraryWords(couplet, hemistich1, hemistich2);

                            var sourceByKey = (await GetLiterarySourcesCachedAsync()).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First());
                            var deviceById = devices.ToDictionary(d => d.Id);
                            foreach (var tag in tags)
                            {
                                if (deviceById.TryGetValue(tag.DeviceId, out var device))
                                    model.LiteraryTags.Add(BuildLiteraryTagView(tag, device, couplet, sourceByKey, model.Sources));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                model = new _CoupletRelatedPartialModel();
            }
            return Partial("~/Pages/Partials/GanjoorPage/_CoupletRelatedPartial.cshtml", model);
        }
    }
}
