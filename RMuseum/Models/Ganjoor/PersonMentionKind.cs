namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// why a person is named in a tagged couplet/poem (PoemGeoDateTag.PersonMention): is the person
    /// actually part of the story/context being told, or only mentioned in passing? Needed to tell
    /// "poems where this person appears" from "poems that merely refer to them", e.g. when building
    /// presence and lifetime charts. Stored as int; new values can be appended later.
    /// </summary>
    public enum PersonMentionKind
    {
        /// <summary>
        /// the person is part of the story or context of the passage: acts in it, is spoken to or about as
        /// part of the narration (this includes the poet and the people he names as his helpers)
        /// </summary>
        Participant = 0,

        /// <summary>
        /// the person is mentioned only as a comparison, memory, example, or a reference to a past/future
        /// figure ("brave as Rostam") - not part of the passage's own story
        /// </summary>
        Allusion = 1,
    }
}
