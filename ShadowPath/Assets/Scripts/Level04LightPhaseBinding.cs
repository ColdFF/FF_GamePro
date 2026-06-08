using UnityEngine;

/// <summary>
/// Marks a Level04 shadow user as available to both lights, Phase A only, or Phase B only.
/// </summary>
public class Level04LightPhaseBinding : MonoBehaviour
{
    public enum PhaseAvailability
    {
        Both,
        PhaseAOnly,
        PhaseBOnly
    }

    [Header("Phase Access")]
    public PhaseAvailability availability = PhaseAvailability.Both;

    /// <summary>
    /// Purpose: Reports whether this object should participate in the requested Level04 light phase.
    /// Input: The active Level04 dual-light phase.
    /// Output: True when this binding allows the phase; otherwise false.
    /// </summary>
    public bool AllowsPhase(Level04DualLightController.LightPhase activePhase)
    {
        if (availability == PhaseAvailability.Both)
        {
            return true;
        }

        if (availability == PhaseAvailability.PhaseAOnly)
        {
            return activePhase == Level04DualLightController.LightPhase.PhaseA;
        }

        return activePhase == Level04DualLightController.LightPhase.PhaseB;
    }
}
