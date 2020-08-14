using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Models.Note.ViewModels
{
    /// <summary>
    /// New User Note For Artifact
    /// </summary>
    public class PostUserNote
    {
        /// <summary>
        /// Artifact / Item Id
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Note Type
        /// </summary>
        public RNoteType NoteType { get; set; }

        /// <summary>
        /// Contents
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// Reference Note Id
        /// </summary>
        public Guid? ReferenceNoteId { get; set; }
    }
}
