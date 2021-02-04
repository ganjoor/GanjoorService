using Microsoft.AspNetCore.Http;


namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// new artifact view model
    /// </summary>
    internal class NewArtifact
    {

        public string Name { get; set; }

        public string Description { get; set; }

        public string SrcUrl { get; set; }

        public string PicTitle { get; set; }

        public string PicDescription { get; set; }

        public IFormFile File { get; set; }

        public string PicSrcUrl { get; set; }
        
    }
}
