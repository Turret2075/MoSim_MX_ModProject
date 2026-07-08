using System;
using RobotFramework.Controllers.PidSystems;
using UnityEngine;

namespace RobotFramework.Controllers.Drivetrain
{
    public class AutoAlign : MonoBehaviour
    {
        protected DriveController DriveController;
    
        [SerializeField] private PidConstants drivePID;
        [SerializeField] private PidConstants rotatePID;
        private PIDController _xPidController;
        private PIDController _yPidController;
        private PIDController _rotatePidController;
        
        protected void Start()
        {
            _xPidController ??= new PIDController();

            _xPidController.proportionalGain = drivePID.kP;
            _xPidController.integralGain = drivePID.kI;
            _xPidController.derivativeGain = drivePID.kD;
            _xPidController.integralSaturation = drivePID.Isaturation;

            _xPidController.outputMax = drivePID.Max;
            _xPidController.outputMin = -drivePID.Max;

            _xPidController.integralSaturation = rotatePID.Isaturation;
        
            _yPidController ??= new PIDController();

            _yPidController.proportionalGain = drivePID.kP;
            _yPidController.integralGain = drivePID.kI;
            _yPidController.derivativeGain = drivePID.kD;
            _yPidController.integralSaturation = drivePID.Isaturation;

            _yPidController.outputMax = drivePID.Max;
            _yPidController.outputMin = -drivePID.Max;

            _yPidController.integralSaturation = rotatePID.Isaturation;

            _rotatePidController ??= new PIDController();

            _rotatePidController.proportionalGain = rotatePID.kP;
            _rotatePidController.integralGain = rotatePID.kI;
            _rotatePidController.derivativeGain = rotatePID.kD;
            _rotatePidController.integralSaturation = rotatePID.Isaturation;

            _rotatePidController.outputMax = rotatePID.Max;
            _rotatePidController.outputMin = -rotatePID.Max;

            _rotatePidController.integralSaturation = rotatePID.Isaturation;

            DriveController = gameObject.GetComponent<DriveController>();
        }

        protected void LateUpdate()
        {
            UpdatePid(_xPidController, drivePID);
            UpdatePid(_yPidController, drivePID);
            UpdatePid(_rotatePidController, rotatePID);
        
        }
    
        protected void AlignPosition(Transform target, Quaternion? targetRotation = null)
        {
            Quaternion finalRotation = targetRotation ?? target.rotation;
        
            AlignPosition(target.position, finalRotation);
        }

        protected void AlignPosition(Vector3 position, Quaternion targetRotation)
        {
            Vector2 vector = (Vec3ToVec2(transform.position)) - Vec3ToVec2(position);
            
            float xvelocity = _xPidController.UpdateLinear(Time.fixedDeltaTime, vector.x, 0);
            float yvelocity = _yPidController.UpdateLinear(Time.fixedDeltaTime, vector.y, 0);
            float rInput = _rotatePidController.UpdateAngle(Time.fixedDeltaTime, Mathf.Repeat(transform.eulerAngles.y, 360), targetRotation.eulerAngles.y);
        
            Vector2 inputVector = new Vector2(xvelocity, yvelocity);
        
            if (inputVector.magnitude > drivePID.Max)
            {
                inputVector = inputVector.normalized * drivePID.Max;
            }
        
            DriveController.overideInput(inputVector, -rInput, DriveController.DriveMode.FieldOriented);
        }

        public float getDistance()
        {
            
            return new Vector2(_xPidController.errorLast, _yPidController.errorLast).magnitude;
        }
    
        public static Vector2 Vec3ToVec2(Vector3 vec3)
        {
            return new Vector2(vec3.x, vec3.z);
        }
    
        private void UpdatePid(PIDController pidController, PidConstants pidConstants)
        {
            if (!Mathf.Approximately(pidConstants.kP, pidController.proportionalGain) ||
                !Mathf.Approximately(pidConstants.kI, pidController.integralGain) ||
                !Mathf.Approximately(pidConstants.kD, pidController.derivativeGain) ||
                !Mathf.Approximately(pidConstants.Isaturation, pidController.integralSaturation))
            {
                pidController.proportionalGain = pidConstants.kP;
                pidController.integralGain = pidConstants.kI;
                pidController.derivativeGain = pidConstants.kD;
                pidController.integralSaturation = pidConstants.Isaturation;
                pidController.ResetController();
            }

            if (!Mathf.Approximately(pidConstants.Max, pidController.outputMax))
            {
                pidController.outputMax = pidConstants.Max;
                pidController.outputMin = -pidConstants.Max;
            }
        }
    
        [Serializable]
        private struct Pose2d
        {
            public float x;
            public float y;
            public float angle;

            public Pose2d(float x, float y, float angle)
            {
                this.x = x;
                this.y = y;
                this.angle = angle;
            }

            public Vector2 GetPosition()
            {
                return new Vector2(this.x, this.y);
            }

            public Pose2d(Transform trans)
            {
                this.x = trans.position.x;
                this.y = trans.position.z;
                this.angle = trans.rotation.eulerAngles.y;
            }

            public float GetAngle()
            {
                return Mathf.Repeat(angle + 90, 360);
            }
        }
    }
}
