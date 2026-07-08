﻿namespace MoSimCore.Interfaces
{
    /// <summary>
    /// Interface for score data structures that track alliance points.
    /// Implement this to create game-specific scoring systems.
    /// </summary>
    public interface IScoreData
    {
        /// <summary>Gets the total points for an alliance.</summary>
        int TotalPoints { get; }
        
        /// <summary>
        /// Resets all score values to their initial state.
        /// </summary>
        void Reset();
    }
}