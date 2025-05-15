using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class PeterResponse
{
    public string emotion;
    public string response;
    public string audio_path;
}

public class OuijaBoard : MonoBehaviour
{
    public Text previewText;
    public AudioSource audioSource;

    private string currentMessage = "";

    public void AddLetter(string letter)
    {
        currentMessage += letter;
        UpdatePreview();
    }

    public void AddYes()
    {
        currentMessage += "YES ";
        UpdatePreview();
    }

    public void AddNo()
    {
        currentMessage += "NO ";
        UpdatePreview();
    }

    public void Goodbye()
    {
        Debug.Log("👋 Goodbye submitted to Peter.");
        SubmitMessage();
    }

    public void AddSpace()
    {
        currentMessage += " ";
        UpdatePreview();
    }

    public void DeleteLast()
    {
        if (currentMessage.Length > 0)
            currentMessage = currentMessage.Substring(0, currentMessage.Length - 1);
        UpdatePreview();
    }

    public void SubmitMessage()
    {
        if (!string.IsNullOrWhiteSpace(currentMessage))
        {
            Debug.Log("🔮 Sending to Peter: " + currentMessage);
            StartCoroutine(SendToFlask(currentMessage));
        }

        currentMessage = "";
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        previewText.text = currentMessage;
    }

    IEnumerator SendToFlask(string message)
    {
        string json = "{\"text\": \"" + message + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest("http://localhost:5000/speak", "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Flask error: " + req.error);
        }
        else
        {
            Debug.Log("✅ Flask response: " + req.downloadHandler.text);

            PeterResponse response = JsonUtility.FromJson<PeterResponse>(req.downloadHandler.text);
            StartCoroutine(PlayPeterAudio(response.audio_path));
        }
    }

    IEnumerator PlayPeterAudio(string audioPath)
    {
        string path = "file://" + audioPath;

        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Audio load error: " + www.error);
        }
        else
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Vector3.zero);
            }
        }
    }
}
