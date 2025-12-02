namespace Genies.AssetLocations
{
    public static class AssetLocationUtility
    {
        /// <summary>
        /// Returns a Uri string from the metadata of an AssetContainer (Naf Content)
        /// => WardrobeGear/recqmEyKPwQ58KLNF/manifest.bin?v=4&lod=lod1
        /// </summary>
        public static string ToContainerUri(string assetType, string guid, string version, string lod)
        {
            var baseUri = ToBaseUri(assetType, guid);
            // edge case no ? or & needed
            if (version == "0" && string.IsNullOrEmpty(lod))
            {
                return $"{baseUri}/manifest.bin";
            }

            // most common case!
            var partialUri = version == "0"?  $"{baseUri}/manifest.bin?" : $"{baseUri}/manifest.bin?v={version}";
            var uri = string.IsNullOrEmpty(lod) ? $"{partialUri}" : $"{partialUri}&lod={lod.TrimStart('_')}";
            return uri;
        }

        /// <summary>
        /// Return Uri string from metadata of an IconContainer (Naf Content) includes size in the address!
        /// => WardrobeGear/recqmEyKPwQ58KLNF_x1024/manifest.bin?v=4&amp;lod=lod2
        /// </summary>
        public static string ToIconContainerUri(string assetType, string guid, string version, string iconSize)
        {
            var baseUri = ToBaseUri(assetType, $"{guid}{iconSize}");
            var uri = version == "0" ? $"{baseUri}/manifest.bin" : $"{baseUri}/manifest.bin?v={version}";
            return uri;
        }

        /// <summary>
        /// Return full Url string from metadata of an AssetContainer (Naf Content)
        /// => https://genies-universal-content.s3.us-west-2.amazonaws.com/WardrobeGear/rec00Stgc5AgSUNQI/v6/manifest.bin
        /// </summary>
        public static string ToContainerFullUrl(string baseUrl, string assetType, string guid, string version, string lod)
        {
            var baseUri = ToBaseUriWithVersion(assetType, guid, version,baseUrl);
            var baseUriWithLod = string.IsNullOrEmpty(lod) ? $"{baseUri}" : $"{baseUri}/{lod.TrimStart('_')}";
            return $"{baseUriWithLod}/manifest.bin";
        }

        /// <summary>
        /// Return full Url string from metadata of an AssetContainer (Naf Content) includes size in the address!
        /// => https://genies-universal-content.s3.us-west-2.amazonaws.com/WardrobeGear/rec00Stgc5AgSUNQI_x512/v6/manifest.bin
        /// </summary>
        public static string ToIconFullUrl(string baseUrl, string assetType, string guid, string version, string iconSize)
        {
            var baseUri = ToBaseUriWithVersion(assetType, $"{guid}{iconSize}", version, baseUrl);
            return $"{baseUri}/manifest.bin";
        }

        // basePrefix can include creator or whole domain when building url eg. {domain}/{creator}
        private static string ToBaseUri(string assetType, string guid, string basePrefix = null)
        {
            return string.IsNullOrEmpty(basePrefix) ? $"{assetType}/{guid}" : $"{basePrefix}/{assetType}/{guid}";
        }

        private static string ToBaseUriWithVersion(string assetType, string guid, string version, string basePrefix = null)
        {
            var baseUrl = ToBaseUri(assetType, guid, basePrefix);
            return version == "0"? baseUrl : $"{baseUrl}/v{version}";
        }
    }
}
