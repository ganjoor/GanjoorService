using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Generic.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace GanjooRazor.Pages
{
    public class _GroupedByDateViewTablePartialModel : PageModel
    {
        public string DataType { get; set; }
        public GroupedByDateViewModel[] Days { get; set; }
        public PaginationMetadata DaysPagination { get; set; }
        public GroupedByUserViewModel[] Users { get; set; }
        public PaginationMetadata UsersPagination { get; set; }


        

    }
}
