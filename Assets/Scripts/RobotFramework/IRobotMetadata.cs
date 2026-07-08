﻿using System.Collections.Generic;
using UnityEngine;

namespace RobotFramework
{
    /// <summary>
    /// Defines the contract for robot metadata containing team information and assets.
    /// Implement this interface to provide robot data to the game system.
    /// </summary>
    public interface IRobotMetadata
    {
        /// <summary>Gets the team number associated with this robot.</summary>
        int TeamNumber { get; }
        
        /// <summary>Gets the team name associated with this robot.</summary>
        string TeamName { get; }
        
        /// <summary>Gets the main robot prefab used in gameplay.</summary>
        GameObject RobotPrefab { get; }
        
        /// <summary>Gets the prefab displayed in the main menu.</summary>
        GameObject MainMenuPrefab { get; }
        
        /// <summary>Gets the team image/icon for UI elements.</summary>
        Sprite RobotImage { get; }
        
        /// <summary>Gets the list of tutorial card GameObjects for this robot.</summary>
        List<GameObject> TutorialCards { get; }
    }
}