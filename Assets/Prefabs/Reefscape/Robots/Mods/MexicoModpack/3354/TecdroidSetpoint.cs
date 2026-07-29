
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._3354
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Tecdroid Setpoint", order = 0)]
   public class TecdroidSetpoint : ScriptableObject
   {
      [Tooltip("Deg")] public float armAngle;
        [Tooltip("Deg")] public float wristAngle;
        [Tooltip("Inch")] public float armDistance;
      [Tooltip("Deg")] public float climbAngle;
   }
}
