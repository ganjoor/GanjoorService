using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.PublicExport
{
    /// <summary>
    /// Root manifest written at the repository root (manifest.json).
    /// Lets consuming apps detect schema changes and do cheap "did anything change" checks
    /// without downloading the whole tree.
    /// </summary>
    public class PublicExportManifestDto
    {
        /// <summary>
        /// bump this whenever a DTO shape changes in a way consumers should know about
        /// </summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// UTC generation timestamp of this run (informational only — never embed per-file
        /// timestamps inside poem/cat files, that would defeat deterministic diffs)
        /// </summary>
        public string GeneratedAtUtc { get; set; }

        public int PoetsCount { get; set; }

        public int PoemsCount { get; set; }

        /// <summary>
        /// number of ids grouped into each id-index shard file (see UrlTemplates.CatIdIndexShard /
        /// PoemIdIndexShard). A consumer resolving id X fetches shard file "{X / IdIndexShardSize}.json".
        /// </summary>
        public int IdIndexShardSize { get; set; }

        /// <summary>
        /// URL patterns for every file kind in this export, so an app can treat this repo as an
        /// API without having to read the export source code. {placeholders} are literal.
        /// </summary>
        public PublicExportUrlTemplatesDto UrlTemplates { get; set; } = new PublicExportUrlTemplatesDto();

        public List<PublicExportManifestPoetEntryDto> Poets { get; set; } = new List<PublicExportManifestPoetEntryDto>();
    }

    public class PublicExportUrlTemplatesDto
    {
        public string Poet { get; set; } = "poets/{poetSlug}/poet.json";
        public string Category { get; set; } = "poets/{poetSlug}/{catPath}/_cat.json";
        public string Poem { get; set; } = "poets/{poetSlug}/{catPath}/{poemSlug}.json";
        public string PoetIdIndex { get; set; } = "index/poets-by-id.json";
        public string CatIdIndexShard { get; set; } = "index/cats-by-id/{bucket}.json";
        public string PoemIdIndexShard { get; set; } = "index/poems-by-id/{bucket}.json";
    }

    public class PublicExportManifestPoetEntryDto
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
        public string FullUrl { get; set; }
    }

    /// <summary>
    /// poet.json — biographical data only, no account/user linkage exists on GanjoorPoet at all
    /// </summary>
    public class PoetPublicDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public string Description { get; set; }
        public string FullUrl { get; set; }
        public string ImageUrl { get; set; }
        public int BirthYearInLHijri { get; set; }
        public bool ValidBirthDate { get; set; }
        public int DeathYearInLHijri { get; set; }
        public bool ValidDeathDate { get; set; }
        public string BirthPlace { get; set; }
        public string DeathPlace { get; set; }
    }

    /// <summary>
    /// _cat.json — one per category/collection folder
    /// </summary>
    public class CatPublicDto
    {
        public int Id { get; set; }
        public int PoetId { get; set; }
        public int? ParentId { get; set; }
        public string Title { get; set; }
        public string FullUrl { get; set; }
        public string Description { get; set; }
        public string DescriptionHtml { get; set; }
        public string BookName { get; set; }

        /// <summary>
        /// child categories, sorted by Id for deterministic output
        /// </summary>
        public List<CatChildRefDto> ChildCats { get; set; } = new List<CatChildRefDto>();

        /// <summary>
        /// poems directly under this category, sorted by Id for deterministic output
        /// </summary>
        public List<PoemChildRefDto> Poems { get; set; } = new List<PoemChildRefDto>();
    }

    public class CatChildRefDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FullUrl { get; set; }
    }

    public class PoemChildRefDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FullUrl { get; set; }
    }

    /// <summary>
    /// {poem-slug}.json — the poem text itself. GanjoorPoem/GanjoorVerse/GanjoorPoemSection carry
    /// no user/account reference in the source schema, so this is a straight field allowlist,
    /// not a redaction pass.
    /// </summary>
    public class PoemPublicDto
    {
        public int Id { get; set; }
        public int CatId { get; set; }
        public string Title { get; set; }
        public string FullTitle { get; set; }
        public string FullUrl { get; set; }
        public string RhymeLetters { get; set; }
        public string SourceName { get; set; }
        public string SourceUrlSlug { get; set; }
        public string Language { get; set; }
        public string PoemSummary { get; set; }
        public MetreRefDto Metre { get; set; }
        public List<PoemSectionPublicDto> Sections { get; set; } = new List<PoemSectionPublicDto>();
        public List<VersePublicDto> Verses { get; set; } = new List<VersePublicDto>();
    }

    public class MetreRefDto
    {
        public int Id { get; set; }
        public string Rhythm { get; set; }
        public string Name { get; set; }
    }

    public class PoemSectionPublicDto
    {
        public int Index { get; set; }
        public int Number { get; set; }
        public string SectionType { get; set; }
        public string VerseType { get; set; }
        public string RhymeLetters { get; set; }
        public string PlainText { get; set; }
        public string HtmlText { get; set; }
        public string PoemFormat { get; set; }
        public string Language { get; set; }
        public int CoupletsCount { get; set; }
    }

    public class VersePublicDto
    {
        public int VOrder { get; set; }
        public string Position { get; set; }
        public string Text { get; set; }
        public int? CoupletIndex { get; set; }
        public int? SectionIndex1 { get; set; }
        public int? SectionIndex2 { get; set; }
        public int? SectionIndex3 { get; set; }
        public int? SectionIndex4 { get; set; }
    }

    /// <summary>
    /// metres.json — shared lookup table written once at the repo root
    /// </summary>
    public class MetrePublicDto
    {
        public int Id { get; set; }
        public string UrlSlug { get; set; }
        public string Rhythm { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// languages.json — shared lookup table written once at the repo root
    /// </summary>
    public class LanguagePublicDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string NativeName { get; set; }
        public bool RightToLeft { get; set; }
    }
}
