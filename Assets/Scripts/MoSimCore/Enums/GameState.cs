﻿namespace MoSimCore.Enums
{
    /// <summary>
    /// Represents the current state/period of a match.
    /// </summary>
    public enum GameState
    {
        /// <summary>Autonomous period - robots operate independently.</summary>
        Auto,
        
        /// <summary>Teleoperated period - robots are under driver control.</summary>
        Teleop,
        
        /// <summary>Endgame period - final phase with special scoring opportunities.</summary>
        Endgame,
        
        /// <summary>Match has ended - robots are disabled.</summary>
        End
    }
}