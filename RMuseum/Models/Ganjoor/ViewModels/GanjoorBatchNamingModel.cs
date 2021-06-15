namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Batch Naming Model
    /// </summary>
    public class GanjoorBatchNamingModel
    {
        /// <summary>
        /// Start with (not including required spaces)
        /// </summary>
        /// <example>
        /// شمارهٔ 
        /// </example>
        public string StartWithNotIncludingSpaces { get; set; }

        /// <summary>
        /// remove previous pattern from start until any numbers
        /// </summary>
        public bool RemovePreviousPattern { get; set; }
        
        /// <summary>
        /// remove set of characters other than spaces from beginning and end
        /// </summary>
        /// <example>.-</example>
        public string RemoveSetOfCharacters { get; set; }

        /// <summary>
        /// simulate naming
        /// </summary>
        public bool Simulate { get; set; }
    }
}
