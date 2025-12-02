using System.Collections.Generic;
using System.Linq;

namespace Genies.Ugc
{
    public sealed class UgcTemplateSplitData
    {
        /// <summary>
        /// The split index of this split within the <see cref="UgcTemplateData"/>.
        /// </summary>
        public int SplitIndex { get; }

        /// <summary>
        /// The data of the elements available for this split.
        /// </summary>
        public IReadOnlyList<UgcTemplateElementData> Elements { get; }

        public UgcTemplateSplitData(int splitIndex, IEnumerable<UgcTemplateElementData> elements)
        {
            SplitIndex = splitIndex;
            Elements = elements.ToList().AsReadOnly();
        }
    }
}
