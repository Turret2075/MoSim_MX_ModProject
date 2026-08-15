using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._6647
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Voltec Setpoint", order = 0)]
    public class VoltecOffseasonSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
        [Tooltip("Degrees")] public float intakeAngle;
    }
}
