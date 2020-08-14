using RMuseum.Models.Artifact;
using RMuseum.Models.Artifact.ViewModels;
using System;

namespace RMuseum.Models.Bookmark.ViewModels
{
    public class RUserBookmarkViewModel
    {
        public RUserBookmarkViewModel(RUserBookmark src)
        {
            Id = src.Id;
            RAppUserId = src.RAppUserId;
            RArtifactMasterRecord = src.RArtifactMasterRecord;
            RArtifactItemRecord = null;//this should be filled by an external call              
            DateTime = src.DateTime;
            RBookmarkType = src.RBookmarkType;
            Note = src.Note;
        }

        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid RAppUserId { get; set; }
  
        /// <summary>
        /// Master Record
        /// </summary>
        public virtual RArtifactMasterRecord RArtifactMasterRecord { get; set; }

  
        /// <summary>
        /// Item Record
        /// </summary>
        public virtual RArtifactItemRecordViewModel RArtifactItemRecord { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public RBookmarkType RBookmarkType { get; set; }

        /// <summary>
        /// User Note
        /// </summary>
        public string Note { get; set; }
    }
}
