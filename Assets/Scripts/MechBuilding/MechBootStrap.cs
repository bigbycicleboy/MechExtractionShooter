using UnityEngine;

public class MechBootstrap : MonoBehaviour
{
    public static MechBootstrap Instance;

    public ModuleData startingModule;
    public Vector3 startPosition = Vector3.zero;
    public Quaternion startRotation = Quaternion.identity;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        MechBuilder builder = FindFirstObjectByType<MechBuilder>();
        builder.SpawnStartingModule(startingModule, startPosition, startRotation);
    }
}