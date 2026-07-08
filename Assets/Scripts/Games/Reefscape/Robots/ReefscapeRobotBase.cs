using System;
using Games.Reefscape.Enums;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.GamePieceSystem;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Games.Reefscape.Robots
{
    public class ReefscapeRobotBase : RobotBase
    {
        [field: NonSerialized]
        protected new ReefscapeRobotGamePieceController RobotGamePieceController { get; private set; }

        public InputAction L4Action { get; private set; }
        public InputAction L3Action { get; private set; }
        public InputAction L2Action { get; private set; }
        public InputAction L1Action { get; private set; }
        public InputAction ClimbAction { get; private set; }

        public InputAction AutoAlignLeftAction { get; private set; }
        public InputAction AutoAlignRightAction { get; private set; }

        public InputAction RobotModeToggleAction { get; private set; }
        public InputAction IntakeModeToggleAction { get; private set; }
        public InputAction RobotSpecialAction { get; private set; }

        [field: SerializeField] public ReefscapeSetpoints CurrentSetpoint { get; private set; }
        protected ReefscapeSetpoints LastSetpoint { get; private set; }
        public bool IsIntaking => IntakeAction.IsPressed();

        public ReefscapeRobotMode CurrentRobotMode { get; private set; }
        public ReefscapeIntakeMode CurrentIntakeMode { get; private set; }
        [field: SerializeField] public CoralStationMode CurrentCoralStationMode { get; private set; }

        [SerializeField] protected bool superCycler = false;

        private GameObject _targetReef;
        protected bool FacingReef;

        private bool _hasCoral = true;
        private bool _hasAlgae;
        protected int HasCoralTrigger { get; set; }

        protected override void Awake()
        {
            base.Awake();

            RobotGamePieceController = GetComponent<ReefscapeRobotGamePieceController>();
            if (RobotGamePieceController == null)
            {
                Debug.LogError("ReefscapeRobotGamePieceController component not found on the robot!");
            }

            SetupInputActions();
        }

        protected override void Start()
        {
            base.Start();

            var coralStations = FindObjectsByType(typeof(CoralStation), FindObjectsSortMode.None);
            if (coralStations.Length == 0)
            {
                Debug.LogError("No CoralStation found in the scene!");
            }

            foreach (var coralStation in coralStations)
            {
                var station = (CoralStation)coralStation;
                if (Alliance == Alliance.Blue && station.Alliance == Alliance.Blue ||
                    Alliance == Alliance.Red && station.Alliance == Alliance.Red)
                {
                    station.Robots.Add(this);
                }
            }

            _targetReef = Alliance == Alliance.Blue ? GameObject.Find("BlueReef") : GameObject.Find("RedReef");
        }

        private void SetupInputActions()
        {
            L4Action = InputActionMap.FindAction("L4");
            L3Action = InputActionMap.FindAction("L3");
            L2Action = InputActionMap.FindAction("L2");
            L1Action = InputActionMap.FindAction("L1");
            ClimbAction = InputActionMap.FindAction("Climb");

            AutoAlignLeftAction = InputActionMap.FindAction("AutoAlignLeft");
            AutoAlignRightAction = InputActionMap.FindAction("AutoAlignRight");

            RobotModeToggleAction = InputActionMap.FindAction("RobotModeToggle");
            IntakeModeToggleAction = InputActionMap.FindAction("IntakeModeToggle");
            RobotSpecialAction = InputActionMap.FindAction("RobotSpecial");
        }

        protected override void Update()
        {
            base.Update();

            if (RobotModeToggleAction.triggered && !RightStickModifierAction.IsPressed())
            {
                CurrentRobotMode = CurrentRobotMode switch
                {
                    ReefscapeRobotMode.Coral => ReefscapeRobotMode.Algae,
                    ReefscapeRobotMode.Algae => ReefscapeRobotMode.Coral,
                    _ => ReefscapeRobotMode.Coral
                };
            }

            if (IntakeModeToggleAction.triggered &&
                RobotGamePieceController.GetPieceByName("Coral").currentStateNum == 0 &&
                !RightStickModifierAction.IsPressed())
            {
                CurrentIntakeMode = CurrentIntakeMode switch
                {
                    ReefscapeIntakeMode.L1 => ReefscapeIntakeMode.Normal,
                    ReefscapeIntakeMode.Normal => ReefscapeIntakeMode.L1,
                    _ => ReefscapeIntakeMode.Normal
                };
            }

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                return;
            }

            if (AutoAlignLeftAction.triggered || AutoAlignRightAction.triggered)
            {
                CheckFacingReef();
            }

            if (IntakeAction.IsPressed() && CurrentSetpoint != ReefscapeSetpoints.HighAlgae &&
                CurrentSetpoint != ReefscapeSetpoints.LowAlgae && CurrentSetpoint != ReefscapeSetpoints.Stack)
            {
                CurrentSetpoint = ReefscapeSetpoints.Intake;
            }
            else if (OuttakeAction.triggered)
            {
                CurrentSetpoint = ReefscapeSetpoints.Place;
            }
            else if (L1Action.triggered)
            {
                if (RobotGamePieceController.GetPieceByName("Coral").currentStateNum == 0 &&
                    RobotGamePieceController.GetPieceByName("Algae").currentStateNum == 0)
                {
                    if (CurrentSetpoint == ReefscapeSetpoints.Stow && CurrentRobotMode == ReefscapeRobotMode.Algae)
                    {
                        CurrentSetpoint = ReefscapeSetpoints.Stack;
                    }
                    else
                    {
                        CurrentSetpoint = ReefscapeSetpoints.Stow;
                    }
                }
                else if (RobotGamePieceController.GetPieceByName("Coral").currentStateNum > 0 ||
                         RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0)
                {
                    CurrentSetpoint = RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0
                        ? ReefscapeSetpoints.Processor
                        : ReefscapeSetpoints.L1;
                }
            }
            else if (L2Action.triggered)
            {
                CheckFacingReef();
                if (CurrentSetpoint is ReefscapeSetpoints.L2 or ReefscapeSetpoints.LowAlgae)
                {
                    CurrentSetpoint = ReefscapeSetpoints.Stow;
                }
                else
                {
                    var isTrue = RobotGamePieceController.GetPieceByName("Coral").currentStateNum == 0 ||
                                 (CurrentRobotMode == ReefscapeRobotMode.Algae && superCycler);
                    CurrentSetpoint = isTrue ? ReefscapeSetpoints.LowAlgae : ReefscapeSetpoints.L2;
                }
            }
            else if (L3Action.triggered)
            {
                CheckFacingReef();
                if (CurrentSetpoint is ReefscapeSetpoints.L3 or ReefscapeSetpoints.HighAlgae)
                {
                    CurrentSetpoint = ReefscapeSetpoints.Stow;
                }
                else
                {
                    var isTrue = RobotGamePieceController.GetPieceByName("Coral").currentStateNum == 0 ||
                                 (CurrentRobotMode == ReefscapeRobotMode.Algae && superCycler);
                    CurrentSetpoint = isTrue ? ReefscapeSetpoints.HighAlgae : ReefscapeSetpoints.L3;
                }
            }
            else if (L4Action.triggered)
            {
                CheckFacingReef();
                if (CurrentSetpoint is ReefscapeSetpoints.L4 or ReefscapeSetpoints.Barge)
                {
                    CurrentSetpoint = ReefscapeSetpoints.Stow;
                }
                else if (RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0 ||
                         RobotGamePieceController.GetPieceByName("Coral").currentStateNum > 0)
                {
                    var isTrue = RobotGamePieceController.GetPieceByName("Coral").currentStateNum == 0 ||
                                 (CurrentRobotMode == ReefscapeRobotMode.Algae && superCycler);
                    var algaeSetpoint = RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0
                        ? ReefscapeSetpoints.Barge
                        : ReefscapeSetpoints.Stow;
                    CurrentSetpoint = isTrue ? algaeSetpoint : ReefscapeSetpoints.L4;
                }
            }
            else if (RobotSpecialAction.triggered)
            {
                CurrentSetpoint = CurrentSetpoint is ReefscapeSetpoints.RobotSpecial
                    ? ReefscapeSetpoints.Stow
                    : ReefscapeSetpoints.RobotSpecial;
            }
            else if (ClimbAction.triggered && !RightStickModifierAction.IsPressed())
            {
                CurrentSetpoint = CurrentSetpoint switch
                {
                    ReefscapeSetpoints.Stow => ReefscapeSetpoints.Climb,
                    ReefscapeSetpoints.Climb => ReefscapeSetpoints.Climbed,
                    ReefscapeSetpoints.Climbed => ReefscapeSetpoints.Stow,
                    _ => CurrentSetpoint
                };
            }
            else if (
                (StowAction.IsPressed() &&
                 (CurrentSetpoint != ReefscapeSetpoints.Climb && CurrentSetpoint != ReefscapeSetpoints.Climbed)) ||
                (CurrentSetpoint == ReefscapeSetpoints.Intake && !IntakeAction.IsPressed()) ||
                (CurrentSetpoint is ReefscapeSetpoints.HighAlgae or ReefscapeSetpoints.LowAlgae
                     or ReefscapeSetpoints.Stack &&
                 !IntakeAction.IsPressed() && RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0) &&
                !RightStickModifierAction.IsPressed())
            {
                CurrentSetpoint = ReefscapeSetpoints.Stow;
            }


            if (CurrentSetpoint != LastSetpoint && CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                LastSetpoint = CurrentSetpoint;
                SetState(CurrentSetpoint);
            }
            
            HandleRumble();
        }

        protected void SetState(ReefscapeSetpoints setpoint)
        {
            LastSetpoint = CurrentSetpoint;
            CurrentSetpoint = setpoint;
        }

        protected void SetRobotMode(ReefscapeRobotMode mode)
        {
            CurrentRobotMode = mode;
        }

        private void CheckFacingReef()
        {
            var toReefVector = (_targetReef.transform.position - transform.position).normalized;
            var robotForwardVector = transform.forward.normalized;
            var angle = Vector3.Dot(robotForwardVector, toReefVector);
            FacingReef = angle > 0.0f;
        }

        public bool GetFacingReef()
        {
            return FacingReef;
        }

        private void HandleRumble()
        {
            if (!DoControllerRumble)
            {
                return;
            }

            switch (_hasCoral)
            {
                case false when (HasCoralTrigger > 0
                    ? RobotGamePieceController.GetPieceByName("Coral").currentStateNum == HasCoralTrigger
                        : RobotGamePieceController.GetPieceByName("Coral").currentStateNum > 0):
                    _hasCoral = true;
                    OnRumbleTrigger.Invoke();
                    break;
                case true when RobotGamePieceController.GetPieceByName("Coral").currentStateNum == 0:
                    _hasCoral = false;
                    break;
            }

            switch (_hasAlgae)
            {
                case false when RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0:
                    _hasAlgae = true;
                    OnRumbleTrigger.Invoke();
                    break;
                case true when RobotGamePieceController.GetPieceByName("Algae").currentStateNum == 0:
                    _hasAlgae = false;
                    break;
            }
        }
    }
}