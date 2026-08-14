using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.PublicExport
{
    /// <summary>
    /// Root manifest for the Tajik data export — a separate, sparser repository from the main
    /// ganjoor-data one. Tajik translation only covers a subset of poets/categories/poems, so this
    /// export only ever contains entries that actually have a Tajik counterpart in the database
    /// (GanjoorTajikPoet/Cat/Poem/Verse) — it does not mirror the full corpus the way the main
    /// export does. Ids and FullUrls match the main repo 1:1 (same underlying entity ids), so a
    /// consumer resolves structure/metre/sections for a poem by fetching the same id/path from
    /// ganjoor-data and merging verses by VOrder.
    /// </summary>
    public class TajikPublicExportManifestDto
    {
        public int SchemaVersion { get; set; } = 1;
        public string GeneratedAtUtc { get; set; }
        public int PoetsCount { get; set; }
        public int PoemsCount { get; set; }
        public int IdIndexShardSize { get; set; }

        /// <summary>
        /// where to find the structural data (metre, rhyme, sections, full verse text in Persian)
        /// this repo's ids/paths correspond to
        /// </summary>
        public string MainDataRepo { get; set; } = "https://github.com/ganjoor/ganjoor-data";

        public TajikPublicExportUrlTemplatesDto UrlTemplates { get; set; } = new TajikPublicExportUrlTemplatesDto();

        public List<PublicExportManifestPoetEntryDto> Poets { get; set; } = new List<PublicExportManifestPoetEntryDto>();
    }

    public class TajikPublicExportUrlTemplatesDto
    {
        public string Poet { get; set; } = "poets/{poetSlug}/poet.json";
        public string Category { get; set; } = "poets/{poetSlug}/{catPath}/_cat.json";
        public string Poem { get; set; } = "poets/{poetSlug}/{catPath}/{poemSlug}.json";
        public string PoetIdIndex { get; set; } = "index/poets-by-id.json";
        public string CatIdIndexShard { get; set; } = "index/cats-by-id/{bucket}.json";
        public string PoemIdIndexShard { get; set; } = "index/poems-by-id/{bucket}.json";
    }

    /// <summary>
    /// poet.json — only written for a poet with an actual GanjoorTajikPoet record.
    /// FullUrl matches the poet's path in the main ganjoor-data repo exactly.
    /// </summary>
    public class TajikPoetPublicDto
    {
        public int Id { get; set; }
        public string TajikNickname { get; set; }
        public string TajikDescription { get; set; }
        public int BirthYearInLHijri { get; set; }
        public string FullUrl { get; set; }
    }

    /// <summary>
    /// _cat.json — only written for a category with an actual GanjoorTajikCat record.
    /// ChildCats/Poems only list children that themselves have a Tajik translation — a consumer
    /// never gets a link to a file that doesn't exist.
    /// </summary>
    public class TajikCatPublicDto
    {
        public int Id { get; set; }
        public int PoetId { get; set; }
        public int? ParentId { get; set; }
        public string TajikTitle { get; set; }
        public string TajikDescription { get; set; }
        public string FullUrl { get; set; }
        public List<CatChildRefDto> ChildCats { get; set; } = new List<CatChildRefDto>();
        public List<PoemChildRefDto> Poems { get; set; } = new List<PoemChildRefDto>();
    }

    /// <summary>
    /// {poem-slug}.json — only written for a poem with an actual GanjoorTajikPoem record.
    /// Deliberately doesn't carry metre/rhyme/section data — GanjoorTajikPoem/Verse don't store
    /// their own copy of that either; fetch it from the same id/path in the main ganjoor-data repo.
    /// </summary>
    public class TajikPoemPublicDto
    {
        public int Id { get; set; }
        public int CatId { get; set; }
        public string TajikTitle { get; set; }
        public string FullTitle { get; set; }
        public string FullUrl { get; set; }
        public string TajikPlainText { get; set; }
        public List<TajikVersePublicDto> Verses { get; set; } = new List<TajikVersePublicDto>();
    }

    /// <summary>
    /// VOrder is the join key back to the corresponding verse in the main repo's poem file — no
    /// position/section/couplet data is duplicated here, matching how GanjoorTajikVerse itself
    /// stores nothing but the transliterated text and its order.
    /// </summary>
    public class TajikVersePublicDto
    {
        public int VOrder { get; set; }
        public string TajikText { get; set; }
    }
}
