using MoSimCore.BaseClasses;
using Unity.Cinemachine;
using UnityEngine;

namespace GameSystems.Cameras
{
    public class DriverStationVCam : BaseVCamScript
    {
        public Vector2 TranslationValue { get; set; }
        private Vector3 _startingDirection;
        private Vector3 _startingRotation;

        private Rigidbody _rb;
        
        [SerializeField] private CinemachineCamera vcam;
        
        [SerializeField] private float movementSpeed = 5f;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                Debug.LogError("Rigidbody component not found on this GameObject.");
            }

            if (vcam == null)
            {
                vcam = GetComponentInChildren<CinemachineCamera>();
                if (vcam == null)
                {
                    Debug.LogError("CinemachineCamera component not found on this GameObject.");
                    return;
                }
            }
            
            _startingDirection = transform.forward;
            _startingRotation = transform.right;
        }

        private void Update()
        {
            if (TargetRobot is not null)
            {
                vcam.LookAt = TargetRobot;
            }
        }

        private void FixedUpdate()
        {
            var moveDirection = _startingDirection * TranslationValue.y + _startingRotation * TranslationValue.x;
            
            if (_rb is not null)
            {
                _rb.MovePosition(_rb.position + moveDirection * (movementSpeed * Time.fixedDeltaTime));
            }
            else
            {
                Debug.LogWarning("Rigidbody is not assigned, movement will not be applied.");
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

            if (vcam == null)
            {
                Debug.LogError("Virtual Camera is not assigned.");

                vcam = GetComponent<CinemachineCamera>();

                if (vcam == null)
                {
                    Debug.LogError("CinemachineCamera component not found on this GameObject.");
                    return;
                }
            }

            vcam.LookAt = target;
        }
    }
}