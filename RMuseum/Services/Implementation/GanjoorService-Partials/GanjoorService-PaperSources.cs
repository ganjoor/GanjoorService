using Microsoft.EntityFrameworkCore;
using RSecurityBackend.Models.Generic;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using RMuseum.Models.GanjoorIntegration;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// category paper sources
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPaperSource[]>> GetCategoryPaperSourcesAsync(int categoryId)
        {
            try
            {
                return new RServiceResult<GanjoorPaperSource[]>
                    (
                    await _context.GanjoorPaperSources.AsNoTracking().Where(p => p.GanjoorCatId == categoryId).OrderByDescending(c => c.IsTextOriginalSource).OrderBy(c => c.OrderIndicator).ThenBy(c => c.Id).ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPaperSource[]>(null, exp.ToString());
            }
        }
    }
}
