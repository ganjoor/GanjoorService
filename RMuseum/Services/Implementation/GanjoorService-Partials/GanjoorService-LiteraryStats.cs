using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// tags of a poet and/or of a category with all its subcategories (0 = no restriction)
        /// </summary>
        private async Task<IQueryable<PoemLiteraryTag>> _LiteraryTagsOfAsync(int poetId, int catId)
        {
            var tags = _context.PoemLiteraryTags.AsNoTracking().AsQueryable();
            if (poetId > 0)
                tags = tags.Where(t => t.Poem.Cat.PoetId == poetId);
            if (catId > 0)
            {
                string catUrl = await _context.GanjoorCategories.AsNoTracking().Where(c => c.Id == catId).Select(c => c.FullUrl).FirstOrDefaultAsync();
                if (string.IsNullOrEmpty(catUrl))
                    return tags.Where(t => false);
                string prefix = catUrl + "/";
                tags = tags.Where(t => t.Poem.Cat.FullUrl == catUrl || t.Poem.Cat.FullUrl.StartsWith(prefix));
            }
            return tags;
        }

        private async Task<LiteraryStatsConcept[]> _TopLiteraryConceptsAsync(IQueryable<PoemLiteraryTag> tags, int take)
        {
            var top = await _context.PoemLiteraryTagParts.AsNoTracking()
                .Where(p => p.ConceptId != null)
                .Join(tags, p => p.TagId, t => t.Id, (p, t) => p.ConceptId.Value)
                .GroupBy(id => id)
                .Select(g => new { ConceptId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(take)
                .ToListAsync();
            var ids = top.Select(x => x.ConceptId).ToArray();
            var names = await _context.LiteraryConcepts.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);
            return top.Select(x => new LiteraryStatsConcept
            {
                ConceptId = x.ConceptId,
                Name = names.ContainsKey(x.ConceptId) ? names[x.ConceptId] : "",
                UsageCount = x.Count
            }).ToArray();
        }

        /// <summary>
        /// summary statistics of literary tags (optionally only for one poet)
        /// </summary>
        /// <param name="poetId">0 = all poets</param>
        /// <param name="catId">0 = no category restriction (a category includes its subcategories)</param>
        /// <param name="includePoets">whether the list of poets having tags is needed</param>
        public async Task<RServiceResult<LiteraryStatsOverview>> GetLiteraryStatsOverviewAsync(int poetId, int catId = 0, bool includePoets = true)
        {
            try
            {
                var tags = await _LiteraryTagsOfAsync(poetId, catId);
                var overview = new LiteraryStatsOverview
                {
                    TotalTags = await tags.CountAsync(),
                    TotalPoems = await tags.Select(t => t.PoemId).Distinct().CountAsync()
                };
                overview.Devices = (await tags.GroupBy(t => t.DeviceId)
                    .Select(g => new LiteraryStatsDevice
                    {
                        DeviceId = g.Key,
                        TagCount = g.Count(),
                        PoemCount = g.Select(x => x.PoemId).Distinct().Count()
                    })
                    .ToListAsync()).OrderByDescending(d => d.TagCount).ToArray();
                overview.TopConcepts = await _TopLiteraryConceptsAsync(tags, 40);
                overview.TotalConceptUses = await _context.PoemLiteraryTagParts.AsNoTracking()
                    .Where(p => p.ConceptId != null)
                    .Join(tags, p => p.TagId, t => t.Id, (p, t) => p.ConceptId.Value)
                    .CountAsync();

                if (!includePoets || overview.TotalTags == 0)
                {
                    overview.Poets = Array.Empty<LiteraryStatsPoet>();
                    return new RServiceResult<LiteraryStatsOverview>(overview);
                }

                var poetCounts = await _context.PoemLiteraryTags.AsNoTracking()
                    .GroupBy(t => t.Poem.Cat.PoetId)
                    .Select(g => new { PoetId = g.Key, Count = g.Count() })
                    .ToListAsync();
                var poetIds = poetCounts.Select(x => x.PoetId).ToArray();
                var poetNames = await _context.GanjoorPoets.AsNoTracking().Where(p => poetIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
                overview.Poets = poetCounts
                    .Select(x => new LiteraryStatsPoet { PoetId = x.PoetId, Name = poetNames.ContainsKey(x.PoetId) ? poetNames[x.PoetId] : "", TagCount = x.Count })
                    .OrderByDescending(x => x.TagCount).ToArray();
                return new RServiceResult<LiteraryStatsOverview>(overview);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryStatsOverview>(null, exp.ToString());
            }
        }

        /// <summary>
        /// the most used concepts of one device (optionally for one poet)
        /// </summary>
        public async Task<RServiceResult<LiteraryStatsConcept[]>> GetLiteraryDeviceTopConceptsAsync(int deviceId, int poetId, int take, int catId = 0)
        {
            try
            {
                if (take <= 0 || take > 100)
                    take = 30;
                var tags = (await _LiteraryTagsOfAsync(poetId, catId)).Where(t => t.DeviceId == deviceId);
                return new RServiceResult<LiteraryStatsConcept[]>(await _TopLiteraryConceptsAsync(tags, take));
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryStatsConcept[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// ids of a concept and all its descendants
        /// </summary>
        private async Task<int[]> _LiteraryConceptWithDescendantsAsync(int conceptId)
        {
            var links = await _context.LiteraryConcepts.AsNoTracking()
                .Where(c => c.ParentId != null)
                .Select(c => new { c.Id, ParentId = c.ParentId.Value })
                .ToListAsync();
            var childrenOf = links.ToLookup(x => x.ParentId, x => x.Id);
            var result = new HashSet<int> { conceptId };
            var queue = new Queue<int>();
            queue.Enqueue(conceptId);
            while (queue.Count > 0 && result.Count < 5000)
            {
                foreach (var child in childrenOf[queue.Dequeue()])
                    if (result.Add(child))
                        queue.Enqueue(child);
            }
            return result.ToArray();
        }

        /// <summary>
        /// tags of a device (or of a concept and its descendants), with the couplets, a page at a time
        /// </summary>
        /// <param name="deviceId">0 = any device</param>
        /// <param name="conceptId">0 = any concept</param>
        /// <param name="poetId">0 = all poets</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="catId">0 = no category restriction</param>
        public async Task<RServiceResult<LiteraryStatsTagPage>> GetLiteraryStatsTagsAsync(int deviceId, int conceptId, int poetId, int skip, int take, int catId = 0)
        {
            try
            {
                if (deviceId <= 0 && conceptId <= 0)
                    return new RServiceResult<LiteraryStatsTagPage>(null, "آرایه یا مفهوم مشخص نشده است.");
                if (take <= 0 || take > 50)
                    take = 20;
                if (skip < 0)
                    skip = 0;

                var tags = await _LiteraryTagsOfAsync(poetId, catId);
                if (deviceId > 0)
                    tags = tags.Where(t => t.DeviceId == deviceId);
                if (conceptId > 0)
                {
                    var conceptIds = await _LiteraryConceptWithDescendantsAsync(conceptId);
                    tags = tags.Where(t => _context.PoemLiteraryTagParts.Any(p => p.TagId == t.Id && p.ConceptId != null && conceptIds.Contains(p.ConceptId.Value)));
                }

                int total = await tags.CountAsync();
                var page = await tags
                    .OrderBy(t => t.PoemId).ThenBy(t => t.CoupletIndex).ThenBy(t => t.Id)
                    .Skip(skip).Take(take)
                    .Select(t => new
                    {
                        t.Id,
                        t.DeviceId,
                        t.PoemId,
                        t.CoupletIndex,
                        t.Uncertain,
                        t.Note,
                        PoemFullTitle = t.Poem.FullTitle,
                        PoemFullUrl = t.Poem.FullUrl,
                        PoetId = t.Poem.Cat.PoetId,
                        PoetName = t.Poem.Cat.Poet.Name
                    })
                    .ToListAsync();

                var tagIds = page.Select(t => t.Id).ToArray();
                var parts = await _context.PoemLiteraryTagParts.AsNoTracking()
                    .Where(p => tagIds.Contains(p.TagId))
                    .OrderBy(p => p.Order)
                    .ToListAsync();
                var slotIds = parts.Select(p => p.SlotId).Distinct().ToArray();
                var slotNames = await _context.LiteraryDeviceSlots.AsNoTracking().Where(s => slotIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name);
                var partConceptIds = parts.Where(p => p.ConceptId != null).Select(p => p.ConceptId.Value).Distinct().ToArray();
                var conceptNames = await _context.LiteraryConcepts.AsNoTracking().Where(c => partConceptIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);

                // couplets of the listed poems, numbered like the editor does
                var poemIds = page.Select(t => t.PoemId).Distinct().ToArray();
                var verses = await _context.GanjoorVerses.AsNoTracking()
                    .Where(v => poemIds.Contains(v.PoemId))
                    .OrderBy(v => v.PoemId).ThenBy(v => v.VOrder)
                    .Select(v => new { v.PoemId, v.VersePosition, v.Text })
                    .ToListAsync();
                var couplets = new Dictionary<int, Dictionary<int, Tuple<string, string>>>();
                foreach (var poemVerses in verses.GroupBy(v => v.PoemId))
                {
                    var map = new Dictionary<int, Tuple<string, string>>();
                    int n = -1;
                    string m1 = null;
                    foreach (var v in poemVerses)
                    {
                        switch (v.VersePosition)
                        {
                            case VersePosition.Paragraph:
                            case VersePosition.Single:
                                n++;
                                map[n] = new Tuple<string, string>(v.Text, null);
                                m1 = null;
                                break;
                            case VersePosition.Right:
                            case VersePosition.CenteredVerse1:
                                n++;
                                m1 = v.Text;
                                map[n] = new Tuple<string, string>(m1, null);
                                break;
                            case VersePosition.Left:
                            case VersePosition.CenteredVerse2:
                                if (m1 != null)
                                    map[n] = new Tuple<string, string>(m1, v.Text);
                                break;
                        }
                    }
                    couplets[poemVerses.Key] = map;
                }

                var items = new List<LiteraryStatsTagItem>();
                foreach (var t in page)
                {
                    Tuple<string, string> hemistichs = null;
                    if (t.CoupletIndex >= 0 && couplets.TryGetValue(t.PoemId, out var map))
                        map.TryGetValue(t.CoupletIndex, out hemistichs);

                    var itemParts = new List<LiteraryStatsTagPart>();
                    foreach (var p in parts.Where(x => x.TagId == t.Id))
                    {
                        string words = p.Text;
                        if (hemistichs != null && p.WordStart != null && p.WordEnd != null && (p.Misra == 1 || p.Misra == 2))
                        {
                            string hemistich = p.Misra == 1 ? hemistichs.Item1 : hemistichs.Item2;
                            var list = (hemistich ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                            var picked = list.Where((w, i) => i + 1 >= p.WordStart && i + 1 <= p.WordEnd).ToList();
                            if (picked.Count > 0)
                                words = string.Join(" ", picked);
                        }
                        itemParts.Add(new LiteraryStatsTagPart
                        {
                            SlotName = slotNames.ContainsKey(p.SlotId) ? slotNames[p.SlotId] : "",
                            Words = words,
                            ConceptId = p.ConceptId,
                            ConceptName = p.ConceptId != null && conceptNames.ContainsKey(p.ConceptId.Value) ? conceptNames[p.ConceptId.Value] : null,
                            Misra = p.Misra,
                            WordStart = p.WordStart,
                            WordEnd = p.WordEnd
                        });
                    }

                    items.Add(new LiteraryStatsTagItem
                    {
                        TagId = t.Id,
                        DeviceId = t.DeviceId,
                        PoemId = t.PoemId,
                        PoemFullTitle = t.PoemFullTitle,
                        PoemFullUrl = t.PoemFullUrl,
                        PoetId = t.PoetId,
                        PoetName = t.PoetName,
                        CoupletIndex = t.CoupletIndex,
                        Hemistich1 = hemistichs?.Item1,
                        Hemistich2 = hemistichs?.Item2,
                        Uncertain = t.Uncertain,
                        Note = t.Note,
                        Parts = itemParts.ToArray()
                    });
                }
                return new RServiceResult<LiteraryStatsTagPage>(new LiteraryStatsTagPage { Total = total, Items = items.ToArray() });
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryStatsTagPage>(null, exp.ToString());
            }
        }
    }
}
