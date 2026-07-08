using MoSimCore.BaseClasses;
using Unity.Cinemachine;
using UnityEngine;

namespace GameSystems.Cameras
{
    public class FirstPersonVCam : BaseVCamScript
    {
        public CinemachineThirdPersonFollow Follow { get; private set; }

        private void Start()
        {
            Follow = GetComponent<CinemachineThirdPersonFollow>();
        }

        private void Update()
        {
            if (TargetRobot != null)
            {
                Vcam.Follow = TargetRobot;
                Vcam.LookAt = TargetRobot;
            }
        }

        public override void SetCameraTarget(Transform target)
        {
            if (target == null)
            {
                Debug.LogError("Target is null");
                return;
            }
            
            TargetRobot = target;
            
            if (Vcam == null)
            {
                Debug.LogError("Virtual Camera is not assigned.");
                
                Vcam = GetComponent<CinemachineCamera>();
                
                if (Vcam == null)
                {
                    Debug.LogError("CinemachineCamera component not found on this GameObject.");
                    return;
                }
            }

            Vcam.Follow = target;
            Vcam.LookAt = target;
        }

        public override void FlipCamera()
        {
            // For first person camera, flip by adjusting CameraSide and rotating transform
            if (Follow != null)
            {
                // Flip the camera side (left to right or right to left)
                Follow.CameraSide = -Follow.CameraSide;
            }
            // Also rotate the transform 180 degrees around Y axis
            base.FlipCamera();
        }
    }
}