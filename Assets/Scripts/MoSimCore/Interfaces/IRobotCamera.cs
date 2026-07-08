﻿using UnityEngine;

namespace MoSimCore.Interfaces
{
    /// <summary>
    /// Interface for camera components that follow and view robots.
    /// </summary>
    public interface IRobotCamera
    {
        /// <summary>Gets the camera's GameObject.</summary>
        GameObject CameraObject { get; }
        
        /// <summary>Gets the camera's Transform component.</summary>
        Transform Transform { get; }
        
        /// <summary>
        /// Sets the target robot for the camera to follow.
        /// </summary>
        /// <param name="target">The transform of the robot to track.</param>
        void SetCameraTarget(Transform target);
    }
}