using System;
using System.Collections;
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
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._6647
{
    public class VoltecOffseason : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint arm;
        [SerializeField] private GenericJoint groundIntake;
        [SerializeField] private GenericRoller[] intakeRollers;
        [SerializeField] private VoltecClimb climber;
        [SerializeField] private ClimbScorer scorer;

        [Header("PIDs")]
        [SerializeField] private PidConstants armPid;
        [SerializeField] private PidConstants intakePid;

        [Header("Coral Setpoints")]
        [SerializeField] private VoltecOffseasonSetpoint stow;
        [SerializeField] private VoltecOffseasonSetpoint coralStow;
        [SerializeField] private VoltecOffseasonSetpoint groundCoral;
        [Tooltip("Posicion del elevador cuando baja a recoger el coral desde el chasis, despues del handoff.")]
        [SerializeField] private VoltecOffseasonSetpoint coralPickup;
        [SerializeField] private VoltecOffseasonSetpoint l1;
        [SerializeField] private VoltecOffseasonSetpoint l2;
        [SerializeField] private VoltecOffseasonSetpoint l2Place;
        [SerializeField] private VoltecOffseasonSetpoint l3;
        [SerializeField] private VoltecOffseasonSetpoint l3Place;
        [SerializeField] private VoltecOffseasonSetpoint l4;
        [SerializeField] private VoltecOffseasonSetpoint l4Place;


        [Header("Algae Setpoints")]
        [SerializeField] private VoltecOffseasonSetpoint lowAlgae;
        [SerializeField] private VoltecOffseasonSetpoint lowAlgaeBack;
        [SerializeField] private VoltecOffseasonSetpoint highAlgae;
        [SerializeField] private VoltecOffseasonSetpoint highAlgaeBack;
        [SerializeField] private VoltecOffseasonSetpoint groundAlgae;
        [SerializeField] private VoltecOffseasonSetpoint algaeStow;
        [SerializeField] private VoltecOffseasonSetpoint processor;
        [SerializeField] private VoltecOffseasonSetpoint stack;
        [SerializeField] private VoltecOffseasonSetpoint bargeFront;
        [SerializeField] private VoltecOffseasonSetpoint bargeBack;

        [Header("Climb Setpoints")]
        [SerializeField] private VoltecOffseasonSetpoint climbPrep;
        [SerializeField] private VoltecOffseasonSetpoint climbed;

        [Header("Intake Components")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private string currentState;
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralSecondSetpointState;
        [SerializeField] private GamePieceState coralThirdSetpointState;
        [SerializeField] private GamePieceState coralFourthSetpointState;
        [SerializeField] private GamePieceState coralChassisStowState;
        [SerializeField] private GamePieceState coralArmStowState;
        [SerializeField] private GamePieceState algaeStowState;
        [SerializeField] private GamePieceState algaeIntakeState;


        [Header("Intake Wheels")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private float intakeWheelSpeed = 300f;
        [SerializeField] private GenericAnimationJoint[] eEWheels;
        [SerializeField] private float eEWheelSpeed = 300f;
        [Tooltip("Un poco mas fuerte que eEWheelSpeed para que el algae se sienta con mas garra al intakear.")]
        [SerializeField] private float algaeIntakeRollerSpeed = 400f;

        [Header("Algae Stall Audio")]
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallAudio;

        [Header("Robot Audio")]
        [SerializeField] private AudioSource rollerSource;
        [SerializeField] private AudioClip intakeClip;

        [Header("Auto Align Offsets")]
        [SerializeField] private float atSetpointOffset;
        [SerializeField] private float preAlignOffset;
        private ReefscapeAutoAlign _align;

        [Header("Target Positions")]
        private float _elevatorTargetHeight;
        private float _armTargetAngle;
        private float _intakeTargetAngle;

        private bool _intakeSequenceRunning;
        private bool _disruptable;
        private bool wasCoral;
        private bool _isPlacingCoral;
        private bool _pickupCoroutineRunning;

        private ReefscapeSetpoints? _bufferedSetpoint;
        private bool bufferAlgeaState;

        public RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        public RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        protected override void Start()
        {
            base.Start();

            arm.SetPid(armPid);
            groundIntake.SetPid(intakePid);

            _elevatorTargetHeight = 0;
            _armTargetAngle = stow.armAngle;
            _intakeTargetAngle = stow.intakeAngle;

            RobotGamePieceController.SetPreload(coralArmStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
            _coralController.gamePieceStates = new[]
            {
                coralIntakeState, coralSecondSetpointState, coralThirdSetpointState, coralFourthSetpointState,
                coralChassisStowState, coralArmStowState
            };
            _coralController.intakes.Add(coralIntake);

            _algaeController.gamePieceStates = new[] { algaeStowState };
            _algaeController.intakes.Add(algaeIntake);

            _disruptable = true;
            _intakeSequenceRunning = false;
            wasCoral = false;
            _isPlacingCoral = false;
            _bufferedSetpoint = null;
            bufferAlgeaState = false;

            rollerSource.playOnAwake = false;
            rollerSource.clip = intakeClip;
            rollerSource.loop = true;
            rollerSource.Stop();

            algaeStallSource.playOnAwake = false;
            algaeStallSource.clip = algaeStallAudio;
            algaeStallSource.loop = true;
            algaeStallSource.Stop();

            _align = gameObject.GetComponent<ReefscapeAutoAlign>();
        }

        private void LateUpdate()
        {
            arm.UpdatePid(armPid);
            groundIntake.UpdatePid(intakePid);
        }

        private void FixedUpdate()
        {
            var readState = _coralController.GetCurrentState();
            if (readState != null)
            {
                currentState = readState.name;
            }

            if (_coralController.HasPiece())
            {
                foreach (var roller in intakeRollers)
                {
                    roller.flipVelocity();
                }
            }

            _algaeController.SetTargetState(algaeStowState);

            if (_algaeController.HasPiece() || CurrentSetpoint == ReefscapeSetpoints.Barge)
            {
                if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    wasCoral = true;
                }

                SetRobotMode(ReefscapeRobotMode.Algae);
            }
            else if (_coralController.HasPiece() && CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
            }

            if (_disruptable && bufferAlgeaState)
            {
                SetRobotMode(ReefscapeRobotMode.Algae);
                bufferAlgeaState = false;
            }

            if (_intakeSequenceRunning || _coralController.IntakeHasPieces(coralIntake))
            {
                if (CurrentSetpoint != ReefscapeSetpoints.Stow && CurrentSetpoint != ReefscapeSetpoints.Intake)
                {
                    _bufferedSetpoint = CurrentSetpoint;
                }
            }

            if (((_coralController.currentStateNum != coralArmStowState.stateNum && !_disruptable) &&
                 !_coralController.atTarget) || _intakeSequenceRunning)
            {
                if (!_disruptable && CurrentRobotMode != ReefscapeRobotMode.Coral && _intakeSequenceRunning &&
                    !_algaeController.HasPiece())
                {
                    bufferAlgeaState = true;
                    SetRobotMode(ReefscapeRobotMode.Coral);
                }
                else if (_disruptable && CurrentRobotMode != ReefscapeRobotMode.Coral)
                {
                }
                else
                {
                    SetState(ReefscapeSetpoints.Stow);
                }
            }

            if ((!_intakeSequenceRunning && CurrentSetpoint != ReefscapeSetpoints.Intake) && _bufferedSetpoint != null)
            {
                SetState(_bufferedSetpoint.Value);
                _bufferedSetpoint = null;
            }

            bool coralAtEE = _coralController.currentStateNum == coralArmStowState.stateNum && _coralController.atTarget;

            if (coralAtEE && CurrentRobotMode != ReefscapeRobotMode.Coral)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
            }

            UpdateIntakeAudio();

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (!_intakeSequenceRunning || _coralController.HasPiece())
                    {
                        SetSetpoint(_algaeController.HasPiece() ? algaeStow : coralStow);
                    }

                    climber.NotClimbing();
                    break;

                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Algae && !_algaeController.HasPiece())
                    {
                        SetSetpoint(groundAlgae);
                        _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                        _algaeController.SetTargetState(algaeStowState);
                        foreach (var wheel in eEWheels)
                            wheel.VelocityRoller(algaeIntakeRollerSpeed).useAxis(JointAxis.Y);
                    }

                    break;

                case ReefscapeSetpoints.Place:
                    if (LastSetpoint == ReefscapeSetpoints.Stow && coralAtEE)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        break;
                    }

                    if (_algaeController.HasPiece())
                    {
                        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 5f, 0));
                        if (wasCoral)
                        {
                            SetRobotMode(ReefscapeRobotMode.Coral);
                            wasCoral = false;
                        }
                    }
                    else if (CurrentRobotMode != ReefscapeRobotMode.Algae &&
                             _coralController.currentStateNum == coralArmStowState.stateNum && !_isPlacingCoral)
                    {
                        StartCoroutine(PlaceCoral());
                    }
                    else if (_isPlacingCoral)
                    {
                        switch (LastSetpoint)
                        {
                            case ReefscapeSetpoints.L4:
                                SetSetpoint(l4Place);
                                break;
                            case ReefscapeSetpoints.L3:
                                SetSetpoint(l3Place);
                                break;
                            case ReefscapeSetpoints.L2:
                                SetSetpoint(l2Place);
                                break;
                        }
                    }

                    if (OuttakeAction.IsPressed())
                    {
                        switch (LastSetpoint)
                        {
                            case ReefscapeSetpoints.Barge:
                            case ReefscapeSetpoints.Processor:
                            case ReefscapeSetpoints.L4:
                            case ReefscapeSetpoints.L3:
                            case ReefscapeSetpoints.L2:
                            case ReefscapeSetpoints.L1:
                                float outSpeed = FacingReef ? -eEWheelSpeed : eEWheelSpeed;
                                foreach (var wheel in eEWheels)
                                    wheel.VelocityRoller(outSpeed).useAxis(JointAxis.Y);
                                break;
                        }
                    }

                    break;

                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    break;

                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    break;

                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? lowAlgae : lowAlgaeBack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                    _algaeController.SetTargetState(algaeStowState);
                    if (IntakeAction.IsPressed())
                    {
                        foreach (var wheel in eEWheels)
                            wheel.VelocityRoller(algaeIntakeRollerSpeed).useAxis(JointAxis.Y);
                    }

                    break;

                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    break;

                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? highAlgae : highAlgaeBack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                    _algaeController.SetTargetState(algaeStowState);
                    if (IntakeAction.IsPressed())
                    {
                        foreach (var wheel in eEWheels)
                            wheel.VelocityRoller(algaeIntakeRollerSpeed).useAxis(JointAxis.Y);
                    }

                    break;

                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4);
                    break;

                case ReefscapeSetpoints.Stack:
                    SetSetpoint(stack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                    _algaeController.SetTargetState(algaeStowState);
                    if (IntakeAction.IsPressed())
                    {
                        foreach (var wheel in eEWheels)
                            wheel.VelocityRoller(algaeIntakeRollerSpeed).useAxis(JointAxis.Y);
                    }

                    break;

                case ReefscapeSetpoints.Barge:
                {
                    VoltecOffseasonSetpoint targetBarge = FacingBarge() ? bargeFront : bargeBack;
                    SetSetpoint(targetBarge);
                    break;
                }


                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climbPrep);
                    climber.Climb();
                    break;

                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    climber.NotClimbing();
                    climber.RetractArm();
                    break;

                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processor);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            IntakeSequence();

            SetSetpoints();

            if (scorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climb && climber.WingsOpen())
            {
                climber.PlayClick();
                SetState(ReefscapeSetpoints.Climbed);
            }
            else if (!scorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climbed)
            {
                SetState(ReefscapeSetpoints.Climb);
            }

            UpdateAutoAlign();
        }

        private IEnumerator PlaceCoral()
        {
            _isPlacingCoral = true;

            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4Place);
                    yield return new WaitForSeconds(0.1f);
                    yield return new WaitUntil(() => _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0)));

                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3Place);
                    yield return new WaitUntil(() =>
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0.5f, FacingReef ? 2.5f : -2.5f)));

                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2Place);
                    yield return new WaitUntil(() =>
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0.5f, FacingReef ? 2.5f : -2.5f)));

                    break;
            }
        }

        private IEnumerator PickupCoralFromChassis()
        {
            _pickupCoroutineRunning = true;

            // 1. El coral ya esta agarrado a chasis (coralChassisStowState) con el brazo en posicion.
            yield return new WaitForSeconds(0.1f);

            // 2. Bajamos el elevador (y ajustamos el brazo si coralPickup trae un angulo distinto) para ir a recogerlo.
            _elevatorTargetHeight = coralPickup.elevatorHeight;
            _armTargetAngle = coralPickup.armAngle;
            yield return new WaitForSeconds(0.15f);

            // 3. Ya lo tiene el end effector: avanzamos el estado de la pieza.
            _coralController.SetTargetState(coralArmStowState);
            yield return new WaitForSeconds(0.15f);

            // 4. Subimos de vuelta a la posicion de stow.
            _elevatorTargetHeight = coralStow.elevatorHeight;
            _armTargetAngle = coralStow.armAngle;

            _pickupCoroutineRunning = false;
        }

        private bool FacingBarge()
        {
            return (transform.position.x > 0 && transform.rotation.eulerAngles.y > 180) ||
                   (transform.position.x <= 0 && transform.rotation.eulerAngles.y <= 180);
        }

        private void IntakeSequence()
        {
            if (!IntakeAction.IsPressed())
            {
                _intakeSequenceRunning = false;
                if (!_coralController.HasPiece())
                {
                    _disruptable = true;
                    if (_isPlacingCoral)
                    {
                        _isPlacingCoral = false;
                    }
                }
            }

            if (CurrentRobotMode == ReefscapeRobotMode.Coral ||
                (_algaeController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Algae))
            {
                if (CurrentSetpoint != ReefscapeSetpoints.HighAlgae && CurrentSetpoint != ReefscapeSetpoints.LowAlgae &&
                    CurrentSetpoint != ReefscapeSetpoints.Barge && CurrentSetpoint != ReefscapeSetpoints.Place)
                {
                    bool hasAlgae = _algaeController.HasPiece();
                    _coralController.RequestIntake(coralIntake, IntakeAction.IsPressed());

                    if (IntakeAction.IsPressed() ||
                        (_coralController.HasPiece() && _coralController.currentStateNum != coralArmStowState.stateNum))
                    {
                        _disruptable = false;
                        _intakeSequenceRunning = true;

                        _armTargetAngle = hasAlgae ? _armTargetAngle : groundCoral.armAngle;
                        _elevatorTargetHeight = hasAlgae ? _elevatorTargetHeight : groundCoral.elevatorHeight;
                        _intakeTargetAngle = groundCoral.intakeAngle;

                        _coralController.SetTargetState(_coralController.currentStateNum switch
                        {
                            0 => nameof(coralIntakeState),
                            1 => nameof(coralSecondSetpointState),
                            2 => nameof(coralThirdSetpointState),
                            3 => nameof(coralFourthSetpointState),
                            4 => nameof(coralChassisStowState),
                            _ => _coralController.movingTo
                        });

                        if (BaseGameManager.Instance.RobotState == RobotState.Enabled &&
                            Mathf.Approximately(_intakeTargetAngle, groundCoral.intakeAngle) && _coralController.currentStateNum < 5)
                        {
                            foreach (var wheel in intakeWheels)
                            {
                                wheel.VelocityRoller(intakeWheelSpeed).useAxis(JointAxis.X);
                            }
                        }

                        bool atChassisStow = _coralController.currentStateNum == coralChassisStowState.stateNum &&
                                              _coralController.atTarget;
                        if (atChassisStow)
                        {
                            _armTargetAngle = hasAlgae ? _armTargetAngle : coralStow.armAngle;
                            _intakeTargetAngle = coralStow.intakeAngle;

                            // Bug anterior: comparaba contra -coralStow.armAngle y nunca daba true.
                            bool armAtChassisAngle =
                                Utils.WithinAngularRange(arm.GetSingleAxisAngle(JointAxis.X), coralStow.armAngle, 5f);

                            if (armAtChassisAngle && !hasAlgae && !_pickupCoroutineRunning)
                            {
                                // El coral ya llego al chasis con el brazo en posicion: ahora bajamos
                                // el elevador a recogerlo (igual que RoboWhales).
                                StartCoroutine(PickupCoralFromChassis());
                            }
                            else if (!armAtChassisAngle)
                            {
                                _elevatorTargetHeight = hasAlgae ? _elevatorTargetHeight : coralStow.elevatorHeight;
                            }

                            _disruptable = true;
                        }

                        if (_pickupCoroutineRunning)
                        {
                            foreach (var wheel in eEWheels)
                                wheel.VelocityRoller(eEWheelSpeed).useAxis(JointAxis.Y);
                        }

                        bool atArmStow = _coralController.atTarget && _coralController.currentStateNum == coralArmStowState.stateNum;
                        if (atArmStow)
                        {
                            SetState(ReefscapeSetpoints.Stow);
                            _intakeSequenceRunning = false;
                        }
                    }
                    else if ((_coralController.atTarget && _coralController.currentStateNum == coralArmStowState.stateNum) &&
                             _intakeSequenceRunning)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        _intakeSequenceRunning = false;
                    }
                }
            }
        }

        private void UpdateIntakeAudio()
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

            var intakeAudioActive = (IntakeAction.IsPressed() && _coralController.currentStateNum != coralArmStowState.stateNum)
                                     || OuttakeAction.IsPressed()
                                     || (_coralController.HasPiece()
                                         && _coralController.currentStateNum != coralChassisStowState.stateNum
                                         && _coralController.currentStateNum != coralArmStowState.stateNum);
            if (!rollerSource.isPlaying && intakeAudioActive)
            {
                rollerSource.Play();
            }
            else if (rollerSource.isPlaying && !intakeAudioActive)
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

        private void UpdateAutoAlign()
        {
            if (_align == null) return; // Falta el componente ReefscapeAutoAlign en el GameObject de Voltec (revisar en el Inspector)

            bool isCoralSetpoint = CurrentSetpoint == ReefscapeSetpoints.L1 ||
                                    CurrentSetpoint == ReefscapeSetpoints.L2 ||
                                    CurrentSetpoint == ReefscapeSetpoints.L3 ||
                                    CurrentSetpoint == ReefscapeSetpoints.L4;

            if ((AtSetpoint() && isCoralSetpoint) || CurrentSetpoint == ReefscapeSetpoints.LowAlgae ||
                CurrentSetpoint == ReefscapeSetpoints.HighAlgae || CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                _align.offset = new Vector3(-0.1f, 0, atSetpointOffset);
            }
            else
            {
                _align.offset = new Vector3(-0.1f, 0, preAlignOffset);
            }
        }

        private bool AtSetpoint(VoltecOffseasonSetpoint stp)
        {
            return
                Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
                Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f);
        }

        private bool AtSetpoint()
        {
            return
                Utils.InRange(elevator.GetElevatorHeight(), _elevatorTargetHeight, 7f) &&
                Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 20f);
        }

        private void SetSetpoint(VoltecOffseasonSetpoint setpoint)
        {
            _armTargetAngle = setpoint.armAngle;
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _intakeTargetAngle = setpoint.intakeAngle;
        }

        private void SetSetpoints()
        {
            // No noWrap: el brazo debe poder girar hasta atras (rango completo).
            // noWrap(135): tu rango util es -180..0..+90 (arco de 270 grados pasando por el 0).
            // El arco sin usar es +90..180/-180 (90 grados) - 135 esta justo a la mitad de esa zona muerta.
            // Esto evita que el PID intente el "camino corto" que cruza el limite fisico y causa el teletransporte.
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(135f);

            elevator.SetTarget(_elevatorTargetHeight);

            groundIntake.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X);
        }
    }
}