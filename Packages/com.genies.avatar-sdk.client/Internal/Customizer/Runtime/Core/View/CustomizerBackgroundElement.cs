using Cysharp.Threading.Tasks;
using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework
{
    public class CustomizerBackgroundElement : MonoBehaviour
    {
        [SerializeField]
        private Image _bg;

        public async UniTask AnimateColor(Color targetColor, float duration, Ease ease, bool immediate = false)
        {
            _bg.Terminate();

            if (immediate)
            {
                _bg.color = targetColor;
                return;
            }

            // Springs provide natural color transitions (ease parameter ignored for springs)
            await _bg.SpringColor(targetColor, SpringPhysics.Presets.Smooth);
        }
    }
}
