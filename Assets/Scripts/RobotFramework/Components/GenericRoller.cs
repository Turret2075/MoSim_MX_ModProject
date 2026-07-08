using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using UnityEngine;

namespace RobotFramework.Components
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(HingeJoint))]
    public class GenericRoller : MonoBehaviour
    {
        private Rigidbody _rb;

        private HingeJoint _hj;

        private float targetVelocity;
        private bool overidenVelocity;
    
        // Start is called before the first frame update
        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _hj = GetComponent<HingeJoint>();

            targetVelocity = _hj.motor.targetVelocity;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
       
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                JointMotor jm = _hj.motor;
                jm.targetVelocity = 0;
                _hj.motor = jm;
                return;
            }

            if (overidenVelocity)
            {
                overidenVelocity = false;
                return;
            }
        
            if (!Mathf.Approximately(_hj.motor.targetVelocity, targetVelocity))
            {
                JointMotor jm = _hj.motor;
                jm.targetVelocity = targetVelocity;
                _hj.motor = jm;
            }
        }
        /// <summary>
        /// Set the constant angular velocity
        /// </summary>
        /// <param name="velocity"></param>
        /// <returns></returns>
        public void SetAngularVelocity(float velocity)
        {
            targetVelocity = velocity;
        }

        /// <summary>
        /// Flip the direction of travel when called, this is non destructive and will revert the first frame it is not called.
        /// </summary>
        public void flipVelocity()
        {
            JointMotor jm = _hj.motor;
            jm.targetVelocity = -targetVelocity;
            _hj.motor = jm;
            overidenVelocity = true;
        }

        /// <summary>
        /// set the velocity to 0 without overiding the current setpoint. it will return to setpoint velocity the first frame this is not called
        /// </summary>
        public void stopAngularVelocity()
        {
            JointMotor jm = _hj.motor;
            jm.targetVelocity = 0;
            _hj.motor = jm;
            overidenVelocity = true;
        }

        /// <summary>
        /// Set the angular velocity until the next frame this is not called at which point it returns to the default value
        /// </summary>
        /// <param name="velocity"></param>
        public void ChangeAngularVelocity(float velocity)
        {
            JointMotor jm = _hj.motor;
            jm.targetVelocity = velocity;
            _hj.motor = jm;
            overidenVelocity = true;
        }
    }
}
