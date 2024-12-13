using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using RMuseum.DbContext;
using System.Collections.Generic;
using RSecurityBackend.Services.Implementation;
using RMuseum.Utils;
using RMuseum.Models.Ganjoor;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        private async Task<string> PrepareTajikPoetHtmlTextAsync(RMuseumDbContext context, GanjoorTajikPoet poet)
        {

            string[] lines = poet.TajikDescription.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            string html = "";
            foreach (var line in lines)
            {
                html += $"<p>{line}</p>";
            }
            var poetCat = await context.GanjoorCategories.AsNoTracking().Where(c => c.PoetId == poet.Id && c.ParentId == null).SingleAsync();
            var subCats = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var subCat in subCats)
            {
                var tajikCat = await context.TajikCats.AsNoTracking().Where(t => t.Id == subCat.Id).SingleOrDefaultAsync();
                if(tajikCat != null)
                {
                    html += $"<p><a href=\"{subCat.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikCat.TajikTitle)}</a></p>";
                }
            }

            var catPoems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            var tajikPoems = await context.TajikPoems.AsNoTracking().Where(p => p.CatId == poetCat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var catPoem in catPoems)
            {
                var tajikPoem = tajikPoems.Where(p => p.Id == catPoem.Id).SingleOrDefault();
                if(tajikPoem != null)
                {
                    html += $"<p><a href=\"{catPoem.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikPoem.TajikTitle)}</a></p>";
                }
                
            }
            return html;
        }

        private async Task<string> PrepareTajikCatHtmlTextAsync(RMuseumDbContext context, GanjoorTajikCat cat)
        {
            if(cat.TajikDescription == null)
            {
                cat.TajikDescription = "";
            }
            string[] lines = cat.TajikDescription.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            string html = "";
            foreach (var line in lines)
            {
                html += $"<p>{line}</p>";
            }
            var subCats = await context.GanjoorCategories.AsNoTracking().Where(c => c.ParentId == cat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var subCat in subCats)
            {
                var tajikCat = await context.TajikCats.AsNoTracking().Where(t => t.Id == subCat.Id).SingleAsync();
                html += $"<p><a href=\"{subCat.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikCat.TajikTitle)}</a></p>";
            }

            var catPoems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == cat.Id).OrderBy(p => p.Id).ToListAsync();
            var tajikPoems = await context.TajikPoems.AsNoTracking().Where(p => p.CatId == cat.Id).OrderBy(p => p.Id).ToListAsync();
            foreach (var catPoem in catPoems)
            {
                var tajikPoem = tajikPoems.Where(p => p.Id == catPoem.Id).SingleOrDefault();
                if(tajikPoem != null)
                {
                    html += $"<p><a href=\"{catPoem.FullUrl}\">{LanguageUtils.CleanTextForTransileration(tajikPoem.TajikTitle)}</a></p>";
                }
            }
            return html;
        }
    }


}
