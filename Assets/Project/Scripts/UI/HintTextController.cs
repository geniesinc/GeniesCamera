using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintTextController : MonoBehaviour
{
    public TextMeshProUGUI hintText;

    float displayTime = 1.5f;
    float fadeTime = 0.5f;

    // Update is called once per frame
    void Start()
    {
        StartCoroutine(FadeAway());
    }

    IEnumerator FadeAway()
    {
        yield return new WaitForSeconds(displayTime);

        Color startColor = hintText.color;
        Color endColor = hintText.color;
        endColor.a = 0;

        float currTime = 0f;
        while(currTime < fadeTime)
        {
            hintText.color = Color.Lerp(startColor, endColor, currTime / fadeTime);

            currTime += Time.deltaTime;
            yield return null;
        }

        hintText.color = endColor;
        yield return null;

        Destroy(gameObject);
    }
}
