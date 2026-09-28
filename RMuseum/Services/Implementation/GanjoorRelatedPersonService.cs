using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// related people service implementation
    /// </summary>
    public class GanjoorRelatedPersonService : IGanjoorRelatedPersonService
    {
        /// <summary>
        /// get all people
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorRelatedPerson[]>> GetPeopleAsync()
        {
            try
            {
                return new RServiceResult<GanjoorRelatedPerson[]>
                    (
                    await _context.GanjoorRelatedPersons
                    .OrderBy(p => p.Name).ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorRelatedPerson[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get person by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorRelatedPerson>> GetPersonAsync(int id)
        {
            try
            {
                return new RServiceResult<GanjoorRelatedPerson>
                    (
                    await _context.GanjoorRelatedPersons
                    .Where(p => p.Id == id)
                    .SingleOrDefaultAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorRelatedPerson>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        public GanjoorRelatedPersonService(RMuseumDbContext context)
        {
            _context = context;
        }
    }
}
