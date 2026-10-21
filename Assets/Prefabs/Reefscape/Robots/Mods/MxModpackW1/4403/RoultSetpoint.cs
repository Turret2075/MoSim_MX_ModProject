using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack.RoultRobot
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Roult Setpoint", order = 0)]
   public class RoultSetpoint : ScriptableObject
   {
      [Tooltip("Inches")] public float elevatorHeight;
      [Tooltip("Degrees")] public float algaeArmAngle;
      [Tooltip("Degrees. DescorerAlt se mueve solo al angulo OPUESTO, no hay que ponerlo aqui.")]
      public float algaeDescorerArmAngle;

      [Header("Climber")]
      [Tooltip("Degrees. Pivot del funnel. Si un setpoint no lo cambia, regresa a este valor (0 = normal).")]
      public float funnelAngle;
      [Tooltip("Degrees. Pivot del climber. Si un setpoint no lo cambia, regresa a este valor (0 = normal).")]
      public float climberPivotAngle;
   }
}
