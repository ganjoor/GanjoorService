using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Utils.SemanticSearch
{
    public class PoetScopeEntry
    {
        public int PoetId { get; set; }
        public string Nickname { get; set; }
    }

    public class CategoryScopeEntry
    {
        public int CatId { get; set; }
        public int PoetId { get; set; }
        public string Title { get; set; }
    }

    public class DetectedScope
    {
        public int? PoetId { get; set; }
        public string PoetName { get; set; }
        public int? CatId { get; set; }
        public string CategoryName { get; set; }

        public bool HasAny => PoetId.HasValue || CatId.HasValue;
    }

    /// <summary>
    /// Detects when a free-text query names a specific poet and/or book/collection (e.g.
    /// "در کدام شعر حافظ" -> حافظ, "در کدام بخش شاهنامه" -> شاهنامه) so search can be scoped to
    /// just that poet/category instead of the whole corpus.
    ///
    /// Deliberately simple substring matching, not real NLP/NER — good enough for the common,
    /// unambiguous case (a poet's distinctive nickname, or a book title effectively unique to one
    /// poet, like شاهنامه), and safely conservative for the ambiguous case: a generic category
    /// title shared by many poets (غزلیات appears for most of them) is only used as a scope if a
    /// specific poet was ALSO named in the same query, narrowing which one is meant — otherwise
    /// it's dropped rather than guessing which poet's غزلیات the person meant.
    /// </summary>
    public class QueryScopeIndex
    {
        private readonly List<PoetScopeEntry> _poets;
        private readonly List<CategoryScopeEntry> _categories;

        private QueryScopeIndex(List<PoetScopeEntry> poets, List<CategoryScopeEntry> categories)
        {
            _poets = poets;
            _categories = categories;
        }

        public static async Task<QueryScopeIndex> LoadAsync(RMuseumDbContext context)
        {
            var poets = await context.GanjoorPoets.AsNoTracking()
                                .Where(p => p.Published)
                                .Select(p => new PoetScopeEntry { PoetId = p.Id, Nickname = p.Nickname })
                                .ToListAsync();

            // "book"/"collection" level = direct children of a poet's own root category
            // (شاهنامه, غزلیات, دیوان شمس, ...) - not deeper structural subsections, which would
            // add a lot of short, generic, easily-false-positive titles to match against.
            var rootCatIds = await context.GanjoorCategories.AsNoTracking()
                                    .Where(c => c.ParentId == null)
                                    .Select(c => c.Id)
                                    .ToListAsync();
            var rootCatIdSet = new HashSet<int>(rootCatIds);

            var categories = await context.GanjoorCategories.AsNoTracking()
                                    .Where(c => c.ParentId != null)
                                    .Select(c => new { c.Id, c.ParentId, c.PoetId, c.Title })
                                    .ToListAsync();

            var scopedCategories = categories
                .Where(c => rootCatIdSet.Contains(c.ParentId.Value))
                .Select(c => new CategoryScopeEntry { CatId = c.Id, PoetId = c.PoetId, Title = c.Title })
                .ToList();

            return new QueryScopeIndex(poets, scopedCategories);
        }

        public DetectedScope DetectScope(string query)
        {
            var result = new DetectedScope();
            if (string.IsNullOrWhiteSpace(query))
                return result;

            PoetScopeEntry bestPoet = null;
            foreach (var poet in _poets)
            {
                if (string.IsNullOrEmpty(poet.Nickname))
                    continue;
                if (query.Contains(poet.Nickname) && (bestPoet == null || poet.Nickname.Length > bestPoet.Nickname.Length))
                {
                    bestPoet = poet;
                }
            }

            string bestCategoryTitle = null;
            foreach (var cat in _categories)
            {
                if (string.IsNullOrEmpty(cat.Title))
                    continue;
                if (query.Contains(cat.Title) && (bestCategoryTitle == null || cat.Title.Length > bestCategoryTitle.Length))
                {
                    bestCategoryTitle = cat.Title;
                }
            }

            CategoryScopeEntry bestCategory = null;
            if (bestCategoryTitle != null)
            {
                var matches = _categories.Where(c => c.Title == bestCategoryTitle).ToList();
                if (matches.Count == 1)
                {
                    bestCategory = matches[0]; // unambiguous - only one category anywhere has this exact title
                }
                else if (bestPoet != null)
                {
                    // ambiguous title (shared by multiple poets), but a specific poet was also
                    // named - use that poet's version of it, if they have one
                    bestCategory = matches.FirstOrDefault(c => c.PoetId == bestPoet.PoetId);
                }
                // else: ambiguous with no poet to disambiguate against - deliberately left null
                // rather than guessing which poet's version was meant
            }

            result.PoetId = bestPoet?.PoetId;
            result.PoetName = bestPoet?.Nickname;
            result.CatId = bestCategory?.CatId;
            result.CategoryName = bestCategory?.Title;
            return result;
        }
    }
}
