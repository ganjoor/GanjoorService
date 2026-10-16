using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// a comparison key for finding duplicate concepts: unifies Arabic/Persian letter forms, removes
        /// diacritics, tatweel, ZWNJ, spaces and hyphens and strips simple plural endings (ها، های، ان)
        /// </summary>
        private static string _ConceptDuplicateKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";
            var sb = new StringBuilder();
            foreach (char ch in name)
            {
                if (ch == '‌' || ch == '‍' || ch == 'ـ' || ch == '-' || char.IsWhiteSpace(ch))
                    continue;
                if ((ch >= 'ً' && ch <= 'ْ') || ch == 'ٰ' || ch == 'ٔ')
                    continue;
                switch (ch)
                {
                    case 'ي': case 'ى': sb.Append('ی'); break;
                    case 'ك': sb.Append('ک'); break;
                    case 'ة': case 'ۀ': sb.Append('ه'); break;
                    case 'أ': case 'إ': case 'آ': sb.Append('ا'); break;
                    default: sb.Append(ch); break;
                }
            }
            string key = sb.ToString();
            if (key.Length >= 5 && key.EndsWith("های"))
                key = key.Substring(0, key.Length - 3);
            else if (key.Length >= 5 && (key.EndsWith("ها") || key.EndsWith("ان")))
                key = key.Substring(0, key.Length - 2);
            return key;
        }

        /// <summary>
        /// a page of approved concepts for management: filtered by name/alias, with usage counts
        /// </summary>
        /// <param name="query">name filter (empty: all)</param>
        /// <param name="sort">name (default), usage-desc or usage-asc</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        public async Task<RServiceResult<LiteraryConceptListPage>> GetLiteraryConceptsPageAsync(string query, string sort, int skip, int take)
        {
            try
            {
                if (take <= 0 || take > 200)
                    take = 50;
                if (skip < 0)
                    skip = 0;
                string q = _NormalizeConceptName(query);

                var concepts = _context.LiteraryConcepts.AsNoTracking().Where(c => c.Status == LiteraryEntityStatus.Approved);
                if (q != "")
                    concepts = concepts.Where(c => c.Name.Contains(q) || (c.Aliases != null && c.Aliases.Contains(q)));

                int total = await concepts.CountAsync();

                var withUsage = concepts.Select(c => new
                {
                    Concept = c,
                    Usage = _context.PoemLiteraryTagParts.Count(p => p.ConceptId == c.Id)
                });
                switch (sort)
                {
                    case "usage-desc":
                        withUsage = withUsage.OrderByDescending(x => x.Usage).ThenBy(x => x.Concept.Name);
                        break;
                    case "usage-asc":
                        withUsage = withUsage.OrderBy(x => x.Usage).ThenBy(x => x.Concept.Name);
                        break;
                    default:
                        withUsage = withUsage.OrderBy(x => x.Concept.Name);
                        break;
                }
                var page = await withUsage.Skip(skip).Take(take).ToListAsync();

                var items = await _ToConceptListItemsAsync(page.Select(x => (x.Concept, x.Usage)).ToList());
                return new RServiceResult<LiteraryConceptListPage>(new LiteraryConceptListPage { Total = total, Items = items });
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryConceptListPage>(null, exp.ToString());
            }
        }

        private async Task<LiteraryConceptListItem[]> _ToConceptListItemsAsync(List<(LiteraryConcept Concept, int Usage)> list)
        {
            var parentIds = list.Where(x => x.Concept.ParentId != null).Select(x => x.Concept.ParentId.Value).Distinct().ToArray();
            var parentNames = parentIds.Length == 0
                ? new Dictionary<int, string>()
                : await _context.LiteraryConcepts.AsNoTracking().Where(c => parentIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);

            var ids = list.Select(x => x.Concept.Id).ToArray();
            var childCounts = ids.Length == 0
                ? new Dictionary<int, int>()
                : await _context.LiteraryConcepts.AsNoTracking()
                    .Where(c => c.ParentId != null && ids.Contains(c.ParentId.Value))
                    .GroupBy(c => c.ParentId.Value)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Id, x => x.Count);

            return list.Select(x => new LiteraryConceptListItem
            {
                Id = x.Concept.Id,
                Name = x.Concept.Name,
                Aliases = x.Concept.Aliases,
                ParentId = x.Concept.ParentId,
                ParentName = x.Concept.ParentId != null && parentNames.ContainsKey(x.Concept.ParentId.Value) ? parentNames[x.Concept.ParentId.Value] : null,
                UsageCount = x.Usage,
                ChildCount = childCounts.ContainsKey(x.Concept.Id) ? childCounts[x.Concept.Id] : 0
            }).ToArray();
        }

        /// <summary>
        /// groups of approved concepts which are probably the same (see _ConceptDuplicateKey), the most used groups first
        /// </summary>
        /// <param name="maxGroups"></param>
        public async Task<RServiceResult<LiteraryConceptDuplicateGroup[]>> GetLiteraryConceptDuplicateGroupsAsync(int maxGroups)
        {
            try
            {
                if (maxGroups <= 0 || maxGroups > 500)
                    maxGroups = 100;

                var all = await _context.LiteraryConcepts.AsNoTracking()
                    .Where(c => c.Status == LiteraryEntityStatus.Approved)
                    .ToListAsync();

                var groups = all
                    .Select(c => new { Concept = c, Key = _ConceptDuplicateKey(c.Name) })
                    .Where(x => x.Key != "")
                    .GroupBy(x => x.Key)
                    .Where(g => g.Count() > 1)
                    .ToList();

                var usage = await _context.PoemLiteraryTagParts.AsNoTracking()
                    .Where(p => p.ConceptId != null)
                    .GroupBy(p => p.ConceptId.Value)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Id, x => x.Count);

                var ordered = groups
                    .Select(g => new
                    {
                        g.Key,
                        Members = g.Select(x => (x.Concept, Usage: usage.ContainsKey(x.Concept.Id) ? usage[x.Concept.Id] : 0))
                                   .OrderByDescending(x => x.Usage).ThenBy(x => x.Concept.Name).ToList()
                    })
                    .OrderByDescending(g => g.Members.Sum(m => m.Usage)).ThenBy(g => g.Key)
                    .Take(maxGroups)
                    .ToList();

                var result = new List<LiteraryConceptDuplicateGroup>();
                foreach (var g in ordered)
                {
                    result.Add(new LiteraryConceptDuplicateGroup
                    {
                        Key = g.Key,
                        Items = await _ToConceptListItemsAsync(g.Members)
                    });
                }
                return new RServiceResult<LiteraryConceptDuplicateGroup[]>(result.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryConceptDuplicateGroup[]>(null, exp.ToString());
            }
        }
    }
}
