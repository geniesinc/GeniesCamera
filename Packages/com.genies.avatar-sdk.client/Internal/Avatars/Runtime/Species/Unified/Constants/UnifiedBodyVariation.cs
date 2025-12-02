using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the body variations available for the unified species.
    /// </summary>
    public static class UnifiedBodyVariation
    {
        public const string Male = "male";
        public const string Female = "female";
        public const string Gap = "gap";

        public static readonly IReadOnlyList<string> All = new List<string>
        {
            Male,
            Female,
            Gap
        }.AsReadOnly();
    }
}