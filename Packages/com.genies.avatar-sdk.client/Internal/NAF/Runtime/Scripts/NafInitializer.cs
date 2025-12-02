using UnityEngine;

namespace Genies.Naf
{
    /**
     * Simple component to auto-initialize the NAF plugin in your scene.
     */
    public sealed class NafInitializer : MonoBehaviour
    {
        [SerializeField] private NafSettings settings;

        [Tooltip("If true, the NAF plugin will be initialized on Start. If false, it will be initialized on Awake")]
        [SerializeField] private bool initializeOnStart;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject.transform);
            if (!initializeOnStart)
            {
                NafPlugin.Initialize(settings);
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                NafPlugin.Initialize(settings);
            }
        }
    }
}
