using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GanjooRazor.Pages
{
    public class _BNumPartialModel : PageModel
    {
        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }

       
    }
}
