using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// quoted poem view model
    /// </summary>
    public class GanjoorQuotedPoemViewModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        public GanjoorQuotedPoemViewModel() { }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="src"></param>
        public GanjoorQuotedPoemViewModel(GanjoorQuotedPoem src)
        {
            Id = src.Id;
            PoemId = src.PoemId;
            Poem = src.Poem;
            RelatedPoemId = src.RelatedPoemId;
            IsPriorToRelated = src.IsPriorToRelated;
            ChosenForMainList = src.ChosenForMainList;
            SortOrder = src.SortOrder;
            CachedRelatedPoemPoetDeathYearInLHijri = src.CachedRelatedPoemPoetDeathYearInLHijri;
            CachedRelatedPoemPoetName = src.CachedRelatedPoemPoetName;
            CachedRelatedPoemPoetUrl = src.CachedRelatedPoemPoetUrl;
            CachedRelatedPoemPoetImage = src.CachedRelatedPoemPoetImage;
            CachedRelatedPoemFullTitle = src.CachedRelatedPoemFullTitle;
            CachedRelatedPoemFullUrl = src.CachedRelatedPoemFullUrl;
            CoupletVerse1 = src.CoupletVerse1;
            CoupletVerse1ShouldBeEmphasized = src.CoupletVerse1ShouldBeEmphasized;
            CoupletVerse2 = src.CoupletVerse2;
            CoupletVerse2ShouldBeEmphasized = src.CoupletVerse2ShouldBeEmphasized;
            CoupletIndex = src.CoupletIndex;
            RelatedCoupletVerse1 = src.RelatedCoupletVerse1;
            RelatedCoupletVerse1ShouldBeEmphasized = src.RelatedCoupletVerse1ShouldBeEmphasized;
            RelatedCoupletVerse2 = src.RelatedCoupletVerse2;
            RelatedCoupletVerse2ShouldBeEmphasized = src.RelatedCoupletVerse2ShouldBeEmphasized;
            RelatedCoupletIndex = src.RelatedCoupletIndex;
            Note = src.Note;
            Published = src.Published;
            SamePoemsQuotedCount = src.SamePoemsQuotedCount;
            ClaimedByBothPoets = src.ClaimedByBothPoets;
            PoetId = src.PoetId;
            RelatedPoetId = src.RelatedPoetId;
            IndirectQuotation = src.IndirectQuotation;
        }

        /// <summary>
        /// record id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// related poem id (this could be a poem we do not have it available)
        /// </summary>
        public int? RelatedPoemId { get; set; }

        /// <summary>
        /// this poem is written and should be displayed prior to the related poem
        /// </summary>
        public bool IsPriorToRelated { get; set; }

        

        /// <summary>
        /// if the related poet poem has multiple poems related to this poem, which one is chosen to be shown at main poems pages?
        /// </summary>
        public bool ChosenForMainList { get; set; }

        /// <summary>
        /// this sort order can be used to pin a record on top
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// related poem peot death date in hijri (used for sorting)
        /// </summary>
        public int CachedRelatedPoemPoetDeathYearInLHijri { get; set; }

        /// <summary>
        /// related poem poet name
        /// </summary>
        public string CachedRelatedPoemPoetName { get; set; }

        /// <summary>
        /// related poem poet url
        /// </summary>
        public string CachedRelatedPoemPoetUrl { get; set; }

        /// <summary>
        /// related poem poet image
        /// </summary>
        public string CachedRelatedPoemPoetImage { get; set; }

        /// <summary>
        /// full title
        /// </summary>
        public string CachedRelatedPoemFullTitle { get; set; }

        /// <summary>
        /// full url
        /// </summary>
        public string CachedRelatedPoemFullUrl { get; set; }


        /// <summary>
        /// couplet 1 verse 1
        /// </summary>
        public string CoupletVerse1 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool CoupletVerse1ShouldBeEmphasized { get; set; }

        /// <summary>
        /// couplet 1 verse 2
        /// </summary>
        public string CoupletVerse2 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool CoupletVerse2ShouldBeEmphasized { get; set; }

        /// <summary>
        /// couplet 1 index
        /// </summary>
        public int? CoupletIndex { get; set; }

        /// <summary>
        /// related couplet 1 verse 1
        /// </summary>
        public string RelatedCoupletVerse1 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCoupletVerse1ShouldBeEmphasized { get; set; }

        /// <summary>
        /// related couplet 1 verse 2
        /// </summary>
        public string RelatedCoupletVerse2 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCoupletVerse2ShouldBeEmphasized { get; set; }

        /// <summary>
        /// related couplet 1 index
        /// </summary>
        public int? RelatedCoupletIndex { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// published (approved)
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// when a poem contains more than one quoted couplet from another one this should be more than 1
        /// </summary>
        public int SamePoemsQuotedCount { get; set; }

        /// <summary>
        /// poems (same poem) are claimed by both poets
        /// </summary>
        public bool ClaimedByBothPoets { get; set; }

        /// <summary>
        /// poet id (redundant for simplifying queries)
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// related poet id
        /// </summary>
        public int? RelatedPoetId { get; set; }

        /// <summary>
        /// related indirectly
        /// </summary>
        public bool IndirectQuotation { get; set; }
    }
}
