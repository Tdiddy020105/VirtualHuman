using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MicInputUploader : MonoBehaviour
{
    [Header("Server + Orb")]
    public string flaskURL = "http://localhost:5000/speak";
    public PeterOrb peterOrb;

    [Header("UI – main buttons")]
    public Button startButton;
    public Button stopButton;

    [Header("UI – confirm dialog")]
    public GameObject confirmPanel;
    public Button yesButton;
    public Button cancelButton;

    [Header("UI – Subtitles")]
    public TextMeshProUGUI subtitleText;
    public float subtitleDuration = 5f;

    [Header("End Screen")]
    public GameObject endPanel;
    public TextMeshProUGUI endText;
    public float endDuration = 2f;

    [Header("Recording settings")]
    public int recordSeconds = 5;
    public int sampleRate = 16000;

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

    [Header("UI – Mic Icon")]
    public Image micIcon; 

    [Header("Memory Thinking UI")]
    public CanvasGroup memoryPanelGroup;
    public TextMeshProUGUI memoryText;

    void Start()
    {
        if (startButton) startButton.onClick.AddListener(() => StartCoroutine(RecordAndSend()));
        if (stopButton)  stopButton.onClick.AddListener(OpenConfirm);
        if (yesButton)   yesButton.onClick.AddListener(KillPeter);
        if (cancelButton) cancelButton.onClick.AddListener(() => confirmPanel.SetActive(false));

        confirmPanel?.SetActive(false);
        memoryPanelGroup?.gameObject.SetActive(false);
        subtitleText.alpha = 0; // Make sure subtitle starts hidden
    }

    IEnumerator ShowMemoryThinking(string content)
    {
        memoryText.text = content;
        memoryPanelGroup.alpha = 0;
        memoryPanelGroup.gameObject.SetActive(true);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            memoryPanelGroup.alpha = Mathf.Lerp(0, 1, t / 0.5f);
            yield return null;
        }

        memoryPanelGroup.alpha = 1;
    }

    IEnumerator RecordAndSend()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("🎙️ No microphone found!");
            yield break;
        }
        if (isRecording) yield break;

        startButton.interactable = false;
        micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, false, recordSeconds, sampleRate);
        isRecording = true;
        Debug.Log("🎙️ Recording…");

        // 🔴 Turn mic icon red
        if (micIcon != null) micIcon.color = Color.red;

        yield return new WaitForSeconds(recordSeconds);

        Microphone.End(micDevice);
        isRecording = false;
        Debug.Log("🎙️ Recording finished.");

        // ⚪ Reset mic icon color
        if (micIcon != null) micIcon.color = Color.white;

        byte[] wavData = SaveWavUtility.FromAudioClip("mic_input", recordedClip);
        startButton.interactable = true;

        StartCoroutine(ShowMemoryThinking("Zoeken in geheugen…"));
        yield return StartCoroutine(SendWavToFlask(wavData));
        memoryPanelGroup.gameObject.SetActive(false);
    }

    IEnumerator SendWavToFlask(byte[] wavData)
    {
        UnityWebRequest req = new UnityWebRequest(flaskURL, "POST")
        {
            uploadHandler = new UploadHandlerRaw(wavData),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "audio/wav");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Upload failed: " + req.error);
            yield break;
        }

        EmotionResponse res = JsonUtility.FromJson<EmotionResponse>(req.downloadHandler.text);
        if (res == null || string.IsNullOrEmpty(res.audio_base64))
        {
            Debug.LogError("❌ Invalid response or missing audio.");
            yield break;
        }

        if (subtitleText != null)
        {
            StopAllCoroutines(); // prevent subtitle overlap and memoryPanel conflict
            StartCoroutine(ShowSubtitle(res.response));
        }

        Debug.Log($"🧠 Emotion: {res.emotion}");
        Debug.Log($"🤖 Text: {res.response}");

        peterOrb?.SetEmotion(res.emotion);
        memoryPanelGroup.gameObject.SetActive(false);
        FindObjectOfType<PeterAudioPlayer>()?.PlayBase64MP3(res.audio_base64);
        peterOrb?.StartOrbPulse();
    }

    IEnumerator ShowSubtitle(string text)
    {
        subtitleText.text = "";
        subtitleText.alpha = 1;

        float delay = 0.04f; // typing speed (seconds per character)

        for (int i = 0; i < text.Length; i++)
        {
            subtitleText.text += text[i];
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(subtitleDuration);

        subtitleText.alpha = 0;
    }

    void OpenConfirm() => confirmPanel.SetActive(true);

    void KillPeter()
    {
        confirmPanel.SetActive(false);
        StartCoroutine(EndSequence());
    }

    IEnumerator EndSequence()
    {
        endPanel.SetActive(true);

        float t = 0f;
        while (t < endDuration)
        {
            t += 0.2f;
            endText.text = UnityEngine.Random.value > 0.5f
                ? "Deleting Peter…"
                : "D e l e t i n g  P e t e r . . .";
            yield return new WaitForSeconds(0.2f);
        }

        SceneManager.LoadScene("IntroScene");
    }

    [Serializable]
    public class EmotionResponse
    {
        public string emotion;
        public string response;
        public string audio_base64;
    }
}
