using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Globalization;

namespace RMuseum.Models.Note.ViewModels
{
    /// <summary>
    /// Safe User Note View Model
    /// </summary>
    public class RUserNoteViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="src"></param>
        /// <param name="user"></param>
        public RUserNoteViewModel(RUserNote src, PublicRAppUser user)
        {
            Id = src.Id;
            RAppUserId = src.RAppUserId;
            UserName = (user.FirstName + " " + user.SureName);
            RUserImageId = user.RImageId;           
            Modified = src.Modified;            
            NoteType = src.NoteType;
            HtmlContent = src.HtmlContent;
            ReferenceNoteId = src.ReferenceNoteId;
            Status = src.Status;
            Notes = new RUserNoteViewModel[] { };


            PersianCalendar pc = new PersianCalendar();

            DateTime = $"{pc.GetYear(src.DateTime)}/{pc.GetMonth(src.DateTime)}/{pc.GetDayOfMonth(src.DateTime)}&nbsp;{pc.GetHour(src.DateTime)}:{pc.GetMinute(src.DateTime)}";
            LastModified = $"{pc.GetYear(src.LastModified)}/{pc.GetMonth(src.LastModified)}/{pc.GetDayOfMonth(src.LastModified)}&nbsp;{pc.GetHour(src.LastModified)}:{pc.GetMinute(src.LastModified)}";

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
        /// User Name (FirstName + SureName)
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// user image id
        /// </summary>
        public Guid? RUserImageId { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public string DateTime { get; set; }

        /// <summary>
        /// Is Updated by User
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// Last Modified
        /// </summary>
        public string LastModified { get; set; }

        /// <summary>
        /// Private / Public
        /// </summary>
        public RNoteType NoteType { get; set; }

        /// <summary>
        /// content
        /// </summary>
        public string HtmlContent { get; set; }

              /// <summary>
        /// Reference Note Id
        /// </summary>
        public Guid? ReferenceNoteId { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public PublishStatus Status { get; set; }

        /// <summary>
        /// Related Entity (Artifact/Item) Name
        /// </summary>
        /// <remarks>
        /// It is set only in cummulative api calls
        /// </remarks>
        public string RelatedEntityName { get; set; }

        /// <summary>
        /// Realated entity Image Id 
        /// </summary>
        /// <remarks>
        /// It is set only in cummulative api calls
        /// </remarks>
        public Guid RelatedEntityImageId { get; set; }

        /// <summary>
        /// Realated entity Friendly Url (for artifact items it would be ParentFiendlyUrl/ItemFriendlyUrl )
        /// </summary>
        /// <remarks>
        /// It is set only in cummulative api calls
        /// </remarks>
        public string RelatedEntityFriendlyUrl { get; set; }

        /// <summary>
        /// Related Item Artifact Name
        /// </summary>
        /// <remarks>
        /// It is set only in cummulative api calls
        /// </remarks>
        public string RelatedItemParentName { get; set; }

        /// <summary>
        /// child notes
        /// </summary>
        /// <remarks>
        /// It os set only in certain cases
        /// </remarks>
        public RUserNoteViewModel[] Notes { get; set; }
    }
}
