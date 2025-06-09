using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MicInputUploader : MonoBehaviour
{
    public string flaskURL = "http://localhost:5000/speak";
    public PeterOrb peterOrb;

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

    public Button startButton;
    public Button stopButton;

    void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartMicRecording);
        if (stopButton != null)
            stopButton.onClick.AddListener(StopRecordingAndSend);

        if (startButton != null) startButton.interactable = true;
        if (stopButton != null) stopButton.interactable = false;
    }

    [ContextMenu("Start Mic Recording")]
    public void StartMicRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("🎙️ No microphone found!");
            return;
        }

        micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, false, 5, 16000);
        isRecording = true;
        Debug.Log("🎙️ Recording started...");

        if (startButton != null) startButton.interactable = false;
        if (stopButton != null) stopButton.interactable = true;
    }

    [ContextMenu("Stop and Send")]
    public void StopRecordingAndSend()
    {
        if (!isRecording) return;

        Microphone.End(micDevice);
        isRecording = false;
        Debug.Log("🎙️ Recording stopped. Converting to WAV...");

        if (startButton != null) startButton.interactable = true;
        if (stopButton != null) stopButton.interactable = false;

        byte[] wavData = SaveWavUtility.FromAudioClip("mic_input", recordedClip);
        StartCoroutine(SendWavToFlask(wavData));
    }

    IEnumerator SendWavToFlask(byte[] wavData)
    {
        UnityWebRequest request = new UnityWebRequest(flaskURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(wavData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "audio/wav");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Upload failed: " + request.error);
            yield break;
        }

        EmotionResponse response = JsonUtility.FromJson<EmotionResponse>(request.downloadHandler.text);

        if (response != null && !string.IsNullOrEmpty(response.audio_base64))
        {
            Debug.Log($"🧠 Emotion: {response.emotion}");
            Debug.Log($"🤖 Text: {response.response}");

            if (peterOrb != null)
                peterOrb.SetEmotion(response.emotion);

            // ✅ NEW: Play audio with FMOD
            var audioPlayer = FindObjectOfType<PeterAudioPlayer>();
            if (audioPlayer != null)
            {
                audioPlayer.PlayBase64MP3(response.audio_base64);
            }
            else
            {
                Debug.LogError("❌ PeterAudioPlayer not found in scene.");
            }

            // Optional: trigger visual pulse
            if (peterOrb != null)
                peterOrb.StartOrbPulse();
        }
        else
        {
            Debug.LogError("❌ Invalid response or missing audio.");
        }
    }

    [Serializable]
    public class EmotionResponse
    {
        public string emotion;
        public string response;
        public string audio_base64;
    }
}
