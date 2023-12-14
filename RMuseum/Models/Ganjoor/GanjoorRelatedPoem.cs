using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// related poem to a poem
    /// </summary>
    public class GanjoorRelatedPoem
    {
        /// <summary>
        /// record id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// related poem id (this could be a poem we do not have it available)
        /// </summary>
        public int? RelatedPoemId { get; set; }

        /// <summary>
        /// this poem is written and should be displayed prior to the related poem
        /// </summary>
        public bool IsPriorToRelated { get; set; }

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
        /// couplet 1 poet name
        /// </summary>
        public string Couplet1PoetName { get; set; }

        /// <summary>
        /// couplet 1 verse 1
        /// </summary>
        public string Couplet1Verse1 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool Couplet1Verse1IsMainPart { get; set; }

        /// <summary>
        /// couplet 1 verse 2
        /// </summary>
        public string Couplet1Verse2 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool Couplet1Verse2IsMainPart { get; set; }

        /// <summary>
        /// couplet 1 index
        /// </summary>
        public int? Couplet1Index { get; set; }

        /// <summary>
        /// related couplet 1 poet name
        /// </summary>
        public string RelatedCouplet1PoetName { get; set; }

        /// <summary>
        /// related couplet 1 verse 1
        /// </summary>
        public string RelatedCouplet1Verse1 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet1Verse1IsMainPart { get; set; }

        /// <summary>
        /// related couplet 1 verse 2
        /// </summary>
        public string RelatedCouplet1Verse2 { get; set; }


        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet1Verse2IsMainPart { get; set; }

        /// <summary>
        /// related couplet 1 index
        /// </summary>
        public int? RelatedCouplet1Index { get; set; }

        /// <summary>
        /// related couplet 1 verse 3
        /// </summary>
        public string RelatedCouplet1Verse3 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet1Verse3IsMainPart { get; set; }

        /// <summary>
        /// related couplet 1 verse 4
        /// </summary>
        public string RelatedCouplet1Verse4 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet1Verse4IsMainPart { get; set; }

        /// <summary>
        /// related couplet 1 - verse 3/4 index
        /// </summary>
        public int? RelatedCouplet1Verse34Index { get; set; }

        /// <summary>
        /// couplet 2 poet name
        /// </summary>
        public string Couplet2PoetName { get; set; }

        /// <summary>
        /// couplet 2 verse 1
        /// </summary>
        public string Couplet2Verse1 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool Couplet2Verse1IsMainPart { get; set; }

        /// <summary>
        /// couplet 2 verse 2
        /// </summary>
        public string Couplet2Verse2 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool Couplet2Verse2IsMainPart { get; set; }

        /// <summary>
        /// couplet 2 index
        /// </summary>
        public int? Couplet2Index { get; set; }

        /// <summary>
        /// related couplet 2 poet name
        /// </summary>
        public string RelatedCouplet2PoetName { get; set; }

        /// <summary>
        /// related couplet 2 verse 1
        /// </summary>
        public string RelatedCouplet2Verse1 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet2Verse1IsMainPart { get; set; }

        /// <summary>
        /// related couplet 2 verse 2
        /// </summary>
        public string RelatedCouplet2Verse2 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet2Verse2IsMainPart { get; set; }

        /// <summary>
        /// related couplet 2 index
        /// </summary>
        public int? RelatedCouplet2Index { get; set; }

        /// <summary>
        /// related couplet 2 verse 3
        /// </summary>
        public string RelatedCouplet2Verse3 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet2Verse3IsMainPart { get; set; }

        /// <summary>
        /// related couplet 2 verse 4
        /// </summary>
        public string RelatedCouplet2Verse4 { get; set; }

        /// <summary>
        /// is main part
        /// </summary>
        public bool RelatedCouplet2Verse4IsMainPart { get; set; }

        /// <summary>
        /// related couplet 2 - verse 3/4 index
        /// </summary>
        public int? RelatedCouplet2Verse34Index { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }
    }
}
