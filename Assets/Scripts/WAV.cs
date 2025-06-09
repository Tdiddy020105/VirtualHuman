using System;
using UnityEngine;

public class WAV
{
    public float[] LeftChannel { get; private set; }
    public int ChannelCount { get; private set; }
    public int SampleCount { get; private set; }
    public int Frequency { get; private set; }

    public WAV(byte[] wav)
    {
        if (wav == null || wav.Length < 44)
        {
            Debug.LogError("Invalid WAV file (too short or null).");
            return;
        }

        Frequency = BitConverter.ToInt32(wav, 24);
        ChannelCount = BitConverter.ToInt16(wav, 22);

        // Search for "data" chunk
        int pos = 12;
        while (pos + 4 < wav.Length &&
              !(wav[pos] == 'd' && wav[pos + 1] == 'a' && wav[pos + 2] == 't' && wav[pos + 3] == 'a'))
        {
            pos++;
        }

        if (pos + 8 >= wav.Length)
        {
            Debug.LogError("WAV data chunk not found or file is too short.");
            return;
        }

        int dataStart = pos + 8;
        int dataSize = BitConverter.ToInt32(wav, pos + 4);
        SampleCount = dataSize / 2;

        if (dataStart + dataSize > wav.Length)
        {
            Debug.LogError("Declared data size goes beyond file length.");
            return;
        }

        LeftChannel = new float[SampleCount];
        int i = 0;
        for (int offset = dataStart; offset < dataStart + dataSize; offset += 2)
        {
            short sample = BitConverter.ToInt16(wav, offset);
            LeftChannel[i++] = sample / 32768f;
        }
    }
}
