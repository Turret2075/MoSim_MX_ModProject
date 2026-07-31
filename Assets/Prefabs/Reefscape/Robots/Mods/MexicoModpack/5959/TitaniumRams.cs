using System.Collections;
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
using MoSimLib;
using UnityEngine;
 
/*
Although this mod from team 5959 was mainly made by Turret2075,
the code is not made entirely by me.

The initial code was made by Turret2075, but it was broken.
Eback fixed it for me, but after a lot of errors and the guide not helping me much, I ragequit the project for like half a year.
I came back to the project and fixed some more things, so I could do the first release of the modpack.

It was rated silver and had some bugs, but it was playable. But still not enough for gold.
For the next release along with the newest adition, Overture, I added the real funnel and the algae descorer, but it just got rated gold.

I really dont know how to code C# and I dont know how to use Unity, but I dont seek to fully learn it. I only know a bit of Python and Java for FRC.
I got studies, my music path and also need to help and contribute to my actual FRC team, i want us 5959 to be a better team in Mexico and go to Worlds.

I just wanna make a modpack consisting of Mexican FRC team for everyone to play, as there is not any Mex team in the game.
So I was thinking of just giving the code to Claude AI so it can be completely tuned and optimized, but i doubted about AI fair use and originality...

I asked Beemi if he could do the rollers, but he was doing Builder mods at the time, guess im the only lifeless guy in the project.
In the end AI just made the rollers, but the rest of the code is made by the team behind this modpack.

If you're a MoSim mod reviewer and you are reading this, please understand what i wrote. If there is any problem with all of this, you can tell me in the Discord Ticket.
But if you are just visiting this, i hope you understand the story.


With love, Turret2075 from Team 5959 Titanium Rams <3.
*/

namespace Prefabs.Reefscape.Robots.Mods.MexicoModpack._5959
{
    public class TitaniumRams : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint algaeArm;
        [SerializeField] private GenericJoint algaeDescorerArm;
 
        [Header("PID")]
        [SerializeField] private PidConstants algaeArmPid;
        [SerializeField] private PidConstants algaeDescorerArmPid;
 
        [Header("Setpoints")]
        [SerializeField] private TitaniumRamsSetpoint stow;
        [SerializeField] private TitaniumRamsSetpoint AlgaeStow;
        [SerializeField] private TitaniumRamsSetpoint intake;        // coral intake pose
        [SerializeField] private TitaniumRamsSetpoint groundalgae;   // algae ground intake pose
        [SerializeField] private TitaniumRamsSetpoint processor;    // processor pose
        [SerializeField] private TitaniumRamsSetpoint l1;
        [SerializeField] private TitaniumRamsSetpoint l2;
        [SerializeField] private TitaniumRamsSetpoint l3;
        [SerializeField] private TitaniumRamsSetpoint descoreLowAlgae;
        [SerializeField] private TitaniumRamsSetpoint descoreHighAlgae;
 
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
 
        [Header("Arm Movement Audio")]
        [SerializeField] private AudioSource algaeArmAudioSource;
        [SerializeField] private AudioClip algaeArmMoveClip;
        [SerializeField] private AudioSource algaeDescorerAudioSource;
        [SerializeField] private AudioClip algaeDescorerMoveClip;
        [SerializeField] private float armAngleTolerance = 3f;
        
        [Header("Animation Joints (Wheels)")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private GenericAnimationJoint[] intakeWheelsReverse;
        [SerializeField] private GenericAnimationJoint[] algaeintakeWheels;
        [SerializeField] private GenericAnimationJoint[] algaeintakeWheelsReverse;
        [SerializeField] private float wheelIntakeSpeed = 1000f;
        [SerializeField] private float wheelIntakeSpeedReverse = -1000f;
 
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
 
        private float _elevatorTargetHeight;
        private float _algaeTargetAngle;
        private float _algaeDescorerTargetAngle;
        private bool _isScoring;
 
        protected override void Start()
        {
            base.Start();
 
            if (algaeArm != null) algaeArm.SetPid(algaeArmPid);
            if (algaeDescorerArm != null) algaeDescorerArm.SetPid(algaeDescorerArmPid);
 
            _elevatorTargetHeight = 0f;
            _algaeTargetAngle = 0f;
            _algaeDescorerTargetAngle = 0f;
            // Preload coral
            
            RobotGamePieceController.SetPreload(coralStowState);
 
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());
 
            algaeStallSource.clip = algaeStallAudio;
            algaeStallSource.loop = true;
            algaeStallSource.Stop();
            
            rollerSource.clip = intakeClip;
            rollerSource.loop = true;
            rollerSource.Stop();
 
            algaeArmAudioSource.clip = algaeArmMoveClip;
            algaeArmAudioSource.loop = true;
            algaeArmAudioSource.Stop();
 
            algaeDescorerAudioSource.clip = algaeDescorerMoveClip;
            algaeDescorerAudioSource.loop = true;
            algaeDescorerAudioSource.Stop();
 
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
            algaeDescorerArm.UpdatePid(algaeDescorerArmPid);
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

            _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && IntakeAction.IsPressed());  
            
 
 
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (hasAlgae)
                    {
                        SetSetpoint(AlgaeStow);
                    }
                    else {
                        SetSetpoint(stow);
                    }
                    break;
 
                case ReefscapeSetpoints.Intake:
                if (CurrentRobotMode == ReefscapeRobotMode.Coral || hasAlgae)
                {
                    SetSetpoint(intake);
                }
                else
                {
                    SetSetpoint(groundalgae);
                }
                _coralController.RequestIntake(coralIntake, CurrentRobotMode == ReefscapeRobotMode.Coral && IntakeAction.IsPressed() && !hasAlgae);
                              
                break;
                
                case ReefscapeSetpoints.Place:
                    if (OuttakeAction != null && OuttakeAction.triggered)
                    {
                        PlacePiece();
                        StartCoroutine(ScoreCoroutine());
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
                    StopAllIntakes();
                    break;
 
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processor);
                    StopAllIntakes();
                    break;
 
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(descoreLowAlgae);
                    StopAllIntakes();
                    break;
 
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(descoreHighAlgae);
                    StopAllIntakes();
                    break;
 
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    StopAllIntakes();
                    break;
            }
 
            UpdateRollers(hasCoral, hasAlgae, intakePressed);
 
            UpdateSetpoints();
            UpdateAudio();
            UpdateArmAudio();
        }
 
        private void UpdateRollers(bool hasCoral, bool hasAlgae, bool intakePressed)
        {
            // Mientras se está anotando (ver ScoreCoroutine), esta lógica no debe pisar
            // la velocidad que está aplicando la corrutina.
            if (_isScoring) return;
 
            // Rollers del efector de coral: giran solo en modo Coral, en el setpoint de
            // Intake, con el botón presionado y sin coral ya agarrado.
            bool wantCoralIntake = CurrentRobotMode == ReefscapeRobotMode.Coral
                                   && CurrentSetpoint == ReefscapeSetpoints.Intake
                                   && intakePressed && !hasCoral;
 
            if (wantCoralIntake)
            {
                foreach (var wheel in intakeWheels) wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.Y);
                foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(wheelIntakeSpeedReverse).useAxis(JointAxis.Y);
            }
            else
            {
                foreach (var wheel in intakeWheels) wheel.VelocityRoller(0).useAxis(JointAxis.Y);
                foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(0).useAxis(JointAxis.Y);
            }
 
            // Rollers del intake de algas: mecanismo aparte, independiente del de coral.
            // Se activa en modo Algae, ya sea en Intake (piso) o Stack, sin alga ya agarrada.
            bool wantAlgaeIntake = CurrentRobotMode == ReefscapeRobotMode.Algae
                                   && (CurrentSetpoint == ReefscapeSetpoints.Intake || CurrentSetpoint == ReefscapeSetpoints.Stack)
                                   && intakePressed && !hasAlgae;
 
            if (wantAlgaeIntake)
            {
                foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.X);
                foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(wheelIntakeSpeedReverse).useAxis(JointAxis.X);
            }
            else
            {
                foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(0).useAxis(JointAxis.X);
                foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(0).useAxis(JointAxis.X);
            }
        }
 
        private IEnumerator ScoreCoroutine()
        {
            _isScoring = true;
 
            // Igual que en Overture: al anotar, se invierte la dirección de los rollers
            // del mecanismo correspondiente durante medio segundo para expulsar la pieza.
            bool scoringAlgae = CurrentRobotMode == ReefscapeRobotMode.Algae;
            float speed = scoringAlgae ? -wheelIntakeSpeed : wheelIntakeSpeed;
            float speedReverse = scoringAlgae ? -wheelIntakeSpeedReverse : wheelIntakeSpeedReverse;
 
            float timer = 0f;
            while (timer < 0.5f)
            {
                if (scoringAlgae)
                {
                    foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(speed);
                    foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(speedReverse);
                }
                else
                {
                    foreach (var wheel in intakeWheels) wheel.VelocityRoller(speed);
                    foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(speedReverse);
                }
 
                timer += Time.deltaTime;
                yield return null;
            }
 
            foreach (var wheel in intakeWheels) wheel.VelocityRoller(0);
            foreach (var wheel in intakeWheelsReverse) wheel.VelocityRoller(0);
            foreach (var wheel in algaeintakeWheels) wheel.VelocityRoller(0);
            foreach (var wheel in algaeintakeWheelsReverse) wheel.VelocityRoller(0);
 
            _isScoring = false;
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
            if (_algaeController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, -1.2f));
            }
            else
            {
                if (_coralController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Coral)
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
            _algaeDescorerTargetAngle = setpoint.algaeDescorerArmAngle;
        }
 
        private void UpdateSetpoints()
        {
            if (elevator != null) elevator.SetTarget(_elevatorTargetHeight);
 
            // FIX: actually use the setpoint angle instead of forcing 0
            if (algaeArm != null)
                algaeArm.SetTargetAngle(_algaeTargetAngle).withAxis(JointAxis.X);
 
            if (algaeDescorerArm != null)
                algaeDescorerArm.SetTargetAngle(_algaeDescorerTargetAngle).withAxis(JointAxis.X);
        }
        private void UpdateArmAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (algaeArmAudioSource != null && algaeArmAudioSource.isPlaying) algaeArmAudioSource.Stop();
                if (algaeDescorerAudioSource != null && algaeDescorerAudioSource.isPlaying) algaeDescorerAudioSource.Stop();
                return;
            }
 
            // Pivot del algaeArm: suena mientras el angulo actual no coincide con el
            // setpoint objetivo (_algaeTargetAngle), independiente del descorer.
            if (algaeArm != null && algaeArmAudioSource != null)
            {
                bool algaeArmAtTarget = Utils.InAngularRange(algaeArm.GetSingleAxisAngle(JointAxis.X), _algaeTargetAngle, armAngleTolerance);
 
                if (!algaeArmAtTarget && !algaeArmAudioSource.isPlaying)
                {
                    algaeArmAudioSource.Play();
                }
                else if (algaeArmAtTarget && algaeArmAudioSource.isPlaying)
                {
                    algaeArmAudioSource.Stop();
                }
            }
 
            // Pivot del algaeDescorerArm: mecanismo aparte, su propia fuente y setpoint.
            if (algaeDescorerArm != null && algaeDescorerAudioSource != null)
            {
                bool descorerAtTarget = Utils.InAngularRange(algaeDescorerArm.GetSingleAxisAngle(JointAxis.X), _algaeDescorerTargetAngle, armAngleTolerance);
 
                if (!descorerAtTarget && !algaeDescorerAudioSource.isPlaying)
                {
                    algaeDescorerAudioSource.Play();
                }
                else if (descorerAtTarget && algaeDescorerAudioSource.isPlaying)
                {
                    algaeDescorerAudioSource.Stop();
                }
            }
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