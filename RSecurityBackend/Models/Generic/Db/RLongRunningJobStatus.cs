using System;

namespace RSecurityBackend.Models.Generic.Db
{
    /// <summary>
    /// Long Running Job Status
    /// </summary>
    public class RLongRunningJobStatus
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// progress value (percent of custom value)
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Start Time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End Time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Current Step
        /// </summary>
        public string Step { get; set; }

        /// <summary>
        /// Exception
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// finished
        /// </summary>
        public bool Succeeded { get; set; }
    }
}
