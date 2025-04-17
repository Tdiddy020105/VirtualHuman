using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class TTSManager : MonoBehaviour
{
    public string flaskURL = "http://localhost:5000/speak"; // Or LAN IP if not localhost
    public AudioSource audioSource;

    [TextArea(2, 5)]
    public string userInput = "Hello, how are you?";

    [ContextMenu("Send To Flask")]
    public void SendToFlask()
    {
        StartCoroutine(SendTextAndPlayAudio(userInput));
    }

    IEnumerator SendTextAndPlayAudio(string text)
    {
        // Build JSON
        string json = "{\"text\": \"" + text.Replace("\"", "\\\"") + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(flaskURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("🎤 Sending text to Flask...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Request failed: " + request.error);
            yield break;
        }

        Debug.Log("✅ Response received! Length: " + request.downloadHandler.data.Length);

        // Save audio to temp path
        string path = Path.Combine(Application.persistentDataPath, "response.mp3");
        File.WriteAllBytes(path, request.downloadHandler.data);

        // Load audio file
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

            Debug.Log("🔊 Playing audio...");
        }
    }
}
