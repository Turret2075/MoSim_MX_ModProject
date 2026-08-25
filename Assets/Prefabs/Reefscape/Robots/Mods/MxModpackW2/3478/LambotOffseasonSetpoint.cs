using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Lambot._9978
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Lambot Offseason Setpoint", order = 0)]
    public class LambotOffseasonSetpoint : ScriptableObject
    {
        [Tooltip("Deg")] public float armAngle;
        [Tooltip("Inch")] public float elevatorHeight;
        [Tooltip("Deg")] public float intakeAngle;

    }
}
