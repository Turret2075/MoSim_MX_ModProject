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

namespace Prefabs.Reefscape.Robots.Mods.LAMBOT._3478
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/LAMBOT Setpoint", order = 0)]
    public class LAMBOTSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Degrees")] public float endEffectorAngle;
    }

    public class Robot3478 : ReefscapeRobotBase
    {
        // ---------------- Elevator / End Effector ----------------
        [Header("Joints")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint endEffectorPivot;

        [Header("PID")]
        [SerializeField] private PidConstants elevatorPid;
        [SerializeField] private PidConstants endEffectorPid;

        [Header("Setpoints")]
        [SerializeField] private LAMBOTSetpoint stowSetpoint;
        [SerializeField] private LAMBOTSetpoint intakeSetpoint;
        [SerializeField] private LAMBOTSetpoint l1Setpoint;
        [SerializeField] private LAMBOTSetpoint l2Setpoint;
        [SerializeField] private LAMBOTSetpoint l3Setpoint;
        [SerializeField] private LAMBOTSetpoint l4Setpoint;

        private float _targetElevatorHeight; // TODO tune-in-MoSim
        private float _targetEndEffectorAngle; // TODO tune-in-MoSim

        private void SetSetpoint(LAMBOTSetpoint setpoint)
        {
            _targetElevatorHeight = setpoint.elevatorHeight;
            _targetEndEffectorAngle = setpoint.endEffectorAngle;
        }

        // ---------------- Funnel (also climb-deploy) ----------------
        [Header("Funnel")]
        [SerializeField] private GenericJoint funnelPivot;
        [SerializeField] private PidConstants funnelPid;
        [Tooltip("Degrees, funnel stowed/intake position")]
        [SerializeField] private float funnelIntakeAngle; // TODO tune-in-MoSim
        [Tooltip("Degrees, funnel rotated out of the way for climb")]
        [SerializeField] private float funnelClimbAngle; // TODO tune-in-MoSim
        private float _targetFunnelAngle;

        // ---------------- Bar Climber ----------------
        [Header("Climber")]
        [SerializeField] private GenericJoint climberBar;
        [SerializeField] private PidConstants climberPid;
        [Tooltip("Degrees, bar stowed under frame")]
        [SerializeField] private float climberStowAngle; // TODO tune-in-MoSim
        [Tooltip("Degrees, bar extended out to catch the cage")]
        [SerializeField] private float climberPrepAngle; // TODO tune-in-MoSim
        [Tooltip("Degrees, bar pulled in, pulling the cage/robot up")]
        [SerializeField] private float climberClimbAngle; // TODO tune-in-MoSim
        private float _targetClimberAngle;

        // Once the climb sequence has pulled the cage in, scoring is disabled.
        private bool _climbLocked;

        // ---------------- Rollers / Wheels ----------------
        [Header("Intake / End Effector Wheels")]
        [SerializeField] private GenericAnimationJoint[] funnelIntakeWheels;
        [SerializeField] private GenericAnimationJoint[] endEffectorWheels;
        [SerializeField] private float intakeWheelSpeed = 1f; // TODO tune-in-MoSim
        [SerializeField] private float scoreWheelSpeed = 1f; // TODO tune-in-MoSim
        [SerializeField] private float l1WheelSpeed = 0.5f; // TODO tune-in-MoSim

        private void RunRollers(GenericAnimationJoint[] group, float speed)
        {
            if (group == null) return;
            foreach (var roller in group)
            {
                if (roller != null) roller.VelocityRoller(speed);
            }
        }

        // ---------------- Game Piece Controller (Coral) ----------------
        [Header("Coral Game Piece")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;

        // ---------------- Audio ----------------
        [Header("Robot Audio")]
        [SerializeField] private AudioSource funnelIntakeSource;   // looping funnel/intake whir
        [SerializeField] private AudioClip funnelIntakeClip;
        [SerializeField] private AudioSource scoreSource;          // looping end-effector score whir
        [SerializeField] private AudioClip scoreClip;
        [SerializeField] private AudioSource coralStallSource;     // looping held-piece stall
        [SerializeField] private AudioClip coralStallClip;
        [SerializeField] private AudioSource climberSource;        // looping climber motor
        [SerializeField] private AudioClip climberClip;
        [SerializeField] private AudioSource oneShotSource;        // one-shots (funnel clack, climb latch)
        [SerializeField] private AudioClip funnelClackClip;
        [SerializeField] private AudioClip climbLatchClip;

        private OverlapBoxBounds _coralClackDetector;
        [SerializeField] private Transform coralClackTrigger;
        private LayerMask _coralMask;
        private bool _canClack;

        private bool _prevClimbLocked;

        protected override void Start()
        {
            base.Start();

            // --- PID setup ---
            elevator.SetPid(elevatorPid);
            endEffectorPivot.SetPid(endEffectorPid);
            funnelPivot.SetPid(funnelPid);
            climberBar.SetPid(climberPid);

            // --- Initial targets ---
            SetSetpoint(stowSetpoint);
            _targetFunnelAngle = funnelIntakeAngle;
            _targetClimberAngle = climberStowAngle;
            _climbLocked = false;
            _prevClimbLocked = false;

            // --- Game piece controller setup ---
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _coralController.gamePieceStates = new[] { coralIntakeState, coralStowState };
            _coralController.intakes.Add(coralIntake);

            // --- Audio init ---
            if (funnelIntakeSource != null)
            {
                funnelIntakeSource.clip = funnelIntakeClip;
                funnelIntakeSource.loop = true;
                funnelIntakeSource.Stop();
            }
            if (scoreSource != null)
            {
                scoreSource.clip = scoreClip;
                scoreSource.loop = true;
                scoreSource.Stop();
            }
            if (coralStallSource != null)
            {
                coralStallSource.clip = coralStallClip;
                coralStallSource.loop = true;
                coralStallSource.Stop();
            }
            if (climberSource != null)
            {
                climberSource.clip = climberClip;
                climberSource.loop = true;
                climberSource.Stop();
            }

            // --- Funnel clack detector ---
            if (coralClackTrigger != null)
            {
                _coralClackDetector = new OverlapBoxBounds(coralClackTrigger);
            }
            _coralMask = LayerMask.GetMask("Coral");
            _canClack = true;
        }

        private void LateUpdate()
        {
            elevator.SetTarget(_targetElevatorHeight); // TODO tune-in-MoSim
            endEffectorPivot.UpdatePid(endEffectorPid);
            funnelPivot.UpdatePid(funnelPid);
            climberBar.UpdatePid(climberPid);
        }

        private void FixedUpdate()
        {
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stowSetpoint);
                    _targetFunnelAngle = funnelIntakeAngle;
                    if (!_climbLocked)
                    {
                        if (IntakeAction.IsPressed() && !_coralController.HasPiece())
                        {
                            _coralController.RequestIntake(coralIntake, true);
                            RunRollers(funnelIntakeWheels, intakeWheelSpeed);
                        }
                        else
                        {
                            _coralController.RequestIntake(coralIntake, false);
                            RunRollers(funnelIntakeWheels, 0f);
                        }
                    }
                    RunRollers(endEffectorWheels, 0f);
                    break;

                case ReefscapeSetpoints.Intake:
                    SetSetpoint(intakeSetpoint);
                    _targetFunnelAngle = funnelIntakeAngle;
                    if (!_climbLocked && !_coralController.HasPiece())
                    {
                        _coralController.RequestIntake(coralIntake, true);
                        RunRollers(funnelIntakeWheels, intakeWheelSpeed);
                        RunRollers(endEffectorWheels, intakeWheelSpeed);
                    }
                    else
                    {
                        _coralController.RequestIntake(coralIntake, false);
                        RunRollers(funnelIntakeWheels, 0f);
                        RunRollers(endEffectorWheels, 0f);
                        if (_coralController.HasPiece())
                        {
                            _coralController.SetTargetState(coralStowState);
                        }
                    }
                    break;

                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1Setpoint);
                    if (!_climbLocked && OuttakeAction.IsPressed() && _coralController.atTarget)
                    {
                        RunRollers(endEffectorWheels, l1WheelSpeed);
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0f, 0f, 1f));
                    }
                    else
                    {
                        RunRollers(endEffectorWheels, 0f);
                    }
                    break;

                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2Setpoint);
                    if (!_climbLocked && OuttakeAction.IsPressed() && _coralController.atTarget)
                    {
                        RunRollers(endEffectorWheels, scoreWheelSpeed);
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0f, 0f, 1f));
                    }
                    else
                    {
                        RunRollers(endEffectorWheels, 0f);
                    }
                    break;

                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3Setpoint);
                    if (!_climbLocked && OuttakeAction.IsPressed() && _coralController.atTarget)
                    {
                        RunRollers(endEffectorWheels, scoreWheelSpeed);
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0f, 0f, 1f));
                    }
                    else
                    {
                        RunRollers(endEffectorWheels, 0f);
                    }
                    break;

                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4Setpoint);
                    if (!_climbLocked && OuttakeAction.IsPressed() && _coralController.atTarget)
                    {
                        RunRollers(endEffectorWheels, scoreWheelSpeed);
                        // Gentle release on L4 so the piece doesn't rocket off the branch.
                        _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0f, 0f, 1f), 0.35f, scoreWheelSpeed);
                    }
                    else
                    {
                        RunRollers(endEffectorWheels, 0f);
                    }
                    break;

                case ReefscapeSetpoints.Place:
                    // Generic place: hold current pose, allow outtake.
                    if (!_climbLocked && OuttakeAction.IsPressed() && _coralController.atTarget)
                    {
                        RunRollers(endEffectorWheels, scoreWheelSpeed);
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0f, 0f, 1f));
                    }
                    else
                    {
                        RunRollers(endEffectorWheels, 0f);
                    }
                    break;

                case ReefscapeSetpoints.Climb:
                    // Funnel rotates out of the way, elevator/EE tuck down, bar deploys.
                    SetSetpoint(stowSetpoint);
                    _targetFunnelAngle = funnelClimbAngle;
                    DriveController.SetDriveMp(0.5f);
                    RunRollers(funnelIntakeWheels, 0f);
                    RunRollers(endEffectorWheels, 0f);

                    if (!Utils.InAngularRange(funnelPivot.GetSingleAxisAngle(JointAxis.X), funnelClimbAngle, 3f))
                    {
                        // Funnel still swinging clear; hold the bar stowed.
                        _targetClimberAngle = climberStowAngle;
                    }
                    else
                    {
                        _targetClimberAngle = climberPrepAngle;
                    }

                    // Driver pulls the bar in to hook and drag the cage into the robot.
                    if (IntakeAction.IsPressed())
                    {
                        _targetClimberAngle = climberClimbAngle;
                    }

                    if (Utils.InAngularRange(climberBar.GetSingleAxisAngle(JointAxis.X), climberClimbAngle, 3f))
                    {
                        // Cage is pulled in — robot can no longer score.
                        _climbLocked = true;
                    }
                    break;

                case ReefscapeSetpoints.Climbed:
                    _targetClimberAngle = climberClimbAngle;
                    _targetFunnelAngle = funnelClimbAngle;
                    RunRollers(funnelIntakeWheels, 0f);
                    RunRollers(endEffectorWheels, 0f);
                    _climbLocked = true;
                    break;

                default:
                    SetSetpoint(stowSetpoint);
                    RunRollers(endEffectorWheels, 0f);
                    RunRollers(funnelIntakeWheels, 0f);
                    break;
            }

            climberBar.SetTargetAngle(_targetClimberAngle).withAxis(JointAxis.X); // TODO tune-in-MoSim
            funnelPivot.SetTargetAngle(_targetFunnelAngle).withAxis(JointAxis.X); // TODO tune-in-MoSim
            endEffectorPivot.SetTargetAngle(_targetEndEffectorAngle).withAxis(JointAxis.X); // TODO tune-in-MoSim

            UpdateSetpoints();
            UpdateAudio();
        }

        private void UpdateAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (funnelIntakeSource != null && funnelIntakeSource.isPlaying) funnelIntakeSource.Stop();
                if (scoreSource != null && scoreSource.isPlaying) scoreSource.Stop();
                if (coralStallSource != null && coralStallSource.isPlaying) coralStallSource.Stop();
                if (climberSource != null && climberSource.isPlaying) climberSource.Stop();
                return; // ALWAYS silence first when disabled
            }

            // Funnel/intake whir: playing while actively intaking without a piece.
            bool intaking = !_climbLocked && IntakeAction.IsPressed() && !_coralController.HasPiece();
            if (intaking && funnelIntakeSource != null && !funnelIntakeSource.isPlaying) funnelIntakeSource.Play();
            else if (!intaking && funnelIntakeSource != null && funnelIntakeSource.isPlaying) funnelIntakeSource.Stop();

            // Score whir: playing while ejecting at a scoring setpoint.
            bool scoring = !_climbLocked && OuttakeAction.IsPressed() &&
                (CurrentSetpoint == ReefscapeSetpoints.Place || CurrentSetpoint == ReefscapeSetpoints.L1 ||
                 CurrentSetpoint == ReefscapeSetpoints.L2 || CurrentSetpoint == ReefscapeSetpoints.L3 ||
                 CurrentSetpoint == ReefscapeSetpoints.L4);
            if (scoring) { if (scoreSource != null && !scoreSource.isPlaying) scoreSource.Play(); }
            else { if (scoreSource != null && scoreSource.isPlaying) scoreSource.Stop(); }

            // Held-piece stall loop.
            bool holding = _coralController.HasPiece() && _coralController.atTarget;
            if (holding) { if (coralStallSource != null && !coralStallSource.isPlaying) coralStallSource.Play(); }
            else { if (coralStallSource != null && coralStallSource.isPlaying) coralStallSource.Stop(); }

            // Climber motor loop: playing while the bar is actively moving toward a target.
            bool climberMoving = (CurrentSetpoint == ReefscapeSetpoints.Climb) &&
                !Utils.InAngularRange(climberBar.GetSingleAxisAngle(JointAxis.X), _targetClimberAngle, 1f);
            if (climberMoving) { if (climberSource != null && !climberSource.isPlaying) climberSource.Play(); }
            else { if (climberSource != null && climberSource.isPlaying) climberSource.Stop(); }

            // One-shot: funnel "clack" when a coral piece is detected passing through.
            if (_coralClackDetector != null)
            {
                var hit = _coralClackDetector.OverlapBox(_coralMask);
                if (hit.Length > 0)
                {
                    if (_canClack && oneShotSource != null && funnelClackClip != null)
                    {
                        oneShotSource.PlayOneShot(funnelClackClip);
                        _canClack = false;
                    }
                }
                else
                {
                    _canClack = true;
                }
            }

            // One-shot: climb latch, fires the instant the cage gets pulled in.
            if (_climbLocked && !_prevClimbLocked && oneShotSource != null && climbLatchClip != null)
            {
                oneShotSource.PlayOneShot(climbLatchClip);
            }
            _prevClimbLocked = _climbLocked;
        }
    }
}