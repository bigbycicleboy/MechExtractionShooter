using UnityEngine;

public class MovementManager : MonoBehaviour
{
    public MonoBehaviour[] allScripts;

    void Start()
    {
        foreach (MonoBehaviour script in allScripts)
        {
            script.enabled = false;
        }
    }

    public void ChangeMovement(int index)
    {
        for(int i = 0; i != allScripts.Length ; i++)
        {
            if(i == index)
            {
                allScripts[index].enabled = true;
            }
        }
    }
}