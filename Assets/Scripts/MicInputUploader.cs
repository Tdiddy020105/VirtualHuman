using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;   // for Kill action
using UnityEngine.UI;
using TMPro;

public class MicInputUploader : MonoBehaviour
{
    [Header("Server + Orb")]
    public string flaskURL = "http://localhost:5000/speak";
    public PeterOrb peterOrb;

    [Header("UI – main buttons")]
    public Button startButton;            // “Praat met Peter”
    public Button stopButton;             // “Stop / Kill”

    [Header("UI – confirm dialog")]
    public GameObject confirmPanel;       // inactive by default
    public Button yesButton;
    public Button cancelButton;

    public GameObject endPanel;         
    public TextMeshProUGUI endText;      
    public float endDuration = 2f;        

    [Header("Recording settings")]
    public int recordSeconds = 5;
    public int sampleRate    = 16000;

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

    void Start()
    {
        // Button wiring
        if (startButton) startButton.onClick.AddListener(() => StartCoroutine(RecordAndSend()));
        if (stopButton)  stopButton.onClick.AddListener(OpenConfirm);

        if (yesButton)    yesButton.onClick.AddListener(KillPeter);
        if (cancelButton) cancelButton.onClick.AddListener(() => confirmPanel.SetActive(false));

        confirmPanel?.SetActive(false);
    }

    IEnumerator RecordAndSend()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("🎙️ No microphone found!");
            yield break;
        }
        if (isRecording) yield break;          // already busy

        // Disable talk-button during capture
        startButton.interactable = false;
        micDevice     = Microphone.devices[0];
        recordedClip  = Microphone.Start(micDevice, false, recordSeconds, sampleRate);
        isRecording   = true;
        Debug.Log("🎙️ Recording…");

        yield return new WaitForSeconds(recordSeconds);

        // Stop + convert
        Microphone.End(micDevice);
        isRecording = false;
        Debug.Log("🎙️ Recording finished. Converting to WAV…");

        byte[] wavData = SaveWavUtility.FromAudioClip("mic_input", recordedClip);
        startButton.interactable = true;       // re-enable for next round
        yield return StartCoroutine(SendWavToFlask(wavData));
    }

    IEnumerator SendWavToFlask(byte[] wavData)
    {
        UnityWebRequest req = new UnityWebRequest(flaskURL, "POST")
        {
            uploadHandler   = new UploadHandlerRaw(wavData),
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

        Debug.Log($"🧠 Emotion: {res.emotion}");
        Debug.Log($"🤖 Text: {res.response}");

        peterOrb?.SetEmotion(res.emotion);
        FindObjectOfType<PeterAudioPlayer>()?.PlayBase64MP3(res.audio_base64);
        peterOrb?.StartOrbPulse();
    }

    void OpenConfirm() => confirmPanel.SetActive(true);

    void KillPeter()
    {
        confirmPanel.SetActive(false);     // hide confirm
        StartCoroutine(EndSequence());
    }

    IEnumerator EndSequence()
    {
        // show creepy overlay
        endPanel.SetActive(true);

        // optional: glitch the text a few times
        float t = 0f;
        while (t < endDuration)
        {
            t += 0.2f;
            endText.text = UnityEngine.Random.value > 0.5f ? "Deleting Peter…" : "D e l e t i n g  P e t e r . . .";
            yield return new WaitForSeconds(0.2f);
        }

        // fade or wait, then reload intro
        SceneManager.LoadScene("IntroScene");
    }

    [Serializable] public class EmotionResponse
    {
        public string emotion;
        public string response;
        public string audio_base64;
    }
}
