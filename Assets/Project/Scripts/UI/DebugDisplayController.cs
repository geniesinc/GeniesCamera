using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugDisplayController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugTextL;
    [SerializeField] private TextMeshProUGUI debugTextR;

    // Fade this feature away
    [SerializeField] private RawImage[] imagesToFade;
    private Color[] imagesOriginalColor;
    [SerializeField] private TextMeshProUGUI[] textsToFade;
    private Color[] textsOriginalColor;
    private float fadeTime = 0.75f;

    private int frameCacheLength = 60;
    private List<float> frameCache = new List<float>();

    private int maxLinesPerColumn = 26;

    private void Start()
    {
        // Setup Caches
        imagesOriginalColor = new Color[imagesToFade.Length];
        for (int i = 0; i < imagesToFade.Length; i++)
        {
            imagesOriginalColor[i] = imagesToFade[i].color;
        }
        textsOriginalColor = new Color[textsToFade.Length];
        for (int i = 0; i < textsOriginalColor.Length; i++)
        {
            textsOriginalColor[i] = textsToFade[i].color;
        }
    }

    public void SetText(string text)
    {        
        // Split the string by the newline character
        string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
        Array.Sort(lines, StringComparer.Ordinal);

        // Create the first string with the first N lines
        debugTextL.text = string.Join("\n", lines.Take(maxLinesPerColumn));

        // Create the second string with the remaining lines
        debugTextR.text = "\n" + string.Join("\n", lines.Skip(maxLinesPerColumn));
    }

    private string GetFpsString()
    {
        frameCache.Add(Time.unscaledDeltaTime);
        if (frameCache.Count > frameCacheLength)
        {
            frameCache.RemoveAt(0);
        }

        float totalTimeOfAllFrames = 0f;
        foreach (float frame in frameCache)
        {
            totalTimeOfAllFrames += frame;
        }
        // 1 frame divided by stored total of 0.01 deltaTime = 100 fps
        return (frameCache.Count / totalTimeOfAllFrames).ToString("#.##");
    }

    private IEnumerator FadeAndTurnOff()
    {
        // Fade colors away
        float currTime = 0;
        while(currTime < fadeTime)
        {
            for (int i = 0; i < imagesToFade.Length; i++)
            {
                imagesToFade[i].color = Color.Lerp(imagesOriginalColor[i], Color.clear, currTime / fadeTime);
            }
            for (int i = 0; i < textsToFade.Length; i++)
            {
                textsToFade[i].color = Color.Lerp(textsOriginalColor[i], Color.clear, currTime / fadeTime);
            }
            currTime += Time.deltaTime;
            yield return null;
        }

        // Turn off and reset colors
        gameObject.SetActive(false);
        for (int i = 0; i < imagesToFade.Length; i++)
        {
            imagesToFade[i].color = imagesOriginalColor[i];
        }
        for (int i = 0; i < textsToFade.Length; i++)
        {
            textsToFade[i].color = textsOriginalColor[i];
        }
    }
}
