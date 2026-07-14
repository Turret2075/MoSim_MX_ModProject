
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._5959
{
   [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/Titanium Rams Setpoint", order = 0)]
   public class TitaniumRamsSetpoint : ScriptableObject
   {
      [Tooltip("Inches")] public float elevatorHeight;
      [Tooltip("Degrees")] public float algaeArmAngle;
      [Tooltip("Degrees")] public float algaeDescorerArmAngle;
   }
}