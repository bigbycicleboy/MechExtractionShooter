using UnityEngine;
using System.Collections.Generic;

public class Module : MonoBehaviour
{
    public ModuleData data;
    public SnapPoint parentSnap;

    public List<Module> children = new();

    public int depth;

    public void AttachTo(SnapPoint snap)
    {
        parentSnap = snap;
        depth = snap.attachedModule != null
            ? snap.attachedModule.depth + 1
            : 0;
    }

    public void Detach()
    {
        if (parentSnap != null)
        {
            parentSnap.occupied = false;
            parentSnap.attachedModule = null;
        }

        foreach (var child in children)
            child.Detach();

        Destroy(gameObject);
    }
}