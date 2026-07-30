using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Wildcats._9483
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Wildcats Setpoint", order = 0)]
    public class WildcatsSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float intakeAngle;
    }
}