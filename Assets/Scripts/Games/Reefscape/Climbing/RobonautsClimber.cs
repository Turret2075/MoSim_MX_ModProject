using Games.Reefscape.Scoring.Scorers;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Robots.Climbing
{
    public class RobonautsClimber : MonoBehaviour
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
    
        
        [FormerlySerializedAs("pidmi")] [SerializeField] private PidConstants climbPid;
        [FormerlySerializedAs("pidmi")] [SerializeField] private PidConstants pidConstants;
        
        [Header("Climber Wheels")]
        [SerializeField] private GameObject intakeWheelGameObjectL;
        [SerializeField] private GameObject intakeWheelGameObjectR;
        [SerializeField] private float targetIntakeWheelSpeed = 100f;
        private float _intakeWheelSpeed;
    
        [SerializeField] private float climbingAngularVelocity = 40f;
        private float _angularVelocity;

        [SerializeField] private float ClickerSpeed = 720f;

        private float _pivotTarget;
        
        private float _climbingTarget;

        private bool deployed;
        
        private void Start()
        {
            _climbScorer = GetComponentInParent<ClimbScorer>();
            if (_climbScorer == null)
            {
                Debug.LogError("JITBClimber: ClimbScorer component not found in parent.");
            }
            
            deployPivot.SetPid(pidConstants); 
            climbPivot.SetPid(climbPid);
            intakeWheelL.SetPid(pidConstants);
            intakeWheelR.SetPid(pidConstants);
            _pivotTarget = -160;
            _angularVelocity = 0;
            _climbingTarget = 70;

            deployed = false;
        }

        private void LateUpdate()
        {
            deployPivot.UpdatePid(pidConstants);
            climbPivot.UpdatePid(climbPid);
        }

        // Update is called once per frame
        private void Update()
        {
            clickerL.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            clickerR.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
        
            clickerL1.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            clickerR1.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            
            clickerL2.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            clickerR2.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
        
        }

        private void FixedUpdate()
        {
            deployPivot.SetTargetAngle(_pivotTarget).withAxis(JointAxis.X);
            climbPivot.SetTargetAngle(_climbingTarget).withAxis(JointAxis.X);
            intakeWheelL.SetAngularVelocity(_angularVelocity).WithAxis(JointAxis.Y);
            intakeWheelR.SetAngularVelocity(-_angularVelocity).WithAxis(JointAxis.Y);
            intakeWheelGameObjectL.transform.Rotate(Vector3.up, -_intakeWheelSpeed * Time.fixedDeltaTime);
            intakeWheelGameObjectR.transform.Rotate(Vector3.up, _intakeWheelSpeed * Time.fixedDeltaTime);
            
            if (deployed && Utils.InAngularRange(deployPivot.GetSingleAxisAngle(JointAxis.X), 0, 1))
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
            
            deployed = true;
        }

        public void NotClimbing()
        {
            if (!deployed)
            {
                _pivotTarget = -160;
                _climbingTarget = 70;
            }
            else
            {
                _climbingTarget = 0;
            }
            
            if (deployed && Utils.InAngularRange(climbPivot.GetSingleAxisAngle(JointAxis.X), 0, 1))
            {
                climbPivot.lockAllAxis();
            }
            
            _angularVelocity = 0;
            _intakeWheelSpeed = 0;
        }
    }
}
