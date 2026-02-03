using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public Light sun;
    
    float originalIntensity;

    void Start()
    {
        originalIntensity = sun.intensity;
    }

    void Update()
    {
        sun.transform.Rotate(Vector3.right, Time.deltaTime * 0.25f);
        if(sun.transform.eulerAngles.x > 180)
        {
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, 0, Time.deltaTime * 0.5f);
            sun.intensity = Mathf.Lerp(sun.intensity, 0, Time.deltaTime * 0.5f);
        }
        else
        {
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, 0.015f, Time.deltaTime * 0.5f);
            sun.intensity = Mathf.Lerp(sun.intensity, originalIntensity, Time.deltaTime * 0.5f);
        }
    }
}
