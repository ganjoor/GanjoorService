namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// Audio Synchronization Status (binary combination is acceptable)
    /// </summary>
    public enum AudioSyncStatus
    {
        /// <summary>
        /// no changed
        /// </summary>
        SynchronizedOrRejected = 0,

        /// <summary>
        /// new upload
        /// </summary>
        NewItem = 1,

        /// <summary>
        /// meta data should be updated
        /// </summary>
        MetadataChanged = 2,

        /// <summary>
        /// sound files should be reuploaded (this means xml file is also needs to reuploaded)
        /// </summary>
        SoundFilesChanged = 4,

        /// <summary>
        /// Xml file should be reuploaded
        /// </summary>
        //XmlFileChanged = 8,

        /// <summary>
        /// Item should be deleted
        /// </summary>
        Deleted = 16

    }
}
