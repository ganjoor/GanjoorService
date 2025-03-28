using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Generic.ViewModels;

namespace GanjooRazor.Pages
{
    public class _GroupedByDateViewPartialModel : PageModel
    {
        public string DataType { get; set; }
        public GroupedByDateViewModel[] Days { get; set; }

        public GroupedByUserViewModel[] Users { get; set; }

        public SummedUpViewModel Summary { get; set; }

        public _GroupedByDateViewTablePartialModel TableModel
        {
            get
            {
                return new _GroupedByDateViewTablePartialModel()
                {
                    Days = Days,
                    Users = Users,
                };
            }
        }
    }
}
