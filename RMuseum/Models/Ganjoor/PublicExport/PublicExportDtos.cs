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
        public int SchemaVersion { get; set; } = 2;

        /// <summary>
        /// UTC generation timestamp of this run (informational only — never embed per-file
        /// timestamps inside poem/cat files, that would defeat deterministic diffs)
        /// </summary>
        public string GeneratedAtUtc { get; set; }

        public int PoetsCount { get; set; }

        public int PoemsCount { get; set; }

        /// <summary>
        /// number of people (people.json)
        /// </summary>
        public int PeopleCount { get; set; }

        /// <summary>
        /// number of places (locations.json)
        /// </summary>
        public int LocationsCount { get; set; }

        /// <summary>
        /// number of couplet-bound place/date tags (geotags.json)
        /// </summary>
        public int GeoTagsCount { get; set; }

        /// <summary>
        /// number of couplet-bound person tags (the sum of all index/person-tags shards)
        /// </summary>
        public int PersonTagsCount { get; set; }

        /// <summary>
        /// number of ids grouped into each index/person-tags shard file: the shard of person X is
        /// "{X / PersonIndexShardSize}.json"
        /// </summary>
        public int PersonIndexShardSize { get; set; }

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
        public string Locations { get; set; } = "locations.json";
        public string People { get; set; } = "people.json";
        public string PersonRelations { get; set; } = "person-relations.json";
        public string PersonAffiliations { get; set; } = "person-affiliations.json";
        public string GeoTags { get; set; } = "geotags.json";
        public string PersonTagsIndexShard { get; set; } = "index/person-tags/{bucket}.json";
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

        /// <summary>
        /// approved place/date/person tags bound to couplets of this poem, sorted by couplet
        /// index; left out of the file (null) when the poem has none
        /// </summary>
        public List<PoemTagPublicDto> Tags { get; set; }
    }

    /// <summary>
    /// an approved tag on one couplet of a poem: a place, a date, a person, or a combination.
    /// LocationId refers to locations.json and PersonId to people.json. Who suggested or
    /// approved a tag is deliberately not part of the export.
    /// </summary>
    public class PoemTagPublicDto
    {
        /// <summary>
        /// the tagged couplet (VersePublicDto.CoupletIndex)
        /// </summary>
        public int CoupletIndex { get; set; }
        public int? LocationId { get; set; }
        public int? PersonId { get; set; }

        /// <summary>
        /// "Participant" or "Allusion" - set only together with PersonId
        /// </summary>
        public string PersonMention { get; set; }

        /// <summary>
        /// lunar Hijri date parts, each optional
        /// </summary>
        public int? LunarYear { get; set; }
        public int? LunarMonth { get; set; }
        public int? LunarDay { get; set; }
        public bool VerifiedDate { get; set; }

        /// <summary>
        /// true when the tag is meant to be left out of the category-level summaries (map, timeline)
        /// </summary>
        public bool IgnoreInCategory { get; set; }
        public bool MachineGenerated { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// locations.json item
    /// </summary>
    public class LocationPublicDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool MachineGenerated { get; set; }
    }

    /// <summary>
    /// people.json item - a person (or legendary figure) who is not necessarily a poet
    /// </summary>
    public class PersonPublicDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// other names, as stored (separator-delimited text)
        /// </summary>
        public string Aliases { get; set; }
        public string WikiUrl { get; set; }

        /// <summary>
        /// "Unknown", "Male" or "Female"
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// "Normal", "Important" or "VeryImportant"
        /// </summary>
        public string Importance { get; set; }
        public int BirthYearInLHijri { get; set; }
        public bool ValidBirthDate { get; set; }
        public int DeathYearInLHijri { get; set; }
        public bool ValidDeathDate { get; set; }
        public int? BirthLocationId { get; set; }
        public int? DeathLocationId { get; set; }
        public string FamilyTreeCaption { get; set; }
        public bool MachineGenerated { get; set; }
    }

    /// <summary>
    /// a couplet attesting a relation or affiliation. A relation is considered attested "in a
    /// book" by the evidence whose MasterCatId is that book's category id - the same person
    /// pair can be related differently in different books.
    /// </summary>
    public class PersonEvidencePublicDto
    {
        public int PoemId { get; set; }
        public int CoupletIndex { get; set; }

        /// <summary>
        /// category id (see CatPublicDto) of the book this evidence belongs to
        /// </summary>
        public int MasterCatId { get; set; }

        /// <summary>
        /// true only when the evidence was inferred rather than attached by a person
        /// </summary>
        public bool? Inferred { get; set; }
    }

    /// <summary>
    /// person-relations.json item - kinship. For "Parent" and "Ancestor" Person1Id is the
    /// parent/ancestor and Person2Id the child/descendant; "Sibling" and "Spouse" are symmetric.
    /// </summary>
    public class PersonRelationPublicDto
    {
        public int Id { get; set; }
        public int Person1Id { get; set; }
        public int Person2Id { get; set; }

        /// <summary>
        /// "Parent", "Sibling", "Spouse" or "Ancestor"
        /// </summary>
        public string RelationType { get; set; }

        /// <summary>
        /// for "Ancestor": the exact number of generations, when known
        /// </summary>
        public int? DegreeHint { get; set; }
        public string Note { get; set; }
        public List<PersonEvidencePublicDto> Evidence { get; set; } = new List<PersonEvidencePublicDto>();
    }

    /// <summary>
    /// person-affiliations.json item - non-family ties. For directional types (Minister, Advisor,
    /// Courtier, Patron, ...) Person1Id is the one in the serving role, Person2Id the one served.
    /// </summary>
    public class PersonAffiliationPublicDto
    {
        public int Id { get; set; }
        public int Person1Id { get; set; }
        public int Person2Id { get; set; }

        /// <summary>
        /// the name of the PersonAffiliationType value, e.g. "Minister", "Panegyrized", "Other"
        /// </summary>
        public string AffiliationType { get; set; }
        public string Note { get; set; }
        public List<PersonEvidencePublicDto> Evidence { get; set; } = new List<PersonEvidencePublicDto>();
    }

    /// <summary>
    /// geotags.json item - a place/date tag, flattened out of the poem files so a map or
    /// timeline can be built from one file
    /// </summary>
    public class GeoTagPublicDto
    {
        public int PoemId { get; set; }
        public int CoupletIndex { get; set; }
        public int LocationId { get; set; }
        public int? LunarYear { get; set; }
        public int? LunarMonth { get; set; }
        public int? LunarDay { get; set; }
        public bool VerifiedDate { get; set; }
        public bool IgnoreInCategory { get; set; }
        public bool MachineGenerated { get; set; }
    }

    /// <summary>
    /// index/person-tags/{bucket}.json item - where a person is tagged, so "all couplets about X"
    /// doesn't need a scan of every poem file
    /// </summary>
    public class PersonTagRefPublicDto
    {
        public int PoemId { get; set; }
        public int CoupletIndex { get; set; }
        public string PersonMention { get; set; }
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

        /// <summary>
        /// AI-generated (prefixed "هوش مصنوعی:", matching PoemSummary's own convention) until a
        /// human reviews/edits it, at which point the prefix is removed — that prefix's
        /// presence/absence is itself the provenance signal, same as for PoemSummary, nothing
        /// new needed here to track it. Published specifically so a human editor's curatorial
        /// work on a couplet's interpretation — genuinely irreplaceable if the live database
        /// were ever lost, unlike the poem text itself, which exists in many other sources —
        /// isn't only ever stored in one place.
        /// </summary>
        public string CoupletSummary { get; set; }
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
