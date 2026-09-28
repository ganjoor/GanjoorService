using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
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
        /// get people who caption a family tree
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorRelatedPerson[]>> GetFamilyTreeRootsAsync()
        {
            try
            {
                return new RServiceResult<GanjoorRelatedPerson[]>
                    (
                    await _context.GanjoorRelatedPersons
                    .Where(p => !string.IsNullOrEmpty(p.FamilyTreeCaption))
                    .OrderBy(p => p.FamilyTreeCaption)
                    .ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorRelatedPerson[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get a person along with all their kinship/affiliation edges
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonRelationsViewModel>> GetPersonRelationsAsync(int id)
        {
            try
            {
                var person = await _context.GanjoorRelatedPersons.Where(p => p.Id == id).SingleOrDefaultAsync();
                if (person == null)
                {
                    return new RServiceResult<GanjoorPersonRelationsViewModel>(null, "شخصیت پیدا نشد.");
                }

                var relationRows = await _context.GanjoorPersonRelations
                    .Include(r => r.Person1)
                    .Include(r => r.Person2)
                    .Where(r => r.Person1Id == id || r.Person2Id == id)
                    .ToListAsync();

                var relations = relationRows.Select(r => new GanjoorPersonRelationInfo()
                {
                    OtherPersonId = r.Person1Id == id ? r.Person2Id : r.Person1Id,
                    OtherPersonName = r.Person1Id == id ? r.Person2.Name : r.Person1.Name,
                    RelationType = r.RelationType,
                    DegreeHint = r.DegreeHint,
                    Note = r.Note,
                    SubjectIsPerson1 = r.Person1Id == id,
                }).ToList();

                var affiliationRows = await _context.GanjoorPersonAffiliations
                    .Include(a => a.Person1)
                    .Include(a => a.Person2)
                    .Where(a => a.Person1Id == id || a.Person2Id == id)
                    .ToListAsync();

                var affiliations = affiliationRows.Select(a => new GanjoorPersonAffiliationInfo()
                {
                    OtherPersonId = a.Person1Id == id ? a.Person2Id : a.Person1Id,
                    OtherPersonName = a.Person1Id == id ? a.Person2.Name : a.Person1.Name,
                    AffiliationType = a.AffiliationType,
                    Note = a.Note,
                    SubjectIsPerson1 = a.Person1Id == id,
                }).ToList();

                return new RServiceResult<GanjoorPersonRelationsViewModel>(new GanjoorPersonRelationsViewModel()
                {
                    Person = person,
                    Relations = relations,
                    Affiliations = affiliations,
                });
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonRelationsViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get the (approved, materialized) poem geo/date tags that name this person
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PoemGeoDateTag[]>> GetPoemsByPersonAsync(int id)
        {
            try
            {
                var tags = await _context.PoemGeoDateTags
                    .Include(t => t.Poem)
                    .Where(t => t.PersonId == id && t.MachineGenerated == false)
                    .OrderBy(t => t.Id)
                    .ToArrayAsync();

                // Poem is included only to get to FullUrl/FullTitle for a link - strip the heavy
                // text fields before this goes over the wire, same as GetCatPoemGeoDateTagsAsync does
                foreach (var tag in tags)
                {
                    if (tag.Poem != null)
                    {
                        tag.Poem.HtmlText = null;
                        tag.Poem.PlainText = null;
                    }
                }

                return new RServiceResult<PoemGeoDateTag[]>(tags);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemGeoDateTag[]>(null, exp.ToString());
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
