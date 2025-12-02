namespace Genies.Ugc
{
    /// <summary>
    /// Used by <see cref="WearableRandomizer"/> to generate random material/pattern IDs and scales for UGC split regions.
    /// </summary>
    internal struct RegionTextures
    {
        public string Id1;
        public float Scale1;
        public string Id2;
        public float Scale2;
        public string Id3;
        public float Scale3;
        public string Id4;
        public float Scale4;

        public RegionTextures(string id1, float scale1, string id2, float scale2, string id3, float scale3, string id4, float scale4)
        {
            Id1 = id1;
            Scale1 = scale1;
            Id2 = id2;
            Scale2 = scale2;
            Id3 = id3;
            Scale3 = scale3;
            Id4 = id4;
            Scale4 = scale4;
        }

        public string GetId(int regionIndex)
        {
            return regionIndex switch
            {
                0 => Id1,
                1 => Id2,
                2 => Id3,
                3 => Id4,
                _ => default,
            };
        }

        public void SetId(int regionIndex, string id)
        {
            switch (regionIndex)
            {
                case 0: Id1 = id; break;
                case 1: Id2 = id; break;
                case 2: Id3 = id; break;
                case 3: Id4 = id; break;
            }
        }

        public float GetScale(int regionIndex)
        {
            return regionIndex switch
            {
                0 => Scale1,
                1 => Scale2,
                2 => Scale3,
                3 => Scale4,
                _ => default,
            };
        }

        public void SetScale(int regionIndex, float scale)
        {
            switch (regionIndex)
            {
                case 0: Scale1 = scale; break;
                case 1: Scale2 = scale; break;
                case 2: Scale3 = scale; break;
                case 3: Scale4 = scale; break;
            }
        }
    }
}
