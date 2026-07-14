using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._604
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Quixilver A Setpoint", order = 0)]
    public class QuixilverASetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float armAngle;
        [Tooltip("Degrees")] public float wristAngle;
        [Tooltip("Degrees")] public float climbAngle;
    }
}