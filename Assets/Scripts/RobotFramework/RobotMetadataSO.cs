using System.Collections.Generic;
using UnityEngine;

namespace RobotFramework
{
    /// <summary>
    /// ScriptableObject that holds metadata for a robot.
    /// Inherit and create concrete assets per game. DO NOT INSTANTIATE THIS TYPE IN CODE.
    /// </summary>
    public class RobotMetadataSO : ScriptableObject, IRobotMetadata
    {
        // Identity
        [Header("Identity")]
        [SerializeField]
        [Tooltip("The team number associated with this robot.")]
        private int teamNumber;

        [SerializeField]
        [Tooltip("The team name associated with this robot.")]
        private string teamName;

        // Prefabs
        [Header("Prefabs")]
        [SerializeField]
        [Tooltip("The main robot prefab GameObject that will be instantiated in the game scene.")]
        private GameObject robotPrefab;

        [SerializeField]
        [Tooltip("The game piece the robot starts with preloaded (optional).")]
        private GameObject robotPreloadGamePiece;

        [SerializeField]
        [Tooltip("The prefab to display in the main menu for robot selection.")]
        private GameObject mainMenuPrefab;

        // Alternate Variant
        [Header("Alternate Variant")]
        [SerializeField]
        [Tooltip("Whether this robot has an alternate configuration/variant.")]
        private bool hasAlternateRobot;

        [SerializeField]
        [Tooltip("The alternate robot prefab (used when HasAlternateRobot is true).")]
        private GameObject alternateRobotPrefab;

        [SerializeField]
        [Tooltip("The alternate main menu prefab (used when HasAlternateRobot is true).")]
        private GameObject alternateMainMenuPrefab;

        // UI & Tutorial
        [Header("UI & Tutorial")]
        [SerializeField]
        [Tooltip("The image/icon representing this robot in UI elements.")]
        private Sprite robotImage;

        [SerializeField]
        [Tooltip("Tutorial card GameObjects for the primary configuration.")]
        private List<GameObject> tutorialCards;

        [SerializeField]
        [Tooltip("Tutorial card GameObjects for the alternate configuration.")]
        private List<GameObject> alternateTutorialCards;
        
        public bool IsModded { get; set; } = false;

        // Public read-only API (meets IRobotMetadata)
        public int TeamNumber => teamNumber;
        public string TeamName => teamName;
        public GameObject RobotPrefab => robotPrefab;
        public GameObject RobotPreloadGamePiece => robotPreloadGamePiece;
        public GameObject MainMenuPrefab => mainMenuPrefab;
        public bool HasAlternateRobot => hasAlternateRobot;
        public GameObject AlternateRobotPrefab => alternateRobotPrefab;
        public GameObject AlternateMainMenuPrefab => alternateMainMenuPrefab;
        public Sprite RobotImage => robotImage;
        public List<GameObject> TutorialCards => tutorialCards;
        public List<GameObject> AlternateTutorialCards => alternateTutorialCards;

        /// <summary>
        /// Returns a string representation of the robot in the format "TeamNumber - TeamName".
        /// </summary>
        public override string ToString()
        {
            return $"{TeamNumber} - {TeamName}";
        }

        /// <summary>
        /// Generates a hash code for the robot metadata based on team number and team name.
        /// </summary>
        public override int GetHashCode()
        {
            return teamNumber + (teamName != null ? teamName.GetHashCode() : 0);
        }

        /// <summary>
        /// Editor-only validation to keep values consistent and avoid null references.
        /// Validates required prefabs, warns on missing assets, and ensures alternate settings are consistent.
        /// </summary>
        private void OnValidate()
        {
            // Normalize strings
            if (!string.IsNullOrEmpty(teamName)) teamName = teamName.Trim();

            // Clamp team number to non-negative
            if (teamNumber < 0) teamNumber = 0;

            // Ensure lists are non-null
            if (tutorialCards == null) tutorialCards = new List<GameObject>();
            if (alternateTutorialCards == null) alternateTutorialCards = new List<GameObject>();

            // Validate required prefabs
            if (robotPrefab == null)
            {
                Debug.LogWarning($"Robot metadata '{name}' is missing the main RobotPrefab!", this);
            }

            if (mainMenuPrefab == null)
            {
                Debug.LogWarning($"Robot metadata '{name}' is missing the MainMenuPrefab!", this);
            }

            // Validate alternate prefabs consistency
            if (hasAlternateRobot)
            {
                if (alternateRobotPrefab == null)
                {
                    Debug.LogWarning(
                        $"Robot metadata '{name}' has HasAlternateRobot enabled but AlternateRobotPrefab is null!",
                        this);
                }

                if (alternateMainMenuPrefab == null)
                {
                    Debug.LogWarning(
                        $"Robot metadata '{name}' has HasAlternateRobot enabled but AlternateMainMenuPrefab is null!",
                        this);
                }
            }
            else
            {
                // If no alternate variant, clear alternate-only refs
                if (alternateRobotPrefab != null || alternateMainMenuPrefab != null)
                {
                    alternateRobotPrefab = null;
                    alternateMainMenuPrefab = null;
                }
            }
        }
    }
}