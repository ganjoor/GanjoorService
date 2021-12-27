namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// recitation technical error
    /// </summary>
    public class RecitationTechnicalError
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Recitation Id
        /// </summary>
        public int RecitationId { get; set; }

        /// <summary>
        /// recitation
        /// </summary>
        public Recitation Recitation { get; set; }

        /// <summary>
        /// problem
        /// </summary>
        public string Problem { get; set; }
    }
}
