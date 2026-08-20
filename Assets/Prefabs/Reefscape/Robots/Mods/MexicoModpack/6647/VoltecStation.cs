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
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._6647._9982B
{
    public class VoltecStation : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint algaeArm;

        [Header("PIDS")]
        [SerializeField] private PidConstants algaeArmPid;

        [Header("Rollers - End Effector (como TitaniumRams)")]
        [SerializeField] private GenericRoller endEffectorRollerLeft;
        [SerializeField] private GenericRoller endEffectorRollerRight;


        [Header("Rollers - Funnel (como Firebots)")]
        [SerializeField] private GenericRoller funnelRollerLeft;
        [SerializeField] private GenericRoller funnelRollerLeft2;
        [SerializeField] private GenericRoller funnelRollerRight;

        [Header("Rollers - Algae")]
        [SerializeField] private GenericRoller algaeRollerLeft;
        [SerializeField] private GenericRoller algaeRollerRight;

        [Header("Roller Velocities")]
        [SerializeField] private float endEffectorIntakeVelocity;
        [SerializeField] private float endEffectorOuttakeVelocity;
        [SerializeField] private float funnelIntakeVelocity;
        [SerializeField] private float algaeIntakeVelocity;
        [SerializeField] private float algaeOuttakeVelocity;

        [Header("Coral Setpoints")]
        [SerializeField] private VoltecBSetpoint stow;
        [SerializeField] private VoltecBSetpoint intake;
        [SerializeField] private VoltecBSetpoint l1;
        [SerializeField] private VoltecBSetpoint l1Place;
        [SerializeField] private VoltecBSetpoint l2;
        [SerializeField] private VoltecBSetpoint l3;
        [SerializeField] private VoltecBSetpoint l4;
        [SerializeField] private VoltecBSetpoint l4Place;

        [Header("Algae Setpoints")]
        [SerializeField] private VoltecBSetpoint AlgaeStow;
        [SerializeField] private VoltecBSetpoint AlgaeIntake;
        [SerializeField] private VoltecBSetpoint lowAlgae;
        [SerializeField] private VoltecBSetpoint highAlgae;
        [SerializeField] private VoltecBSetpoint barge;

        [Header("Intake Components")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState algaeStowState;

        [Header("Algae Stall Audio")]
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallAudio;

        [Header("End Effector Audio")]
        [SerializeField] private AudioSource endEffectorRollerSource;
        [SerializeField] private AudioClip endEffectorRollerClip;

        [Header("Funnel Audio")]
        [SerializeField] private AudioSource funnelRollerSource;
        [SerializeField] private AudioClip funnelRollerClip;

        [Header("Algae Roller Audio")]
        [SerializeField] private AudioSource algaeRollerSource;
        [SerializeField] private AudioClip algaeRollerClip;

        [Header("Colliders")]
        [SerializeField] private BoxCollider[] algaeDisableColliders;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _elevatorTargetHeight;
        private float _algaeArmTargetAngle;
        private bool _endEffectorRollersActive;
        private bool _funnelRollersActive;
        private bool _algaeRollersActive;
        private bool _outtakeWasPressed;
        private bool _isScoring;

        protected override void Start()
        {
            base.Start();

            algaeArm.SetPid(algaeArmPid);

            _elevatorTargetHeight = 0;
            _algaeArmTargetAngle = 0;

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

            endEffectorRollerSource.clip = endEffectorRollerClip;
            endEffectorRollerSource.loop = true;
            endEffectorRollerSource.Stop();

            funnelRollerSource.clip = funnelRollerClip;
            funnelRollerSource.loop = true;
            funnelRollerSource.Stop();

            algaeRollerSource.clip = algaeRollerClip;
            algaeRollerSource.loop = true;
            algaeRollerSource.Stop();

        }

        private void LateUpdate()
        {
            algaeArm.UpdatePid(algaeArmPid);
        }

        private void FixedUpdate()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();
            bool intakePressed = IntakeAction.IsPressed();

            // Deteccion manual de flanco de subida para el outtake (igual que TitaniumRams):
            // asi el spin de los rollers sigue directo al boton, no al CurrentSetpoint,
            // y no se queda pegado si no hay pieza que soltar.
            bool outtakeHeld = OuttakeAction != null && OuttakeAction.IsPressed();
            bool outtakeJustPressed = outtakeHeld && !_outtakeWasPressed;

            _algaeController.SetTargetState(algaeStowState);
            _coralController.SetTargetState(coralStowState);

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
                    SetSetpoint(intake);
                    // Solo agarra coral de la Station, no del piso ni algas aqui
                    _coralController.RequestIntake(coralIntake, CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral && !hasAlgae);
                    break;
                case ReefscapeSetpoints.Place:
                    if (LastSetpoint == ReefscapeSetpoints.Barge)
                    {
                        SetSetpoint(barge);
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L4)
                    {
                        SetSetpoint(l4Place);
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L1)
                    {
                        SetSetpoint(l1Place);
                    }

                    if (outtakeJustPressed)
                    {
                        PlacePiece();
                        StartCoroutine(ScoreCoroutine());
                    }
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(AlgaeIntake);
                    _algaeController.RequestIntake(algaeIntake, intakePressed && !hasAlgae && !hasCoral);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgae);
                    _algaeController.RequestIntake(algaeIntake, intakePressed && !hasAlgae && !hasCoral);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgae);
                    _algaeController.RequestIntake(algaeIntake, intakePressed && !hasAlgae && !hasCoral);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Barge:
                    // Un solo setpoint de barge: sube y ahi mismo se hace el outtake con rollers
                    SetSetpoint(barge);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
            }

            UpdateSetpoints();
            UpdateRollers(hasCoral, hasAlgae, intakePressed);
            UpdateAudio();

            _outtakeWasPressed = outtakeHeld;
        }

        private void PlacePiece()
        {
            // Las algas solo se anotan en el Barge (fisicamente no hay processor).
            // No usamos HasPiece() solo porque tambien liberaria el alga si por error
            // se presiona outtake en otro setpoint (p. ej. Processor) mientras se trae.
            if (_algaeController.HasPiece() && LastSetpoint == ReefscapeSetpoints.Barge)
            {
                // Barge (estilo Robonauts): no se avienta, solo sube y se outtakea con los rollers
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 4f, 0));
            }
            else if (_coralController.HasPiece())
            {
                if (LastSetpoint == ReefscapeSetpoints.L4)
                {
                    _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 5.5f), 1f, 0.5f);
                }
                else if (LastSetpoint == ReefscapeSetpoints.L1)
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 2));
                }
                else
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 6));
                }
            }
        }

        private IEnumerator ScoreCoroutine()
        {
            _isScoring = true;

            // Igual que en TitaniumRams: mientras el boton de outtake siga presionado,
            // los rollers (y el audio, que sigue las banderas de abajo) se mantienen
            // girando sin limite de tiempo, para poder despejar un jam. Se detienen
            // en el instante que se suelta el boton, sin depender del CurrentSetpoint.
            bool scoringAlgae = LastSetpoint == ReefscapeSetpoints.Barge;

            while (OuttakeAction != null && OuttakeAction.IsPressed())
            {
                if (scoringAlgae)
                {
                    algaeRollerLeft.ChangeAngularVelocity(-algaeOuttakeVelocity);
                    algaeRollerRight.ChangeAngularVelocity(algaeOuttakeVelocity);
                    _algaeRollersActive = true;
                }
                else
                {
                    endEffectorRollerLeft.ChangeAngularVelocity(endEffectorOuttakeVelocity);
                    endEffectorRollerRight.ChangeAngularVelocity(-endEffectorOuttakeVelocity);
                    _endEffectorRollersActive = true;
                }

                yield return null;
            }

            if (scoringAlgae)
            {
                algaeRollerLeft.ChangeAngularVelocity(0);
                algaeRollerRight.ChangeAngularVelocity(0);
                _algaeRollersActive = false;
            }
            else
            {
                endEffectorRollerLeft.ChangeAngularVelocity(0);
                endEffectorRollerRight.ChangeAngularVelocity(0);
                _endEffectorRollersActive = false;
            }

            _isScoring = false;
        }

        private void SetSetpoint(VoltecBSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _algaeArmTargetAngle = setpoint.algaeArmAngle;
        }

        private void UpdateRollers(bool hasCoral, bool hasAlgae, bool intakePressed)
        {
            // Mientras se esta anotando (ver ScoreCoroutine), esta logica no debe pisar
            // la velocidad que esta aplicando la corrutina.
            if (_isScoring)
            {
                return;
            }

            bool wantsCoralIntake = CurrentSetpoint == ReefscapeSetpoints.Intake &&
                                     CurrentRobotMode == ReefscapeRobotMode.Coral &&
                                     intakePressed && !hasCoral && !hasAlgae;

            // End Effector Rollers
            if (wantsCoralIntake)
            {
                endEffectorRollerLeft.ChangeAngularVelocity(-endEffectorIntakeVelocity);
                endEffectorRollerRight.ChangeAngularVelocity(endEffectorIntakeVelocity);
                _endEffectorRollersActive = true;
            }
            else
            {
                endEffectorRollerLeft.ChangeAngularVelocity(0);
                endEffectorRollerRight.ChangeAngularVelocity(0);
                _endEffectorRollersActive = false;
            }

            // Funnel Rollers
            if (wantsCoralIntake)
            {
                funnelRollerLeft.ChangeAngularVelocity(-funnelIntakeVelocity);
                funnelRollerLeft2.ChangeAngularVelocity(-funnelIntakeVelocity);
                funnelRollerRight.ChangeAngularVelocity(funnelIntakeVelocity);
                _funnelRollersActive = true;
            }
            else
            {
                funnelRollerLeft.ChangeAngularVelocity(0);
                funnelRollerLeft2.ChangeAngularVelocity(0);
                funnelRollerRight.ChangeAngularVelocity(0);
                _funnelRollersActive = false;
            }

            // Algae Rollers (solo intake: reef y lollipop; el outtake vive en ScoreCoroutine)
            bool wantsAlgaeIntake = (CurrentSetpoint == ReefscapeSetpoints.LowAlgae ||
                                      CurrentSetpoint == ReefscapeSetpoints.HighAlgae ||
                                      CurrentSetpoint == ReefscapeSetpoints.Stack) &&
                                     intakePressed && !hasAlgae && !hasCoral;

            if (wantsAlgaeIntake)
            {
                algaeRollerLeft.ChangeAngularVelocity(-algaeIntakeVelocity);
                algaeRollerRight.ChangeAngularVelocity(algaeIntakeVelocity);
                _algaeRollersActive = true;
            }
            else
            {
                algaeRollerLeft.ChangeAngularVelocity(0);
                algaeRollerRight.ChangeAngularVelocity(0);
                _algaeRollersActive = false;
            }

            // Igual que en OvertureWorlds: se apagan los colliders del alga mientras se
            // esta intakeando, para que la pieza no rebote raro contra el frame del robot
            ToggleAlgaeColliders(!wantsAlgaeIntake);
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

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            algaeArm.SetTargetAngle(_algaeArmTargetAngle).withAxis(JointAxis.X);
        }

        private void UpdateAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (endEffectorRollerSource.isPlaying || funnelRollerSource.isPlaying || algaeRollerSource.isPlaying || algaeStallSource.isPlaying)
                {
                    endEffectorRollerSource.Stop();
                    funnelRollerSource.Stop();
                    algaeRollerSource.Stop();
                    algaeStallSource.Stop();
                }

                return;
            }

            if (_endEffectorRollersActive && !endEffectorRollerSource.isPlaying)
            {
                endEffectorRollerSource.Play();
            }
            else if (!_endEffectorRollersActive && endEffectorRollerSource.isPlaying)
            {
                endEffectorRollerSource.Stop();
            }

            if (_funnelRollersActive && !funnelRollerSource.isPlaying)
            {
                funnelRollerSource.Play();
            }
            else if (!_funnelRollersActive && funnelRollerSource.isPlaying)
            {
                funnelRollerSource.Stop();
            }

            if (_algaeRollersActive && !algaeRollerSource.isPlaying)
            {
                algaeRollerSource.Play();
            }
            else if (!_algaeRollersActive && algaeRollerSource.isPlaying)
            {
                algaeRollerSource.Stop();
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
