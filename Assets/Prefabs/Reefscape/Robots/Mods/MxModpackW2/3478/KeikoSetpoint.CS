using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Lambot._3478
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Keiko Setpoint", order = 0)]
    public class KeikoSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float endEffectorAngle;
    }
}
