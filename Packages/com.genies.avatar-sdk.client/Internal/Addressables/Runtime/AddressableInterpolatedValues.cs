namespace Genies.Addressables
{
    /// <summary>
    /// These are strings values used for addressable interpolated versioning through the catalog merge utility
    /// </summary>
    public static class AddressableInterpolatedValues
    {
        //This is the string that the version Int will be replaced by in the 'InternalId'
        public const string InterpolatedVersionString = "VersionInt";

        //This is the add to the current key to locate the new interpolated entry
        public const string InterpolatedVersionKeyAddString = "_v";

        //this is a dummy replace string used to avoid collisions on the primary key
        //for duplicating the same catalog
        public const string InterpolatedReplaceString = "_EmptyReplace1";

        //This is a helper add that avoid dup deps when duplicating entries
        public const string InterpolatedInternalIdKeyAddString = "_abp_v";
    }
}
