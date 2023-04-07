using RSecurityBackend.Models.Image;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Category
    /// </summary>
    public class GanjoorCat
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// poet_id
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// poet
        /// </summary>
        public GanjoorPoet Poet { get; set; }

        /// <summary>
        /// text
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// parent_id
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// parent
        /// </summary>
        public virtual GanjoorCat Parent { get; set; }

        /// <summary>
        /// url => slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// sample: /hafez/ghazal
        /// </summary>
        public string FullUrl { get; set; }

        /// <summary>
        /// TOC Style
        /// </summary>
        public GanjoorTOC TableOfContentsStyle { get; set; }

        /// <summary>
        /// Category Type
        /// </summary>
        public GanjoorCatType CatType { get; set; }

        /// <summary>
        /// additional descripion or note
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// html mode of additional descripion or note
        /// </summary>
        public string DescriptionHtml { get; set; }

        /// <summary>
        /// order when mixed with poems
        /// </summary>
        public int MixedModeOrder { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// category book name
        /// </summary>
        public string BookName { get; set; }

        /// <summary>
        /// poet image
        /// </summary>
        public virtual RImage RImage { get; set; }

        /// <summary>
        /// user image id
        /// </summary>
        public Guid? RImageId { get; set; }

        /// <summary>
        /// sum up sub categories geo locations
        /// </summary>
        public bool SumUpSubsGeoLocations { get; set; }

        /// <summary>
        /// category map name
        /// </summary>
        public string MapName { get; set; }
    }
}
