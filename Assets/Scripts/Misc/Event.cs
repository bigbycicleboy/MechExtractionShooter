using UnityEngine;
using UnityEngine.Events;

public class Event : MonoBehaviour
{
    public enum Trigger
    {
        OnTriggerEnter,
        OnTriggerExit,
        OnButtonPress,
        OnStart
    }

    public UnityEvent unityEvent;

    public void Start()
    {
        unityEvent.Invoke();
    }
}