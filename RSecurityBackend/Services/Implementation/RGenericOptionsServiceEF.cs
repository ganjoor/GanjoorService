using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Generic.Db;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// generic options service
    /// </summary>
    public class RGenericOptionsServiceEF : IRGenericOptionsService
    {
        /// <summary>
        /// modify an option or add a new one if an option with the requested name does not exist
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RGenericOption>> SetAsync(string optionName, string optionValue, Guid? userId)
        {
            var existing = await _context.Options.Where(o => o.Name == optionName && o.RAppUserId == userId).SingleOrDefaultAsync();
            if (existing != null)
            {
                existing.Value = optionValue;
                _context.Options.Update(existing);
                await _context.SaveChangesAsync();

                return new RServiceResult<RGenericOption>(existing);
            }

            var option = new RGenericOption()
            {
                Name = optionName,
                Value = optionValue,
                RAppUserId = userId
            };

            _context.Options.Add(option);
            await _context.SaveChangesAsync();

            return new RServiceResult<RGenericOption>(option);
        }

        /// <summary>
        /// get option value
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<string>> GetValueAsync(string optionName, Guid? userId)
        {
            var option = await _context.Options.AsNoTracking().Where(o => o.Name == optionName && o.RAppUserId == userId).SingleOrDefaultAsync();
            if (option == null)
                return new RServiceResult<string>(""); //empty value
            return new RServiceResult<string>(option.Value);
        }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public RGenericOptionsServiceEF(RSecurityDbContext<RAppUser, RAppRole, Guid> context)
        {
            _context = context;
        }
    }
}
