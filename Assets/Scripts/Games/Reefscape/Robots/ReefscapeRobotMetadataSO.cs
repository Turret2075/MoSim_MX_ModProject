using RobotFramework;
using UnityEngine;

namespace Games.Reefscape.Robots
{
    /// <summary>
    /// Reefscape-specific robot metadata. Extends RobotMetadataSO with auto-climb flags.
    /// Create via Assets > Create > Games > Reefscape > ReefscapeRobotMetadata.
    /// </summary>
    [CreateAssetMenu(fileName = "ReefscapeRobotMetadata", menuName = "Games/Reefscape/ReefscapeRobotMetadata", order = 2)]
    public class ReefscapeRobotMetadataSO : RobotMetadataSO, IReefscapeRobotMetadata
    {
        [Header("Reefscape Settings")]
        [SerializeField]
        [Tooltip("Whether the primary robot configuration can automatically climb.")]
        private bool autoClimbs;

        [SerializeField]
        [Tooltip("Whether the alternate robot configuration can automatically climb.")]
        private bool alternateAutoClimbs;

        public bool AutoClimbs => autoClimbs;
        public bool AlternateAutoClimbs => alternateAutoClimbs;

        private void OnValidate()
        {
            if (!HasAlternateRobot && alternateAutoClimbs)
            {
                alternateAutoClimbs = false;
            }
        }
    }
}