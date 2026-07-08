using MoSimCore.Interfaces;
using Unity.Cinemachine;
using UnityEngine;

namespace MoSimCore.BaseClasses
{
    /// <summary>
    /// Abstract base class for robot camera controllers using Cinemachine.
    /// Provides common camera functionality for following and viewing robots.
    /// </summary>
    public abstract class BaseVCamScript : MonoBehaviour, IRobotCamera
    {
        /// <summary>Gets the Cinemachine virtual camera component.</summary>
        protected CinemachineCamera Vcam { get; set; }
        
        /// <summary>Gets the camera GameObject.</summary>
        public GameObject CameraObject => gameObject;
        
        /// <summary>Gets the camera's transform.</summary>
        public Transform Transform => transform;

        /// <summary>The transform of the robot currently being tracked by this camera.</summary>
        protected Transform TargetRobot;

        /// <summary>
        /// Initializes the Cinemachine camera component.
        /// </summary>
        protected void Awake()
        {
            Vcam = GetComponent<CinemachineCamera>();
        }

        /// <summary>
        /// Sets the target robot for the camera to follow.
        /// Implement this to configure camera tracking behavior.
        /// </summary>
        /// <param name="target">The transform of the target robot.</param>
        public abstract void SetCameraTarget(Transform target);
        
        /// <summary>
        /// Rotates the camera 180 degrees around the Y axis for alliance-swapped views.
        /// Override to customize flip behavior.
        /// </summary>
        public virtual void FlipCamera()
        {
            transform.Rotate(0f, 180f, 0f, Space.World);
        }
    }
}
