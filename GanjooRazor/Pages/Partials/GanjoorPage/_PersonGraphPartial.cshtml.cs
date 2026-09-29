using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// model for the on-demand "شخصیت‌ها" (characters) tab shown on a poet/category page - renders
    /// the same force-directed graph as PeopleGraph.cshtml/peoplegraph.js, but scoped to one
    /// work/category (see GanjoorRelatedPersonService.GetCatPersonGraphAsync) instead of the whole
    /// site. Element ids are prefixed "pg-cat-" so this partial can sit on the same page as (and not
    /// collide with) any other graph instance.
    /// </summary>
    public class _PersonGraphPartialModel : PageModel
    {
        /// <summary>
        /// category id this graph is scoped to
        /// </summary>
        public int CatId { get; set; }

        /// <summary>
        /// the graph data returned by GET api/ganjoor/cat/{id}/persongraph
        /// </summary>
        public GanjoorPersonGraphViewModel Graph { get; set; }

        /// <summary>
        /// Graph, re-serialized with an explicit camelCase contract (same convention
        /// PeopleGraphModel.GraphDataJson/FamilyTree's TreeDataJson use), so peoplegraph.js has a
        /// predictable shape to parse regardless of which page embeds it
        /// </summary>
        public string GraphDataJson =>
            JsonConvert.SerializeObject(
                Graph,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
            );

        /// <summary>
        /// true if the graph has at least one node pulled in only as a one-hop relative/affiliate
        /// (never itself tagged in this work's verses) - used to decide whether to show the
        /// explanatory legend note about dashed/secondary nodes
        /// </summary>
        public bool HasSecondaryNodes
        {
            get
            {
                if (Graph?.Nodes == null)
                    return false;
                foreach (var node in Graph.Nodes)
                {
                    if (!node.DirectlyTagged)
                        return true;
                }
                return false;
            }
        }
    }
}
