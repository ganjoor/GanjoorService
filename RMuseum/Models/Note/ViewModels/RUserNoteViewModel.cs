using RMuseum.Models.Artifact;
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
        /// Related entity external normal size image url
        /// </summary>
        public string RelatedEntityExternalNormalSizeImageUrl { get; set; }

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

        /// <summary>
        /// prepare note datetime
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string PrepareNoteDateTime(DateTime dateTime)
        {
            PersianCalendar pc = new PersianCalendar();
            return $"{pc.GetYear(dateTime)}/{pc.GetMonth(dateTime)}/{pc.GetDayOfMonth(dateTime)}&nbsp;{pc.GetHour(dateTime)}:{pc.GetMinute(dateTime)}";
        }
    }
}
