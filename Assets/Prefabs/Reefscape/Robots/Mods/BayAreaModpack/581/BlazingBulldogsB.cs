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
using RobotFramework.Controllers.Drivetrain;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._581
{
public class BlazingBulldogsB: ReefscapeRobotBase
{
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint arm;
    [SerializeField] private GenericJoint intakeJoint;
    [SerializeField] private GenericJoint climber;
    [SerializeField] private GenericRoller[] intakeRollers;
    [SerializeField] private GenericRoller[] endEffectorRollers;
    [SerializeField] private GenericRoller[] climbRollers;
    [SerializeField] private Transform algaeTarget;
    [SerializeField] private Transform algaeSlider;
    [SerializeField] private Transform intakeCoralTarget;
    [SerializeField] private Transform coralSlider;
    [SerializeField] private BoxCollider climbScorerCollider;
    [SerializeField] private BoxCollider climbCollider;
    [SerializeField] private ClimbScorer scorer;
    [SerializeField] private BoxCollider lollipopIntakeVision;
    private OverlapBoxBounds _cageDetector;

    [Header("PIDs")]
    [SerializeField] private PidConstants armPid;
    [SerializeField] private PidConstants armPidWithPiece;
    [SerializeField] private PidConstants intakePid;
    [SerializeField] private PidConstants climbPid;
    // [SerializeField] private PidConstants climbLatchPid;
    
    [Header("Intakes")]
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    [SerializeField] private ReefscapeGamePieceIntake lollipopCoralIntake;
    [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
    
    [Header("Game Piece Stow States")]
    [SerializeField] private GamePieceState coralStowState;
    [SerializeField] private GamePieceState intakeStowState;
    [SerializeField] private GamePieceState algaeStowState;

    [Header("Setpoints")]
    [SerializeField] private BlazingBulldogsBSetpoint stow;
    [SerializeField] private BlazingBulldogsBSetpoint transfer;
    [SerializeField] private BlazingBulldogsBSetpoint intake;
    [SerializeField] private BlazingBulldogsBSetpoint intakeL1;
    [SerializeField] private BlazingBulldogsBSetpoint l1Intake;
    [SerializeField] private BlazingBulldogsBSetpoint l1Front;
    [SerializeField] private BlazingBulldogsBSetpoint l2Front;
    [SerializeField] private BlazingBulldogsBSetpoint l3Front;
    [SerializeField] private BlazingBulldogsBSetpoint l4Front;
    [SerializeField] private BlazingBulldogsBSetpoint l2FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l3FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l4FrontPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l2FrontRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l3FrontRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l4FrontRelease;
    [SerializeField] private BlazingBulldogsBSetpoint lowAlgaeFront;
    [SerializeField] private BlazingBulldogsBSetpoint highAlgaeFront;
    [SerializeField] private BlazingBulldogsBSetpoint l1Back;
    [SerializeField] private BlazingBulldogsBSetpoint l2Back;
    [SerializeField] private BlazingBulldogsBSetpoint l3Back;
    [SerializeField] private BlazingBulldogsBSetpoint l4Back;
    [SerializeField] private BlazingBulldogsBSetpoint l2BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l3BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l4BackPlace;
    [SerializeField] private BlazingBulldogsBSetpoint l2BackRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l3BackRelease;
    [SerializeField] private BlazingBulldogsBSetpoint l4BackRelease;
    [SerializeField] private BlazingBulldogsBSetpoint lowAlgaeBack;
    [SerializeField] private BlazingBulldogsBSetpoint highAlgaeBack;
    [SerializeField] private BlazingBulldogsBSetpoint lollipopAlgae;
    [SerializeField] private BlazingBulldogsBSetpoint groundAlgae;
    [SerializeField] private BlazingBulldogsBSetpoint bargeFront;
    [SerializeField] private BlazingBulldogsBSetpoint bargeBack;
    [SerializeField] private BlazingBulldogsBSetpoint processor;
    [SerializeField] private BlazingBulldogsBSetpoint climbPrep;
    [SerializeField] private BlazingBulldogsBSetpoint climbed;
    [SerializeField] private BlazingBulldogsBSetpoint lollipopCoral;
    
    [Header("End Effector Roller Audio")]
    [SerializeField] private AudioSource endEffectorRollerSource;
    [SerializeField] private AudioClip endEffectorRollerClip;
    
    // [Header("Score Audio")]
    // [SerializeField] private AudioSource scoreSource;
    // [SerializeField] private AudioClip scoreClip;
    
    [Header("Intake Roller Audio")]
    [SerializeField] private AudioSource intakeRollerSource;
    [SerializeField] private AudioClip intakeRollerClip;
    
    [Header("Climb Roller Audio")]
    [SerializeField] private AudioSource climbRollerSource;
    [SerializeField] private AudioClip climbRollerClip;
    
    [Header("Climb Click Audio")]
    [SerializeField] private AudioSource climbClickSource;
    [SerializeField] private AudioClip climbClickClip;
    
    [Header("Algae Stall Audio")]
    [SerializeField] private AudioSource algaeStallSource;
    [SerializeField] private AudioClip algaeStallClip;
    
    [Header("Auto Align")]
    [SerializeField] private float zOffset;
    [SerializeField] private float xOffset;
    private ReefscapeAutoAlign _align;

    [Header("Miscellaneous")]
    [SerializeField] private float reefAvoidanceDistance;
    [SerializeField] private float bargeAvoidanceDistance;
    
    private float _elevatorTargetHeight;
    private float _armTargetAngle;
    private float _intakeTargetAngle;
    private float _climberTargetAngle;
    private bool _handoff;
    private bool _justPlaced;
    private Vector3 _blueReef;
    private Vector3 _redReef;
    private Collider[] _colliders;
    private OverlapBoxBounds _visionDetect;
    private LayerMask _mask;
    private BlazingBulldogsBSetpoint _currentSetpoint;
    private bool _justPlacedAlgae;
    private bool _wasCoralMode;
    private int _levelSelected;
    private bool _bypassL1Mode;
    private bool _wentToStow;
    private bool _wentToLollipopCoral;
    // private bool _playedScoreSound;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
    
    protected override void Start()
    {
        base.Start();
        
        arm.SetPid(armPid);
        climber.SetPid(climbPid);
        intakeJoint.SetPid(intakePid);
        
        _elevatorTargetHeight = 0;
        _armTargetAngle = 0;
        _climberTargetAngle = 0;
        _intakeTargetAngle = 0;
        _handoff = true;
        _justPlaced = false;
        _cageDetector = new OverlapBoxBounds(climbScorerCollider);
        _blueReef = GameObject.Find("BlueReef").transform.position;
        _redReef = GameObject.Find("RedReef").transform.position;
        _colliders = new Collider[6];
        _visionDetect = new OverlapBoxBounds(lollipopIntakeVision);
        _mask = LayerMask.GetMask("Coral");
        _currentSetpoint = stow;
        _align = GetComponent<ReefscapeAutoAlign>();
        _justPlacedAlgae = false;
        _wasCoralMode = false;
        _levelSelected = 0;
        _bypassL1Mode = false;
        _wentToStow = false;
        _wentToLollipopCoral = false;
        // _playedScoreSound = false;
        
        RobotGamePieceController.SetPreload(coralStowState);
        _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
        _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
        
        endEffectorRollerSource.clip = endEffectorRollerClip;
        endEffectorRollerSource.loop = true;
        endEffectorRollerSource.Stop();
        
        // scoreSource.clip = scoreClip;
        // scoreSource.loop = false;
        // scoreSource.Stop();
        
        intakeRollerSource.clip = endEffectorRollerClip;
        intakeRollerSource.loop = true;
        intakeRollerSource.Stop();
        
        climbRollerSource.clip = climbRollerClip;
        climbRollerSource.loop = true;
        climbRollerSource.Stop();
        
        climbClickSource.clip = climbClickClip;
        climbClickSource.loop = false;
        climbClickSource.Stop();
        
        algaeStallSource.clip = algaeStallClip;
        algaeStallSource.loop = true;
        algaeStallSource.Stop();

        _coralController.gamePieceStates = new[]
        {
            coralStowState,
            intakeStowState
        };
        _coralController.intakes.Add(coralIntake);
        _coralController.intakes.Add(lollipopCoralIntake);

        _algaeController.gamePieceStates = new[] { algaeStowState };
        _algaeController.intakes.Add(algaeIntake);
    }

    private void LateUpdate()
    {
        arm.UpdatePid((_algaeController.HasPiece() || CoralAtStow(coralStowState)) ? armPidWithPiece : armPid);
        climber.UpdatePid(climbPid);
        intakeJoint.UpdatePid(intakePid);
    }

    private void SetSetpoint(BlazingBulldogsBSetpoint setpoint)
    {
        _currentSetpoint = setpoint;
        
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _armTargetAngle = setpoint == transfer && _coralController.HasPiece() ? GetTransferArmAngle() : setpoint.armAngle;
        _intakeTargetAngle = setpoint.intakeAngle;
        _climberTargetAngle = setpoint.climbAngle;
    }

    private void UpdateSetpoints()
    {
        float elevatorMinHeight = 30 * Mathf.Cos((arm.GetSingleAxisAngle(JointAxis.X) - 180) * Mathf.Deg2Rad);
        if (arm.GetSingleAxisAngle(JointAxis.X) < 100 || arm.GetSingleAxisAngle(JointAxis.X) > 260)
        {
            elevatorMinHeight = 0;
        }
        elevator.SetTarget(Mathf.Max(_elevatorTargetHeight, elevatorMinHeight));
        intakeJoint.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.Z);
        climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X);

        float armNoWarpAngle;
        
        if (DistanceToReef(GetClosestReef()) < reefAvoidanceDistance)
        {
            // armNoWarpAngle = IsFacingReef(GetClosestReef()) ? 135 : 225;
            armNoWarpAngle = IsFacingReef(GetClosestReef()) ? 105 : 255;
        }
        else if (DistanceToBarge() < bargeAvoidanceDistance)
        {
            armNoWarpAngle = !FacingBarge() ? 270 : 90;
        }
        else
        {
            armNoWarpAngle = -1;
        }

        // bool targetOnRight = _armTargetAngle > 180;
        // bool armOnRight = arm.GetSingleAxisAngle(JointAxis.X) > 180;

        if (_currentSetpoint == lollipopCoral)
        {
            armNoWarpAngle = 230;
        }
        
        // if (elevator.GetElevatorHeight() < 40 && targetOnRight != armOnRight && !Utils.InAngularRange(_armTargetAngle, 180, 10) && !Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), 180, 20)) 
        if (elevator.GetElevatorHeight() < 40 && !Utils.InAngularRange(_armTargetAngle%360, 180, 60) && !Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X)%360, 180, 60))
        {
            armNoWarpAngle = 180;
        }
        
        arm.noWrap = armNoWarpAngle >= 0;
        
        if (armNoWarpAngle >= 0)
        {
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(armNoWarpAngle);
        }
        else
        {
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X);
        }
        // arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(armNoWarpAngle);
    }
    
    private IEnumerator PlacePiece(bool hasCoral, bool hasAlgae)
    {
        if (hasAlgae && (CurrentIntakeMode == ReefscapeIntakeMode.Normal || !hasCoral || CurrentRobotMode == ReefscapeRobotMode.Algae)) // Place algae
        {
            _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 1.5f, 0));
            _justPlacedAlgae = true;
            // _playedScoreSound = true;
            if (hasCoral || _wasCoralMode)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
                _wasCoralMode = false;
            }
            
            foreach (var roller in endEffectorRollers)
            {
                roller.SetAngularVelocity(-500f);
            }
            yield return new WaitForSeconds(0.5f);
            foreach (var roller in endEffectorRollers)
            {
                roller.SetAngularVelocity(0f);
            }
            
            yield return new WaitForSeconds(0.25f);
            _justPlacedAlgae = false;

            if (CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                SetState(ReefscapeSetpoints.Stow);
            }
        }
        else if (!_justPlacedAlgae) // Place coral
        {
            if (_coralController.currentStateNum == coralStowState.stateNum)
            {
                float start = Time.time;
                switch (GetLevelByState())
                {
                    case 4:
                        // yield return new WaitUntil(() => ArmAtSetpoint(IsFacingReef(GetClosestReef()) ? l4Front :  l4Back));
                        yield return new WaitUntil(() => AtSetpoint(IsFacingReef(GetClosestReef()) ? l4FrontRelease :  l4BackRelease) || (Time.time - start > 0.4f));
                        if (CurrentSetpoint == ReefscapeSetpoints.Place) _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
                        // if (!_playedScoreSound && !scoreSource.isPlaying) scoreSource.Play();
                        // _playedScoreSound = true;
                        break;
                    case 3:
                        // yield return new WaitUntil(() => ArmAtSetpoint(IsFacingReef(GetClosestReef()) ? l3Front :  l3Back));
                        yield return new WaitUntil(() => AtSetpoint(IsFacingReef(GetClosestReef()) ? l3FrontRelease :  l3BackRelease) || (Time.time - start > 0.4f));
                        if (CurrentSetpoint == ReefscapeSetpoints.Place) _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1.5f, IsFacingReef(GetClosestReef()) ? 2.5f : -2.5f));
                        // if (!_playedScoreSound && !scoreSource.isPlaying) scoreSource.Play();
                        // _playedScoreSound = true;
                        break;
                    case 2:
                        // yield return new WaitUntil(() => ArmAtSetpoint(IsFacingReef(GetClosestReef()) ? l2Front :  l2Back));
                        yield return new WaitUntil(() => AtSetpoint(IsFacingReef(GetClosestReef()) ? l2FrontRelease :  l2BackRelease) || (Time.time - start > 0.4f));
                        if (CurrentSetpoint == ReefscapeSetpoints.Place) _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1.5f, IsFacingReef(GetClosestReef()) ? 2.5f : -2.5f));
                        // if (!_playedScoreSound && !scoreSource.isPlaying) scoreSource.Play();
                        // _playedScoreSound = true;
                        break;
                    case 1:
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1.5f, 0));
                        // if (!_playedScoreSound && !scoreSource.isPlaying) scoreSource.Play();
                        // _playedScoreSound = true;
                        break;
                    default:
                        _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1f, 0));
                        break;
                }
                foreach (var roller in endEffectorRollers)
                {
                    roller.SetAngularVelocity(-500f);
                }
                yield return new WaitForSeconds(0.5f);
                foreach (var roller in endEffectorRollers)
                {
                    roller.SetAngularVelocity(0f);
                }
            }
            else
            {
                if (hasCoral)
                {
                    foreach (var roller in intakeRollers)
                    {
                        roller.SetAngularVelocity(-500f);
                    }
                }
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(-0.05f, 3f, 0), 0.09f, 0.8f);
                yield return new WaitForSeconds(0.1f);
                // if (!_playedScoreSound && !scoreSource.isPlaying) scoreSource.Play();
                // _playedScoreSound = true;
                yield return new WaitForSeconds(0.4f);
                foreach (var roller in intakeRollers)
                {
                    roller.SetAngularVelocity(0f);
                }
            }
            _handoff = false;
            _justPlaced = true;
            _levelSelected = 0;
            _bypassL1Mode = false;
        }
    }

    private void UpdateRollers(bool hasCoral, bool hasAlgae)
    {
        if (IntakeAction.IsPressed() && !hasCoral && (CurrentRobotMode == ReefscapeRobotMode.Coral || hasAlgae) && CurrentSetpoint != ReefscapeSetpoints.Stack && CurrentSetpoint != ReefscapeSetpoints.RobotSpecial)
        {
            foreach (var roller in intakeRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }
        
        if (hasCoral && !_coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum)
        {
            foreach (var roller in endEffectorRollers)
            {
                roller.ChangeAngularVelocity(500f);
            }
            foreach (var roller in intakeRollers)
            {
                roller.ChangeAngularVelocity(-500f);
            }
        }
        
        if (IntakeAction.IsPressed() && (CurrentSetpoint == ReefscapeSetpoints.Stack || CurrentSetpoint == ReefscapeSetpoints.RobotSpecial || CurrentRobotMode == ReefscapeRobotMode.Algae) && !(hasAlgae || CoralAtStow(coralStowState)))
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
                roller.ChangeAngularVelocity(1500f);
            }
        }
    }

    private IEnumerator UpdateAudio()
    {
         // Score Sound
        // if (CurrentSetpoint == ReefscapeSetpoints.Place && !scoreSource.isPlaying && CurrentRobotMode == ReefscapeRobotMode.Coral && !_playedScoreSound)
        // {
        //     yield return new WaitForSeconds(0.08f);
        //     scoreSource.Play();
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
        
        // Intake Rollers
        float intakeRollerSpeed = Mathf.Max(new float[]
        {
            Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
            Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
            Mathf.Abs(intakeRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        });
        if (intakeRollerSpeed > 5 && !intakeRollerSource.isPlaying)
        {
            intakeRollerSource.Play();
        }
        else if (intakeRollerSpeed <= 5 && intakeRollerSource.isPlaying)
        {
            intakeRollerSource.Stop();
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
    
    private bool ArmAtSetpoint(BlazingBulldogsBSetpoint stp)
    {
        return Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X),
            stp == transfer ? GetTransferArmAngle() : stp.armAngle, 2f);
    }
    
    private bool AtSetpoint(BlazingBulldogsBSetpoint stp)
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp == transfer ? GetTransferArmAngle() : stp.armAngle, 2f) &&
            Utils.InAngularRange(intakeJoint.GetSingleAxisAngle(JointAxis.Z), stp.intakeAngle, 2f);
    }
    
    private bool AtSetpoint()
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), _elevatorTargetHeight, 7f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 20f) &&
            Utils.InAngularRange(intakeJoint.GetSingleAxisAngle(JointAxis.Z), _intakeTargetAngle, 20f);
    }

    private bool CoralAtStow(GamePieceState stowState)
    {
        return _coralController.atTarget && _coralController.currentStateNum == stowState.stateNum;
    }

    private bool FacingBarge()
    {
        return (transform.position.x > 0 && transform.rotation.eulerAngles.y > 180) || (transform.position.x <= 0 && transform.rotation.eulerAngles.y <= 180);
    }
    
    private float DistanceToReef(Vector3 reefPos)
    {
        return Mathf.Sqrt(Mathf.Pow(transform.position.x - reefPos.x, 2) + Mathf.Pow(transform.position.z - reefPos.z, 2));
    }
    
    private float DistanceToBarge()
    {
        return Mathf.Abs(transform.position.x);
    }
    
    private Vector3 GetClosestReef()
    {
        return DistanceToReef(_blueReef) < DistanceToReef(_redReef) ? _blueReef : _redReef;
    }

    private bool IsFacingReef(Vector3 reefPos)
    {
        var toReefVector = (reefPos - transform.position).normalized;
        var robotForwardVector = transform.forward.normalized;
        var angle = Vector3.Dot(robotForwardVector, toReefVector);
        return angle > 0.0f;
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

    private float GetTransferArmAngle()
    {
        return 180 - Mathf.Rad2Deg*Mathf.Atan2(coralSlider.localPosition.z/0.0254f, 28f);
    }
    
    private void AlgaeSlider()
    {
        if (algaeIntake.GamePiece != null)
        {
            var localSliderSpaceZ = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).z;
            algaeSlider.localPosition = new Vector3(0, 0, localSliderSpaceZ);
        }
    }
    
    private void CoralSlider()
    {
        if (coralIntake.GamePiece != null)
        {
            var localSliderSpaceZ = intakeCoralTarget.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).z;
            coralSlider.localPosition = new Vector3(0, 0, Mathf.Clamp(localSliderSpaceZ, -0.0875f, 0.057f));
        }
        else if (!_coralController.HasPiece())
        {
            coralSlider.localPosition = new Vector3(0, 0, 0);
        }
    }
    
    private void UpdateAutoAlign()
    {
        _align.offset = new Vector3(IsFacingReef(GetClosestReef()) ? xOffset : -xOffset, 0, zOffset);
    }
    
    private void RunIntakeVision()
        {
            if ((CurrentSetpoint != ReefscapeSetpoints.RobotSpecial && CurrentSetpoint != ReefscapeSetpoints.Stack) || CurrentRobotMode == ReefscapeRobotMode.Algae || _coralController.HasPiece() || !IsIntaking)
            {
                return;
            }
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
                
                var angle = Quaternion.LookRotation(lollipopIntakeVision.transform.position - close.transform.position, lollipopIntakeVision.transform.up).eulerAngles.y - (transform.position.x >= 0 ? 180f : 90f);
                Vector2 translateInput = TranslateAction.ReadValue<Vector2>();
                float translateAngle = Mathf.Atan2(translateInput.y, translateInput.x) * Mathf.Rad2Deg;
                float heading = transform.rotation.eulerAngles.y - 90f;
                float forwardValue = 0.6f * translateInput.magnitude * Mathf.Sin(Mathf.Deg2Rad * (translateAngle + heading));
                if (GetActiveCamera().transform.eulerAngles.y > 180) forwardValue *= -1;
                DriveController.overideInput(new Vector2(forwardValue, 0f), 0, DriveController.DriveMode.RobotRelative);
                if (transform.position.x >= 0)
                {
                    DriveController.SoftSteer(Mathf.Clamp((-angle + lollipopIntakeVision.transform.eulerAngles.y)/100, 0.18f, -0.18f));
                }
                else
                {
                    float turnValue = -angle + lollipopIntakeVision.transform.eulerAngles.y - 270;
                    turnValue = turnValue < -180 ? turnValue + 360 : turnValue;
                    DriveController.SoftSteer(Mathf.Clamp(-turnValue/100, -0.18f, 0.18f));
                }
            }
        }

    private void FixedUpdate()
    {
        bool hasAlgae = _algaeController.HasPiece();
        bool hasCoral = _coralController.HasPiece();
        bool intakeHasCoral = _coralController.atTarget && _coralController.currentStateNum == intakeStowState.stateNum;
        bool armHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum;
        
        AlgaeSlider();
        CoralSlider();
        
        climbCollider.enabled = scorer.AutoClimbTriggered;
        
        _algaeController.SetTargetState(algaeStowState);

        if (_handoff || CoralAtStow(coralStowState) || (CoralAtStow(intakeStowState) && AtSetpoint(transfer)) || (AtSetpoint(lollipopCoral) && !CoralAtStow(intakeStowState)))
        {
            _coralController.SetTargetState(coralStowState);
            _handoff = true;
        }
        else
        {
            _coralController.SetTargetState(intakeStowState);
        }

        if (!hasCoral)
        {
            _handoff = false;
        }

        if (CurrentRobotMode == ReefscapeRobotMode.Coral && IsIntaking)
        {
            _justPlaced = false;
        }

        if (CurrentSetpoint == ReefscapeSetpoints.Place && hasCoral)
        {
            _justPlaced = true;
        }
        
        // if (hasCoral && CurrentSetpoint != ReefscapeSetpoints.Place)
        // {
        //     _playedScoreSound = false;
        // }

        if (_justPlaced)
        {
            BlazingBulldogsBSetpoint placeSetpoint = _currentSetpoint;
            if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                placeSetpoint = IsFacingReef(GetClosestReef()) ? l4FrontPlace : l4BackPlace;
            }
            else if (LastSetpoint == ReefscapeSetpoints.L3)
            {
                placeSetpoint = IsFacingReef(GetClosestReef()) ? l3FrontPlace : l3BackPlace;
            }
            else if (LastSetpoint == ReefscapeSetpoints.L2)
            {
                placeSetpoint = IsFacingReef(GetClosestReef()) ? l2FrontPlace : l2BackPlace;
            }
            SetSetpoint(placeSetpoint);
        }

        if (hasAlgae)
        {
            if (CurrentRobotMode == ReefscapeRobotMode.Coral)
            {
                _wasCoralMode = true;
            }
            SetRobotMode(ReefscapeRobotMode.Algae);
        }
        
        // _coralController.SetTargetState(_coralController.currentStateNum switch
        // {
        //     1 => coralStowState,
        //     2 => intakeStowState,
        //     _ => coralStowState
        // });
        
        // if (!IntakeAction.IsPressed())
        // {
        //     _algaeController.RequestIntake(algaeIntake, false);
        //     _coralController.RequestIntake(coralIntake, false);
        // }
        
        if (intakeHasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral && (
                CurrentSetpoint == ReefscapeSetpoints.L1 ||
                CurrentSetpoint == ReefscapeSetpoints.L2 ||
                CurrentSetpoint == ReefscapeSetpoints.L3 ||
                CurrentSetpoint == ReefscapeSetpoints.L4))
        {
            _levelSelected = CurrentSetpoint switch
            {
                ReefscapeSetpoints.L1 => 1,
                ReefscapeSetpoints.L2 => 2,
                ReefscapeSetpoints.L3 => 3,
                ReefscapeSetpoints.L4 => 4,
                ReefscapeSetpoints.Intake => _levelSelected,
                _ => 0
            };
            _bypassL1Mode = true;
        }

        if ((CurrentIntakeMode == ReefscapeIntakeMode.Normal || _bypassL1Mode) && ((_levelSelected != 0 && CurrentSetpoint != ReefscapeSetpoints.Stow) || (_handoff && !armHasCoral)))
        {
            SetState(ReefscapeSetpoints.Stow);
        }

        if (_levelSelected != 0 && armHasCoral)
        {
            SetState(_levelSelected switch
            {
                1 => ReefscapeSetpoints.L1,
                2 => ReefscapeSetpoints.L2,
                3 => ReefscapeSetpoints.L3,
                4 => ReefscapeSetpoints.L4,
                _ => ReefscapeSetpoints.Stow
            });
            _levelSelected = 0;
        }

        if (CurrentSetpoint != ReefscapeSetpoints.Stow)
        {
            _wentToStow = false;
        }

        if (CurrentSetpoint != ReefscapeSetpoints.Stack)
        {
            _wentToLollipopCoral = false;
        }
        
        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                if (!L1Action.IsPressed()) _wentToStow = true;
                if (L1Action.IsPressed() && LastSetpoint == ReefscapeSetpoints.Stow && !hasCoral)
                {
                    if (_wentToStow || AtSetpoint(stow) || AtSetpoint(transfer))
                    {
                        SetState(ReefscapeSetpoints.Stack);
                        _wentToStow = false;
                    }
                }
                if (hasAlgae || CoralAtStow(coralStowState) || (CurrentIntakeMode == ReefscapeIntakeMode.L1 && !_bypassL1Mode))
                {
                    if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && CoralAtStow(intakeStowState))
                    {
                        SetSetpoint(l1Intake);
                    }
                    else
                    {
                        SetSetpoint(stow);
                    }
                }
                else
                {
                    SetSetpoint(transfer);
                }
                break;
            case ReefscapeSetpoints.Intake:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral || hasAlgae)
                {
                    if (!armHasCoral)
                    {
                        if (hasAlgae || CurrentIntakeMode == ReefscapeIntakeMode.L1)
                        {
                            SetSetpoint(intakeL1);
                        }
                        else
                        {
                            SetSetpoint(intake);
                        }
                    }
                    else
                    {
                        SetSetpoint(stow);
                    }
                }
                else
                {
                    SetSetpoint(groundAlgae);
                }
                
                _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && IntakeAction.IsPressed());
                _coralController.RequestIntake(coralIntake, !hasCoral && IntakeAction.IsPressed());
                break;
            case ReefscapeSetpoints.Place:
                StartCoroutine(PlacePiece(hasCoral, hasAlgae));
                break;
            case ReefscapeSetpoints.L1:
                if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && !_bypassL1Mode)
                {
                   SetSetpoint(l1Intake); 
                }
                else
                {
                    SetSetpoint(IsFacingReef(GetClosestReef()) ? l1Front : l1Back);
                }
                break;
            case ReefscapeSetpoints.Stack:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    if (!L1Action.IsPressed()) _wentToLollipopCoral = true;
                    else if (_wentToLollipopCoral)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        _wentToLollipopCoral = false;
                    }
                    SetSetpoint(lollipopCoral);
                }
                else
                {
                    SetSetpoint(lollipopAlgae);
                }
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(lollipopCoralIntake, IntakeAction.IsPressed() && !hasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral);
                break;
            case ReefscapeSetpoints.L2:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? l2Front : l2Back);
                break;
            case ReefscapeSetpoints.LowAlgae:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? lowAlgaeFront : lowAlgaeBack);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L3:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? l3Front : l3Back);
                break;
            case ReefscapeSetpoints.HighAlgae:
                SetSetpoint(IsFacingReef(GetClosestReef()) ? highAlgaeFront : highAlgaeBack);
                
                _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                _coralController.RequestIntake(coralIntake, false);
                break;
            case ReefscapeSetpoints.L4:
                // if (_justPlaced)
                // {
                //     SetSetpoint(IsFacingReef(GetClosestReef()) ? l4FrontPlace : l4BackPlace);
                // }
                // else
                // {
                //     SetSetpoint(IsFacingReef(GetClosestReef()) ? l4Front : l4Back);
                // }
                SetSetpoint(IsFacingReef(GetClosestReef()) ? l4Front : l4Back);
                break;
            case ReefscapeSetpoints.Processor:
                SetSetpoint(processor);
                break;
            case ReefscapeSetpoints.Barge:
                SetSetpoint(FacingBarge() ? bargeFront : bargeBack);
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetState(ReefscapeSetpoints.Stack);
                // SetSetpoint(lollipopCoral);
                //
                // _algaeController.RequestIntake(algaeIntake, !hasCoral && !hasAlgae && AtSetpoint(lollipopCoral));
                // _coralController.RequestIntake(lollipopCoralIntake, !hasCoral && !hasAlgae && AtSetpoint(lollipopCoral));
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
            default:
                break;
        }

        if (_coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget)
        {
            SetSetpoint(transfer);
        }
        
        UpdateSetpoints();
        StartCoroutine(UpdateAudio());
        UpdateRollers(hasCoral, hasAlgae);
        UpdateAutoAlign();
        RunIntakeVision();
    }
}
}