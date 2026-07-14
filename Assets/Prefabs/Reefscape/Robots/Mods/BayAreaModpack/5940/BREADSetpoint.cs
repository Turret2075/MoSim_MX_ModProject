using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._5940
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/BREAD Setpoint", order = 0)]
    public class BREADSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
        [Tooltip("Degrees")] public float climbAngle;
    }
}