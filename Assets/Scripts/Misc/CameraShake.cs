using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;
    private Vector3 initialPosition;
    private bool isShaking = false;

    void Awake()
    {
        instance = this;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            initialPosition = transform.localPosition;

            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = initialPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = initialPosition;
        isShaking = false;
    }
}