using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Used by <see cref="WearableRandomizer"/> to generate random colors for UGC split regions.
    /// </summary>
    internal struct RegionColors
    {
        public Color Color1;
        public Color Color2;
        public Color Color3;
        public Color Color4;

        public RegionColors(Color color1, Color color2, Color color3, Color color4)
        {
            Color1 = color1;
            Color2 = color2;
            Color3 = color3;
            Color4 = color4;
        }

        public Color this[int regionIndex]
        {
            get => regionIndex switch
            {
                0 => Color1,
                1 => Color2,
                2 => Color3,
                3 => Color4,
                _ => default,
            };

            set
            {
                switch (regionIndex)
                {
                    case 0: Color1 = value; break;
                    case 1: Color2 = value; break;
                    case 2: Color3 = value; break;
                    case 3: Color4 = value; break;
                }
            }
        }
    }
}
