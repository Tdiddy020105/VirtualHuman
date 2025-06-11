using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroSequence : MonoBehaviour
{
    public CanvasGroup fadeTextGroup;
    public TextMeshProUGUI fadeText;
    public GameObject startButton;

    public TextMeshProUGUI loadingText;
    public RectTransform progressBarFill;
    public GameObject progressBarContainer;
    public float loadingDuration = 2.5f;

    public AudioSource audioSource;
    public AudioClip bootupSound;
    public AudioClip peterIntroVoiceLine;

    public float fadeDuration = 1f;
    public float displayDuration = 2.5f;

    public CanvasGroup sceneFadeOverlay;

    [Header("Subtitle")]
    public TextMeshProUGUI subtitleText;
    public CanvasGroup subtitleGroup;
    public float typingSpeed = 0.04f;

    private string[] messages = {
        "Dit is Peter Dielesen, geboren in 1841. Hij is terug tot leven gebracht doormiddel van data.",
        "Hij is overleden in 1918. Vandaag leeft ie weer.",
        "Wat als dit jouw familielid was? Zou je dit ok vinden?"
    };

    public void StartSimulation()
    {
        Debug.Log("✅ Start clicked");
        startButton.SetActive(false);
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        foreach (string msg in messages)
        {
            fadeText.text = msg;
            yield return FadeCanvas(0, 1);
            yield return new WaitForSeconds(displayDuration);
            yield return FadeCanvas(1, 0);
        }

        // Show fake loading bar
        loadingText.gameObject.SetActive(true);
        progressBarContainer.SetActive(true);
        yield return StartCoroutine(FakeLoadingBar());

        // 🔊 Play bootup sound (no subtitle here!)
        if (bootupSound != null)
        {
            audioSource.PlayOneShot(bootupSound);
            yield return new WaitForSeconds(bootupSound.length);
        }

        // 🗣️ Play Peter's intro voice line + subtitle
        if (peterIntroVoiceLine != null)
        {
            audioSource.PlayOneShot(peterIntroVoiceLine);
            yield return StartCoroutine(ShowSubtitle("Hallo...? Is iemand daar? Waar ben ik...?"));
        }

        // Transition to next scene
        yield return StartCoroutine(FadeToBlack(1.5f));
        SceneManager.LoadScene("SampleScene");
    }

    IEnumerator FakeLoadingBar()
    {
        float t = 0f;
        while (t < loadingDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / loadingDuration);
            progressBarFill.localScale = new Vector3(progress, 1f, 1f);
            yield return null;
        }

        loadingText.gameObject.SetActive(false);
        progressBarContainer.SetActive(false);
    }

    IEnumerator ShowSubtitle(string fullText)
    {
        subtitleText.text = "";
        subtitleGroup.alpha = 1;

        foreach (char c in fullText)
        {
            subtitleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(1f);
        subtitleGroup.alpha = 0;
    }

    IEnumerator FadeCanvas(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / fadeDuration);
            fadeTextGroup.alpha = alpha;
            yield return null;
        }
        fadeTextGroup.alpha = to;
    }

    IEnumerator FadeToBlack(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            sceneFadeOverlay.alpha = Mathf.Lerp(0, 1, t / duration);
            yield return null;
        }
        sceneFadeOverlay.alpha = 1;
    }
}
