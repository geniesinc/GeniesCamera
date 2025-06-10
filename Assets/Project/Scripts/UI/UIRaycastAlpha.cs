using UnityEngine;
using UnityEngine.UI;

// This means the Image component will only respond to raycasts if the
// alpha value of the pixel under the pointer is greater than threshold.
[RequireComponent(typeof(Image))]
public class UIRaycastAlpha : MonoBehaviour
{
    const float threshold = 0.5f;
    void Start()
    {
        var img = GetComponent<Image>();
        img.alphaHitTestMinimumThreshold = threshold;
    }
}