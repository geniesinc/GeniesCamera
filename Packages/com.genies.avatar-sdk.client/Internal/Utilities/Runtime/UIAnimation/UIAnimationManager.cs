using UnityEngine;

namespace Genies.UI.Animations
{
    /// <summary>
    /// Singleton manager for running animations. Persists across scenes and handles coroutines
    /// even when target objects are inactive.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class UIAnimationManager : MonoBehaviour
    {
        private static UIAnimationManager _instance;

        public static UIAnimationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create new instance
                    var go = new GameObject("UIAnimationManager");
                    _instance = go.AddComponent<UIAnimationManager>();
                    DontDestroyOnLoad(go);
                }
                
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}

