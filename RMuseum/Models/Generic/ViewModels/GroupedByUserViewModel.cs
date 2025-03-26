using System;

namespace RMuseum.Models.Generic.ViewModels
{
    /// <summary>
    /// grouped by user view model
    /// </summary>
    public class GroupedByUserViewModel
    {
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
