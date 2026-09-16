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
using OffvertureRobot = Prefabs.Reefscape.Robots.Mods.Offverture._7421RMX.Offverture;

namespace Prefabs.Reefscape.Robots.Mods.Offverture
{
    /// <summary>
    /// Offverture's align-offset brain, replacing the frontLeft/frontRight/backLeft/backRight fields and
    /// AutoAlignnnn()/SetAlignOffsets() logic that used to live directly on Offverture. Adapted from
    /// StuyPulseAutoAlign/LambotAutoAlign's offset-asset pattern (see l1offset/frontLeftOffset/etc. on
    /// StuyPulseAutoAlign, offset/bargeOffset on LambotAutoAlign) - but scaled down to only what Offverture
    /// actually needs. Unlike those two, OverAlign does NOT reimplement reef-face finding, barge-scorer
    /// detection, reef avoidance, or PID driving - Offverture already delegates all of that to the
    /// framework's ReefscapeAutoAlign component, the same way it did before this change. OverAlign's only
    /// job is deciding which AutoAlignOffverset asset applies given the robot's current state and writing
    /// it into ReefscapeAutoAlign's offset/rotation every FixedUpdate, completely independently of
    /// Offverture's own FixedUpdate (same independent-component pattern LambotAutoAlign uses, reading
    /// straight off ReefscapeRobotBase instead of having the robot script push values into it).
    ///
    /// Two offset groups, each with its own front/back x left/right assets so tuning is per-branch same
    /// as before:
    /// - Reef (L2/L3/L4 branch scoring) - frontLeft/frontRight/backLeft/backRight, same as the old fields.
    /// - LowerAlign (L1) - StuyPulse switches to a separate align (l1offset) the moment it enters L1 mode,
    ///   because froggy/L1 scoring sits at a different spot on the reef face than the branches. Offverture
    ///   only has one way to score L1 (the ground intake, no froggy arm / end effector option like Stuy), so
    ///   LowerAlign is used for BOTH CurrentIntakeMode.L1 (L1Mode) and CurrentSetpoint.L1 (the L1 setpoint
    ///   itself) - either one is enough to switch off the reef offsets above.
    ///
    /// There is intentionally no human-player-station align here. Offverture's coral intake is a ground
    /// intake, not a human-station pickup, so that StuyPulse/Lambot case doesn't apply and was left out.
    /// Barge (algae) align has likewise been removed - OverAlign now only ever drives coral offsets.
    ///
    /// Reef branch align (L2/L3/L4, not L1) also gets an "align prep" step, same idea as LAMBOT's
    /// initialAutoAlignOffset -> l2AutoAlignOffset/l3AutoAlignOffset/l4AutoAlignOffset switch, but without
    /// needing a separate prep asset per side: while the arm hasn't yet reached its target angle for the
    /// setpoint it's heading to (Offverture.armAtTargetAngle() is false), the same branch offset
    /// (frontLeft/frontRight/backLeft/backRight) is used but with alignPrepZOffset (8 by default, matching
    /// the z offset reef aligns already use) subtracted from its z - then the untouched branch offset
    /// takes over once the arm settles. LowerAlign (L1) is intentionally excluded, same as the exclusion
    /// note above.
    /// </summary>
    public class OverAlign : MonoBehaviour
    {
        [Header("Reef Align Offsets (L2/L3/L4)")]
        [SerializeField] private AutoAlignOffverset frontLeft;
        [SerializeField] private AutoAlignOffverset frontRight;
        [SerializeField] private AutoAlignOffverset backLeft;
        [SerializeField] private AutoAlignOffverset backRight;

        [Header("Align Prep (Reef Only, L2/L3/L4)")]
        [Tooltip("How much to subtract from the z offset of the branch offset (frontLeft/frontRight/backLeft/backRight) while the arm hasn't yet reached its target angle - reef aligns use 8 for z, so 8 pulls the prep point back by that same amount instead of needing a separate prep asset per side. Not used for L1 (LowerAlign).")]
        [SerializeField] private float alignPrepZOffset = 8f;

        [Header("Lower Align Offsets (L1)")]
        [Tooltip("Used instead of the reef offsets above whenever the robot is in L1 intake mode (CurrentIntakeMode.L1) or targeting the L1 setpoint (CurrentSetpoint.L1).")]
        [SerializeField] private AutoAlignOffverset lowerFrontLeft;
        [SerializeField] private AutoAlignOffverset lowerFrontRight;
        [SerializeField] private AutoAlignOffverset lowerBackLeft;
        [SerializeField] private AutoAlignOffverset lowerBackRight;

        private ReefscapeRobotBase _robot;
        private OffvertureRobot _offvertureRobot;
        private ReefscapeAutoAlign _autoAlign;

        private void Awake()
        {
            _robot = GetComponent<ReefscapeRobotBase>();
            _offvertureRobot = GetComponent<OffvertureRobot>();
            _autoAlign = GetComponent<ReefscapeAutoAlign>();
        }

        private void FixedUpdate()
        {
            if (_robot == null || _autoAlign == null)
            {
                return;
            }

            bool facingReef = _robot.GetFacingReef();

            bool leftPressed = _robot.AutoAlignLeftAction.IsPressed();
            bool rightPressed = _robot.AutoAlignRightAction.IsPressed();

            if (!leftPressed && !rightPressed)
            {
                return;
            }

            if (_robot.CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                return;
            }

            // LowerAlign: tanto en L1Mode (CurrentIntakeMode.L1) como en el setpoint L1 - Offverture solo
            // puede hacer L1 con el intake de piso, a diferencia de Stuy que puede usar el froggy arm o el
            // end effector. Sin align prep aquí - eso es solo para L2/L3/L4.
            bool useLowerAlign = _robot.CurrentIntakeMode == ReefscapeIntakeMode.L1 ||
                                  _robot.CurrentSetpoint == ReefscapeSetpoints.L1;

            if (useLowerAlign)
            {
                if (leftPressed)
                {
                    ApplyOffset(facingReef ? lowerBackLeft : lowerFrontLeft);
                }
                else if (rightPressed)
                {
                    ApplyOffset(facingReef ? lowerBackRight : lowerFrontRight);
                }
                return;
            }

            // Align prep: mientras el brazo todavía no llega a su ángulo objetivo (subiendo hacia el
            // setpoint de la rama), se usa el mismo offset final de la rama pero con el z offset
            // reducido en alignPrepZOffset (8 por default, igual que el z de los aligns de coral) en
            // vez de necesitar un asset de prep aparte por lado.
            bool armReady = _offvertureRobot == null || _offvertureRobot.armAtTargetAngle();

            if (leftPressed)
            {
                ApplyOffset(facingReef ? backLeft : frontLeft, armReady ? 0f : -alignPrepZOffset);
            }
            else if (rightPressed)
            {
                ApplyOffset(facingReef ? backRight : frontRight, armReady ? 0f : -alignPrepZOffset);
            }
        }

        private void ApplyOffset(AutoAlignOffverset alignment, float zDelta = 0f)
        {
            if (alignment == null)
            {
                return;
            }

            _autoAlign.offset = new Vector3(alignment.xOffset, alignment.yOffset, alignment.zOffset + zDelta);
            _autoAlign.rotation = alignment.Rotation;
        }
    }
}