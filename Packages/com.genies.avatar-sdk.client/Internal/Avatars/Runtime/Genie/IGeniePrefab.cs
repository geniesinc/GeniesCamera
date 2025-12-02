using UnityEngine;

namespace Genies.Avatars
{
    public interface IGeniePrefab
    {
        public IGenie Instantiate();
        public IGenie Instantiate(Transform parent);
        public IGenie Instantiate(Transform parent, bool worldPositionStays);
        public IGenie Instantiate(Vector3 position, Quaternion rotation);
        public IGenie Instantiate(Vector3 position, Quaternion rotation, Transform parent);
    }
}