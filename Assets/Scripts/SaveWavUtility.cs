using System.IO;
using UnityEngine;

public static class SaveWavUtility
{
    public static byte[] FromAudioClip(string filename, AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            WriteWavFile(stream, clip);
            return stream.ToArray();
        }
    }

    private static void WriteWavFile(Stream stream, AudioClip clip)
    {
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int headerSize = 44;
            int fileSize = clip.samples * clip.channels * 2 + headerSize;

            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(fileSize - 8);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)clip.channels);
            writer.Write(16000);
            writer.Write(16000 * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(clip.samples * clip.channels * 2);

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            foreach (float sample in samples)
            {
                short intSample = (short)(sample * 32767);
                writer.Write(intSample);
            }
        }
    }
}
