using UnityEngine;

public class Radio : MonoBehaviour
{
    [Header("Made by Keo.CS no credits needed")]
    [Header("Thanks Wikipedia for help")]
    public AudioSource audioSource;
    public float minScale = 1.0f;
    public float maxScale = 3.0f;
    public float intensity = 1.0f;

    private float[] samples = new float[512];

    void Update()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.GetOutputData(samples, 0);
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            float rmsValue = Mathf.Sqrt(sum / samples.Length);
            float scale = Mathf.Lerp(minScale, maxScale, rmsValue * intensity);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
