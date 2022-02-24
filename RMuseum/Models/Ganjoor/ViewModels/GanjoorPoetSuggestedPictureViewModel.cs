using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    public class GanjoorPoetSuggestedPictureViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Poet Id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// order
        /// </summary>
        public int PicOrder { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// image url
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// suggested by id
        /// </summary>
        public Guid? SuggestedById { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// selected
        /// </summary>
        public bool ChosenOne { get; set; }

        /// <summary>
        /// rejection cause
        /// </summary>
        public string RejectionCause { get; set; }

    }
}
