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

        [Header("EE Rollers Reverse")]
        [SerializeField] private GenericAnimationJoint[] eERollersReverse;

        [Header("Colliders")]
        [Tooltip("Igual que en VoltecStation: se apagan mientras se intaquea o se acaba de outtakear el alga, para que la pieza no rebote raro contra el frame del robot.")]
        [SerializeField] private BoxCollider[] algaeDisableColliders;

        [Header("Algae Stall Audio")]
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallAudio;

        [Header("Robot Audio")]
        [SerializeField] private AudioSource rollerSource;
        [SerializeField] private AudioClip intakeClip;
        [Tooltip("Segunda capa de sonido de intake (ej. IntakeDeeperSound), suena igual/junto con rollerSource.")]
        [SerializeField] private AudioSource rollerDeeperSource;
        [SerializeField] private AudioClip intakeDeeperClip;

        [Header("Coral Pickup Audio")]
        [Tooltip("Se reproduce UNA sola vez, justo cuando el elevador empieza a bajar a coralPickup.")]
        [SerializeField] private AudioSource coralPickupSource;
        [SerializeField] private AudioClip coralPickupClip;

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
        // Handoff de chasis: combinacion de 3 mods.
        // - _6328 (prioridad 1): sin coroutine, se checa cada FixedUpdate contra
        //   la posicion REAL del brazo/elevador (ver HandoffCoralFromChassis()).
        // - Alphabots (prioridad 2): una funcion dedicada tipo HandoffCoral()
        //   con UN flag de fase que se resetea en el ELSE atado a la MISMA
        //   condicion que lo prende. Esto es justo lo que le faltaba a la
        //   version anterior: _handoffCommitted/_pickupDescentStarted no se
        //   reseteaban de forma confiable, asi que el SEGUNDO coral a veces
        //   arrancaba ya "a medio camino" de la fase del primero en vez de
        //   empezar desde el angulo de chasis - eso es lo que mandaba el
        //   brazo de mas hacia abajo hasta trabarlo.
        // - RoboWhales (prioridad 3): la idea de una sola bandera "esto esta
        //   corriendo" que blinda el resto del archivo (aqui: el guard sobre
        //   currentStateNum en IntakeSequence) para que nada mas le pise el
        //   target al brazo/elevador mientras el handoff esta activo.
        private bool _handoffAtChassisStow;
        private bool _handoffCommitted;
        private bool _pickupDescentStarted;
        private bool _l2SequenceRunning;
        private bool _l2SequenceComplete;
        private bool _algaeCollidersLocked;

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

            rollerDeeperSource.playOnAwake = false;
            rollerDeeperSource.clip = intakeDeeperClip;
            rollerDeeperSource.loop = true;
            rollerDeeperSource.Stop();

            coralPickupSource.playOnAwake = false;
            coralPickupSource.loop = false;
            coralPickupSource.Stop();

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
                    // Modo Algae en reposo (comportamiento original): no tocamos CurrentSetpoint,
                    // asi Barge/Processor/Place/outtake funcionan normal.
                }
                else if (CurrentRobotMode != ReefscapeRobotMode.Coral)
                {
                    // Modo Algae interrumpido (comportamiento original): forzamos a Stow.
                    SetState(ReefscapeSetpoints.Stow);
                }
                // Modo Coral (fix nuevo): no forzamos nada - IntakeSequence() maneja todo sin
                // pelearse con el Update() de la base (esto es lo que arreglaba el parpadeo).
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

            // Igual que en VoltecStation: se apagan los colliders del alga mientras se
            // esta intakeando. _algaeCollidersLocked evita que esto pise el delay de
            // reactivacion tras un outtake (ver ReactivateAlgaeCollidersAfterDelay).
            bool wantsAlgaeIntake = (CurrentSetpoint == ReefscapeSetpoints.LowAlgae ||
                                      CurrentSetpoint == ReefscapeSetpoints.HighAlgae ||
                                      CurrentSetpoint == ReefscapeSetpoints.Stack ||
                                      (CurrentSetpoint == ReefscapeSetpoints.Intake &&
                                       CurrentRobotMode == ReefscapeRobotMode.Algae)) &&
                                     IntakeAction.IsPressed() && !coralAtEE && !_algaeController.HasPiece();

            if (!_algaeCollidersLocked)
            {
                ToggleAlgaeColliders(!wantsAlgaeIntake);
            }

            UpdateIntakeAudio();

            if (CurrentSetpoint != ReefscapeSetpoints.L2)
            {
                _l2SequenceComplete = false;
            }

            if (CurrentSetpoint == ReefscapeSetpoints.Climbed) DriveController.SetDriveMp(0f);
            else if (CurrentSetpoint == ReefscapeSetpoints.Climb) DriveController.SetDriveMp(0.5f);
            else if (CurrentSetpoint is ReefscapeSetpoints.Barge || LastSetpoint == ReefscapeSetpoints.Barge) DriveController.SetDriveMp(0.8f);
            else DriveController.SetDriveMp(1);

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
                        SpinEERollers(algaeIntakeRollerSpeed);
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
                        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 4f, 0));

                        // Al soltar el alga se apagan los colliders para que la pieza salga
                        // limpia (sin rebotar) y se reactivan 1 segundo despues.
                        _algaeCollidersLocked = true;
                        ToggleAlgaeColliders(false);
                        StartCoroutine(ReactivateAlgaeCollidersAfterDelay(1f));

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
                                float outSpeed = FacingReef ? -eEWheelSpeed : eEWheelSpeed;
                                SpinEERollers(outSpeed);
                                break;
                        }
                    }

                    break;

                case ReefscapeSetpoints.L1:
                    // El robot real no puede hacer L1 con coral - lo mandamos a Stow.
                    SetState(ReefscapeSetpoints.Stow);
                    break;

                case ReefscapeSetpoints.L2:
                    if (!_l2SequenceRunning && !_l2SequenceComplete)
                    {
                        StartCoroutine(GoToL2Sequence());
                    }
                    break;

                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? lowAlgae : lowAlgaeBack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                    _algaeController.SetTargetState(algaeStowState);
                    if (IntakeAction.IsPressed())
                    {
                        SpinEERollers(algaeIntakeRollerSpeed);
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
                        SpinEERollers(algaeIntakeRollerSpeed);
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
                        SpinEERollers(algaeIntakeRollerSpeed);
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

        // NOTA: el pickup de chasis ya NO usa una coroutine con tiempos fijos.
        // Ahora vive dentro de IntakeSequence(), como un gate continuo (estilo
        // _6328 / canHandoff) que se revisa cada FixedUpdate contra la
        // posicion REAL del brazo y el elevador. Ver _handoffCommitted y
        // _pickupDescentStarted.

        private IEnumerator GoToL2Sequence()
        {
            _l2SequenceRunning = true;

            // 1. Primero sube/baja el elevador y acomoda el intake.
            _elevatorTargetHeight = coralStow.elevatorHeight;
            _intakeTargetAngle = l2.intakeAngle;

            yield return new WaitForSeconds(0.2f);

            // 2. Ya paso el tiempo: gira el brazo (solo si seguimos en L2, por si el driver cambio de setpoint).
            if (CurrentSetpoint == ReefscapeSetpoints.L2)
            {
                _elevatorTargetHeight = l2.elevatorHeight;
                _armTargetAngle = l2.armAngle;
            }

            _l2SequenceRunning = false;
            _l2SequenceComplete = true;
        }

        private void ToggleAlgaeColliders(bool enable)
        {
            if (algaeDisableColliders == null) return;

            foreach (var col in algaeDisableColliders)
            {
                if (col != null)
                {
                    col.enabled = enable;
                }
            }
        }

        private IEnumerator ReactivateAlgaeCollidersAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            _algaeCollidersLocked = false;
            ToggleAlgaeColliders(true);
        }

        private bool FacingBarge()
        {
            return (transform.position.x > 0 && transform.rotation.eulerAngles.y > 180) ||
                   (transform.position.x <= 0 && transform.rotation.eulerAngles.y <= 180);
        }

        // Spins eEWheels at `speed` and eERollersReverse at the opposite speed,
        // so the reverse set always turns the other way (useful to center a piece
        // instead of pushing it to one side when both wheel sets grip it).
        // NOTE: this Voltec's EE rollers spin on the Z axis, not Y.
        private void SpinEERollers(float speed)
        {
            foreach (var wheel in eEWheels)
                wheel.VelocityRoller(speed).useAxis(JointAxis.Z);
            foreach (var wheel in eERollersReverse)
                wheel.VelocityRoller(-speed).useAxis(JointAxis.Z);
        }

        // Maneja el pickup del coral desde el chasis (coralChassisStow -> coralArmStow).
        // Estructura tipo Alphabots.HandoffCoral(): una funcion dedicada, sin
        // coroutine, llamada cada FixedUpdate. Regresa true el frame exacto en
        // que el coral ya se solto hacia el end effector (handoff confirmado).
        //
        // Dos fases, controladas por _handoffAtChassisStow:
        //  1) Asentar el brazo en coralStow.armAngle mientras el coral sigue
        //     agarrado al chasis.
        //  2) Ya asentado, bajar a coralPickup y esperar la confirmacion REAL
        //     (AtSetpoint) antes de avanzar el estado de la pieza.
        //
        // El "else" de hasta abajo resetea _handoffAtChassisStow/_pickupDescentStarted
        // SIEMPRE que no estemos en la condicion de arriba (igual que Alphabots).
        // Esto es lo que le faltaba a la version anterior: sin ese reset
        // garantizado, un residuo de fase del coral previo podia colarse al
        // arrancar el segundo coral y mandaba el brazo a un angulo que no le
        // tocaba en ese punto del ciclo.
        private bool HandoffCoralFromChassis(bool hasAlgae)
        {
            bool atChassisStow = _coralController.currentStateNum == coralChassisStowState.stateNum &&
                                  _coralController.atTarget;

            if (atChassisStow && !hasAlgae)
            {
                _intakeTargetAngle = coralStow.intakeAngle;

                if (!_handoffAtChassisStow)
                {
                    // Fase 1: asentar el brazo antes de bajar por el coral.
                    _armTargetAngle = coralStow.armAngle;

                    if (Utils.WithinAngularRange(arm.GetSingleAxisAngle(JointAxis.X), coralStow.armAngle, 5f))
                    {
                        _handoffAtChassisStow = true;
                    }
                }
                else
                {
                    // Fase 2: ya asentado, vamos por el coral a coralPickup.
                    if (!_pickupDescentStarted)
                    {
                        if (coralPickupSource != null && coralPickupClip != null)
                        {
                            coralPickupSource.PlayOneShot(coralPickupClip);
                        }
                        _pickupDescentStarted = true;
                    }

                    _armTargetAngle = coralPickup.armAngle;
                    _elevatorTargetHeight = coralPickup.elevatorHeight;
                    SpinEERollers(eEWheelSpeed);

                    // Tolerancia mas holgada que el default (2/2) porque con el
                    // peso extra del superciclo el PID no siempre asienta tan fino.
                    if (AtSetpoint(coralPickup, elevatorTolerance: 5f, armToleranceDeg: 5f))
                    {
                        _coralController.SetTargetState(coralArmStowState);
                        _handoffCommitted = true;
                        _handoffAtChassisStow = false;
                        _pickupDescentStarted = false;
                        return true;
                    }
                }

                _disruptable = true;
                return false;
            }

            // No estamos (o ya no estamos) en la ventana de pickup de chasis:
            // resetear siempre, para que el proximo coral empiece limpio desde
            // fase 1 y nunca herede la fase del coral anterior.
            _handoffAtChassisStow = false;
            _pickupDescentStarted = false;
            return false;
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

                        // Estilo StuyPulse: la asignacion base a groundCoral/coralStow va
                        // SIEMPRE, sin condicionarla al estado actual de la pieza (el
                        // ternario hasAlgae ya se encarga de no tocar el target mientras
                        // cargamos algae). El guard que tenia antes aqui
                        // (`if (currentStateNum < coralChassisStowState.stateNum)`) era el
                        // bug: si currentStateNum se queda en 4/5 (p.ej. justo despues de
                        // placear el primer coral, antes de que el controller resetee la
                        // pieza), el guard se saltaba esta asignacion por completo y el
                        // brazo se iba derecho a coralPickup ignorando groundCoral - y con
                        // algae a bordo tampoco entraba a HandoffCoralFromChassis (esa
                        // funcion hace early-return si hasAlgae), asi que no se asignaba
                        // NADA ese frame, rompiendo el manejo de algae+coral simultaneo.
                        // HandoffCoralFromChassis (abajo) sigue siendo quien sobreescribe
                        // esto en cuanto de verdad llegamos a coralChassisStow.
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

                        bool atArmStow = _coralController.currentStateNum == coralArmStowState.stateNum &&
                                          _coralController.atTarget;

                        // Gate de handoff combinado (ver HandoffCoralFromChassis arriba):
                        // frame-based como _6328, funcion dedicada con reset garantizado
                        // como Alphabots. Se llama DESPUES de la asignacion base de
                        // arriba a proposito (mismo orden que StuyPulse/RoboWhales):
                        // si de verdad estamos en coralChassisStow, esta funcion
                        // sobreescribe _armTargetAngle/_elevatorTargetHeight; si no, los
                        // deja tal cual los puso el bloque de arriba.
                        HandoffCoralFromChassis(hasAlgae);

                        if (_handoffCommitted && !atArmStow)
                        {
                            // Nos quedamos en coralPickup y seguimos girando el EE hasta
                            // CONFIRMAR (por estado real, no por un timer) que el coral
                            // ya paso a coralArmStowState. Esto es justo lo que arregla
                            // el handoff que a veces no se completaba.
                            _armTargetAngle = coralPickup.armAngle;
                            _elevatorTargetHeight = coralPickup.elevatorHeight;
                            SpinEERollers(eEWheelSpeed);
                        }

                        if (atArmStow)
                        {
                            _handoffCommitted = false;
                            _elevatorTargetHeight = coralStow.elevatorHeight;
                            _armTargetAngle = coralStow.armAngle;
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
                if (rollerSource.isPlaying || rollerDeeperSource.isPlaying || algaeStallSource.isPlaying)
                {
                    rollerSource.Stop();
                    rollerDeeperSource.Stop();
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

            if (!rollerDeeperSource.isPlaying && intakeAudioActive)
            {
                rollerDeeperSource.Play();
            }
            else if (rollerDeeperSource.isPlaying && !intakeAudioActive)
            {
                rollerDeeperSource.Stop();
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

            bool isCoralSetpoint = CurrentSetpoint == ReefscapeSetpoints.L2 ||
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

        private bool AtSetpoint(VoltecOffseasonSetpoint stp, float elevatorTolerance = 2f, float armToleranceDeg = 2f)
        {
            return
                Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, elevatorTolerance) &&
                Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, armToleranceDeg);
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