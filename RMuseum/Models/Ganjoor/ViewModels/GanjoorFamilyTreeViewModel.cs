using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// the whole connected kinship component reachable from one person, for the interactive chart at
    /// /FamilyTree/{id} (client draws the layout; this just hands over every person and every edge in
    /// that connected subgraph - see GanjoorRelatedPersonService.GetFamilyTreeAsync). Deliberately not
    /// scoped to strict descendants of RootId: an ancestor several rows up, or a spouse's own side of
    /// the family, is part of the same connected component and is included too, exactly like
    /// akhakhafrasiyab.ir's chart isn't confined to the clicked node's own subtree.
    /// </summary>
    public class GanjoorFamilyTreeViewModel
    {
        /// <summary>
        /// the person id the chart was requested for (not necessarily the topmost node once rendered -
        /// the client walks Parent/Ancestor edges to find the actual top of each lineage in Persons)
        /// </summary>
        public int RootId { get; set; }

        /// <summary>
        /// every person in the connected component, including RootId itself
        /// </summary>
        public List<GanjoorRelatedPerson> Persons { get; set; }

        /// <summary>
        /// every kinship edge touching any person in Persons
        /// </summary>
        public List<GanjoorFamilyTreeEdge> Relations { get; set; }
    }

    /// <summary>
    /// a GanjoorPersonRelation row stripped down to what the client-side layout needs (no
    /// Person1/Person2 navigation - Persons above already carries full person data)
    /// </summary>
    public class GanjoorFamilyTreeEdge
    {
        public int Person1Id { get; set; }
        public int Person2Id { get; set; }
        public PersonRelationType RelationType { get; set; }
        public int? DegreeHint { get; set; }
    }
}
