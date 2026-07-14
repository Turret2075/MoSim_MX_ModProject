using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using Games.Reefscape.Scoring.Scorers;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._5940
{
public class BREAD: ReefscapeRobotBase
{
    [Header("Components")]
    [SerializeField] private GenericElevator elevator;
    [SerializeField] private GenericJoint arm;
    [SerializeField] private GenericJoint climber;
    [SerializeField] private GenericJoint intakeJoint;
    [SerializeField] private GenericJoint l1Bar;
    [SerializeField] private GenericRoller[] intakeRollers;
    [SerializeField] private GenericRoller[] endEffectorRollers;
    [SerializeField] private GenericRoller[] indexerRollers;
    [SerializeField] private Transform algaeTarget;
    [SerializeField] private Transform algaeSlider;

    [Header("PIDs")]
    [SerializeField] private PidConstants armPid;
    [SerializeField] private PidConstants climbPid;
    [SerializeField] private PidConstants intakePid;
    [SerializeField] private PidConstants l1BarPid;
    
    [Header("Intakes")]
    [SerializeField] private ReefscapeGamePieceIntake coralIntake;
    [SerializeField] private Transform coralIntakeTarget;
    [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
    
    [Header("Game Piece Stow States")]
    [SerializeField] private GamePieceState coralStowState;
    [SerializeField] private GamePieceState indexerStowState;
    [SerializeField] private GamePieceState l1StowState;
    [SerializeField] private GamePieceState algaeStowState;
    
    [Header("Setpoints")]
    [SerializeField] private BREADSetpoint stow;
    [SerializeField] private BREADSetpoint intake;
    [SerializeField] private BREADSetpoint l1;
    [SerializeField] private BREADSetpoint l2;
    [SerializeField] private BREADSetpoint l3;
    [SerializeField] private BREADSetpoint l4;
    [SerializeField] private BREADSetpoint lowAlgae;
    [SerializeField] private BREADSetpoint highAlgae;
    [SerializeField] private BREADSetpoint lollipopAlgae;
    [SerializeField] private BREADSetpoint groundAlgae;
    [SerializeField] private BREADSetpoint bargePrep;
    [SerializeField] private BREADSetpoint bargePlace;
    [SerializeField] private BREADSetpoint bargeRelease;
    [SerializeField] private BREADSetpoint processor;
    [SerializeField] private BREADSetpoint climbPrep;
    [SerializeField] private BREADSetpoint climbed;
    
    [Header("Intake Roller Audio")]
    [SerializeField] private AudioSource intakeRollerSource;
    [SerializeField] private AudioClip intakeRollerClip;
    
    [Header("Indexer Roller Audio")]
    [SerializeField] private AudioSource indexerRollerSource;
    [SerializeField] private AudioClip indexerRollerClip;
    
    [Header("End Effector Roller Audio")]
    [SerializeField] private AudioSource endEffectorRollerSource;
    [SerializeField] private AudioClip endEffectorRollerClip;
    
    // [Header("Score Audio")]
    // [SerializeField] private AudioSource scoreSource;
    // [SerializeField] private AudioClip scoreClip;
    
    [Header("Auto Align Offsets")]
    [SerializeField] private float atSetpointOffset;
    [SerializeField] private float preAlignOffset;
    [SerializeField] private float l1AlignOffset;
    private ReefscapeAutoAlign _align;

    [Header("Debug")]
    [SerializeField] private Vector3 bargeAlgaeForce;
    private float _elevatorTargetHeight;
    private float _armTargetAngle;
    private float _climberTargetAngle;
    private float _intakeTargetAngle;
    private float _l1BarTargetAngle;
    private bool _handoff;
    private int _levelSelected;
    // private bool _playedScoreSound;
    [SerializeField] private float l1IntakeAngle;
    [SerializeField] private float bargeDelay;
    [SerializeField] private float bargeForce;
    
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
    private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
    
    protected override void Start()
    {
        base.Start();
        
        arm.SetPid(armPid);
        climber.SetPid(climbPid);
        intakeJoint.SetPid(intakePid);
        l1Bar.SetPid(l1BarPid);
        
        _elevatorTargetHeight = 0;
        _armTargetAngle = 0;
        _climberTargetAngle = 0;
        _intakeTargetAngle = 0;
        _l1BarTargetAngle = 40;
        _handoff = true;
        _levelSelected = 0;
        // _playedScoreSound = false;
        
        RobotGamePieceController.SetPreload(coralStowState);
        _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
        _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

        _coralController.gamePieceStates = new[]
        {
            coralStowState,
            indexerStowState,
            l1StowState
        };
        _coralController.intakes.Add(coralIntake);

        _algaeController.gamePieceStates = new[] { algaeStowState };
        _algaeController.intakes.Add(algaeIntake);

        climber.noWrapAngle = 180f;

        arm.noWrapAngle = 120f;

        intakeRollerSource.clip = intakeRollerClip;
        intakeRollerSource.loop = true;
        intakeRollerSource.Stop();
        
        indexerRollerSource.clip = indexerRollerClip;
        indexerRollerSource.loop = true;
        indexerRollerSource.Stop();
        
        endEffectorRollerSource.clip = endEffectorRollerClip;
        endEffectorRollerSource.loop = true;
        endEffectorRollerSource.Stop();
        
        // scoreSource.clip = scoreClip;
        // scoreSource.loop = false;
        // scoreSource.Stop();
        
        _align = gameObject.GetComponent<ReefscapeAutoAlign>();
    }

    private void LateUpdate()
    {
        arm.UpdatePid(armPid);
        climber.UpdatePid(climbPid);
        intakeJoint.UpdatePid(intakePid);
        l1Bar.UpdatePid(l1BarPid);
    }

    private void SetSetpoint(BREADSetpoint setpoint)
    {
        _elevatorTargetHeight = setpoint.elevatorHeight;
        _armTargetAngle = setpoint.armAngle;
        _climberTargetAngle = setpoint.climbAngle;
        
        bool armAtTarget = Utils.InAngularRange(arm.transform.eulerAngles.x, _armTargetAngle, 10f);
        
        if (_elevatorTargetHeight < 14 && !armAtTarget)
        {
            _elevatorTargetHeight = 14f;
        }

        if (CoralAtStow(l1StowState) || CurrentSetpoint == ReefscapeSetpoints.Climb || CurrentSetpoint == ReefscapeSetpoints.Climbed)
        {
            _intakeTargetAngle = l1IntakeAngle;
        }
        else
        {
            _intakeTargetAngle = 0;
        }

        if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
        {
            _l1BarTargetAngle = 80;
        }
        else
        {
            _l1BarTargetAngle = 40;
        }
    }

    private void UpdateSetpoints()
    {
        elevator.SetTarget(_elevatorTargetHeight);
        arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(120f);
        climber.SetTargetAngle(_climberTargetAngle).withAxis(JointAxis.X).noWrap(180f);
        l1Bar.SetTargetAngle(_l1BarTargetAngle).withAxis(JointAxis.X);
        intakeJoint.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X);
    }
    
    private IEnumerator PlacePiece(bool hasCoral, bool hasAlgae)
    {
        if (CoralAtStow(l1StowState))
        {
            if (hasAlgae && CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                if (LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    // yield return new WaitForSeconds(bargeDelay);
                    yield return new WaitUntil(() => AtSetpoint(bargeRelease));
                    _algaeController.ReleaseGamePieceWithForce(bargeAlgaeForce);
                }
                else
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -2f));
                }
                // if (LastSetpoint == ReefscapeSetpoints.Processor)
                // {
                //     _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -2f));
                // }
                // else
                // {
                //     // _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -bargeForce));
                //     _algaeController.ReleaseGamePieceWithForce(bargeAlgaeForce);
                // }

                // _playedScoreSound = true;

                foreach (var roller in endEffectorRollers)
                {
                    roller.SetAngularVelocity(-800f);
                }
                
                if (_coralController.HasPiece())
                {
                    SetRobotMode(ReefscapeRobotMode.Coral);
                }
            }
            else
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 2, 0), 0.25f, 0.6f);
                foreach (var roller in intakeRollers)
                {
                    roller.SetAngularVelocity(-1000f);
                }

                yield return new WaitForSeconds(0.5f);
                
                foreach (var roller in intakeRollers)
                {
                    roller.SetAngularVelocity(0f);
                }
            }
        }
        else if (hasAlgae)
        {
            if (LastSetpoint == ReefscapeSetpoints.Barge)
            {
                // yield return new WaitForSeconds(bargeDelay);
                yield return new WaitUntil(() => AtSetpoint(bargeRelease));
                _algaeController.ReleaseGamePieceWithForce(bargeAlgaeForce);
            }
            else
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -2f));
            }

            // if (LastSetpoint == ReefscapeSetpoints.Processor)
            // {
            //     _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -3f));
            // }
            // else
            // {
            //     // _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -bargeForce));
            //     _algaeController.ReleaseGamePieceWithForce(bargeAlgaeForce);
            // }

            // _playedScoreSound = true;
            
            foreach (var roller in endEffectorRollers)
            {
                roller.SetAngularVelocity(-800f);
            }
            
            if (_coralController.HasPiece())
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
            }
        }
        else if (hasCoral && CoralAtStow(coralStowState))
        {
            // if (LastSetpoint != ReefscapeSetpoints.L1 || CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            // {
            //     _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5f));
            // }
            // else
            // {
            //     _coralController.ReleaseGamePieceWithForce(new Vector3(0, 3f, 0));
            // }
            if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                // _coralController.ReleaseGamePieceWithForce(new Vector3(0, -3f, 2f));
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 3f), 1f, 0.5f);
            }
            else if (LastSetpoint == ReefscapeSetpoints.L1)
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 3.5f));
            }
            else
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5f));
            }
            
            foreach (var roller in endEffectorRollers)
            {
                roller.SetAngularVelocity(800f);
            }
            
            _handoff = false;
            _levelSelected = 0;
        }
        
        yield return new WaitForSeconds(0.5f);
        foreach (var roller in endEffectorRollers)
        {
            roller.SetAngularVelocity(0f);
        }

        // _justEjected = true;
    }

    private void UpdateRollers(bool hasCoral, bool hasAlgae)
    {
        if ((IntakeAction.IsPressed() && !_coralController.atTarget) || coralIntake.requestIntake)
        {
            foreach (var roller in intakeRollers)
            {
                roller.ChangeAngularVelocity(1000f);
            }
        }

        if (IntakeAction.IsPressed() && !_algaeController.atTarget && (CurrentRobotMode == ReefscapeRobotMode.Algae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae || CurrentSetpoint == ReefscapeSetpoints.LowAlgae))
        {
            foreach (var roller in endEffectorRollers)
            {
                roller.ChangeAngularVelocity(500f);
            }
        }

        if (_coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget)
        {
            foreach (var roller in endEffectorRollers)
            {
                roller.ChangeAngularVelocity(-500f);
            }
        }

        if (hasCoral && !_coralController.atTarget)
        {
            foreach (var roller in indexerRollers)
            {
                roller.ChangeAngularVelocity(500f);
            }
        }
    }

    private IEnumerator UpdateAudio()
    {
        // Score Sound
        // if (CurrentSetpoint == ReefscapeSetpoints.Place && CurrentIntakeMode != ReefscapeIntakeMode.L1 && !scoreSource.isPlaying && CurrentRobotMode == ReefscapeRobotMode.Coral && !_playedScoreSound)
        // {
        //     yield return new WaitForSeconds(0.08f);
        //     // scoreSource.Play();
        //     _playedScoreSound = true;
        // }
        
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
        
        // Indexer Rollers
        float indexerRollerSpeed = Mathf.Max(new float[]
        {
            Mathf.Abs(indexerRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
            Mathf.Abs(indexerRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
            Mathf.Abs(indexerRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
        });
        if (indexerRollerSpeed > 10 && !indexerRollerSource.isPlaying)
        {
            indexerRollerSource.Play();
        }
        else if (intakeRollerSpeed <= 10 && indexerRollerSource.isPlaying)
        {
            indexerRollerSource.Stop();
        }
        
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

        yield return null;
    }

    private void UpdateAutoAlign()
    {
        if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
        {
            _align.offset = new Vector3(-0.1f, 0, l1AlignOffset);
            _align.enableBackwardsAlign = true;
        }
        else
        {
            _align.enableBackwardsAlign = false;
            bool isCoralSetpoint = CurrentSetpoint == ReefscapeSetpoints.L1 || CurrentSetpoint == ReefscapeSetpoints.L2 || CurrentSetpoint == ReefscapeSetpoints.L3 || CurrentSetpoint == ReefscapeSetpoints.L4;
            if ((AtSetpoint() && isCoralSetpoint) || CurrentSetpoint == ReefscapeSetpoints.LowAlgae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae || CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                _align.offset = new Vector3(-0.1f, 0, atSetpointOffset);
            }
            else
            {
                _align.offset = new Vector3(-0.1f, 0, preAlignOffset);
            }
        }
    }
    
    private bool AtSetpoint(BREADSetpoint stp)
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f);
    }
    
    private bool AtSetpoint()
    {
        return
            Utils.InRange(elevator.GetElevatorHeight(), _elevatorTargetHeight, 7f) &&
            Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 20f);
    }

    private bool CoralAtStow(GamePieceState stowState)
    {
        return _coralController.currentStateNum == stowState.stateNum && _coralController.atTarget;
    }
    
    private void AlgaeSlider()
    {
        if (algaeIntake.GamePiece != null)
        {
            var localSliderSpaceX = algaeTarget.transform.InverseTransformPoint(algaeIntake.GamePiece.transform.position).x;
            algaeSlider.localPosition = new Vector3(localSliderSpaceX, 0, 0);
        }
    }

    private void FixedUpdate()
    {
        bool hasAlgae = _algaeController.HasPiece();
        bool hasCoral = _coralController.HasPiece();
        bool indexerHasCoral = _coralController.atTarget && _coralController.currentStateNum == indexerStowState.stateNum;
        bool armHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum;
        bool l1HasCoral = _coralController.atTarget && _coralController.currentStateNum == l1StowState.stateNum;
        
        // Debug.Log((indexerHasCoral ? "i+" : "i-") + (armHasCoral ? "a+" : "a-") + (CurrentRobotMode == ReefscapeRobotMode.Coral ? "coral" : "algae"));
        // Debug.Log((hasCoral ? "yes " : "no ") + (CurrentRobotMode == ReefscapeRobotMode.Coral ? "coral " : "algae ") + (armHasCoral ? "yesarm " : "noarm "));

        AlgaeSlider();
        
        _algaeController.SetTargetState(algaeStowState);
        // _coralController.SetTargetState(coralStowState);

        // if (hasCoral && CurrentSetpoint != ReefscapeSetpoints.Place)
        // {
        //     _playedScoreSound = false;
        // }
        
        // Coral Stow Management

        if (indexerHasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral && (
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
        }

        if ((_levelSelected != 0 && CurrentSetpoint != ReefscapeSetpoints.Intake) || (_handoff && !armHasCoral))
        {
            SetState(ReefscapeSetpoints.Intake);
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

        if (armHasCoral && CurrentRobotMode != ReefscapeRobotMode.Coral)
        {
            SetRobotMode(ReefscapeRobotMode.Coral);
        }
        
        if (indexerHasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral && AtSetpoint(intake))
        {
            _handoff = true;
        }

        if (!_handoff)
        {
            if (l1HasCoral || (CurrentIntakeMode == ReefscapeIntakeMode.L1 && !indexerHasCoral))
            {
                _coralController.SetTargetState(l1StowState);
            }
            else
            {
                _coralController.SetTargetState(indexerStowState);
            }
        }
        else
        {
            _coralController.SetTargetState(coralStowState);
        }

        if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
        {
            coralIntake.ChangeTarget(l1StowState.stateTarget);
        }
        else
        {
            coralIntake.ChangeTarget(coralIntakeTarget);
        }
        
        if (!IntakeAction.IsPressed())
        {
            _algaeController.RequestIntake(algaeIntake, false);
            _coralController.RequestIntake(coralIntake, false);
        }

        if (_handoff && !CoralAtStow(coralStowState))
        {
            SetState(ReefscapeSetpoints.Intake);
        }

        if (_handoff)
        {
            SetRobotMode(ReefscapeRobotMode.Coral);
        }

        if (indexerHasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral &&
            LastSetpoint == ReefscapeSetpoints.Intake)
        {
            SetState(ReefscapeSetpoints.Intake);
        }
        
        switch (CurrentSetpoint)
        {
            case ReefscapeSetpoints.Stow:
                if (hasAlgae)
                {
                    SetSetpoint(stow);
                }
                else if (hasCoral && !(CoralAtStow(coralStowState)))
                {
                    SetSetpoint(intake);
                }
                else
                {
                    SetSetpoint(stow);
                }
                break;
            case ReefscapeSetpoints.Intake:
                if (hasAlgae)
                {
                    SetSetpoint(stow);
                }
                else if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    SetSetpoint(intake);
                }
                else
                {
                    SetSetpoint(groundAlgae);
                }
                
                _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && IntakeAction.IsPressed() && !CoralAtStow(coralStowState));
                _coralController.RequestIntake(coralIntake, !hasCoral && IntakeAction.IsPressed());
                break;
            case ReefscapeSetpoints.Place:
                if (LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    SetSetpoint(bargePlace);
                }
                if (OuttakeAction.triggered)
                {
                    StartCoroutine(PlacePiece(hasCoral, hasAlgae)); 
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
                SetSetpoint(bargePrep);
                break;
            case ReefscapeSetpoints.RobotSpecial:
                SetState(ReefscapeSetpoints.Stow);
                break;
            case ReefscapeSetpoints.Climb:
                SetSetpoint(climbPrep);
                break;
            case ReefscapeSetpoints.Climbed:
                SetSetpoint(climbed);
                break;
        }

        if (hasCoral && hasAlgae)
        {
            SetRobotMode(ReefscapeRobotMode.Algae);
        }
        
        UpdateSetpoints();
        StartCoroutine(UpdateAudio());
        UpdateRollers(hasCoral, hasAlgae);
        UpdateAutoAlign();
    }
}
}