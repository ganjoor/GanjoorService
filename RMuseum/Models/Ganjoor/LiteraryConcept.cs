using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// a normalized concept (مفهوم) used as a slot value: e.g. «معشوق», «پروانه», «شمع». It is what makes
    /// aggregate statistics possible ("how many times is the beloved compared to the moon?"): «لب معشوق»
    /// is a concept whose parent is «معشوق», so counts can be rolled up.
    /// </summary>
    public class LiteraryConcept
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// normalized name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// other spellings/names, separated by « | »
        /// </summary>
        public string Aliases { get; set; }

        /// <summary>
        /// the broader concept (e.g. «معشوق» for «لب معشوق»)
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// status
        /// </summary>
        public LiteraryEntityStatus Status { get; set; }

        /// <summary>
        /// the user whose approved tag introduced it (null for imported ones)
        /// </summary>
        public Guid? CreatedByUserId { get; set; }

        /// <summary>
        /// creation date
        /// </summary>
        public DateTime CreateDate { get; set; }
    }
}
