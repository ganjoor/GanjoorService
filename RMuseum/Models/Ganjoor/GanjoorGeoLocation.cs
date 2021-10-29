namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Geo Locations (Cities) referred by Ganjoor Metadata
    /// </summary>
    public class GanjoorGeoLocation
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
        /// Latitude
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude
        /// </summary>
        public double Longitude { get; set; }
    }
}
