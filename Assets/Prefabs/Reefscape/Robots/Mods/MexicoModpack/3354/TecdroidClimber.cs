using Games.Reefscape.Scoring.Scorers;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using UnityEngine;

namespace Robots.Climbing
{
    public class TecdroidClimber : MonoBehaviour
    {
        private ClimbScorer _climbScorer;

        [Header("Clicker Joints")]
        [SerializeField] private GenericAnimationJoint clickerL;
        [SerializeField] private GenericAnimationJoint clickerR;
        [SerializeField] private GenericAnimationJoint clickerL1;
        [SerializeField] private GenericAnimationJoint clickerR1;
        [SerializeField] private GenericAnimationJoint clickerL2;
        [SerializeField] private GenericAnimationJoint clickerR2;

        [Header("Climber Joints")]
        [SerializeField] private GenericJoint deployPivot;
        [SerializeField] private GenericJoint climbPivot;

        [SerializeField] private GenericJoint intakeWheelL;
        [SerializeField] private GenericJoint intakeWheelR;

        [SerializeField] private PidConstants climbPid;
        [SerializeField] private PidConstants pidConstants;

        [Header("Climber Wheels")]
        [SerializeField] private GameObject intakeWheelGameObjectL;
        [SerializeField] private GameObject intakeWheelGameObjectR;
        [SerializeField] private float targetIntakeWheelSpeed = 100f;
        private float _intakeWheelSpeed;

        [SerializeField] private float climbingAngularVelocity = 40f;
        private float _angularVelocity;

        [SerializeField] private float clickerSpeed = 720f;

        private float _pivotTarget;

        private float _climbingTarget;

        private bool _deployed;

        private void Start()
        {
            _climbScorer = GetComponentInParent<ClimbScorer>();
            if (_climbScorer == null)
            {
                Debug.LogError("TecdroidClimber: ClimbScorer component not found in parent.");
            }

            deployPivot.SetPid(pidConstants);
            climbPivot.SetPid(climbPid);
            intakeWheelL.SetPid(pidConstants);
            intakeWheelR.SetPid(pidConstants);
            _pivotTarget = -160;
            _angularVelocity = 0;
            _climbingTarget = 70;

            _deployed = false;
        }

        private void LateUpdate()
        {
            deployPivot.UpdatePid(pidConstants);
            climbPivot.UpdatePid(climbPid);
        }

        private void Update()
        {
            clickerL.SpringLoaded().AllowedDirection(1).RotationSpeed(clickerSpeed);
            clickerR.SpringLoaded().AllowedDirection(1).RotationSpeed(clickerSpeed);

            clickerL1.SpringLoaded().AllowedDirection(1).RotationSpeed(clickerSpeed);
            clickerR1.SpringLoaded().AllowedDirection(1).RotationSpeed(clickerSpeed);

            clickerL2.SpringLoaded().AllowedDirection(1).RotationSpeed(clickerSpeed);
            clickerR2.SpringLoaded().AllowedDirection(1).RotationSpeed(clickerSpeed);
        }

        private void FixedUpdate()
        {
            deployPivot.SetTargetAngle(_pivotTarget).withAxis(JointAxis.X);
            climbPivot.SetTargetAngle(_climbingTarget).withAxis(JointAxis.X);
            intakeWheelL.SetAngularVelocity(_angularVelocity).WithAxis(JointAxis.Y);
            intakeWheelR.SetAngularVelocity(-_angularVelocity).WithAxis(JointAxis.Y);
            intakeWheelGameObjectL.transform.Rotate(Vector3.up, -_intakeWheelSpeed * Time.fixedDeltaTime);
            intakeWheelGameObjectR.transform.Rotate(Vector3.up, _intakeWheelSpeed * Time.fixedDeltaTime);

            if (_deployed && Utils.InAngularRange(deployPivot.GetSingleAxisAngle(JointAxis.X), 0, 1))
            {
                deployPivot.lockAllAxis();
            }
        }

        public void Climb()
        {
            climbPivot.freeAngularAxis(JointAxis.X);
            _pivotTarget = 0;
            _climbingTarget = 70;
            _angularVelocity = climbingAngularVelocity;
            _intakeWheelSpeed = targetIntakeWheelSpeed;

            _deployed = true;
        }

        public void NotClimbing()
        {
            if (!_deployed)
            {
                _pivotTarget = -160;
                _climbingTarget = 70;
            }
            else
            {
                _climbingTarget = 0;
            }

            if (_deployed && Utils.InAngularRange(climbPivot.GetSingleAxisAngle(JointAxis.X), 0, 1))
            {
                climbPivot.lockAllAxis();
            }

            _angularVelocity = 0;
            _intakeWheelSpeed = 0;
        }
    }
}
