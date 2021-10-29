using NetTopologySuite.Geometries;


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
        /// geo location
        /// </summary>
        public Point Location { get; set; }
    }
}
