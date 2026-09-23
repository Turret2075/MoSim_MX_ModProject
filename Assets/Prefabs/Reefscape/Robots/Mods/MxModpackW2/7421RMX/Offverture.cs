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
        [Tooltip("Seconds to wait between moving the arm and moving the elevator when heading to a coral scoring setpoint (L2/L3/L4) or the lollipop coral pickup (lollipopCoral) - lollipopCoral now uses the same arm-first-then-elevator sequencing as the reef branches instead of moving instantly. L1 and the intake->arm handoff (coralTransferring) each have their own delay below and ignore this one.")]
        [SerializeField] private float coralSetpointDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the elevator and moving the arm when returning from a coral setpoint back to stow/intake.")]
        [SerializeField] private float returningCoralDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the arm and moving the elevator when heading to an algae scoring setpoint (low/high reef algae, processor), and also used for ground algae / the algae lollipop pickup (lolli) when coming from stow - see GetDelayType. Barge is intentionally instant and ignores this delay.")]
        [SerializeField] private float algaeSetpointDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the elevator and moving the arm when returning from an algae setpoint back to stow/intake.")]
        [SerializeField] private float returningAlgaeDelay = 0.25f;

        [Tooltip("Seconds to wait between moving the arm and moving the elevator when heading to coralTransferring (the intake->arm coral handoff). Split out from coralSetpointDelay so handoff timing can be tuned independently of L2/L3/L4. Placeholder matches the value coralSetpointDelay used for this before the split (0.25s).")]
        [Range(0f, 1f)]
        [SerializeField] private float handoffDelay = 0.25f;

        [Header("Climb Sequencing")]
        [Tooltip("Seconds to wait after commanding the intake before moving the arm for Climb. (Field name kept for the Inspector - the climber itself no longer moves at the start of this delay: it now moves last, after the elevator, matching the real robot's intake -> arm -> elevator -> climber order.)")]
        [SerializeField] private float afterClimberDelay = 0.25f;

        [Tooltip("Seconds to wait after moving the arm before raising the elevator for Climb. The climber then opens immediately after the elevator target is applied.")]
        [FormerlySerializedAs("afterClimbElevDelay")]
        [SerializeField] private float afterArmDelay = 0.25f;

        // Setpoints that should move the arm and elevator together with no
        // sequencing delay at all: barge, climbed, and L1 (L1 is instant so the
        // setpoint delay never affects it). These are applied directly every FixedUpdate
        // instead of through a coroutine, so tuning changes made to the setpoint asset while
        // the robot is already targeting it are picked up immediately. Ground algae, the algae
        // lollipop (lolli) and the coral lollipop (lollipopCoral) used to be in this instant
        // group too, but now go through a coroutine with a delay that depends on LastSetpoint -
        // see GetDelayType.

        [Header("Center of Mass")]
        [SerializeField] private bool addCenterOfMassX;
        [SerializeField] private bool addCenterOfMassZ;
        [SerializeField] private float climbedCenterOfMassX;
        [SerializeField] private float climbedCenterOfMassZ;
        private Rigidbody _mainRb;
        private Vector3 _originalCenterOfMass;
        private bool _isCgShifted;

        
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

        // Ported from the real robot: L2Command/L3Command/L4Command read AlignManager::getHeading()
        // once, at the moment the button is pressed (BeforeStarting(RunOnce(alignManager.setHeading))),
        // and ReefFrontToReefPosition/ReefBackToReefPosition keep that same Front/Back heading when
        // switching directly between branches (L2<->L3<->L4) without going back through Sustained/Stow.
        // Previously this sim re-read FacingReef every FixedUpdate while CurrentSetpoint was L2/L3/L4,
        // so the target branch (Front vs Back) could flip mid-transit if the robot rotated slightly
        // during approach. _lockedFacingReef captures FacingReef once on entering the reef branch from
        // outside it (see GetLockedFacingReef()) and is reused for as long as the robot stays on
        // L2/L3/L4, matching the real robot's behavior.
        private bool _reefHeadingLocked;
        private bool _lockedFacingReef;

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
        
        [Tooltip("Ported from Bulldogs' lollipopCoral: a coral picked directly off a standalone post rather than the floor or a branch, used under the same Stack setpoint the algae lollipop (lolli) already uses, but only while CurrentRobotMode is Coral. HEADS UP: Bulldogs' arm swings +90 degrees to reach its lollipop coral; Offverture's arm has to swing -90 for the same reach because the climber sits in the way of the +90 sweep. Don't copy Bulldogs' armAngle sign onto this asset - it needs the opposite one.")]
        [SerializeField] private OffvertureSetpoint lollipopCoral;

        [Tooltip("OPTIONAL / no longer used for the pickup - the lollipop coral is now taken with armCoralIntake. Dedicated game-piece intake collider for the lollipop coral pickup (separate from the floor intake and armCoralIntake, same as Bulldogs' lollipopCoralIntake), since the post-mounted piece sits somewhere neither of those already covers.")]
        [SerializeField] private ReefscapeGamePieceIntake lollipopCoralIntake;

        [Header("Lollipop Coral Vision")]
        [Tooltip("Box trigger used to spot a lollipop coral and gently steer the robot onto it while IntakeAction is held on Stack in Coral mode - ported from Bulldogs' lollipopIntakeVision/RunIntakeVision().")]
        [SerializeField] private BoxCollider lollipopIntakeVision;
        private OverlapBoxBounds _lollipopVisionDetect;
        private Collider[] _lollipopVisionColliders;
        private LayerMask _coralVisionMask;

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
            if (lollipopCoralIntake != null && lollipopCoralIntake != armCoralIntake)
            {
                _coralController.intakes.Add(lollipopCoralIntake);
            }
            
            _algaeController.gamePieceStates = new[]
            {
                algaeStowState
            };
            _algaeController.intakes.Add(algaeIntake);

            _lollipopVisionDetect = new OverlapBoxBounds(lollipopIntakeVision);
            _lollipopVisionColliders = new Collider[6];
            _coralVisionMask = LayerMask.GetMask("Coral");
            
            algaeStallSource.clip = algaeStallAudio;
            algaeStallSource.loop = true;
            algaeStallSource.Stop();
            
            intakeAudio.clip = rollerAudio;
            intakeAudio.loop = true;
            intakeAudio.Stop();
            
            eeAudio.clip = rollerAudio;
            eeAudio.loop = true;
            eeAudio.Stop();

            _mainRb = gameObject.GetComponent<Rigidbody>();
            _isCgShifted = false;
            if (_mainRb != null)
            {
                _originalCenterOfMass = _mainRb.centerOfMass;
            }
            else
            {
                Debug.LogWarning("ts isnt working btw???");
            }
        }

        private void LateUpdate()
        {
            // Climber e intake sí necesitan este refresh cada frame. El brazo NO -
            // arm.UpdatePid(armPid) aquí era lo que causaba el oscilar al agarrar alga:
            // reaplicaba/reseteaba el PID del brazo cada frame en vez de dejarlo
            // correr solo, así que bajo la carga extra del alga nunca terminaba de
            // asentar. ChillOut nunca llama UpdatePid para el brazo - arm.SetPid(armPid)
            // se pone una sola vez en Start y no se vuelve a tocar (ver ReturnHomeSequence
            // más abajo, que ya documentaba esa intención). No se restauró un PID aparte
            // para sostener alga (AlgaeArmHoldPID) - sigue siendo un solo armPid.
            climber.UpdatePid(climberPid);
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

        // See _reefHeadingLocked/_lockedFacingReef above. Call once per FixedUpdate before the
        // CurrentSetpoint switch: while CurrentSetpoint isn't a reef branch (L2/L3/L4) this just
        // tracks the live FacingReef value and keeps the lock cleared, same as the real robot
        // sitting at SustainedPosition; the instant CurrentSetpoint becomes a reef branch it latches
        // FacingReef for the rest of the reef visit, same as alignManager.setHeading() being read
        // once at button-press and carried across CoralHoldToL2Front/ReefFrontToReefPosition/etc.
        private bool GetLockedFacingReef()
        {
            bool inReefBranch = CurrentSetpoint == ReefscapeSetpoints.L2 ||
                                 CurrentSetpoint == ReefscapeSetpoints.L3 ||
                                 CurrentSetpoint == ReefscapeSetpoints.L4 ||
                                 CurrentSetpoint == ReefscapeSetpoints.Place;

            if (!inReefBranch)
            {
                _reefHeadingLocked = false;
                return FacingReef;
            }

            if (!_reefHeadingLocked)
            {
                _lockedFacingReef = FacingReef;
                _reefHeadingLocked = true;
            }

            return _lockedFacingReef;
        }

        // Real robot's AlgaeHighManualCommand/AlgaeLowManualCommand (AlgaeCommands.cpp) only fire from
        // Positions::SustainedPosition, L1Position, or AlgaeHold - there is no direct command path from
        // a coral branch (L2/L3/L4/L1/Place) straight to AlgaeHighReef/AlgaeLowReef. This sim used to
        // let HighAlgae/LowAlgae set the arm/elevator target directly regardless of where the arm was
        // coming from, so going straight from a coral setpoint (e.g. L4) to an algae pickup took
        // whatever the noWrap heuristic decided was the "short" way, instead of the full swing back
        // through neutral that actually happens on the robot (because on the robot you can't get there
        // without passing back through Sustained first). _algaeRouteDecided/_algaeNeedsStowRoute latch
        // that decision once on entry (LastSetpoint only reflects the prior setpoint for a single
        // frame), the same pattern as _reefHeadingLocked above.
        private bool _algaeRouteDecided;
        private bool _algaeNeedsStowRoute;

        private bool ShouldRouteAlgaeReefThroughStow()
        {
            bool inAlgaeReefSetpoint = CurrentSetpoint == ReefscapeSetpoints.HighAlgae ||
                                        CurrentSetpoint == ReefscapeSetpoints.LowAlgae;

            if (!inAlgaeReefSetpoint)
            {
                _algaeRouteDecided = false;
                return false;
            }

            if (!_algaeRouteDecided)
            {
                _algaeNeedsStowRoute = LastSetpoint == ReefscapeSetpoints.L2 ||
                                        LastSetpoint == ReefscapeSetpoints.L3 ||
                                        LastSetpoint == ReefscapeSetpoints.L4 ||
                                        LastSetpoint == ReefscapeSetpoints.L1 ||
                                        LastSetpoint == ReefscapeSetpoints.Place;
                _algaeRouteDecided = true;
            }

            return _algaeNeedsStowRoute;
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

            // Lollipop coral: se queda en el brazo (coralStowState / ArmCoralIntake). Sin esto, un intk
            // viejo en true (se queda así hasta el siguiente transfer) o el modo L1 mandaban el coral
            // al intake de piso (coralIntakeState) al recogerlo.
            if (CurrentSetpoint == ReefscapeSetpoints.Stack && CurrentRobotMode == ReefscapeRobotMode.Coral)
            {
                intk = false;
                _coralController.SetTargetState(coralStowState);
            }
            else if (CurrentIntakeMode == ReefscapeIntakeMode.Normal && intk)
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
            
            // Latched once per reef visit - see GetLockedFacingReef().
            bool lockedFacingReef = GetLockedFacingReef();

            // True for as long as we're still swinging back through stow before an algae reef pickup
            // that was requested directly from a coral branch - see ShouldRouteAlgaeReefThroughStow().
            bool routeAlgaeThroughStow = ShouldRouteAlgaeReefThroughStow() &&
                                          !atSetpoint(hasAlgae ? stowAlgae : stow);

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
                        // intakeOutAlgae doubles as the "supercycle" floor-intake pose (going for
                        // coral while already holding algae) - the earlier bug here wasn't the logic,
                        // it was intakeOutAlgae's own intake angle being set to 180 instead of 100.
                        // With that fixed, route on hasAlgae instead of CurrentRobotMode so the
                        // supercycle pose is used any time algae is already held, whether the driver
                        // is nominally in Coral or Algae mode - not just when in Algae mode.
                        SetSetpoint(hasAlgae ? intakeOutAlgae : intakeOut);
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
                            PlaceBranch(GetPlaceSetpointByLevel(lockedFacingReef));
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
                    // Stack already covered the algae lollipop (lolli). Ported from Bulldogs: the same
                    // Stack setpoint also covers a lollipop coral, picked with its own dedicated
                    // lollipopCoralIntake instead of the floor/arm coral intakes, whenever the driver
                    // is in Coral mode.
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral && !(CurrentIntakeMode == ReefscapeIntakeMode.L1))
                    {
                        // Brazo primero, luego elevador (coralSetpointDelay, igual que L2/L3/L4 - ver GetDelayType).
                        SetSetpoint(lollipopCoral);
                        // Se recoge con armCoralIntake (como OvertureWorlds), no con un collider dedicado.
                        _coralController.RequestIntake(armCoralIntake, IntakeAction.IsInProgress() && !hasCoral);
                        _coralController.RequestIntake(coralIntake, false);
                        _algaeController.RequestIntake(algaeIntake, false);
                    }
                    else
                    {
                        SetSetpoint(lolli);
                        _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                        _coralController.RequestIntake(coralIntake, false);
                    }

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
                        SetSetpoint(!lockedFacingReef ? l2Front : l2Back);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L2;
                    }
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    // armHasCoral agregado: sin esto, agarrar un coral (armHasCoral queda true
                    // en Stow, ya terminada la transferencia) y luego cambiar a modo Algae
                    // disparaba el setpoint de alga con el coral todavía en el brazo, porque
                    // transferring ya es false y el brazo ya no está en coralTransferring.
                    if (transferring || atSetpoint(coralTransferring) || armHasCoral) 
                    {
                        SetState(ReefscapeSetpoints.L2);
                    } else {
                        SetSetpoint(routeAlgaeThroughStow ? (hasAlgae ? stowAlgae : stow) : (!FacingReef ? lowFront : lowBack));
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
                        SetSetpoint(!lockedFacingReef ? l3Front : l3Back);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L3;
                    }
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    // Mismo fix que LowAlgae: armHasCoral bloquea el setpoint de alga si ya
                    // hay un coral asentado en el brazo (antes de scorear).
                    if (transferring || atSetpoint(coralTransferring) || armHasCoral) 
                    {
                        SetState(ReefscapeSetpoints.L2);
                    }
                    else
                    {
                        SetSetpoint(routeAlgaeThroughStow ? (hasAlgae ? stowAlgae : stow) : (!FacingReef ? highFront : highBack));
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
                        SetSetpoint(!lockedFacingReef ? l4Front : l4Back);
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
                    SetSetpoint(FacingBarge() ? barge1 : barge2);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    // Igual que Bulldogs: RobotSpecial entra a Stack (lollipop coral en modo Coral,
                    // lolli en modo Algae). Antes rebotaba a Stow y el setpoint nunca se activaba.
                    SetState(ReefscapeSetpoints.Stack);
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
                    IntakeModeToggleAction.Enable();
                    IntakeModeToggleAction.Disable();
                    if (hasAlgae)
                    {
                        // Salir de L1 para puntuar el alga en Barge. Sin coral que transferir,
                        // nextLevel nunca se consume desde Stow y el robot se quedaba ahí.
                        SetState(ReefscapeSetpoints.Barge);
                    }
                    else
                    {
                        nextLevel = ReefscapeSetpoints.L4;
                        SetState(ReefscapeSetpoints.Stow);
                    }
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

            RunLollipopVision();
            
            ApplySetpoints();

            if (_mainRb != null)
            {
                if (CurrentSetpoint == ReefscapeSetpoints.Climbed)
                {
                    if (!_isCgShifted)
                    {
                        _mainRb.centerOfMass = new Vector3(climbedCenterOfMassX, _originalCenterOfMass.y, climbedCenterOfMassZ);
                        _isCgShifted = true;
                    }
                }
                else if (_isCgShifted)
                {
                    _mainRb.centerOfMass = _originalCenterOfMass;
                    _isCgShifted = false;
                }
            }

        }

        // Ported from Bulldogs' RunIntakeVision(): while sitting on the lollipop pickup (Stack) in
        // Coral mode with nothing already held and IntakeAction pressed, look for the nearest coral in
        // the vision box and nudge the drivetrain toward/around it. Self-gated by the early return, so
        // it's safe to call unconditionally every FixedUpdate.
        private void RunLollipopVision()
        {
            if (CurrentSetpoint != ReefscapeSetpoints.Stack ||
                CurrentRobotMode == ReefscapeRobotMode.Algae ||
                _coralController.HasPiece() ||
                !IsIntaking)
            {
                return;
            }

            for (int i = 0; i < _lollipopVisionColliders.Length; i++)
            {
                _lollipopVisionColliders[i] = null;
            }

            var size = _lollipopVisionDetect.OverlapBoxNonAlloc(ref _lollipopVisionColliders, _coralVisionMask);

            if (_lollipopVisionColliders == null || !_lollipopVisionColliders[0])
            {
                return;
            }

            GameObject closest = _lollipopVisionColliders[0].gameObject;
            for (int i = 1; i < size; i++)
            {
                if (Vector3.Distance(_lollipopVisionColliders[i].transform.position, transform.position) <
                    Vector3.Distance(closest.transform.position, transform.position))
                {
                    closest = _lollipopVisionColliders[i].gameObject;
                }
            }

            var angle = Quaternion.LookRotation(lollipopIntakeVision.transform.position - closest.transform.position,
                lollipopIntakeVision.transform.up).eulerAngles.y - (transform.position.x >= 0 ? 180f : 90f);

            Vector2 translateInput = TranslateAction.ReadValue<Vector2>();
            float translateAngle = Mathf.Atan2(translateInput.y, translateInput.x) * Mathf.Rad2Deg;
            float heading = transform.rotation.eulerAngles.y - 90f;
            float forwardValue = 0.6f * translateInput.magnitude * Mathf.Sin(Mathf.Deg2Rad * (translateAngle + heading));
            if (GetActiveCamera().transform.eulerAngles.y > 180) forwardValue *= -1;

            DriveController.overideInput(new Vector2(forwardValue, 0f), 0, DriveController.DriveMode.RobotRelative);

            if (transform.position.x >= 0)
            {
                DriveController.SoftSteer(Mathf.Clamp((-angle + lollipopIntakeVision.transform.eulerAngles.y) / 100, 0.18f, -0.18f));
            }
            else
            {
                float turnValue = -angle + lollipopIntakeVision.transform.eulerAngles.y - 270;
                turnValue = turnValue < -180 ? turnValue + 360 : turnValue;
                DriveController.SoftSteer(Mathf.Clamp(-turnValue / 100, -0.18f, 0.18f));
            }
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
        private OffvertureSetpoint GetPlaceSetpointByLevel(bool facingReef)
        {
            switch (GetLevelByState())
            {
                case 2:
                    return !facingReef ? l2FrontPlace : l2BackPlace;
                case 3:
                    return !facingReef ? l3FrontPlace : l3BackPlace;
                case 4:
                    return !facingReef ? l4FrontPlace : l4BackPlace;
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

            if (setpoint == stowAlgae)
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

            // groundAlgae (Intake, algae mode) y lolli (Stack, algae mode - no confundir con
            // lollipopCoral, el otro setpoint del Stack): siempre giran brazo (y aplican el
            // intake de piso) primero y suben el elevador después (RaiseToSetpointSequence);
            // solo cambia cuánto esperan entre uno y otro. Viniendo de stow usan
            // algaeSetpointDelay; viniendo de cualquier otro lado usan returningAlgaeDelay (ver
            // el switch de SetSetpoint).
            if (setpoint == groundAlgae || setpoint == lolli)
            {
                return LastSetpoint == ReefscapeSetpoints.Stow
                    ? SetpointDelayType.AlgaeSetpoint
                    : SetpointDelayType.ReturningAlgae;
            }

            // lollipopCoral (Stack, modo coral): mismo criterio que arriba pero con las variantes
            // de coral - coralSetpointDelay viniendo de stow, returningCoralDelay viniendo de
            // cualquier otro lado. Siempre brazo primero, elevador después.
            if (setpoint == lollipopCoral)
            {
                return LastSetpoint == ReefscapeSetpoints.Stow
                    ? SetpointDelayType.CoralSetpoint
                    : SetpointDelayType.ReturningCoral;
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

            // barge1, barge2, climbed, l1, intakeOutAlgae: no sequencing delay. l1 is instant so the
            // setpoint delay never affects it, unlike L2/L3/L4. groundAlgae, lolli and lollipopCoral
            // used to be instant too, but now get a conditional delay based on LastSetpoint - see
            // above. intakeOutAlgae
            // (the supercycle floor-intake pose, used while already holding algae) used to share
            // stowAlgae's ReturningAlgae classification, which staggered the arm behind the floor
            // intake (elevator first, then arm after a delay, on a slowed-down PID) while the floor
            // intake itself jumped straight to its target - the mismatch between an immediately-moving
            // floor intake and a lagging arm caused it to jam. ChillOut has no staggering at all
            // (its SetSetpoint applies elevator/arm/intake together, every frame, unconditionally), so
            // intakeOutAlgae is classified Instant here to match that: all three joints move together.
            // (climb is handled by its own case above, not here; climbed is just a climber move so
            // Instant is correct for it too.)
            return SetpointDelayType.Instant;
        }

        private void SetSetpoint(OffvertureSetpoint setpoint)
        {
            // Ya estamos apuntando exactamente a este setpoint - no hay nada que hacer.
            if (isCurrentSetpoint(setpoint))
            {
                return;
            }

            var delayType = GetDelayType(setpoint);

            // El intake no forma parte de la secuencia brazo/elevador - se aplica de inmediato.
            // El climber igual se aplicaba de inmediato aquí, PERO en el robot real
            // (StateManager::SustainedToEndPosition) el climber es lo ÚLTIMO en moverse - primero
            // salen del camino intake, arm y elevator, y solo hasta el final se abre el climber.
            // Para Climb, entonces, el climber target ya NO se aplica aquí: se aplica al final de
            // ClimbSequence, después del elevador. Cualquier otro setpoint (incluido Climbed, que en
            // el robot real es únicamente un movimiento del climber) conserva el comportamiento
            // anterior de aplicarse de inmediato.
            _intakeTargetAngle = setpoint.intakeAngle;
            if (delayType != SetpointDelayType.Climb)
            {
                _climberTargetAngle = setpoint.climberAngle;
            }

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
                    // lollipopCoral siempre gira brazo (y aplica el intake de piso, ya seteado
                    // arriba) primero y sube el elevador después, igual que CoralSetpoint - la
                    // única diferencia cuando NO viene de stow es que usa returningCoralDelay en
                    // vez de coralSetpointDelay como tiempo de espera.
                    if (setpoint == lollipopCoral)
                    {
                        _setpointSequenceRoutine = StartCoroutine(RaiseToSetpointSequence(setpoint, returningCoralDelay));
                    }
                    else
                    {
                        _setpointSequenceRoutine = StartCoroutine(ReturnHomeSequence(setpoint, returningCoralDelay));
                    }
                    break;
                case SetpointDelayType.AlgaeSetpoint:
                    _setpointSequenceRoutine = StartCoroutine(RaiseToSetpointSequence(setpoint, algaeSetpointDelay));
                    break;
                case SetpointDelayType.ReturningAlgae:
                    // Mismo criterio que ReturningCoral arriba: groundAlgae y lolli siempre giran
                    // brazo (+ intake de piso) primero, sin importar de dónde vengan - solo cambia
                    // el tiempo de espera (returningAlgaeDelay en vez de algaeSetpointDelay).
                    if (setpoint == groundAlgae || setpoint == lolli)
                    {
                        _setpointSequenceRoutine = StartCoroutine(RaiseToSetpointSequence(setpoint, returningAlgaeDelay));
                    }
                    else
                    {
                        _setpointSequenceRoutine = StartCoroutine(ReturnHomeSequence(setpoint, returningAlgaeDelay));
                    }
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
        // ChillOut style: sin cambio de motion profile - arm.SetPid(armPid) se pone una sola vez en
        // Start/Awake y nunca se toca de nuevo. El real robot's Arm::setArmLowerSpeed()/setArmNormalSpeed()
        // (armAlgaeHoldPid) no aporta nada en el sim, así que se quitó junto con el swap de PID.
        private IEnumerator ReturnHomeSequence(OffvertureSetpoint setpoint, float delay)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            yield return new WaitForSeconds(delay);
            _armTargetAngle = setpoint.armAngle;
            _setpointSequenceRoutine = null;
        }

        // SetSetpoint applies the intake target immediately before starting this routine (the climber
        // target is intentionally withheld - see SetSetpoint). Ported order, matching
        // StateManager::SustainedToEndPosition on the real robot: intake (already applied) -> arm ->
        // elevator -> climber. The climber moves last, once the arm and elevator are already out of
        // the way, instead of opening at the same time as everything else.
        private IEnumerator ClimbSequence(OffvertureSetpoint setpoint)
        {
            yield return new WaitForSeconds(afterClimberDelay);
            _armTargetAngle = setpoint.armAngle;
            yield return new WaitForSeconds(afterArmDelay);
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _climberTargetAngle = setpoint.climberAngle;
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

        private bool FacingBarge()
        {
        return (transform.position.x > 0 && transform.rotation.eulerAngles.y > 180) || (transform.position.x <= 0 && transform.rotation.eulerAngles.y <= 180);
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
