using System.Collections;
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
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using DriveController = RobotFramework.Controllers.Drivetrain.DriveController;

namespace Prefabs.Reefscape.Robots.Mods.Offverture._7421RMX
{
    public class Offverture: ReefscapeRobotBase
    {
        [Header("Joints")]
        [SerializeField] private GenericElevator elevator;

        [SerializeField] private GenericJoint arm;
        [SerializeField] private PidConstants armPid;
        
        [SerializeField] private GenericJoint intake;
        [SerializeField] private PidConstants intakePid;

        [SerializeField] private GenericJoint climber;
        [SerializeField] private PidConstants climberPid;

        [SerializeField] private DriveController driveController;

        private float _elevatorTargetHeight;
        private float _armTargetAngle;
        private float _intakeTargetAngle;
        private float _climberTargetAngle;
        
        [Header("Setpoints")]
        [SerializeField] private OffvertureSetpoint stow;
        [SerializeField] private OffvertureSetpoint stowAlgae;
        [SerializeField] private OffvertureSetpoint intakeOut;
        [SerializeField] private OffvertureSetpoint intakeOutAlgae;
        [SerializeField] private OffvertureSetpoint coralTransferring;
        
        [SerializeField] private OffvertureSetpoint l4Front;
        [SerializeField] private OffvertureSetpoint l4Back;
        [SerializeField] private OffvertureSetpoint l3Front;
        [SerializeField] private OffvertureSetpoint l3Back;
        [SerializeField] private OffvertureSetpoint l2Front;
        [SerializeField] private OffvertureSetpoint l2Back;

        [Header("Coral Place Setpoints (L2/L3/L4)")]
        [Tooltip("OvertureWorlds-style base+place pair: the setpoints above (l2Front/l2Back/etc.) are the base branch setpoints, and these are what the arm/elevator move to on the Place setpoint - same idea as OvertureWorlds' l2Place/l3Place/l4Place, but ignoring the L4Ready intermediate stage OvertureWorlds uses for L4 (Offverture goes base -> place directly for L2/L3/L4 alike).")]
        [SerializeField] private OffvertureSetpoint l2FrontPlace;
        [SerializeField] private OffvertureSetpoint l2BackPlace;
        [SerializeField] private OffvertureSetpoint l3FrontPlace;
        [SerializeField] private OffvertureSetpoint l3BackPlace;
        [SerializeField] private OffvertureSetpoint l4FrontPlace;
        [SerializeField] private OffvertureSetpoint l4BackPlace;

        [SerializeField] private OffvertureSetpoint l1;
        
        [SerializeField] private OffvertureSetpoint groundAlgae;
        [SerializeField] private OffvertureSetpoint lolli;
        [SerializeField] private OffvertureSetpoint lowFront;
        [SerializeField] private OffvertureSetpoint lowBack;
        [SerializeField] private OffvertureSetpoint highFront;
        [SerializeField] private OffvertureSetpoint highBack;
        [SerializeField] private OffvertureSetpoint process;
        [SerializeField] private OffvertureSetpoint barge1;
        [SerializeField] private OffvertureSetpoint barge2;

        [SerializeField] private OffvertureSetpoint climb;
        [SerializeField] private OffvertureSetpoint climbed;

        [Header("Arm/Elevator Sequencing")]
        [Tooltip("Seconds to wait between moving the arm and moving the elevator when heading to a coral scoring setpoint (L2/L3/L4). L1 and the intake->arm handoff (coralTransferring) each have their own delay below and ignore this one.")]
        [SerializeField] private float coralSetpointDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the elevator and moving the arm when returning from a coral setpoint back to stow/intake.")]
        [SerializeField] private float returningCoralDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the arm and moving the elevator when heading to an algae scoring setpoint (low/high reef algae, processor). Ground algae, the lollipop pickup, and barge are intentionally instant and ignore this delay.")]
        [SerializeField] private float algaeSetpointDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the elevator and moving the arm when returning from an algae setpoint back to stow/intake.")]
        [SerializeField] private float returningAlgaeDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the arm and moving the elevator when heading to coralTransferring (the intake->arm coral handoff). Split out from coralSetpointDelay so handoff timing can be tuned independently of L2/L3/L4. Placeholder matches the value coralSetpointDelay used for this before the split (0.25s).")]
        [Range(0f, 1f)]
        [SerializeField] private float handoffDelay = 0.25f;

        [Header("Climb Sequencing")]
        [Tooltip("Seconds to wait after commanding the climber and intake before raising the elevator for Climb.")]
        [SerializeField] private float afterClimberDelay = 0.25f;

        [Tooltip("Seconds to wait after moving the arm before raising the elevator for Climb.")]
        [FormerlySerializedAs("afterClimbElevDelay")]
        [SerializeField] private float afterArmDelay = 0.25f;

        // Setpoints that should move the arm and elevator together with no
        // sequencing delay at all: barge, ground algae, the lollipop pickup,
        // climb/climbed, and now L1 (L1 is instant so the setpoint delay never
        // affects it). These are applied directly every FixedUpdate instead of
        // through a coroutine, so tuning changes made to the setpoint asset while
        // the robot is already targeting it are picked up immediately.
        private enum SetpointDelayType
        {
            Instant,
            CoralSetpoint,
            ReturningCoral,
            AlgaeSetpoint,
            ReturningAlgae,
            Handoff,
            Climb
        }

        private OffvertureSetpoint _activeSequenceSetpoint;
        private Coroutine _setpointSequenceRoutine;
        
        [Header("Intake and Stow States")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake armCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState algaeStowState;
        
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
        
        [Header("Align")]
        [Tooltip("Owns every align offset asset (reef, LowerAlign for L1, and barge algae) and feeds the picked one into ReefscapeAutoAlign every FixedUpdate - see OverAlign.")]
        [SerializeField] private OverAlign overAlign;
        
        [Header("Roller Stuff")]
        [SerializeField] private GenericRoller intakeRoller;

        private bool intaking;

        private bool transferOnce = false;
        private bool intk = false;
        private bool transferring = false;
        private bool coralInPossesion = false;

        private bool l1once = false;

        private bool placed = false;
        
        private bool placeOnce = false;

        [SerializeField] private Collider[] scoop;

        private ReefscapeSetpoints nextLevel = ReefscapeSetpoints.Stow;

        [SerializeField] private GenericAnimationJoint[] intakeRollers;
        [SerializeField] private GenericAnimationJoint[] eeRollers;
        
        [Header("Algae Stall Audio")]
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallAudio;
        
        [Header("Intake and EE Rollers")]
        [SerializeField] private AudioSource intakeAudio;
        [SerializeField] private AudioSource eeAudio;
        [SerializeField] private AudioClip rollerAudio;
        
        protected override void Start()
        {
            base.Start();
            
            overAlign = gameObject.GetComponent<OverAlign>();
            
            arm.SetPid(armPid);
            intake.SetPid(intakePid);
            climber.SetPid(climberPid);

            _elevatorTargetHeight = 0;
            _armTargetAngle = 0;
            _intakeTargetAngle = 0;
            _climberTargetAngle = 0;
            
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                coralIntakeState,
                coralStowState
            };
            _coralController.intakes.Add(armCoralIntake);
            _coralController.intakes.Add(coralIntake);
            
            _algaeController.gamePieceStates = new[]
            {
                algaeStowState
            };
            _algaeController.intakes.Add(algaeIntake);
            
            algaeStallSource.clip = algaeStallAudio;
            algaeStallSource.loop = true;
            algaeStallSource.Stop();
            
            intakeAudio.clip = rollerAudio;
            intakeAudio.loop = true;
            intakeAudio.Stop();
            
            eeAudio.clip = rollerAudio;
            eeAudio.loop = true;
            eeAudio.Stop();
        }

        private void LateUpdate()
        {
            // GenericJoint PID controllers need to be stepped every frame.  The other
            // Overtures do this for both joints; Offverture previously only updated
            // the climber, so ES ARM changed _armTargetAngle without driving the arm.
            climber.UpdatePid(climberPid);
            arm.UpdatePid(armPid);
            intake.UpdatePid(intakePid);
        }

        private bool atSetpoint(OffvertureSetpoint stp)
        {
            return
                Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
                Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f) &&
                Utils.InAngularRange(intake.GetSingleAxisAngle(JointAxis.X), stp.intakeAngle, 2f);
        }
        
        public bool atSetpoint(OffvertureSetpoint stp, GenericJoint jnt)
        {
            return Utils.InAngularRange(jnt.GetSingleAxisAngle(JointAxis.X), stp.elevatorHeight, 2f);
        }
        
        public bool atSetpoint(OffvertureSetpoint stp, GenericElevator elv)
            {
                return Utils.InRange(elv.GetElevatorHeight(), stp.elevatorHeight, 2f);
            }
        
        public bool armAtTargetAngle()
        {
            return Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 2f);
        }


        private void setIntakeIntake()
        {
            intakeRoller.SetAngularVelocity(4000);
            foreach (var col in scoop)
            {
                col.enabled = true;
            }
        }

        private void setIntakeOuttaking()
        {
            intakeRoller.SetAngularVelocity(-3000);
            foreach (var col in scoop)
            {
                col.enabled = false;
            }
        }
        
        private void setIntakeOuttaking(float s)
        {
            intakeRoller.SetAngularVelocity(-s);
            foreach (var col in scoop)
            {
                col.enabled = false;
            }
        }

        private void FixedUpdate()
        {
            RunAudio();

            if (CurrentSetpoint is ReefscapeSetpoints.Climb or ReefscapeSetpoints.Climbed)
            {
                DriveController.SetDriveMp(0.875f);
            }
            else
            {
                DriveController.SetDriveMp(1);
            }
            
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();
            bool intakeHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralIntakeState.stateNum;
            bool armHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum;
            
            _algaeController.SetTargetState(algaeStowState);
            
            if (!IntakeAction.IsPressed())
            {
                _algaeController.RequestIntake(algaeIntake, false);
                _coralController.RequestIntake(coralIntake, false);
                transferOnce = false;
            }

            if (intakeHasCoral || transferring)
            {
                coralInPossesion = true;
            }

            if (hasCoral && CurrentSetpoint != ReefscapeSetpoints.L1 && CurrentIntakeMode != ReefscapeIntakeMode.L1)
            {
                setIntakeOuttaking(0);
            }

            if (CurrentSetpoint == ReefscapeSetpoints.L1 || LastSetpoint == ReefscapeSetpoints.L1 && CurrentSetpoint != ReefscapeSetpoints.Intake && CurrentSetpoint != ReefscapeSetpoints.Stow)
            {
                setIntakeOuttaking();
            }
            else
            {
                setIntakeIntake();
            }

            if (armHasCoral)
            {
                _coralController.RequestIntake(armCoralIntake, false);
            }

            if (CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                if (intakeHasCoral && atSetpoint(stow))
                {
                    transferring = true;
                } 
                else if (armHasCoral && transferring)
                {
                    transferring = false;
                    SetState(nextLevel);
                }
            }
        
            if (CurrentSetpoint != ReefscapeSetpoints.Place) {
                placeOnce = false;
            }

            if (atSetpoint(coralTransferring) && !transferOnce && CurrentIntakeMode == ReefscapeIntakeMode.Normal && transferring)
            {
                transferToArm();
                transferOnce = true;
                intk = false;
            }
            else if (!hasCoral && !atSetpoint(l1))
            {
                setIntakeIntake();
            }

            if (atSetpoint(coralTransferring) && !(CurrentIntakeMode == ReefscapeIntakeMode.L1 || CurrentSetpoint == ReefscapeSetpoints.L1))
            {
                setIntakeRollers(-20);
                setEndEffectorRollers(20);
            } else if (atSetpoint(coralTransferring))
            {
                setIntakeRollers(20);
                setEndEffectorRollers(-20);
            }

            if (CurrentIntakeMode == ReefscapeIntakeMode.Normal && intk)
            {
                _coralController.SetTargetState(coralIntakeState);
            }
            else if (CurrentIntakeMode == ReefscapeIntakeMode.Normal && CurrentSetpoint !=  ReefscapeSetpoints.L1 && !hasCoral)
            {
                _coralController.SetTargetState(coralStowState);
            }
            else if (CurrentIntakeMode == ReefscapeIntakeMode.L1 || CurrentSetpoint ==  ReefscapeSetpoints.L1 && !hasCoral)
            {
                _coralController.SetTargetState(coralIntakeState);
            }

            if (transferring)
            {
                if (L4Action.IsPressed())
                {
                    nextLevel = ReefscapeSetpoints.L4;
                }
                if (L3Action.IsPressed())
                {
                    nextLevel = ReefscapeSetpoints.L3;
                }
                if (L2Action.IsPressed())
                {
                    nextLevel = ReefscapeSetpoints.L2;
                }

            }

            if (CurrentSetpoint != ReefscapeSetpoints.L1)
            {
                l1once = false;
            }

            if (CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                placed = false;
            }

            if (!IntakeAction.IsPressed() && !OuttakeAction.IsPressed())
            {
                intakeRollersStop();
                endEffectorRollersStop();
            }

            if (CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                intakeRollersStop();
                endEffectorRollersStop();
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (CurrentIntakeMode == ReefscapeIntakeMode.Normal && (intakeHasCoral || coralInPossesion) && transferring)
                    {
                        SetSetpoint(coralTransferring);
                    }
                    else
                    {
                        SetSetpoint(hasAlgae ? stowAlgae : stow);
                    }

                    if (!transferring)
                    {
                        intakeRollersStop();
                        endEffectorRollersStop();
                    }

                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, !transferring && atSetpoint(stow));
                    break;
                case ReefscapeSetpoints.Intake:
                    if ((CurrentRobotMode == ReefscapeRobotMode.Coral ||
                        hasAlgae) && !hasCoral)
                    {
                        // Coral intake must keep the coral intake pose even while an
                        // algae piece is held.  Selecting intakeOutAlgae here sent
                        // the floor intake to its 180-degree algae pose.
                        SetSetpoint(CurrentRobotMode == ReefscapeRobotMode.Coral ? intakeOut : intakeOutAlgae);
                    }

                    if (CurrentRobotMode == ReefscapeRobotMode.Algae && !armHasCoral && !hasAlgae)
                    {
                        SetSetpoint(groundAlgae);
                        _coralController.RequestIntake(coralIntake, false);
                        setIntakeOuttaking(0);
                    }
                    
                    _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && !armHasCoral && IntakeAction.IsPressed());
                    if (!hasCoral && !atSetpoint(coralTransferring) && !transferOnce)
                    {
                        _coralController.RequestIntake(coralIntake, !transferring);
                        intk = true;
                    }

                    if (atSetpoint(intakeOut))
                    {
                        setIntakeRollers(50);
                    } 
                    else if (atSetpoint(intakeOutAlgae))
                    {
                        setIntakeRollers(50);
                        setEndEffectorRollers(50);
                    }
                    else
                    {
                        setEndEffectorRollers(50);
                    }

                    break;
                case ReefscapeSetpoints.Place:
                    _coralController.RequestIntake(coralIntake, false);
                    _coralController.RequestIntake(armCoralIntake, false);

                    if (!placeOnce)
                    {
                        if (LastSetpoint == ReefscapeSetpoints.L4 || LastSetpoint == ReefscapeSetpoints.L3 ||
                            LastSetpoint == ReefscapeSetpoints.L2)
                        {
                            PlaceBranch(GetPlaceSetpointByLevel());
                            setEndEffectorRollers(-20);
                        }
                        else
                        {
                            PlacePiece();
                        }
                    }

                    nextLevel = ReefscapeSetpoints.Stow;
                    break;
                case ReefscapeSetpoints.L1:
                    if (intakeHasCoral && (CurrentIntakeMode == ReefscapeIntakeMode.L1 || l1once))
                    {
                        SetSetpoint(l1);
                    }
                    else
                    {
                        if (transferring)
                        {
                            nextLevel = ReefscapeSetpoints.L1;
                            SetState(ReefscapeSetpoints.L1);
                        }
                        else
                        {
                            if (atSetpoint(coralTransferring))
                            {
                                if (armHasCoral && !l1once)
                                {
                                    //_coralController.RequestIntake(armCoralIntake, false);
                                    //_coralController.ReleaseGamePieceWithForce(new Vector3(0, 3, 0));
                                    l1once = true;
                                }

                            }
                            else
                            {
                                SetSetpoint(coralTransferring);
                            }

                            if (l1once)
                            {
                                //_coralController.RequestIntake(armCoralIntake, false);
                                setIntakeIntake();
                                _coralController.SetTargetState(coralIntakeState);
                                //_coralController.RequestIntake(coralIntake, true);
                            }
                        }
                    } 
                    // _coralController.RequestIntake(coralIntake, true);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(lolli);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    if (IntakeAction.IsPressed())
                    {
                        setEndEffectorRollers(50);
                    }
                    else
                    {
                        setEndEffectorRollers(0);
                    }
                    break;
                case ReefscapeSetpoints.L2:
                    if (armHasCoral)
                    {
                        SetSetpoint(!FacingReef ? l2Front : l2Back);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L2;
                    }
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    if (transferring || atSetpoint(coralTransferring)) 
                    {
                        SetState(ReefscapeSetpoints.L2);
                    } else {
                        SetSetpoint(!FacingReef ? lowFront : lowBack);
                        _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                        _coralController.RequestIntake(coralIntake, false);
                        if (IntakeAction.IsPressed())
                        {
                            setEndEffectorRollers(50);
                        }
                        else
                        {
                            setEndEffectorRollers(0);
                        }
                    }
                    break;
                case ReefscapeSetpoints.L3:
                    if (armHasCoral)
                    {
                        SetSetpoint(!FacingReef ? l3Front : l3Back);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L3;
                    }
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    if (transferring || atSetpoint(coralTransferring)) 
                    {
                        SetState(ReefscapeSetpoints.L2);
                    }
                    else
                    {
                        SetSetpoint(!FacingReef ? highFront : highBack);
                        _algaeController.RequestIntake(algaeIntake,
                            IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                        _coralController.RequestIntake(coralIntake, false);
                        if (IntakeAction.IsPressed())
                        {
                            setEndEffectorRollers(50);
                        }
                        else
                        {
                            setEndEffectorRollers(0);
                        }
                    }

                    break;
                case ReefscapeSetpoints.L4:
                    if (armHasCoral)
                    {
                        SetSetpoint(!FacingReef ? l4Front : l4Back);
                    }
                    else if (hasAlgae)
                    {
                        SetState(ReefscapeSetpoints.Barge);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L4;
                    }
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(process);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(FacingReef ? barge1 : barge2);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climb);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    break;
            }

            if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
            {
                if (L4Action.IsPressed())
                {
                    nextLevel =  ReefscapeSetpoints.L4;
                    IntakeModeToggleAction.Enable();
                    IntakeModeToggleAction.Disable();
                    SetState(ReefscapeSetpoints.Stow);
                }
                else if (L3Action.IsPressed())
                {
                    nextLevel =  ReefscapeSetpoints.L3;
                    IntakeModeToggleAction.Enable();
                    IntakeModeToggleAction.Disable();
                    SetState(ReefscapeSetpoints.Stow);
                }
                else if (L2Action.IsPressed())
                {
                    nextLevel =  ReefscapeSetpoints.L2;
                    IntakeModeToggleAction.Enable();
                    IntakeModeToggleAction.Disable();
                    SetState(ReefscapeSetpoints.Stow);
                }
            }

            if (transferring)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
            }

            if (placeOnce)
            {
                intakeRollersStop();
                endEffectorRollersStop();
            }

            
            ApplySetpoints();
        }

        private void transferToArm()
        {
            if (_coralController.currentStateNum == coralIntakeState.stateNum && _coralController.atTarget)
            {
                //_coralController.ReleaseGamePieceWithForce(new Vector3(0, -3, 0));
                _coralController.SetTargetState(coralStowState);
            }

            //_coralController.RequestIntake(armCoralIntake, transferring); //atSetpoint(coralTransferring));
        }
        

        private int GetLevelByState()
        {
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
            }
            
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
            }

            return 0;
        }

        // OvertureWorlds-style base+place lookup: base setpoints (l2Front/l3Front/l4Front/etc.) are used
        // on the way up, and this returns the matching *Place setpoint for the Place case - base then
        // place, ignoring OvertureWorlds' L4Ready intermediate stage for L4, same as L2/L3. L1 places
        // through its own existing PlacePiece() flow, so it isn't handled here.
        private OffvertureSetpoint GetPlaceSetpointByLevel()
        {
            switch (GetLevelByState())
            {
                case 2:
                    return !FacingReef ? l2FrontPlace : l2BackPlace;
                case 3:
                    return !FacingReef ? l3FrontPlace : l3BackPlace;
                case 4:
                    return !FacingReef ? l4FrontPlace : l4BackPlace;
            }

            return null;
        }

        private void PlacePiece()
        {
			if (!placeOnce) {
            	if (_algaeController.atTarget)
            	{
                	_algaeController.ReleaseGamePieceWithForce(atSetpoint(barge1, elevator) ? new Vector3(0, 4, 0) : new Vector3(0, 2, 0));
                    setEndEffectorRollers(-20);
            	}
            	else if (CurrentIntakeMode == ReefscapeIntakeMode.L1 || LastSetpoint == ReefscapeSetpoints.L1)
            	{
                    _coralController.ReleaseGamePieceWithForce(new Vector3(4, .6f, 0));
//                    _coralController.ReleaseGamePieceWithForce(new Vector3(-1.5f, -3.8f, 0));
                	coralInPossesion = false;
                    setIntakeRollers(20);
            	}
            	else
            	{
                	_coralController.ReleaseGamePieceWithForce(new Vector3(0, 0.5f, FacingReef ? 0.5f : -0.5f));
                	// _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
                	coralInPossesion = false;
                    if (_coralController.atTarget && _coralController.currentStateNum == coralIntakeState.stateNum)
                    {
                        setIntakeRollers(20);
                    }
                    else
                    {
                        setEndEffectorRollers(-20);
                    }
            	}
			}

            placeOnce = true;
        }

        // OvertureWorlds-style placing: instead of computing a lowered arm/elevator offset from
        // ElevatorLowerHeight/ArmLowerHeight, this now just drives straight to the dedicated *Place
        // setpoint asset (l2FrontPlace/l2BackPlace/l3FrontPlace/l3BackPlace/l4FrontPlace/l4BackPlace),
        // same as OvertureWorlds' SetSetpoint(l2Place)/l3Place/l4Place - base setpoint then place
        // setpoint, no intermediate L4Ready stage. The release-force-by-level tuning is unchanged.
        // If a *Place asset hasn't been assigned yet in the Inspector, this falls back to releasing at
        // the current (base branch) position instead of silently doing nothing.
        //
        // placeOnce = true is set at the end once the release actually fires (after armAtTargetAngle()
        // clears), same as PlacePiece() already does - without it the caller's `if (!placeOnce)` guard in
        // the Place case never latches, so this kept re-running and calling ReleaseGamePieceWithForce every
        // FixedUpdate for as long as CurrentSetpoint stayed Place, instead of a single release. That's why
        // L2/L3/L4 placing was broken while L1/algae placing (through PlacePiece, which already set the
        // flag) worked fine.
        private void PlaceBranch(OffvertureSetpoint placeSetpoint)
        {
            if (placeSetpoint != null)
            {
                SetSetpoint(placeSetpoint);
            }

            if (!armAtTargetAngle())
            {
                return;
            }

            switch (GetLevelByState())
            {
                case 4:
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, .5f, !FacingReef ? 2 : -2));
                    coralInPossesion = false;
                    break;
                case 3:
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1, !FacingReef ? 1 : -1));
                    coralInPossesion = false;
                    break;
                case 2:
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, .7f, !FacingReef ? 1 : -1));
                    coralInPossesion = false;
                    break;
            }

            placeOnce = true;
        }

        private void setIntakeRollers(float speed)
        {
            for(int i = 0; i < intakeRollers.Length; i++)
            {
                intakeRollers[i].VelocityRoller(5 * speed);
            }
        }

        private void intakeRollersStop()
        {
            setIntakeRollers(0);
        }
        
        private void setEndEffectorRollers(float speed)
        {
            for(int i = 0; i < eeRollers.Length; i++)
            {
                eeRollers[i].VelocityRoller(5 * speed);
            }
        }

        private void endEffectorRollersStop()
        {
            setEndEffectorRollers(0);
        }

        private void RunAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (intakeAudio.isPlaying || eeAudio.isPlaying || algaeStallSource.isPlaying)
                {
                    intakeAudio.Stop();
                    eeAudio.Stop();
                    algaeStallSource.Stop();
                }

                return;
            }
            if (_algaeController.atTarget && !algaeStallSource.isPlaying)
            {
                algaeStallSource.Play();
            }

            if (!_algaeController.atTarget)
            {
                algaeStallSource.Stop();
            }

            
            if (IntakeAction.IsPressed())
            {
                if (CurrentSetpoint == ReefscapeSetpoints.HighAlgae || CurrentSetpoint == ReefscapeSetpoints.LowAlgae ||
                    CurrentSetpoint == ReefscapeSetpoints.Stack ||
                    (CurrentSetpoint == ReefscapeSetpoints.Intake && CurrentRobotMode == ReefscapeRobotMode.Algae))
                {
                    if (!eeAudio.isPlaying)
                    {
                        eeAudio.Play();
                    }
                }
                
                else if (CurrentSetpoint == ReefscapeSetpoints.Intake && !atSetpoint(stow, intake))
                {
                    if (!intakeAudio.isPlaying)
                    {
                        intakeAudio.Play();
                    }
                }
            }
            else if (OuttakeAction.IsPressed())
            {
                if (_coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum)
                {
                    if (!eeAudio.isPlaying)
                    {
                        eeAudio.Play();
                    }
                }
                
                else if (_algaeController.atTarget)
                {
                    if (!eeAudio.isPlaying)
                    {
                        eeAudio.Play();
                    }
                }
                
                else if (_coralController.atTarget && _coralController.currentStateNum == coralIntakeState.stateNum)
                {
                    if (!intakeAudio.isPlaying)
                    {
                        intakeAudio.Play();
                    }
                }
            } 
            else if (transferring)
            {
                if (!eeAudio.isPlaying)
                {
                    eeAudio.Play();
                }
                if (!intakeAudio.isPlaying)
                {
                    intakeAudio.Play();
                }
            }
            else if (!OuttakeAction.IsPressed() && !OuttakeAction.IsPressed() && !transferring)
            {
                if (intakeAudio.isPlaying)
                {
                    intakeAudio.Stop();
                }
                
                if (eeAudio.isPlaying)
                {
                    eeAudio.Stop();
                }
            }
        }

        private SetpointDelayType GetDelayType(OffvertureSetpoint setpoint)
        {
            if (setpoint == stow || setpoint == intakeOut)
            {
                return SetpointDelayType.ReturningCoral;
            }

            if (setpoint == stowAlgae || setpoint == intakeOutAlgae)
            {
                return SetpointDelayType.ReturningAlgae;
            }

            if (setpoint == coralTransferring)
            {
                return SetpointDelayType.Handoff;
            }

            if (setpoint == climb)
            {
                return SetpointDelayType.Climb;
            }

            if (setpoint == l4Front || setpoint == l4Back ||
                setpoint == l3Front || setpoint == l3Back ||
                setpoint == l2Front || setpoint == l2Back)
            {
                return SetpointDelayType.CoralSetpoint;
            }

            if (setpoint == lowFront || setpoint == lowBack ||
                setpoint == highFront || setpoint == highBack ||
                setpoint == process)
            {
                return SetpointDelayType.AlgaeSetpoint;
            }

            // barge1, barge2, groundAlgae, lolli, climb, climbed, l1: no sequencing delay - l1 is
            // instant so the setpoint delay never affects it, unlike L2/L3/L4.
            return SetpointDelayType.Instant;
        }

        private void SetSetpoint(OffvertureSetpoint setpoint)
        {
            // Ya estamos apuntando exactamente a este setpoint - no hay nada que hacer.
            if (isCurrentSetpoint(setpoint))
            {
                return;
            }

            // El intake y el climber no forman parte de la secuencia brazo/elevador - se aplican de inmediato.
            _intakeTargetAngle = setpoint.intakeAngle;
            _climberTargetAngle = setpoint.climberAngle;

            var delayType = GetDelayType(setpoint);

            // Barge, ground algae, lollipop, y climb/climbed: sin delay. Se aplican
            // directamente cada FixedUpdate (sin pasar por _activeSequenceSetpoint)
            // para que cualquier cambio en los valores del setpoint (tuning en vivo)
            // se refleje de inmediato en vez de quedarse pegado al primer valor leído.
            if (delayType == SetpointDelayType.Instant)
            {
                if (_setpointSequenceRoutine != null)
                {
                    StopCoroutine(_setpointSequenceRoutine);
                    _setpointSequenceRoutine = null;
                }

                _activeSequenceSetpoint = setpoint;
                _armTargetAngle = setpoint.armAngle;
                _elevatorTargetHeight = setpoint.elevatorHeight;
                return;
            }

            // Ya hay una secuencia corriendo hacia este mismo setpoint - déjala terminar
            // en vez de reiniciarla en cada FixedUpdate.
            if (_activeSequenceSetpoint == setpoint)
            {
                return;
            }

            _activeSequenceSetpoint = setpoint;

            if (_setpointSequenceRoutine != null)
            {
                StopCoroutine(_setpointSequenceRoutine);
            }

            switch (delayType)
            {
                case SetpointDelayType.CoralSetpoint:
                    _setpointSequenceRoutine = StartCoroutine(RaiseToSetpointSequence(setpoint, coralSetpointDelay));
                    break;
                case SetpointDelayType.ReturningCoral:
                    _setpointSequenceRoutine = StartCoroutine(ReturnHomeSequence(setpoint, returningCoralDelay));
                    break;
                case SetpointDelayType.AlgaeSetpoint:
                    _setpointSequenceRoutine = StartCoroutine(RaiseToSetpointSequence(setpoint, algaeSetpointDelay));
                    break;
                case SetpointDelayType.ReturningAlgae:
                    _setpointSequenceRoutine = StartCoroutine(ReturnHomeSequence(setpoint, returningAlgaeDelay));
                    break;
                case SetpointDelayType.Handoff:
                    _setpointSequenceRoutine = StartCoroutine(RaiseToSetpointSequence(setpoint, handoffDelay));
                    break;
                case SetpointDelayType.Climb:
                    _setpointSequenceRoutine = StartCoroutine(ClimbSequence(setpoint));
                    break;
            }
        }

        // Subiendo a un setpoint: primero gira el brazo, espera, y luego sube el elevador.
        private IEnumerator RaiseToSetpointSequence(OffvertureSetpoint setpoint, float delay)
        {
            _armTargetAngle = setpoint.armAngle;
            yield return new WaitForSeconds(delay);
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _setpointSequenceRoutine = null;
        }

        // Regresando a stow/intake: primero baja el elevador, espera, y luego regresa el brazo.
        private IEnumerator ReturnHomeSequence(OffvertureSetpoint setpoint, float delay)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            yield return new WaitForSeconds(delay);
            _armTargetAngle = setpoint.armAngle;
            _setpointSequenceRoutine = null;
        }

        // SetSetpoint applies climber and intake targets before starting this routine.
        // Climb then moves the arm and finally raises the elevator after the requested delays.
        private IEnumerator ClimbSequence(OffvertureSetpoint setpoint)
        {
            yield return new WaitForSeconds(afterClimberDelay);
            _armTargetAngle = setpoint.armAngle;
            yield return new WaitForSeconds(afterArmDelay);
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _setpointSequenceRoutine = null;
        }

        private bool isCurrentSetpoint(OffvertureSetpoint setpoint)
        {
            return
                _elevatorTargetHeight == setpoint.elevatorHeight &&
                _armTargetAngle == setpoint.armAngle &&
                _intakeTargetAngle == setpoint.intakeAngle &&
                _climberTargetAngle == setpoint.climberAngle;
        }
        
        private void ApplySetpoints() 
        {
            elevator.SetTarget(_elevatorTargetHeight);
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(
                (((CurrentRobotMode == ReefscapeRobotMode.Algae &&
                   _algaeController.atTarget) || 
                  CurrentSetpoint == ReefscapeSetpoints.HighAlgae ||
                  CurrentSetpoint == ReefscapeSetpoints.LowAlgae || 
                  LastSetpoint == ReefscapeSetpoints.HighAlgae ||
                  LastSetpoint == ReefscapeSetpoints.LowAlgae || 
                  CurrentSetpoint == ReefscapeSetpoints.Stack ||
                  LastSetpoint == ReefscapeSetpoints.Stack ||
                  LastSetpoint == ReefscapeSetpoints.Place) || 
                 _algaeController.atTarget)
                    ? 180
                    : (!FacingReef ? 150 : 210));
            intake.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X).noWrap(-90);
            climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X).noWrap(180f);
        }
        
    }
    
}
