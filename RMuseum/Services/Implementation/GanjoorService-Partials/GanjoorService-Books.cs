using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// Get list of books (GanjoorCat entries whose CatType is Book), sorted alphabetically by name.
        /// Not to be confused with GetBooksAsync() elsewhere in this class, which lists GanjoorCat
        /// entries by their (separate, legacy) BookName field for cover-image generation.
        /// </summary>
        /// <param name="name">optional, only books whose name contains this (case-insensitive)</param>
        /// <param name="poetId">optional, only books belonging to this poet</param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorBookViewModel[]>> GetBookCatalogAsync(string name = null, int? poetId = null)
        {
            var query = _context.GanjoorCategories.AsNoTracking()
                .Where(c => c.CatType == GanjoorCatType.Book);

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(c => c.Title.Contains(name));
            }

            if (poetId != null)
            {
                query = query.Where(c => c.PoetId == poetId.Value);
            }

            List<GanjoorBookViewModel> books =
                await query
                .Select(c => new GanjoorBookViewModel()
                {
                    Id = c.Id,
                    Name = c.Title,
                    FullUrl = c.FullUrl,
                    PoetId = c.PoetId,
                    PoetName = c.Poet.Nickname,
                })
                .ToListAsync();

            // the fa-IR comparer can't be translated to SQL, so sorting happens here in memory -
            // the same approach GetPoets() above uses, fine for a list this small (< 200 rows)
            StringComparer fa = StringComparer.Create(new CultureInfo("fa-IR"), true);
            books.Sort((a, b) => fa.Compare(a.Name, b.Name));

            return new RServiceResult<GanjoorBookViewModel[]>(books.ToArray());
        }
    }
}
