using UnityEngine;
using UnityEngine.Serialization;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack.RoultRobot
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Roult Setpoint", order = 0)]
   public class RoultSetpoint : ScriptableObject
   {
      [Tooltip("Inches")] public float elevatorHeight;
      [Tooltip("Degrees")] public float algaeArmAngle;

      [Header("Descorer (garra)")]
      // El valor viejo de algaeDescorerArmAngle se conserva en el brazo izquierdo.
      [FormerlySerializedAs("algaeDescorerArmAngle")]
      [Tooltip("Degrees")] public float algaeDescorerArmLeftAngle;
      [Tooltip("Degrees")] public float algaeDescorerArmRightAngle;

      [Header("Climber")]
      [Tooltip("Degrees. Pivot del funnel. Si un setpoint no lo cambia, regresa a este valor (0 = normal).")]
      public float funnelAngle;
      [Tooltip("Degrees. Pivot del climber. Si un setpoint no lo cambia, regresa a este valor (0 = normal).")]
      public float climberPivotAngle;
   }
}
