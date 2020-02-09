using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ONSPAudioSource : MonoBehaviour
{
    public bool EnableSpatialization { get; set; }
    public bool EnableRfl { get; set; }
    public float Gain { get; set; }
    public bool UseInvSqr { get; set; }
    public float Near { get; set; }
    public float Far { get; set; }

    public void SetParameters(ref AudioSource source) { }
}

public class OVRLipSync : MonoBehaviour
{
    public enum ContextProviders
    {
        Original,
        Enhanced,
        Enhanced_with_Laughter,
    };
    public static readonly int VisemeCount = 15;
    [System.Serializable]
    public class Frame
    {
        internal static Frame singleton = new Frame();
        public void CopyInput(Frame input) { }
        public void Reset() { }
        public int frameNumber;
        public int frameDelay;
        public float[] Visemes = new float[VisemeCount];
        public float laughterScore;
    }
}

public class OVRLipSyncContext : MonoBehaviour
{
    public bool audioLoopback = false;
    public bool skipAudioSource = false;
    public OVRLipSync.ContextProviders provider = OVRLipSync.ContextProviders.Enhanced;

    public void ProcessAudioSamplesRaw(short[] data, int channels) { }
    public void ProcessAudioSamplesRaw(float[] data, int channels) { }
    public OVRLipSync.Frame GetCurrentPhonemeFrame() { return OVRLipSync.Frame.singleton; }
}

public class OVRLipSyncMicInput : MonoBehaviour
{
    public enum micActivation
    {
        HoldToSpeak,
        PushToSpeak,
        ConstantSpeak
    }

    public bool enableMicSelectionGUI = false;
    public micActivation micControl = micActivation.ConstantSpeak;
    private int micFrequency = 48000;
    public float MicFrequency
    {
        get { return micFrequency; }
        set { micFrequency = (int)Mathf.Clamp((float)value, 0, 96000); }
    }
}
