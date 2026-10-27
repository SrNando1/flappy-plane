using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Clarão que cobre o ecrã inteiro e desvanece.</summary>
[RequireComponent(typeof(Image))]
public class ScreenFlash : MonoBehaviour
{
    Image image;
    Coroutine running;

    void Awake()
    {
        image = GetComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0f);
    }

    public void Flash(Color color, float duration)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run(color, duration));
    }

    IEnumerator Run(Color color, float duration)
    {
        float age = 0f;

        while (age < duration)
        {
            age += Time.deltaTime;
            float a = Mathf.Lerp(color.a, 0f, age / duration);
            image.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }

        image.color = new Color(color.r, color.g, color.b, 0f);
        running = null;
    }
}
