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
using UnityEngine;
using UnityEngine.Serialization;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._3501
{
public class Firebots: ReefscapeRobotBase
{
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint dale;
    [SerializeField] private GenericRoller daleRoller;
    [SerializeField] private GenericRoller upperTootsieRoller;
    [SerializeField] private GenericRoller lowerTootsieRoller;
    [SerializeField] private GenericRoller backLeftFunnelRoller;
    [SerializeField] private GenericRoller backRightFunnelRoller;
    [SerializeField] private GenericRoller frontLeftFunnelRoller;
    [SerializeField] private GenericRoller frontRightFunnelRoller;

    [Header("Constants")]
    [SerializeField] private PidConstants dalePid;
    [SerializeField] private float tootsieRollersOuttakeVelocity;
    [SerializeField] private float tootsieRollersIntakeVelocity;
    [SerializeField] private float funnelRollersL1OuttakeVelocity;
    [SerializeField] private float funnelRollersIntakeVelocity;

    [Header("Setpoints")]
    [SerializeField] private FirebotsSetpoint stow;
    [SerializeField] private FirebotsSetpoint intake;
    [SerializeField] private FirebotsSetpoint l1;
    [SerializeField] private FirebotsSetpoint l2;
    [SerializeField] private FirebotsSetpoint l3;
    [SerializeField] private FirebotsSetpoint l4;
    [SerializeField] private FirebotsSetpoint lowAlgae;
    [SerializeField] private FirebotsSetpoint highAlgae;

    [Header("Intake Components")] 
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    
    // [Header("Colliders")]
    // [SerializeField] private GameObject leftFunnelWall;
    // [SerializeField] private GameObject lowerFunnelFloor;
    // [SerializeField] private GameObject l1LowerFunnelWall;

    [Header("Game Piece States")]
    [SerializeField] private GamePieceState coralStowState;
    [FormerlySerializedAs("MidwayStowState")] [SerializeField] private GamePieceState midwayStowState;
    [SerializeField] private GamePieceState l1StowState;
    
    [Header("Roller Audio")]
    [SerializeField] private AudioSource rollerSource;
    [SerializeField] private AudioClip rollerClip;
    
    [Header("Tootsie Slide Audio")]
    [SerializeField] private AudioSource tootsieSource;
    [SerializeField] private AudioClip tootsieClip;
        
    [Header("Funnel Audio")]
    [SerializeField] private AudioSource funnelSource;
    [SerializeField] private AudioClip funnelClackClip;
    [SerializeField] private BoxCollider coralTrigger;
    private OverlapBoxBounds soundDetector;
    
    [SerializeField] private AudioSource funnelRollersSource;
    [SerializeField] private AudioClip funnelRollersClip;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    
    private float _elevatorTargetHeight;
    private float _daleTargetAngle;
    private float _daleRollerTargetVelocity;
    private int _currentCoralStow;

    private const int TOOTSIE_CORAL_STOW = 1;
    private const int MIDWAY_CORAL_STOW = 2;
    private const int L1_CORAL_STOW = 3;
    
    private ReefscapeAutoAlign _align;
    
    private float _daleXOffset;
    
    private LayerMask coralMask;
    private bool canClack;
    private bool outtakingL1;
    private bool _justPlayedScoreSound;
    
    protected override void Start()
    {
        base.Start();
        
        dale.SetPid(dalePid);

        _elevatorTargetHeight = 0;
        _daleTargetAngle = 0;
        _daleRollerTargetVelocity = 0;
        
        RobotGamePieceController.SetPreload(coralStowState);
        _currentCoralStow = 0;
        _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());

        _coralController.gamePieceStates = new[]
        {
            coralStowState,
            l1StowState,
            midwayStowState
        };
        _coralController.intakes.Add(coralIntake);
        
        _align = gameObject.GetComponent<ReefscapeAutoAlign>();
        _daleXOffset = -2.75f;
        
        rollerSource.clip = rollerClip;
        rollerSource.loop = true;
        rollerSource.Stop();
        
        tootsieSource.clip = tootsieClip;
        tootsieSource.loop = false;
        tootsieSource.Stop();
        
        funnelRollersSource.clip = funnelRollersClip;
        funnelRollersSource.loop = true;
        funnelRollersSource.Stop();
        
        funnelSource.clip = funnelClackClip;
        funnelSource.loop = false;
        funnelRollersSource.Stop();
        
        soundDetector = new OverlapBoxBounds(coralTrigger);
        
        coralMask = LayerMask.GetMask("Coral");
        canClack = true;
        outtakingL1 = false;
        _justPlayedScoreSound = false;
    }

    private void SetSetpoint(FirebotsSetpoint setpoint)
    {
        if (_coralController.HasPiece() && !_coralController.atTarget)
        {
            _elevatorTargetHeight = intake.elevatorHeight;
        }
        else
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
        }
        _daleTargetAngle = setpoint.daleAngle;
        _daleRollerTargetVelocity = setpoint.daleRollerVelocity;
    }

    private IEnumerator PlacePiece()
    {
        if (CurrentRobotMode == ReefscapeRobotMode.Coral)
        {
            if (LastSetpoint == ReefscapeSetpoints.L1)
            {
                // _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, -3.5f), 0.2f, 0.75f);
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, -5f));
                outtakingL1 = true;
                yield return new WaitForSeconds(0.5f);
                outtakingL1 = false;
            }
            else if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0f, 2.5f), 1.5f, 0.25f);
                yield return new WaitForSeconds(0.4f);
            }
            else
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 1f, 3f), 0.5f, 0.5f);
                yield return new WaitForSeconds(0.2f);
            }

            if (!IntakeAction.inProgress && CurrentSetpoint != ReefscapeSetpoints.HighAlgae && CurrentSetpoint != ReefscapeSetpoints.LowAlgae)
            {
                SetState(ReefscapeSetpoints.Stow);
            }
            // yield return new WaitForSeconds(1.0f);
            // outtakingL1 = false;
        }
    }

    private void SetCoralStow(int stowState)
    {
        switch (stowState)
        {
            case TOOTSIE_CORAL_STOW:
                _coralController.SetTargetState(coralStowState);
                _currentCoralStow = TOOTSIE_CORAL_STOW;
                break;
            case MIDWAY_CORAL_STOW:
                _coralController.SetTargetState(midwayStowState);
                _currentCoralStow = MIDWAY_CORAL_STOW;
                break;
            case L1_CORAL_STOW:
                _coralController.SetTargetState(l1StowState);
                _currentCoralStow = L1_CORAL_STOW;
                break;
        }
    }

    private void UpdateSetpoints()
    {
        elevator.SetTarget(_elevatorTargetHeight);
        dale.SetTargetAngle(_daleTargetAngle).withAxis(JointAxis.Y);
        if (_daleRollerTargetVelocity > 0)
        {
            daleRoller.SetAngularVelocity(_daleRollerTargetVelocity);
        }
        else
        {
            daleRoller.stopAngularVelocity();
        }

        // if (CurrentSetpoint == ReefscapeSetpoints.L1)
        // {
        //     lowerFunnelFloor.SetActive(false);
        //     l1LowerFunnelWall.SetActive(true);
        // }
        // else
        // {
        //     lowerFunnelFloor.SetActive(true);
        //     l1LowerFunnelWall.SetActive(false);
        // }
    }

    private void LateUpdate()
    {
        dale.UpdatePid(dalePid);
    }

    private void FixedUpdate()
    {
        bool hasCoral = _coralController.HasPiece();
        // _coralController.SetTargetState(coralStowState);

        if (CurrentSetpoint == ReefscapeSetpoints.Stow && !_coralController.atTarget)
        {
            SetState(ReefscapeSetpoints.Intake);
        }

        if (!_coralController.atTarget && hasCoral && CurrentSetpoint != ReefscapeSetpoints.Place)
        {
            if (_currentCoralStow == TOOTSIE_CORAL_STOW)
            {
                upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersIntakeVelocity);
                lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersIntakeVelocity);
                    
                backLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                backRightFunnelRoller.ChangeAngularVelocity(0.8f * funnelRollersIntakeVelocity);
                frontLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                frontRightFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
            }
            else
            {
                upperTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeVelocity);
                lowerTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeVelocity);
                    
                backLeftFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                backRightFunnelRoller.ChangeAngularVelocity(0.8f * -funnelRollersIntakeVelocity);
                frontLeftFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                frontRightFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
            }
        }

        // if (CurrentSetpoint == ReefscapeSetpoints.L1)
        // {
        //     _align.enableBackwardsAlign = true;
        //     _align.enableForwardAlign = false;
        //     
        //     if (AutoAlignLeftAction.inProgress)
        //     {
        //         _align.offset = new Vector3(0f, 0f, 0f);
        //         _align.rotation = -40;
        //     } 
        //     else if (AutoAlignRightAction.inProgress)
        //     {
        //         _align.offset = new Vector3(0f, 0f, 0f);
        //         _align.rotation = 40;
        //     }
        // }
        // else
        // {
        //     _align.enableBackwardsAlign = false;
        //     _align.enableForwardAlign = true;
        // }
        
        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                SetSetpoint(stow);
                break;
            case ReefscapeSetpoints.Intake:
                SetSetpoint(intake);
                // _coralController.SetTargetState(coralStowState);
                SetCoralStow(TOOTSIE_CORAL_STOW);
                _coralController.RequestIntake(coralIntake, !hasCoral);
                
                // _align.offset = new Vector3(0f, 0f, 8f);
                // _align.rotation = 0;
                
                if ((IntakeAction.inProgress && !hasCoral) || (hasCoral && !_coralController.atTarget))
                {
                    upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersIntakeVelocity);
                    lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersIntakeVelocity);
                    
                    backLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                    backRightFunnelRoller.ChangeAngularVelocity(0.8f * funnelRollersIntakeVelocity);
                    frontLeftFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                    frontRightFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                }
                
                // if ((hasCoral && !_coralController.atTarget) || coralIntake.hasGamePiece)
                // {
                //     leftFunnelWall.SetActive(false);
                //     lowerFunnelFloor.SetActive(false);
                // }
                // else
                // {
                //     leftFunnelWall.SetActive(true);
                //     lowerFunnelFloor.SetActive(true);
                // }
                break;
            case ReefscapeSetpoints.Place:
                if (LastSetpoint == ReefscapeSetpoints.L1)
                {
                    upperTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeVelocity);
                    lowerTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeVelocity);
                    
                    backLeftFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                    backRightFunnelRoller.ChangeAngularVelocity(0.8f * -funnelRollersIntakeVelocity);
                    frontLeftFunnelRoller.ChangeAngularVelocity(funnelRollersIntakeVelocity);
                    frontRightFunnelRoller.ChangeAngularVelocity(-funnelRollersIntakeVelocity);
                }
                else
                {
                    upperTootsieRoller.ChangeAngularVelocity(-tootsieRollersOuttakeVelocity);
                    lowerTootsieRoller.ChangeAngularVelocity(tootsieRollersOuttakeVelocity);
                }
                StartCoroutine(PlacePiece());
                break;
            case ReefscapeSetpoints.L1:
                SetSetpoint(l1);
                if (Utils.InRange(elevator.GetElevatorHeight(), l1.elevatorHeight, 0.2f))
                {
                    // _coralController.SetTargetState(l1StowState);
                    if (_currentCoralStow == TOOTSIE_CORAL_STOW)
                    {
                        SetCoralStow(MIDWAY_CORAL_STOW);
                    }
                    else if (_currentCoralStow == MIDWAY_CORAL_STOW && _coralController.atTarget)
                    {
                        SetCoralStow(L1_CORAL_STOW);
                    }
                    else
                    {
                        SetCoralStow(L1_CORAL_STOW);
                    }
                    
                }
                // _align.offset = new Vector3(0f, 0f, 8f);
                break;
            case ReefscapeSetpoints.Stack:
                break;
            case ReefscapeSetpoints.L2:
                // _coralController.SetTargetState(coralStowState);
                SetCoralStow(TOOTSIE_CORAL_STOW);
                SetSetpoint(l2);
                _align.offset = new Vector3(0f, 0f, 8f);
                // _align.rotation = 0;
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(lowAlgae);
                _align.offset = new Vector3(_daleXOffset, 0f, 8f);
                // _align.rotation = 0;
                break;
            case ReefscapeSetpoints.L3:
                // _coralController.SetTargetState(coralStowState);
                SetCoralStow(TOOTSIE_CORAL_STOW);
                SetSetpoint(l3);
                _align.offset = new Vector3(0f, 0f, 8f);
                // _align.rotation = 0;
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(highAlgae);
                _align.offset = new Vector3(_daleXOffset, 0f, 8f);
                // _align.rotation = 0;
                break;
            case ReefscapeSetpoints.L4:
                // _coralController.SetTargetState(coralStowState);
                SetCoralStow(TOOTSIE_CORAL_STOW);
                SetSetpoint(l4);
                _align.offset = new Vector3(0f, 0f, 8f);
                // _align.rotation = 0;
                break;
            case ReefscapeSetpoints.Processor:
                break;
            case ReefscapeSetpoints.Barge:
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetState(ReefscapeSetpoints.Stow);
                break;
            case ReefscapeSetpoints.Climb:
                break;
            case ReefscapeSetpoints.Climbed:
                break;
        }
        
        UpdateSetpoints();
        StartCoroutine(UpdateAudio());
    }

    private IEnumerator UpdateAudio()
    {
        if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
        {
            if (rollerSource.isPlaying || funnelRollersSource.isPlaying || funnelSource.isPlaying || tootsieSource.isPlaying)
            {
                rollerSource.Stop();
                funnelRollersSource.Stop();
                funnelSource.Stop();
                tootsieSource.Stop();
            }

            yield return null;
        }
        
        // Dale Roller
        if (_daleRollerTargetVelocity > 0 && !rollerSource.isPlaying)
        {
            rollerSource.Play();
        }
        else  if (_daleRollerTargetVelocity == 0 && rollerSource.isPlaying)
        {
            rollerSource.Stop();
        }
        
        // Score Sound
        if (CurrentSetpoint == ReefscapeSetpoints.Place && LastSetpoint != ReefscapeSetpoints.L1 && !tootsieSource.isPlaying && !_justPlayedScoreSound && CurrentRobotMode == ReefscapeRobotMode.Coral)
        {
            yield return new WaitForSeconds(0.08f);
            // tootsieSource.Play();
            _justPlayedScoreSound = true;
            yield return new WaitForSeconds(2f);
            _justPlayedScoreSound = false;
        }
        
        // Funnel Rollers
        bool hasCoral = _coralController.HasPiece();
        if (!funnelRollersSource.isPlaying && (
            (!_coralController.atTarget && hasCoral && CurrentSetpoint != ReefscapeSetpoints.Place) ||
            (IntakeAction.inProgress && !hasCoral) || (hasCoral && !_coralController.atTarget) ||
            (CurrentSetpoint == ReefscapeSetpoints.Place && LastSetpoint == ReefscapeSetpoints.L1)
            ))
        {
            funnelRollersSource.Play();
        }
        else if (funnelRollersSource.isPlaying && !(
                     (!_coralController.atTarget && hasCoral && CurrentSetpoint != ReefscapeSetpoints.Place) ||
                     (IntakeAction.inProgress && !hasCoral) || (hasCoral && !_coralController.atTarget) ||
                     (CurrentSetpoint == ReefscapeSetpoints.Place && LastSetpoint == ReefscapeSetpoints.L1)
                 ))
        {
            funnelRollersSource.Stop();
        }
        
        // Funnel Clack
        var a = soundDetector.OverlapBox(coralMask);
        if (a.Length > 0)
        {
            if (canClack && !funnelSource.isPlaying && !outtakingL1)
            {
                funnelSource.Play();
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