using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class PeterOrb : MonoBehaviour
{
    public string flaskURL = "http://localhost:5000/speak";
    public AudioSource audioSource;
    public string testInput = "Who are you?";
    public Transform orbTransform; // assign your orb object here

    private bool isSpeaking = false;
    private Vector3 baseScale;
    private Vector3 basePosition;

    [Header("Floating Settings")]
    public float floatAmplitude = 0.1f;
    public float floatFrequency = 1f;

    void Start()
    {
        if (orbTransform == null) orbTransform = transform;
        baseScale = orbTransform.localScale;
        basePosition = orbTransform.position;
    }

    void Update()
    {
        // Floating logic
        if (orbTransform != null)
        {
            float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            orbTransform.position = basePosition + new Vector3(0f, floatOffset, 0f);
        }
    }

    [ContextMenu("Talk To Peter")]
    public void TalkToPeter()
    {
        Debug.Log("🧠 TalkToPeter triggered.");
        StartCoroutine(SendTextAndPlayAudio(testInput));
    }

    IEnumerator SendTextAndPlayAudio(string text)
    {
        string json = "{\"text\": \"" + text.Replace("\"", "\\\"") + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(flaskURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("🧠 Sending text to Flask...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Request failed: " + request.error);
            yield break;
        }

        Debug.Log("✅ Got WAV file");
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
            if (clip == null)
            {
                Debug.LogError("❌ Loaded AudioClip is null!");
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();
            isSpeaking = true;
            StartCoroutine(ScalePulseDuringSpeech(clip));
            yield return new WaitWhile(() => audioSource.isPlaying);
            isSpeaking = false;
            orbTransform.localScale = baseScale;
            Debug.Log("🛑 Done speaking.");
        }
    }

    IEnumerator ScalePulseDuringSpeech(AudioClip clip)
    {
        Debug.Log("📡 Smooth ScalePulse running...");
        int sampleSize = 512;
        float[] buffer = new float[sampleSize];
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        int channels = clip.channels;
        int position = 0;

        Vector3 currentScale = baseScale;

        while (position + sampleSize < samples.Length)
        {
            for (int i = 0; i < sampleSize; i++)
            {
                buffer[i] = samples[position + i * channels];
            }

            float sum = 0f;
            for (int i = 0; i < sampleSize; i++)
            {
                sum += buffer[i] * buffer[i];
            }

            float rmsValue = Mathf.Sqrt(sum / sampleSize);
            float scaleBoost = Mathf.Clamp(rmsValue * 20f, 0f, 0.2f); // 🧠 reduced range
            Vector3 targetScale = baseScale + Vector3.one * scaleBoost;

            // 🌀 Smoothly transition to the new scale
            currentScale = Vector3.Lerp(currentScale, targetScale, 0.2f);
            orbTransform.localScale = currentScale;

            position += sampleSize;
            yield return new WaitForSeconds(sampleSize / (float)clip.frequency);
        }

        // Smoothly return to base scale
        while (Vector3.Distance(orbTransform.localScale, baseScale) > 0.001f)
        {
            orbTransform.localScale = Vector3.Lerp(orbTransform.localScale, baseScale, 0.1f);
            yield return null;
        }

        orbTransform.localScale = baseScale;
        Debug.Log("🛑 Smooth ScalePulse done.");
    }

}
