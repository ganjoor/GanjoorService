using Microsoft.AspNetCore.Mvc;
using RMuseum.Services;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/pdf")]
    public class PDFLibraryController : Controller
    {
        /// <summary>
        /// PDF Service
        /// </summary>
        protected readonly IPDFLibraryService _pdfService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="pdfService"></param>
        public PDFLibraryController(IPDFLibraryService pdfService)
        {
            _pdfService = pdfService;
        }
    }
}
