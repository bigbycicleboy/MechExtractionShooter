using UnityEngine;

public class ModuleButton : MonoBehaviour
{
    public ModuleData module;
    public MechBuilder builder;

    public void Select()
    {
        builder.SelectModule(module);
    }
}