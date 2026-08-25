using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._9995
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/BotbustersGreen Setpoint", order = 0)]
   public class BotbustersGreenSetpoint : ScriptableObject
   {
      [Tooltip("Deg")] public float armPivotAngle;
      [Tooltip("Deg")] public float endEffectorPivotAngle;
      [Tooltip("Deg")] public float endEffectorTwistAngle;
   }
}
