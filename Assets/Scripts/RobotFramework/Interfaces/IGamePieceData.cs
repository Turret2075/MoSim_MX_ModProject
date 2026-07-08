﻿namespace RobotFramework.Interfaces
{
    /// <summary>
    /// Defines the contract for game piece data containing name and symmetry information.
    /// Implement this interface to create game-specific game piece types.
    /// </summary>
    public interface IGamePieceData
    {
        /// <summary>Gets the name of this game piece type.</summary>
        string Name { get; }
        
        /// <summary>Gets whether this game piece has directional symmetry.</summary>
        bool HasSymmetry { get; }
        
        /// <summary>Gets the type of symmetry this game piece exhibits.</summary>
        SymmetryType SymmetryType { get; }
    }

    /// <summary>
    /// Defines how a game piece is symmetric around its axes.
    /// </summary>
    public enum SymmetryType
    {
        /// <summary>No symmetry - piece has a distinct orientation.</summary>
        None,
        
        /// <summary>Symmetric around the X axis.</summary>
        XAxis,
        
        /// <summary>Symmetric around the Y axis.</summary>
        YAxis,
        
        /// <summary>Symmetric around the Z axis.</summary>
        ZAxis,
        
        /// <summary>Symmetric around both X and Y axes.</summary>
        XYAxis,
        
        /// <summary>Symmetric around both X and Z axes.</summary>
        XZAxis,
        
        /// <summary>Symmetric around both Y and Z axes.</summary>
        YZAxis,
        
        /// <summary>Symmetric around all three axes.</summary>
        XYZAxis,
    }
}