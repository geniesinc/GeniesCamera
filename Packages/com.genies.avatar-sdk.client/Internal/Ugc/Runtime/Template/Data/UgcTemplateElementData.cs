namespace Genies.Ugc
{
    public sealed class UgcTemplateElementData
    {
        /// <summary>
        /// The unique ID of this UGC element.
        /// </summary>
        public string ElementId { get; }

        /// <summary>
        /// The number of regions that this element has.
        /// </summary>
        public int Regions { get; }

        /// <summary>
        /// The mega shader version that this element uses.
        /// </summary>
        public string MaterialVersion { get; }

        public UgcTemplateElementData(
            string elementId,
            int regions,
            string materialVersion)
        {
            ElementId = elementId;
            Regions = regions;
            MaterialVersion = materialVersion;
        }
    }
}
