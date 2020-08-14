namespace RMuseum.Models.Artifact
{
    /// <summary>
    /// Items or pictures status
    /// </summary>
    public enum PublishStatus
    {
        Draft = 0,
        Awaiting = 1,
        Refused = 2,
        Published = 4,
        Restricted = 8,
        Hidden  = 16,
        Deleted = 32
    }
}
