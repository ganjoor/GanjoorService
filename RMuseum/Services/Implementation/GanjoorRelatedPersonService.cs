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
        /// submit a suggested edit to an already-approved person's own fields
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonEditSuggestion>> SuggestPersonEditAsync(GanjoorPersonEditSuggestion suggestion)
        {
            try
            {
                if (suggestion == null || string.IsNullOrWhiteSpace(suggestion.SuggestedName))
                {
                    return new RServiceResult<GanjoorPersonEditSuggestion>(null, "نام شخصیت نمی‌تواند خالی باشد.");
                }

                var personExists = await _context.GanjoorRelatedPersons.Where(p => p.Id == suggestion.PersonId).AnyAsync();
                if (!personExists)
                {
                    return new RServiceResult<GanjoorPersonEditSuggestion>(null, "شخصیت پیدا نشد.");
                }

                suggestion.Id = 0;
                suggestion.Date = DateTime.Now;
                suggestion.SuggestedName = suggestion.SuggestedName.Trim();
                suggestion.SuggestedDescription = string.IsNullOrWhiteSpace(suggestion.SuggestedDescription) ? null : suggestion.SuggestedDescription.Trim();
                suggestion.SuggestedWikiUrl = string.IsNullOrWhiteSpace(suggestion.SuggestedWikiUrl) ? null : suggestion.SuggestedWikiUrl.Trim();
                suggestion.SuggestedFamilyTreeCaption = string.IsNullOrWhiteSpace(suggestion.SuggestedFamilyTreeCaption) ? null : suggestion.SuggestedFamilyTreeCaption.Trim();
                suggestion.Reviewed = false;
                suggestion.Result = CorrectionReviewResult.NotReviewed;
                suggestion.ReviewNote = null;
                suggestion.ReviewerUserId = null;

                _context.GanjoorPersonEditSuggestions.Add(suggestion);
                await _context.SaveChangesAsync();

                return new RServiceResult<GanjoorPersonEditSuggestion>(suggestion);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonEditSuggestion>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get the next unreviewed person-edit suggestion for the moderator queue
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonEditSuggestion>> GetNextUnreviewedPersonEditSuggestionAsync(int skip)
        {
            try
            {
                var suggestion = await _context.GanjoorPersonEditSuggestions
                    .Include(s => s.Person)
                    .Include(s => s.User)
                    .Include(s => s.SuggestedBirthLocation)
                    .Include(s => s.SuggestedDeathLocation)
                    .Where(s => s.Reviewed == false)
                    .OrderBy(s => s.Id)
                    .Skip(skip)
                    .FirstOrDefaultAsync();

                return new RServiceResult<GanjoorPersonEditSuggestion>(suggestion);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonEditSuggestion>(null, exp.ToString());
            }
        }

        /// <summary>
        /// unreviewed person-edit suggestion count
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreviewedPersonEditSuggestionCountAsync()
        {
            try
            {
                return new RServiceResult<int>(await _context.GanjoorPersonEditSuggestions.Where(s => s.Reviewed == false).CountAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// apply a moderator's decision to a pending person-edit suggestion
        /// </summary>
        /// <param name="moderatorUserId"></param>
        /// <param name="suggestionId"></param>
        /// <param name="result"></param>
        /// <param name="reviewNote"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonEditSuggestion>> ModeratePersonEditSuggestionAsync(Guid moderatorUserId, int suggestionId, CorrectionReviewResult result, string reviewNote)
        {
            try
            {
                var suggestion = await _context.GanjoorPersonEditSuggestions.Where(s => s.Id == suggestionId).SingleOrDefaultAsync();
                if (suggestion == null)
                {
                    return new RServiceResult<GanjoorPersonEditSuggestion>(null, "پیشنهاد ویرایش پیدا نشد.");
                }

                if (suggestion.Reviewed)
                {
                    return new RServiceResult<GanjoorPersonEditSuggestion>(null, "این پیشنهاد پیش‌تر بررسی شده است.");
                }

                if (result == CorrectionReviewResult.Approved)
                {
                    var person = await _context.GanjoorRelatedPersons.Where(p => p.Id == suggestion.PersonId).SingleOrDefaultAsync();
                    if (person == null)
                    {
                        return new RServiceResult<GanjoorPersonEditSuggestion>(null, "شخصیت مقصد این پیشنهاد پیدا نشد.");
                    }

                    person.Name = suggestion.SuggestedName;
                    person.Description = suggestion.SuggestedDescription;
                    person.WikiUrl = suggestion.SuggestedWikiUrl;
                    person.BirthYearInLHijri = suggestion.SuggestedBirthYearInLHijri;
                    person.DeathYearInLHijri = suggestion.SuggestedDeathYearInLHijri;
                    person.ValidBirthDate = suggestion.SuggestedValidBirthDate;
                    person.ValidDeathDate = suggestion.SuggestedValidDeathDate;
                    person.BirthLocationId = suggestion.SuggestedBirthLocationId;
                    person.DeathLocationId = suggestion.SuggestedDeathLocationId;
                    person.FamilyTreeCaption = suggestion.SuggestedFamilyTreeCaption;
                    // Id and MachineGenerated on the person are intentionally left untouched
                }

                suggestion.Reviewed = true;
                suggestion.Result = result;
                suggestion.ReviewNote = reviewNote;
                suggestion.ReviewDate = DateTime.Now;
                suggestion.ReviewerUserId = moderatorUserId;

                await _context.SaveChangesAsync();

                return new RServiceResult<GanjoorPersonEditSuggestion>(suggestion);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonEditSuggestion>(null, exp.ToString());
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
