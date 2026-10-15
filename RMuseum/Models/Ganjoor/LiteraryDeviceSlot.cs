namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// what a slot of a device holds
    /// </summary>
    public enum LiteraryDeviceSlotKind
    {
        /// <summary>
        /// a stretch of words of the poem (e.g. ادات in a simile)
        /// </summary>
        Span = 0,

        /// <summary>
        /// a stretch of words of the poem plus the concept it stands for (e.g. مشبّه: «رخ» ← «چهرهٔ معشوق»)
        /// </summary>
        SpanConcept = 1,

        /// <summary>
        /// only a concept that is not literally in the poem (e.g. مستعارٌله)
        /// </summary>
        Concept = 2,

        /// <summary>
        /// free text (e.g. a meaning of an ایهام)
        /// </summary>
        Text = 3
    }

    /// <summary>
    /// a slot (position) of a device: e.g. مشبّه، مشبّه‌به، وجه شبه، ادات for تشبیه
    /// </summary>
    public class LiteraryDeviceSlot
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// device id
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// optional stable code (e.g. "tenor", "vehicle")
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Persian name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// kind
        /// </summary>
        public LiteraryDeviceSlotKind Kind { get; set; }

        /// <summary>
        /// the slot has to be filled for the tag to be valid
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// the slot can be filled more than once in one tag (e.g. the meanings of an ایهام)
        /// </summary>
        public bool AllowMultiple { get; set; }

        /// <summary>
        /// display order
        /// </summary>
        public int Order { get; set; }
    }
}
