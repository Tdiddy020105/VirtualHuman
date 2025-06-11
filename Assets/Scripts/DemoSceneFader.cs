using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DemoSceneFader : MonoBehaviour
{
    public CanvasGroup orbGroup;       // drag the orb here
    public CanvasGroup uiGroup;        // drag your UI parent canvas here (with buttons etc.)
    public float fadeDuration = 1.5f;

    void Start()
    {
        // start fully invisible
        orbGroup.alpha = 0f;
        uiGroup.alpha = 0f;

        StartCoroutine(FadeInSequence());
    }

    IEnumerator FadeInSequence()
    {
        // Wait briefly if you want to delay start
        yield return new WaitForSeconds(0.5f);

        // Fade in orb
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            orbGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        // Optional delay
        yield return new WaitForSeconds(0.5f);

        // Fade in UI
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            uiGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
    }
}
