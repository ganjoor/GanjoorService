using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Areas.User.Pages
{
    public class DeleteAccountModel : PageModel
    {
        [BindProperty]
        public SelfDeleteViewModel DeleteViewModel { get; set; }
        public void OnGet()
        {
        }
    }
}
