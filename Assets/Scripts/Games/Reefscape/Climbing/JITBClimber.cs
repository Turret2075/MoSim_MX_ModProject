using Games.Reefscape.Scoring.Scorers;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Robots.Climbing
{
    public class JITBClimber : MonoBehaviour
    {
        private ClimbScorer _climbScorer;
        
        [Header("Clicker Joints")]
        [SerializeField] private GenericAnimationJoint clickerL;
        [SerializeField] private GenericAnimationJoint clickerR;
        [SerializeField] private GenericAnimationJoint clickerL1;
        [SerializeField] private GenericAnimationJoint clickerR1;

        [Header("Climber Joints")]
        [SerializeField] private GenericJoint armElevator;

        [SerializeField] private GenericJoint intakeWheelL;
        [SerializeField] private GenericJoint intakeWheelR;
    
        [FormerlySerializedAs("pidmi")] [SerializeField] private PidConstants pidConstants;
        
        [Header("Climber Wheels")]
        [SerializeField] private GameObject intakeWheelGameObjectL;
        [SerializeField] private GameObject intakeWheelGameObjectR;
        [SerializeField] private float targetIntakeWheelSpeed = 100f;
        private float _intakeWheelSpeed;
    
        [SerializeField] private float climbingAngularVelocity = 40f;
        private float _angularVelocity;

        [SerializeField] private float ClickerSpeed = 720f;

        private float _extendTarget;
        
        private void Start()
        {
            _climbScorer = GetComponentInParent<ClimbScorer>();
            if (_climbScorer == null)
            {
                Debug.LogError("JITBClimber: ClimbScorer component not found in parent.");
            }
            
            armElevator.SetPid(pidConstants);   
            intakeWheelL.SetPid(pidConstants);
            intakeWheelR.SetPid(pidConstants);
            _extendTarget = 0;
            _angularVelocity = 0;
        }

        private void LateUpdate()
        {
            armElevator.UpdatePid(pidConstants);
        }

        // Update is called once per frame
        private void Update()
        {
            clickerL.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            clickerR.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
        
            clickerL1.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            clickerR1.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
        
        }

        private void FixedUpdate()
        {
            armElevator.SetLinearTarget(_extendTarget).withAxis(JointAxis.Y).flipDirection();
            intakeWheelL.SetAngularVelocity(_angularVelocity).WithAxis(JointAxis.Y);
            intakeWheelR.SetAngularVelocity(-_angularVelocity).WithAxis(JointAxis.Y);
            intakeWheelGameObjectL.transform.Rotate(Vector3.up, -_intakeWheelSpeed * Time.fixedDeltaTime);
            intakeWheelGameObjectR.transform.Rotate(Vector3.up, _intakeWheelSpeed * Time.fixedDeltaTime);
        }

        public void Climb()
        {
            armElevator.freeLinearAxis(JointAxis.Y);
            _extendTarget = 7;
            _angularVelocity = climbingAngularVelocity;
            _intakeWheelSpeed = targetIntakeWheelSpeed;
        }
        
        public bool WingsOpen()
        {
            var result = (Utils.InAngularRange(clickerL1.transform.localEulerAngles.y, 0, 3) &&
                          Utils.InAngularRange(clickerR1.transform.localEulerAngles.y, 0, 3));
            return result;
        }

        public void NotClimbing()
        {
            if (_climbScorer.ScoringTriggered && Mathf.Abs(armElevator.GetAxisLocation(JointAxis.Y)) < 0.01f)
            {
                armElevator.lockAllAxis();
            }
            _extendTarget = 0;
            _angularVelocity = 0;
            _intakeWheelSpeed = 0;
        }
    }
}
