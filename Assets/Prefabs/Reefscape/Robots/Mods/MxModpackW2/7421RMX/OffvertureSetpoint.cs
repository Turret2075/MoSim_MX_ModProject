using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Offverture._7421RMX
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Offverture Setpoint", order = 0)]
    public class OffvertureSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
        [Tooltip("Degrees")] public float intakeAngle;
        [Tooltip("Degrees")] public float climberAngle;
    }
}