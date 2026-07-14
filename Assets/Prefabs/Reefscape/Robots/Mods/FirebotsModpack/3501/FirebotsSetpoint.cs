using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._3501
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Firebots Setpoint", order = 0)]
    public class FirebotsSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float daleAngle;
        [Tooltip("Degrees per second")] public float daleRollerVelocity;
    }
}