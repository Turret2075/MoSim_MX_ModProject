using System;
using System.Collections;
using System.Diagnostics;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using GameSystems.Management;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework;
using RobotFramework.Components;
using RobotFramework.Controllers.Drivetrain;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._604
{
    public class QuixilverB: ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint shooterPivot;
        [SerializeField] private GenericJoint intakePivot;
        [SerializeField] private GenericRoller leftIntakeRollerJoint;
        [SerializeField] private GenericRoller rightIntakeRollerJoint;
        [SerializeField] private GenericRoller topIntakeRoller;
        [SerializeField] private Transform leftIntakeSensor;
        [SerializeField] private Transform rightIntakeSensor;
        [SerializeField] private GenericJoint slideJoint;
        
        [Header("Animation Joints (Wheels)")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private float wheelIntakeSpeed = 500f;
        [SerializeField] private GenericAnimationJoint[] shooterWheels;
        [SerializeField] private float shooterWheelSpeed = 1000f;

        [Header("PIDs")]
        [SerializeField] private PidConstants shooterPivotPid;
        [SerializeField] private PidConstants intakePivotPid;

        [Header("Intakes")] [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        
        [Header("Game Piece Stow States")]
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState intakeStowState;
        
        [Header("Setpoints")]
        [SerializeField] private QuixilverBSetpoint stow;
        [SerializeField] private QuixilverBSetpoint intake;
        [SerializeField] private QuixilverBSetpoint transfer;
        [SerializeField] private QuixilverBSetpoint l1;
        [SerializeField] private QuixilverBSetpoint l2;
        [SerializeField] private QuixilverBSetpoint l3;
        [SerializeField] private QuixilverBSetpoint l4;
        [SerializeField] private QuixilverBSetpoint highAlgae;
        [SerializeField] private QuixilverBSetpoint lowAlgae;
        
        [Header("Shooting Forces")]
        [SerializeField] private Vector3 l1Force;
        [SerializeField] private Vector2 l1DelayTorque;
        [SerializeField] private Vector3 l2Force;
        [SerializeField] private Vector2 l2DelayTorque;
        [SerializeField] private Vector3 l3Force;
        [SerializeField] private Vector2 l3DelayTorque;
        [SerializeField] private Vector3 l4Force;
        [SerializeField] private Vector2 l4DelayTorque;
        [SerializeField] private Vector3 highAlgaeForce;
        [SerializeField] private Vector2 highAlgaeDelayTorque;
        [SerializeField] private Vector3 lowAlgaeForce;
        [SerializeField] private Vector2 lowAlgaeDelayTorque;
        
        [Header("Intake Audio")]
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeClip;
        
        [Header("Shooter Audio")]
        [SerializeField] private AudioSource shooterAudioSource;
        [SerializeField] private AudioClip shooterClip;
        
        [Header("Auto Align Offsets")]
        [SerializeField] private Vector3 regularAutoAlignOffset;
        [SerializeField] private Vector3 daleAutoAlignOffsetLeft;
        [SerializeField] private Vector3 daleAutoAlignOffsetRight;
        
        [Header("Miscellaneous")]
        [SerializeField] private GameObject coralStowStateGameObject;

        private float _elevatorTargetHeight;
        private float _intakeTargetAngle;
        private float _shooterTargetAngle;
        private bool _handoff;
        private bool _intakeWheelsSpinning;
        private bool _shooterWheelsSpinning;
        private bool _isShooting;
        private bool _lockDriving;

        // private int _levelSelected;
        private ReefscapeAutoAlign _align;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        
        protected override void Start()
        {
            base.Start();
            
            shooterPivot.SetPid(shooterPivotPid);
            intakePivot.SetPid(intakePivotPid);
            
            _elevatorTargetHeight = 0;
            _intakeTargetAngle = 0;
            _shooterTargetAngle = 0;
            
            _intakeWheelsSpinning = false;
            _shooterWheelsSpinning = false;
            _isShooting = false;
            _lockDriving = false;

            _handoff = true;
            // _levelSelected = 0;
            _align = GetComponent<ReefscapeAutoAlign>();
            
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            
            _coralController.gamePieceStates = new[]
            {
                coralStowState,
                intakeStowState
            };
            _coralController.intakes.Add(coralIntake);
            
            intakeAudioSource.clip = intakeClip;
            intakeAudioSource.loop = true;
            intakeAudioSource.playOnAwake = false;
            
            shooterAudioSource.clip = shooterClip;
            shooterAudioSource.loop = true;
            shooterAudioSource.playOnAwake = false;
        }

        private IEnumerator PlacePiece()
        {
            if (!_coralController.HasPiece()) yield break;
            var coral = FindChildWithPrefix(coralStowStateGameObject.gameObject.transform, "Coral").gameObject;

            if (GetLevelByState() > 0)
            {
                DriveController.overideInput(new Vector2(0, 0), 0, DriveController.DriveMode.FieldOriented);
                shooterWheels[0].VelocityRoller(-shooterWheelSpeed).useAxis(JointAxis.X);
                shooterWheels[1].VelocityRoller(-shooterWheelSpeed).useAxis(JointAxis.X);
                shooterWheels[2].VelocityRoller(-shooterWheelSpeed).useAxis(JointAxis.X);
                shooterWheels[3].VelocityRoller(-shooterWheelSpeed).useAxis(JointAxis.X);
                shooterWheels[4].VelocityRoller(shooterWheelSpeed).useAxis(JointAxis.X);
                shooterWheels[5].VelocityRoller(shooterWheelSpeed).useAxis(JointAxis.X);
                shooterWheels[6].VelocityRoller(shooterWheelSpeed).useAxis(JointAxis.X);
                shooterWheels[7].VelocityRoller(shooterWheelSpeed).useAxis(JointAxis.X);
                _shooterWheelsSpinning = true;
                _isShooting = true;
                _lockDriving = true;
            }
            
            switch (GetLevelByState())
            {
                case 1:
                    Debug.Log("1");
                    _coralController.ReleaseGamePieceWithForce(l1Force);
                    yield return new WaitForSeconds(l1DelayTorque.x);
                    coral.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(l1DelayTorque.y, 0, 0));
                    break;
                case 2:
                    Debug.Log("2");
                    _coralController.ReleaseGamePieceWithForce(l2Force);
                    yield return new WaitForSeconds(l2DelayTorque.x);
                    coral.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(l2DelayTorque.y, 0, 0));
                    break;
                case 3:
                    Debug.Log("3");
                    _coralController.ReleaseGamePieceWithForce(l3Force);
                    yield return new WaitForSeconds(l3DelayTorque.x);
                    coral.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(l3DelayTorque.y, 0, 0));
                    break;
                case 4:
                    Debug.Log("4");
                    _coralController.ReleaseGamePieceWithForce(l4Force);
                    yield return new WaitForSeconds(l4DelayTorque.x);
                    coral.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(l4DelayTorque.y, 0, 0));
                    break;
                case 5:
                    Debug.Log("5");
                    _coralController.ReleaseGamePieceWithForce(lowAlgaeForce);
                    yield return new WaitForSeconds(lowAlgaeDelayTorque.x);
                    coral.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(lowAlgaeDelayTorque.y, 0, 0));
                    break;
                case 6:
                    Debug.Log("6");
                    _coralController.ReleaseGamePieceWithForce(highAlgaeForce);
                    yield return new WaitForSeconds(highAlgaeDelayTorque.x);
                    coral.GetComponent<Rigidbody>().AddRelativeTorque(new Vector3(highAlgaeDelayTorque.y, 0, 0));
                    break;
                default:
                    break;
            }

            yield return new WaitForSeconds(0.1f);
            _lockDriving = false;

            yield return new WaitForSeconds(0.2f);
            foreach (var wheel in shooterWheels)
                wheel.VelocityRoller(0).useAxis(JointAxis.X);
            _shooterWheelsSpinning = false;
            _isShooting = false;
            
            SetRobotMode(ReefscapeRobotMode.Coral);
            yield return null;
        }

        private void SetSetpoint(QuixilverBSetpoint setpoint)
        {
            _elevatorTargetHeight = 3.5f;
            _intakeTargetAngle = setpoint.intakeAngle;
            _shooterTargetAngle = setpoint.shooterAngle;
        }

        private void UpdateSetpoints()
        {
            // if (!_lockedIntakeSlide) elevator.SetTarget(_elevatorTargetHeight);
            elevator.SetTarget(_elevatorTargetHeight);
            intakePivot.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X).flipDirection().noWrap(-90f);
            shooterPivot.SetTargetAngle(_shooterTargetAngle).withAxis(JointAxis.X).flipDirection();
        }

        private void LateUpdate()
        {
            shooterPivot.UpdatePid(shooterPivotPid);
            intakePivot.UpdatePid(intakePivotPid);
        }
        
        private Transform FindChildWithPrefix(Transform parent, string prefix)
        {
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith(prefix))
                {
                    return child;
                }
            }
        
            Debug.LogWarning($"No child found starting with '{prefix}'");
            return null;
        }
        
        private int GetLevelByState()
        {
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
                case ReefscapeSetpoints.LowAlgae:
                    return 5;
                case ReefscapeSetpoints.HighAlgae:
                    return 6;
            }
            
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
                case ReefscapeSetpoints.LowAlgae:
                    return 5;
                case ReefscapeSetpoints.HighAlgae:
                    return 6;
            }

            return 0;
        }
        
        private bool CoralAtStow(GamePieceState stowState)
        {
            return _coralController.currentStateNum == stowState.stateNum && _coralController.atTarget;
        }
        
        private bool AtSetpoint(QuixilverBSetpoint stp)
        {
            return
                Utils.InAngularRange((-shooterPivot.GetSingleAxisAngle(JointAxis.X))+360, stp.shooterAngle, 2f) &&
                Utils.InAngularRange(-intakePivot.GetSingleAxisAngle(JointAxis.X)+360, stp.intakeAngle, 2f);
        }
    
        private bool AtSetpoint()
        {
            return
                Utils.InAngularRange(shooterPivot.GetSingleAxisAngle(JointAxis.X), _shooterTargetAngle, 2f) &&
                Utils.InAngularRange(intakePivot.GetSingleAxisAngle(JointAxis.X), _intakeTargetAngle, 2f);
        }


        private void FixedUpdate()
        {
            bool hasCoral = _coralController.HasPiece();
            
            // Debug.Log((GetActiveCamera().transform.eulerAngles.y > 180) + ":" + (PlayerPrefs.GetInt("PerspectiveAutoAlign", 1)) + ":" + (Math.Abs(transform.position.x) > 4.489323));
            
            // UpdateIntakeAudio();

            if (CurrentRobotMode == ReefscapeRobotMode.Coral)
            {
                _align.offset = regularAutoAlignOffset;
            }
            else
            {
                var flip = false;
                // if (PlayerPrefs.GetInt("PerspectiveAutoAlign", 1) != 1) flip = !flip;
                if (GetActiveCamera().transform.eulerAngles.y < 180) flip = !flip;
                if (Math.Abs(transform.position.x) > 4.489323 && PlayerPrefs.GetInt("PerspectiveAutoAlign", 1) == 1) flip = !flip;
                if (transform.position.x > 0) flip = !flip;
                
                if (AutoAlignLeftAction.inProgress)
                {
                    _align.offset = flip ? daleAutoAlignOffsetLeft : daleAutoAlignOffsetRight;
                }
                else
                {
                    _align.offset = flip ? daleAutoAlignOffsetRight : daleAutoAlignOffsetLeft;
                }
            }
            
            // --- IMPROVED WHEEL LOGIC ---
            // We only run this if we are NOT in the middle of a scoring coroutine
            // if (!_isScoring)
            // {
            bool isIntaking = CurrentSetpoint == ReefscapeSetpoints.Intake && IntakeAction.IsPressed();
            
            if (isIntaking)
            {
                intakeWheels[0].VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.Y);
                intakeWheels[1].VelocityRoller(-wheelIntakeSpeed).useAxis(JointAxis.Y);
                intakeWheels[2].VelocityRoller(-wheelIntakeSpeed).useAxis(JointAxis.X);
            }
            else
            {
                // Regular stopping of rollers
                leftIntakeRollerJoint.ChangeAngularVelocity(0);
                rightIntakeRollerJoint.ChangeAngularVelocity(0);
                topIntakeRoller.ChangeAngularVelocity(0);
            
                // Explicitly stop wheel animations
                intakeWheels[0].VelocityRoller(0).useAxis(JointAxis.Y);
                intakeWheels[1].VelocityRoller(0).useAxis(JointAxis.Y);
                intakeWheels[2].VelocityRoller(0).useAxis(JointAxis.X);
            }
            // }
            
            if (CoralAtStow(intakeStowState) && AtSetpoint(transfer))
            {
                _handoff = true;
            }
            
            _coralController.SetTargetState(_handoff ? coralStowState : intakeStowState);

            if (!hasCoral)
            {
                _handoff = false;
            }

            if (_lockDriving)
            {
                DriveController.overideInput(new Vector2(0, 0), 0, DriveController.DriveMode.FieldOriented);
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (hasCoral && !(CoralAtStow(coralStowState)))
                    {
                        SetSetpoint(transfer);
                    }
                    else
                    {
                        SetSetpoint(stow);
                    }
                    break;
                case ReefscapeSetpoints.Intake:
                    SetSetpoint((_coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget) ? transfer : intake);
                    
                    _coralController.RequestIntake(coralIntake, !hasCoral && AtSetpoint(intake));
                    break;
                case ReefscapeSetpoints.Place:
                    StartCoroutine(PlacePiece());
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(CoralAtStow(coralStowState) ? l1 : transfer);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(CoralAtStow(coralStowState) ? l2 : transfer);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    if (!hasCoral)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        break;
                    }
                    SetSetpoint(CoralAtStow(coralStowState) ? lowAlgae : transfer);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(CoralAtStow(coralStowState) ? l3 : transfer);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    if (!hasCoral)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        break;
                    }
                    SetSetpoint(CoralAtStow(coralStowState) ? highAlgae : transfer);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(CoralAtStow(coralStowState) ? l4 : transfer);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    break;
                case ReefscapeSetpoints.Climbed:
                    break;
            }
            
            // More 2910 intake logic
            _coralController.MoveIntake(coralIntake, intakeStowState.stateTarget);
            if (!leftIntakeRollerJoint.gameObject.activeSelf)
            {
                leftIntakeRollerJoint.gameObject.SetActive(true);
                rightIntakeRollerJoint.gameObject.SetActive(true);
            }
            
            var rayDirection = intakeStowState.stateTarget.forward;
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

            UpdateRollers();
            UpdateAudio();
            UpdateSetpoints();
        }

        private void UpdateRollers()
        {
            if (_coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget)
            {
                shooterWheels[0].VelocityRoller(-shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                shooterWheels[1].VelocityRoller(-shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                shooterWheels[2].VelocityRoller(-shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                shooterWheels[3].VelocityRoller(-shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                shooterWheels[4].VelocityRoller(shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                shooterWheels[5].VelocityRoller(shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                shooterWheels[6].VelocityRoller(shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                shooterWheels[7].VelocityRoller(shooterWheelSpeed/1.5f).useAxis(JointAxis.X);
                _shooterWheelsSpinning = true;
            }
            else if (!_isShooting)
            {
                foreach (var wheel in shooterWheels)
                    wheel.VelocityRoller(0).useAxis(JointAxis.X);
                _shooterWheelsSpinning = false;
            }

            if (_coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget)
            {
                intakeWheels[0].VelocityRoller(-wheelIntakeSpeed).useAxis(JointAxis.Y);
                intakeWheels[1].VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.Y);
                intakeWheels[2].VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.X);
                _intakeWheelsSpinning = true;
            }
            if (_coralController.atTarget)
            {
                intakeWheels[0].VelocityRoller(0).useAxis(JointAxis.Y);
                intakeWheels[1].VelocityRoller(0).useAxis(JointAxis.Y);
                intakeWheels[2].VelocityRoller(0).useAxis(JointAxis.X);
                _intakeWheelsSpinning = false;
            }
        }

        private void UpdateAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (intakeAudioSource.isPlaying)
                {
                    intakeAudioSource.Stop();
                }

                if (shooterAudioSource.isPlaying)
                {
                    shooterAudioSource.Stop();
                }

                return;
            }
            
            // Intake wheels
            bool isIntaking = IntakeAction.IsPressed() && _coralController.currentStateNum == intakeStowState.stateNum &&  !_coralController.atTarget;
            if ((isIntaking || _intakeWheelsSpinning) && !intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Play();
            }
            else if (!isIntaking && !_intakeWheelsSpinning && intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Stop();
            }
            
            // Shooter wheels
            if (_shooterWheelsSpinning && !shooterAudioSource.isPlaying)
            {
                shooterAudioSource.Play();
            }
            else if (!_shooterWheelsSpinning && shooterAudioSource.isPlaying)
            {
                shooterAudioSource.Stop();
            }
        }
        
        // private void UpdateIntakeAudio()
        // {
        //     if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
        //     {
        //         if (intakeAudioSource.isPlaying)
        //         {
        //             intakeAudioSource.Stop();
        //         }
        //
        //         return;
        //     }
        //
        //     if ((IntakeAction.IsPressed() || OuttakeAction.IsPressed() || CurrentSetpoint is ReefscapeSetpoints.Climb) &&
        //         !intakeAudioSource.isPlaying)
        //     {
        //         intakeAudioSource.Play();
        //     }
        //     else if (!IntakeAction.IsPressed() && !OuttakeAction.IsPressed() && CurrentSetpoint is not ReefscapeSetpoints.Climb &&
        //              intakeAudioSource.isPlaying)
        //     {
        //         intakeAudioSource.Stop();
        //     }
        //
        // }
    }
}