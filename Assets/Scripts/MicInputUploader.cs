using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MicInputUploader : MonoBehaviour
{
    [Header("Server")]
    public string flaskURL = "http://localhost:5000/speak";

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
    public int sampleRate = 16000;
    public float startThreshold = 0.01f;
    public float stopThreshold = 0.005f;
    public float silenceDuration = 1.0f;

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;
    private float silenceTimer = 0f;

    [Header("UI – Mic Icon")]
    public Image micIcon;

    [Header("Memory Thinking UI")]
    public CanvasGroup memoryPanelGroup;
    public TextMeshProUGUI memoryText;

    void Start()
    {
        if (yesButton) yesButton.onClick.AddListener(KillPeter);
        if (cancelButton) cancelButton.onClick.AddListener(() => confirmPanel.SetActive(false));

        confirmPanel?.SetActive(false);
        memoryPanelGroup?.gameObject.SetActive(false);
        subtitleText.alpha = 0;

        micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, true, 10, sampleRate);
    }

    void Update()
    {
        if (!Microphone.IsRecording(micDevice)) return;

        float[] samples = new float[128];
        int micPos = Microphone.GetPosition(micDevice) - samples.Length;
        if (micPos < 0) return;

        recordedClip.GetData(samples, micPos);
        float volume = GetAverageVolume(samples);

        if (!isRecording && volume > startThreshold)
        {
            Debug.Log("🎤 Voice detected. Start recording.");
            isRecording = true;
            silenceTimer = 0f;
            if (micIcon != null) micIcon.color = Color.red;
        }
        else if (isRecording)
        {
            if (volume < stopThreshold)
                silenceTimer += Time.deltaTime;
            else
                silenceTimer = 0f;

            if (silenceTimer >= silenceDuration)
            {
                Debug.Log("⏹️ Silence detected. Stop recording.");
                StopRecordingAndSend();
            }
        }
    }

    float GetAverageVolume(float[] data)
    {
        float sum = 0f;
        foreach (float sample in data)
            sum += Mathf.Abs(sample);
        return sum / data.Length;
    }

    void StopRecordingAndSend()
    {
        isRecording = false;
        int samplePos = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);

        AudioClip finalClip = AudioClip.Create("mic_input", samplePos, 1, sampleRate, false);
        float[] audioData = new float[samplePos];
        recordedClip.GetData(audioData, 0);
        finalClip.SetData(audioData, 0);

        byte[] wavData = SaveWavUtility.FromAudioClip("mic_input", finalClip);

        if (micIcon != null) micIcon.color = Color.white;

        StartCoroutine(ShowMemoryThinking("Zoeken in geheugen…"));
        StartCoroutine(SendWavToFlask(wavData));

        recordedClip = Microphone.Start(micDevice, true, 10, sampleRate);
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

        StopAllCoroutines(); // Stop "thinking" + subtitle typing
        StartCoroutine(ShowSubtitle(res.response));

        Debug.Log($"🧠 Emotion: {res.emotion}");
        Debug.Log($"🤖 Text: {res.response}");

        PeterPortrait portrait = FindAnyObjectByType<PeterPortrait>();
        PeterAudioPlayer audioPlayer = FindAnyObjectByType<PeterAudioPlayer>();

        portrait?.SetEmotion(res.emotion);
        portrait?.SetTalking();

        if (audioPlayer != null)
        {
            audioPlayer.PlayBase64MP3(res.audio_base64);
            StartCoroutine(ResetToIdleAfterSpeech(portrait, 3.5f)); // estimate duration
        }
    }

    IEnumerator ResetToIdleAfterSpeech(PeterPortrait portrait, float delay)
    {
        yield return new WaitForSeconds(delay + 0.2f);
        portrait?.SetIdle();
    }

    IEnumerator ShowSubtitle(string text)
    {
        subtitleText.text = "";
        subtitleText.alpha = 1;
        float delay = 0.04f;

        foreach (char c in text)
        {
            subtitleText.text += c;
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(subtitleDuration);
        subtitleText.alpha = 0;
    }

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
