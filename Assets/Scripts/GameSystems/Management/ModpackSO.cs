using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobotFramework;
using UnityEngine;

namespace GameSystems.Management
{
    /// <summary>
    /// Base ScriptableObject representing a modpack containing multiple robots and associated metadata.
    /// Inherit from this class to create game-specific modpack implementations.
    /// DO NOT INSTANTIATE THIS TYPE IN CODE.
    /// </summary>
    public class BaseModpackSO : ScriptableObject
    {
        [Header("Modpack Info")]
        [SerializeField]
        [Tooltip("The name of the modpack.")]
        private string modpackName;
        
        [SerializeField]
        [Tooltip("The version of the modpack in semantic versioning format (e.g., 1.0.0).")]
        private string modpackVersion;
        
        [SerializeField]
        [Tooltip("The author or creator of the modpack.")]
        private string authorName;
        
        [SerializeField]
        [Tooltip("A brief description of the modpack and its contents.")]
        private string description;
        
        [Header("Included Robots")]
        [SerializeField]
        [Tooltip("List of robot metadata objects included in this modpack.")]
        private List<RobotMetadataSO> robots;
        
        /// <summary>Gets the name of the modpack.</summary>
        public string ModpackName => modpackName;
        
        /// <summary>Gets the version string of the modpack.</summary>
        public string ModpackVersion => modpackVersion;
        
        /// <summary>Gets the author name of the modpack.</summary>
        public string AuthorName => authorName;
        
        /// <summary>Gets the description of the modpack.</summary>
        public string Description => description;
        
        /// <summary>Gets the list of robots included in this modpack.</summary>
        public List<RobotMetadataSO> Robots => robots;

        /// <summary>
        /// Returns a formatted string representation of the modpack.
        /// </summary>
        /// <returns>A string in the format "ModpackName vModpackVersion by AuthorName".</returns>
        public override string ToString()
        {
            return $"{ModpackName} v{ModpackVersion} by {AuthorName}";
        }

        /// <summary>
        /// Validates semantic versioning format and modpack data consistency.
        /// Checks version format (X.Y.Z), removes null robots, and warns on issues.
        /// </summary>
        private void OnValidate()
        {
            // Normalize strings
            if (!string.IsNullOrEmpty(modpackName)) modpackName = modpackName.Trim();
            if (!string.IsNullOrEmpty(authorName)) authorName = authorName.Trim();
            if (!string.IsNullOrEmpty(description)) description = description.Trim();

            // Validate and normalize modpack version
            if (!string.IsNullOrEmpty(modpackVersion))
            {
                modpackVersion = modpackVersion.Trim();
                
                // Semantic versioning regex: matches X.Y.Z with optional pre-release and metadata
                // Examples: 1.0.0, 1.0.0-alpha, 1.0.0+build.123
                const string semanticVersionPattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
                
                if (!Regex.IsMatch(modpackVersion, semanticVersionPattern))
                {
                    Debug.LogWarning(
                        $"Modpack '{name}' version '{modpackVersion}' does not follow semantic versioning format (e.g., 1.0.0).",
                        this);
                }
            }

            // Ensure robots list exists
            if (robots == null) robots = new List<RobotMetadataSO>();

            // Remove null entries and warn about duplicates
            var nullIndices = new List<int>();
            var seenRobots = new HashSet<RobotMetadataSO>();

            for (int i = robots.Count - 1; i >= 0; i--)
            {
                if (robots[i] == null)
                {
                    nullIndices.Add(i);
                }
                else if (seenRobots.Contains(robots[i]))
                {
                    Debug.LogWarning(
                        $"Modpack '{name}' contains duplicate robot metadata '{robots[i].name}'. Removing duplicate.",
                        this);
                    robots.RemoveAt(i);
                }
                else
                {
                    seenRobots.Add(robots[i]);
                }
            }

            if (nullIndices.Count > 0)
            {
                Debug.LogWarning(
                    $"Modpack '{name}' contains {nullIndices.Count} null robot entries. Removing them.",
                    this);
                foreach (var index in nullIndices)
                {
                    robots.RemoveAt(index);
                }
            }
        }
    }
}

