using System;
using System.Collections;
using System.Diagnostics;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using GameSystems.Management;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Prefabs.Reefscape.Robots.Mods.BayAreaModpack._604
{
    public class QuixilverA: ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint arm;
        [SerializeField] private GenericJoint wrist;
        [SerializeField] private GenericJoint climb;
        [SerializeField] private GenericJoint rightFunnelFlap;
        [SerializeField] private GenericJoint leftFunnelFlap;
        [SerializeField] private BoxCollider[] wristColliders;
        [SerializeField] private CapsuleCollider[] wristCapsuleColliders;
        [SerializeField] private GenericRoller[] funnelRollers;
        [SerializeField] private GenericRoller[] endEffectorRollers;
        [SerializeField] private BoxCollider coralBlocker;

        [Header("PIDs")]
        [SerializeField] private PidConstants armPid;
        [SerializeField] private PidConstants armL4Pid;
        [SerializeField] private PidConstants wristPid;
        [SerializeField] private PidConstants wristL4AvoidancePid;
        [SerializeField] private PidConstants climbPid;
        [SerializeField] private PidConstants funnelFlapPid;

        [Header("Intakes")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;
        
        [Header("Game Piece Stow States")]
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState funnelStowState;
        [SerializeField] private GamePieceState algaeStowState;
        
        [Header("Setpoints")]
        [SerializeField] private QuixilverASetpoint stow;
        [SerializeField] private QuixilverASetpoint intake;
        [SerializeField] private QuixilverASetpoint l1;
        [SerializeField] private QuixilverASetpoint l2;
        [SerializeField] private QuixilverASetpoint l3;
        [SerializeField] private QuixilverASetpoint l4;
        [SerializeField] private QuixilverASetpoint highAlgae;
        [SerializeField] private QuixilverASetpoint lowAlgae;
        [SerializeField] private QuixilverASetpoint processor;
        [SerializeField] private QuixilverASetpoint barge;
        [SerializeField] private QuixilverASetpoint bargePlace;
        [SerializeField] private QuixilverASetpoint groundAlgae;
        [SerializeField] private QuixilverASetpoint lollipopAlgae;
        [SerializeField] private QuixilverASetpoint climbPrep;
        [SerializeField] private QuixilverASetpoint climbed;
        
        [Header("Scoring Forces")]
        [SerializeField] private Vector3 l1Force;
        [SerializeField] private Vector3 l2Force;
        [SerializeField] private Vector3 l3Force;
        [SerializeField] private Vector3 l4Force;
        [SerializeField] private Vector3 processorForce;
        [SerializeField] private Vector3 bargeForce;
        [SerializeField] private float bargeDelay;
        
        [Header("Funnel Audio")]
        [SerializeField] private AudioSource funnelAudioSource;
        [SerializeField] private AudioClip funnelClip;
        
        [Header("End Effector Audio")]
        [SerializeField] private AudioSource eeAudioSource;
        [SerializeField] private AudioClip eeClip;
        
        [Header("Auto Align Offsets")]
        [SerializeField] private Vector3 regularAutoAlignOffset;
        [SerializeField] private Vector3 autoAlignOffsetLeft;
        [SerializeField] private Vector3 autoAlignOffsetRight;
        [SerializeField] private Vector3 autoAlignOffsetLeftL4Prep;
        [SerializeField] private Vector3 autoAlignOffsetRightL4Prep;
        [SerializeField] private Vector3 algaeLeftFarAlign;
        [SerializeField] private Vector3 algaeRightFarAlign;
        [SerializeField] private Vector3 algaeLeftCloseAlign;
        [SerializeField] private Vector3 algaeRightCloseAlign;
        //
        [Header("Miscellaneous")]
        [SerializeField] private float l4SequenceElevDelay;
        [SerializeField] private float l4SequenceWristDelay;
        // [SerializeField] private GameObject coralStowStateGameObject;

        private float _elevatorTargetHeight;
        private float _armTargetAngle;
        private float _wristTargetAngle;
        private float _climbTargetAngle;
        private float _rightFunnelFlapAngle;
        private float _leftFunnelFlapAngle;
        private bool _handoff;
        private bool _isScoring;
        private ReefscapeAutoAlign _align;
        private Vector3 _blueReef;
        private Vector3 _redReef;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
        private float _l4DelayTimer;
        
        protected override void Start()
        {
            base.Start();
            
            arm.SetPid(armPid);
            wrist.SetPid(wristPid);
            climb.SetPid(climbPid);
            rightFunnelFlap.SetPid(funnelFlapPid);
            leftFunnelFlap.SetPid(funnelFlapPid);
            
            _elevatorTargetHeight = 0;
            _armTargetAngle = 0;
            _wristTargetAngle = 0;
            _climbTargetAngle = 0;
            _rightFunnelFlapAngle = 0;
            _leftFunnelFlapAngle = 0;
            _handoff = true;
            _isScoring = false;
            _l4DelayTimer = 0f;
            
            _blueReef = GameObject.Find("BlueReef").transform.position;
            _redReef = GameObject.Find("RedReef").transform.position;

            _align = GetComponent<ReefscapeAutoAlign>();
            
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            
            _coralController.gamePieceStates = new[]
            {
                funnelStowState,
                coralStowState
            };
            _coralController.intakes.Add(coralIntake);
            
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
            
            _algaeController.gamePieceStates = new[]
            {
                algaeStowState
            };
            _algaeController.intakes.Add(algaeIntake);

            funnelAudioSource.clip = funnelClip;
            funnelAudioSource.loop = true;
            funnelAudioSource.playOnAwake = false;
            
            eeAudioSource.clip = eeClip;
            eeAudioSource.loop = true;
            eeAudioSource.playOnAwake = false;
        }

        private IEnumerator PlacePiece()
        {
            if (!_isScoring)
            {
                Vector3 force = GetLevelByState() switch
                {
                    1 => l1Force,
                    2 => l2Force,
                    3 => l3Force,
                    4 => l4Force,
                    5 => processorForce,
                    6 => bargeForce,
                    _ => new Vector3(0, 0, 6)
                };

                if (GetLevelByState() == 6)
                {
                    yield return new WaitForSeconds(bargeDelay);
                }
                
                // foreach (var roller in endEffectorRollers)
                // {
                //     roller.SetAngularVelocity(900f);
                // }
                _isScoring = true;

                if (CoralAtStow(coralStowState)) _coralController.ReleaseGamePieceWithForce(force);
                _algaeController.ReleaseGamePieceWithForce(force);
                
                yield return new WaitForSeconds(0.5f);
                
                // foreach (var roller in endEffectorRollers)
                // {
                //     roller.SetAngularVelocity(0);
                // }
                _isScoring = false;
                if (CurrentSetpoint == ReefscapeSetpoints.Place)
                {
                    yield return new WaitUntil(() => DistanceToReef(GetClosestReef()) > 1.5f);
                    if (CurrentSetpoint == ReefscapeSetpoints.Place) SetState(ReefscapeSetpoints.Stow);
                }
            }
        }

        private void SetSetpoint(QuixilverASetpoint setpoint)
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled) return;
            
            _elevatorTargetHeight = (CurrentSetpoint == ReefscapeSetpoints.L4 && (!ArmAtSetpoint(l4, 10f) || _l4DelayTimer < l4SequenceElevDelay)) ? stow.elevatorHeight : setpoint.elevatorHeight;
            _armTargetAngle = setpoint.armAngle;
            _wristTargetAngle = (CurrentSetpoint == ReefscapeSetpoints.L4 && (!ArmAtSetpoint(l4) || _l4DelayTimer < l4SequenceWristDelay)) ? (ArmAtSetpoint(l4, 10f) ? -40f : 90f - Mathf.Repeat(-arm.GetSingleAxisAngle(JointAxis.X), 360f)) : setpoint.wristAngle;
            _climbTargetAngle = setpoint.climbAngle;
            _rightFunnelFlapAngle = 0f;
            _leftFunnelFlapAngle = 0f;
        }

        private void UpdateSetpoints()
        {
            climb.SetTargetAngle(_climbTargetAngle).withAxis(JointAxis.X).flipDirection().noWrap(270f);
            elevator.SetTarget(_elevatorTargetHeight);
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).flipDirection();
            wrist.SetTargetAngle(_wristTargetAngle).withAxis(JointAxis.X).flipDirection();
            rightFunnelFlap.SetTargetAngle(_rightFunnelFlapAngle).withAxis(JointAxis.X);
            leftFunnelFlap.SetTargetAngle(_leftFunnelFlapAngle).withAxis(JointAxis.X);
        }

        private void LateUpdate()
        {
            arm.UpdatePid((CurrentSetpoint == ReefscapeSetpoints.L4) ? armL4Pid : armPid);
            wrist.UpdatePid((CurrentSetpoint == ReefscapeSetpoints.L4 && ArmAtSetpoint(l4, 8f)) ? wristL4AvoidancePid : wristPid);
            // wrist.UpdatePid(wristPid);
            climb.UpdatePid(climbPid);
            rightFunnelFlap.UpdatePid(funnelFlapPid);
            leftFunnelFlap.UpdatePid(funnelFlapPid);
        }
        
        private float DistanceToReef(Vector3 reefPos)
        {
            return Mathf.Sqrt(Mathf.Pow(transform.position.x - reefPos.x, 2) + Mathf.Pow(transform.position.z - reefPos.z, 2));
        }
        
        private Vector3 GetClosestReef()
        {
            return DistanceToReef(_blueReef) < DistanceToReef(_redReef) ? _blueReef : _redReef;
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
                case ReefscapeSetpoints.Processor:
                    return 5;
                case ReefscapeSetpoints.Barge:
                    return 6;
                case ReefscapeSetpoints.LowAlgae:
                    return 7;
                case ReefscapeSetpoints.HighAlgae:
                    return 8;
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
                case ReefscapeSetpoints.Processor:
                    return 5;
                case ReefscapeSetpoints.Barge:
                    return 6;
                case ReefscapeSetpoints.LowAlgae:
                    return 7;
                case ReefscapeSetpoints.HighAlgae:
                    return 8;
            }

            return 0;
        }
        
        private bool CoralAtStow(GamePieceState stowState)
        {
            return _coralController.currentStateNum == stowState.stateNum && _coralController.atTarget;
        }
        
        private bool AtSetpoint(QuixilverASetpoint stp)
        {
            return
                Utils.InAngularRange(Mathf.Repeat(-arm.GetSingleAxisAngle(JointAxis.X), 360f), Mathf.Repeat(stp.armAngle, 360f), 2f) &&
                                     Utils.InAngularRange(Mathf.Repeat(-wrist.GetSingleAxisAngle(JointAxis.X), 360f), Mathf.Repeat(stp.wristAngle, 360f), 2f);
        }
        
        private bool ArmAtSetpoint(QuixilverASetpoint stp, float tolerance = 4f)
        {
            return
                Utils.InAngularRange(Mathf.Repeat(-arm.GetSingleAxisAngle(JointAxis.X), 360f), Mathf.Repeat(stp.armAngle, 360f), tolerance);
        }
    
        private bool AtSetpoint()
        {
            return
                Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _armTargetAngle, 2f) &&
                Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), _wristTargetAngle, 2f);
        }


        private void FixedUpdate()
        {
            bool hasCoral = _coralController.HasPiece();
            bool hasAlgae = _algaeController.HasPiece();
            
            coralBlocker.enabled = (!hasCoral || _coralController.atTarget) && (CurrentRobotMode != ReefscapeRobotMode.Coral || !IntakeAction.IsPressed() || hasAlgae);
            // coralBlocker.enabled = (!hasCoral || CoralAtStow(coralStowState)) && !(IntakeAction.IsPressed());

            if (!hasCoral) _handoff = false;
            
            if (CoralAtStow(funnelStowState) && CurrentRobotMode == ReefscapeRobotMode.Coral && AtSetpoint(intake))
            {
                _handoff = true;
            }

            if (ArmAtSetpoint(l4, 10f)) _l4DelayTimer += Time.fixedDeltaTime;
            else _l4DelayTimer = 0f;
            
            UpdateAudio();
            UpdateSetpoints();
            
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled) return;
            
            _coralController.SetTargetState(_handoff ? coralStowState : funnelStowState);
            _algaeController.SetTargetState(algaeStowState);
            
            if (!IntakeAction.IsPressed() && !hasAlgae && !hasCoral)
            {
                _algaeController.RequestIntake(algaeIntake, false);
                _coralController.RequestIntake(coralIntake, false);
            }

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(CoralAtStow(funnelStowState) || (hasCoral && _coralController.currentStateNum == coralStowState.stateNum && !_coralController.atTarget) ? intake : stow);
                    break;
                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint((CoralAtStow(funnelStowState) || _coralController.currentStateNum == coralStowState.stateNum) ? intake : stow);
                    }
                    else
                    {
                        SetSetpoint(groundAlgae);
                    }
                    
                    
                    _coralController.RequestIntake(coralIntake, !hasCoral && !hasAlgae && CurrentRobotMode == ReefscapeRobotMode.Coral);
                    _algaeController.RequestIntake(algaeIntake, !hasCoral && !hasAlgae && CurrentRobotMode == ReefscapeRobotMode.Algae);
                    break;
                case ReefscapeSetpoints.Place:
                    if (LastSetpoint == ReefscapeSetpoints.Barge)
                    {
                        SetSetpoint(bargePlace);
                    }
                    
                    StartCoroutine(PlacePiece());
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(CoralAtStow(coralStowState) ? l1 : intake);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(lollipopAlgae);
                    
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(CoralAtStow(coralStowState) ? l2 : intake);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgae);
                    
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(CoralAtStow(coralStowState) ? l3 : intake);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgae);
                    
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(CoralAtStow(coralStowState) ? l4 : intake);
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
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    break;
            }
            
            UpdateRollers();
            UpdateAutoAlign();
        }

        private void UpdateAutoAlign()
        {
            var flip = false;
            if (GetActiveCamera().transform.eulerAngles.y < 180) flip = !flip;
            if (Math.Abs(transform.position.x) > 4.489323 && PlayerPrefs.GetInt("PerspectiveAutoAlign", 1) == 1) flip = !flip;
            if (transform.position.x > 0) flip = !flip;

            if (GetLevelByState() >= 7)
            {
                if (AutoAlignLeftAction.inProgress)
                {
                    if (IntakeAction.IsPressed()) _align.offset = flip ? algaeLeftCloseAlign : algaeRightCloseAlign;
                    else _align.offset = flip ? algaeLeftFarAlign : algaeRightFarAlign;
                }
                else
                {
                    if (IntakeAction.IsPressed()) _align.offset = flip ? algaeRightCloseAlign : algaeLeftCloseAlign;
                    else _align.offset = flip ? algaeRightFarAlign : algaeLeftFarAlign;
                }
            }
            else if ((GetLevelByState() == 4 && !AtSetpoint(l4)) || CurrentSetpoint == ReefscapeSetpoints.Stow) _align.offset = flip ? autoAlignOffsetLeftL4Prep : autoAlignOffsetRightL4Prep;
            else _align.offset = flip ? autoAlignOffsetLeft : autoAlignOffsetRight;
        }

        private void UpdateRollers()
        {
            if (CurrentRobotMode == ReefscapeRobotMode.Coral && !_coralController.atTarget && IntakeAction.IsPressed() && !_algaeController.HasPiece())
            {
                foreach (var roller in funnelRollers)
                {
                    roller.ChangeAngularVelocity(800f);
                }
            }

            if (IntakeAction.IsPressed() && !_coralController.atTarget && !_algaeController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Coral)
            {
                foreach (var roller in endEffectorRollers)
                {
                    roller.ChangeAngularVelocity(500f);
                }
            }
            else if (IntakeAction.IsPressed() && !_algaeController.atTarget && !_coralController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                foreach (var roller in endEffectorRollers)
                {
                    roller.ChangeAngularVelocity(-500f);
                }
            }
            else if (_isScoring)
            {
                foreach (var roller in endEffectorRollers)
                {
                    roller.ChangeAngularVelocity(900);
                }
            }
        }

        private void UpdateAudio()
        {
            // EE Rollers
            float endEffectorRollerSpeed = Mathf.Max(new float[]
            {
                Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
                Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
                Mathf.Abs(endEffectorRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
            });
            if (endEffectorRollerSpeed > 5 && !eeAudioSource.isPlaying)
            {
                eeAudioSource.Play();
            }
            else if (endEffectorRollerSpeed <= 5 && eeAudioSource.isPlaying)
            {
                eeAudioSource.Stop();
            }
        
            // Funnel Rollers
            float funnelRollerSpeed = Mathf.Max(new float[]
            {
                Mathf.Abs(funnelRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.x),
                Mathf.Abs(funnelRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.y),
                Mathf.Abs(funnelRollers[0].gameObject.GetComponent<Rigidbody>().angularVelocity.z)
            });
            if (funnelRollerSpeed > 5 && !funnelAudioSource.isPlaying)
            {
                funnelAudioSource.Play();
            }
            else if (funnelRollerSpeed <= 5 && funnelAudioSource.isPlaying)
            {
                funnelAudioSource.Stop();
            }
        }
    }
}