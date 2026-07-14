using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._581
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Blazing Bulldogs B Setpoint", order = 0)]
    public class BlazingBulldogsBSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
        [Tooltip("Degrees")] public float intakeAngle;
        [Tooltip("Degrees")] public float climbAngle;
    }
}