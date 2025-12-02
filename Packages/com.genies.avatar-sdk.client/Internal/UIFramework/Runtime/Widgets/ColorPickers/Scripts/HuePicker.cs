using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// Slider for picking hue.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class HuePicker : MonoBehaviour
    {
        public bool Interactable
        {
            get
            {
                return slider != null && slider.interactable;
            }
            set
            {
                if (slider != null)
                {
                    slider.interactable = value;
                }
            }
        }

        public Slider Slider => slider;
        [SerializeField] private Slider slider;

        public ColorHandle ColorHandle => colorHandle;
        [SerializeField] private ColorHandle colorHandle;

        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        internal void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            slider ??= GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            colorHandle ??= GetComponentInChildren<ColorHandle>();
            colorHandle.Initialize();
        }
    }
}
