using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._581
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Blazing Bulldogs A Setpoint", order = 0)]
    public class BlazingBulldogsASetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
        [Tooltip("Degrees")] public float wristAngle;
        [Tooltip("Degrees")] public float climbAngle;
    }
}