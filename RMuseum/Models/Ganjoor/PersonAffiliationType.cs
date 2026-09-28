namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// the kind of non-family tie a GanjoorPersonAffiliation represents between two people - e.g.
    /// a minister serving a king, an advisor, a patron. Unlike PersonRelationType this has nothing
    /// to do with kinship; it's what lets two otherwise unrelated family trees show up as adjacent
    /// (e.g. "some Barmakids served the Abbasid court") without merging them into one tree. New
    /// values can be appended safely later (stored as int) as more cases come up.
    /// </summary>
    public enum PersonAffiliationType
    {
        /// <summary>
        /// Person1 served as minister/vizier to Person2
        /// </summary>
        Minister = 0,

        /// <summary>
        /// Person1 was an advisor/counselor to Person2, without holding a formal ministerial post
        /// </summary>
        Advisor = 1,

        /// <summary>
        /// Person1 was a courtier/attendant/servant of Person2 (a catch-all for court-affiliated
        /// roles not covered by a more specific type)
        /// </summary>
        Courtier = 2,

        /// <summary>
        /// Person1 was a patron/sponsor of Person2 (e.g. a king patronizing a poet)
        /// </summary>
        Patron = 3,

        /// <summary>
        /// Person1 and Person2 were allies (symmetric - order doesn't matter)
        /// </summary>
        Ally = 4,

        /// <summary>
        /// Person1 and Person2 were rivals/enemies (symmetric - order doesn't matter)
        /// </summary>
        Rival = 5,

        /// <summary>
        /// doesn't fit any of the above - rely on Note for what the tie actually is
        /// </summary>
        Other = 99,
    }
}
