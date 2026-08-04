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

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._9995
{
    public class BotbustersGreen : ReefscapeRobotBase
    {
        [Header("Robot Components")]
        [SerializeField] private GenericJoint armJoint;
        [SerializeField] private GenericJoint wristJoint;
        // El efector final va en un diferencial: wristJoint mueve el pivot,
        // wristTwistJoint mueve el twist. Ajusta el axis de abajo (FixedUpdate)
        // según cómo esté orientado el diferencial en el rig.
        [SerializeField] private GenericJoint wristTwistJoint;

        // El climber de 9995 NO tiene pivot propio: va montado sobre el
        // armPivot (armJoint). Por eso no hay un GenericJoint ni componente
        // aparte para el climber — climbSetpoint/climbedSetpoint mueven
        // directamente armJoint (junto con wrist/twist) como cualquier
        // otro setpoint.

        [Header("Wheels")]
        // Intake de piso, dos pares de GenericRoller (no animation):
        // par delantero (agarra el coral) + par trasero (lo termina de meter).
        [SerializeField] private GenericRoller[] frontIntakeRollers;
        [SerializeField] private GenericRoller[] backIntakeRollers;
        [SerializeField] private Transform leftIntakeSensor;
        [SerializeField] private Transform rightIntakeSensor;
        [SerializeField] private float wheelIntakeSpeed = 8000f;

        [Header("PID Constants")]
        [SerializeField] private PidConstants armPidConstants;
        [SerializeField] private PidConstants wristPidConstants;
        [SerializeField] private PidConstants wristTwistPidConstants;
        [SerializeField] private float pivotStep;
        private float _originalPivotMax;

        [Header("Robot Setpoints")]
        [SerializeField] private BotbustersGreenSetpoint stowSetpoint;
        [SerializeField] private BotbustersGreenSetpoint coralStowSetpoint;
        [SerializeField] private BotbustersGreenSetpoint groundCoralIntakeSetpoint;
        [SerializeField] private BotbustersGreenSetpoint algaeLowSetpoint;
        [SerializeField] private BotbustersGreenSetpoint l3Setpoint;
        [SerializeField] private BotbustersGreenSetpoint l1Setpoint;
        [SerializeField] private BotbustersGreenSetpoint climbSetpoint;
        [SerializeField] private BotbustersGreenSetpoint climbedSetpoint;

        private ReefscapeSetpoints _previousSetpoint = ReefscapeSetpoints.Stow;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;

        private ReefscapeAutoAlign align;

        [Header("Game Piece Intakes")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;

        [Header("Game Piece States")]
        [SerializeField] private string currentState;
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;

        [Header("Intake Audio")]
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeClip;

        [Header("Clicker Joints")]
        // Igual que en 5449 (Prototype): no tiene nada que ver con el pivot
        // del climber (que en 9995 es armJoint), es un mecanismo aparte.
        [SerializeField] private GenericAnimationJoint clickerL;
        [SerializeField] private GenericAnimationJoint clickerR;
        [SerializeField] private float ClickerSpeed = 200;

        [Header("Target Setpoints")]
        [SerializeField] private float _targetArmPivotAngle;
        [SerializeField] private float _targetEndEffectorPivotAngle;
        [SerializeField] private float _targetEndEffectorTwistAngle;

        private bool _isScoring;
        private bool preAligned = false;
        private ClimbScorer climbScorer;

        protected override void Start()
        {
            base.Start();

            climbScorer = gameObject.GetComponent<ClimbScorer>();

            armJoint.SetPid(armPidConstants);
            wristJoint.SetPid(wristPidConstants);
            wristTwistJoint.SetPid(wristTwistPidConstants);
            _originalPivotMax = armPidConstants.Max;

            _targetArmPivotAngle = stowSetpoint.armPivotAngle;
            _targetEndEffectorPivotAngle = stowSetpoint.endEffectorPivotAngle;
            _targetEndEffectorTwistAngle = stowSetpoint.endEffectorTwistAngle;

            RobotGamePieceController.SetPreload(coralStowState);

            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());

            _coralController.gamePieceStates = new[] { coralIntakeState, coralStowState };
            _coralController.intakes.Add(coralIntake);

            intakeAudioSource.clip = intakeClip;
            intakeAudioSource.loop = true;
            intakeAudioSource.playOnAwake = false;

            align = gameObject.GetComponent<ReefscapeAutoAlign>();
            preAligned = false;
        }

        private new void Update()
        {
            base.Update();

            // Ratchet de los clickers del climber (igual que en 5449).
            clickerL.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            clickerR.SpringLoaded().AllowedDirection(-1).RotationSpeed(ClickerSpeed);
        }

        private void LateUpdate()
        {
            armJoint.UpdatePid(armPidConstants);
            wristJoint.UpdatePid(wristPidConstants);
            wristTwistJoint.UpdatePid(wristTwistPidConstants);
        }

        private void FixedUpdate()
        {
            armJoint.SetTargetAngle(_targetArmPivotAngle).withAxis(JointAxis.X).flipDirection();
            wristJoint.SetTargetAngle(_targetEndEffectorPivotAngle).withAxis(JointAxis.Y).flipDirection();
            // Twist del diferencial. Si en el rig el twist gira sobre otro eje,
            // cambia JointAxis.Y por el que corresponda.
            wristTwistJoint.SetTargetAngle(_targetEndEffectorTwistAngle).withAxis(JointAxis.Y);

            var canIntakeCoral = _coralController.currentStateNum == 0 && IntakeAction.IsPressed();
            var realStep = pivotStep;

            if (Utils.WithinAngularRange(armJoint.GetSingleAxisAngle(JointAxis.X), _targetArmPivotAngle, 15f))
                armPidConstants.Max = Mathf.Max(armPidConstants.Max - (realStep * Time.fixedDeltaTime), realStep);
            else
                armPidConstants.Max = Mathf.Min(armPidConstants.Max + (realStep * Time.fixedDeltaTime), _originalPivotMax);

            var readState = _coralController.GetCurrentState();
            if (readState != null)
            {
                currentState = readState.name;
            }

            UpdateIntakeAudio();

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled) return;

            if (CurrentSetpoint is ReefscapeSetpoints.Climb or ReefscapeSetpoints.Climbed) DriveController.SetDriveMp(0.5f);
            else DriveController.SetDriveMp(1);

            // --- LÓGICA DE RODILLOS ---
            // Se corre solo si no estamos en medio de una coroutine de scoring.
            if (!_isScoring)
            {
                bool isIntaking = CurrentSetpoint == ReefscapeSetpoints.Intake && IntakeAction.IsPressed();
                bool isDescoring = CurrentSetpoint == ReefscapeSetpoints.LowAlgae && IntakeAction.IsPressed();

                if (isIntaking)
                {
                    foreach (var roller in frontIntakeRollers) roller.ChangeAngularVelocity(wheelIntakeSpeed);
                    foreach (var roller in backIntakeRollers) roller.ChangeAngularVelocity(wheelIntakeSpeed);
                }
                else if (isDescoring)
                {
                    // Gira los rollers para tumbar el alga del reef.
                    foreach (var roller in frontIntakeRollers) roller.ChangeAngularVelocity(-wheelIntakeSpeed);
                    foreach (var roller in backIntakeRollers) roller.ChangeAngularVelocity(-wheelIntakeSpeed);
                }
                else
                {
                    foreach (var roller in frontIntakeRollers) roller.ChangeAngularVelocity(0);
                    foreach (var roller in backIntakeRollers) roller.ChangeAngularVelocity(0);
                }
            }

            AutoAlignOffsets();

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (_coralController.currentStateNum != 0)
                    {
                        SetSetpoint(coralStowSetpoint);
                        _coralController.SetTargetState(coralStowState);
                    }
                    else
                    {
                        SetSetpoint(stowSetpoint);
                    }
                    break;

                case ReefscapeSetpoints.Intake:
                    // Solo puede agarrar coral de piso, siempre con el intake
                    // normal (como Overture): no hay un modo/pose L1 aparte.
                    SetSetpoint(groundCoralIntakeSetpoint);
                    _coralController.SetTargetState(coralIntakeState);
                    _coralController.RequestIntake(coralIntake, canIntakeCoral);
                    break;

                case ReefscapeSetpoints.Place:
                    StartCoroutine(PlaceGamePiece(LastSetpoint));
                    break;

                case ReefscapeSetpoints.L1:
                    // L1 solo mirando al frente, usando el coral tal cual se
                    // agarró (misma pose de intake que cualquier otro nivel).
                    SetSetpoint(l1Setpoint);
                    _coralController.SetTargetState(coralStowState);
                    break;

                case ReefscapeSetpoints.L3:
                    // L3 solo mirando hacia atrás (el brazo pivotea, no hay elevador).
                    // El coral es fijo en el intake (como en 7421), así que no
                    // hay un estado "back" distinto: siempre coralStowState.
                    SetSetpoint(l3Setpoint);
                    _coralController.SetTargetState(coralStowState);
                    break;

                case ReefscapeSetpoints.LowAlgae:
                    // Descore de algas LOW, siempre mirando hacia atrás.
                    SetSetpoint(algaeLowSetpoint);
                    break;

                case ReefscapeSetpoints.Climb:
                    // El climber va montado en armPivot: subir a Climb es
                    // simplemente llevar armJoint (+ wrist/twist) a la pose
                    // de climbSetpoint, igual que cualquier otro setpoint.
                    SetSetpoint(climbSetpoint);
                    break;

                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbedSetpoint);
                    break;

                // --- Setpoints que este robot NO soporta: se quedan quietos en Stow ---
                // (Se dejan explícitos en vez de default para que un botón mal
                // presionado no rompa nada, en lugar de tirar la excepción.)
                case ReefscapeSetpoints.Processor:
                case ReefscapeSetpoints.Stack:
                case ReefscapeSetpoints.L2:
                case ReefscapeSetpoints.HighAlgae:
                case ReefscapeSetpoints.L4:
                case ReefscapeSetpoints.Barge:
                case ReefscapeSetpoints.RobotSpecial:
                    SetSetpoint(stowSetpoint);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Raycast de agarre de coral en piso (intake normal, sin modo L1 aparte).
            _coralController.MoveIntake(coralIntake, coralIntakeState.stateTarget);

            var rayDirection = coralIntakeState.stateTarget.forward;
            var distance = 0.0254f * 5f;
            var coralMask = LayerMask.GetMask("Coral");
            var coralRight = Physics.Raycast(rightIntakeSensor.position, rayDirection, distance, coralMask);
            var coralLeft = Physics.Raycast(leftIntakeSensor.position, rayDirection, distance, coralMask);

            if (IntakeAction.IsPressed() && CurrentSetpoint != ReefscapeSetpoints.LowAlgae)
            {
                if (coralRight && coralLeft)
                {
                    foreach (var roller in frontIntakeRollers) roller.ChangeAngularVelocity(wheelIntakeSpeed);
                    foreach (var roller in backIntakeRollers) roller.ChangeAngularVelocity(wheelIntakeSpeed);
                }
            }

            // Auto-transición del climb. Al no haber un joint/script de climber
            // aparte (va montado en armPivot), la condición de "enganchado" es
            // simplemente el trigger del ClimbScorer, sin WingsOpen().
            if (climbScorer != null && climbScorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climb)
            {
                SetState(ReefscapeSetpoints.Climbed);
            }
            else if (climbScorer != null && !climbScorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climbed)
            {
                SetState(ReefscapeSetpoints.Climb);
            }

            _previousSetpoint = CurrentSetpoint;
        }

        private IEnumerator PlaceGamePiece(ReefscapeSetpoints lastSetpoint)
        {
            _isScoring = true; // Bloquea la lógica de rodillos de FixedUpdate

            bool isL1 = lastSetpoint == ReefscapeSetpoints.L1;

            // L1 (frente) -> rodillos hacia adelante (+)
            // L3 (atrás)  -> rodillos hacia atrás (-)
            float speed = isL1 ? wheelIntakeSpeed : -wheelIntakeSpeed;

            foreach (var roller in frontIntakeRollers) roller.ChangeAngularVelocity(speed);
            foreach (var roller in backIntakeRollers) roller.ChangeAngularVelocity(speed);

            // Ajusta estos vectores de fuerza según la orientación real del
            // efector al soltar en L1 (frente) vs L3 (atrás).
            Vector3 force = isL1 ? new Vector3(1, 0, 0) : new Vector3(0, 0, -5);

            _coralController.ReleaseGamePieceWithForce(force);

            // Espera hasta que la pieza se suelte (estado vuelve a 0) o timeout 0.5s
            float timer = 0f;
            while (_coralController.currentStateNum != 0 && timer < 0.5f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            foreach (var roller in frontIntakeRollers) roller.ChangeAngularVelocity(0);
            foreach (var roller in backIntakeRollers) roller.ChangeAngularVelocity(0);

            _isScoring = false;
        }

        private void AutoAlignOffsets()
        {
            if (!AutoAlignLeftAction.IsPressed() && !AutoAlignRightAction.IsPressed())
            {
                preAligned = false;
            }
            float xOffset1 = 0f;
            float xOffset2 = -0f;
            float zOffset1 = 0f;
            float zOffset2 = 0f;
            if (!preAligned && (AutoAlignLeftAction.IsPressed() || AutoAlignRightAction.IsPressed()))
            {
                xOffset1 = 0f;
                xOffset2 = -0f;
                zOffset1 = 0f;
                zOffset2 = 0f;
                if (align.getDistance() < 0.0254f * 6f && !AutoAlignLeftAction.triggered && !AutoAlignRightAction.triggered)
                {
                    preAligned = true;
                }
            }
            float xOffset = !FacingReef ? xOffset1 : xOffset2;
            float zOffset = !FacingReef ? zOffset1 : zOffset2;
            align.offset = new Vector3(xOffset, 0, zOffset);
        }


        private void UpdateIntakeAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (intakeAudioSource.isPlaying)
                {
                    intakeAudioSource.Stop();
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
        }

        private void SetSetpoint(BotbustersGreenSetpoint setpoint)
        {
            _targetArmPivotAngle = setpoint.armPivotAngle;
            _targetEndEffectorPivotAngle = setpoint.endEffectorPivotAngle;
            _targetEndEffectorTwistAngle = setpoint.endEffectorTwistAngle;
        }
    }
}