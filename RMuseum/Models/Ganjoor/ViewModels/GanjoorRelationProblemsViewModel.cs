using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// a book (master category) that attests a relation or affiliation
    /// </summary>
    public class GanjoorRelationProblemBook
    {
        /// <summary>
        /// category id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// category title, when it could be found
        /// </summary>
        public string Title { get; set; }
    }

    /// <summary>
    /// a person named in a problem
    /// </summary>
    public class GanjoorRelationProblemPerson
    {
        /// <summary>
        /// person id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// one relation (or affiliation) row taking part in a problem
    /// </summary>
    public class GanjoorRelationProblemEdge
    {
        /// <summary>
        /// relation or affiliation id
        /// </summary>
        public int RelationId { get; set; }

        /// <summary>
        /// true for a non-family affiliation row
        /// </summary>
        public bool IsAffiliation { get; set; }

        /// <summary>
        /// first person (the parent/ancestor for directional kinship types)
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// first person's name
        /// </summary>
        public string Person1Name { get; set; }

        /// <summary>
        /// second person (the child/descendant for directional kinship types)
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// second person's name
        /// </summary>
        public string Person2Name { get; set; }

        /// <summary>
        /// kinship type (not an affiliation)
        /// </summary>
        public PersonRelationType? RelationType { get; set; }

        /// <summary>
        /// affiliation type (not a kinship relation)
        /// </summary>
        public PersonAffiliationType? AffiliationType { get; set; }

        /// <summary>
        /// exact generations for an Ancestor relation, when known
        /// </summary>
        public int? DegreeHint { get; set; }

        /// <summary>
        /// free-text note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// number of human-attached evidence couplets
        /// </summary>
        public int EvidenceCount { get; set; }

        /// <summary>
        /// books attesting it
        /// </summary>
        public List<GanjoorRelationProblemBook> Books { get; set; } = new List<GanjoorRelationProblemBook>();
    }

    /// <summary>
    /// a loop in the Parent/Ancestor graph (a strongly connected group of people): the family tree
    /// chart cannot lay such a group out as a tree
    /// </summary>
    public class GanjoorRelationCycleProblem
    {
        /// <summary>
        /// people on the loop(s)
        /// </summary>
        public List<GanjoorRelationProblemPerson> People { get; set; } = new List<GanjoorRelationProblemPerson>();

        /// <summary>
        /// all Parent/Ancestor relations between those people
        /// </summary>
        public List<GanjoorRelationProblemEdge> Edges { get; set; } = new List<GanjoorRelationProblemEdge>();

        /// <summary>
        /// the relation that is the best first candidate for removal or correction: an Ancestor one
        /// (a looser claim than Parent) with the fewest evidence couplets. Removing it may not break every
        /// loop of a large group.
        /// </summary>
        public int? SuggestedRelationId { get; set; }
    }

    /// <summary>
    /// what is wrong with a person's recorded parents
    /// </summary>
    public enum RelationParentProblemKind
    {
        /// <summary>
        /// more than two different parents attested by the same book
        /// </summary>
        TooManyParentsInBook = 0,

        /// <summary>
        /// two parents of the same (known) gender attested by the same book
        /// </summary>
        SameGenderParentsInBook = 1,

        /// <summary>
        /// more than two parents overall, no single book has more than two: legitimate when the books
        /// disagree, worth a look when they do not
        /// </summary>
        ManyParentsAcrossBooks = 2,
    }

    /// <summary>
    /// a person whose recorded parents look wrong
    /// </summary>
    public class GanjoorRelationParentsProblem
    {
        /// <summary>
        /// what is wrong
        /// </summary>
        public RelationParentProblemKind Kind { get; set; }

        /// <summary>
        /// the child
        /// </summary>
        public GanjoorRelationProblemPerson Child { get; set; }

        /// <summary>
        /// the book the problem is within (null for ManyParentsAcrossBooks)
        /// </summary>
        public GanjoorRelationProblemBook Book { get; set; }

        /// <summary>
        /// the Parent relations involved
        /// </summary>
        public List<GanjoorRelationProblemEdge> ParentEdges { get; set; } = new List<GanjoorRelationProblemEdge>();
    }

    /// <summary>
    /// the same relation recorded more than once
    /// </summary>
    public class GanjoorRelationDuplicateProblem
    {
        /// <summary>
        /// the identical rows
        /// </summary>
        public List<GanjoorRelationProblemEdge> Edges { get; set; } = new List<GanjoorRelationProblemEdge>();
    }

    /// <summary>
    /// kinds of incompatible relations between the same two people
    /// </summary>
    public enum RelationPairConflictKind
    {
        /// <summary>
        /// each is recorded as a parent of the other
        /// </summary>
        ParentInBothDirections = 0,

        /// <summary>
        /// recorded as parent/child and as siblings
        /// </summary>
        ParentAndSibling = 1,

        /// <summary>
        /// recorded as parent/child and as spouses
        /// </summary>
        ParentAndSpouse = 2,

        /// <summary>
        /// recorded as siblings and as spouses
        /// </summary>
        SiblingAndSpouse = 3,
    }

    /// <summary>
    /// two people linked by relations that cannot all be true
    /// </summary>
    public class GanjoorRelationPairConflictProblem
    {
        /// <summary>
        /// what is wrong
        /// </summary>
        public RelationPairConflictKind Kind { get; set; }

        /// <summary>
        /// the relations involved
        /// </summary>
        public List<GanjoorRelationProblemEdge> Edges { get; set; } = new List<GanjoorRelationProblemEdge>();
    }

    /// <summary>
    /// everything the relation checker found
    /// </summary>
    public class GanjoorRelationProblemsViewModel
    {
        /// <summary>
        /// number of kinship relations checked
        /// </summary>
        public int RelationsChecked { get; set; }

        /// <summary>
        /// number of affiliations checked
        /// </summary>
        public int AffiliationsChecked { get; set; }

        /// <summary>
        /// loops in the Parent/Ancestor graph
        /// </summary>
        public List<GanjoorRelationCycleProblem> Cycles { get; set; } = new List<GanjoorRelationCycleProblem>();

        /// <summary>
        /// suspicious parents
        /// </summary>
        public List<GanjoorRelationParentsProblem> ParentProblems { get; set; } = new List<GanjoorRelationParentsProblem>();

        /// <summary>
        /// identical rows
        /// </summary>
        public List<GanjoorRelationDuplicateProblem> Duplicates { get; set; } = new List<GanjoorRelationDuplicateProblem>();

        /// <summary>
        /// incompatible relations between the same two people
        /// </summary>
        public List<GanjoorRelationPairConflictProblem> PairConflicts { get; set; } = new List<GanjoorRelationPairConflictProblem>();

        /// <summary>
        /// rows relating a person to themselves
        /// </summary>
        public List<GanjoorRelationProblemEdge> SelfRelations { get; set; } = new List<GanjoorRelationProblemEdge>();
    }
}
