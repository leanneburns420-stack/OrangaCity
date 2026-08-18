using UnityEngine;

public class NightAndDay : MonoBehaviour
{
    public Material Day;
    public Material SunSet;
    public Material Night;
    public Material SunRise;

    public float timeForDay = 1500;
    private float currentTime = 0f;

    void Update()
    {
        currentTime += Time.deltaTime;
        float timeFraction = currentTime / timeForDay;
        timeFraction = Mathf.Clamp01(timeFraction);

        if (timeFraction < 0.25f)
        {
            RenderSettings.skybox = Day;
        }
        else if (timeFraction < 0.5f)
        {
            RenderSettings.skybox = SunSet;
        }
        else if (timeFraction < 0.75f)
        {
            RenderSettings.skybox = Night;
        }
        else
        {
            RenderSettings.skybox = SunRise;
        }

        if (currentTime >= timeForDay)
        {
            currentTime = 0f;
        }
    }
}
