using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using MoSimLib;
using UnityEngine;
using UnityEngine.Serialization;

/*
Roult: base de TitaniumRams (5959) + ideas de Tecdroid (CoralL4Stow),
LAMBOT (pivot en el funnel, climb, center of mass) y los clickers (hook con ligas) de BotbustersGreen.

Cambios principales vs TitaniumRams:
- Descorer en garra: brazo izquierdo y derecho totalmente independientes (joint, PID, eje y angulo propios), cada uno igual que el descorer de 5959.
- Setpoint L4 (CoralL4) con CoralL4Stow opcional (checkbox useCoralL4Stow).
- Sin barge de alga. Coral solo se anota de frente.
- Climber: funnel pivot + climber pivot (angulos en el RoultSetpoint) + clickers de ratchet.
- Center of Mass shift al estar en Climbed.
*/

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack.RoultRobot
{
    public class Roult : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint algaeArm;
        // Garra del descorer: dos brazos independientes, cada uno con su joint, PID, eje y angulo en el setpoint.
        // FormerlySerializedAs conserva las referencias ya asignadas en el prefab.
        [FormerlySerializedAs("algaeDescorerArm")]
        [SerializeField] private GenericJoint algaeDescorerArmLeft;
        [FormerlySerializedAs("algaeDescorerArmAlt")]
        [SerializeField] private GenericJoint algaeDescorerArmRight;
        [SerializeField] private JointAxis algaeDescorerArmLeftAxis = JointAxis.X;
        [SerializeField] private JointAxis algaeDescorerArmRightAxis = JointAxis.X;

        [Header("Climber Components")]
        [SerializeField] private GenericJoint funnelPivot;
        [SerializeField] private GenericJoint climberPivot;

        [Header("PID")]
        [SerializeField] private PidConstants algaeArmPid;
        [FormerlySerializedAs("algaeDescorerArmPid")]
        [SerializeField] private PidConstants algaeDescorerArmLeftPid;
        [SerializeField] private PidConstants algaeDescorerArmRightPid;
        [SerializeField] private PidConstants funnelPivotPid;
        [SerializeField] private PidConstants climberPivotPid;

        [Header("Setpoints")]
        [SerializeField] private RoultSetpoint stow;
        [SerializeField] private RoultSetpoint AlgaeStow;
        [SerializeField] private RoultSetpoint intake;        // coral intake pose
        [SerializeField] private RoultSetpoint groundalgae;   // algae ground intake pose
        [SerializeField] private RoultSetpoint processor;     // processor pose
        [SerializeField] private RoultSetpoint l1;
        [SerializeField] private RoultSetpoint l2;
        [SerializeField] private RoultSetpoint l3;
        [SerializeField] private RoultSetpoint CoralL4;
        [SerializeField] private RoultSetpoint descoreLowAlgae;
        [SerializeField] private RoultSetpoint descoreHighAlgae;
        [SerializeField] private RoultSetpoint grabLowAlgae;   // igual que LowAlgae pero con la garra cerrada
        [SerializeField] private RoultSetpoint grabHighAlgae;  // igual que HighAlgae pero con la garra cerrada
        [SerializeField] private RoultSetpoint climb;
        [SerializeField] private RoultSetpoint climbed;

        [Header("Climber Axes")]
        // Los ANGULOS del funnel y del climber pivot van en cada RoultSetpoint.
        [SerializeField] private JointAxis funnelPivotAxis = JointAxis.X;
        [SerializeField] private JointAxis climberPivotAxis = JointAxis.Y;

        [Header("Clicker Joints (ratchet del climber, igual que BotbustersGreen)")]
        [SerializeField] private GenericAnimationJoint clickerL;
        [SerializeField] private GenericAnimationJoint clickerR;
        [SerializeField] private float clickerSpeed = 200f;

        [Header("Intakes")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState coralL4StowState;
        [Tooltip("Si esta apagado, SIEMPRE se usa CoralStow (incluso en L4).")]
        [SerializeField] private bool useCoralL4Stow = true;
        [SerializeField] private GamePieceState algaeStowState;

        [Header("Scoring")]
        [SerializeField] private float coralReleaseForce = 6f;
        [SerializeField] private float L4coralReleaseForce = 5f;
        [SerializeField] private float L1coralReleaseForce = 3f;
        
        [SerializeField] private float algaeReleaseForce = -1.2f;

        [Header("Center of Mass")]
        [SerializeField] private bool addCenterOfMassX;
        [SerializeField] private bool addCenterOfMassZ;
        [SerializeField] private float climbedCenterOfMassX;
        [SerializeField] private float climbedCenterOfMassZ;
        private Rigidbody _mainRb;
        private Vector3 _originalCenterOfMass;
        private bool _isCgShifted;

        [Header("Algae Stall Audio")]
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallAudio;

        [Header("Robot Audio")]
        [SerializeField] private AudioSource rollerSource;
        [SerializeField] private AudioClip intakeClip;

        [Header("Animation Joints (Wheels)")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private GenericAnimationJoint[] intakeWheelsReverse;
        [SerializeField] private GenericAnimationJoint[] algaeintakeWheels;
        [SerializeField] private GenericAnimationJoint[] algaeintakeWheelsReverse;
        [SerializeField] private float wheelIntakeSpeed = 1000f;
        [SerializeField] private float wheelIntakeSpeedReverse = -1000f;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _elevatorTargetHeight;
        private float _algaeTargetAngle;
        private float _algaeDescorerLeftTargetAngle;
        private float _algaeDescorerRightTargetAngle;
        private float _funnelTargetAngle;
        private float _climberPivotTargetAngle;
        private bool _climbLocked;
        private bool _isScoring;
        private bool _rollersActive;
        private bool _outtakeWasPressed;
        [Tooltip("Segundos que el robot conserva su pose tras pedir/recibir alga, antes de ir a stow, mientras el controller registra la pieza.")]
        [SerializeField] private float algaeStowGraceTime = 0.4f;
        private float _algaeGraceTimer;

        private bool _climbWasPressed;

        protected override void Start()
        {
            base.Start();

            // Un elemento vacio en estas listas (None en el Inspector) lanzaba una
            // NullReferenceException en cada FixedUpdate. Se filtran al iniciar.
            intakeWheels = RemoveNulls(intakeWheels, nameof(intakeWheels));
            intakeWheelsReverse = RemoveNulls(intakeWheelsReverse, nameof(intakeWheelsReverse));
            algaeintakeWheels = RemoveNulls(algaeintakeWheels, nameof(algaeintakeWheels));
            algaeintakeWheelsReverse = RemoveNulls(algaeintakeWheelsReverse, nameof(algaeintakeWheelsReverse));

            if (algaeArm != null) algaeArm.SetPid(algaeArmPid);
            if (algaeDescorerArmLeft != null) algaeDescorerArmLeft.SetPid(algaeDescorerArmLeftPid);
            if (algaeDescorerArmRight != null) algaeDescorerArmRight.SetPid(algaeDescorerArmRightPid);
            if (funnelPivot != null) funnelPivot.SetPid(funnelPivotPid);
            if (climberPivot != null) climberPivot.SetPid(climberPivotPid);

            _elevatorTargetHeight = 0f;
            _algaeTargetAngle = 0f;
            _algaeDescorerLeftTargetAngle = 0f;
            _algaeDescorerRightTargetAngle = 0f;
            _funnelTargetAngle = 0f;
            _climberPivotTargetAngle = 0f;
            _climbLocked = false;

            // Preload coral
            RobotGamePieceController.SetPreload(coralStowState);

            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            if (algaeStallSource != null)
            {
                algaeStallSource.clip = algaeStallAudio;
                algaeStallSource.loop = true;
                algaeStallSource.Stop();
            }

            if (rollerSource != null)
            {
                rollerSource.clip = intakeClip;
                rollerSource.loop = true;
                rollerSource.Stop();
            }

            // Setup controllers properly (igual que Titanium, pero con los dos estados de coral)
            if (_coralController != null)
            {
                _coralController.gamePieceStates = coralL4StowState != null
                    ? new[] { coralStowState, coralL4StowState }
                    : new[] { coralStowState };
                if (coralIntake != null) _coralController.intakes.Add(coralIntake);
            }

            if (_algaeController != null)
            {
                _algaeController.gamePieceStates = new[] { algaeStowState };
                if (algaeIntake != null) _algaeController.intakes.Add(algaeIntake);
            }

            // Center of mass (igual que LAMBOT / Tecdroid)
            _mainRb = gameObject.GetComponent<Rigidbody>();
            _isCgShifted = false;
            if (_mainRb != null)
            {
                _originalCenterOfMass = _mainRb.centerOfMass;
            }
            else
            {
                Debug.LogWarning("Roult: no se encontro Rigidbody, el CG shift no va a funcionar.");
            }
        }

        private GenericAnimationJoint[] RemoveNulls(GenericAnimationJoint[] wheels, string listName)
        {
            if (wheels == null) return new GenericAnimationJoint[0];

            var clean = System.Array.FindAll(wheels, w => w != null);
            if (clean.Length != wheels.Length)
            {
                Debug.LogWarning($"Roult: '{listName}' tenia {wheels.Length - clean.Length} elemento(s) vacio(s) en el Inspector, se ignoran.");
            }

            return clean;
        }

        private new void Update()
        {
            base.Update();

            // Ratchet de los clickers del climber (el hook con ligas), igual que BotbustersGreen.
            if (clickerL != null) clickerL.SpringLoaded().AllowedDirection(1).RotationSpeed(clickerSpeed);
            if (clickerR != null) clickerR.SpringLoaded().AllowedDirection(-1).RotationSpeed(clickerSpeed);
        }

        // True si el robot ya trae alga O si el alga ya esta dentro del intake pero el controller
        // todavia no la registra (currentStateNum sigue en 0 uno o varios ticks). Sin esto, el
        // case Stow veia "sin alga", mandaba el robot a stow y luego regresaba a AlgaeStow.
        private bool AlgaeHeldOrEntering()
        {
            return _algaeController.HasPiece()
                   || _algaeController.currentStateNum != 0
                   || (algaeIntake != null && _algaeController.IntakeHasPieces(algaeIntake));
        }

        private void LateUpdate()
        {
            if (algaeArm != null) algaeArm.UpdatePid(algaeArmPid);
            if (algaeDescorerArmLeft != null) algaeDescorerArmLeft.UpdatePid(algaeDescorerArmLeftPid);
            if (algaeDescorerArmRight != null) algaeDescorerArmRight.UpdatePid(algaeDescorerArmRightPid);
            if (funnelPivot != null) funnelPivot.UpdatePid(funnelPivotPid);
            if (climberPivot != null) climberPivot.UpdatePid(climberPivotPid);
        }

        private void FixedUpdate()
        {
            if (_coralController == null || _algaeController == null) return;

            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            // keep both pieces in their stow states
            // CoralL4Stow solo se usa cuando el elevador va a L4 (o se esta anotando desde L4)
            if (algaeStowState != null) _algaeController.SetTargetState(algaeStowState);

            bool atL4 = CurrentSetpoint == ReefscapeSetpoints.L4
                        || (CurrentSetpoint == ReefscapeSetpoints.Place && LastSetpoint == ReefscapeSetpoints.L4);
            GamePieceState wantedCoralState = (useCoralL4Stow && atL4 && coralL4StowState != null)
                ? coralL4StowState
                : coralStowState;
            if (wantedCoralState != null) _coralController.SetTargetState(wantedCoralState);

            // Los angulos de funnel y climber pivot vienen del setpoint activo,
            // asi que al salir de Climb regresan solos al valor del nuevo setpoint.
            _climbLocked = CurrentSetpoint == ReefscapeSetpoints.Climb || CurrentSetpoint == ReefscapeSetpoints.Climbed;

            // Igual que BotbustersGreen: el drive va a la mitad al subir.
            if (_climbLocked) DriveController.SetDriveMp(0.5f);
            else DriveController.SetDriveMp(1f);

            bool intakePressed = IntakeAction != null && IntakeAction.IsPressed();

            // Deteccion manual de flanco de subida (ver comentario en TitaniumRams).
            bool outtakeHeld = OuttakeAction != null && OuttakeAction.IsPressed();
            bool outtakeJustPressed = outtakeHeld && !_outtakeWasPressed;

            bool climbHeld = ClimbAction != null && ClimbAction.IsPressed();
            bool climbJustPressed = climbHeld && !_climbWasPressed;

            bool wantAlgaeIntakeRequest = !_climbLocked && CurrentRobotMode == ReefscapeRobotMode.Algae && intakePressed && !hasAlgae;
            _algaeController.RequestIntake(algaeIntake, wantAlgaeIntakeRequest);

            // Mientras se pide el alga (o ya esta entrando al intake) se renueva el margen del Stow.
            if (wantAlgaeIntakeRequest || (algaeIntake != null && _algaeController.IntakeHasPieces(algaeIntake)))
                _algaeGraceTimer = algaeStowGraceTime;
            else
                _algaeGraceTimer = Mathf.Max(0f, _algaeGraceTimer - Time.fixedDeltaTime);

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (hasAlgae)
                    {
                        SetSetpoint(AlgaeStow);
                    }
                    else {
                        SetSetpoint(stow);
                    }
                    break;
                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral || hasAlgae)
                    {
                        SetSetpoint(intake);
                    }
                    else
                    {
                        SetSetpoint(groundalgae);
                    }
                    _coralController.RequestIntake(coralIntake,
                        !_climbLocked && CurrentRobotMode == ReefscapeRobotMode.Coral && intakePressed && !hasAlgae);
                    break;

                case ReefscapeSetpoints.Place:
                    if (outtakeJustPressed)
                    {
                        bool hadBoth = hasCoral && hasAlgae;

                        PlacePiece();
                        StartCoroutine(ScoreCoroutine());

                        if (hadBoth)
                        {
                            // Superciclo: si tenias coral Y alga y acabas de anotar la
                            // pieza correspondiente al modo actual, cambia automaticamente
                            // al otro modo para poder moverte directo a su setpoint y soltarla.
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

                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.L4:
                    SetSetpoint(CoralL4);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.Stack:
                    // algae-only intake setpoint
                    SetSetpoint(groundalgae);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processor);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.Barge:
                    // Este robot NO puede alga en barge: se ignora y regresa a Stow.
                    SetState(ReefscapeSetpoints.Stow);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.LowAlgae:
                    // Garra abierta a la altura de low algae; con intake presionado cierra la garra (Grab).
                    // La garra no tiene algaeIntake (como Titanium), solo se mueve su setpoint.
                    SetSetpoint(intakePressed ? grabLowAlgae : descoreLowAlgae);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(intakePressed ? grabHighAlgae : descoreHighAlgae);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climb);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    break;

                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    StopAllIntakes();
                    break;
            }

            UpdateSetpoints();
            UpdateCenterOfMass();

            UpdateRollers(hasCoral, hasAlgae, intakePressed);
            UpdateAudio();

            _outtakeWasPressed = outtakeHeld;
            _climbWasPressed = climbHeld;
        }

        private void UpdateRollers(bool hasCoral, bool hasAlgae, bool intakePressed)
        {
            // Mientras se esta anotando (ver ScoreCoroutine), esta logica no debe pisar
            // la velocidad que esta aplicando la corrutina.
            if (_isScoring)
            {
                _rollersActive = true;
                return;
            }

            // Rollers del efector de coral: giran solo en modo Coral, en el setpoint de
            // Intake, con el boton presionado y sin coral ya agarrado.
            bool wantCoralIntake = !_climbLocked
                                   && CurrentRobotMode == ReefscapeRobotMode.Coral
                                   && CurrentSetpoint == ReefscapeSetpoints.Intake
                                   && intakePressed && !hasCoral && !hasAlgae;

            if (wantCoralIntake)
            {
                foreach (var wheel in intakeWheels) wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.Y);
                foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(wheelIntakeSpeedReverse).useAxis(JointAxis.Y);
            }
            else
            {
                foreach (var wheel in intakeWheels) wheel.VelocityRoller(0).useAxis(JointAxis.Y);
                foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(0).useAxis(JointAxis.Y);
            }

            // Rollers del intake de algas: mecanismo aparte, independiente del de coral.
            // Se activa en modo Algae, ya sea en Intake (piso) o Stack, sin alga ya agarrada.
            bool wantAlgaeIntake = !_climbLocked
                                   && CurrentRobotMode == ReefscapeRobotMode.Algae
                                   && (CurrentSetpoint == ReefscapeSetpoints.Intake || CurrentSetpoint == ReefscapeSetpoints.Stack)
                                   && intakePressed && !hasAlgae;

            if (wantAlgaeIntake)
            {
                foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.X);
                foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(wheelIntakeSpeedReverse).useAxis(JointAxis.X);
            }
            else
            {
                foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(0).useAxis(JointAxis.X);
                foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(0).useAxis(JointAxis.X);
            }

            _rollersActive = wantCoralIntake || wantAlgaeIntake;
        }

        private IEnumerator ScoreCoroutine()
        {
            _isScoring = true;

            // Al anotar, se invierte la direccion de los rollers del mecanismo
            // correspondiente para expulsar la pieza.
            bool scoringAlgae = CurrentRobotMode == ReefscapeRobotMode.Algae;
            float speed = scoringAlgae ? -wheelIntakeSpeed : wheelIntakeSpeed;
            float speedReverse = scoringAlgae ? -wheelIntakeSpeedReverse : wheelIntakeSpeedReverse;

            // Outtake infinito: mientras el boton siga presionado, los rollers (y el
            // audio, que sigue a _rollersActive) se mantienen girando. Se suelta al soltar el boton.
            while (OuttakeAction != null && OuttakeAction.IsPressed())
            {
                if (scoringAlgae)
                {
                    foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(speed);
                    foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(speedReverse);
                }
                else
                {
                    foreach (var wheel in intakeWheels) wheel.VelocityRoller(speed);
                    foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(speedReverse);
                }

                yield return null;
            }

            foreach (var wheel in intakeWheels) wheel.VelocityRoller(0);
            foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(0);
            foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(0);
            foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(0);

            _isScoring = false;
        }

        private void StopAllIntakes()
        {
            if (_coralController != null && coralIntake != null)
                _coralController.RequestIntake(coralIntake, false);

            if (_algaeController != null && algaeIntake != null)
                _algaeController.RequestIntake(algaeIntake, false);
        }

        private void PlacePiece()
        {
            if (_algaeController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, algaeReleaseForce));
            }
            else
            {
                // Coral solo se anota de frente: la fuerza siempre va hacia adelante.
                if (_coralController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    if(LastSetpoint == ReefscapeSetpoints.L4)
                    {
                        // Si se esta anotando desde L4, se suelta con fuerza hacia atras para que caiga en la zona de anotacion.
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, L4coralReleaseForce));
                    }
                    else if(LastSetpoint == ReefscapeSetpoints.L1)
                    {
                        // Si se esta anotando desde L1, se suelta con fuerza hacia atras para que caiga en la zona de anotacion.
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, L1coralReleaseForce));
                    }
                    else{
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, coralReleaseForce));
                    }
                }
            }
        }

        private void SetSetpoint(RoultSetpoint setpoint)
        {
            if (setpoint == null) return;
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _algaeTargetAngle = setpoint.algaeArmAngle;
            _algaeDescorerLeftTargetAngle = setpoint.algaeDescorerArmLeftAngle;
            _algaeDescorerRightTargetAngle = setpoint.algaeDescorerArmRightAngle;
            _funnelTargetAngle = setpoint.funnelAngle;
            _climberPivotTargetAngle = setpoint.climberPivotAngle;
        }

        private void UpdateSetpoints()
        {
            if (elevator != null) elevator.SetTarget(_elevatorTargetHeight);

            if (algaeArm != null)
                algaeArm.SetTargetAngle(_algaeTargetAngle).withAxis(JointAxis.X);

            if (algaeDescorerArmLeft != null)
                algaeDescorerArmLeft.SetTargetAngle(_algaeDescorerLeftTargetAngle).withAxis(algaeDescorerArmLeftAxis);

            if (algaeDescorerArmRight != null)
                algaeDescorerArmRight.SetTargetAngle(_algaeDescorerRightTargetAngle).withAxis(algaeDescorerArmRightAxis);

            if (funnelPivot != null)
                funnelPivot.SetTargetAngle(_funnelTargetAngle).withAxis(funnelPivotAxis);

            if (climberPivot != null)
                climberPivot.SetTargetAngle(_climberPivotTargetAngle).withAxis(climberPivotAxis);
        }

        private void UpdateCenterOfMass()
        {
            if (_mainRb == null) return;

            if (CurrentSetpoint == ReefscapeSetpoints.Climbed)
            {
                if (!_isCgShifted)
                {
                    // Cada eje solo se mueve si su checkbox esta activado.
                    _mainRb.centerOfMass = new Vector3(
                        addCenterOfMassX ? climbedCenterOfMassX : _originalCenterOfMass.x,
                        _originalCenterOfMass.y,
                        addCenterOfMassZ ? climbedCenterOfMassZ : _originalCenterOfMass.z);
                    _isCgShifted = true;
                }
            }
            else if (_isCgShifted)
            {
                _mainRb.centerOfMass = _originalCenterOfMass;
                _isCgShifted = false;
            }
        }

        private void UpdateAudio()
        {
            if (rollerSource == null || algaeStallSource == null) return;

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (rollerSource.isPlaying || algaeStallSource.isPlaying)
                {
                    rollerSource.Stop();
                    algaeStallSource.Stop();
                }

                return;
            }

            if (_rollersActive && !rollerSource.isPlaying)
            {
                rollerSource.Play();
            }
            else if (!_rollersActive && rollerSource.isPlaying)
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