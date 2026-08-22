using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Lambot._3478
{
    public class Lambot: ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint endEffector;
        [SerializeField] private GenericJoint algaeArm;
        [SerializeField] private GenericJoint climberBar;
        [SerializeField] private GenericJoint climberFlap;
        
        [Header("PIDS")]
        [SerializeField] private PidConstants endEffectorPid;
        [SerializeField] private PidConstants algaeArmPid;
        [SerializeField] private PidConstants climberBarPid;
        [SerializeField] private PidConstants climberFlapPid;

        [Header("coral Setpoints")]
        [SerializeField] private KeikoSetpoint stow;
        [SerializeField] private KeikoSetpoint intake;
        [SerializeField] private KeikoSetpoint l1;
        [SerializeField] private KeikoSetpoint l2;
        [SerializeField] private KeikoSetpoint l3;
        [SerializeField] private KeikoSetpoint l4;
        
        [Header("algae Setpoints")]
        [SerializeField] private KeikoSetpoint lowAlgae;
        [SerializeField] private KeikoSetpoint highAlgae;
        
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
        
        protected override void Start()
        {
            base.Start();
            
            endEffector.SetPid(endEffectorPid);
            algaeArm.SetPid(algaeArmPid);
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
            algaeArm.UpdatePid(algaeArmPid);
            climberBar.UpdatePid(climberBarPid);
            climberFlap.UpdatePid(climberFlapPid);
        }

        private void FixedUpdate()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();
            
            _algaeController.SetTargetState(algaeStowState);
            _coralController.SetTargetState(coralStowState);
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Intake:
                    SetSetpoint(intake);

                    _algaeController.RequestIntake(algaeIntake, !_climbLocked && CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, !_climbLocked && !hasCoral && !hasAlgae);
                    break;
                case ReefscapeSetpoints.Place:
                    PlacePiece();
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(intake);
                    _algaeController.RequestIntake(algaeIntake, !_climbLocked && IntakeAction.IsPressed() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgae);
                    _algaeController.RequestIntake(algaeIntake, !_climbLocked && IntakeAction.IsPressed() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgae);
                    _algaeController.RequestIntake(algaeIntake, !_climbLocked && IntakeAction.IsPressed() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Barge:
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    _climbLocked = true;
                    _climbBarTargetAngle = -120;
                    _funnelPivotTargetAngle = -115;
                    break;
                case ReefscapeSetpoints.Climbed:
                    _climbBarTargetAngle = 5;
                    break;
            }
            
            UpdateSetpoints();
            UpdateAudio();
        }

        private void PlacePiece()
        {
            if (_algaeController.HasPiece())
            {
                if (LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 10, 1.5f));
                }
                else
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 1.5f));
                }
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
            }
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
            algaeArm.SetTargetAngle(0).withAxis(JointAxis.X);
            climberBar.SetTargetAngle(_climbBarTargetAngle).withAxis(JointAxis.X);
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