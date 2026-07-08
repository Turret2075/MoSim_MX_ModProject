﻿using MoSimCore.Interfaces;
using UnityEngine;

namespace MoSimCore.BaseClasses
{
    /// <summary>
    /// Abstract base class for score UI components in game modes.
    /// Provides a template for updating alliance scores in the UI.
    /// </summary>
    /// <typeparam name="T">The type of score data, must implement IScoreData.</typeparam>
    public abstract class BaseScoreUI<T> : MonoBehaviour where T : IScoreData
    {
        /// <summary>
        /// Updates the total score displays for both alliances.
        /// Implement this to show the overall point totals.
        /// </summary>
        /// <param name="blueScore">Score data for the blue alliance.</param>
        /// <param name="redScore">Score data for the red alliance.</param>
        protected abstract void UpdateTotalScores(T blueScore, T redScore);
        
        /// <summary>
        /// Updates the detailed score breakdowns for both alliances.
        /// Implement this to show individual scoring components.
        /// </summary>
        /// <param name="blueScore">Score data for the blue alliance.</param>
        /// <param name="redScore">Score data for the red alliance.</param>
        protected abstract void UpdateDetailedScores(T blueScore, T redScore);
        
        /// <summary>
        /// Updates the game piece counter displays for both alliances.
        /// Implement this to show quantities of game pieces scored or held.
        /// </summary>
        /// <param name="blueScore">Score data for the blue alliance.</param>
        /// <param name="redScore">Score data for the red alliance.</param>
        protected abstract void UpdateGamePieceCounters(T blueScore, T redScore);

        /// <summary>
        /// Main update method that refreshes all UI elements with current score data.
        /// Override to customize the update sequence.
        /// </summary>
        /// <param name="blueScore">Score data for the blue alliance.</param>
        /// <param name="redScore">Score data for the red alliance.</param>
        public virtual void UpdateUI(T blueScore, T redScore)
        {
            UpdateTotalScores(blueScore, redScore);
            UpdateDetailedScores(blueScore, redScore);
            UpdateGamePieceCounters(blueScore, redScore);
        }
    }
}