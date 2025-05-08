using System;
using System.IO;
using UnityEngine;

public static class SaveWavUtility
{
    public static byte[] FromAudioClip(string filename, AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            WriteWavFile(stream, clip);
            byte[] wavData = stream.ToArray();
            Debug.Log($"WAV Data Length: {wavData.Length} bytes");
            return wavData;
        }
    }

    private static void WriteWavFile(Stream stream, AudioClip clip)
    {
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            const int headerSize = 44;
            int fileSize = clip.samples * clip.channels * 2 + headerSize;

            // Write RIFF header
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(fileSize - 8);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // PCM chunk size
            writer.Write((short)1); // Audio format: PCM
            writer.Write((short)clip.channels);
            writer.Write(16000); // Sample rate
            writer.Write(16000 * clip.channels * 2); // Byte rate
            writer.Write((short)(clip.channels * 2)); // Block align
            writer.Write((short)16); // Bits per sample

            // Write data chunk
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(clip.samples * clip.channels * 2);

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            foreach (float sample in samples)
            {
                short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
                writer.Write(intSample);
            }
        }

        Debug.Log("✅ WAV written as 16-bit PCM, 16000 Hz.");
    }
}
