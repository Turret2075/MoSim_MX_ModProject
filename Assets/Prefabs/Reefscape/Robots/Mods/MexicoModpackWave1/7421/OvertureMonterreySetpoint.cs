
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._7421._7421Monterrey
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Overture Monterrey Setpoint", order = 0)]
   public class OvertureMonterreySetpoint : ScriptableObject
   {
      [Tooltip("Inches")] public float elevatorHeight;
      [Tooltip("Degrees")] public float armAngle;
      [Tooltip("Degrees")] public float endEffectorAngle;
      [Tooltip("Degrees")] public float climberAngle;
      [Tooltip("Degrees")] public float clawAngle;
   }
}