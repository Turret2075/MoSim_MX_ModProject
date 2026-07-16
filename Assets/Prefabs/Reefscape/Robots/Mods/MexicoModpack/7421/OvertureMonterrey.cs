using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using Games.Reefscape.Scoring.Scorers;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using Robots.Climbing;
using DriveController = RobotFramework.Controllers.Drivetrain.DriveController;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._7421._7421Monterrey
{
    public class OvertureMonterrey : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint arm;
        [SerializeField] private GenericJoint wrist;
        [SerializeField] private GenericJoint climber;
        [SerializeField] private GenericJoint claw;
        [SerializeField] private Transform coralSlider;
        [SerializeField] private Transform algaeSlider;
        [Header("PIDS")]
        [SerializeField] private PidConstants armPid;
        [SerializeField] private PidConstants wristPid;
        [SerializeField] private PidConstants clawPid;
        [SerializeField] private PidConstants climberPid;

        [Header("Setpoints")]
        [SerializeField] private OvertureMonterreySetpoint stow;
        [SerializeField] private OvertureMonterreySetpoint Algaestow;
        [SerializeField] private OvertureMonterreySetpoint intake;
        [SerializeField] private OvertureMonterreySetpoint intakeback;
        [SerializeField] private OvertureMonterreySetpoint stack;
        [SerializeField] private OvertureMonterreySetpoint l1;
        [SerializeField] private OvertureMonterreySetpoint l1back;
        [SerializeField] private OvertureMonterreySetpoint l2;
        [SerializeField] private OvertureMonterreySetpoint l2Place;
        [SerializeField] private OvertureMonterreySetpoint l2back;
        [SerializeField] private OvertureMonterreySetpoint l2backPlace;
        [SerializeField] private OvertureMonterreySetpoint l3;
        [SerializeField] private OvertureMonterreySetpoint l3Place;
        [SerializeField] private OvertureMonterreySetpoint l3Place2;
        [SerializeField] private OvertureMonterreySetpoint l3back;
        [SerializeField] private OvertureMonterreySetpoint l3backPlace;
        [SerializeField] private OvertureMonterreySetpoint l3backPlace2;
        [SerializeField] private OvertureMonterreySetpoint l4;
        [SerializeField] private OvertureMonterreySetpoint l4Place;
        [SerializeField] private OvertureMonterreySetpoint l4back;
        [SerializeField] private OvertureMonterreySetpoint l4backPlace;
        [SerializeField] private OvertureMonterreySetpoint l4ready;
        [SerializeField] private OvertureMonterreySetpoint l4readyback;
        [SerializeField] private OvertureMonterreySetpoint barge;
        [SerializeField] private OvertureMonterreySetpoint groundAlgae;
        [SerializeField] private OvertureMonterreySetpoint lowAlgae;
        [SerializeField] private OvertureMonterreySetpoint highAlgae;
        [SerializeField] private OvertureMonterreySetpoint lowbackAlgae;
        [SerializeField] private OvertureMonterreySetpoint highbackAlgae;
        [SerializeField] private OvertureMonterreySetpoint climb;
        [SerializeField] private OvertureMonterreySetpoint climbed;
        [SerializeField] private OvertureMonterreySetpoint processor;
        [SerializeField] private OvertureMonterreySetpoint special;
        [SerializeField] private OvertureMonterreySetpoint lollipop;

        [Header("Intake Componenets")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState algaeStowState;

        [Header("Algae Stall Audio")]
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallAudio;

        [Header("Robot Audio")]
        [SerializeField] private AudioSource rollerSource;
        [SerializeField] private AudioClip intakeClip;

        [Header("Animation Joints (Wheels)")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private GenericAnimationJoint[] algaeintakeWheels;
        [SerializeField] private float wheelIntakeSpeed = 1000f;

        [Header("Drivetrain")]
        [SerializeField] private DriveController driveController;

        [Header("Colliders")]
        [SerializeField] private BoxCollider[] algaeDisableColliders;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private ReefscapeAutoAlign align;

        private ClimbScorer _climbScorer;
        private float _elevatorTargetHeight;
        private float _armTargetAngle;
        private float _wristTargetAngle;
        private float _clawTargetAngle;
        private float _climbTargetAngle;
        private bool _isScoring;
        private bool _alreadyPlaced;
        private bool preAligned = false;
        private bool _robotSpecialPressed;
        private bool _stationMode;
        private bool lollipopmode;
        private bool lollipoppressed;
        private bool isDelayedTransition = false;

        protected override void Start()
        {
            base.Start();

            _climbScorer = gameObject.GetComponent<ClimbScorer>();
            climber.SetPid(climberPid);
            arm.SetPid(armPid);
            claw.SetPid(clawPid);
            wrist.SetPid(wristPid);

            _elevatorTargetHeight = 0;
            _climbTargetAngle = 0;
            _armTargetAngle = 0;
            _clawTargetAngle = 0;
            _wristTargetAngle = 0;
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                coralStowState
            };
            _coralController.intakes.Add(coralIntake);

            _algaeController.gamePieceStates = new[] { algaeStowState };
            _algaeController.intakes.Add(algaeIntake);

            algaeStallSource.clip = algaeStallAudio;
            algaeStallSource.loop = true;
            algaeStallSource.Stop();

            rollerSource.clip = intakeClip;
            rollerSource.loop = true;
            rollerSource.Stop();

            align = gameObject.GetComponent<ReefscapeAutoAlign>();
            preAligned = false;

            _alreadyPlaced = false;
            _robotSpecialPressed = false;
            _stationMode = false;

            lollipopmode = false;
            lollipoppressed = false;
            

        }

        private void LateUpdate()
        {
            climber.UpdatePid(climberPid);
            arm.UpdatePid(armPid);
            claw.UpdatePid(clawPid);
            wrist.UpdatePid(wristPid);
        }

        private IEnumerator DelayedSetpoint(OvertureMonterreySetpoint firstSetpoint, OvertureMonterreySetpoint secondSetpoint, float delay = 2f)
        {
            isDelayedTransition = true;

            SetSetpoint(firstSetpoint);
            yield return new WaitForSeconds(delay);
            SetSetpoint(secondSetpoint);
        }

        private void FixedUpdate()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            _algaeController.SetTargetState(algaeStowState);
            _coralController.SetTargetState(coralStowState);

            if (coralIntake.GamePiece != null)
            {
                var localSliderSpace = coralIntake.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).z;
                coralSlider.localPosition = new Vector3(0, 0, localSliderSpace);
            }

            if (algaeIntake.GamePiece != null)
            {
                var localSliderSpace2 = algaeIntake.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).z;
                algaeSlider.localPosition = new Vector3(0, 0, localSliderSpace2);
            }

            if (!_isScoring)
            {
                bool isIntaking = (CurrentSetpoint == ReefscapeSetpoints.Intake || CurrentSetpoint == ReefscapeSetpoints.RobotSpecial || CurrentSetpoint == ReefscapeSetpoints.Stack) && IntakeAction.IsPressed();

                if (isIntaking && CurrentRobotMode == ReefscapeRobotMode.Coral || CurrentSetpoint == ReefscapeSetpoints.LowAlgae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae)
                {
                    foreach (var wheel in intakeWheels)
                        wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.X);
                }
                else if (isIntaking && CurrentRobotMode == ReefscapeRobotMode.Algae)
                {
                    foreach (var wheel in algaeintakeWheels)
                        wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.X);
                }
                else
                {
                    foreach (var wheel in intakeWheels)
                        wheel.VelocityRoller(0).useAxis(JointAxis.X);
                    foreach (var wheel in algaeintakeWheels)
                        wheel.VelocityRoller(0).useAxis(JointAxis.X);
                }
            }

            if (_climbScorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climb)
                SetState(ReefscapeSetpoints.Climbed);

            AutoAlignOffsets();
            CheckStationMode();
            CheckLollipopMode();

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    isDelayedTransition = false;
                    if (lollipopmode)
                    {
                        SetSetpoint(lollipop);
                    }
                    else if (hasAlgae)
                    {
                        SetSetpoint(Algaestow);
                    }
                    else
                        SetSetpoint(stow);
                    armPid.Max = 4f;
                    break;
                case ReefscapeSetpoints.Intake:
                    _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, !hasCoral && !hasAlgae);
                    if (CurrentRobotMode == ReefscapeRobotMode.Algae)
                    {
                        SetSetpoint(groundAlgae);
                        _algaeController.RequestIntake(algaeIntake, true);
                    }
                    else if (lollipopmode)
                    {
                        _coralController.RequestIntake(coralIntake, true);
                        SetSetpoint(lollipop);
                    }
                    else if (_stationMode)
                    {
                        _coralController.RequestIntake(coralIntake, true);
                        SetSetpoint(special);
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L2 || LastSetpoint == ReefscapeSetpoints.L3 || LastSetpoint == ReefscapeSetpoints.L4 && !isDelayedTransition)
                    {
                        _coralController.RequestIntake(coralIntake, true);
                        StartCoroutine(DelayedSetpoint(stow, intake, 2f));
                        armPid.Max = 6f;
                    }
                    else
                    {
                        _coralController.RequestIntake(coralIntake, true);
                        SetSetpoint(FacingReef ? intake : intakeback);
                        armPid.Max = 6f;
                    }
                    break;
                case ReefscapeSetpoints.Place:
                    StartCoroutine(PlaceCoroutine());
                    StartCoroutine(PlacePiece());
                    if (hasAlgae && !hasCoral)
                    {
                        _algaeController.ReleaseGamePieceWithForce(new Vector3(-3f, 0, 0));
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L2 && CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint(FacingReef ? l2Place : l2backPlace);
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L3 && CurrentRobotMode == ReefscapeRobotMode.Coral && !isDelayedTransition)
                    {
                        if (FacingReef)
                        {
                            StartCoroutine(DelayedSetpoint(l3Place, l3Place2, 0.25f));
                        }
                        else
                        {
                            StartCoroutine(DelayedSetpoint(l3backPlace, l3backPlace2, 0.25f));
                        }
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L4 && CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint(FacingReef ? l4Place : l4backPlace);
                    }
                    break;
                case ReefscapeSetpoints.L1:
                    if (hasAlgae && !hasCoral)
                    {
                        SetSetpoint(processor);
                    }
                    else
                    {
                        SetSetpoint(FacingReef ? l1 : l1back);
                    }
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(stack);
                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L2:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint(FacingReef ? l2 : l2back);
                    }
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? lowAlgae : lowbackAlgae);
                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L3:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint(FacingReef ? l3 : l3back);
                    }
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? highAlgae : highbackAlgae);
                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L4:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral && !isDelayedTransition)
                    {
                        if (FacingReef)
                        {
                            StartCoroutine(DelayedSetpoint(l4, l4ready, 0.5f));
                        }
                        else
                        {
                            StartCoroutine(DelayedSetpoint(l4back, l4readyback, 0.5f));
                        }
                    }
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(barge);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processor);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climb);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    break;
            }

            if ((CurrentRobotMode == ReefscapeRobotMode.Algae && CurrentSetpoint == ReefscapeSetpoints.Intake) || (CurrentSetpoint == ReefscapeSetpoints.LowAlgae && IntakeAction.IsPressed()) || (CurrentSetpoint == ReefscapeSetpoints.HighAlgae && IntakeAction.IsPressed()) || (CurrentSetpoint == ReefscapeSetpoints.Stack && IntakeAction.IsPressed()))
            {
                ToggleAlgaeColliders(false);
                _algaeController.RequestIntake(algaeIntake, true);
            }
            else
            {
                ToggleAlgaeColliders(true);
                _algaeController.RequestIntake(algaeIntake, false);
            }

            if (!IntakeAction.IsPressed() && CurrentSetpoint != ReefscapeSetpoints.Stow)
            {
                armPid.Max = 4f;
            }

            if (!hasCoral && L1Action.triggered)
            {
                SetSetpoint(lollipop);
            }

            UpdateSetpoints();
            UpdateAudio();
        }
        private IEnumerator PlaceCoroutine()
        {
            if (_alreadyPlaced) yield break;

            _isScoring = true;
            StartCoroutine(PlacePiece());

            // Reversed logic: Scored speed is the opposite of whatever the current mode's intake direction is
            float currentIntakeDirection = (CurrentRobotMode == ReefscapeRobotMode.Algae) ? 1f : -1f;
            float scoreSpeed = -wheelIntakeSpeed * currentIntakeDirection;

            float timer = 0;
            while (timer < 0.5f)
            {
                foreach (var wheel in intakeWheels) wheel.VelocityRoller(scoreSpeed);
                timer += Time.deltaTime;
                yield return null;
            }

            foreach (var wheel in intakeWheels) wheel.VelocityRoller(0);
            _isScoring = false;
        }

        private void CheckStationMode()
        {
            if (RobotSpecialAction.IsPressed() && !_robotSpecialPressed && BaseGameManager.Instance.RobotState == RobotState.Enabled)
                _stationMode = !_stationMode;

            CurrentCoralStationMode.DropType = _stationMode ? DropType.Station : DropType.Ground;
            CurrentCoralStationMode.RotationVariance = _stationMode ? 0f : 0f;
            CurrentCoralStationMode.DropStrength = _stationMode ? 5f : 1.5f;
            CurrentCoralStationMode.DropDistance = _stationMode ? 1.5f : 5f;
            _robotSpecialPressed = RobotSpecialAction.IsPressed();
        }

        private void CheckLollipopMode()
        {
            if (L1Action.triggered && !lollipoppressed && BaseGameManager.Instance.RobotState == RobotState.Enabled && !_coralController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Coral && !_algaeController.HasPiece())
            {
                lollipopmode = !lollipopmode;
            }
            lollipoppressed = L1Action.triggered;

            if (_coralController.HasPiece())
            {
                lollipopmode = false;
            }
        }

        private IEnumerator PlacePiece()
        {
            if ((_coralController.HasPiece() && _algaeController.HasPiece()) || _coralController.HasPiece())
            {
                if (CurrentRobotMode == ReefscapeRobotMode.Algae)
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(3f, 3f, 0));
                else if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    if (LastSetpoint == ReefscapeSetpoints.L4)
                    {
                        if (!FacingReef)
                        {
                            yield return new WaitForSeconds(0.1f);
                            _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, -2f), 0.5f, 1.75f);
                        }
                        yield return new WaitForSeconds(0.1f);
                        _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 2f), 0.5f, 1.75f);
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L1 || LastSetpoint == ReefscapeSetpoints.Stow)
                    {
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 2f, 0));
                    }                    
                    else
                    {
                        if (!FacingReef)
                        {
                            yield return new WaitForSeconds(0.1f);
                            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, -3f));
                        }
                        yield return new WaitForSeconds(0.1f);
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 3));
                    }
                }
            }
            else if (_algaeController.HasPiece())
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(3f, 3f, 0));
            }
        }




        private void SetSetpoint(OvertureMonterreySetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _climbTargetAngle = setpoint.climberAngle;
            _armTargetAngle = setpoint.armAngle;
            _wristTargetAngle = setpoint.endEffectorAngle;
            _clawTargetAngle = setpoint.clawAngle;
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            climber.SetTargetAngle(_climbTargetAngle).withAxis(JointAxis.X);
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.Z).noWrap(180f);
            wrist.SetTargetAngle(_wristTargetAngle).withAxis(JointAxis.Y).noWrap(180f);
            claw.SetTargetAngle(_clawTargetAngle).withAxis(JointAxis.X).noWrap(180f);
        }

        private void AutoAlignOffsets()
        {
            if (!AutoAlignLeftAction.IsPressed() && !AutoAlignRightAction.IsPressed())
            {
                preAligned = false;
            }
            float xOffset1 = 12f;
            float xOffset2 = -12f;
            float zOffset1 = 4f;
            float zOffset2 = 4f;
            if (!preAligned && (AutoAlignLeftAction.IsPressed() || AutoAlignRightAction.IsPressed()))
            {
                xOffset1 = 12f;
                xOffset2 = -12f;
                zOffset1 = 4f;
                zOffset2 = 4f;
                if (align.getDistance() < 0.0254f * 6f && !AutoAlignLeftAction.triggered && !AutoAlignRightAction.triggered)
                {
                    preAligned = true;
                }
            }
            float xOffset = !FacingReef ? xOffset1 : xOffset2;
            float zOffset = !FacingReef ? zOffset1 : zOffset2;
            align.offset = new Vector3(xOffset, 0, zOffset);
        }

        private void ToggleAlgaeColliders(bool enable)
        {
            if (algaeDisableColliders == null) return;

            foreach (var collider in algaeDisableColliders)
            {
                if (collider != null)
                {
                    collider.enabled = enable;
                }
            }
        }

        private void UpdateAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (rollerSource.isPlaying || algaeStallSource.isPlaying)
                {
                    rollerSource.Stop();
                    algaeStallSource.Stop();
                }

                return;
            }

            if (((IntakeAction.IsPressed() && !_coralController.HasPiece() && !_coralController.HasPiece()) ||
                 OuttakeAction.IsPressed() || CurrentSetpoint == ReefscapeSetpoints.LowAlgae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae) &&
                !rollerSource.isPlaying)
            {
                rollerSource.Play();
            }
            else if (!IntakeAction.IsPressed() && !OuttakeAction.IsPressed() && rollerSource.isPlaying)
            {
                rollerSource.Stop();
            }
            else if (IntakeAction.IsPressed() && (_coralController.HasPiece() || _algaeController.HasPiece()))
            {
                rollerSource.Stop();
            }

            if (_algaeController.HasPiece() && !algaeStallSource.isPlaying)
            {
                algaeStallSource.Play();
            }
            else if (!_algaeController.HasPiece() && algaeStallSource.isPlaying)
            {
                algaeStallSource.Stop();
            }
        }
    }
}