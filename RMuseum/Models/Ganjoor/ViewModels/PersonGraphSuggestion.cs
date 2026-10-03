using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// deserialized shape of GanjoorPoemGeoDateTagCorrection.SuggestedPersonGraphJson - a brand new
    /// person (optionally linked to other people, new or existing) suggested in one go alongside a
    /// poem's geo/date/person tag. See that field's doc comment for the full JSON shape.
    /// </summary>
    public class PersonGraphSuggestion
    {
        /// <summary>
        /// the node that ends up assigned to the tag's PersonId once approved
        /// </summary>
        public PersonGraphNode Person { get; set; }

        /// <summary>
        /// other people this submission also introduces or links to, referenced from Relations by
        /// their LocalKey
        /// </summary>
        public List<PersonGraphNode> RelatedPeople { get; set; }

        /// <summary>
        /// kinship and/or non-family edges between any of Person/RelatedPeople's local keys
        /// </summary>
        public List<PersonGraphRelationEntry> Relations { get; set; }
    }

    /// <summary>
    /// one person node in a PersonGraphSuggestion - either a link to an existing, already approved
    /// GanjoorRelatedPerson (ExistingPersonId set, the rest ignored) or a brand new person to create
    /// (ExistingPersonId null, the rest describing them)
    /// </summary>
    public class PersonGraphNode
    {
        /// <summary>
        /// key used only to resolve Relations entries within this same submission - never stored
        /// beyond approval time
        /// </summary>
        public string LocalKey { get; set; }

        /// <summary>
        /// set to link to an existing, already approved person instead of creating a new one - when
        /// set, every other field on this node is ignored
        /// </summary>
        public int? ExistingPersonId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string WikiUrl { get; set; }
        public int? BirthYearInLHijri { get; set; }
        public int? DeathYearInLHijri { get; set; }
        public bool ValidBirthDate { get; set; }
        public bool ValidDeathDate { get; set; }
        public int? BirthLocationId { get; set; }
        public int? DeathLocationId { get; set; }

        /// <summary>
        /// see GanjoorRelatedPerson.FamilyTreeCaption - only meaningful for whichever node is meant
        /// to be a tree's named root
        /// </summary>
        public string FamilyTreeCaption { get; set; }

        /// <summary>
        /// name of a PersonImportance value ("Normal", "Important", "VeryImportant") - same
        /// string-enum convention as PersonGraphRelationEntry.RelationType/AffiliationType below.
        /// Null/empty is treated as Normal.
        /// </summary>
        public string Importance { get; set; }
    }

    /// <summary>
    /// one edge in a PersonGraphSuggestion, between two nodes referenced by their LocalKey (Person1/
    /// Person2 - matching PersonGraphNode.LocalKey, not a real database id yet). Kind picks which
    /// enum RelationType/AffiliationType is parsed against and which live table the edge is
    /// materialized into on approval.
    /// </summary>
    public class PersonGraphRelationEntry
    {
        /// <summary>
        /// "family" (default, materialized as GanjoorPersonRelation - RelationType against
        /// PersonRelationType) or "affiliation" (materialized as GanjoorPersonAffiliation -
        /// AffiliationType against PersonAffiliationType)
        /// </summary>
        public string Kind { get; set; }

        public string Person1 { get; set; }
        public string Person2 { get; set; }

        /// <summary>
        /// parsed against PersonRelationType when Kind is "family"
        /// </summary>
        public string RelationType { get; set; }

        /// <summary>
        /// parsed against PersonAffiliationType when Kind is "affiliation"
        /// </summary>
        public string AffiliationType { get; set; }

        /// <summary>
        /// only meaningful for a family relation of type Ancestor - see GanjoorPersonRelation.DegreeHint
        /// </summary>
        public int? DegreeHint { get; set; }

        public string Note { get; set; }
    }
}
