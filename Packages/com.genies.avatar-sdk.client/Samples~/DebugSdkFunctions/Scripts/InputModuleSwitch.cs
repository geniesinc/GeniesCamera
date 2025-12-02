using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Genies.Sdk.Avatar.Samples.DebugSdkFunctions
{
    public class InputModuleSwitch : MonoBehaviour
    {
        [SerializeField] private InputSystemUIInputModule _inputSystemUIInputModule;
        [SerializeField] private StandaloneInputModule _standaloneInputModule;

        private InputSystemUIInputModule InputSystemModuleInstance => _inputSystemUIInputModule;
        private StandaloneInputModule StandaloneModuleInstance => _standaloneInputModule;

        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            if (InputSystemModuleInstance != null)
            {
                InputSystemModuleInstance.enabled = true;
            }

            if (StandaloneModuleInstance != null)
            {
                Destroy(StandaloneModuleInstance);
            }
#else
            if (StandaloneModuleInstance != null)
            {
                StandaloneModuleInstance.enabled = true;
            }

            if (InputSystemModuleInstance != null)
            {
                Destroy(InputSystemModuleInstance);
            }
#endif
        }
    }
}
