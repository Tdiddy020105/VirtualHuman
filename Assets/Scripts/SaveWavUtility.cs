using System;
using System.IO;
using UnityEngine;

public static class SaveWavUtility
{
    const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(string name, AudioClip clip, bool trimSilence = false)
    {
        if (trimSilence)
        {
            clip = TrimSilence(clip, 0.01f);
        }

        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        byte[] wav = ConvertAudioClipToWav(samples, clip.channels, clip.frequency);
        return wav;
    }

    private static byte[] ConvertAudioClipToWav(float[] samples, int channels, int sampleRate)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int sampleCount = samples.Length;

        int byteRate = sampleRate * channels * 2;

        // Write header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(HEADER_SIZE + sampleCount * 2 - 8);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(sampleCount * 2);

        // Write samples
        foreach (var sample in samples)
        {
            short s = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
            writer.Write(s);
        }

        writer.Flush();
        writer.Close();

        return stream.ToArray();
    }

    private static AudioClip TrimSilence(AudioClip clip, float min)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        int start = 0;
        int end = samples.Length - 1;

        for (; start < samples.Length; start++)
            if (Mathf.Abs(samples[start]) > min)
                break;

        for (; end > 0; end--)
            if (Mathf.Abs(samples[end]) > min)
                break;

        int length = end - start + 1;
        float[] trimmed = new float[length];
        Array.Copy(samples, start, trimmed, 0, length);

        AudioClip newClip = AudioClip.Create("trimmed", length, clip.channels, clip.frequency, false);
        newClip.SetData(trimmed, 0);
        return newClip;
    }
}
