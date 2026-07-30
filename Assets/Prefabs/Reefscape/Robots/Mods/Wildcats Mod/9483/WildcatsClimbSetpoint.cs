using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Wildcats._9483
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Wildcats Climb Setpoint", order = 0)]
    public class WildcatsClimbSetpoint : ScriptableObject
    {
        [Tooltip("Degrees")] public float elevatorAngle;
        [Tooltip("Degrees")] public float leftPincerAngle;
        [Tooltip("Degrees")] public float rightPincerAngle;
    }
}