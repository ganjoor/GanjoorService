using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Notification;
using RSecurityBackend.Services;
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
                    Id = r.Id,
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
        /// get the whole connected kinship component reachable from this person
        /// </summary>
        /// <param name="rootId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorFamilyTreeViewModel>> GetFamilyTreeAsync(int rootId)
        {
            try
            {
                var rootExists = await _context.GanjoorRelatedPersons.Where(p => p.Id == rootId).AnyAsync();
                if (!rootExists)
                {
                    return new RServiceResult<GanjoorFamilyTreeViewModel>(null, "شخصیت پیدا نشد.");
                }

                // kinship graph tends to be small (a few hundred rows at most for this kind of data),
                // so it's simplest/cheapest to load the whole table and walk it in memory rather than
                // issuing a recursive query
                var allRelations = await _context.GanjoorPersonRelations.ToListAsync();

                var edgesByPersonId = new Dictionary<int, List<GanjoorPersonRelation>>();
                void IndexEdge(int personId, GanjoorPersonRelation edge)
                {
                    if (!edgesByPersonId.TryGetValue(personId, out var list))
                    {
                        list = new List<GanjoorPersonRelation>();
                        edgesByPersonId[personId] = list;
                    }
                    list.Add(edge);
                }
                foreach (var edge in allRelations)
                {
                    IndexEdge(edge.Person1Id, edge);
                    IndexEdge(edge.Person2Id, edge);
                }

                var visitedPersonIds = new HashSet<int>() { rootId };
                var visitedRelationIds = new HashSet<int>();
                var queue = new Queue<int>();
                queue.Enqueue(rootId);

                while (queue.Count > 0)
                {
                    var personId = queue.Dequeue();
                    if (!edgesByPersonId.TryGetValue(personId, out var touchingEdges))
                        continue;

                    foreach (var edge in touchingEdges)
                    {
                        visitedRelationIds.Add(edge.Id);
                        var otherPersonId = edge.Person1Id == personId ? edge.Person2Id : edge.Person1Id;
                        if (visitedPersonIds.Add(otherPersonId))
                        {
                            queue.Enqueue(otherPersonId);
                        }
                    }
                }

                var persons = await _context.GanjoorRelatedPersons
                    .Where(p => visitedPersonIds.Contains(p.Id))
                    .OrderBy(p => p.Id)
                    .ToListAsync();

                var relations = allRelations
                    .Where(r => visitedRelationIds.Contains(r.Id))
                    .Select(r => new GanjoorFamilyTreeEdge()
                    {
                        Person1Id = r.Person1Id,
                        Person2Id = r.Person2Id,
                        RelationType = r.RelationType,
                        DegreeHint = r.DegreeHint,
                    })
                    .ToList();

                return new RServiceResult<GanjoorFamilyTreeViewModel>(new GanjoorFamilyTreeViewModel()
                {
                    RootId = rootId,
                    Persons = persons,
                    Relations = relations,
                });
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorFamilyTreeViewModel>(null, exp.ToString());
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
                if (suggestion == null || (!suggestion.SuggestedForDeletion && string.IsNullOrWhiteSpace(suggestion.SuggestedName)))
                {
                    return new RServiceResult<GanjoorPersonEditSuggestion>(null, "نام شخصیت نمی‌تواند خالی باشد.");
                }

                var person = await _context.GanjoorRelatedPersons.Where(p => p.Id == suggestion.PersonId).SingleOrDefaultAsync();
                if (person == null)
                {
                    return new RServiceResult<GanjoorPersonEditSuggestion>(null, "شخصیت پیدا نشد.");
                }

                suggestion.Id = 0;
                suggestion.Date = DateTime.Now;
                suggestion.SuggestedName = suggestion.SuggestedName?.Trim();
                suggestion.SuggestedDescription = string.IsNullOrWhiteSpace(suggestion.SuggestedDescription) ? null : suggestion.SuggestedDescription.Trim();
                suggestion.SuggestedWikiUrl = string.IsNullOrWhiteSpace(suggestion.SuggestedWikiUrl) ? null : suggestion.SuggestedWikiUrl.Trim();
                suggestion.SuggestedFamilyTreeCaption = string.IsNullOrWhiteSpace(suggestion.SuggestedFamilyTreeCaption) ? null : suggestion.SuggestedFamilyTreeCaption.Trim();
                suggestion.Reviewed = false;
                suggestion.Result = CorrectionReviewResult.NotReviewed;
                suggestion.ReviewNote = null;
                suggestion.ReviewerUserId = null;

                _context.GanjoorPersonEditSuggestions.Add(suggestion);
                await _context.SaveChangesAsync();

                await NotifyModeratorsOfPendingSuggestionAsync(
                    suggestion.SuggestedForDeletion ? "پیشنهاد حذف شخصیت" : "پیشنهاد ویرایش شخصیت",
                    (suggestion.SuggestedForDeletion
                        ? $"کاربری پیشنهاد حذف شخصیت «{person.Name}» را داده است."
                        : $"کاربری ویرایش جدیدی برای شخصیت «{person.Name}» پیشنهاد داده است.") +
                    $" لطفاً بخش <a href=\"https://ganjoor.net/Admin/ReviewPersonEdits\">ویرایش‌های پیشنهادی شخصیت‌ها</a> را بررسی فرمایید."
                );

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

                var person = await _context.GanjoorRelatedPersons.Where(p => p.Id == suggestion.PersonId).SingleOrDefaultAsync();
                if (person == null)
                {
                    return new RServiceResult<GanjoorPersonEditSuggestion>(null, "شخصیت مقصد این پیشنهاد پیدا نشد.");
                }
                var personName = person.Name; // snapshot before a possible deletion below, for the notification text

                if (result == CorrectionReviewResult.Approved)
                {
                    if (suggestion.SuggestedForDeletion)
                    {
                        await DeletePersonAndReferencesAsync(person);
                    }
                    else
                    {
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
                }

                suggestion.Reviewed = true;
                suggestion.Result = result;
                suggestion.ReviewNote = reviewNote;
                suggestion.ReviewDate = DateTime.Now;
                suggestion.ReviewerUserId = moderatorUserId;

                await _context.SaveChangesAsync();

                if (result == CorrectionReviewResult.Approved)
                {
                    await _notificationService.PushNotification(
                        suggestion.UserId,
                        suggestion.SuggestedForDeletion ? "تأیید حذف شخصیت پیشنهادی" : "تأیید ویرایش پیشنهادی شخصیت",
                        suggestion.SuggestedForDeletion
                            ? $"پیشنهاد شما برای حذف شخصیت «{personName}» تأیید و اعمال شد. از این که به تکمیل اطلاعات گنجور کمک کردید سپاسگزاریم."
                            : $"ویرایش پیشنهادی شما برای شخصیت «{personName}» تأیید و اعمال شد. از این که به تکمیل اطلاعات گنجور کمک کردید سپاسگزاریم."
                    );
                }
                else
                {
                    await _notificationService.PushNotification(
                        suggestion.UserId,
                        suggestion.SuggestedForDeletion ? "رد پیشنهاد حذف شخصیت" : "رد ویرایش پیشنهادی شخصیت",
                        (suggestion.SuggestedForDeletion
                            ? $"پیشنهاد شما برای حذف شخصیت «{personName}» تأیید نشد."
                            : $"ویرایش پیشنهادی شما برای شخصیت «{personName}» تأیید نشد.") +
                        (string.IsNullOrWhiteSpace(reviewNote) ? "" : $"{Environment.NewLine}یادداشت بازبین: «{reviewNote}»"),
                        NotificationType.Warning
                    );
                }

                return new RServiceResult<GanjoorPersonEditSuggestion>(suggestion);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonEditSuggestion>(null, exp.ToString());
            }
        }

        /// <summary>
        /// removes a person and every row that would otherwise block that deletion at the database
        /// level (kinship edges, affiliations, and other pending relation-edit suggestions touching
        /// them - all Restrict-on-delete FKs, see RMuseumDbContext.OnModelCreating), plus detaches
        /// (but does not delete) any approved poem geo/date tag that named them, since the tag
        /// itself may still carry a real location/date worth keeping.
        /// </summary>
        private async Task DeletePersonAndReferencesAsync(GanjoorRelatedPerson person)
        {
            var relations = await _context.GanjoorPersonRelations
                .Where(r => r.Person1Id == person.Id || r.Person2Id == person.Id)
                .ToListAsync();
            var relationIds = relations.Select(r => r.Id).ToList();

            var affiliations = await _context.GanjoorPersonAffiliations
                .Where(a => a.Person1Id == person.Id || a.Person2Id == person.Id)
                .ToListAsync();
            var affiliationIds = affiliations.Select(a => a.Id).ToList();

            // any other still-pending relation-edit suggestion that touches this person directly, or
            // targets one of the relations/affiliations we're about to remove, would otherwise dangle
            // or violate the Restrict FKs above - auto-reject those with an explanatory note rather
            // than letting the delete fail or silently drop them
            var conflictingRelationSuggestions = await _context.GanjoorPersonRelationEditSuggestions
                .Where(s => !s.Reviewed && (
                    s.Person1Id == person.Id ||
                    s.Person2Id == person.Id ||
                    (s.ExistingRelationId != null && relationIds.Contains(s.ExistingRelationId.Value)) ||
                    (s.ExistingAffiliationId != null && affiliationIds.Contains(s.ExistingAffiliationId.Value))
                ))
                .ToListAsync();
            foreach (var conflicting in conflictingRelationSuggestions)
            {
                conflicting.Reviewed = true;
                conflicting.Result = CorrectionReviewResult.RejectedBecauseUnnecessaryChange;
                conflicting.ReviewNote = "شخصیت یا نسبت مرتبط با این پیشنهاد حذف شد.";
                conflicting.ReviewDate = DateTime.Now;
            }

            var geoDateTags = await _context.PoemGeoDateTags.Where(t => t.PersonId == person.Id).ToListAsync();
            foreach (var tag in geoDateTags)
            {
                tag.PersonId = null;
            }

            // any other still-pending edit suggestion FOR this same person (not the one being
            // approved right now, which the caller updates separately) is moot once the person is
            // gone - same auto-reject treatment
            var conflictingEditSuggestions = await _context.GanjoorPersonEditSuggestions
                .Where(s => !s.Reviewed && s.PersonId == person.Id)
                .ToListAsync();
            foreach (var conflicting in conflictingEditSuggestions)
            {
                conflicting.Reviewed = true;
                conflicting.Result = CorrectionReviewResult.RejectedBecauseUnnecessaryChange;
                conflicting.ReviewNote = "این شخصیت حذف شد.";
                conflicting.ReviewDate = DateTime.Now;
            }

            _context.GanjoorPersonRelations.RemoveRange(relations);
            _context.GanjoorPersonAffiliations.RemoveRange(affiliations);
            _context.GanjoorRelatedPersons.Remove(person);
        }

        /// <summary>
        /// get a single kinship edge by its own id, with both sides' names resolved
        /// </summary>
        /// <param name="relationId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonRelation>> GetRelationByIdAsync(int relationId)
        {
            try
            {
                var relation = await _context.GanjoorPersonRelations
                    .Include(r => r.Person1)
                    .Include(r => r.Person2)
                    .Where(r => r.Id == relationId)
                    .SingleOrDefaultAsync();

                if (relation == null)
                {
                    return new RServiceResult<GanjoorPersonRelation>(null, "نسبت پیدا نشد.");
                }

                return new RServiceResult<GanjoorPersonRelation>(relation);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonRelation>(null, exp.ToString());
            }
        }

        /// <summary>
        /// submit a suggested addition, change or removal of a kinship edge
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonRelationEditSuggestion>> SuggestPersonRelationEditAsync(GanjoorPersonRelationEditSuggestion suggestion)
        {
            try
            {
                if (suggestion == null)
                {
                    return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "اطلاعات پیشنهاد ناقص است.");
                }

                GanjoorPersonRelation existingRelation = null;
                GanjoorPersonAffiliation existingAffiliation = null;
                if (suggestion.Action == PersonRelationSuggestionAction.Modify || suggestion.Action == PersonRelationSuggestionAction.Remove)
                {
                    if (suggestion.Kind == PersonRelationSuggestionKind.Affiliation)
                    {
                        if (suggestion.ExistingAffiliationId == null)
                        {
                            return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "وابستگی مورد نظر برای ویرایش یا حذف مشخص نشده است.");
                        }

                        existingAffiliation = await _context.GanjoorPersonAffiliations
                            .Include(a => a.Person1)
                            .Include(a => a.Person2)
                            .Where(a => a.Id == suggestion.ExistingAffiliationId.Value)
                            .SingleOrDefaultAsync();

                        if (existingAffiliation == null)
                        {
                            return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "وابستگی مورد نظر پیدا نشد.");
                        }

                        // same trust-the-existing-row convention as the Family branch below
                        suggestion.Person1Id = existingAffiliation.Person1Id;
                        suggestion.Person2Id = existingAffiliation.Person2Id;
                        if (suggestion.Action == PersonRelationSuggestionAction.Remove)
                        {
                            suggestion.SuggestedAffiliationType = existingAffiliation.AffiliationType;
                            suggestion.SuggestedNote = existingAffiliation.Note;
                        }
                    }
                    else
                    {
                        if (suggestion.ExistingRelationId == null)
                        {
                            return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "نسبت مورد نظر برای ویرایش یا حذف مشخص نشده است.");
                        }

                        existingRelation = await _context.GanjoorPersonRelations
                            .Include(r => r.Person1)
                            .Include(r => r.Person2)
                            .Where(r => r.Id == suggestion.ExistingRelationId.Value)
                            .SingleOrDefaultAsync();

                        if (existingRelation == null)
                        {
                            return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "نسبت مورد نظر پیدا نشد.");
                        }

                        // always trust the existing relation's own Person1Id/Person2Id/type/etc over
                        // whatever the client sent, so the suggestion is guaranteed self-consistent with
                        // what it actually targets, even for a Remove-display
                        suggestion.Person1Id = existingRelation.Person1Id;
                        suggestion.Person2Id = existingRelation.Person2Id;
                        if (suggestion.Action == PersonRelationSuggestionAction.Remove)
                        {
                            suggestion.SuggestedRelationType = existingRelation.RelationType;
                            suggestion.SuggestedDegreeHint = existingRelation.DegreeHint;
                            suggestion.SuggestedNote = existingRelation.Note;
                        }
                    }
                }
                else // Add
                {
                    if (suggestion.Person1Id == suggestion.Person2Id)
                    {
                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "دو طرف یک نسبت نمی‌توانند یک نفر باشند.");
                    }
                }

                var person1 = await _context.GanjoorRelatedPersons.Where(p => p.Id == suggestion.Person1Id).SingleOrDefaultAsync();
                var person2 = await _context.GanjoorRelatedPersons.Where(p => p.Id == suggestion.Person2Id).SingleOrDefaultAsync();
                if (person1 == null || person2 == null)
                {
                    return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "یکی از دو طرف نسبت پیدا نشد.");
                }

                suggestion.Id = 0;
                suggestion.ExistingRelation = null;
                suggestion.ExistingAffiliation = null;
                suggestion.Person1 = null;
                suggestion.Person2 = null;
                suggestion.Date = DateTime.Now;
                suggestion.SuggestedNote = string.IsNullOrWhiteSpace(suggestion.SuggestedNote) ? null : suggestion.SuggestedNote.Trim();
                suggestion.SuggestionNote = string.IsNullOrWhiteSpace(suggestion.SuggestionNote) ? null : suggestion.SuggestionNote.Trim();
                suggestion.Reviewed = false;
                suggestion.Result = CorrectionReviewResult.NotReviewed;
                suggestion.ReviewNote = null;
                suggestion.ReviewerUserId = null;

                _context.GanjoorPersonRelationEditSuggestions.Add(suggestion);
                await _context.SaveChangesAsync();

                bool isAffiliation = suggestion.Kind == PersonRelationSuggestionKind.Affiliation;
                string actionTitle = suggestion.Action switch
                {
                    PersonRelationSuggestionAction.Add => isAffiliation ? "پیشنهاد وابستگی جدید" : "پیشنهاد نسبت خویشاوندی جدید",
                    PersonRelationSuggestionAction.Modify => isAffiliation ? "پیشنهاد ویرایش وابستگی" : "پیشنهاد ویرایش نسبت خویشاوندی",
                    _ => isAffiliation ? "پیشنهاد حذف وابستگی" : "پیشنهاد حذف نسبت خویشاوندی",
                };
                string edgeLabel = isAffiliation ? "وابستگی" : "نسبت خویشاوندی";
                string actionText = suggestion.Action switch
                {
                    PersonRelationSuggestionAction.Add => $"کاربری پیشنهاد افزودن {edgeLabel} جدید بین «{person1.Name}» و «{person2.Name}» را داده است.",
                    PersonRelationSuggestionAction.Modify => $"کاربری پیشنهاد ویرایش {edgeLabel} بین «{person1.Name}» و «{person2.Name}» را داده است.",
                    _ => $"کاربری پیشنهاد حذف {edgeLabel} بین «{person1.Name}» و «{person2.Name}» را داده است.",
                };

                await NotifyModeratorsOfPendingSuggestionAsync(
                    actionTitle,
                    actionText + " لطفاً بخش <a href=\"https://ganjoor.net/Admin/ReviewPersonRelationEdits\">ویرایش‌های پیشنهادی نسبت‌های خویشاوندی</a> را بررسی فرمایید."
                );

                return new RServiceResult<GanjoorPersonRelationEditSuggestion>(suggestion);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get the next unreviewed relation-edit suggestion for the moderator queue
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonRelationEditSuggestion>> GetNextUnreviewedPersonRelationEditSuggestionAsync(int skip)
        {
            try
            {
                var suggestion = await _context.GanjoorPersonRelationEditSuggestions
                    .Include(s => s.Person1)
                    .Include(s => s.Person2)
                    .Include(s => s.ExistingRelation)
                    .Include(s => s.ExistingAffiliation)
                    .Include(s => s.User)
                    .Where(s => s.Reviewed == false)
                    .OrderBy(s => s.Id)
                    .Skip(skip)
                    .FirstOrDefaultAsync();

                return new RServiceResult<GanjoorPersonRelationEditSuggestion>(suggestion);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, exp.ToString());
            }
        }

        /// <summary>
        /// unreviewed relation-edit suggestion count
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreviewedPersonRelationEditSuggestionCountAsync()
        {
            try
            {
                return new RServiceResult<int>(await _context.GanjoorPersonRelationEditSuggestions.Where(s => s.Reviewed == false).CountAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// apply a moderator's decision to a pending relation-edit suggestion
        /// </summary>
        /// <param name="moderatorUserId"></param>
        /// <param name="suggestionId"></param>
        /// <param name="result"></param>
        /// <param name="reviewNote"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonRelationEditSuggestion>> ModeratePersonRelationEditSuggestionAsync(Guid moderatorUserId, int suggestionId, CorrectionReviewResult result, string reviewNote)
        {
            try
            {
                var suggestion = await _context.GanjoorPersonRelationEditSuggestions
                    .Include(s => s.Person1)
                    .Include(s => s.Person2)
                    .Where(s => s.Id == suggestionId)
                    .SingleOrDefaultAsync();

                if (suggestion == null)
                {
                    return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "پیشنهاد پیدا نشد.");
                }

                if (suggestion.Reviewed)
                {
                    return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "این پیشنهاد پیش‌تر بررسی شده است.");
                }

                var person1Name = suggestion.Person1?.Name;
                var person2Name = suggestion.Person2?.Name;

                if (result == CorrectionReviewResult.Approved)
                {
                    if (suggestion.Kind == PersonRelationSuggestionKind.Affiliation)
                    {
                        switch (suggestion.Action)
                        {
                            case PersonRelationSuggestionAction.Add:
                                _context.GanjoorPersonAffiliations.Add(new GanjoorPersonAffiliation()
                                {
                                    Person1Id = suggestion.Person1Id,
                                    Person2Id = suggestion.Person2Id,
                                    AffiliationType = suggestion.SuggestedAffiliationType ?? PersonAffiliationType.Other,
                                    Note = suggestion.SuggestedNote,
                                });
                                break;
                            case PersonRelationSuggestionAction.Modify:
                                {
                                    var existing = await _context.GanjoorPersonAffiliations.Where(a => a.Id == suggestion.ExistingAffiliationId.Value).SingleOrDefaultAsync();
                                    if (existing == null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "وابستگی مورد نظر دیگر وجود ندارد.");
                                    }
                                    existing.AffiliationType = suggestion.SuggestedAffiliationType ?? existing.AffiliationType;
                                    existing.Note = suggestion.SuggestedNote;
                                    break;
                                }
                            case PersonRelationSuggestionAction.Remove:
                                {
                                    var existing = await _context.GanjoorPersonAffiliations.Where(a => a.Id == suggestion.ExistingAffiliationId.Value).SingleOrDefaultAsync();
                                    if (existing != null)
                                    {
                                        // any other still-pending suggestion targeting this same affiliation
                                        // would otherwise dangle once it's gone - auto-reject those too
                                        var conflicting = await _context.GanjoorPersonRelationEditSuggestions
                                            .Where(s => !s.Reviewed && s.Id != suggestion.Id && s.ExistingAffiliationId == existing.Id)
                                            .ToListAsync();
                                        foreach (var c in conflicting)
                                        {
                                            c.Reviewed = true;
                                            c.Result = CorrectionReviewResult.RejectedBecauseUnnecessaryChange;
                                            c.ReviewNote = "این وابستگی پیش‌تر حذف شد.";
                                            c.ReviewDate = DateTime.Now;
                                        }
                                        _context.GanjoorPersonAffiliations.Remove(existing);
                                    }
                                    break;
                                }
                        }
                    }
                    else
                    {
                        switch (suggestion.Action)
                        {
                            case PersonRelationSuggestionAction.Add:
                                _context.GanjoorPersonRelations.Add(new GanjoorPersonRelation()
                                {
                                    Person1Id = suggestion.Person1Id,
                                    Person2Id = suggestion.Person2Id,
                                    RelationType = suggestion.SuggestedRelationType,
                                    DegreeHint = suggestion.SuggestedDegreeHint,
                                    Note = suggestion.SuggestedNote,
                                });
                                break;
                            case PersonRelationSuggestionAction.Modify:
                                {
                                    var existing = await _context.GanjoorPersonRelations.Where(r => r.Id == suggestion.ExistingRelationId.Value).SingleOrDefaultAsync();
                                    if (existing == null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "نسبت مورد نظر دیگر وجود ندارد.");
                                    }
                                    existing.RelationType = suggestion.SuggestedRelationType;
                                    existing.DegreeHint = suggestion.SuggestedDegreeHint;
                                    existing.Note = suggestion.SuggestedNote;
                                    break;
                                }
                            case PersonRelationSuggestionAction.Remove:
                                {
                                    var existing = await _context.GanjoorPersonRelations.Where(r => r.Id == suggestion.ExistingRelationId.Value).SingleOrDefaultAsync();
                                    if (existing != null)
                                    {
                                        // any other still-pending suggestion targeting this same relation
                                        // would otherwise dangle once it's gone - auto-reject those too
                                        var conflicting = await _context.GanjoorPersonRelationEditSuggestions
                                            .Where(s => !s.Reviewed && s.Id != suggestion.Id && s.ExistingRelationId == existing.Id)
                                            .ToListAsync();
                                        foreach (var c in conflicting)
                                        {
                                            c.Reviewed = true;
                                            c.Result = CorrectionReviewResult.RejectedBecauseUnnecessaryChange;
                                            c.ReviewNote = "این نسبت پیش‌تر حذف شد.";
                                            c.ReviewDate = DateTime.Now;
                                        }
                                        _context.GanjoorPersonRelations.Remove(existing);
                                    }
                                    break;
                                }
                        }
                    }
                }

                suggestion.Reviewed = true;
                suggestion.Result = result;
                suggestion.ReviewNote = reviewNote;
                suggestion.ReviewDate = DateTime.Now;
                suggestion.ReviewerUserId = moderatorUserId;

                await _context.SaveChangesAsync();

                string edgeLabel = suggestion.Kind == PersonRelationSuggestionKind.Affiliation ? "وابستگی" : "نسبت خویشاوندی";
                string actionLabel = suggestion.Action switch
                {
                    PersonRelationSuggestionAction.Add => $"افزودن {edgeLabel}",
                    PersonRelationSuggestionAction.Modify => $"ویرایش {edgeLabel}",
                    _ => $"حذف {edgeLabel}",
                };

                if (result == CorrectionReviewResult.Approved)
                {
                    await _notificationService.PushNotification(
                        suggestion.UserId,
                        $"تأیید {actionLabel}",
                        $"پیشنهاد شما برای {actionLabel} بین «{person1Name}» و «{person2Name}» تأیید و اعمال شد. از این که به تکمیل اطلاعات گنجور کمک کردید سپاسگزاریم."
                    );
                }
                else
                {
                    await _notificationService.PushNotification(
                        suggestion.UserId,
                        $"رد {actionLabel}",
                        $"پیشنهاد شما برای {actionLabel} بین «{person1Name}» و «{person2Name}» تأیید نشد." +
                        (string.IsNullOrWhiteSpace(reviewNote) ? "" : $"{Environment.NewLine}یادداشت بازبین: «{reviewNote}»"),
                        NotificationType.Warning
                    );
                }

                return new RServiceResult<GanjoorPersonRelationEditSuggestion>(suggestion);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get the whole known network of people, for the force-directed "ontology" explorer
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonGraphViewModel>> GetPersonGraphAsync()
        {
            try
            {
                var relations = await _context.GanjoorPersonRelations.ToListAsync();
                var affiliations = await _context.GanjoorPersonAffiliations.ToListAsync();

                var involvedPersonIds = new HashSet<int>();
                foreach (var r in relations)
                {
                    involvedPersonIds.Add(r.Person1Id);
                    involvedPersonIds.Add(r.Person2Id);
                }
                foreach (var a in affiliations)
                {
                    involvedPersonIds.Add(a.Person1Id);
                    involvedPersonIds.Add(a.Person2Id);
                }

                var persons = await _context.GanjoorRelatedPersons
                    .Where(p => involvedPersonIds.Contains(p.Id))
                    .ToListAsync();
                var personById = persons.ToDictionary(p => p.Id);

                // no "work" scope here, so every node is directly part of the graph - none of them
                // are a one-hop addition the way GetCatPersonGraphAsync's are
                var nodes = _BuildGraphNodes(persons, null);
                var edges = _BuildGraphEdges(relations, affiliations, personById);

                return new RServiceResult<GanjoorPersonGraphViewModel>(new GanjoorPersonGraphViewModel()
                {
                    Nodes = nodes,
                    Edges = edges,
                });
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonGraphViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get every category id in the subtree rooted at catId (catId itself plus every descendant,
        /// walked breadth-first) - a lighter-weight, purpose-built counterpart of
        /// GanjoorService._populateCategoryChildren (which builds full category objects via
        /// _GetCatById); this only needs ids, so it queries them directly
        /// </summary>
        private async Task<List<int>> _GetCategorySubtreeIdsAsync(int catId)
        {
            var result = new List<int> { catId };
            var queue = new Queue<int>();
            queue.Enqueue(catId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var childIds = await _context.GanjoorCategories
                    .Where(c => c.ParentId == current)
                    .Select(c => c.Id)
                    .ToListAsync();
                foreach (var childId in childIds)
                {
                    result.Add(childId);
                    queue.Enqueue(childId);
                }
            }
            return result;
        }

        /// <summary>
        /// get the network of people relevant to one work/category (e.g. a poet's Shahnameh, or one
        /// story within it like Nezami's Leyli o Majnoon) - the category-scoped counterpart of
        /// GetPersonGraphAsync, for the "شخصیت‌ها" tab on a category/poet page. Starts from every
        /// person directly tagged (PoemGeoDateTag.PersonId) in a poem under catId's subtree, then
        /// adds their relatives/affiliates one hop out even when
        /// those relatives are never tagged in the work themselves - so, say, a hero's father still
        /// shows up if he's known but never named in a verse, which helps a reader unfamiliar with
        /// the story rather than leaving the tree looking broken. Those one-hop additions are marked
        /// DirectlyTagged = false so the client can draw them as secondary.
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonGraphViewModel>> GetCatPersonGraphAsync(int catId)
        {
            try
            {
                var catExists = await _context.GanjoorCategories.Where(c => c.Id == catId).AnyAsync();
                if (!catExists)
                {
                    return new RServiceResult<GanjoorPersonGraphViewModel>(null, "بخش پیدا نشد.");
                }

                var catIds = await _GetCategorySubtreeIdsAsync(catId);

                var directPersonIds = await _context.PoemGeoDateTags
                    .Where(t => t.MachineGenerated == false && t.PersonId != null && catIds.Contains(t.Poem.CatId))
                    .Select(t => t.PersonId.Value)
                    .Distinct()
                    .ToListAsync();

                if (directPersonIds.Count == 0)
                {
                    return new RServiceResult<GanjoorPersonGraphViewModel>(new GanjoorPersonGraphViewModel()
                    {
                        Nodes = new List<GanjoorPersonGraphNode>(),
                        Edges = new List<GanjoorPersonGraphEdge>(),
                    });
                }

                var directIdSet = new HashSet<int>(directPersonIds);

                var oneHopRelations = await _context.GanjoorPersonRelations
                    .Where(r => directIdSet.Contains(r.Person1Id) || directIdSet.Contains(r.Person2Id))
                    .ToListAsync();
                var oneHopAffiliations = await _context.GanjoorPersonAffiliations
                    .Where(a => directIdSet.Contains(a.Person1Id) || directIdSet.Contains(a.Person2Id))
                    .ToListAsync();

                var allIdSet = new HashSet<int>(directIdSet);
                foreach (var r in oneHopRelations)
                {
                    allIdSet.Add(r.Person1Id);
                    allIdSet.Add(r.Person2Id);
                }
                foreach (var a in oneHopAffiliations)
                {
                    allIdSet.Add(a.Person1Id);
                    allIdSet.Add(a.Person2Id);
                }

                // re-query rather than reuse oneHopRelations/oneHopAffiliations, so an edge between
                // two one-hop additions (not just their link back to a directly-tagged person) is
                // also included, now that the final node set is known
                var relations = await _context.GanjoorPersonRelations
                    .Where(r => allIdSet.Contains(r.Person1Id) && allIdSet.Contains(r.Person2Id))
                    .ToListAsync();
                var affiliations = await _context.GanjoorPersonAffiliations
                    .Where(a => allIdSet.Contains(a.Person1Id) && allIdSet.Contains(a.Person2Id))
                    .ToListAsync();

                var persons = await _context.GanjoorRelatedPersons
                    .Where(p => allIdSet.Contains(p.Id))
                    .ToListAsync();
                var personById = persons.ToDictionary(p => p.Id);

                var nodes = _BuildGraphNodes(persons, directIdSet);
                var edges = _BuildGraphEdges(relations, affiliations, personById);

                return new RServiceResult<GanjoorPersonGraphViewModel>(new GanjoorPersonGraphViewModel()
                {
                    Nodes = nodes,
                    Edges = edges,
                });
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonGraphViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// shared node-list builder for GetPersonGraphAsync/GetCatPersonGraphAsync - directlyTaggedIds
        /// null means "everyone counts as directly part of the graph" (the whole-site graph has no
        /// notion of one-hop additions)
        /// </summary>
        private static List<GanjoorPersonGraphNode> _BuildGraphNodes(List<GanjoorRelatedPerson> persons, HashSet<int> directlyTaggedIds)
        {
            return persons.Select(p => new GanjoorPersonGraphNode()
            {
                Id = p.Id,
                Name = p.Name,
                HasFamilyTree = !string.IsNullOrEmpty(p.FamilyTreeCaption),
                DirectlyTagged = directlyTaggedIds == null || directlyTaggedIds.Contains(p.Id),
            }).ToList();
        }

        /// <summary>
        /// shared edge-list builder for GetPersonGraphAsync/GetCatPersonGraphAsync - flattens
        /// GanjoorPersonRelation and GanjoorPersonAffiliation rows into the common
        /// GanjoorPersonGraphEdge shape, resolving both sides' names from personById
        /// </summary>
        private static List<GanjoorPersonGraphEdge> _BuildGraphEdges(List<GanjoorPersonRelation> relations, List<GanjoorPersonAffiliation> affiliations, Dictionary<int, GanjoorRelatedPerson> personById)
        {
            var edges = new List<GanjoorPersonGraphEdge>();

            foreach (var r in relations)
            {
                edges.Add(new GanjoorPersonGraphEdge()
                {
                    Person1Id = r.Person1Id,
                    Person1Name = personById.TryGetValue(r.Person1Id, out var rp1) ? rp1.Name : "",
                    Person2Id = r.Person2Id,
                    Person2Name = personById.TryGetValue(r.Person2Id, out var rp2) ? rp2.Name : "",
                    Category = "Relation",
                    TypeValue = (int)r.RelationType,
                    DegreeHint = r.DegreeHint,
                    Note = r.Note,
                });
            }

            foreach (var a in affiliations)
            {
                edges.Add(new GanjoorPersonGraphEdge()
                {
                    Person1Id = a.Person1Id,
                    Person1Name = personById.TryGetValue(a.Person1Id, out var ap1) ? ap1.Name : "",
                    Person2Id = a.Person2Id,
                    Person2Name = personById.TryGetValue(a.Person2Id, out var ap2) ? ap2.Name : "",
                    Category = "Affiliation",
                    TypeValue = (int)a.AffiliationType,
                    Note = a.Note,
                });
            }

            return edges;
        }

        /// <summary>
        /// Database Context
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// used to find every user holding the Ganjoor:Modify permission, to notify them when a new
        /// suggestion needs review - same permission the moderation endpoints themselves require
        /// </summary>
        protected readonly IAppUserService _appUserService;

        /// <summary>
        /// used to notify moderators of a new pending suggestion, and submitters of its outcome -
        /// same service/pattern GanjoorService uses for poem corrections and song suggestions
        /// </summary>
        protected readonly IRNotificationService _notificationService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="appUserService"></param>
        /// <param name="notificationService"></param>
        public GanjoorRelatedPersonService(RMuseumDbContext context, IAppUserService appUserService, IRNotificationService notificationService)
        {
            _context = context;
            _appUserService = appUserService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// notify every user holding the Ganjoor:Modify permission that a new suggestion of the
        /// given kind is pending review at reviewPageUrl - shared by both suggestion types below
        /// </summary>
        private async Task NotifyModeratorsOfPendingSuggestionAsync(string title, string htmlText)
        {
            var moderators = await _appUserService.GetUsersHavingPermission(RMuseumSecurableItem.GanjoorEntityShortName, SecurableItem.ModifyOperationShortName);
            if (string.IsNullOrEmpty(moderators.ExceptionString))
            {
                foreach (var moderator in moderators.Result)
                {
                    await _notificationService.PushNotification((Guid)moderator.Id, title, htmlText, NotificationType.ActionRequired);
                }
            }
        }
    }
}
