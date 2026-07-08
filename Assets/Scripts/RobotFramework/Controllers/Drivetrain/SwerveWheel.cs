using UnityEngine;

namespace RobotFramework.Controllers.Drivetrain
{
    public class SwerveWheel : MonoBehaviour
    {
        private WheelCollider _wheelCollider;
        public float wheelAngle;

        private void Start()
        {
            _wheelCollider = GetComponent<WheelCollider>();
        }

        private void Update()
        {
            if (wheelAngle < 0)
            {
                wheelAngle = -wheelAngle + 180;
            }
            
            if (wheelAngle < 0)
            {
                wheelAngle = -wheelAngle + 180;
            }

            if (wheelAngle > 360)
            {
                wheelAngle -= 360;
            }
            
            if (wheelAngle > 360)
            {
                wheelAngle -= 360;
            }

            _wheelCollider.steerAngle = wheelAngle;
            _wheelCollider.brakeTorque = 0.0000000f;
            _wheelCollider.motorTorque = 0.000000000000000000000000000001f;
        }
    }
}