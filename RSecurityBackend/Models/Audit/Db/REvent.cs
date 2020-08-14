using System;

namespace RSecurityBackend.Models.Audit.Db
{
    /// <summary>
    /// Audit Event
    /// </summary>
    public class REvent
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Event Type
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Last Updated Date
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Duration
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// User Name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// IP Address
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Response Code
        /// </summary>
        public int ResponseStatusCode { get; set; }

        /// <summary>
        /// Requset Url
        /// </summary>
        public string RequestUrl { get; set; }

        /// <summary>
        /// Jason Data
        /// </summary>
        public string JsonData { get; set; }

    }
}
