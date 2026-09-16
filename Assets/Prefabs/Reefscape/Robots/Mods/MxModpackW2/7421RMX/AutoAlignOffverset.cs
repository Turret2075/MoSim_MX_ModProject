using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Offverture
{
    [CreateAssetMenu(fileName = "AutoAlignOffverset", menuName = "Robot/AutoAlignOffverset", order = 0)]
    public class AutoAlignOffverset : ScriptableObject
    {
        [Tooltip("Inches")] public float xOffset;
        [Tooltip("Inches")] public float yOffset;
        [Tooltip("Inches")] public float zOffset;
        [Tooltip("Degrees")] public float Rotation;
    }
}
