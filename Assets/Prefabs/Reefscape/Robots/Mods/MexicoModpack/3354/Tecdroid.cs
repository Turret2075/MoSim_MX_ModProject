using System;
using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using Games.Reefscape.Scoring.Scorers;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;
 
namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._3354
{
    public class Tecdroid : ReefscapeRobotBase
    {
        [Header("Robot Components")] 
        [SerializeField] private GenericJoint armJoint;
        [SerializeField] private GenericJoint wristJoint;
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericRoller leftIntakeRollerJoint;
        [SerializeField] private GenericRoller rightIntakeRollerJoint;
        [SerializeField] private GenericRoller topIntakeRoller;
        [SerializeField] private Transform leftIntakeSensor;
        [SerializeField] private Transform rightIntakeSensor;
        [SerializeField] private TecdroidClimb climber;
        [SerializeField] private ClimbScorer climbScorer;
        [SerializeField] private Transform algaeSlider;
 
         
        
        [Header("Animation Joints (Wheels)")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private float wheelIntakeSpeed = 500f;

        private bool _isScoring = false; // Prevents FixedUpdate from overriding scoring animation
 
        [SerializeField] private Collider l1POSCollider;
 
        [Header("PID Constants")] 
        [SerializeField] private PidConstants armPidConstants;
        [SerializeField] private PidConstants wristPidConstants;
        [SerializeField] private PidConstants climbPid;
        
        
        [SerializeField] private float pivotStep;
        private float _originalPivotMax;
        
        [Header("Robot Setpoints")] 
        [SerializeField] private TecdroidSetpoint stowSetpoint;
        [SerializeField] private TecdroidSetpoint coralStowSetpoint;
        [SerializeField] private TecdroidSetpoint algaeStowSetpoint;
        [SerializeField] private TecdroidSetpoint groundCoralIntakeSetpoint;
        [SerializeField] private TecdroidSetpoint groundAlgaeIntakeSetpoint;
        [SerializeField] private TecdroidSetpoint stationCoralIntakeSetpoint;
        [SerializeField] private TecdroidSetpoint stackAlgaeIntakeSetpoint;
        [SerializeField] private TecdroidSetpoint l4Setpoint;
        [SerializeField] private TecdroidSetpoint l4BackSetpoint;
        [SerializeField] private TecdroidSetpoint l4BackPlaceSetpoint;
        [SerializeField] private TecdroidSetpoint bargeFrontSetpoint;
        [SerializeField] private TecdroidSetpoint bargeFrontPlaceSetpoint;
        [SerializeField] private TecdroidSetpoint bargeBackSetpoint;
        [SerializeField] private TecdroidSetpoint bargeBackPlaceSetpoint;
 
        [SerializeField] private TecdroidSetpoint l3Setpoint;
        [SerializeField] private TecdroidSetpoint l3BackSetpoint;
        [SerializeField] private TecdroidSetpoint highAlgaeSetpoint;
        [SerializeField] private TecdroidSetpoint highAlgaeBackSetpoint;
        [SerializeField] private TecdroidSetpoint l2Setpoint;
        [SerializeField] private TecdroidSetpoint l2BackSetpoint;
        [SerializeField] private TecdroidSetpoint lowAlgaeSetpoint;
        [SerializeField] private TecdroidSetpoint lowAlgaeBackSetpoint;
        [SerializeField] private TecdroidSetpoint l1Setpoint;
        [SerializeField] private TecdroidSetpoint l1VertSetpoint;
        [SerializeField] private TecdroidSetpoint l1HighSetpoint;
        [SerializeField] private TecdroidSetpoint processorSetpoint;
        [SerializeField] private TecdroidSetpoint climbSetpoint;
        [SerializeField] private TecdroidSetpoint climbedSetpoint;
 
        private ReefscapeSetpoints _previousSetpoint = ReefscapeSetpoints.Stow;
 
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
 
        [Header("Game Piece Intakes")] 
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
 
        [Header("Game Piece States")] 
        [SerializeField] private string currentState;
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState coralBackStowState;
        [SerializeField] private GamePieceState coralFrontStowState;
        [SerializeField] private GamePieceState coralL1TargetState;
        [SerializeField] private GamePieceState algaeStowState;
        [SerializeField] private GamePieceState algaeHomeState;
        [SerializeField] private float algaeEjectForce;
 
        [Header("Intake Audio")] [SerializeField]
        private AudioSource intakeAudioSource;
 
        [SerializeField] private AudioClip intakeClip;
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallClip;
        
        [Header("Target Setpoints")] [SerializeField]
        private float _targetArmAngle;
 
        [SerializeField] private float _targetWristAngle;
        [SerializeField] private float _targetArmDistance;
        private bool _robotSpectialPressed;
        private bool _stationMode;
 
        protected override void Start()
        {
            base.Start();
            armJoint.SetPid(armPidConstants);
            wristJoint.SetPid(wristPidConstants);
            _originalPivotMax = armPidConstants.Max;
 
            _targetArmAngle = stowSetpoint.armAngle;
            _targetWristAngle = stowSetpoint.wristAngle;
            _targetArmDistance = stowSetpoint.armDistance;
 
            RobotGamePieceController.SetPreload(coralStowState);
 
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
 
            _coralController.gamePieceStates = new[] { coralIntakeState, coralStowState, coralBackStowState, coralFrontStowState, coralL1TargetState };
            _coralController.intakes.Add(coralIntake);
 
            _algaeController.gamePieceStates = new[] { algaeStowState, algaeHomeState };
            _algaeController.intakes.Add(algaeIntake);
            
            _robotSpectialPressed = false;
            _stationMode = false;
            
            intakeAudioSource.clip = intakeClip;
            intakeAudioSource.loop = true;
            intakeAudioSource.playOnAwake = false;
            
            algaeStallSource.clip = algaeStallClip;
            algaeStallSource.loop = true;
            algaeStallSource.playOnAwake = false;
        }
 
        private void LateUpdate()
        {
            armJoint.UpdatePid(armPidConstants);
            wristJoint.UpdatePid(wristPidConstants);
        }
 
        private void FixedUpdate()
        {
            armJoint.SetTargetAngle(_targetArmAngle).withAxis(JointAxis.X).flipDirection();
            wristJoint.SetTargetAngle(_targetWristAngle).withAxis(JointAxis.X).flipDirection().noWrap(-30);
            elevator.SetTarget(_targetArmDistance);
            
 
            var canIntakeCoral = _coralController.currentStateNum == 0 && IntakeAction.IsPressed() && _algaeController.currentStateNum == 0;
            var canIntakeAlgae = _algaeController.currentStateNum == 0 && IntakeAction.IsPressed() && _coralController.currentStateNum == 0;
            var realStep = pivotStep;
 
            if (algaeIntake.GamePiece != null)
            {
                var localSliderSpace = algaeIntake.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).x;
                algaeSlider.localPosition = new Vector3(-localSliderSpace, algaeSlider.localPosition.y, algaeSlider.localPosition.z);
            }
 
            if (Utils.WithinAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X), _targetArmAngle, 15f))
                armPidConstants.Max = Mathf.Max(armPidConstants.Max - (realStep * Time.fixedDeltaTime), realStep);
            else
                armPidConstants.Max = Mathf.Min(armPidConstants.Max + (realStep * Time.fixedDeltaTime), _originalPivotMax);
 
            l1POSCollider.enabled = (CurrentIntakeMode == ReefscapeIntakeMode.L1);
 
            var readState = _coralController.GetCurrentState();
            if (readState != null)
            {
                currentState = readState.name;
            }
            
            UpdateIntakeAudio();
 
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled) return;
 
            _algaeController.SetTargetState(_algaeController.currentStateNum > 0 ? algaeHomeState : algaeStowState);
            CheckStationMode();
            
            if ((_previousSetpoint == ReefscapeSetpoints.RobotSpecial && IntakeAction.IsPressed()) || (_stationMode && IntakeAction.IsPressed() && CurrentSetpoint != ReefscapeSetpoints.HighAlgae && CurrentSetpoint != ReefscapeSetpoints.LowAlgae && CurrentRobotMode != ReefscapeRobotMode.Algae))
            {
                SetState(ReefscapeSetpoints.RobotSpecial);
            }
 
            // --- IMPROVED WHEEL LOGIC ---
            // We only run this if we are NOT in the middle of a scoring coroutine
            if (!_isScoring)
            {
                bool isIntaking = (CurrentSetpoint == ReefscapeSetpoints.Intake || CurrentSetpoint == ReefscapeSetpoints.RobotSpecial || CurrentSetpoint == ReefscapeSetpoints.Stack) && IntakeAction.IsPressed();
 
                if (isIntaking)
                {
                    foreach (var wheel in intakeWheels)
                        wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.X);
                }
                else
                {
                    // Regular stopping of rollers
                    leftIntakeRollerJoint.ChangeAngularVelocity(0);
                    rightIntakeRollerJoint.ChangeAngularVelocity(0);
                    topIntakeRoller.ChangeAngularVelocity(0);
                    
                    // Explicitly stop wheel animations
                    foreach (var wheel in intakeWheels)
                        wheel.VelocityRoller(0).useAxis(JointAxis.X);
                }
            }
 
            if (CurrentSetpoint is ReefscapeSetpoints.Climb or ReefscapeSetpoints.Climbed) DriveController.SetDriveMp(0.5f);
            else if (CurrentSetpoint is ReefscapeSetpoints.Barge || LastSetpoint == ReefscapeSetpoints.Barge) DriveController.SetDriveMp(0.8f);
            else DriveController.SetDriveMp(1);
 
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (_coralController.currentStateNum != 0 || _algaeController.currentStateNum != 0)
                    {
                        SetSetpoint(_coralController.currentStateNum > 0 ? CurrentIntakeMode == ReefscapeIntakeMode.L1 ? l1HighSetpoint : coralStowSetpoint : algaeStowSetpoint);
                        _coralController.SetTargetState(coralStowState);
                        break;
                    }
                    SetSetpoint(stowSetpoint);
                    _coralController.SetTargetState(coralStowState);

                    if (climber != null) climber.NotClimbing();
                    break;
                case ReefscapeSetpoints.Intake:
                    if (LastSetpoint == ReefscapeSetpoints.RobotSpecial) { SetState(ReefscapeSetpoints.RobotSpecial); break; }
                    SetSetpoint(CurrentRobotMode == ReefscapeRobotMode.Coral ? groundCoralIntakeSetpoint : groundAlgaeIntakeSetpoint);
                    _coralController.SetTargetState(CurrentIntakeMode == ReefscapeIntakeMode.L1 ? coralL1TargetState : coralIntakeState);
                    _coralController.RequestIntake(coralIntake, canIntakeCoral && CurrentRobotMode == ReefscapeRobotMode.Coral);
                    _algaeController.RequestIntake(algaeIntake, canIntakeAlgae && CurrentRobotMode == ReefscapeRobotMode.Algae);
                    break;
                case ReefscapeSetpoints.Place:
                    StartCoroutine(PlaceGamePiece(LastSetpoint, readState));
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(CurrentIntakeMode == ReefscapeIntakeMode.L1 ? l1Setpoint : l1VertSetpoint);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processorSetpoint);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(stackAlgaeIntakeSetpoint);
                    _algaeController.RequestIntake(algaeIntake, canIntakeAlgae);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(FacingReef ? l2Setpoint : l2BackSetpoint);
                    _coralController.SetTargetState(FacingReef ? coralFrontStowState : coralBackStowState);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? lowAlgaeSetpoint : lowAlgaeBackSetpoint);
                    _algaeController.RequestIntake(algaeIntake, canIntakeAlgae);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(FacingReef ? l3Setpoint : l3BackSetpoint);
                    _coralController.SetTargetState(FacingReef ? coralFrontStowState : coralBackStowState);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? highAlgaeSetpoint : highAlgaeBackSetpoint);
                    _algaeController.RequestIntake(algaeIntake, canIntakeAlgae);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(FacingReef ? l4Setpoint : l4BackSetpoint);
                    _coralController.SetTargetState(FacingReef ? coralFrontStowState : coralBackStowState);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(FacingBarge() ? bargeFrontSetpoint : bargeBackSetpoint);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetSetpoint(stationCoralIntakeSetpoint);
                    _coralController.SetTargetState(coralStowState);
                    _coralController.RequestIntake(coralIntake, canIntakeCoral);
                    break;
                case ReefscapeSetpoints.Climb: 
                    SetSetpoint(climbSetpoint);
                    climber.Climb();
                    if (climber != null) climber.Climb(); 
                    break;
                
                case ReefscapeSetpoints.Climbed: 
                    SetSetpoint(climbedSetpoint);
                    climber.NotClimbing();
                    climber.RetractArm();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
 
            // Raycast logic remains the same...
            if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
            {
                _coralController.MoveIntake(coralIntake, coralL1TargetState.stateTarget);
                _coralController.SetTargetState(coralL1TargetState);
                if (leftIntakeRollerJoint.gameObject.activeSelf)
                {
                    leftIntakeRollerJoint.gameObject.SetActive(false);
                    rightIntakeRollerJoint.gameObject.SetActive(false);
                }
            }
            else
            {
                _coralController.MoveIntake(coralIntake, coralIntakeState.stateTarget);
                if (!leftIntakeRollerJoint.gameObject.activeSelf)
                {
                    leftIntakeRollerJoint.gameObject.SetActive(true);
                    rightIntakeRollerJoint.gameObject.SetActive(true);
                }
 
                var rayDirection = coralIntakeState.stateTarget.forward;
                var distance = 0.0254f * 5f;
                var coralMask = LayerMask.GetMask("Coral");
                var coralRight = Physics.Raycast(rightIntakeSensor.position, rayDirection, distance, coralMask);
                var coralLeft = Physics.Raycast(leftIntakeSensor.position, rayDirection, distance, coralMask);
 
                if (IntakeAction.IsPressed() && CurrentSetpoint != ReefscapeSetpoints.LowAlgae && CurrentSetpoint != ReefscapeSetpoints.HighAlgae)
                {
                    if (coralRight && coralLeft)
                    {
                        leftIntakeRollerJoint.ChangeAngularVelocity(8000);
                        rightIntakeRollerJoint.ChangeAngularVelocity(8000);
                    }
                }
            }
            

            // Climber and Drive modifiers remain the same...
            if (climbScorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climb && climber.WingsOpen())
            {
                climber.PlayClick();
                SetState(ReefscapeSetpoints.Climbed);
            }
            else if (!climbScorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climbed)
                SetState(ReefscapeSetpoints.Climb);
 
            _previousSetpoint = CurrentSetpoint;
        }
 
        private IEnumerator PlaceGamePiece(ReefscapeSetpoints lastSetpoint, GamePieceState readState)
        {
            _isScoring = true; // Lock FixedUpdate intake wheels
            
            // Front (FacingReef) -> Spin Same Way (+)
            // Back (Not FacingReef) -> Spin Opposite Way (-)
            float speed = FacingReef ? wheelIntakeSpeed : -wheelIntakeSpeed;
 
            foreach (var wheel in intakeWheels)
                wheel.VelocityRoller(speed).useAxis(JointAxis.X);
 
            if (lastSetpoint is ReefscapeSetpoints.Barge)
            {
                _targetArmAngle = FacingBarge() ? bargeFrontPlaceSetpoint.armAngle : bargeBackPlaceSetpoint.armAngle;
                _targetWristAngle = FacingBarge() ? bargeFrontPlaceSetpoint.wristAngle : bargeBackPlaceSetpoint.wristAngle;
                _targetArmDistance = FacingBarge() ? bargeFrontPlaceSetpoint.armDistance : bargeBackPlaceSetpoint.armDistance;
                yield return new WaitForSeconds(0.075f);
            }
            else if ((lastSetpoint == ReefscapeSetpoints.L1 && CurrentIntakeMode != ReefscapeIntakeMode.L1))
            {
                leftIntakeRollerJoint.ChangeAngularVelocity(1000);
                rightIntakeRollerJoint.ChangeAngularVelocity(-1000);
                topIntakeRoller.flipVelocity();
            }
            else if ((lastSetpoint is not ReefscapeSetpoints.Processor && !FacingReef))
            {
                leftIntakeRollerJoint.flipVelocity();
                rightIntakeRollerJoint.flipVelocity();
                topIntakeRoller.flipVelocity();
            }
 
            Vector3 force;
            if (CurrentIntakeMode == ReefscapeIntakeMode.L1 || (readState != null && readState.stateNum == coralL1TargetState.stateNum))
                force = new Vector3(1, 0, 0);
            else
            {
                force = FacingReef ? new Vector3(0, 0, -5) : new Vector3(0, 0, 5);
                if (LastSetpoint == ReefscapeSetpoints.L1) force = new Vector3(0, 0, 2f);
            }
 
            _coralController.ReleaseGamePieceWithForce(force);
            _algaeController.ReleaseGamePieceWithForce(new Vector3(0, algaeEjectForce, 0));
 
            if (lastSetpoint is ReefscapeSetpoints.L4 && !FacingReef)
            {
                yield return new WaitForSeconds(0.05f);
                _targetArmAngle = l4BackPlaceSetpoint.armAngle;
                _targetWristAngle = l4BackPlaceSetpoint.wristAngle;
                _targetArmDistance = l4BackPlaceSetpoint.armDistance;
            }
 
            // Wait until game pieces are released (state becomes 0) or timeout after 0.5s
            float timer = 0f;
            while ((_coralController.currentStateNum != 0 || _algaeController.currentStateNum != 0) && timer < 0.5f)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Explicitly stop wheels
            foreach (var wheel in intakeWheels) 
                wheel.VelocityRoller(0).useAxis(JointAxis.X);
                
            _isScoring = false; // Release lock
        }
 
        private bool FacingBarge()
        {
        return (transform.position.x > 0 && transform.rotation.eulerAngles.y > 180) || (transform.position.x <= 0 && transform.rotation.eulerAngles.y <= 180);
        }
 
        private void CheckStationMode()
        {
            if (RobotSpecialAction.IsPressed() && !_robotSpectialPressed && BaseGameManager.Instance.RobotState == RobotState.Enabled)
                _stationMode = !_stationMode;
 
            CurrentCoralStationMode.DropType = _stationMode ? DropType.Station : DropType.Ground;
            _robotSpectialPressed = RobotSpecialAction.IsPressed();
        }
        
        private void UpdateIntakeAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (intakeAudioSource.isPlaying || algaeStallSource.isPlaying)
                {
                    intakeAudioSource.Stop();
                    algaeStallSource.Stop();
                }
 
                return;
            }
 
            if ((IntakeAction.IsPressed() || OuttakeAction.IsPressed() || CurrentSetpoint is ReefscapeSetpoints.Climb) &&
                !intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Play();
            }
            else if (!IntakeAction.IsPressed() && !OuttakeAction.IsPressed() && CurrentSetpoint is not ReefscapeSetpoints.Climb &&
                     intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Stop();
            }
 
            if (RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0 && !algaeStallSource.isPlaying)
            {
                algaeStallSource.Play();
            }
            else if (RobotGamePieceController.GetPieceByName("Algae").currentStateNum == 0 && algaeStallSource.isPlaying)
            {
                algaeStallSource.Stop();
            }
        }
        
    
        private IEnumerator RotateArmFirst(TecdroidSetpoint setpoint)
        {
            _targetArmAngle = setpoint.armAngle;
            _targetWristAngle = setpoint.wristAngle;
            yield return new WaitForSeconds(0.65f);
            _targetArmDistance = setpoint.armDistance;
        }
 
        private void SetSetpoint(TecdroidSetpoint setpoint)
        {
            _targetArmAngle = setpoint.armAngle;
            _targetWristAngle = setpoint.wristAngle;
            _targetArmDistance = setpoint.armDistance;
        }
    }
}