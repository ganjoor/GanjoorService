namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// Recitation approved mistake
    /// </summary>
    public class RecitationApprovedMistake
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// recitation id
        /// </summary>
        public int RecitationId { get; set; }

        /// <summary>
        /// recitation
        /// </summary>
        public Recitation Recitation { get; set; }

        /// <summary>
        /// mistake
        /// </summary>
        public string Mistake { get; set; }
    }
}
