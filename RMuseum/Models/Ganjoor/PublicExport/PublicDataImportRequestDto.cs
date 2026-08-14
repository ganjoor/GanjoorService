namespace RMuseum.Models.Ganjoor.PublicExport
{
    /// <summary>
    /// where to read the exported public data tree from, for StartImportFromPublicDataRepo
    /// </summary>
    public class PublicDataImportRequestDto
    {
        /// <summary>
        /// true: fetch over HTTP, Location is a base URL (e.g. jsDelivr).
        /// false: read from a local folder, Location is a filesystem path (e.g. a `git clone`).
        /// </summary>
        public bool UseHttp { get; set; }

        /// <summary>
        /// base URL (UseHttp=true) or local folder path (UseHttp=false) of the exported data tree
        /// </summary>
        public string Location { get; set; }
    }
}
