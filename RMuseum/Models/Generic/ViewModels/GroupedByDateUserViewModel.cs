using System;

namespace RMuseum.Models.Generic.ViewModels
{
    /// <summary>
    /// grouped by date / user
    /// </summary>
    public class GroupedByDateUserViewModel
    {
        /// <summary>
        /// date (day date or month or ...)
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// number of clicks
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// user name
        /// </summary>
        public string UserName { get; set; }
    }
}
