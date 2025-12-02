using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    public interface IDefinitionConverter
    {
        /// <summary>
        /// Expose the target to use as safe check if it needs to convert or not
        /// </summary>
        string TargetVersion { get; }

        void GetSupportedConversionTargets(ICollection<DefinitionConversionTarget> results);
        bool SupportsConversionTarget(DefinitionConversionTarget target);
        UniTask<DefinitionToken> ConvertAsync(DefinitionToken definition, string targetVersion);
    }
}
