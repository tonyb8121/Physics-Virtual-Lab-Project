using UnityEngine;

public class AudioGenerator : MonoBehaviour
{
    public static AudioClip GenerateLaunchSound()
    {
        int sampleRate = 44100;
        float duration = 0.3f;
        int samples = (int)(sampleRate * duration);
        
        AudioClip clip = AudioClip.Create("LaunchSound", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            // Explosive sound - high to low frequency sweep
            float freq = Mathf.Lerp(800f, 100f, t / duration);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-5f * t);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    public static AudioClip GenerateLandSound()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        int samples = (int)(sampleRate * duration);
        
        AudioClip clip = AudioClip.Create("LandSound", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            // Thud sound - low frequency with quick decay
            float freq = 150f;
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-20f * t);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
}