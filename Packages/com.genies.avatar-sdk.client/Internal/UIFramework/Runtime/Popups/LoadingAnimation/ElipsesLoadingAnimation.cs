using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    public class ElipsesLoadingAnimation : MonoBehaviour
    {
        [FormerlySerializedAs("loadingDots")] public Image[] LoadingDots;
        [FormerlySerializedAs("loopTime")] public float LoopTime;

        private void OnEnable()
        {
            for (var i = 0; i < LoadingDots.Length; i++)
            {
                StartCoroutine(LoadingLoopAnimation(LoopTime/LoadingDots.Length*i, LoadingDots[i]));
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator LoadingLoopAnimation(float startTime, Image image)
        {
            var timer = startTime;
            Color color = image.color;
            while (true)
            {
                color.a = Mathf.Cos(2 * Mathf.PI * timer / LoopTime) * 0.5f + 0.5f;
                image.color = color;
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }
}
