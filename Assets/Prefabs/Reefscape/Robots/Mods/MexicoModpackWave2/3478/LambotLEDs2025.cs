using System.Collections.Generic;
using Games.Reefscape.Enums;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Lambot
{
    /// <summary>
    /// Lambot-specific LED controller, ported from the behavior described for the real robot's LED code
    /// (screenshots of alignToReef(), the coral/algae idle color if/else, and updateHangerModeCommand()) plus
    /// the plain-text summary given alongside them. Three independently-lit sections, matching the real
    /// code's Section.Left / Section.Edges / Section.Center - wired the same multi-material way
    /// StuyPulseLEDController wires its optional left/right accent strips, except here all three are always
    /// separate (no falling back to a shared strip) since this robot always has three physical strips.
    ///
    /// Priority (highest first), same top-down "first match wins" idiom as StuyPulseLEDController/ReefscapeLEDS:
    ///   1. Disabled -> alliance color, all three sections (no rainbow/scroll asset was described for this
    ///      robot, so this borrows StuyPulseLEDController's disabled convention instead of inventing one).
    ///   2. Hang mode -> Aqua, all three sections, overriding everything else. The real code's
    ///      updateHangerModeCommand() is actually more nuanced than a flat color (Purple while the toggle is
    ///      first switched on, then Aqua-if-coral/Blue-if-not once switched back off) and only touches
    ///      Section.Center - but the plain-text summary just says "aqua on hang mode" as a single state, so
    ///      that's what's implemented. It's also not clear MoSim exposes a matching "hangerModeEnabled"
    ///      toggle at all, so this is driven off ReefscapeRobotBase.CurrentSetpoint == Climb/Climbed instead
    ///      (the same setpoints StuyPulseLEDController/ReefscapeLEDS already use for their own climb states),
    ///      which is the closest grounded equivalent. If Lambot's actual hang toggle needs to be wired up
    ///      differently, or the Purple/two-tone behavior is wanted after all, swap the isHanging check below.
    ///   3. Auto-aligning to reef (Left + Edges only, Center keeps showing the coral/algae mode color from #4
    ///      the whole time - the real alignToReef() only ever touches Left/Edges, never Center): red while
    ///      still far from the target, yellow while closing in, green once at the target. The real code's
    ///      progression is driven by a one-shot RunOnce command (Red at the start of the command, then
    ///      Yellow/Green in finallyDo based on a `pidDrive.finishedCorrectly()` this project doesn't expose) -
    ///      ported here as a continuous distance-based gradient using ReefscapeAutoAlign.getDistance(), the
    ///      same align-distance API RoboGymLights already reads from, so it updates live instead of only at
    ///      the start/end of the align command.
    ///   4. Otherwise, all three sections show the coral/algae mode color (Blue for coral, Magenta for
    ///      algae) - Left/Edges mirror Center here so the whole robot reads as one consistent color when not
    ///      actively aligning, rather than sitting dark. NOTE: one of the screenshots (the isCoralMode
    ///      if/else) shows Green for coral mode rather than Blue, but its syntax was a broken if/else/else
    ///      with no matching if for the last branch, and the accompanying plain-text summary was explicit
    ///      ("blue/magenta for coral/algae mode") - so Blue/Magenta is what's implemented. Swap
    ///      coralModeColor below if Green was actually intended.
    /// </summary>
    public class LambotLEDs2025 : MonoBehaviour
    {
        [Header("LED Surfaces")]
        [Tooltip("Section.Left - drag every GameObject (with a Renderer) that makes up this strip.")]
        [SerializeField] private GameObject[] leftLeds;
        [Tooltip("Section.Edges - drag every GameObject (with a Renderer) that makes up this strip.")]
        [SerializeField] private GameObject[] edgesLeds;
        [Tooltip("Section.Center - drag every GameObject (with a Renderer) that makes up this strip.")]
        [SerializeField] private GameObject[] centerLeds;

        [Tooltip("The generated Shader asset from Assets/Materials/LEds/LEDs.shadergraph - same one GRRLights/LEDStripController/StuyPulseLEDController use. If left empty, this clones whatever material is already on each LED mesh instead.")]
        [SerializeField] private Shader shaderGraphShader;

        [Header("Intensity")]
        [Tooltip("Brightness for every state here - none of the described scenarios call for blinking or a dim/off state, so this is the single intensity used everywhere.")]
        [SerializeField] private float intensity = 150f;

        [Header("Reef Align (Left + Edges)")]
        [Tooltip("Reference to this robot's auto-align component (same field RoboGymLights uses) for getDistance().")]
        [SerializeField] private ReefscapeAutoAlign align;
        [Tooltip("At or beyond this distance from the target, Left/Edges show reefAlignFarColor (red).")]
        [SerializeField] private float alignFarDistance = 1.0f;
        [Tooltip("At or below this distance from the target, Left/Edges show reefAlignAtTargetColor (green). Between this and alignFarDistance shows reefAlignInProgressColor (yellow).")]
        [SerializeField] private float alignAtTargetDistance = 0.1f;
        [SerializeField] private Color reefAlignFarColor = Color.red;
        [SerializeField] private Color reefAlignInProgressColor = Color.yellow;
        [SerializeField] private Color reefAlignAtTargetColor = Color.green;

        [Header("Coral / Algae Mode (all sections when idle)")]
        [SerializeField] private Color coralModeColor = Color.blue;
        [SerializeField] private Color algaeModeColor = Color.magenta;

        [Header("Hang Mode (all sections, overrides everything)")]
        [SerializeField] private Color hangModeColor = new Color(0f, 1f, 1f); // aqua

        [Header("Disabled (all sections)")]
        [SerializeField] private Color disabledColorBlue = Color.blue;
        [SerializeField] private Color disabledColorRed = Color.red;

        private ReefscapeRobotBase _base;

        private Material _leftMaterial;
        private Material _edgesMaterial;
        private Material _centerMaterial;

        private readonly Dictionary<Color, Texture2D> _solidTextures = new();

        private void Start()
        {
            _base = GetComponent<ReefscapeRobotBase>();
            if (align == null) align = GetComponent<ReefscapeAutoAlign>();

            _leftMaterial = BuildSharedMaterial(leftLeds);
            _edgesMaterial = BuildSharedMaterial(edgesLeds);
            _centerMaterial = BuildSharedMaterial(centerLeds);
        }

        // Same idea as StuyPulseLEDController.BuildSharedMaterial: one shared, runtime-instanced material
        // assigned across every renderer in the group.
        private Material BuildSharedMaterial(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0) return null;

            Material shared = null;
            foreach (var obj in objects)
            {
                if (obj == null || !obj.TryGetComponent<Renderer>(out var renderer)) continue;

                shared ??= shaderGraphShader != null ? new Material(shaderGraphShader) : new Material(renderer.sharedMaterial);
                renderer.material = shared;
            }

            return shared;
        }

        private void Update()
        {
            if (_base == null) return;

            // 1. Disabled - alliance color across the whole robot.
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                SetAll(_base.Alliance == Alliance.Red ? disabledColorRed : disabledColorBlue);
                return;
            }

            // 2. Hang mode - overrides align and mode colors on every section. See class comment for why
            // this reads CurrentSetpoint instead of a hangerModeEnabled-style toggle.
            var isHanging = _base.CurrentSetpoint == ReefscapeSetpoints.Climb ||
                             _base.CurrentSetpoint == ReefscapeSetpoints.Climbed;
            if (isHanging)
            {
                SetAll(hangModeColor);
                return;
            }

            var modeColor = _base.CurrentRobotMode == ReefscapeRobotMode.Coral ? coralModeColor : algaeModeColor;

            // 3. Auto-aligning to reef - Left/Edges only, red -> yellow -> green as the robot closes in.
            var aligning = _base.AutoAlignLeftAction.IsPressed() || _base.AutoAlignRightAction.IsPressed();
            if (aligning && align != null)
            {
                var distance = align.getDistance();
                var alignColor = distance <= alignAtTargetDistance ? reefAlignAtTargetColor
                    : distance >= alignFarDistance ? reefAlignFarColor
                    : reefAlignInProgressColor;

                Set(_leftMaterial, alignColor);
                Set(_edgesMaterial, alignColor);
            }
            else
            {
                // 4. Not aligning - Left/Edges mirror the same mode color as Center instead of sitting dark.
                Set(_leftMaterial, modeColor);
                Set(_edgesMaterial, modeColor);
            }

            // Center always shows the coral/algae mode color unless hang mode already returned above.
            Set(_centerMaterial, modeColor);
        }

        private void SetAll(Color color)
        {
            Set(_leftMaterial, color);
            Set(_edgesMaterial, color);
            Set(_centerMaterial, color);
        }

        // Same idea as StuyPulseLEDController/GRRLights' Set(): _X/_Y stay at 0 (unused scroll offset),
        // _intensity drives brightness, _Texture2D gets a baked solid-color texture.
        private void Set(Material material, Color color)
        {
            if (material == null) return;

            material.SetFloat("_X", 0f);
            material.SetFloat("_Y", 0f);
            material.SetFloat("_intensity", intensity);
            material.SetTexture("_Texture2D", GetSolidTexture(color));
        }

        // Bakes (and caches) a tiny solid-color texture the first time a given color is used - identical
        // approach to StuyPulseLEDController.GetSolidTexture.
        private Texture2D GetSolidTexture(Color color)
        {
            if (_solidTextures.TryGetValue(color, out var existing)) return existing;

            var texture = new Texture2D(4, 4) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[texture.width * texture.height];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();

            _solidTextures[color] = texture;
            return texture;
        }
    }
}
