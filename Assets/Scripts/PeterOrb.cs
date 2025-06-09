using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class PeterOrb : MonoBehaviour
{
    public string flaskURL = "http://localhost:5000/speak";
    public AudioSource audioSource;
    public string testInput = "Who are you?";
    public Transform orbTransform;

    private bool isSpeaking = false;
    private Vector3 baseScale;
    private Vector3 basePosition;

    [Header("Floating Settings")]
    public float floatAmplitude = 0.1f;
    public float floatFrequency = 1f;

    public enum EmotionState { Calm, Confused, Angry, Curious }

    [Header("Emotion Settings")]
    public EmotionState currentEmotion = EmotionState.Calm;
    public Light orbLight;
    public Color calmColor = Color.blue;
    public Color confusedColor = Color.yellow;
    public Color angryColor = Color.red;
    public Color curiousColor = Color.green;
    private Color targetColor;
    private float colorLerpSpeed = 2f;

    [Header("Material Settings")]
    public Material orbMaterial;


    void Start()
    {
        if (orbTransform == null) orbTransform = transform;
        baseScale = orbTransform.localScale;
        basePosition = orbTransform.position;

        if (orbLight != null)
        {
            UpdateOrbColor();
            orbLight.color = targetColor;
        }
    }

    void Update()
    {
        // Floating logic
        if (orbTransform != null)
        {
            float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            orbTransform.position = basePosition + new Vector3(0f, floatOffset, 0f);
        }

        // Color lerping
        if (orbLight != null)
        {
            orbLight.color = Color.Lerp(orbLight.color, targetColor, Time.deltaTime * colorLerpSpeed);
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

        Debug.Log("✅ Got response from Flask");


        string jsonResponse = request.downloadHandler.text;
        EmotionResponse response = JsonUtility.FromJson<EmotionResponse>(jsonResponse);

        if (response != null)
        {
            Debug.Log($"🧠 Emotion received: {response.emotion}");

            // Update emotion
            SetEmotion(response.emotion);

            // Play audio
            StartCoroutine(PlayAudio(response.audio_path));
        }
    }

    IEnumerator PlayAudio(string audioPath)
    {
        Debug.Log($"🎧 Attempting to play audio from path: {audioPath}");

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + audioPath, AudioType.WAV))
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

            StartCoroutine(ScalePulseDuringSpeech(clip));
        }
    }

    [System.Serializable]
    public class EmotionResponse
    {
        public string emotion;
        public string audio_path;
    }

    public void SetEmotion(string emotion)
    {
        EmotionState newEmotion = EmotionState.Calm;

        switch (emotion.ToLower())
        {
            case "angry":
                newEmotion = EmotionState.Angry;
                break;
            case "confused":
                newEmotion = EmotionState.Confused;
                break;
            case "curious":
                newEmotion = EmotionState.Curious;
                break;
            case "calm":
            default:
                newEmotion = EmotionState.Calm;
                break;
        }

        currentEmotion = newEmotion;
        UpdateOrbColor();
    }

    private void UpdateOrbColor()
    {
        switch (currentEmotion)
        {
            case EmotionState.Calm:
                targetColor = calmColor;
                break;
            case EmotionState.Confused:
                targetColor = confusedColor;
                break;
            case EmotionState.Angry:
                targetColor = angryColor;
                break;
            case EmotionState.Curious:
                targetColor = curiousColor;
                break;
        }

        Debug.Log($"🔵 Emotion changed to {currentEmotion}. Target color: {targetColor}");

        // Update Light Color
        if (orbLight != null)
        {
            orbLight.color = targetColor;
        }

        // Update Material Emission Color (boosted for glow effect)
        if (orbMaterial != null)
        {
            float glowIntensity = 3.0f;  // Adjust to control the glow strength
            Color emissionColor = targetColor * glowIntensity;
            orbMaterial.SetColor("_EmissionColor", emissionColor);
            Debug.Log($"🌟 Emission Color Updated: {emissionColor}");

            // Apply dynamic GI update to ensure the glow is visible
            DynamicGI.SetEmissive(orbTransform.GetComponent<Renderer>(), emissionColor);
        }
    }

    public IEnumerator ScalePulseDuringSpeech(AudioClip clip)
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
            float scaleBoost = Mathf.Clamp(rmsValue * 20f, 0f, 0.2f); 
            Vector3 targetScale = baseScale + Vector3.one * scaleBoost;

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

    private void OnValidate()
    {
        UpdateOrbColor();
    }

    public void StartOrbPulse()
    {
        StartCoroutine(ScalePulseDuringFMODPlayback());
    }

    public IEnumerator ScalePulseDuringFMODPlayback()
    {
        Debug.Log("📡 FMOD ScalePulse running...");

        var player = FindObjectOfType<PeterAudioPlayer>();
        if (player == null)
        {
            Debug.LogWarning("❌ PeterAudioPlayer not found.");
            yield break;
        }

        Vector3 currentScale = baseScale;

        while (player.IsPlaying())
        {
            float fakeRMS = Mathf.PingPong(Time.time * 2f, 0.2f); // animated pulsing
            Vector3 targetScale = baseScale + Vector3.one * fakeRMS;
            currentScale = Vector3.Lerp(currentScale, targetScale, 0.3f);
            orbTransform.localScale = currentScale;
            yield return null;
        }

        // Smooth return to base
        while (Vector3.Distance(orbTransform.localScale, baseScale) > 0.001f)
        {
            orbTransform.localScale = Vector3.Lerp(orbTransform.localScale, baseScale, 0.1f);
            yield return null;
        }

        orbTransform.localScale = baseScale;
        Debug.Log("🛑 FMOD ScalePulse done.");
    }
}
