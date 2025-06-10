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

    public AudioSource audioSource;           // 🔊 Audio source component
    public AudioClip bootupSound;             // 🎵 Boot-up sound clip
    public AudioClip peterIntroVoiceLine;     // 🗣️ Peter voice clip

    public float fadeDuration = 1f;
    public float displayDuration = 2.5f;

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

        // Wait briefly on black screen
        loadingText.gameObject.SetActive(true);
        progressBarContainer.SetActive(true);
        yield return StartCoroutine(FakeLoadingBar());

        // 🔊 Play bootup sound
        if (bootupSound != null)
        {
            audioSource.PlayOneShot(bootupSound);
            yield return new WaitForSeconds(bootupSound.length);
        }

        // 🗣️ Play Peter's intro voice line
        if (peterIntroVoiceLine != null)
        {
            audioSource.PlayOneShot(peterIntroVoiceLine);
            yield return new WaitForSeconds(peterIntroVoiceLine.length + 0.5f);
        }

        // 🎬 Load the main scene
        SceneManager.LoadScene("SampleScene"); // Replace with your actual scene name
    }

    IEnumerator FakeLoadingBar()
    {
        float t = 0f;
        while (t < loadingDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / loadingDuration);
            progressBarFill.localScale = new Vector3(progress, 1f, 1f); // this now stretches from the left
            yield return null;
        }

        // hide when done
        loadingText.gameObject.SetActive(false);
        progressBarContainer.SetActive(false);
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
}
