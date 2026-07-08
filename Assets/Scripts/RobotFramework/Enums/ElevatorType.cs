﻿namespace RobotFramework.Enums
{
    /// <summary>
    /// Defines the mechanical configuration of an elevator/lifting mechanism.
    /// </summary>
    public enum ElevatorType
    {
        /// <summary>Cascade elevator with multiple nested stages stacking outward.</summary>
        Cascade,
        
        /// <summary>Continuous belt/pulley elevator with constant linear motion.</summary>
        Continuous
    }
}