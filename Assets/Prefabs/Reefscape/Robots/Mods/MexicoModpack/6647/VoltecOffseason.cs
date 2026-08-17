using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Games.Reefscape.Enums;
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
using Robots.Climbing;
using UnityEngine;
using UnityEngine.UIElements;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._6647
{
    [RequireComponent(typeof(Rigidbody))]
    public class VoltecOffseasonFusion : ReefscapeRobotBase
    {
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint arm;
        [SerializeField] private GenericJoint climberPivot;
        [SerializeField] private GenericJoint intakePivot;

        [SerializeField] private PidConstants armPid;
        [SerializeField] private PidConstants climberPivotPid;
        [SerializeField] private PidConstants intakePivotPid;

        [SerializeField] private VoltecOffseasonSetpoint elevatorIntake;
        [SerializeField] private VoltecOffseasonSetpoint elevatorL1;
        [SerializeField] private VoltecOffseasonSetpoint elevatorL2;
        [SerializeField] private VoltecOffseasonSetpoint elevatorL3;
        [SerializeField] private VoltecOffseasonSetpoint elevatorL4;
        [SerializeField] private VoltecOffseasonSetpoint elevatorLowAlgae;
        [SerializeField] private VoltecOffseasonSetpoint elevatorHighAlgae;
        [SerializeField] private VoltecOffseasonSetpoint elevatorBarge;
        [SerializeField] private VoltecOffseasonSetpoint elevatorStow;
        [SerializeField] private VoltecOffseasonSetpoint elevatorCoralPickup;
        [SerializeField] private VoltecOffseasonSetpoint elevatorGroundAlgae;
        [SerializeField] private VoltecOffseasonSetpoint elevatorProcessor;
        [SerializeField] private VoltecOffseasonSetpoint elevatorClimb;

        [SerializeField] private VoltecOffseasonSetpoint intakeUp;
        [SerializeField] private VoltecOffseasonSetpoint intakeDown;

        [SerializeField] private VoltecOffseasonSetpoint armAlgaeIntake;
        [SerializeField] private VoltecOffseasonSetpoint armDown;
        [SerializeField] private VoltecOffseasonSetpoint armCoralScoring;
        [SerializeField] private VoltecOffseasonSetpoint armPrepareForCoralScoring;
        [SerializeField] private VoltecOffseasonSetpoint armStow;
        [SerializeField] private VoltecOffseasonSetpoint armL1;
        [SerializeField] private VoltecOffseasonSetpoint armBarge;
        [SerializeField] private VoltecOffseasonSetpoint armGroundAlgae;

        // El VoltecOffseasonSetpoint no trae un campo para el climber (Voltec no lo necesitaba),
        // asi que estos quedan como floats simples en vez de ScriptableObjects.
        [Header("Climber Pivot (grados)")]
        [SerializeField] private float climberOut;
        [SerializeField] private float climberStow;
        [SerializeField] private float climberClimb;

        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [SerializeField] private Transform coralScanSource;

        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState algaeStowState;
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralIntermediatePoint1;
        [SerializeField] private GamePieceState coralIntermediatePoint2;
        [SerializeField] private GamePieceState coralIntermediatePoint3;
        [SerializeField] private GamePieceState coralIntermediatePoint4;
        [SerializeField] private GamePieceState coralCradleState;

        [SerializeField] private GenericAnimationJoint[] intakeRollers;

        [SerializeField] private GenericAnimationJoint climberRoller;
        [SerializeField] private GenericAnimationJoint reversedClimberRoller;

        [SerializeField] private GenericAnimationJoint[] transferRollers;
        [SerializeField] private GenericAnimationJoint[] reversedTransferRollers;

        [SerializeField] private List<GenericAnimationJoint> endEffectorRollers;
        [SerializeField] private List<GenericAnimationJoint> reversedEndEffectorRollers;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeSoundClip;
        [SerializeField] private AudioSource thunkAudioSource;
        [SerializeField] private AudioClip thunkSoundClip;
        [SerializeField] private AudioSource algaeStallAudioSource;
        [SerializeField] private AudioClip algaeStallSoundClip;
        [SerializeField] private AudioSource endEffectorAudioSource;
        [SerializeField] private AudioClip endEffectorSoundClip;
        [SerializeField] private AudioSource elevatorClickAudioSource;
        [SerializeField] private AudioClip elevatorClickSoundClip;
        [SerializeField] private AudioSource climberRollerAudioSource;
        [SerializeField] private AudioClip climberRollerSoundClip;
        [SerializeField] private AudioSource climberLatchAudioSource;
        [SerializeField] private AudioClip climberLatchSoundClip;

        [Header("Elevator Click Settings")]
        [SerializeField] private float elevatorClickHeight1 = 15f;
        [SerializeField] private float elevatorClickHeight2 = 25f;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        [SerializeField] private JITBClimber whaleClimber;
        [SerializeField] private BoxCollider climberTriggerBox;
        [SerializeField] private BoxCollider climberBox;
        [SerializeField] private BoxCollider testBox;

        [SerializeField] private ClimbScorer climbScorer;

        [SerializeField] public GameObject autoClimbTrapBox;

        private float _elevatorTargetHeight;
        private float _armTargetRot;
        private float _intakePivotTargetRot;
        private float _climberPivotTargetRot;
        private bool _isPickupCoroutineRunning = false;
        private bool _isPlacingPiece = false;

        [SerializeField] private float armTolerance;

        private float intakeRollerSpeed = 500f;
        private float climberRollerSpeed = 300f;
        private float endEffectorRollerSpeed = 500f;

        private const int kCoralStowStateNum = 7;
        private int kCradleStateNum = kCoralStowStateNum - 1;

        private bool _isBargeCoroutineRunning = false;
        private bool _waitingForSupercyclePickup = false;

        private bool _holdArmAfterPlace = false;
        private bool _wasInCradle = false;

        private bool _wasAboveHeight1 = false;
        private bool _wasAboveHeight2 = false;

        private Quaternion _lastIntakeRot;
        private Quaternion _lastEndEffectorRot;
        private bool _isElevatorAudioInitialized = false;

        private bool _areClimberRollersRunning = false;
        private bool _wasCageInClimberBox = false;

        [SerializeField] private MeshRenderer bumperRenderer;

        private ReefscapeSetpoints _setpointBeforeUpdate;
        private bool _intendsCoral;

        protected override void Start()
        {
            var interpolator = GetComponent<CustomRigidbodyInterpolation>();
            if (interpolator != null)
            {
                interpolator.enabled = false;
            }

            transform.Rotate(0f, 180f, 0f, Space.Self);
            if (arm != null) arm.SetPid(armPid);
            if (intakePivot != null) intakePivot.SetPid(intakePivotPid);
            if (climberPivot != null) climberPivot.SetPid(climberPivotPid);

            _armTargetRot = 0;
            _intakePivotTargetRot = 0;
            _elevatorTargetHeight = 0;

            base.Start();

            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
            _coralController.gamePieceStates = new[] { coralIntakeState, coralIntermediatePoint1, coralIntermediatePoint2, coralIntermediatePoint3, coralIntermediatePoint4, coralCradleState, coralStowState };
            _algaeController.gamePieceStates = new[] { algaeStowState };
            _coralController.intakes.Add(coralIntake);
            _algaeController.intakes.Add(algaeIntake);

            if (intakeRollers != null && intakeRollers.Length > 0 && intakeRollers[0] != null)
                _lastIntakeRot = intakeRollers[0].transform.localRotation;

            if (endEffectorRollers != null && endEffectorRollers.Count > 0 && endEffectorRollers[0] != null)
                _lastEndEffectorRot = endEffectorRollers[0].transform.localRotation;
        }

        private void SetElevatorSetpoint(VoltecOffseasonSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
        }

        private void SetIntakeSetpoint(VoltecOffseasonSetpoint setpoint)
        {
            _intakePivotTargetRot = setpoint.intakeAngle;
        }

        private void SetArmSetpoint(VoltecOffseasonSetpoint setpoint)
        {
            _armTargetRot = setpoint.armAngle;
        }

        private void SetClimberSetpoint(float climberPivotRotation)
        {
            _climberPivotTargetRot = climberPivotRotation;
        }

        private static readonly PropertyInfo _currentRobotModeProperty =
            typeof(ReefscapeRobotBase).GetProperty("CurrentRobotMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private void SetCurrentRobotMode(ReefscapeRobotMode mode)
        {
            if (_currentRobotModeProperty == null)
                return;

            var setMethod = _currentRobotModeProperty.GetSetMethod(true);
            if (setMethod != null)
            {
                setMethod.Invoke(this, new object[] { mode });
            }
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            arm.SetTargetAngle(_armTargetRot).withAxis(JointAxis.X);
            intakePivot.SetTargetAngle(_intakePivotTargetRot).withAxis(JointAxis.X);
            climberPivot.SetTargetAngle(_climberPivotTargetRot).withAxis(JointAxis.Z);
        }

        protected override void Update()
        {
            _setpointBeforeUpdate = CurrentSetpoint;

            bool coralInArm = _coralController != null &&
                               _coralController.HasPiece() &&
                               _coralController.currentStateNum == kCoralStowStateNum;

            if (coralInArm)
            {
                SetCurrentRobotMode(ReefscapeRobotMode.Coral);
            }

            base.Update();

            SyncBumperSubmeshes();

            bool hasCoral = _coralController != null && _coralController.HasPiece();
            bool hasAlgae = _algaeController != null && _algaeController.HasPiece();
            bool armHasAlgae = hasAlgae && _algaeController.currentStateNum == algaeStowState.stateNum;

            bool coralTransiting = hasCoral && _coralController.currentStateNum < kCradleStateNum;
            bool hasActiveCoral = coralInArm || coralTransiting || _isPickupCoroutineRunning;

            bool isCoralSetpoint = (_setpointBeforeUpdate == ReefscapeSetpoints.L1 ||
                                    _setpointBeforeUpdate == ReefscapeSetpoints.L2 ||
                                    _setpointBeforeUpdate == ReefscapeSetpoints.L3 ||
                                    _setpointBeforeUpdate == ReefscapeSetpoints.L4);

            bool intendsCoral = (hasActiveCoral || isCoralSetpoint) && !armHasAlgae && !_isPlacingPiece;
            _intendsCoral = intendsCoral;

            if (L1Action.triggered)
            {
                if (_setpointBeforeUpdate == ReefscapeSetpoints.Processor || _setpointBeforeUpdate == ReefscapeSetpoints.L1)
                {
                    SetState(ReefscapeSetpoints.Stow);
                }
                else if (hasAlgae)
                {
                    SetState(ReefscapeSetpoints.Processor);
                }
                else if (intendsCoral)
                {
                    SetState(ReefscapeSetpoints.L1);
                }
            }

            if (L2Action.triggered && _setpointBeforeUpdate != ReefscapeSetpoints.L2 && _setpointBeforeUpdate != ReefscapeSetpoints.LowAlgae)
            {
                if (intendsCoral && !hasAlgae)
                {
                    SetState(ReefscapeSetpoints.L2);
                }
                else
                {
                    SetState(ReefscapeSetpoints.LowAlgae);
                }
            }

            if (L3Action.triggered && _setpointBeforeUpdate != ReefscapeSetpoints.L3 && _setpointBeforeUpdate != ReefscapeSetpoints.HighAlgae)
            {
                if (intendsCoral && !hasAlgae)
                {
                    SetState(ReefscapeSetpoints.L3);
                }
                else
                {
                    SetState(ReefscapeSetpoints.HighAlgae);
                }
            }

            if (L4Action.triggered && _setpointBeforeUpdate != ReefscapeSetpoints.L4 && _setpointBeforeUpdate != ReefscapeSetpoints.Barge)
            {
                if (hasAlgae)
                {
                    SetState(ReefscapeSetpoints.Barge);
                }
                else if (intendsCoral)
                {
                    SetState(ReefscapeSetpoints.L4);
                }
            }
        }

        private void LateUpdate()
        {
            arm.UpdatePid(armPid);
            climberPivot.UpdatePid(climberPivotPid);
            intakePivot.UpdatePid(intakePivotPid);
        }

        private IEnumerator PlacePieceCoroutine()
        {
            _isPlacingPiece = true;
            bool hasAlgae = _algaeController.atTarget && _algaeController.currentStateNum == algaeStowState.stateNum;

            if (hasAlgae)
            {
                if ((LastSetpoint == ReefscapeSetpoints.Barge || LastSetpoint == ReefscapeSetpoints.L4) && isNear(elevator.GetElevatorHeight(), elevatorBarge.elevatorHeight, 3) && isNear(arm.GetSingleAxisAngle(JointAxis.X), armBarge.armAngle, 10))
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 2, 0));
                    yield return new WaitForSeconds(0.1f);
                    SetArmSetpoint(armStow);
                    yield return new WaitForSeconds(0.15f);
                }
                else if (LastSetpoint == ReefscapeSetpoints.Processor || LastSetpoint == ReefscapeSetpoints.L1)
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 2, 0));
                    _holdArmAfterPlace = true;
                    yield return new WaitForSeconds(0.15f);
                }
            }
            else
            {
                if (_coralController.atTarget && _coralController.currentStateNum == kCoralStowStateNum)
                {
                    if (LastSetpoint == ReefscapeSetpoints.L4)
                    {
                        yield return new WaitForSeconds(0.2f);
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L1)
                    {
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L1 || LastSetpoint == ReefscapeSetpoints.L2 || LastSetpoint == ReefscapeSetpoints.L3)
                    {
                        yield return new WaitForSeconds(0.09f);
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, -1f));
                    }
                }
            }
            _isPlacingPiece = false;
        }

        private IEnumerator WaitElevatorForL2Coroutine()
        {
            if (CurrentSetpoint == ReefscapeSetpoints.L2)
                yield return new WaitForSeconds(0.1f);
            SetElevatorSetpoint(elevatorL2);
        }

        private IEnumerator WaitElevatorForStowCoroutine()
        {
            if (isNear(elevator.GetElevatorHeight(), elevatorBarge.elevatorHeight, 10))
            {
                yield return new WaitForSeconds(0.75f);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (CurrentSetpoint == ReefscapeSetpoints.Stow)
                SetElevatorSetpoint(elevatorStow);
        }

        private IEnumerator WaitArmForBargeCoroutine()
        {
            _isBargeCoroutineRunning = true;
            yield return new WaitForSeconds(0.8f);

            if (CurrentSetpoint == ReefscapeSetpoints.Barge || CurrentSetpoint == ReefscapeSetpoints.L4)
            {
                SetArmSetpoint(armBarge);
            }

            _isBargeCoroutineRunning = false;
        }

        private IEnumerator WaitElevatorForPickupCoroutine()
        {
            _isPickupCoroutineRunning = true;
            yield return new WaitForSeconds(0.1f);
            SetElevatorSetpoint(elevatorCoralPickup);

            yield return new WaitForSeconds(0.1f);
            _coralController.SetTargetState(coralStowState.name);

            yield return new WaitForSeconds(0.15f);
            SetElevatorSetpoint(elevatorIntake);

            yield return new WaitForSeconds(0.2f);
            _isPickupCoroutineRunning = false;
        }

        private bool isNear(float current, float target, float tolerance)
        {
            return Math.Abs(current - target) < tolerance;
        }

        private void SetEndEffectorRollerSpeed(float speed)
        {
            if (endEffectorRollers != null)
            {
                foreach (var roller in endEffectorRollers)
                {
                    if (roller != null) roller.VelocityRoller(speed).useAxis(JointAxis.Y);
                }
            }

            if (reversedEndEffectorRollers != null)
            {
                foreach (var roller in reversedEndEffectorRollers)
                {
                    if (roller != null) roller.VelocityRoller(-speed).useAxis(JointAxis.Y);
                }
            }
        }

        private void PlayElevatorClick()
        {
            if (elevatorClickAudioSource != null)
            {
                if (elevatorClickSoundClip != null)
                {
                    elevatorClickAudioSource.PlayOneShot(elevatorClickSoundClip);
                }
                else
                {
                    elevatorClickAudioSource.Play();
                }
            }
        }

        private void FixedUpdate()
        {
            if (elevator != null)
            {
                float currentElevatorHeight = elevator.GetElevatorHeight();

                if (!_isElevatorAudioInitialized)
                {
                    _wasAboveHeight1 = currentElevatorHeight >= elevatorClickHeight1;
                    _wasAboveHeight2 = currentElevatorHeight >= elevatorClickHeight2;
                    _isElevatorAudioInitialized = true;
                }

                float deadband = 0.5f;

                if (!_wasAboveHeight1 && currentElevatorHeight > elevatorClickHeight1 + deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight1 = true;
                }
                else if (_wasAboveHeight1 && currentElevatorHeight < elevatorClickHeight1 - deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight1 = false;
                }

                if (!_wasAboveHeight2 && currentElevatorHeight > elevatorClickHeight2 + deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight2 = true;
                }
                else if (_wasAboveHeight2 && currentElevatorHeight < elevatorClickHeight2 - deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight2 = false;
                }
            }

            if (_coralController == null)
            {
                Debug.Log("coral not exist");
                return;
            }

            if (_algaeController == null)
            {
                Debug.Log("algae not exist");
                return;
            }

            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            bool armHasCoral = _coralController.currentStateNum == kCoralStowStateNum;
            bool armHasAlgae = _algaeController.HasPiece() && _algaeController.currentStateNum == algaeStowState.stateNum;
            bool isAlgaeMode = !armHasCoral && CurrentRobotMode == ReefscapeRobotMode.Algae;

            if (_coralController.atTarget && _coralController.currentStateNum == kCradleStateNum && armHasAlgae)
            {
                _waitingForSupercyclePickup = true;
            }

            if (CurrentSetpoint == ReefscapeSetpoints.Intake && !isAlgaeMode)
            {
                _waitingForSupercyclePickup = false;
            }

            bool coralInRollers = hasCoral && _coralController.currentStateNum >= 1 && _coralController.currentStateNum < kCradleStateNum;
            bool coralInCradle = hasCoral && _coralController.currentStateNum == kCradleStateNum;

            bool finishCoralTransit = hasCoral && _coralController.currentStateNum < kCoralStowStateNum;

            bool isCoralProcessing = coralInRollers || (coralInCradle && !_waitingForSupercyclePickup);

            bool isFrozen = (isCoralProcessing && !armHasAlgae) || _isPickupCoroutineRunning && CurrentSetpoint != ReefscapeSetpoints.Processor;

            ReefscapeSetpoints effectiveSetpoint = CurrentSetpoint;
            bool isIntakingCoral = false;
            bool allowFrameworkCoralIntake = true;

            if (isFrozen)
            {
                effectiveSetpoint = ReefscapeSetpoints.Intake;
            }
            else if (armHasAlgae)
            {
                if (effectiveSetpoint == ReefscapeSetpoints.L4)
                {
                    effectiveSetpoint = ReefscapeSetpoints.Barge;
                }
                else if (effectiveSetpoint == ReefscapeSetpoints.L1)
                {
                    effectiveSetpoint = ReefscapeSetpoints.Processor;
                }
                else if (effectiveSetpoint == ReefscapeSetpoints.L2 || effectiveSetpoint == ReefscapeSetpoints.L3)
                {
                    effectiveSetpoint = ReefscapeSetpoints.Stow;
                }
            }

            if (CurrentSetpoint != ReefscapeSetpoints.Processor && CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                _holdArmAfterPlace = false;
            }

            if (effectiveSetpoint == ReefscapeSetpoints.Climb && climbScorer.AutoClimbTriggered)
            {
                SetState(ReefscapeSetpoints.Climbed);
                effectiveSetpoint = ReefscapeSetpoints.Climbed;
                autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = false;
            }

            switch (effectiveSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;

                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetArmSetpoint(armStow);

                    if (coralInCradle && !armHasAlgae)
                    {
                    }
                    else
                    {
                        StartCoroutine(WaitElevatorForStowCoroutine());
                    }

                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, finishCoralTransit);
                    if (whaleClimber != null) whaleClimber.NotClimbing();
                    break;

                case ReefscapeSetpoints.Intake:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    if (armHasCoral)
                    {
                        break;
                    }

                    SetClimberSetpoint(climberStow);
                    SetIntakeSetpoint(intakeDown);

                    bool runningCoralIntake = !isAlgaeMode && (!hasCoral || _coralController.currentStateNum < kCradleStateNum);

                    allowFrameworkCoralIntake = runningCoralIntake;
                    if (runningCoralIntake && !hasCoral)
                    {
                        allowFrameworkCoralIntake = IsCoralOrientationValid();
                    }

                    bool runningAlgaeIntake = isAlgaeMode && !hasAlgae && !armHasCoral;

                    isIntakingCoral = runningCoralIntake;

                    if (isAlgaeMode)
                    {
                        if (armHasAlgae || armHasCoral)
                        {
                            SetArmSetpoint(armStow);
                        }
                        else
                        {
                            SetArmSetpoint(armGroundAlgae);
                            if (!_isPickupCoroutineRunning) SetElevatorSetpoint(elevatorGroundAlgae);
                        }
                    }
                    else
                    {
                        if (armHasCoral)
                        {
                            if (isNear(arm.GetSingleAxisAngle(JointAxis.X), armStow.armAngle, 45))
                            {
                                SetArmSetpoint(armStow);
                            }
                            else
                            {
                                SetArmSetpoint(armDown);
                                if (!_isPickupCoroutineRunning) SetElevatorSetpoint(elevatorIntake);
                            }
                        }
                        else if (armHasAlgae)
                        {
                            SetArmSetpoint(armStow);
                            SetElevatorSetpoint(elevatorStow);
                        }
                        else
                        {
                            SetArmSetpoint(armDown);
                            if (!_isPickupCoroutineRunning) SetElevatorSetpoint(elevatorIntake);
                        }
                    }

                    if (isFrozen)
                    {
                        SetArmSetpoint(armDown);
                        if (!_isPickupCoroutineRunning) SetElevatorSetpoint(elevatorIntake);
                    }

                    SetIntakeSetpoint(intakeDown);

                    _algaeController.RequestIntake(algaeIntake, runningAlgaeIntake);
                    _coralController.RequestIntake(coralIntake, allowFrameworkCoralIntake);

                    if (whaleClimber != null) whaleClimber.NotClimbing();
                    break;

                case ReefscapeSetpoints.Place:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;

                    SetClimberSetpoint(climberStow);
                    SetIntakeSetpoint(intakeDown);

                    if (armHasAlgae && (LastSetpoint == ReefscapeSetpoints.Barge || LastSetpoint == ReefscapeSetpoints.L4))
                    {
                        bool atBargeElevator = isNear(elevator.GetElevatorHeight(), elevatorBarge.elevatorHeight, 3f);
                        bool atBargeArm = isNear(arm.GetSingleAxisAngle(JointAxis.X), armBarge.armAngle, 10f);

                        if (!atBargeElevator || !atBargeArm)
                        {
                            SetState(LastSetpoint);
                            break;
                        }
                    }

                    if (LastSetpoint != ReefscapeSetpoints.L1 && !armHasAlgae && !_holdArmAfterPlace && (LastSetpoint == ReefscapeSetpoints.L1 || LastSetpoint == ReefscapeSetpoints.L2 || LastSetpoint == ReefscapeSetpoints.L3 || LastSetpoint == ReefscapeSetpoints.L4))
                        SetArmSetpoint(armCoralScoring);

                    this.StartCoroutine(PlacePieceCoroutine());
                    break;

                case ReefscapeSetpoints.L1:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetElevatorSetpoint(elevatorL1);
                    SetArmSetpoint(armL1);
                    break;

                case ReefscapeSetpoints.Stack:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetElevatorSetpoint(elevatorIntake);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                    _coralController.RequestIntake(coralIntake, false);
                    break;

                case ReefscapeSetpoints.L2:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetArmSetpoint(armPrepareForCoralScoring);
                    this.StartCoroutine(WaitElevatorForL2Coroutine());
                    break;

                case ReefscapeSetpoints.LowAlgae:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetElevatorSetpoint(elevatorLowAlgae);
                    SetArmSetpoint(armAlgaeIntake);
                    _algaeController.RequestIntake(algaeIntake, !hasAlgae);
                    break;

                case ReefscapeSetpoints.L3:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetElevatorSetpoint(elevatorL3);
                    SetArmSetpoint(armPrepareForCoralScoring);
                    break;

                case ReefscapeSetpoints.HighAlgae:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetElevatorSetpoint(elevatorHighAlgae);
                    SetArmSetpoint(armAlgaeIntake);
                    _algaeController.RequestIntake(algaeIntake, !hasAlgae);
                    break;

                case ReefscapeSetpoints.L4:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetElevatorSetpoint(elevatorL4);
                    SetArmSetpoint(armPrepareForCoralScoring);
                    break;

                case ReefscapeSetpoints.Processor:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    if (!_holdArmAfterPlace)
                    {
                        SetArmSetpoint(armGroundAlgae);
                    }
                    if (!_isPickupCoroutineRunning) SetElevatorSetpoint(elevatorProcessor);
                    break;

                case ReefscapeSetpoints.Barge:
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetElevatorSetpoint(elevatorBarge);
                    if (!_isBargeCoroutineRunning && !isNear(_armTargetRot, armBarge.armAngle, 2f))
                    {
                        StartCoroutine(WaitArmForBargeCoroutine());
                    }
                    break;

                case ReefscapeSetpoints.RobotSpecial:
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    SetClimberSetpoint(climberStow);

                    SetIntakeSetpoint(intakeDown);
                    SetState(ReefscapeSetpoints.Stow);

                    stopClimberRollers();
                    break;

                case ReefscapeSetpoints.Climb:
                    if (hasAlgae)
                    {
                        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 2, 0));
                    }
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = true;
                    rotateClimberRollers();
                    SetClimberSetpoint(climberOut);
                    SetIntakeSetpoint(intakeUp);
                    SetElevatorSetpoint(elevatorClimb);
                    SetArmSetpoint(armStow);
                    if (whaleClimber != null) whaleClimber.Climb();
                    break;

                case ReefscapeSetpoints.Climbed:
                    SetClimberSetpoint(climberClimb);
                    stopClimberRollers();
                    autoClimbTrapBox.GetComponent<BoxCollider>().isTrigger = false;
                    if (whaleClimber != null) whaleClimber.NotClimbing();
                    break;
            }

            UpdateSetpoints();

            if ((effectiveSetpoint == ReefscapeSetpoints.Intake && allowFrameworkCoralIntake) || finishCoralTransit)
            {
                if (!_coralController.HasPiece() && !(_coralController.currentStateNum == kCoralStowStateNum))
                {
                    _coralController.SetTargetState(coralIntakeState.name);
                }
                else if (_coralController.atTarget && _coralController.currentStateNum == 1)
                {
                    _coralController.SetTargetState(coralIntermediatePoint1.name);
                }
                else if (_coralController.atTarget && _coralController.currentStateNum == 2)
                {
                    _coralController.SetTargetState(coralIntermediatePoint2.name);
                }
                else if (_coralController.atTarget && _coralController.currentStateNum == 3)
                {
                    _coralController.SetTargetState(coralIntermediatePoint3.name);
                }
                else if (_coralController.atTarget && _coralController.currentStateNum == 4)
                {
                    _coralController.SetTargetState(coralIntermediatePoint4.name);
                }
                else if (_coralController.atTarget && _coralController.currentStateNum == 5)
                {
                    _coralController.SetTargetState(coralCradleState.name);
                }
                else if (_coralController.atTarget && _coralController.currentStateNum == kCradleStateNum)
                {
                    if (!armHasAlgae && isNear(arm.GetSingleAxisAngle(JointAxis.X), 210, 5) && isNear(elevator.GetElevatorHeight(), 50, 2) && !_isPickupCoroutineRunning)
                    {
                        StartCoroutine(WaitElevatorForPickupCoroutine());
                    }
                }
            }

            if (effectiveSetpoint == ReefscapeSetpoints.LowAlgae || effectiveSetpoint == ReefscapeSetpoints.HighAlgae)
            {
                if (_algaeController.HasPiece())
                {
                    _algaeController.SetTargetState(algaeStowState.name);
                }
            }

            bool isTransitingToCradle = hasCoral && _coralController.currentStateNum < kCradleStateNum;

            if (_coralController.currentStateNum == kCradleStateNum && !_coralController.atTarget)
                isTransitingToCradle = true;

            bool isIntakeRunning = (effectiveSetpoint == ReefscapeSetpoints.Intake && isIntakingCoral) || isTransitingToCradle;

            if (isIntakeRunning)
            {
                foreach (var roller in intakeRollers)
                {
                    roller.VelocityRoller(intakeRollerSpeed);
                }
                foreach (var roller in transferRollers)
                {
                    roller.VelocityRoller(intakeRollerSpeed).useAxis(JointAxis.Y);
                }
                foreach (var roller in reversedTransferRollers)
                {
                    roller.VelocityRoller(-intakeRollerSpeed).useAxis(JointAxis.Y);
                }
            }
            else
            {
                foreach (var roller in intakeRollers)
                {
                    roller.VelocityRoller(0);
                }
                foreach (var roller in transferRollers)
                {
                    roller.VelocityRoller(0).useAxis(JointAxis.Y);
                }
                foreach (var roller in reversedTransferRollers)
                {
                    roller.VelocityRoller(0).useAxis(JointAxis.Y);
                }
            }

            bool reachedIntakeSoundPoint = hasCoral && _coralController.currentStateNum == kCradleStateNum - 2 && _coralController.atTarget;

            if (reachedIntakeSoundPoint)
            {
                if (!_wasInCradle)
                {
                    if (thunkAudioSource != null)
                    {
                        if (thunkSoundClip != null)
                        {
                            thunkAudioSource.PlayOneShot(thunkSoundClip);
                        }
                        else
                        {
                            thunkAudioSource.Play();
                        }
                    }
                    _wasInCradle = true;
                }
            }
            else
            {
                _wasInCradle = false;
            }

            if (armHasAlgae)
            {
                if (algaeStallAudioSource != null)
                {
                    if (algaeStallSoundClip != null && algaeStallAudioSource.clip != algaeStallSoundClip)
                    {
                        algaeStallAudioSource.clip = algaeStallSoundClip;
                    }
                    algaeStallAudioSource.loop = true;
                    if (!algaeStallAudioSource.isPlaying)
                    {
                        algaeStallAudioSource.Play();
                    }
                }
            }
            else
            {
                if (algaeStallAudioSource != null && algaeStallAudioSource.isPlaying)
                {
                    algaeStallAudioSource.Stop();
                }
            }

            if (elevator != null)
            {
                float currentElevatorHeight = elevator.GetElevatorHeight();
                float deadband = 0.5f;

                if (!_wasAboveHeight1 && currentElevatorHeight > elevatorClickHeight1 + deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight1 = true;
                }
                else if (_wasAboveHeight1 && currentElevatorHeight < elevatorClickHeight1 - deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight1 = false;
                }

                if (!_wasAboveHeight2 && currentElevatorHeight > elevatorClickHeight2 + deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight2 = true;
                }
                else if (_wasAboveHeight2 && currentElevatorHeight < elevatorClickHeight2 - deadband)
                {
                    PlayElevatorClick();
                    _wasAboveHeight2 = false;
                }
            }

            bool isHoldingPiece = armHasCoral || _algaeController.HasPiece();

            bool isValidScorePosition = (LastSetpoint == ReefscapeSetpoints.L1 ||
                                         LastSetpoint == ReefscapeSetpoints.L2 ||
                                         LastSetpoint == ReefscapeSetpoints.L3 ||
                                         LastSetpoint == ReefscapeSetpoints.L4 ||
                                         LastSetpoint == ReefscapeSetpoints.Processor ||
                                         LastSetpoint == ReefscapeSetpoints.Barge);

            bool isPlacingOrEjecting = (effectiveSetpoint == ReefscapeSetpoints.Place || _isPlacingPiece) && isValidScorePosition;

            bool isIntakingAlgae = (effectiveSetpoint == ReefscapeSetpoints.LowAlgae
                                   || effectiveSetpoint == ReefscapeSetpoints.HighAlgae
                                   || (effectiveSetpoint == ReefscapeSetpoints.Intake && isAlgaeMode))
                                   && !isHoldingPiece;

            bool isIntakingCoralEndEffector = (_isPickupCoroutineRunning
                                              || (effectiveSetpoint == ReefscapeSetpoints.Intake && !isAlgaeMode))
                                              && !isHoldingPiece;

            bool isEndEffectorCommanded = false;

            if (effectiveSetpoint == ReefscapeSetpoints.Processor)
            {
                SetEndEffectorRollerSpeed(0f);
            }
            else if (isPlacingOrEjecting)
            {
                SetEndEffectorRollerSpeed(-endEffectorRollerSpeed);
                isEndEffectorCommanded = true;
            }
            else if (isIntakingAlgae || isIntakingCoralEndEffector)
            {
                SetEndEffectorRollerSpeed(endEffectorRollerSpeed);
                isEndEffectorCommanded = true;
            }
            else
            {
                SetEndEffectorRollerSpeed(0f);
            }

            bool isIntakeVisuallySpinning = false;
            if (intakeRollers != null && intakeRollers.Length > 0 && intakeRollers[0] != null)
            {
                Quaternion currentIntakeRot = intakeRollers[0].transform.localRotation;
                if (Quaternion.Angle(currentIntakeRot, _lastIntakeRot) > 1.0f)
                {
                    isIntakeVisuallySpinning = true;
                }
                _lastIntakeRot = currentIntakeRot;
            }

            bool isEndEffectorVisuallySpinning = false;
            if (endEffectorRollers != null && endEffectorRollers.Count > 0 && endEffectorRollers[0] != null)
            {
                Quaternion currentEERot = endEffectorRollers[0].transform.localRotation;
                if (Quaternion.Angle(currentEERot, _lastEndEffectorRot) > 1.0f)
                {
                    isEndEffectorVisuallySpinning = true;
                }
                _lastEndEffectorRot = currentEERot;
            }

            if (isIntakeRunning && isIntakeVisuallySpinning)
            {
                if (intakeAudioSource != null)
                {
                    if (intakeSoundClip != null && intakeAudioSource.clip != intakeSoundClip)
                    {
                        intakeAudioSource.clip = intakeSoundClip;
                    }
                    intakeAudioSource.loop = true;
                    if (!intakeAudioSource.isPlaying)
                    {
                        intakeAudioSource.Play();
                    }
                }
            }
            else
            {
                if (intakeAudioSource != null && intakeAudioSource.isPlaying)
                {
                    intakeAudioSource.Stop();
                }
            }

            if (isEndEffectorCommanded && isEndEffectorVisuallySpinning)
            {
                if (endEffectorAudioSource != null)
                {
                    if (endEffectorSoundClip != null && endEffectorAudioSource.clip != endEffectorSoundClip)
                    {
                        endEffectorAudioSource.clip = endEffectorSoundClip;
                    }
                    endEffectorAudioSource.loop = true;
                    if (!endEffectorAudioSource.isPlaying)
                    {
                        endEffectorAudioSource.Play();
                    }
                }
            }
            else
            {
                if (endEffectorAudioSource != null && endEffectorAudioSource.isPlaying)
                {
                    endEffectorAudioSource.Stop();
                }
            }

            if (_areClimberRollersRunning)
            {
                if (climberRollerAudioSource != null)
                {
                    if (climberRollerSoundClip != null && climberRollerAudioSource.clip != climberRollerSoundClip)
                    {
                        climberRollerAudioSource.clip = climberRollerSoundClip;
                    }
                    climberRollerAudioSource.loop = true;
                    if (!climberRollerAudioSource.isPlaying)
                    {
                        climberRollerAudioSource.Play();
                    }
                }
            }
            else
            {
                if (climberRollerAudioSource != null && climberRollerAudioSource.isPlaying)
                {
                    climberRollerAudioSource.Stop();
                }
            }

            bool isCageInBox = IsCageInClimberBox();
            if (isCageInBox && !_wasCageInClimberBox)
            {
                if (climberLatchAudioSource != null)
                {
                    if (climberLatchSoundClip != null)
                    {
                        climberLatchAudioSource.PlayOneShot(climberLatchSoundClip);
                    }
                    else
                    {
                        climberLatchAudioSource.Play();
                    }
                }
            }
            _wasCageInClimberBox = isCageInBox;
        }

        private bool IsClimberTouchingCage()
        {
            if (climberTriggerBox == null) return false;

            Collider[] hits = Physics.OverlapBox(
                climberTriggerBox.bounds.center,
                climberTriggerBox.bounds.extents,
                climberTriggerBox.transform.rotation
            );

            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root) continue;

                if (hit.gameObject.CompareTag("Cage"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsCageInClimberBox()
        {
            if (climberBox == null) return false;

            Collider[] hits = Physics.OverlapBox(
                climberBox.bounds.center,
                climberBox.bounds.extents,
                climberBox.transform.rotation
            );

            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root) continue;

                if (hit.gameObject.CompareTag("Cage"))
                {
                    return true;
                }
            }
            return false;
        }

        private void rotateClimberRollers()
        {
            _areClimberRollersRunning = true;
            climberRoller.VelocityRoller(climberRollerSpeed).useAxis(JointAxis.Y);
            reversedClimberRoller.VelocityRoller(-climberRollerSpeed).useAxis(JointAxis.Y);
        }

        private void stopClimberRollers()
        {
            _areClimberRollersRunning = false;
            climberRoller.VelocityRoller(0).useAxis(JointAxis.Y);
            reversedClimberRoller.VelocityRoller(0).useAxis(JointAxis.Y);
        }

        private bool IsCoralOrientationValid()
        {
            Transform scanTransform = coralScanSource != null ? coralScanSource : (coralIntake != null ? coralIntake.transform : null);

            if (scanTransform == null) return true;

            Collider[] hits = Physics.OverlapSphere(scanTransform.position, 0.5f);

            Transform closestCoral = null;
            float closestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                if (hit.transform.root == transform.root) continue;

                Transform pieceTransform = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;

                if (pieceTransform.name.IndexOf("Coral", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    float distance = Vector3.Distance(pieceTransform.position, scanTransform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestCoral = pieceTransform;
                    }
                }
            }

            if (closestCoral == null) return true;

            Vector3 rollAxis = closestCoral.forward;
            float pitchAngle = Vector3.Angle(rollAxis, Vector3.up);

            bool isHorizontalWithinTolerance = (pitchAngle >= 80f && pitchAngle <= 100f);

            if (!isHorizontalWithinTolerance)
            {
                return false;
            }
            return true;
        }

        private void SyncBumperSubmeshes()
        {
            if (bumperRenderer == null) return;

            Material[] mats = bumperRenderer.sharedMaterials;

            if (mats.Length > 1 && mats[1] != mats[0])
            {
                for (int i = 1; i < mats.Length; i++)
                {
                    mats[i] = mats[0];
                }
                bumperRenderer.materials = mats;
            }
        }
    }
}