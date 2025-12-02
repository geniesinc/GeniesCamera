using System;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IBodyVariationController : IBodyController
    {
        //body variations
        string CurrentVariation { get; }
        new event Action Updated;
        UniTask SetBodyVariationAsync(string bodyVariation);

        // deprecated methods to keep compatibility with chaos mode presets
        GSkelModifierPreset GetCurrentBodyAsPreset();
        UniTask SetPresetAsync(GSkelModifierPreset preset);
    }
}
