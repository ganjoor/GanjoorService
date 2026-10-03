using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// the whole known network of people - every GanjoorRelatedPerson that has at least one kinship
    /// edge or non-family tie, plus every one of those edges/ties - served by GET api/people/graph
    /// for the force-directed "ontology" explorer opened via the PeopleExplorer.open() modal
    /// (formerly the standalone page /PeopleGraph), the nowruzgan-style counterpart of
    /// the strict-tree /FamilyTree/{id} view. Loaded whole (like GetFamilyTreeAsync does for a single
    /// component) since the graph as a whole is small - a few hundred rows at most.
    /// </summary>
    public class GanjoorPersonGraphViewModel
    {
        /// <summary>
        /// every person that appears in at least one edge below
        /// </summary>
        public List<GanjoorPersonGraphNode> Nodes { get; set; }

        /// <summary>
        /// every kinship edge (GanjoorPersonRelation) and non-family tie (GanjoorPersonAffiliation),
        /// merged into one flat list - see GanjoorPersonGraphEdge.Category to tell them apart
        /// </summary>
        public List<GanjoorPersonGraphEdge> Edges { get; set; }
    }

    /// <summary>
    /// one node of the people graph
    /// </summary>
    public class GanjoorPersonGraphNode
    {
        /// <summary>
        /// GanjoorRelatedPerson.Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// GanjoorRelatedPerson.Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// true if this person captions a family tree (GanjoorRelatedPerson.FamilyTreeCaption not
        /// empty) - used client-side to draw such nodes slightly larger/more prominent, and to link
        /// to their /FamilyTree/{id} strict-tree view
        /// </summary>
        public bool HasFamilyTree { get; set; }

        /// <summary>
        /// true unless this node was pulled in only because it's one hop away (a relative or
        /// affiliate) from a person actually tagged in a work's verses - see
        /// GetCatPersonGraphAsync. Always true for the whole-site graph (GetPersonGraphAsync),
        /// where there's no "work" to be tagged within. Client-side, a false value draws the node
        /// as secondary (e.g. dashed/dimmer) since it's context the reader wasn't shown directly.
        /// </summary>
        public bool DirectlyTagged { get; set; } = true;

        /// <summary>
        /// numeric value of PersonImportance (0=Normal, 1=Important, 2=VeryImportant) - used
        /// client-side to size this node larger or smaller, see peoplegraph.js's nodeRadius()
        /// </summary>
        public int Importance { get; set; }
    }

    /// <summary>
    /// one edge of the people graph - either a kinship edge or a non-family tie, flattened to a
    /// common shape so the client can draw/list them uniformly
    /// </summary>
    public class GanjoorPersonGraphEdge
    {
        /// <summary>
        /// first person - directional meaning (if any) depends on Category/TypeValue, same as the
        /// underlying GanjoorPersonRelation/GanjoorPersonAffiliation row
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// first person's name, denormalized here so the client doesn't need a second lookup
        /// </summary>
        public string Person1Name { get; set; }

        /// <summary>
        /// second person
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// second person's name
        /// </summary>
        public string Person2Name { get; set; }

        /// <summary>
        /// "Relation" for a GanjoorPersonRelation edge, "Affiliation" for a GanjoorPersonAffiliation
        /// one - tells the client which enum TypeValue is a numeric value of
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// the numeric value of PersonRelationType (when Category is "Relation") or
        /// PersonAffiliationType (when Category is "Affiliation")
        /// </summary>
        public int TypeValue { get; set; }

        /// <summary>
        /// see GanjoorPersonRelation.DegreeHint - null for an Affiliation edge
        /// </summary>
        public int? DegreeHint { get; set; }

        /// <summary>
        /// free-text note
        /// </summary>
        public string Note { get; set; }
    }
}
