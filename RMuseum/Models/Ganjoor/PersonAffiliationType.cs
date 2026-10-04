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
        /// Person1 was a servant (personal attendant, not a court office) of Person2 - narrower than
        /// Courtier, for a purely domestic/personal-service tie rather than a court role
        /// </summary>
        Servant = 6,

        /// <summary>
        /// Person1 and Person2 were companions/comrades - e.g. fellow travelers, brothers-in-arms
        /// (symmetric - order doesn't matter)
        /// </summary>
        Companion = 7,

        /// <summary>
        /// Person1 succeeded Person2 in a role/office/throne (directional - Person1 is the one who
        /// came after)
        /// </summary>
        Successor = 8,

        /// <summary>
        /// Person1 (a poet) wrote panegyric/praise poetry (قصیدهٔ مدح) about Person2 (the ممدوح -
        /// the praised patron/king/dignitary) - directional, same convention as Minister/Patron
        /// </summary>
        Panegyrized = 9,

        /// <summary>
        /// Person1 (a poet) wrote satirical/mocking poetry (هجو) about Person2 (the هجو شده -
        /// the satire's target) - directional, same convention as Minister/Patron
        /// </summary>
        Satirized = 10,

        /// <summary>
        /// Person1 served as a military commander/general (سردار) under Person2 - directional, same
        /// subordinate-side convention as Minister/Advisor/Courtier, but deliberately kept separate
        /// from Courtier: a سردار's tie to Person2 is a military command relationship, not a court
        /// role, and (unlike e.g. Minister, which implies one standing appointment) doesn't imply the
        /// tie held continuously - a commander could serve across campaigns/kings, with specifics
        /// (which campaign, how long) left to Note
        /// </summary>
        MilitaryCommander = 11,

        /// <summary>
        /// Person1 was a champion/legendary warrior (پهلوان) associated with Person2 - directional,
        /// same subordinate-side convention as MilitaryCommander, but distinct from it: a پهلوان's
        /// standing comes from individual prowess/heroism (e.g. Rostam), not from holding a command
        /// post, and like MilitaryCommander this doesn't imply a permanent/continuous tie
        /// </summary>
        Champion = 12,

        /// <summary>
        /// Person1 and Person2 lived in the same era (هم‌عصر), with no other relationship between
        /// them necessarily known/implied - symmetric (order doesn't matter). Unlike every other
        /// value here, this doesn't assert any kind of tie or interaction, only overlapping lifetimes;
        /// use one of the other types instead whenever an actual relationship is known
        /// </summary>
        Contemporary = 13,

        /// <summary>
        /// Person1 killed Person2 (کشتن) - directional. Deliberately separate from Rival: an act of
        /// killing is usually a single event (e.g. in battle, by treachery, in a duel), and doesn't by
        /// itself mean the two were ongoing enemies/rivals beforehand - they could have been allies,
        /// kin-by-marriage, or strangers. Use Rival instead (additionally, if applicable) when there
        /// was a standing enmity, and Note for how/why the killing happened
        /// </summary>
        Killer = 14,

        /// <summary>
        /// doesn't fit any of the above - rely on Note for what the tie actually is
        /// </summary>
        Other = 99,
    }
}
