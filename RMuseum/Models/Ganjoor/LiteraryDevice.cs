using System;
using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// status of an entity (device, concept) that users can suggest and moderators approve
    /// </summary>
    public enum LiteraryEntityStatus
    {
        /// <summary>
        /// suggested by a user, waiting for a moderator
        /// </summary>
        Suggested = 0,

        /// <summary>
        /// approved and selectable by everyone
        /// </summary>
        Approved = 1,

        /// <summary>
        /// rejected by a moderator
        /// </summary>
        Rejected = 2
    }

    /// <summary>
    /// broad group of a literary device, as books on rhetoric group them
    /// </summary>
    public enum LiteraryDeviceCategory
    {
        /// <summary>
        /// بیان
        /// </summary>
        Bayan = 0,

        /// <summary>
        /// بدیع لفظی
        /// </summary>
        BadiLafzi = 1,

        /// <summary>
        /// بدیع معنوی
        /// </summary>
        BadiManavi = 2,

        /// <summary>
        /// ساختار و موسیقی شعر (ردیف، قافیه و ...)
        /// </summary>
        Structure = 3,

        /// <summary>
        /// anything else
        /// </summary>
        Other = 4
    }

    /// <summary>
    /// a literary device (آرایهٔ ادبی), e.g. تشبیه. Devices are data, not code: the core list is
    /// seeded, and users can suggest new ones (with their slots) which become selectable for
    /// everyone once a moderator approves them.
    /// </summary>
    public class LiteraryDevice
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// optional stable code for devices the program refers to by name (e.g. "tashbih"); null for user suggested ones
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Persian name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// group
        /// </summary>
        public LiteraryDeviceCategory Category { get; set; }

        /// <summary>
        /// operational definition: when exactly may this tag be used
        /// </summary>
        public string Definition { get; set; }

        /// <summary>
        /// an example and/or boundary note
        /// </summary>
        public string Example { get; set; }

        /// <summary>
        /// the book or reference the definition comes from
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// part of the initial core list (as opposed to extension)
        /// </summary>
        public bool IsCore { get; set; }

        /// <summary>
        /// status
        /// </summary>
        public LiteraryEntityStatus Status { get; set; }

        /// <summary>
        /// suggesting user (null for the seeded core list)
        /// </summary>
        public Guid? SuggestedByUserId { get; set; }

        /// <summary>
        /// suggestion date
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// moderator note
        /// </summary>
        public string ReviewNote { get; set; }

        /// <summary>
        /// slots (positions) of the device
        /// </summary>
        public ICollection<LiteraryDeviceSlot> Slots { get; set; }
    }
}
