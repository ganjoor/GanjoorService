using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services.Implementation.ImportedFromDesktopGanjoor;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data;
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
        private async Task<RServiceResult<bool>> _FindCateryPoemsDuplicates(RMuseumDbContext context, int srcCatId, int destCatId)
        {
            try
            {
                var poems = await context.GanjoorPoems.AsNoTracking().Where(p => p.CatId == srcCatId).ToListAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private static bool _AreSimilar(string str1, string str2, bool reverse)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return false;

            string[] words2 = str2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int total = words2.Length;
            int found = 0;
            for (int i = 0; i < total; i++)
            {
                if (str1.IndexOf(words2[i]) != -1)
                    found++;
            }

            if (!reverse)
                return (float)found / total > 0.7f;

            return (float)found / total > 0.7f && _AreSimilar(str2, str1, false);
        }
    }
}