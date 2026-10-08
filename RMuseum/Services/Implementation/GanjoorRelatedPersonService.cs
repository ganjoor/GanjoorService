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

                var relationIds = relationRows.Select(r => r.Id).ToList();
                var evidenceRows = await _context.GanjoorPersonRelationEvidences.AsNoTracking()
                    .Where(e => relationIds.Contains(e.RelationId))
                    .OrderBy(e => e.Id)
                    .ToListAsync();
                var evidenceCatTitles = await _GetCatTitlesAsync(evidenceRows.Select(e => e.MasterCatId).Distinct().ToList());

                var relations = relationRows.Select(r => new GanjoorPersonRelationInfo()
                {
                    Evidence = evidenceRows.Where(e => e.RelationId == r.Id).Select(e => _ToEvidenceInfo(e, evidenceCatTitles)).ToList(),
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
                    Id = a.Id,
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

                    // fill in the tagged couplet's own text (CoupletIndex 0 means "whole poem" - no
                    // single couplet to show), so the person page can show the actual verse instead
                    // of just a link to the poem it came from
                    if (tag.CoupletIndex > 0)
                    {
                        var coupletVerses = await _context.GanjoorVerses.AsNoTracking()
                            .Where(v => v.PoemId == tag.PoemId && v.CoupletIndex == tag.CoupletIndex)
                            .OrderBy(v => v.VOrder)
                            .ToListAsync();
                        if (coupletVerses.Count > 0)
                        {
                            tag.CoupletText = string.Join(" ", coupletVerses.Select(v => v.Text)).Trim();
                        }
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
        /// the state of every relation of a tree as seen from one book (master category), or null for
        /// the unfiltered view: "attested" (human evidence in this book), "otherBook" (evidence only
        /// elsewhere), "unattested" (no human evidence anywhere), or "contradicted" - only ever a
        /// Parent edge that is not attested here but conflicts with a parent attested here for the
        /// same child: the attested parent has the same (known) gender, or the child already has two
        /// attested parents here and this one is neither. Sibling/Spouse/Ancestor edges are never
        /// contradicted (multiple marriages are normal; ancestry has no exclusivity to test).
        /// </summary>
        private static Dictionary<int, string> _ComputeBookRelationStates(
            List<GanjoorPersonRelation> relations,
            List<GanjoorRelatedPerson> persons,
            Dictionary<int, List<int>> booksByRelation,
            int? masterCatId)
        {
            if (masterCatId == null)
            {
                return null;
            }

            var attestedHere = new HashSet<int>(booksByRelation.Where(kv => kv.Value.Contains(masterCatId.Value)).Select(kv => kv.Key));
            var genderOf = persons.ToDictionary(p => p.Id, p => p.Gender);

            var attestedParentsOf = relations
                .Where(r => r.RelationType == PersonRelationType.Parent && attestedHere.Contains(r.Id))
                .GroupBy(r => r.Person2Id)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Person1Id).Distinct().ToList());

            var states = new Dictionary<int, string>();
            foreach (var r in relations)
            {
                string state;
                if (attestedHere.Contains(r.Id))
                {
                    state = "attested";
                }
                else if (r.RelationType == PersonRelationType.Parent
                    && attestedParentsOf.TryGetValue(r.Person2Id, out var attestedParents)
                    && _ConflictsWithAttestedParents(r.Person1Id, attestedParents, genderOf))
                {
                    state = "contradicted";
                }
                else
                {
                    state = booksByRelation.ContainsKey(r.Id) ? "otherBook" : "unattested";
                }
                states[r.Id] = state;
            }
            return states;
        }

        private static bool _ConflictsWithAttestedParents(int parentId, List<int> attestedParents, Dictionary<int, PersonGender> genderOf)
        {
            var others = attestedParents.Where(q => q != parentId).ToList();
            if (others.Count >= 2)
            {
                return true; // both parent slots already taken by attested parents
            }

            var gender = genderOf.TryGetValue(parentId, out var g) ? g : PersonGender.Unknown;
            if (gender == PersonGender.Unknown)
            {
                return false; // can't tell father from mother - leave visible
            }
            return others.Any(q => genderOf.TryGetValue(q, out var gq) && gq == gender);
        }

        /// <summary>
        /// kinship relations with no human-attached evidence couplet (the worklist for adding evidence),
        /// oldest first
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="personId">only relations touching this person</param>
        /// <returns>the page of rows and the total number of matching relations</returns>
        public async Task<RServiceResult<(GanjoorRelationWithoutEvidence[] Rows, int TotalCount)>> GetRelationsWithoutEvidenceAsync(int skip, int take, int? personId)
        {
            try
            {
                var query = _context.GanjoorPersonRelations.AsNoTracking()
                    .Where(r => !_context.GanjoorPersonRelationEvidences.Any(e => e.RelationId == r.Id && !e.Inferred));
                if (personId.HasValue)
                {
                    query = query.Where(r => r.Person1Id == personId.Value || r.Person2Id == personId.Value);
                }

                int total = await query.CountAsync();
                var rows = await query
                    .OrderBy(r => r.Id)
                    .Skip(Math.Max(skip, 0))
                    .Take(Math.Clamp(take, 1, 200))
                    .Select(r => new GanjoorRelationWithoutEvidence()
                    {
                        RelationId = r.Id,
                        Person1Id = r.Person1Id,
                        Person1Name = r.Person1.Name,
                        Person2Id = r.Person2Id,
                        Person2Name = r.Person2.Name,
                        RelationType = r.RelationType,
                        DegreeHint = r.DegreeHint,
                        PendingEvidenceSuggestions = _context.GanjoorPersonRelationEditSuggestions
                            .Count(s => !s.Reviewed && s.ExistingRelationId == r.Id && s.Action == PersonRelationSuggestionAction.AddEvidence),
                    })
                    .ToArrayAsync();

                return new RServiceResult<(GanjoorRelationWithoutEvidence[] Rows, int TotalCount)>((rows, total));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(GanjoorRelationWithoutEvidence[] Rows, int TotalCount)>((null, 0), exp.ToString());
            }
        }

        /// <summary>
        /// affiliation types that imply both people were alive at the same time (the premise of the
        /// contemporaries view): every working/allied/hostile/companion tie, the explicit Contemporary
        /// type, Killer (the killer was alive when the victim died) and Panegyrized (praise is
        /// addressed to someone living). Deliberately left out - they say nothing reliable about
        /// overlap: Successor (an heir can come long after), Satirized (satire can be posthumous), Other.
        /// </summary>
        public static readonly PersonAffiliationType[] OverlapAffiliationTypes = new[]
        {
            PersonAffiliationType.Minister, PersonAffiliationType.Advisor, PersonAffiliationType.Courtier,
            PersonAffiliationType.Patron, PersonAffiliationType.Ally, PersonAffiliationType.Rival,
            PersonAffiliationType.Servant, PersonAffiliationType.Companion, PersonAffiliationType.Panegyrized,
            PersonAffiliationType.MilitaryCommander, PersonAffiliationType.Champion,
            PersonAffiliationType.Contemporary, PersonAffiliationType.Killer,
        };

        /// <summary>
        /// the people connected to this person by kinship or by an overlap-implying affiliation
        /// (transitively), with those ties - input of the "who could have been alive at the same
        /// time" view
        /// </summary>
        /// <param name="rootId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorContemporaryGraphViewModel>> GetContemporaryGraphAsync(int rootId)
        {
            try
            {
                var rootExists = await _context.GanjoorRelatedPersons.AsNoTracking().Where(p => p.Id == rootId).AnyAsync();
                if (!rootExists)
                {
                    return new RServiceResult<GanjoorContemporaryGraphViewModel>(null, "شخصیت پیدا نشد.");
                }

                var allKin = await _context.GanjoorPersonRelations.AsNoTracking().ToListAsync();
                var types = OverlapAffiliationTypes;
                var allAff = await _context.GanjoorPersonAffiliations.AsNoTracking()
                    .Where(a => types.Contains(a.AffiliationType))
                    .ToListAsync();

                var neighbours = new Dictionary<int, List<int>>();
                void Link(int a, int b)
                {
                    if (!neighbours.TryGetValue(a, out var la)) { la = new List<int>(); neighbours[a] = la; }
                    la.Add(b);
                    if (!neighbours.TryGetValue(b, out var lb)) { lb = new List<int>(); neighbours[b] = lb; }
                    lb.Add(a);
                }
                foreach (var r in allKin) Link(r.Person1Id, r.Person2Id);
                foreach (var a in allAff) Link(a.Person1Id, a.Person2Id);

                var visited = new HashSet<int> { rootId };
                var queue = new Queue<int>();
                queue.Enqueue(rootId);
                while (queue.Count > 0)
                {
                    var id = queue.Dequeue();
                    if (!neighbours.TryGetValue(id, out var list)) continue;
                    foreach (var other in list)
                    {
                        if (visited.Add(other)) queue.Enqueue(other);
                    }
                }

                var persons = await _context.GanjoorRelatedPersons.AsNoTracking()
                    .Where(p => visited.Contains(p.Id))
                    .OrderBy(p => p.Id)
                    .ToListAsync();

                return new RServiceResult<GanjoorContemporaryGraphViewModel>(new GanjoorContemporaryGraphViewModel()
                {
                    RootId = rootId,
                    Persons = persons,
                    Kin = allKin.Where(r => visited.Contains(r.Person1Id))
                        .Select(r => new GanjoorContemporaryKinTie()
                        {
                            Person1Id = r.Person1Id,
                            Person2Id = r.Person2Id,
                            RelationType = (int)r.RelationType,
                            DegreeHint = r.DegreeHint,
                        }).ToList(),
                    Affiliations = allAff.Where(a => visited.Contains(a.Person1Id))
                        .Select(a => new GanjoorContemporaryAffiliationTie()
                        {
                            Person1Id = a.Person1Id,
                            Person2Id = a.Person2Id,
                            AffiliationType = (int)a.AffiliationType,
                        }).ToList(),
                });
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorContemporaryGraphViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get the whole connected kinship component reachable from this person
        /// </summary>
        /// <param name="rootId"></param>
        /// <param name="masterCatId">optional master category (book) id: when given, each edge is labelled attested / otherBook / unattested / contradicted relative to that book</param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorFamilyTreeViewModel>> GetFamilyTreeAsync(int rootId, int? masterCatId = null)
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

                var treeRelations = allRelations.Where(r => visitedRelationIds.Contains(r.Id)).ToList();

                // human-attached evidence only: inferred (backfilled) rows never count for book views
                var evidenceRows = await _context.GanjoorPersonRelationEvidences.AsNoTracking()
                    .Where(e => !e.Inferred && visitedRelationIds.Contains(e.RelationId))
                    .ToListAsync();
                var catTitles = await _GetCatTitlesAsync(evidenceRows.Select(e => e.MasterCatId).Distinct().ToList());
                var booksByRelation = evidenceRows
                    .GroupBy(e => e.RelationId)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.MasterCatId).Distinct().ToList());

                var books = evidenceRows
                    .GroupBy(e => e.MasterCatId)
                    .Select(g => new GanjoorFamilyTreeBook()
                    {
                        Id = g.Key,
                        Title = catTitles.TryGetValue(g.Key, out var title) ? title : g.Key.ToString(),
                        RelationCount = g.Select(e => e.RelationId).Distinct().Count(),
                    })
                    .OrderByDescending(b => b.RelationCount)
                    .ThenBy(b => b.Title)
                    .ToList();

                var relationStates = _ComputeBookRelationStates(treeRelations, persons, booksByRelation, masterCatId);

                var relations = treeRelations
                    .Select(r => new GanjoorFamilyTreeEdge()
                    {
                        RelationId = r.Id,
                        Person1Id = r.Person1Id,
                        Person2Id = r.Person2Id,
                        RelationType = r.RelationType,
                        DegreeHint = r.DegreeHint,
                        State = relationStates != null ? relationStates[r.Id] : null,
                        AttestedIn = booksByRelation.TryGetValue(r.Id, out var attestedBooks)
                            ? attestedBooks.Select(b => catTitles.TryGetValue(b, out var bt) ? bt : b.ToString()).ToList()
                            : new List<string>(),
                    })
                    .ToList();

                return new RServiceResult<GanjoorFamilyTreeViewModel>(new GanjoorFamilyTreeViewModel()
                {
                    RootId = rootId,
                    Persons = persons,
                    Relations = relations,
                    MasterCatId = masterCatId,
                    Books = books,
                });
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorFamilyTreeViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// finds another person already carrying a non-null FamilyTreeCaption within the same
        /// connected kinship component as personId (if any). Captions are resolved per requested
        /// person, not stored on the tree itself, so nothing stops two different people in one
        /// connected component from each getting their own caption - which would silently produce
        /// two separate "family tree" list entries (GetFamilyTreeRootsAsync) that both open to the
        /// exact same graph. This is used to catch that at approval time rather than let it happen
        /// silently; it only ever flags an OTHER person, so re-saving/editing the caption a person
        /// already uniquely holds in their own tree is never blocked by it.
        /// </summary>
        private async Task<GanjoorRelatedPerson> _FindOtherFamilyTreeCaptionHolderInComponentAsync(int personId)
        {
            var allRelations = await _context.GanjoorPersonRelations.ToListAsync();
            var edgesByPersonId = new Dictionary<int, List<GanjoorPersonRelation>>();
            void IndexEdge(int pid, GanjoorPersonRelation edge)
            {
                if (!edgesByPersonId.TryGetValue(pid, out var list))
                {
                    list = new List<GanjoorPersonRelation>();
                    edgesByPersonId[pid] = list;
                }
                list.Add(edge);
            }
            foreach (var edge in allRelations)
            {
                IndexEdge(edge.Person1Id, edge);
                IndexEdge(edge.Person2Id, edge);
            }

            var visited = new HashSet<int>() { personId };
            var queue = new Queue<int>();
            queue.Enqueue(personId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!edgesByPersonId.TryGetValue(current, out var touchingEdges))
                    continue;
                foreach (var edge in touchingEdges)
                {
                    var otherPersonId = edge.Person1Id == current ? edge.Person2Id : edge.Person1Id;
                    if (visited.Add(otherPersonId))
                    {
                        queue.Enqueue(otherPersonId);
                    }
                }
            }
            visited.Remove(personId);

            if (visited.Count == 0)
                return null;

            return await _context.GanjoorRelatedPersons
                .Where(p => visited.Contains(p.Id) && !string.IsNullOrEmpty(p.FamilyTreeCaption))
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// loads every directed ancestor-type kinship edge (RelationType Parent or Ancestor,
        /// Person1 = ancestor, Person2 = descendant) currently in the live graph, optionally
        /// excluding one relation row by id (used so a Modify suggestion can be checked against
        /// every OTHER edge without tripping on the very row it's about to replace)
        /// </summary>
        private async Task<List<GanjoorPersonRelation>> _GetAncestorEdgesAsync(int? excludeRelationId)
        {
            var query = _context.GanjoorPersonRelations
                .Where(r => r.RelationType == PersonRelationType.Parent || r.RelationType == PersonRelationType.Ancestor);
            if (excludeRelationId != null)
            {
                query = query.Where(r => r.Id != excludeRelationId.Value);
            }
            return await query.ToListAsync();
        }

        /// <summary>
        /// true if adding a directed ancestor-type edge ancestorId -&gt; descendantId (ancestorId
        /// becomes a parent/ancestor of descendantId) would create a cycle in the kinship graph -
        /// i.e. descendantId is already (directly or transitively) an ancestor of ancestorId, or
        /// they're literally the same person. Without this, nothing stops e.g. approving "A is
        /// parent of B" and later "B is parent of A", which familytree.js's unguarded recursive
        /// layout() would then infinite-loop on when rendering that tree.
        /// </summary>
        private async Task<bool> _WouldCreateAncestryCycleAsync(int ancestorId, int descendantId, int? excludeRelationId)
        {
            if (ancestorId == descendantId)
                return true;

            var edges = await _GetAncestorEdgesAsync(excludeRelationId);
            var childrenOf = new Dictionary<int, List<int>>();
            foreach (var edge in edges)
            {
                if (!childrenOf.TryGetValue(edge.Person1Id, out var list))
                {
                    list = new List<int>();
                    childrenOf[edge.Person1Id] = list;
                }
                list.Add(edge.Person2Id);
            }

            // walk forward from descendantId: if it can already reach ancestorId through existing
            // edges, descendantId is already an ancestor of ancestorId, so the new edge would close a loop
            var visited = new HashSet<int>() { descendantId };
            var queue = new Queue<int>();
            queue.Enqueue(descendantId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == ancestorId)
                    return true;
                if (!childrenOf.TryGetValue(current, out var children))
                    continue;
                foreach (var child in children)
                {
                    if (visited.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// counts this child's distinct existing Parent-type edges (biological parents), optionally
        /// excluding one relation row by id - used to cap a person at two recorded parents, since
        /// familytree.js's buildLayout only ever attaches the first two it sorts to the front and
        /// silently drops any further ones with no error
        /// </summary>
        private async Task<int> _CountParentsAsync(int childId, int? excludeRelationId, ICollection<int> scopeMasterCatIds = null)
        {
            var query = _context.GanjoorPersonRelations
                .Where(r => r.RelationType == PersonRelationType.Parent && r.Person2Id == childId);
            if (excludeRelationId != null)
            {
                query = query.Where(r => r.Id != excludeRelationId.Value);
            }
            var parents = await query.Select(r => new { r.Id, r.Person1Id }).ToListAsync();
            if (scopeMasterCatIds != null)
            {
                var books = await _GetRelationBooksAsync(parents.Select(r => r.Id).ToList());
                parents = parents.Where(r => _InBookScope(books, r.Id, scopeMasterCatIds)).ToList();
            }
            return parents.Select(r => r.Person1Id).Distinct().Count();
        }

        /// <summary>
        /// the master categories (books) in which each of these relations is attested by human-attached
        /// evidence (inferred rows never count) - relations with no such evidence are absent from the result
        /// </summary>
        private async Task<Dictionary<int, HashSet<int>>> _GetRelationBooksAsync(List<int> relationIds)
        {
            var result = new Dictionary<int, HashSet<int>>();
            foreach (var chunk in relationIds.Chunk(500))
            {
                var rows = await _context.GanjoorPersonRelationEvidences.AsNoTracking()
                    .Where(e => !e.Inferred && chunk.Contains(e.RelationId))
                    .Select(e => new { e.RelationId, e.MasterCatId })
                    .ToListAsync();
                foreach (var row in rows)
                {
                    if (!result.TryGetValue(row.RelationId, out var set))
                    {
                        set = new HashSet<int>();
                        result[row.RelationId] = set;
                    }
                    set.Add(row.MasterCatId);
                }
            }
            return result;
        }

        /// <summary>
        /// whether an existing relation takes part in a validation made for the given books: a relation
        /// with no human evidence anywhere belongs to no book in particular, so it is always taken into
        /// account (nothing says it was told by a different source); an attested one only when it is
        /// attested in at least one of those books
        /// </summary>
        private static bool _InBookScope(Dictionary<int, HashSet<int>> booksByRelation, int relationId, ICollection<int> scopeMasterCatIds)
        {
            if (!booksByRelation.TryGetValue(relationId, out var books) || books.Count == 0)
            {
                return true;
            }
            return books.Any(b => scopeMasterCatIds.Contains(b));
        }

        /// <summary>
        /// the books a family relation suggestion should be validated against: the book of the evidence
        /// couplet it carries plus the books the relation it modifies is already attested in. Null (no
        /// book in particular - validate against everything, the pre-evidence behaviour) when neither exists.
        /// </summary>
        private async Task<HashSet<int>> _ResolveValidationScopeAsync(int? evidencePoemId, int? existingRelationId)
        {
            var scope = new HashSet<int>();
            if (evidencePoemId != null)
            {
                var masterCatId = await _GetMasterCatIdAsync(evidencePoemId.Value);
                if (masterCatId != null)
                {
                    scope.Add(masterCatId.Value);
                }
            }
            if (existingRelationId != null)
            {
                var books = await _GetRelationBooksAsync(new List<int> { existingRelationId.Value });
                if (books.TryGetValue(existingRelationId.Value, out var existingBooks))
                {
                    scope.UnionWith(existingBooks);
                }
            }
            return scope.Count == 0 ? null : scope;
        }

        /// <summary>
        /// returns an existing kinship edge between this unordered pair (if any), other than
        /// excludeRelationId, whose RelationType differs from proposedType - used to stop a pair
        /// from simultaneously carrying two contradictory family relations (e.g. Parent AND Spouse,
        /// or Parent AND Sibling, between the very same two people)
        /// </summary>
        private async Task<GanjoorPersonRelation> _GetConflictingRelationAsync(int person1Id, int person2Id, PersonRelationType proposedType, int? excludeRelationId, ICollection<int> scopeMasterCatIds = null)
        {
            var query = _context.GanjoorPersonRelations
                .Include(r => r.Person1)
                .Include(r => r.Person2)
                .Where(r =>
                    ((r.Person1Id == person1Id && r.Person2Id == person2Id) || (r.Person1Id == person2Id && r.Person2Id == person1Id))
                    && r.RelationType != proposedType);
            if (excludeRelationId != null)
            {
                query = query.Where(r => r.Id != excludeRelationId.Value);
            }
            var candidates = await query.ToListAsync();
            if (scopeMasterCatIds != null && candidates.Count > 0)
            {
                var books = await _GetRelationBooksAsync(candidates.Select(r => r.Id).ToList());
                candidates = candidates.Where(r => _InBookScope(books, r.Id, scopeMasterCatIds)).ToList();
            }
            return candidates.FirstOrDefault();
        }

        /// <summary>
        /// returns an existing kinship edge of exactly this type between this pair, other than
        /// excludeRelationId - same ordered pair, or either order for the interchangeable types
        /// (Sibling/Spouse). Complements _GetConflictingRelationAsync, which only looks at
        /// DIFFERENT types, so the same fact can't be recorded twice.
        /// </summary>
        private async Task<GanjoorPersonRelation> _GetDuplicateRelationAsync(int person1Id, int person2Id, PersonRelationType type, int? excludeRelationId)
        {
            bool symmetric = type == PersonRelationType.Sibling || type == PersonRelationType.Spouse;
            var query = _context.GanjoorPersonRelations
                .Include(r => r.Person1)
                .Include(r => r.Person2)
                .Where(r => r.RelationType == type &&
                    ((r.Person1Id == person1Id && r.Person2Id == person2Id) ||
                     (symmetric && r.Person1Id == person2Id && r.Person2Id == person1Id)));
            if (excludeRelationId != null)
            {
                query = query.Where(r => r.Id != excludeRelationId.Value);
            }
            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// same idea as _GetDuplicateRelationAsync, for non-family affiliation edges: same type and
        /// same ordered pair, or either order for the symmetric types
        /// </summary>
        private async Task<GanjoorPersonAffiliation> _GetDuplicateAffiliationAsync(int person1Id, int person2Id, PersonAffiliationType type, int? excludeAffiliationId)
        {
            bool symmetric = _symmetricAffiliationTypes.Contains(type);
            var query = _context.GanjoorPersonAffiliations
                .Include(a => a.Person1)
                .Include(a => a.Person2)
                .Where(a => a.AffiliationType == type &&
                    ((a.Person1Id == person1Id && a.Person2Id == person2Id) ||
                     (symmetric && a.Person1Id == person2Id && a.Person2Id == person1Id)));
            if (excludeAffiliationId != null)
            {
                query = query.Where(a => a.Id != excludeAffiliationId.Value);
            }
            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// runs every family-relation data-integrity check (self-reference is checked separately by
        /// the caller for Add) that applies to adding/changing a kinship edge of relationType between
        /// person1Id and person2Id: ancestry cycles, the two-parents cap, and contradictory relation
        /// types already existing between the same pair. Returns a Persian error message, or null if
        /// the edge is fine to create/apply. Shared by SuggestPersonRelationEditAsync (so a
        /// contradictory suggestion is rejected up front) and ModeratePersonRelationEditSuggestionAsync
        /// (so it's still caught even if another suggestion was approved in the meantime, or the
        /// submission-time check is ever bypassed).
        /// </summary>
        private async Task<string> _ValidateFamilyRelationAsync(int person1Id, int person2Id, PersonRelationType relationType, int? excludeRelationId, bool confirmedExtraParent = false, ICollection<int> scopeMasterCatIds = null)
        {
            var duplicateRelation = await _GetDuplicateRelationAsync(person1Id, person2Id, relationType, excludeRelationId);
            if (duplicateRelation != null)
            {
                return $"این نسبت خویشاوندی هم‌اکنون بین «{duplicateRelation.Person1?.Name}» و «{duplicateRelation.Person2?.Name}» ثبت شده است. برای تغییر جزئیات آن (مثل یادداشت یا درجه) پیشنهاد ویرایش همان نسبت را ثبت کنید.";
            }

            if (relationType == PersonRelationType.Parent || relationType == PersonRelationType.Ancestor)
            {
                if (await _WouldCreateAncestryCycleAsync(person1Id, person2Id, excludeRelationId))
                {
                    return "این نسبت باعث ایجاد حلقهٔ تناقض‌آمیز در شجره‌نامه می‌شود (مثلاً فردی نیای خود شناخته می‌شود). لطفاً نسبت‌های موجود بین این دو نفر و نیاکان/نوادگان آن‌ها را بررسی کنید.";
                }
            }

            if (relationType == PersonRelationType.Parent && !confirmedExtraParent)
            {
                var existingParentsCount = await _CountParentsAsync(person2Id, excludeRelationId, scopeMasterCatIds);
                if (existingParentsCount >= 2)
                {
                    return (scopeMasterCatIds != null ? "این نامبرده در این کتاب (یا بدون ذکر کتاب) " : "این نامبرده ") + "هم‌اکنون دو پدر/مادر ثبت‌شده دارد. اگر این سومی اشتباه است، نخست یکی از نسبت‌های پدر/مادری موجود را ویرایش یا حذف کنید؛ اگر عمداً یک پدر/مادر سوم (مثلاً بر اساس روایت دیگری) اضافه می‌کنید، گزینهٔ تأیید «سومین پدر/مادر» را علامت بزنید.";
                }
            }

            var conflictingRelation = await _GetConflictingRelationAsync(person1Id, person2Id, relationType, excludeRelationId, scopeMasterCatIds);
            if (conflictingRelation != null)
            {
                return (scopeMasterCatIds != null ? "(در این کتاب یا بدون ذکر کتاب) " : "") + $"هم‌اکنون نسبت خویشاوندی دیگری بین «{conflictingRelation.Person1?.Name}» و «{conflictingRelation.Person2?.Name}» ثبت شده که با نوع جدید پیشنهادی در تناقض است. لطفاً نخست آن را ویرایش یا حذف کنید.";
            }

            return null;
        }

        /// <summary>
        /// validation for attaching evidence from a book to an existing relation that is not attested in
        /// that book yet: from then on the relation takes part in that book's view, so it must not
        /// conflict with what that book already says (a second relation type between the same two people,
        /// or a third parent). Null if fine.
        /// </summary>
        private async Task<string> _ValidateRelationInBookAsync(GanjoorPersonRelation relation, int masterCatId, bool confirmedExtraParent)
        {
            var books = await _GetRelationBooksAsync(new List<int> { relation.Id });
            if (books.TryGetValue(relation.Id, out var attestedIn) && attestedIn.Contains(masterCatId))
            {
                return null; // already part of this book, validated when it got there
            }
            return await _ValidateFamilyRelationInBookOnlyAsync(relation.Person1Id, relation.Person2Id, relation.RelationType, relation.Id, confirmedExtraParent, new HashSet<int> { masterCatId });
        }

        private async Task<string> _ValidateFamilyRelationInBookOnlyAsync(int person1Id, int person2Id, PersonRelationType relationType, int excludeRelationId, bool confirmedExtraParent, ICollection<int> scope)
        {
            if (relationType == PersonRelationType.Parent && !confirmedExtraParent)
            {
                if (await _CountParentsAsync(person2Id, excludeRelationId, scope) >= 2)
                {
                    return "این نامبرده در این کتاب (یا بدون ذکر کتاب) هم‌اکنون دو پدر/مادر ثبت‌شده دارد. اگر عمداً پدر/مادر سومی را در این کتاب اضافه می‌کنید، گزینهٔ تأیید «سومین پدر/مادر» را علامت بزنید.";
                }
            }
            var conflictingRelation = await _GetConflictingRelationAsync(person1Id, person2Id, relationType, excludeRelationId, scope);
            if (conflictingRelation != null)
            {
                return $"(در این کتاب یا بدون ذکر کتاب) هم‌اکنون نسبت خویشاوندی دیگری بین «{conflictingRelation.Person1?.Name}» و «{conflictingRelation.Person2?.Name}» ثبت شده که با این نسبت در تناقض است. لطفاً نخست آن را ویرایش یا حذف کنید، یا مستند آن را در کتاب خودش ثبت کنید.";
            }
            return null;
        }

        /// <summary>
        /// affiliation types whose two sides are interchangeable (Person1/Person2 order carries no
        /// meaning) - mirrors the convention already baked into SuggestNewPersonRelation.cshtml's
        /// dropdown, where these are the only affiliation options with no "_Other"/"_Subject" pair
        /// </summary>
        private static readonly HashSet<PersonAffiliationType> _symmetricAffiliationTypes = new HashSet<PersonAffiliationType>()
        {
            PersonAffiliationType.Ally,
            PersonAffiliationType.Rival,
            PersonAffiliationType.Companion,
            PersonAffiliationType.Contemporary,
        };

        /// <summary>
        /// true if modifying an affiliation edge from oldType to newType would cross the symmetric/
        /// directional boundary - PersonAffiliationType.Other is excluded on either side, since its
        /// direction (if any) is whatever the free-text Note says rather than something the type
        /// itself implies, so moving into/out of Other is never treated as crossing the boundary
        /// </summary>
        private static bool _CrossesSymmetricDirectionalBoundary(PersonAffiliationType oldType, PersonAffiliationType newType)
        {
            if (oldType == PersonAffiliationType.Other || newType == PersonAffiliationType.Other)
                return false;
            return _symmetricAffiliationTypes.Contains(oldType) != _symmetricAffiliationTypes.Contains(newType);
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
                // an edit that changes nothing is pointless for the moderator (it is what a contributor
                // gets by opening the edit form and submitting it untouched) - reject it. A deletion
                // request is never a no-op, so it is exempt.
                if (!suggestion.SuggestedForDeletion)
                {
                    string Norm(string s) => (s ?? "").Trim();
                    bool unchanged =
                        Norm(suggestion.SuggestedName) == Norm(person.Name)
                        && Norm(suggestion.SuggestedDescription) == Norm(person.Description)
                        && Norm(suggestion.SuggestedWikiUrl) == Norm(person.WikiUrl)
                        && suggestion.SuggestedBirthYearInLHijri == person.BirthYearInLHijri
                        && suggestion.SuggestedDeathYearInLHijri == person.DeathYearInLHijri
                        && suggestion.SuggestedValidBirthDate == person.ValidBirthDate
                        && suggestion.SuggestedValidDeathDate == person.ValidDeathDate
                        && suggestion.SuggestedBirthLocationId == person.BirthLocationId
                        && suggestion.SuggestedDeathLocationId == person.DeathLocationId
                        && Norm(suggestion.SuggestedFamilyTreeCaption) == Norm(person.FamilyTreeCaption)
                        && suggestion.SuggestedImportance == person.Importance
                        && suggestion.SuggestedGender == person.Gender;
                    if (unchanged)
                    {
                        return new RServiceResult<GanjoorPersonEditSuggestion>(null, "در این پیشنهاد چیزی تغییر نکرده است.");
                    }
                }

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
                        // only when the caption is actually being set/changed: the edit form pre-fills the person's
                        // current caption, so an unrelated edit (description, name...) of someone who already
                        // holds a caption would otherwise be blocked by the other caption holders that
                        // legitimately already exist in the same tree
                        var captionChanged = !string.Equals(
                            (suggestion.SuggestedFamilyTreeCaption ?? "").Trim(),
                            (person.FamilyTreeCaption ?? "").Trim(),
                            StringComparison.Ordinal);
                        if (captionChanged && !string.IsNullOrWhiteSpace(suggestion.SuggestedFamilyTreeCaption) && !suggestion.ConfirmedDuplicateFamilyTreeCaption)
                        {
                            var otherCaptionHolder = await _FindOtherFamilyTreeCaptionHolderInComponentAsync(person.Id);
                            if (otherCaptionHolder != null)
                            {
                                return new RServiceResult<GanjoorPersonEditSuggestion>(null,
                                    $"شخصیت «{otherCaptionHolder.Name}» هم‌اکنون در همین خوشهٔ خویشاوندی (همان شجره‌نامه) عنوان تبارنامهٔ «{otherCaptionHolder.FamilyTreeCaption}» را دارد. تأیید این پیشنهاد باعث می‌شود یک شجره‌نامهٔ واحد دو عنوان/مدخل جداگانه در فهرست شجره‌نامه‌ها پیدا کند. اگر این دو عنوان جدا برای همان یک شجره‌نامه عمداً مدنظر است (مثلاً دو نام رایج برای یک سلسله)، پیشنهاد را با علامت‌زدن گزینهٔ تأیید «عنوان تبارنامهٔ تکراری» دوباره ثبت کنید؛ در غیر این صورت نخست عنوان «{otherCaptionHolder.Name}» را حذف یا ویرایش کنید، یا این پیشنهاد را رد کنید.");
                            }
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
                        person.Importance = suggestion.SuggestedImportance;
                        person.Gender = suggestion.SuggestedGender;
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

            // every relation-edit suggestion row (pending OR already reviewed - history included) that
            // still references this person or one of the relations/affiliations being removed would
            // violate a Restrict FK and make the delete fail. Rows whose own Person1/Person2 is this
            // person can't be kept (those columns are required), so they're deleted; rows that only
            // point at a removed relation/affiliation via the optional Existing*Id columns are kept
            // for history with that reference cleared. Still-pending ones among the latter are
            // auto-rejected with an explanatory note first.
            var referencingSuggestions = await _context.GanjoorPersonRelationEditSuggestions
                .Where(s =>
                    s.Person1Id == person.Id ||
                    s.Person2Id == person.Id ||
                    (s.ExistingRelationId != null && relationIds.Contains(s.ExistingRelationId.Value)) ||
                    (s.ExistingAffiliationId != null && affiliationIds.Contains(s.ExistingAffiliationId.Value)))
                .ToListAsync();
            foreach (var referencing in referencingSuggestions)
            {
                if (referencing.Person1Id == person.Id || referencing.Person2Id == person.Id)
                {
                    _context.GanjoorPersonRelationEditSuggestions.Remove(referencing);
                    continue;
                }
                if (!referencing.Reviewed)
                {
                    referencing.Reviewed = true;
                    referencing.Result = CorrectionReviewResult.RejectedBecauseUnnecessaryChange;
                    referencing.ReviewNote = "شخصیت یا نسبت مرتبط با این پیشنهاد حذف شد.";
                    referencing.ReviewDate = DateTime.Now;
                }
                referencing.ExistingRelationId = null;
                referencing.ExistingAffiliationId = null;
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

                var evidenceRows = await _context.GanjoorPersonRelationEvidences.AsNoTracking()
                    .Where(e => e.RelationId == relation.Id)
                    .OrderBy(e => e.Id)
                    .ToListAsync();
                var catTitles = await _GetCatTitlesAsync(evidenceRows.Select(e => e.MasterCatId).Distinct().ToList());
                relation.Evidence = evidenceRows.Select(e => _ToEvidenceInfo(e, catTitles)).ToList();

                return new RServiceResult<GanjoorPersonRelation>(relation);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonRelation>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get a single affiliation edge by its own id, with both sides' names resolved - the
        /// Kind == Affiliation counterpart of GetRelationByIdAsync, used the same way by
        /// /User/SuggestPersonRelationEdit?affiliationId={id}
        /// </summary>
        /// <param name="affiliationId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorPersonAffiliation>> GetAffiliationByIdAsync(int affiliationId)
        {
            try
            {
                var affiliation = await _context.GanjoorPersonAffiliations
                    .Include(a => a.Person1)
                    .Include(a => a.Person2)
                    .Where(a => a.Id == affiliationId)
                    .SingleOrDefaultAsync();

                if (affiliation == null)
                {
                    return new RServiceResult<GanjoorPersonAffiliation>(null, "وابستگی پیدا نشد.");
                }

                return new RServiceResult<GanjoorPersonAffiliation>(affiliation);
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorPersonAffiliation>(null, exp.ToString());
            }
        }

        private static GanjoorPersonRelationEvidenceInfo _ToEvidenceInfo(GanjoorPersonRelationEvidence e, Dictionary<int, string> catTitles)
        {
            return new GanjoorPersonRelationEvidenceInfo()
            {
                Id = e.Id,
                PoemId = e.PoemId,
                CoupletIndex = e.CoupletIndex,
                CoupletText = e.CoupletText,
                MasterCatId = e.MasterCatId,
                MasterCatTitle = catTitles != null && catTitles.TryGetValue(e.MasterCatId, out var title) ? title : null,
                Inferred = e.Inferred,
            };
        }

        private async Task<Dictionary<int, string>> _GetCatTitlesAsync(List<int> catIds)
        {
            if (catIds == null || catIds.Count == 0)
            {
                return new Dictionary<int, string>();
            }
            return await _context.GanjoorCategories.AsNoTracking()
                .Where(c => catIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Title);
        }

        /// <summary>
        /// the "master category" of a poem: walking up from the poem's own category, the first
        /// category whose CatType is Book, or the topmost category (ParentId == null) when none is -
        /// the label under which relation evidence from this poem is grouped. Null if the poem is unknown.
        /// </summary>
        private async Task<int?> _GetMasterCatIdAsync(int poemId)
        {
            var catId = await _context.GanjoorPoems.AsNoTracking().Where(p => p.Id == poemId).Select(p => (int?)p.CatId).SingleOrDefaultAsync();
            if (catId == null)
            {
                return null;
            }

            int current = catId.Value;
            for (int guard = 0; guard < 100; guard++)
            {
                var cat = await _context.GanjoorCategories.AsNoTracking()
                    .Where(c => c.Id == current)
                    .Select(c => new { c.Id, c.ParentId, c.CatType })
                    .SingleOrDefaultAsync();
                if (cat == null)
                {
                    return null;
                }
                if (cat.CatType == GanjoorCatType.Book || cat.ParentId == null)
                {
                    return cat.Id;
                }
                current = cat.ParentId.Value;
            }
            return null;
        }

        /// <summary>
        /// validates the evidence couplet carried by a suggestion (poem exists, couplet exists, not
        /// already attached to the relation) and fills in its server-side text snapshot. Returns an
        /// error message or null.
        /// </summary>
        private async Task<string> _ValidateAndFillEvidenceAsync(GanjoorPersonRelationEditSuggestion suggestion, int? relationId)
        {
            if (suggestion.EvidencePoemId == null || suggestion.EvidenceCoupletIndex == null)
            {
                return "شعر و بیت مورد نظر برای مستند کردن نسبت مشخص نشده است.";
            }

            var poemId = suggestion.EvidencePoemId.Value;
            var coupletIndex = suggestion.EvidenceCoupletIndex.Value;
            if (coupletIndex < 0)
            {
                return "شمارهٔ بیت نامعتبر است.";
            }

            var coupletVerses = await _context.GanjoorVerses.AsNoTracking()
                .Where(v => v.PoemId == poemId && v.CoupletIndex == coupletIndex)
                .OrderBy(v => v.VOrder)
                .ToListAsync();
            if (coupletVerses.Count == 0)
            {
                return "بیت مورد نظر در شعر مشخص‌شده پیدا نشد.";
            }

            if (await _GetMasterCatIdAsync(poemId) == null)
            {
                return "دستهٔ اصلی این شعر مشخص نشد.";
            }

            if (relationId != null && await _context.GanjoorPersonRelationEvidences.AsNoTracking()
                .AnyAsync(e => e.RelationId == relationId.Value && e.PoemId == poemId && e.CoupletIndex == coupletIndex))
            {
                return "این بیت هم‌اکنون به عنوان مستند این نسبت ثبت شده است.";
            }

            suggestion.EvidenceCoupletText = string.Join(" ", coupletVerses.Select(v => v.Text)).Trim();
            return null;
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

                bool isEvidenceAction = suggestion.Action == PersonRelationSuggestionAction.AddEvidence || suggestion.Action == PersonRelationSuggestionAction.RemoveEvidence;
                if (isEvidenceAction && suggestion.Kind != PersonRelationSuggestionKind.Family)
                {
                    return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "مستند فقط برای نسبت‌های خویشاوندی قابل ثبت است.");
                }

                GanjoorPersonRelation existingRelation = null;
                GanjoorPersonAffiliation existingAffiliation = null;
                if (suggestion.Action == PersonRelationSuggestionAction.Modify || suggestion.Action == PersonRelationSuggestionAction.Remove || isEvidenceAction)
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
                        else if (suggestion.Action == PersonRelationSuggestionAction.Modify && suggestion.SuggestedAffiliationType != null
                            && _CrossesSymmetricDirectionalBoundary(existingAffiliation.AffiliationType, suggestion.SuggestedAffiliationType.Value))
                        {
                            return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null,
                                "تغییر نوع وابستگی بین یک نوع متقارن (مثل هم‌عصر/متحد/رقیب/همراه) و یک نوع جهت‌دار (که در آن یک طرف زیردست/حامی/جانشین/... طرف دیگر است) ممکن نیست، چون جهت صحیح طرف اول و دوم برای نوع تازه معلوم نیست. لطفاً این وابستگی را حذف کرده و یک وابستگی تازه با نوع و جهت درست پیشنهاد دهید.");
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
                        if (suggestion.Action != PersonRelationSuggestionAction.Modify)
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

                // a "Modify" that changes nothing is pointless for the moderator (and is what a contributor
                // gets when they open the edit form and submit without touching anything) - reject it
                if (suggestion.Action == PersonRelationSuggestionAction.Modify)
                {
                    string Norm(string s) => (s ?? "").Trim();
                    bool unchanged = false;
                    if (existingRelation != null)
                    {
                        unchanged = suggestion.SuggestedRelationType == existingRelation.RelationType
                            && suggestion.SuggestedDegreeHint == existingRelation.DegreeHint
                            && Norm(suggestion.SuggestedNote) == Norm(existingRelation.Note);
                    }
                    else if (existingAffiliation != null)
                    {
                        unchanged = suggestion.SuggestedAffiliationType == existingAffiliation.AffiliationType
                            && Norm(suggestion.SuggestedNote) == Norm(existingAffiliation.Note);
                    }
                    if (unchanged)
                    {
                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null,
                            "در این پیشنهاد چیزی تغییر نکرده است. نوع، درجه یا یادداشت را تغییر دهید، یا اگر می‌خواهید مستندی (بیتی از شعر) برای این نسبت ثبت کنید گزینهٔ «افزودن مستند» را انتخاب کنید.");
                    }
                }

                if (suggestion.Kind == PersonRelationSuggestionKind.Family &&
                    (suggestion.Action == PersonRelationSuggestionAction.Add || suggestion.Action == PersonRelationSuggestionAction.Modify))
                {
                    var excludeRelationId = suggestion.Action == PersonRelationSuggestionAction.Modify ? suggestion.ExistingRelationId : null;
                    var validationScope = await _ResolveValidationScopeAsync(suggestion.EvidencePoemId, excludeRelationId);
                    var validationError = await _ValidateFamilyRelationAsync(suggestion.Person1Id, suggestion.Person2Id, suggestion.SuggestedRelationType, excludeRelationId, suggestion.ConfirmedExtraParent, validationScope);
                    if (validationError != null)
                    {
                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, validationError);
                    }
                }

                if (suggestion.Kind == PersonRelationSuggestionKind.Affiliation &&
                    (suggestion.Action == PersonRelationSuggestionAction.Add || suggestion.Action == PersonRelationSuggestionAction.Modify) &&
                    suggestion.SuggestedAffiliationType != null)
                {
                    var excludeAffiliationId = suggestion.Action == PersonRelationSuggestionAction.Modify ? suggestion.ExistingAffiliationId : null;
                    var duplicateAffiliation = await _GetDuplicateAffiliationAsync(suggestion.Person1Id, suggestion.Person2Id, suggestion.SuggestedAffiliationType.Value, excludeAffiliationId);
                    if (duplicateAffiliation != null)
                    {
                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null,
                            $"این وابستگی هم‌اکنون بین «{duplicateAffiliation.Person1?.Name}» و «{duplicateAffiliation.Person2?.Name}» ثبت شده است.");
                    }
                }

                if (suggestion.Kind == PersonRelationSuggestionKind.Family &&
                    (suggestion.Action == PersonRelationSuggestionAction.AddEvidence ||
                     (suggestion.Action == PersonRelationSuggestionAction.Add && suggestion.EvidencePoemId != null)))
                {
                    var evidenceError = await _ValidateAndFillEvidenceAsync(suggestion, suggestion.Action == PersonRelationSuggestionAction.AddEvidence ? existingRelation?.Id : null);
                    if (evidenceError != null)
                    {
                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, evidenceError);
                    }
                    if (suggestion.Action == PersonRelationSuggestionAction.AddEvidence && existingRelation != null && suggestion.EvidencePoemId != null)
                    {
                        var evidenceBook = await _GetMasterCatIdAsync(suggestion.EvidencePoemId.Value);
                        if (evidenceBook != null)
                        {
                            var bookError = await _ValidateRelationInBookAsync(existingRelation, evidenceBook.Value, suggestion.ConfirmedExtraParent);
                            if (bookError != null)
                            {
                                return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, bookError);
                            }
                        }
                    }
                }
                else if (suggestion.Action == PersonRelationSuggestionAction.RemoveEvidence)
                {
                    if (suggestion.ExistingEvidenceId == null)
                    {
                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "مستند مورد نظر برای حذف مشخص نشده است.");
                    }
                    var evidence = await _context.GanjoorPersonRelationEvidences.AsNoTracking()
                        .Where(e => e.Id == suggestion.ExistingEvidenceId.Value && e.RelationId == existingRelation.Id)
                        .SingleOrDefaultAsync();
                    if (evidence == null)
                    {
                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "مستند مورد نظر برای این نسبت پیدا نشد.");
                    }
                    // display snapshot only
                    suggestion.EvidencePoemId = evidence.PoemId;
                    suggestion.EvidenceCoupletIndex = evidence.CoupletIndex;
                    suggestion.EvidenceCoupletText = evidence.CoupletText;
                }
                else
                {
                    suggestion.EvidencePoemId = null;
                    suggestion.EvidenceCoupletIndex = null;
                    suggestion.EvidenceCoupletText = null;
                    suggestion.ExistingEvidenceId = null;
                }
                if (suggestion.Action != PersonRelationSuggestionAction.RemoveEvidence)
                {
                    suggestion.ExistingEvidenceId = null;
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
                    PersonRelationSuggestionAction.AddEvidence => "پیشنهاد مستند برای نسبت خویشاوندی",
                    PersonRelationSuggestionAction.RemoveEvidence => "پیشنهاد حذف مستند نسبت خویشاوندی",
                    _ => isAffiliation ? "پیشنهاد حذف وابستگی" : "پیشنهاد حذف نسبت خویشاوندی",
                };
                string edgeLabel = isAffiliation ? "وابستگی" : "نسبت خویشاوندی";
                string actionText = suggestion.Action switch
                {
                    PersonRelationSuggestionAction.Add => $"کاربری پیشنهاد افزودن {edgeLabel} جدید بین «{person1.Name}» و «{person2.Name}» را داده است.",
                    PersonRelationSuggestionAction.Modify => $"کاربری پیشنهاد ویرایش {edgeLabel} بین «{person1.Name}» و «{person2.Name}» را داده است.",
                    PersonRelationSuggestionAction.AddEvidence => $"کاربری پیشنهاد افزودن مستند (بیتی از شعر) برای {edgeLabel} بین «{person1.Name}» و «{person2.Name}» را داده است.",
                    PersonRelationSuggestionAction.RemoveEvidence => $"کاربری پیشنهاد حذف یکی از مستندهای {edgeLabel} بین «{person1.Name}» و «{person2.Name}» را داده است.",
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
                                {
                                    var addType = suggestion.SuggestedAffiliationType ?? PersonAffiliationType.Other;
                                    var duplicateAffiliation = await _GetDuplicateAffiliationAsync(suggestion.Person1Id, suggestion.Person2Id, addType, null);
                                    if (duplicateAffiliation != null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null,
                                            $"این وابستگی هم‌اکنون بین «{duplicateAffiliation.Person1?.Name}» و «{duplicateAffiliation.Person2?.Name}» ثبت شده است.");
                                    }
                                    _context.GanjoorPersonAffiliations.Add(new GanjoorPersonAffiliation()
                                    {
                                        Person1Id = suggestion.Person1Id,
                                        Person2Id = suggestion.Person2Id,
                                        AffiliationType = addType,
                                        Note = suggestion.SuggestedNote,
                                    });
                                    break;
                                }
                            case PersonRelationSuggestionAction.Modify:
                                {
                                    var existing = await _context.GanjoorPersonAffiliations.Where(a => a.Id == suggestion.ExistingAffiliationId.Value).SingleOrDefaultAsync();
                                    if (existing == null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "وابستگی مورد نظر دیگر وجود ندارد.");
                                    }
                                    var newAffiliationType = suggestion.SuggestedAffiliationType ?? existing.AffiliationType;
                                    if (_CrossesSymmetricDirectionalBoundary(existing.AffiliationType, newAffiliationType))
                                    {
                                        // Person1Id/Person2Id were fixed when the ORIGINAL (symmetric or
                                        // directional) type was created/approved, and a Modify suggestion
                                        // never lets the submitter re-pick which side is which (see the
                                        // "جهت ... قابل تغییر نیست" hint in SuggestPersonRelationEdit.cshtml)
                                        // - so crossing this boundary would silently keep the old Person1/
                                        // Person2 assignment under a type whose direction convention no
                                        // longer matches it
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null,
                                            "تغییر نوع وابستگی بین یک نوع متقارن (مثل هم‌عصر/متحد/رقیب/همراه) و یک نوع جهت‌دار (که در آن یک طرف زیردست/حامی/جانشین/... طرف دیگر است) ممکن نیست، چون جهت صحیح طرف اول و دوم برای نوع تازه معلوم نیست. لطفاً این وابستگی را حذف کرده و یک وابستگی تازه با نوع و جهت درست پیشنهاد دهید.");
                                    }
                                    var duplicateOnModify = await _GetDuplicateAffiliationAsync(existing.Person1Id, existing.Person2Id, newAffiliationType, existing.Id);
                                    if (duplicateOnModify != null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null,
                                            $"وابستگی دیگری از همین نوع هم‌اکنون بین «{duplicateOnModify.Person1?.Name}» و «{duplicateOnModify.Person2?.Name}» ثبت شده است.");
                                    }
                                    existing.AffiliationType = newAffiliationType;
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

                                        // ExistingAffiliationId is a Restrict FK, so every suggestion row still
                                        // pointing at this affiliation - this one included (it's the Remove
                                        // suggestion being approved right now), the ones just auto-rejected
                                        // above, and any older, already-reviewed suggestion from this
                                        // affiliation's own history (e.g. a past approved Modify) - would block
                                        // the delete below unless detached first. The rows themselves are kept
                                        // for history; only the now-dangling reference is cleared.
                                        var allReferencing = await _context.GanjoorPersonRelationEditSuggestions
                                            .Where(s => s.ExistingAffiliationId == existing.Id)
                                            .ToListAsync();
                                        foreach (var r in allReferencing)
                                        {
                                            r.ExistingAffiliationId = null;
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
                                {
                                    // re-validated here (not just at submission time in
                                    // SuggestPersonRelationEditAsync) in case another suggestion
                                    // touching the same people/relations was approved in between
                                    var addScope = await _ResolveValidationScopeAsync(suggestion.EvidencePoemId, null);
                                    var validationError = await _ValidateFamilyRelationAsync(suggestion.Person1Id, suggestion.Person2Id, suggestion.SuggestedRelationType, null, suggestion.ConfirmedExtraParent, addScope);
                                    if (validationError != null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, validationError);
                                    }
                                    var newRelation = new GanjoorPersonRelation()
                                    {
                                        Person1Id = suggestion.Person1Id,
                                        Person2Id = suggestion.Person2Id,
                                        RelationType = suggestion.SuggestedRelationType,
                                        DegreeHint = suggestion.SuggestedDegreeHint,
                                        Note = suggestion.SuggestedNote,
                                    };
                                    _context.GanjoorPersonRelations.Add(newRelation);

                                    // optional evidence submitted together with the new relation
                                    if (suggestion.EvidencePoemId != null && suggestion.EvidenceCoupletIndex != null)
                                    {
                                        var masterCatId = await _GetMasterCatIdAsync(suggestion.EvidencePoemId.Value);
                                        if (masterCatId != null)
                                        {
                                            _context.GanjoorPersonRelationEvidences.Add(new GanjoorPersonRelationEvidence()
                                            {
                                                Relation = newRelation,
                                                PoemId = suggestion.EvidencePoemId.Value,
                                                CoupletIndex = suggestion.EvidenceCoupletIndex.Value,
                                                CoupletText = suggestion.EvidenceCoupletText,
                                                MasterCatId = masterCatId.Value,
                                                Inferred = false,
                                                AddedByUserId = suggestion.UserId,
                                                DateAdded = DateTime.Now,
                                            });
                                        }
                                    }
                                    break;
                                }
                            case PersonRelationSuggestionAction.Modify:
                                {
                                    var existing = await _context.GanjoorPersonRelations.Where(r => r.Id == suggestion.ExistingRelationId.Value).SingleOrDefaultAsync();
                                    if (existing == null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "نسبت مورد نظر دیگر وجود ندارد.");
                                    }
                                    var modifyScope = await _ResolveValidationScopeAsync(suggestion.EvidencePoemId, existing.Id);
                                    var validationError = await _ValidateFamilyRelationAsync(suggestion.Person1Id, suggestion.Person2Id, suggestion.SuggestedRelationType, existing.Id, suggestion.ConfirmedExtraParent, modifyScope);
                                    if (validationError != null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, validationError);
                                    }
                                    existing.RelationType = suggestion.SuggestedRelationType;
                                    existing.DegreeHint = suggestion.SuggestedDegreeHint;
                                    existing.Note = suggestion.SuggestedNote;
                                    break;
                                }
                            case PersonRelationSuggestionAction.AddEvidence:
                                {
                                    if (suggestion.ExistingRelationId == null || suggestion.EvidencePoemId == null || suggestion.EvidenceCoupletIndex == null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "اطلاعات مستند ناقص است.");
                                    }
                                    var existing = await _context.GanjoorPersonRelations.Where(r => r.Id == suggestion.ExistingRelationId.Value).SingleOrDefaultAsync();
                                    if (existing == null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "نسبت مورد نظر دیگر وجود ندارد.");
                                    }
                                    var poemId = suggestion.EvidencePoemId.Value;
                                    var coupletIndex = suggestion.EvidenceCoupletIndex.Value;
                                    if (await _context.GanjoorPersonRelationEvidences.AnyAsync(e => e.RelationId == existing.Id && e.PoemId == poemId && e.CoupletIndex == coupletIndex))
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "این بیت هم‌اکنون به عنوان مستند این نسبت ثبت شده است.");
                                    }
                                    var masterCatId = await _GetMasterCatIdAsync(poemId);
                                    if (masterCatId == null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, "دستهٔ اصلی این شعر مشخص نشد.");
                                    }
                                    var bookError = await _ValidateRelationInBookAsync(existing, masterCatId.Value, suggestion.ConfirmedExtraParent);
                                    if (bookError != null)
                                    {
                                        return new RServiceResult<GanjoorPersonRelationEditSuggestion>(null, bookError);
                                    }
                                    _context.GanjoorPersonRelationEvidences.Add(new GanjoorPersonRelationEvidence()
                                    {
                                        RelationId = existing.Id,
                                        PoemId = poemId,
                                        CoupletIndex = coupletIndex,
                                        CoupletText = suggestion.EvidenceCoupletText,
                                        MasterCatId = masterCatId.Value,
                                        Inferred = false,
                                        AddedByUserId = suggestion.UserId,
                                        DateAdded = DateTime.Now,
                                    });
                                    break;
                                }
                            case PersonRelationSuggestionAction.RemoveEvidence:
                                {
                                    var evidence = suggestion.ExistingEvidenceId == null ? null :
                                        await _context.GanjoorPersonRelationEvidences.Where(e => e.Id == suggestion.ExistingEvidenceId.Value).SingleOrDefaultAsync();
                                    if (evidence != null)
                                    {
                                        _context.GanjoorPersonRelationEvidences.Remove(evidence);
                                    }
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

                                        // ExistingRelationId is a Restrict FK, so every suggestion row still
                                        // pointing at this relation - this one included (it's the Remove
                                        // suggestion being approved right now), the ones just auto-rejected
                                        // above, and any older, already-reviewed suggestion from this
                                        // relation's own history (e.g. a past approved Modify, or simply a
                                        // relation that has had more than one suggestion against it over
                                        // time) - would block the delete below unless detached first. The
                                        // rows themselves are kept for history; only the now-dangling
                                        // reference is cleared.
                                        var allReferencing = await _context.GanjoorPersonRelationEditSuggestions
                                            .Where(s => s.ExistingRelationId == existing.Id)
                                            .ToListAsync();
                                        foreach (var r in allReferencing)
                                        {
                                            r.ExistingRelationId = null;
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
                    PersonRelationSuggestionAction.AddEvidence => "افزودن مستند نسبت خویشاوندی",
                    PersonRelationSuggestionAction.RemoveEvidence => "حذف مستند نسبت خویشاوندی",
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
                Importance = (int)p.Importance,
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
