using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
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
        /// sources of imported literary tags
        /// </summary>
        public async Task<RServiceResult<LiteraryTagSource[]>> GetLiteraryTagSourcesAsync()
        {
            try
            {
                return new RServiceResult<LiteraryTagSource[]>(await _context.LiteraryTagSources.AsNoTracking().OrderBy(s => s.Name).ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryTagSource[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// adds (Id = 0) or updates a source
        /// </summary>
        public async Task<RServiceResult<LiteraryTagSource>> SaveLiteraryTagSourceAsync(LiteraryTagSource source)
        {
            try
            {
                string key = (source.Key ?? "").Trim();
                string name = (source.Name ?? "").Trim();
                string title = (source.Title ?? "").Trim();
                if (key == "" || name == "" || title == "")
                    return new RServiceResult<LiteraryTagSource>(null, "شناسه، نام کوتاه و عنوان الزامی‌اند.");
                if (await _context.LiteraryTagSources.AnyAsync(s => s.Key == key && s.Id != source.Id))
                    return new RServiceResult<LiteraryTagSource>(null, "منبعی با این شناسه وجود دارد.");

                LiteraryTagSource db;
                if (source.Id == 0)
                {
                    db = new LiteraryTagSource();
                    _context.LiteraryTagSources.Add(db);
                }
                else
                {
                    db = await _context.LiteraryTagSources.Where(s => s.Id == source.Id).SingleOrDefaultAsync();
                    if (db == null)
                        return new RServiceResult<LiteraryTagSource>(null, "منبع یافت نشد.");
                }
                db.Key = key;
                db.Name = name;
                db.Title = title;
                db.Authors = (source.Authors ?? "").Trim();
                db.Url = (source.Url ?? "").Trim();
                db.Description = (source.Description ?? "").Trim();
                db.License = (source.License ?? "").Trim();
                db.LicenseText = (source.LicenseText ?? "").Trim();
                db.UpdateDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return new RServiceResult<LiteraryTagSource>(db);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryTagSource>(null, exp.ToString());
            }
        }

        /// <summary>
        /// deletes a source (the tags that refer to it stay)
        /// </summary>
        public async Task<RServiceResult<bool>> DeleteLiteraryTagSourceAsync(int id)
        {
            try
            {
                var db = await _context.LiteraryTagSources.Where(s => s.Id == id).SingleOrDefaultAsync();
                if (db == null)
                    return new RServiceResult<bool>(false, "منبع یافت نشد.");
                _context.LiteraryTagSources.Remove(db);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}
