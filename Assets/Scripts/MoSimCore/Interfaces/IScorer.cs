﻿using MoSimCore.Enums;

namespace MoSimCore.Interfaces
{
    /// <summary>
    /// Interface for objects that can score points (e.g., scoring zones, goals).
    /// Implement this to create game-specific scoring mechanisms.
    /// </summary>
    public interface IScorer
    {
        /// <summary>Gets the alliance this scorer awards points to.</summary>
        Alliance Alliance { get; }
        
        /// <summary>
        /// Adds points to the specified score data based on the current game state.
        /// </summary>
        /// <param name="scoreData">The score data to update.</param>
        /// <param name="gameState">The current game state (affects point values).</param>
        void AddScore(IScoreData scoreData, GameState gameState);
    }
}