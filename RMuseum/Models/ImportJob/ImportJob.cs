using RMuseum.Models.Artifact;
using System;

namespace RMuseum.Models.ImportJob
{
    /// <summary>
    /// Import Job
    /// </summary>
    public class ImportJob
    {
        public Guid Id { get; set; }

        public JobType JobType { get; set; }

        public string ResourceNumber { get; set; }

        public string FriendlyUrl { get; set; }        

        public string SrcUrl { get; set; }        

        public DateTime QueueTime { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public ImportJobStatus Status { get; set; }

        public decimal ProgressPercent { get; set; }

        public string Exception { get; set; }

        public virtual RArtifactMasterRecord Artifact { get; set; }

        public Guid? ArtifactId { get; set; }

        public string SrcContent { get; set; }
    }
}
