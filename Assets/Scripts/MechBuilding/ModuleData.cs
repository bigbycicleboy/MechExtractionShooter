using UnityEngine;

[CreateAssetMenu(menuName = "Mech/Module")]
public class ModuleData : ScriptableObject
{
    public string moduleName;
    public ModuleCategory category;
    public GameObject prefab;

    [Header("Stats")]
    public float health;
    public float weight;
    public float powerUsage;
    public float powerGeneration;

    [Header("Build Rules")]
    public int maxChainDepth = 5;
}