using UnityEngine;
using UnityEngine.SceneManagement;

namespace Genies.Utilities
{
    /*
     * Don't destroy this object when loading a level
     * Script can be added to prefabs or objects in a scene
     */
    public class DontDestroyOnLoad : MonoBehaviour
    {
        [Tooltip("Should this object destroy the duplicates of itself if it reenters the scene it was created?")] [SerializeField]
        private bool destroyDuplicatesOnSceneReenter = false;

        // Start is called before the first frame update
        private void Start()
        {
            if (destroyDuplicatesOnSceneReenter)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // if there is another gameobject with the same name that is not this, destroy it
            DontDestroyOnLoad[] scripts = FindObjectsByType<DontDestroyOnLoad>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (DontDestroyOnLoad script in scripts)
            {
                GameObject g = script.gameObject;
                if (g.name == gameObject.name && g != gameObject)
                {
                    Destroy(g.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (destroyDuplicatesOnSceneReenter)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
    }
}
