using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// everything the "who could have been alive at the same time" view needs for one person: the
    /// people connected to them through kinship or through an affiliation that implies overlapping
    /// lifetimes, and those ties themselves. The timing reasoning is done client-side (contemporaries.js)
    /// because it depends on assumptions the viewer can change (maximum lifespan, minimum parent age).
    /// </summary>
    public class GanjoorContemporaryGraphViewModel
    {
        /// <summary>
        /// the person the view was opened for
        /// </summary>
        public int RootId { get; set; }

        /// <summary>
        /// every person reachable through the ties below
        /// </summary>
        public List<GanjoorRelatedPerson> Persons { get; set; }

        /// <summary>
        /// kinship ties (GanjoorPersonRelation)
        /// </summary>
        public List<GanjoorContemporaryKinTie> Kin { get; set; }

        /// <summary>
        /// affiliation ties that imply the two were alive at the same time (see
        /// GanjoorRelatedPersonService.OverlapAffiliationTypes)
        /// </summary>
        public List<GanjoorContemporaryAffiliationTie> Affiliations { get; set; }
    }

    /// <summary>
    /// one kinship tie
    /// </summary>
    public class GanjoorContemporaryKinTie
    {
        /// <summary>
        /// Person1 (parent/ancestor side for Parent and Ancestor)
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// Person2
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// PersonRelationType numeric value: 0 Parent, 1 Sibling, 2 Spouse, 3 Ancestor
        /// </summary>
        public int RelationType { get; set; }

        /// <summary>
        /// generations between Person1 and Person2 for Ancestor, when known
        /// </summary>
        public int? DegreeHint { get; set; }
    }

    /// <summary>
    /// one affiliation tie
    /// </summary>
    public class GanjoorContemporaryAffiliationTie
    {
        /// <summary>
        /// Person1 (see PersonAffiliationType for direction - e.g. Person1 killed Person2)
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// Person2
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// PersonAffiliationType numeric value
        /// </summary>
        public int AffiliationType { get; set; }
    }
}
