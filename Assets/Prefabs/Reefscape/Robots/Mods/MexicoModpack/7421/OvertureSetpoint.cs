
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._7421
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Overture Setpoint", order = 0)]
   public class OvertureSetpoint : ScriptableObject
   {
      [Tooltip("Inches")] public float elevatorHeight;
      [Tooltip("Degrees")] public float armAngle;
      [Tooltip("Degrees")] public float endEffectorAngle;
      [Tooltip("Degrees")] public float climberAngle;
   }
}