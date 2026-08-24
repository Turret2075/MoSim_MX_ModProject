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
using UnityEngine.Serialization;

namespace Prefabs.Reefscape.Robots.Mods.Lambot._3478
{
    public class Lambot: ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint endEffector;
        [SerializeField] private GenericJoint climberBar;
        [SerializeField] private GenericJoint climberFlap;

        [Header("End Effector Rollers - Coral (parte inferior)")]
        [SerializeField] private GenericAnimationJoint[] coralEndEffectorAnimationRollers;
        [SerializeField] private float coralRollerSpeed = 10f;

        [Header("End Effector Rollers - Algae (parte superior)")]
        [SerializeField] private GenericAnimationJoint[] algaeEndEffectorAnimationRollers;
        [SerializeField] private float algaeRollerSpeed = 10f;

        [Header("Auto Align")]
        [SerializeField] private LambotAutoAlign autoAlign;
        [SerializeField] private Vector3 initialAutoAlignOffset;
        [SerializeField] private Vector3 algaeAutoAlignOffset;
        [SerializeField] private Vector3 algaeAutoAlignOffsetAlt;
        [SerializeField] private Vector3 l4AutoAlignOffset;
        [SerializeField] private Vector3 l3AutoAlignOffset;
        [SerializeField] private Vector3 l2AutoAlignOffset;
        [SerializeField] private Vector3 bargeAutoAlignOffset;
        
        [Header("PIDS")]
        [SerializeField] private PidConstants endEffectorPid;
        [SerializeField] private PidConstants climberBarPid;
        [SerializeField] private PidConstants climberFlapPid;

        [Header("coral Setpoints")]
        [SerializeField] private KeikoSetpoint stow;
        [SerializeField] private KeikoSetpoint coralStow;
        [SerializeField] private KeikoSetpoint intake;
        [SerializeField] private KeikoSetpoint l1;
        [SerializeField] private KeikoSetpoint l2;
        [SerializeField] private KeikoSetpoint l3;
        [SerializeField] private KeikoSetpoint l4;
        
        [Header("algae Setpoints")]
        [SerializeField] private KeikoSetpoint algaeStow;
        [SerializeField] private KeikoSetpoint lowAlgae;
        [SerializeField] private KeikoSetpoint highAlgae;
        [SerializeField] private KeikoSetpoint barge;
        
        [Header("climb Setpoints")]
        [SerializeField] private KeikoSetpoint climb;
        [SerializeField] private KeikoSetpoint climbed;
        
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
        
        [Header("Funnel Close Audio")]
        [SerializeField] private AudioSource funnelCloseSource;
        [SerializeField] private AudioClip funnelCloseAudio;
        [SerializeField] private BoxCollider coralTrigger;

        [Header("Colliders")]
        [SerializeField] private BoxCollider[] algaeDisableColliders;
        private OverlapBoxBounds soundDetector;
        
        
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _elevatorTargetHeight;
        private float _endEffectorTargetAngle;
        private float _climbBarTargetAngle;
        private float _funnelPivotTargetAngle;
        private LayerMask coralMask;
        private bool canClack;
        private bool _climbLocked;
        private bool _outtakeWasPressed;
        
        protected override void Start()
        {
            base.Start();
            
            endEffector.SetPid(endEffectorPid);
            climberBar.SetPid(climberBarPid);
            climberFlap.SetPid(climberFlapPid);

            _elevatorTargetHeight = 0;
            _endEffectorTargetAngle = 0;
            _climbBarTargetAngle = 0;
            _funnelPivotTargetAngle = 0;
            _climbLocked = false;
            
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                coralStowState
            };
            _coralController.intakes.Add(coralIntake);

            _algaeController.gamePieceStates = new[] {algaeStowState};
            _algaeController.intakes.Add(algaeIntake);
            
            algaeStallSource.clip = algaeStallAudio;
            algaeStallSource.loop = true;
            algaeStallSource.Stop();
            
            rollerSource.clip = intakeClip;
            rollerSource.loop = true;
            rollerSource.Stop();
            
            funnelCloseSource.clip = funnelCloseAudio;
            funnelCloseSource.loop = false;
            funnelCloseSource.Stop();

            soundDetector = new OverlapBoxBounds(coralTrigger);

            coralMask = LayerMask.GetMask("Coral");
            canClack = true;
        }

        private void LateUpdate()
        {
            endEffector.UpdatePid(endEffectorPid);
            climberBar.UpdatePid(climberBarPid);
            climberFlap.UpdatePid(climberFlapPid);
        }

        private void FixedUpdate()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            // Deteccion manual de flanco de subida para el outtake: no usamos
            // OuttakeAction.triggered directamente porque FixedUpdate puede correr
            // mas de una vez en el mismo frame, y .triggered puede seguir en true en
            // el segundo tick, causando que se suelte alga y coral "al mismo tiempo"
            // en vez de con dos pulsaciones (igual que en TitaniumRams).
            bool outtakeHeld = OuttakeAction != null && OuttakeAction.IsPressed();
            bool outtakeJustPressed = outtakeHeld && !_outtakeWasPressed;

            _algaeController.SetTargetState(algaeStowState);
            _coralController.SetTargetState(coralStowState);
            
            // Reset de align: igual que LambotOffseason, al re-triggerear los botones de
            // align con el end effector ya en el ángulo de coralStow, se limpia el offset.
            if ((AutoAlignLeftAction.triggered || AutoAlignRightAction.triggered) &&
                Utils.InAngularRange(endEffector.GetSingleAxisAngle(JointAxis.X), coralStow.endEffectorAngle, 1))
            {
                autoAlign.offset = initialAutoAlignOffset;
            }
            
            // Supercycle: coral y alga se piden de forma independiente, cada una solo se
            // bloquea por tener ya SU propia pieza, no por tener la otra. Así el robot
            // puede cargar alga y coral al mismo tiempo (ver Robonauts.RobonautsIntakeSequence /
            // el switch de Intake, mismo criterio).
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (hasAlgae)
                    {
                        SetSetpoint(algaeStow);
                    }
                    else if (hasCoral)
                    {
                        SetSetpoint(coralStow);
                    }
                    else
                    {
                        SetSetpoint(stow);
                    }
                    break;
                case ReefscapeSetpoints.Intake:
                    SetSetpoint(intake);

                    _algaeController.RequestIntake(algaeIntake, !_climbLocked && CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae);
                    _coralController.RequestIntake(coralIntake, !_climbLocked && !hasCoral);

                    // Igual que Robonauts: en cuanto hay coral a bordo, el modo se fuerza a
                    // Coral. Así, si el supercycle termina con coral Y alga cargados, el
                    // primer Place siempre suelta coral primero (a menos que el driver
                    // cambie el modo manualmente después).
                    if (hasCoral)
                    {
                        SetRobotMode(ReefscapeRobotMode.Coral);
                    }
                    break;
                case ReefscapeSetpoints.Place:
                    if (outtakeJustPressed)
                    {
                        bool hadBoth = hasCoral && hasAlgae;

                        PlacePiece();

                        if (hadBoth)
                        {
                            // Superciclo: si tenías coral Y alga y acabás de soltar la
                            // pieza correspondiente al modo actual, cambia automáticamente
                            // al otro modo para poder soltar la otra con la próxima
                            // pulsada de outtake (igual que en TitaniumRams).
                            switch (CurrentRobotMode)
                            {
                                case ReefscapeRobotMode.Algae:
                                    SetRobotMode(ReefscapeRobotMode.Coral);
                                    break;
                                case ReefscapeRobotMode.Coral:
                                    SetRobotMode(ReefscapeRobotMode.Algae);
                                    break;
                            }
                        }
                    }
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.L2:
                    autoAlign.offset =
                        Utils.InAngularRange(endEffector.GetSingleAxisAngle(JointAxis.X), l2.endEffectorAngle, 20)
                            ? l2AutoAlignOffset
                            : initialAutoAlignOffset;
                    SetSetpoint(l2);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                {
                    bool flip = ComputeAlignFlip();
                    if (AutoAlignLeftAction.IsPressed())
                        autoAlign.offset = !flip ? algaeAutoAlignOffset : algaeAutoAlignOffsetAlt;
                    else if (AutoAlignRightAction.IsPressed())
                        autoAlign.offset = !flip ? algaeAutoAlignOffsetAlt : algaeAutoAlignOffset;

                    SetSetpoint(lowAlgae);
                    _algaeController.RequestIntake(algaeIntake, !_climbLocked && IntakeAction.IsPressed() && !hasAlgae);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                }
                case ReefscapeSetpoints.L3:
                    autoAlign.offset = FacingReef
                        ? Utils.InAngularRange(endEffector.GetSingleAxisAngle(JointAxis.X), l3.endEffectorAngle, 20)
                            ? l3AutoAlignOffset
                            : initialAutoAlignOffset
                        : l3AutoAlignOffset;
                    SetSetpoint(l3);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                {
                    bool flip = ComputeAlignFlip();
                    if (AutoAlignLeftAction.IsPressed())
                        autoAlign.offset = !flip ? algaeAutoAlignOffset : algaeAutoAlignOffsetAlt;
                    else if (AutoAlignRightAction.IsPressed())
                        autoAlign.offset = !flip ? algaeAutoAlignOffsetAlt : algaeAutoAlignOffset;

                    SetSetpoint(highAlgae);
                    _algaeController.RequestIntake(algaeIntake, !_climbLocked && IntakeAction.IsPressed() && !hasAlgae);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                }
                case ReefscapeSetpoints.L4:
                    autoAlign.offset = FacingReef
                        ? Utils.InAngularRange(endEffector.GetSingleAxisAngle(JointAxis.X), l4.endEffectorAngle, 20)
                            ? l4AutoAlignOffset
                            : initialAutoAlignOffset
                        : l4AutoAlignOffset;
                    SetSetpoint(l4);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Barge:
                    autoAlign.bargeOffset = bargeAutoAlignOffset;
                    SetSetpoint(barge);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    if (_climbBarTargetAngle == 120)
                    {
                        _climbBarTargetAngle = 90;
                    }
                    else if (_climbBarTargetAngle != 90) SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    _climbLocked = true;
                    SetSetpoint(climb);
                    _climbBarTargetAngle = 120;
                    _funnelPivotTargetAngle = -75;
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    _climbBarTargetAngle = -45;
                    break;
            }
            
            if (ClimbAction.IsPressed() && (LastSetpoint == ReefscapeSetpoints.RobotSpecial || _climbBarTargetAngle == 90))
            {
                SetState(ReefscapeSetpoints.Climbed);
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
            
            UpdateSetpoints();
            UpdateRollers();
            UpdateAudio();

            _outtakeWasPressed = outtakeHeld;
        }

        private void UpdateRollers()
        {
            bool hasCoral = _coralController.HasPiece();
            bool hasAlgae = _algaeController.HasPiece();
            bool placing = CurrentSetpoint == ReefscapeSetpoints.Place;

            // GenericAnimationJoint (coralEndEffectorAnimationRollers / algaeEndEffectorAnimationRollers)
            // tiene velocidad variable vía VelocityRoller(...).useAxis(...), igual que
            // eEWheels/intakeWheels en StuyPulse: estos controlan intake/outtake con signo y magnitud.
            float coralSpeed = 0f;
            float algaeSpeed = 0f;

            if (placing)
            {
                if (OuttakeAction.IsPressed())
                {
                    // Igual que en TitaniumRams: al outtakear giran los dos sets de rollers
                    // (coral y algae) juntos, sin importar cuál de las dos piezas se está
                    // soltando realmente.
                    coralSpeed = -coralRollerSpeed;
                    algaeSpeed = -algaeRollerSpeed;
                }
            }
            else if (IntakeAction.IsPressed())
            {
                if (!hasCoral) coralSpeed = coralRollerSpeed;
                if (CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae) algaeSpeed = algaeRollerSpeed;
            }

            foreach (var animRoller in coralEndEffectorAnimationRollers)
                animRoller.VelocityRoller(coralSpeed).useAxis(JointAxis.Y);

            foreach (var animRoller in algaeEndEffectorAnimationRollers)
                animRoller.VelocityRoller(algaeSpeed).useAxis(JointAxis.Y);
        }

        private void PlacePiece()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            // Con ambas piezas cargadas se suelta la que le toca al modo actual (el
            // cambio al otro modo para la siguiente pulsada lo maneja el caso Place
            // en FixedUpdate). Con una sola, se suelta esa.
            if (hasAlgae && hasCoral)
            {
                if (CurrentRobotMode == ReefscapeRobotMode.Algae)
                {
                    PlaceAlgae();
                }
                else
                {
                    PlaceCoral();
                }
            }
            else if (hasAlgae)
            {
                PlaceAlgae();
            }
            else
            {
                PlaceCoral();
            }
        }

        private void PlaceAlgae()
        {
        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 3f, 0));
        }

        private void PlaceCoral()
        {
            if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 5.5f), 1f, 0.5f);
            }
            else if (LastSetpoint == ReefscapeSetpoints.L1)
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 3.2f));
            }
            else
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 3.5f));
            }
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

        private bool ComputeAlignFlip()
        {
            var flip = false;
            if (GetActiveCamera().transform.eulerAngles.y < 180) flip = !flip;
            if (Mathf.Abs(transform.position.x) > 4.489323f && PlayerPrefs.GetInt("PerspectiveAutoAlign", 1) == 1) flip = !flip;
            if (transform.position.x > 0) flip = !flip;
            return flip;
        }

        private void SetSetpoint(KeikoSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _endEffectorTargetAngle = setpoint.endEffectorAngle;
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            endEffector.SetTargetAngle(_endEffectorTargetAngle).withAxis(JointAxis.X);
            climberBar.SetTargetAngle(_climbBarTargetAngle).withAxis(JointAxis.Y);
            climberFlap.SetTargetAngle(_funnelPivotTargetAngle).withAxis(JointAxis.X);
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

            if (((IntakeAction.IsPressed() && !_coralController.HasPiece() && !_algaeController.HasPiece()) ||
                 OuttakeAction.IsPressed()) &&
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


            var a = soundDetector.OverlapBox(coralMask);
            if (a.Length > 0)
            {
                if (canClack && !funnelCloseSource.isPlaying)
                {
                    funnelCloseSource.Play();
                    canClack = false;
                }
            }
            else
            {
                canClack = true;
            }
        }
    }
}