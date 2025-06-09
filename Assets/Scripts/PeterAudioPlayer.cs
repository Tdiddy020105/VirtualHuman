using UnityEngine;
using System;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;

public class PeterAudioPlayer : MonoBehaviour
{
    private FMOD.System fmodSystem;
    private FMOD.Sound sound;
    private FMOD.Channel channel;

    void Awake()
    {
        fmodSystem = RuntimeManager.CoreSystem;
    }

    public void PlayBase64MP3(string base64)
    {
        byte[] mp3Data = Convert.FromBase64String(base64);
        GCHandle pinnedArray = GCHandle.Alloc(mp3Data, GCHandleType.Pinned);
        IntPtr pointer = pinnedArray.AddrOfPinnedObject();

        var exinfo = new FMOD.CREATESOUNDEXINFO
        {
            cbsize = Marshal.SizeOf<FMOD.CREATESOUNDEXINFO>(),
            length = (uint)mp3Data.Length
        };

        var result = fmodSystem.createSound(
        pointer,
        FMOD.MODE.OPENMEMORY | FMOD.MODE.CREATESTREAM,
        ref exinfo,
        out sound
    );

    if (result != FMOD.RESULT.OK)
    {
        UnityEngine.Debug.LogError("❌ FMOD createSound failed: " + result);
        pinnedArray.Free();
        return;
    }

    FMOD.ChannelGroup dummyGroup = default;
    fmodSystem.playSound(sound, dummyGroup, false, out channel);
    pinnedArray.Free();
    }

    public bool IsPlaying()
    {
        if (channel.hasHandle())
        {
            bool isPlaying;
            channel.isPlaying(out isPlaying);
            return isPlaying;
        }
        return false;
    }
}
