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
        [SerializeField] private VoltecBSetpoint bargeBack;

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

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _elevatorTargetHeight;
        private float _algaeArmTargetAngle;

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
                    PlacePiece();
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
                    SetSetpoint(FacingBarge() ? barge : bargeBack);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
            }

            UpdateSetpoints();
            UpdateRollers(hasCoral, hasAlgae, intakePressed);
            UpdateAudio();
        }

        private void PlacePiece()
        {
            if (_algaeController.HasPiece())
            {
                // Barge (estilo Robonauts): no se avienta, solo sube y se outtakea con los rollers
                algaeRollerLeft.ChangeAngularVelocity(-algaeOuttakeVelocity);
                algaeRollerRight.ChangeAngularVelocity(algaeOuttakeVelocity);
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 4f, 0));
            }
            else
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

                endEffectorRollerLeft.ChangeAngularVelocity(endEffectorOuttakeVelocity);
                endEffectorRollerRight.ChangeAngularVelocity(-endEffectorOuttakeVelocity);
            }
        }

        private bool FacingBarge()
        {
        return (transform.position.x > 0 && transform.rotation.eulerAngles.y > 180) || (transform.position.x <= 0 && transform.rotation.eulerAngles.y <= 180);
        }

        private void SetSetpoint(VoltecBSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _algaeArmTargetAngle = setpoint.algaeArmAngle;
        }

        private void UpdateRollers(bool hasCoral, bool hasAlgae, bool intakePressed)
        {
            bool wantsCoralIntake = CurrentSetpoint == ReefscapeSetpoints.Intake &&
                                     CurrentRobotMode == ReefscapeRobotMode.Coral &&
                                     intakePressed && !hasCoral && !hasAlgae;

            // End Effector Rollers
            if (wantsCoralIntake)
            {
                endEffectorRollerLeft.ChangeAngularVelocity(-endEffectorIntakeVelocity);
                endEffectorRollerRight.ChangeAngularVelocity(endEffectorIntakeVelocity);
            }
            else if (CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                endEffectorRollerLeft.ChangeAngularVelocity(0);
                endEffectorRollerRight.ChangeAngularVelocity(0);
            }

            // Funnel Rollers
            if (wantsCoralIntake)
            {
                funnelRollerLeft.ChangeAngularVelocity(-funnelIntakeVelocity);
                funnelRollerLeft2.ChangeAngularVelocity(-funnelIntakeVelocity);
                funnelRollerRight.ChangeAngularVelocity(funnelIntakeVelocity);
            }
            else
            {
                funnelRollerLeft.ChangeAngularVelocity(0);
                funnelRollerLeft2.ChangeAngularVelocity(0);
                funnelRollerRight.ChangeAngularVelocity(0);
            }

            // Algae Rollers
            bool wantsAlgaeIntake = (CurrentSetpoint == ReefscapeSetpoints.LowAlgae ||
                                      CurrentSetpoint == ReefscapeSetpoints.HighAlgae ||
                                      CurrentSetpoint == ReefscapeSetpoints.Stack) &&
                                     intakePressed && !hasAlgae && !hasCoral;

            if (wantsAlgaeIntake)
            {
                algaeRollerLeft.ChangeAngularVelocity(-algaeIntakeVelocity);
                algaeRollerRight.ChangeAngularVelocity(algaeIntakeVelocity);
            }
            else if (!(CurrentSetpoint == ReefscapeSetpoints.Place && hasAlgae))
            {
                algaeRollerLeft.ChangeAngularVelocity(0);
                algaeRollerRight.ChangeAngularVelocity(0);
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
                if (endEffectorRollerSource.isPlaying || funnelRollerSource.isPlaying || algaeStallSource.isPlaying)
                {
                    endEffectorRollerSource.Stop();
                    funnelRollerSource.Stop();
                    algaeStallSource.Stop();
                }

                return;
            }

            float eeSpeed = endEffectorRollerLeft.gameObject.GetComponent<Rigidbody>().angularVelocity.magnitude;
            if (eeSpeed > 5f && !endEffectorRollerSource.isPlaying)
            {
                endEffectorRollerSource.Play();
            }
            else if (eeSpeed <= 5f && endEffectorRollerSource.isPlaying)
            {
                endEffectorRollerSource.Stop();
            }

            float funnelSpeed = funnelRollerLeft.gameObject.GetComponent<Rigidbody>().angularVelocity.magnitude;
            if (funnelSpeed > 5f && !funnelRollerSource.isPlaying)
            {
                funnelRollerSource.Play();
            }
            else if (funnelSpeed <= 5f && funnelRollerSource.isPlaying)
            {
                funnelRollerSource.Stop();
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