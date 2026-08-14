
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._6647._9982B
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Voltec B Setpoint", order = 0)]
   public class VoltecBSetpoint : ScriptableObject
   {
      [Tooltip("Inches")] public float elevatorHeight;
      [Tooltip("Degrees")] public float algaeArmAngle;
   }
}