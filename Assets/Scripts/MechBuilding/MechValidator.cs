using UnityEngine;
using System.Linq;

public class MechValidator : MonoBehaviour
{
    public MechBuilder builder;

    [Header("Balance")]
    public float maxWeightPerLeg = 50f;

    public bool Validate(out string error)
    {
        error = "";

        if (builder.allModules.Count == 0)
        {
            error = "Mech has no modules.";
            return false;
        }

        // ---- REQUIRED MODULES ----
        bool hasCockpit = builder.allModules.Any(m => m.data.category == ModuleCategory.Cockpit);
        bool hasLeg = builder.allModules.Any(m => m.data.category == ModuleCategory.Locomotion);

        if (!hasCockpit)
        {
            error = "Mech requires a cockpit.";
            return false;
        }

        if (!hasLeg)
        {
            error = "Mech requires locomotion.";
            return false;
        }

        // ---- POWER CHECK ----
        float powerGen = builder.allModules.Sum(m => m.data.powerGeneration);
        float powerUse = builder.allModules.Sum(m => m.data.powerUsage);

        if (powerUse > powerGen)
        {
            error = "Insufficient power generation.";
            return false;
        }

        // ---- WEIGHT VS LEGS ----
        float totalWeight = builder.allModules.Sum(m => m.data.weight);
        int legCount = builder.allModules.Count(m => m.data.category == ModuleCategory.Locomotion);

        float maxSupportedWeight = legCount * maxWeightPerLeg;

        if (totalWeight > maxSupportedWeight)
        {
            error = "Mech is too heavy for its legs.";
            return false;
        }

        return true;
    }
}