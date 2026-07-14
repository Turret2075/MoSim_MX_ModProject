using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._604
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Quixilver B (Quixshot) Setpoint", order = 0)]
    public class QuixilverBSetpoint : ScriptableObject
    {
        [Tooltip("Degrees")] public float shooterAngle;
        [Tooltip("Degrees")] public float intakeAngle;
    }
}