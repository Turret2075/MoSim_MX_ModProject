using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.IronPanthers._5026
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Iron Panthers Setpoint", order = 0)]
    public class IronPanthersSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
    }
}