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

namespace Prefabs.Reefscape.Robots.Mods.PrepaTecPack._5959
{
    public class TitaniumRams : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint algaeArm;

        [Header("PID")]
        [SerializeField] private PidConstants algaeArmPid;

        [Header("Setpoints")]
        [SerializeField] private TitaniumRamsSetpoint stow;
        [SerializeField] private TitaniumRamsSetpoint intake;        // coral intake pose
        [SerializeField] private TitaniumRamsSetpoint groundalgae;   // algae ground intake pose
        [SerializeField] private TitaniumRamsSetpoint processor;    // processor pose
        [SerializeField] private TitaniumRamsSetpoint l1;
        [SerializeField] private TitaniumRamsSetpoint l2;
        [SerializeField] private TitaniumRamsSetpoint l3;

        [Header("Intakes")]
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

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _elevatorTargetHeight;
        private float _algaeTargetAngle;

        protected override void Start()
        {
            base.Start();

            if (algaeArm != null) algaeArm.SetPid(algaeArmPid);

            _elevatorTargetHeight = 0f;
            _algaeTargetAngle = 0f;

            // Preload coral
            if (coralStowState != null)
                RobotGamePieceController.SetPreload(coralStowState);

            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            algaeStallSource.clip = algaeStallAudio;
            algaeStallSource.loop = true;
            algaeStallSource.Stop();
            
            rollerSource.clip = intakeClip;
            rollerSource.loop = true;
            rollerSource.Stop();

            // Setup controllers properly
            if (_coralController != null)
            {
                _coralController.gamePieceStates = new[] { coralStowState };
                if (coralIntake != null) _coralController.intakes.Add(coralIntake);
            }

            if (_algaeController != null)
            {
                _algaeController.gamePieceStates = new[] { algaeStowState };
                if (algaeIntake != null) _algaeController.intakes.Add(algaeIntake);
            }
        }

        private void LateUpdate()
        {
            algaeArm.UpdatePid(algaeArmPid);
        }

        private void FixedUpdate()
        {
            if (_coralController == null || _algaeController == null) return;

            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            // keep both pieces in their stow states
            if (algaeStowState != null) _algaeController.SetTargetState(algaeStowState);
            if (coralStowState != null) _coralController.SetTargetState(coralStowState);

            bool intakePressed = IntakeAction != null && IntakeAction.IsPressed();

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.Intake:
                    // Choose which intake pose to use based on robot mode
                    if (CurrentRobotMode == ReefscapeRobotMode.Algae)
                    {
                        SetSetpoint(groundalgae);
                        _algaeController.RequestIntake(algaeIntake, intakePressed && !hasAlgae && !hasCoral);
                        _coralController.RequestIntake(coralIntake, false);
                    }
                    else
                    {
                        SetSetpoint(intake);
                        _coralController.RequestIntake(coralIntake, intakePressed && !hasCoral && !hasAlgae);
                        _algaeController.RequestIntake(algaeIntake, false);
                    }
                    break;
                
                case ReefscapeSetpoints.Place:
                    if (OuttakeAction != null && OuttakeAction.triggered)
                    {
                        PlacePiece();
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

                case ReefscapeSetpoints.Stack:
                    // algae-only intake setpoint
                    SetSetpoint(groundalgae);
                    _algaeController.RequestIntake(algaeIntake, intakePressed && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;

                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processor);
                    StopAllIntakes();
                    break;

                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    StopAllIntakes();
                    break;
            }

            UpdateSetpoints();
            UpdateAudio();
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
            if (_algaeController.HasPiece())
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 2, 3f));
            }
            else
            {
                if (_coralController.HasPiece())
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 6));
                }
            }
        }
            
        private void SetSetpoint(TitaniumRamsSetpoint setpoint)
        {
            if (setpoint == null) return;
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _algaeTargetAngle = setpoint.algaeArmAngle;
        }

        private void UpdateSetpoints()
        {
            if (elevator != null) elevator.SetTarget(_elevatorTargetHeight);

            // FIX: actually use the setpoint angle instead of forcing 0
            if (algaeArm != null)
                algaeArm.SetTargetAngle(_algaeTargetAngle).withAxis(JointAxis.X);
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
        }
    }
}
