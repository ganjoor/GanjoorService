namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    ///  a person refered by a poem
    /// </summary>
    public class GanjoorRelatedPerson
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// wikipedia url
        /// </summary>
        public string WikiUrl { get; set; }

        /// <summary>
        /// birth year in lunar hijri
        /// </summary>
        public int BirthYearInLHijri { get; set; }

        /// <summary>
        /// death year in lunar hijri
        /// </summary>
        public int DeathYearInLHijri { get; set; }

        /// <summary>
        /// BirthYearInLHijri, for some poets it is only indicator of their century of birth and should not be relied as their valid birth date
        /// </summary>
        public bool ValidBirthDate { get; set; }

        /// <summary>
        /// DeathYearInLHijri, for some poets it is only indicator of their century of death and should not be relied as their valid death date
        /// </summary>
        public bool ValidDeathDate { get; set; }

        /// <summary>
        /// birth location id
        /// </summary>
        public int? BirthLocationId { get; set; }

        /// <summary>
        /// birth location
        /// </summary>
        public virtual GanjoorGeoLocation BirthLocation { get; set; }

        /// <summary>
        /// death location id
        /// </summary>
        public int? DeathLocationId { get; set; }

        /// <summary>
        /// death location
        /// </summary>
        public virtual GanjoorGeoLocation DeathLocation { get; set; }

        /// <summary>
        /// AI generated
        /// </summary>
        public bool MachineGenerated { get; set; }

        /// <summary>
        /// optional caption for the family tree this person is treated as the root of (e.g.
        /// "ساسانیان", "آل برمک") - purely a display label for whoever a tree is being browsed
        /// from; nothing enforces that this person actually has no recorded ancestors themselves,
        /// and most people will leave this null (only whichever person a tree is "named after"
        /// needs one set).
        /// </summary>
        public string FamilyTreeCaption { get; set; }

        /// <summary>
        /// editorial prominence, used to size this person's node in the people graph larger or
        /// smaller (see PersonImportance and peoplegraph.js's nodeRadius()) - defaults to Normal
        /// </summary>
        public PersonImportance Importance { get; set; }

        /// <summary>
        /// gender - only used by the family-tree chart to pick which parent is drawn as the tree's
        /// spine when a child has two recorded parents who both have their own recorded ancestors
        /// (see PersonGender and familytree.js's buildLayout()) - defaults to Unknown
        /// </summary>
        public PersonGender Gender { get; set; }
    }
}
