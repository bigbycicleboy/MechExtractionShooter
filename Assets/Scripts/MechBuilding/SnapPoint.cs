using UnityEngine;
using System.Collections.Generic;

public enum SnapType
{
    None,       // Generic/universal snap point
    Top,        // Snaps to Bottom
    Bottom,     // Snaps to Top
    Left,       // Snaps to Right
    Right,      // Snaps to Left
    Front,      // Snaps to Back
    Back        // Snaps to Front
}

public class SnapPoint : MonoBehaviour
{
    public bool occupied;
    public Module attachedModule;
    public List<ModuleCategory> accepts;
    public SnapType snapType = SnapType.None;
}