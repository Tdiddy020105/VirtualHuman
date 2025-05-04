using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class MicInputUploader : MonoBehaviour
{
    public string flaskURL = "http://localhost:5000/speak";
    public AudioSource audioSource;
    public PeterOrb peterOrb;

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

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
    }

    [ContextMenu("Stop and Send")]
    public void StopRecordingAndSend()
    {
        if (!isRecording) return;

        Microphone.End(micDevice);
        isRecording = false;

        Debug.Log("🎙️ Recording stopped. Converting to WAV...");

        byte[] wavData = SaveWavUtility.FromAudioClip("mic_recording", recordedClip, false);

        StartCoroutine(SendWavToFlask(wavData));
    }

    IEnumerator SendWavToFlask(byte[] wavData)
    {
        Debug.Log("📤 Uploading WAV to Flask...");

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "mic_input.wav", "audio/wav");

        UnityWebRequest request = UnityWebRequest.Post(flaskURL, form);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Upload failed: " + request.error);
            yield break;
        }

        Debug.Log("✅ Flask responded. Saving WAV...");

        string path = Path.Combine(Application.persistentDataPath, "peter_response.wav");
        File.WriteAllBytes(path, request.downloadHandler.data);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Failed to load audio: " + www.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            audioSource.Play();

            if (peterOrb != null)
            {
                StartCoroutine(peterOrb.ScalePulseDuringSpeech(clip));
            }
        }
    }
}
