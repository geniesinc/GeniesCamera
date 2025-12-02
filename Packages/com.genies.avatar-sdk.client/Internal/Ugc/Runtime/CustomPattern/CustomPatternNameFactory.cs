using System;
using Genies.CrashReporting;

namespace Genies.Ugc.CustomPattern
{
    public static class CustomPatternNameFactory
    {
        private const string _customPatternKey = "custom-pattern";
        private const int _validLenghtForUserId = 36;
        private const int _validSplitPartsCount = 4;
        public static string CreateNewName(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                CrashReporter.LogError($"[{nameof(CustomPatternNameFactory)}] Invalid Custom Pattern Name {userId}");
                return null;
            }

            return $"{_customPatternKey}_{userId}_{DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss.fffffffK")}_{Guid.NewGuid().ToString()}";
        }

        public static string GetUserIdFromCustomPatternId(string patternId)
        {
            if (string.IsNullOrEmpty(patternId))
            {
                CrashReporter.LogError($"[{nameof(CustomPatternNameFactory)}] Invalid Pattern Id {patternId}");
                return null;
            }

            //check if the raw string breaks correctly
            var validSplitParts = patternId.Split("_");
            if (validSplitParts.Length != _validSplitPartsCount)
            {
                return null;
            }

            //getting the user id from the split parts
            var filteredSearch = validSplitParts[1];
            //we have to check if its a valid user id
            if (filteredSearch.Length < _validLenghtForUserId)
            {
                return null;
            }

            return filteredSearch;
        }
    }
}
