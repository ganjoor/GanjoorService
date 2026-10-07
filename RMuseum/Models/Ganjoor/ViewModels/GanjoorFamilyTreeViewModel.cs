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

        /// <summary>
        /// the book (master category) this view was computed for, or null for the unfiltered
        /// whole-tree view. Edges then carry a State, see GanjoorFamilyTreeEdge.State.
        /// </summary>
        public int? MasterCatId { get; set; }

        /// <summary>
        /// every book that has at least one human-attached evidence row for a relation in this tree
        /// (what the book picker lists)
        /// </summary>
        public List<GanjoorFamilyTreeBook> Books { get; set; }
    }

    /// <summary>
    /// a master category (book) that attests relations of a tree
    /// </summary>
    public class GanjoorFamilyTreeBook
    {
        /// <summary>
        /// master category id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// category title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// number of this tree's relations attested in this book
        /// </summary>
        public int RelationCount { get; set; }
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

        /// <summary>
        /// the relation row's id
        /// </summary>
        public int RelationId { get; set; }

        /// <summary>
        /// only set when the tree was requested for a book (MasterCatId): "attested" (human evidence
        /// in this book), "otherBook" (evidence only in other books, nothing here contradicts it),
        /// "unattested" (no human evidence anywhere), or "contradicted" (a parent link that conflicts
        /// with a parent link attested in this book - same gender as an attested parent of the same
        /// child, or both parent slots already taken by attested parents). Null in the unfiltered view.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// titles of the books that attest this relation (human evidence only), for tooltips
        /// </summary>
        public List<string> AttestedIn { get; set; }
    }
}
