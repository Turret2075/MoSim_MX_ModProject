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
using RobotFramework.Controllers.Drivetrain;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._581
{
public class BlazingBulldogsA: ReefscapeRobotBase
{
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint arm;
    [SerializeField] private GenericJoint wrist;
    [SerializeField] private GenericJoint climber;
    [SerializeField] private GenericJoint leftLatch;
    [SerializeField] private GenericJoint rightLatch;
    [SerializeField] private GenericRoller[] endEffectorRollers;
    [SerializeField] private GenericRoller[] climbRollers;
    [SerializeField] private Transform algaeTarget;
    [SerializeField] private Transform algaeSlider;
    [SerializeField] private Transform coralTarget;
    [SerializeField] private Transform coralSlider;
    [SerializeField] private BoxCollider climbScorerCollider;
    [SerializeField] private BoxCollider climbCollider;
    [SerializeField] private ClimbScorer scorer;
    [SerializeField] private BoxCollider intakeVision;
    private OverlapBoxBounds _cageDetector;

    [Header("PIDs")]
    [SerializeField] private PidConstants armPid;
    [SerializeField] private PidConstants wristPid;
    [SerializeField] private PidConstants climbPid;
    [SerializeField] private PidConstants climbLatchPid;
    
    [Header("Intakes")]
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
    
    [Header("Game Piece Stow States")]
    [SerializeField] private GamePieceState coralStowState;
    [SerializeField] private GamePieceState algaeStowState;
    
    [Header("Setpoints")]
    [SerializeField] private BlazingBulldogsASetpoint stow;
    [SerializeField] private BlazingBulldogsASetpoint groundIntake;
    [SerializeField] private BlazingBulldogsASetpoint stationIntake;
    [SerializeField] private BlazingBulldogsASetpoint l1;
    [SerializeField] private BlazingBulldogsASetpoint l2;
    [SerializeField] private BlazingBulldogsASetpoint l3;
    [SerializeField] private BlazingBulldogsASetpoint l4;
    [SerializeField] private BlazingBulldogsASetpoint lowAlgae;
    [SerializeField] private BlazingBulldogsASetpoint highAlgae;
    [SerializeField] private BlazingBulldogsASetpoint lollipopAlgae;
    [SerializeField] private BlazingBulldogsASetpoint groundAlgae;
    [SerializeField] private BlazingBulldogsASetpoint barge;
    [SerializeField] private BlazingBulldogsASetpoint processor;
    [SerializeField] private BlazingBulldogsASetpoint climbPrep;
    [SerializeField] private BlazingBulldogsASetpoint climbed;
    
    [Header("End Effector Roller Audio")]
    [SerializeField] private AudioSource endEffectorRollerSource;
    [SerializeField] private AudioClip endEffectorRollerClip;
    
    // [Header("Score Audio")]
    // [SerializeField] private AudioSource scoreSource;
    // [SerializeField] private AudioClip scoreClip;
    
    [Header("Climb Roller Audio")]
    [SerializeField] private AudioSource climbRollerSource;
    [SerializeField] private AudioClip climbRollerClip;
    
    [Header("Climb Click Audio")]
    [SerializeField] private AudioSource climbClickSource;
    [SerializeField] private AudioClip climbClickClip;
    
    [Header("Algae Stall Audio")]
    [SerializeField] private AudioSource algaeStallSource;
    [SerializeField] private AudioClip algaeStallClip;

    [Header("Miscellaneous")]
    [SerializeField] private float l4ScoreDelay;
    [SerializeField] private float l3ScoreDelay;
    [SerializeField] private float l2ScoreDelay;
    private float _elevatorTargetHeight;
    private float _armTargetAngle;
    private float _wristTargetAngle;
    private float _climberTargetAngle;
    private bool _placedVerticalCoral;
    private bool _stationMode;
    private bool _robotSpecialPressed;
    private Collider[] _colliders;
    private OverlapBoxBounds _visionDetect;
    private LayerMask _mask;
    private BlazingBulldogsASetpoint _currSetpoint;
    // private bool _playedScoreSound;
    private bool _blockDriving;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
    
    protected override void Start()
    {
        base.Start();
        
        arm.SetPid(armPid);
        climber.SetPid(climbPid);
        wrist.SetPid(wristPid);
        rightLatch.SetPid(climbLatchPid);
        leftLatch.SetPid(climbLatchPid);
        
        _elevatorTargetHeight = 0;
        _armTargetAngle = 0;
        _climberTargetAngle = 0;
        _wristTargetAngle = 0;
        _placedVerticalCoral = false;
        _cageDetector = new OverlapBoxBounds(climbScorerCollider);
        _stationMode = false;
        _robotSpecialPressed = false;
        _colliders = new Collider[6];
        _visionDetect = new OverlapBoxBounds(intakeVision);
        _mask = LayerMask.GetMask("Coral");
        _currSetpoint = stow;
        // _playedScoreSound = false;
        _blockDriving = false;
        
        RobotGamePieceController.SetPreload(coralStowState);
        _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
        _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
        
        endEffectorRollerSource.clip = endEffectorRollerClip;
        endEffectorRollerSource.loop = true;
        endEffectorRollerSource.Stop();
        
        climbRollerSource.clip = climbRollerClip;
        climbRollerSource.loop = true;
        climbRollerSource.Stop();
        
        // scoreSource.clip = scoreClip;
        // scoreSource.loop = false;
        // scoreSource.Stop();
        
        climbClickSource.clip = climbClickClip;
        climbClickSource.loop = false;
        climbClickSource.Stop();
        
        algaeStallSource.clip = algaeStallClip;
        algaeStallSource.loop = true;
        algaeStallSource.Stop();

        _coralController.gamePieceStates = new[]
        {
            coralStowState
        };
        _coralController.intakes.Add(coralIntake);

        _algaeController.gamePieceStates = new[] { algaeStowState };
        _algaeController.intakes.Add(algaeIntake);
    }

    private void LateUpdate()
    {
        arm.UpdatePid(armPid);
        climber.UpdatePid(climbPid);
        wrist.UpdatePid(wristPid);
        rightLatch.UpdatePid(climbLatchPid);
        leftLatch.UpdatePid(climbLatchPid);
    }

    private void SetSetpoint(BlazingBulldogsASetpoint setpoint)
    {
        _currSetpoint = setpoint;
        
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _armTargetAngle = setpoint.armAngle;
        _wristTargetAngle = setpoint.wristAngle;
        _climberTargetAngle = setpoint.climbAngle;

        if (_wristTargetAngle == 0 && coralSlider.localPosition.z > 0)
        {
            _wristTargetAngle = 180;
        }

        if (CurrentSetpoint == ReefscapeSetpoints.L2 || CurrentSetpoint == ReefscapeSetpoints.L3 ||  CurrentSetpoint == ReefscapeSetpoints.L4)
        {
            _elevatorTargetHeight -= Mathf.Abs(coralSlider.localPosition.z*25.4f);
        }
    }

    private void UpdateSetpoints()
    {
        bool armAtFront = arm.transform.localRotation.eulerAngles.x < 180 || arm.transform.localRotation.eulerAngles.x > 340;
        bool targetAtFront = _armTargetAngle + 360 > 330;
        if (armAtFront != targetAtFront && CurrentSetpoint != ReefscapeSetpoints.Barge)
        {
            _elevatorTargetHeight = 0;
            if (elevator.GetElevatorHeight() > 3)
            {
                _armTargetAngle = armAtFront ? 0 : 280;
            }
        }

        if (arm.transform.localRotation.eulerAngles.x > 300 && arm.transform.localRotation.eulerAngles.x < 340)
        {
            _elevatorTargetHeight = 0;
        }
        
        elevator.SetTarget(Mathf.Max(_elevatorTargetHeight, 0));
        arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(180f);
        wrist.SetTargetAngle(_wristTargetAngle).withAxis(JointAxis.Z);
        climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X);
        leftLatch.SetTargetAngle(0).withAxis(JointAxis.X);
        rightLatch.SetTargetAngle(0).withAxis(JointAxis.X);
    }
    
    private IEnumerator PlacePiece(bool hasCoral, bool hasAlgae)
    {
        if (hasCoral || hasAlgae)
        {
            foreach (var roller in endEffectorRollers)
            {
                roller.SetAngularVelocity(-500f);
            }
        }
        if (LastSetpoint == ReefscapeSetpoints.L2 || LastSetpoint == ReefscapeSetpoints.L3 ||  LastSetpoint == ReefscapeSetpoints.L4)
        {
            if (_coralController.HasPiece()) _blockDriving = true;
            yield return new WaitForSeconds(LastSetpoint switch {
                ReefscapeSetpoints.L2 => l2ScoreDelay,
                ReefscapeSetpoints.L3 => l3ScoreDelay,
                ReefscapeSetpoints.L4 => l4ScoreDelay,
                _ => 0f
                });
            _coralController.ReleaseGamePieceWithForce(new Vector3(0, LastSetpoint switch {
                ReefscapeSetpoints.L2 => 0.5f,
                ReefscapeSetpoints.L3 => 0.1f,
                ReefscapeSetpoints.L4 => 0f,
                _ => 0f
            }, 0));
            _blockDriving = false;
        }
        else
        {
            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0.5f, 0));
        }

        if (hasAlgae)
        {
            _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 3f));
            // _playedScoreSound = true;
        }
        yield return new WaitForSeconds(0.2f);
        foreach (var roller in endEffectorRollers)
        {
            roller.SetAngularVelocity(0);
        }
    }

    private void UpdateRollers(bool hasCoral, bool hasAlgae)
    {
        if (IntakeAction.IsPressed() && !hasCoral && !hasAlgae)
        {
            foreach (var roller in endEffectorRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }

        if (CurrentSetpoint == ReefscapeSetpoints.Climb)
        {
            foreach (var roller in climbRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }
    }

    private IEnumerator UpdateAudio()
    {
        // // Score Sound
        // if (CurrentSetpoint == ReefscapeSetpoints.Place && !scoreSource.isPlaying && CurrentRobotMode == ReefscapeRobotMode.Coral && !_playedScoreSound)
        // {
        //     yield return new WaitForSeconds(0.08f);
        //     // scoreSource.Play();
        //     _playedScoreSound = true;
        // }
        
        // EE Rollers
        float endEffectorRollerSpeed = Mathf.Max(new float[]
        {
            Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
            Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
            Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        });
        if (endEffectorRollerSpeed > 5 && !endEffectorRollerSource.isPlaying)
        {
            endEffectorRollerSource.Play();
        }
        else if (endEffectorRollerSpeed <= 5 && endEffectorRollerSource.isPlaying)
        {
            endEffectorRollerSource.Stop();
        }
        
        // Climb Rollers
        float climbRollerSpeed = Mathf.Max(new float[]
        {
            Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
            Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
            Mathf.Abs(climbRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        });
        if (climbRollerSpeed > 5 && !climbRollerSource.isPlaying)
        {
            climbRollerSource.Play();
        }
        else if (climbRollerSpeed <= 5 && climbRollerSource.isPlaying)
        {
            climbRollerSource.Stop();
        }
        
        // Algae Stall Sound
        if (_algaeController.HasPiece() && _algaeController.atTarget && !algaeStallSource.isPlaying)
        {
            algaeStallSource.Play();
        }
        else if (!_algaeController.HasPiece() && !_algaeController.atTarget && algaeStallSource.isPlaying)
        {
            algaeStallSource.Stop();
        }

        yield return null;
    }
    
    private void CheckStationMode()
    {
        bool changedMode = false;
        if (RobotSpecialAction.IsPressed() && !_robotSpecialPressed && BaseGameManager.Instance.RobotState == RobotState.Enabled)
        {
            _stationMode = !_stationMode;
            changedMode = true;
        }

        if (_stationMode)
        {
            CurrentCoralStationMode.DropType = DropType.Station;
        }
        else
        {
            CurrentCoralStationMode.DropType = DropType.Ground;
        }
            
        _robotSpecialPressed = RobotSpecialAction.IsPressed();
        if (changedMode) SetState(LastSetpoint);
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
    
    private bool AtSetpoint(BlazingBulldogsASetpoint stp)
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f) &&
            Utils.InAngularRange(wrist.GetSingleAxisAngle(JointAxis.Z), stp.wristAngle, 2f);
    }
    
    private bool AtSetpoint()
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), _elevatorTargetHeight, 7f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 20f) &&
            Utils.InAngularRange(wrist.GetSingleAxisAngle(JointAxis.Z), _wristTargetAngle, 20f);
    }
    
    private void AlgaeSlider()
    {
        if (algaeIntake.GamePiece != null)
        {
            var localSliderSpaceY = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).y;
            algaeSlider.localPosition = new Vector3(0, localSliderSpaceY, 0);
        }
    }
    
    private void CoralSlider()
    {
        if (coralIntake.GamePiece != null)
        {
            var localSliderSpaceZ = coralTarget.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).z;
            coralSlider.localPosition = new Vector3(0, 0, localSliderSpaceZ);
        }
    }
    
    private void RunIntakeVision()
        {
            if (!IntakeAction.IsPressed() || _coralController.HasPiece() || _algaeController.HasPiece() || CurrentRobotMode == ReefscapeRobotMode.Algae ||
                CurrentSetpoint == ReefscapeSetpoints.HighAlgae || CurrentSetpoint == ReefscapeSetpoints.LowAlgae || _stationMode) return;
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i] = null;
            }
            var size = _visionDetect.OverlapBoxNonAlloc(ref _colliders, _mask);
            
            if (_colliders != null)
            {
                if (!_colliders[0]) return;
                GameObject close = _colliders[0].gameObject;
                for (int i = 1; i < size; i++) {
                    if (Vector3.Distance(_colliders[i].transform.position, transform.position) <
                        Vector3.Distance(close.transform.position, transform.position))
                    {
                        close = _colliders[i].gameObject;
                    }
                }
                
                Transform offsetTransform = new GameObject().transform;
                offsetTransform.position = transform.position;
                offsetTransform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y+180, transform.rotation.eulerAngles.z);
                var angle = Quaternion.LookRotation(offsetTransform.position - close.transform.position, offsetTransform.up).eulerAngles.y;
                // DriveController.overideInput(new Vector2(0.5f, 0f), Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, 0.18f, -0.18f), DriveController.DriveMode.RobotRelative);
                // DriveController.SoftSteer(Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, 0.4f, -0.4f));
                DriveController.SoftSteer(Mathf.Clamp(-angle + offsetTransform.eulerAngles.y, 0.18f, -0.18f));
            }
        }

    private void FixedUpdate()
    {
        bool hasAlgae = _algaeController.HasPiece();
        bool hasCoral = _coralController.HasPiece();
        
        AlgaeSlider();
        CoralSlider();
        
        // if (_blockDriving) DriveController.overideInput(new Vector2(0, 0), 0, DriveController.DriveMode.FieldOriented);
        if (CurrentSetpoint == ReefscapeSetpoints.Place && hasCoral) DriveController.overideInput(new Vector2(0, 0), 0, DriveController.DriveMode.FieldOriented);

        climbCollider.enabled = _cageDetector.OverlapBox().Length > 3;

        if (IsIntaking)
        {
            _placedVerticalCoral = false;
        }
        
        _algaeController.SetTargetState(algaeStowState);
        _coralController.SetTargetState(coralStowState);
        
        if (!IntakeAction.IsPressed())
        {
            _algaeController.RequestIntake(algaeIntake, false);
            _coralController.RequestIntake(coralIntake, false);
        }
        
        // if (hasCoral && CurrentSetpoint != ReefscapeSetpoints.Place)
        // {
        //     _playedScoreSound = false;
        // }

        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                SetSetpoint(stow);
                break;
            case ReefscapeSetpoints.Intake:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral && !hasAlgae)
                {
                    if (_stationMode)
                    {
                        SetSetpoint(stationIntake);
                    }
                    else
                    {
                        SetSetpoint(groundIntake);
                    }
                }
                else
                {
                    SetSetpoint(groundAlgae);
                }
                
                _algaeController.RequestIntake(algaeIntake, !hasCoral && !hasAlgae && IntakeAction.IsPressed());
                _coralController.RequestIntake(coralIntake, !hasAlgae && !hasCoral && IntakeAction.IsPressed());
                break;
            case ReefscapeSetpoints.Place:
                StartCoroutine(PlacePiece(hasCoral, hasAlgae));
                
                if (!_placedVerticalCoral && GetLevelByState() > 1)
                {
                    _elevatorTargetHeight -= 8;
                    _placedVerticalCoral = true;
                }
                
                break;
            case ReefscapeSetpoints.L1:
                SetSetpoint(l1);
                break;
            case ReefscapeSetpoints.Stack:
                SetSetpoint(lollipopAlgae);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L2:
                SetSetpoint(l2);
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(lowAlgae);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L3:
                SetSetpoint(l3);
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(highAlgae);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L4:
                SetSetpoint(l4);
                break;
            case ReefscapeSetpoints.Processor:
                SetSetpoint(processor);
                break;
            case ReefscapeSetpoints.Barge:
                SetSetpoint(barge);
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetState(ReefscapeSetpoints.Stow);
                break;
            case ReefscapeSetpoints.Climb:
                SetSetpoint(climbPrep);
                if (scorer.AutoClimbTriggered)
                {
                    SetState(ReefscapeSetpoints.Climbed);
                    climbClickSource.Play();
                }
                break;
            case ReefscapeSetpoints.Climbed:
                SetSetpoint(climbed);
                break;
        }
        
        UpdateSetpoints();
        StartCoroutine(UpdateAudio());
        UpdateRollers(hasCoral, hasAlgae);
        CheckStationMode();
        RunIntakeVision();
    }
}
}