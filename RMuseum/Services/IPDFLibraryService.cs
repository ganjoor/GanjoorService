using RMuseum.Models.PDFLibrary.ViewModels;
using RSecurityBackend.Models.Generic;

namespace RMuseum.Services
{
    /// <summary>
    /// PDF Library Services
    /// </summary>
    public interface IPDFLibraryService
    {
        /// <summary>
        /// start importing local pdf file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        RServiceResult<bool> StartImportingLocalPDF(NewPDFBookViewModel model);
    }
}
