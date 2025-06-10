using UnityEngine;
using UnityEngine.UI;

public class LoadingDots : MonoBehaviour
{
    [SerializeField] private RawImage[] dots;
    [SerializeField] private Color[] colors;
    [SerializeField] private float speed = 1f;

    private float timer = 0f;

    void Update()
    {
        dots[0].color = Color.Lerp(colors[0], colors[1], Mathf.PingPong(timer, 1));
        dots[1].color = Color.Lerp(colors[0], colors[1], Mathf.PingPong(timer + 0.5f, 1));
        dots[2].color = Color.Lerp(colors[0], colors[1], Mathf.PingPong(timer + 1f, 1));

        timer += (Time.deltaTime * speed);
        timer %= 2f;
    }

    public void SetColors(Color startColor, Color endColor)
    {
        colors = new Color[dots.Length];
        for(int i=0; i < colors.Length; i++)
        {
            colors[i] = Color.Lerp(startColor, endColor, i / (float)(colors.Length - 1));
        }
    }
}