using Genies.CrashReporting;

using UnityEngine;

namespace Genies.Utilities
{
    public static class GameObjectExtensions
    {
        public static void SetLayerRecursive(this GameObject gameObject, int layer)
        {
            if (layer < 0 || layer > 31)
            {
                CrashReporter.LogWarning($"Tried to set an invalid layer {layer} for GameObject {gameObject.name}. Aborting.");
                return;
            }
            
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        public static void ClampOnScreen(this GameObject uiObject, RectTransform canvasRect)
        {
            RectTransform rect = uiObject.GetComponent<RectTransform>();

            if (rect != null)
            {
                Vector3 pos = rect.localPosition;

                Vector3 minPosition = canvasRect.rect.min - rect.rect.min;
                Vector3 maxPosition = canvasRect.rect.max - rect.rect.max;

                pos.x = Mathf.Clamp(rect.localPosition.x, minPosition.x, maxPosition.x);
                pos.y = Mathf.Clamp(rect.localPosition.y, minPosition.y, maxPosition.y);

                rect.localPosition = pos;
            }
            else
            {
                CrashReporter.Log("uiObject doesn't have a RectTransform", LogSeverity.Error);
            }
        }
    }
}
