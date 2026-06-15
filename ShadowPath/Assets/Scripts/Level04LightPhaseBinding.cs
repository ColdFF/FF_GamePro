using UnityEngine;

/// <summary>
/// Purpose: Tells Level 4 which light phase can use this shadow object.
/// Input: The phase choice set in the Inspector.
/// Output: The dual-light controller knows whether this object should be active now.
/// </summary>
public class Level04LightPhaseBinding : MonoBehaviour
{
    public enum PhaseAvailability
    {
        // This object works in both light phases.
        Both,
        // This object only works when Phase A is active.
        PhaseAOnly,
        // This object only works when Phase B is active.
        PhaseBOnly
    }

    [Header("Phase Access")]
    // Choose which Level 4 light phase is allowed to use this object.
    public PhaseAvailability availability = PhaseAvailability.Both;

    /// <summary>
    /// Purpose: Checks if this object is allowed in the light phase that is currently active.
    /// Input: The current Level 4 light phase.
    /// Output: True means keep this object active; false means hide or disable it for this phase.
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
