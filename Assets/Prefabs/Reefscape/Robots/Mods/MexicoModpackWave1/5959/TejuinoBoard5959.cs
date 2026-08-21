using System.Collections.Generic;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.BaseClasses.GameManagement.TimerManagement;
using MoSimCore.Enums;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.Team5959._5959
{
    /// <summary>
    /// Port of team 5959's real LEDSubsystem.java to MoSim's shader-based LED strips.
    ///
    /// The real robot drives 27 individually addressable LEDs directly (AddressableLED/AddressableLEDBuffer),
    /// so it can set each pixel's raw RGB value every frame. MoSim's strips don't work that way - like
    /// GRRLights/LEDStripController/StuyPulseLEDController, all the meshes assigned to `leds` share ONE
    /// material with only _Texture2D/_X/_Y/_intensity as inputs, so a "color" here means baking a small
    /// solid-color Texture2D and an "animation" means scrolling a texture across the strip via _X - same
    /// idiom RoboGymLights (3950) uses for its disabled-state scroll.
    ///
    /// Behavior ported 1:1 from LEDSubsystem.periodic():
    ///   - Enabled, <= 20s left: solid green.
    ///   - Enabled, at 10/5/3/2/1 seconds left: green blinking, one flat Hz per threshold - no interpolation.
    ///     The real code's own accelerating-blink formula worked out to ~22-42Hz (above what a human eye can
    ///     even perceive as blinking, which is why an earlier literal port of it just looked solid), and a
    ///     Lerp between two Hz values turned out to still be too subtle to read reliably in practice. This
    ///     version is deliberately dumb instead: five hardcoded "if matchTime <= N seconds" checks, each
    ///     driving RoboGymLights' Time.time-modulo Blink() at its own fixed, independently-tunable Hz field
    ///     below. The real code's "off" phase isn't fully black (0,5,0) - ported here as dimIntensity instead
    ///     of 0 so the same near-black-but-not-quite behavior carries over.
    ///
    ///   - Disabled: rainbow. Unlike the runtime-baked solid-color textures used everywhere else in this
    ///     file, the rainbow strip is a plain SerializeField Texture (assign a rainbow/gradient asset in the
    ///     inspector) scrolled via _X over time - exactly RoboGymLights' own idiom for its disabled state
    ///     (`_material.SetFloat("_X", Time.time * 0.08f)`), just with a rainbow texture instead of RoboGym's
    ///     `disabled` texture.
    ///
    /// Match time comes from BaseTimerManager.Timer (found via ReefscapeTimerManager in the scene), which
    /// turns out to line up with the real code's matchTime almost exactly: ReefscapeTimerManager sets
    /// EndgameStartTime = 20f, i.e. the same 20-second threshold the real LEDSubsystem checks by hand. Timer
    /// counts continuously from MatchDuration (150) down through TeleopStartTime (135, where it's reset at
    /// the auto/teleop transition) down to 0, so a plain "Timer &lt;= 20f" / "Timer &lt;= 10f" check reproduces
    /// the real matchTime thresholds without needing to also branch on CurrentGameState - during Auto, Timer
    /// only ever ranges 150-135 (always &gt; 20), so it naturally falls through to the alliance-color branch
    /// there too, same as it would for the bulk of Teleop. No -1/"timer not started" sentinel is needed here
    /// since MoSim's Timer is always valid once the component exists.
    ///
    /// Alliance is read the same way StuyPulseLEDController reads it (_base.Alliance), which sidesteps the
    /// real code's Optional&lt;Alliance&gt;.get() (that throws if alliance isn't set yet - not ported here).
    /// </summary>
    public class Team5959LEDController : MonoBehaviour
    {
        [Header("LED Surfaces")]
        [Tooltip("Drag every GameObject (with a Renderer) that makes up the strip here - mirrors the real robot's single 27-LED AddressableLED strip.")]
        [SerializeField] private GameObject[] leds;
 
        [Tooltip("The generated Shader asset from Assets/Materials/LEds/LEDs.shadergraph - same one GRRLights/LEDStripController/StuyPulseLEDController use. If left empty, this clones whatever material is already on each LED mesh instead.")]
        [SerializeField] private Shader shaderGraphShader;
 
        [Header("Intensity")]
        [Tooltip("Brightness for a fully \"on\" state (solid green, alliance color, endgame blink-on, rainbow).")]
        [SerializeField] private float onIntensity = 150f;
        
        [Header("Rainbow (Disabled)")]
        [Tooltip("Rainbow/gradient texture to scroll across the strip while Disabled - assign an asset here, same as RoboGymLights' texture fields (red/green/white/disabled). No texture is generated at runtime.")]
        [SerializeField] private Texture rainbowTexture;
        [Tooltip("How fast rainbowTexture scrolls across the strip - same _X-over-time idiom as RoboGymLights' disabled scroll (Time.time * 0.08f).")]
        [SerializeField] private float rainbowScrollSpeed = 0.3f;

        [Header("Endgame Blink - fixed Hz per threshold, no interpolation")]
        [Tooltip("Blink rate (Hz) once <= 10 seconds are left.")]
        [SerializeField] private float blinkHzAt10s = 4f;
        [Tooltip("Blink rate (Hz) once <= 5 seconds are left.")]
        [SerializeField] private float blinkHzAt5s = 8f;
        [Tooltip("Blink rate (Hz) once <= 3 seconds are left.")]
        [SerializeField] private float blinkHzAt3s = 10f;
        [Tooltip("Blink rate (Hz) once <= 2 seconds are left.")]
        [SerializeField] private float blinkHzAt2s = 15f;
        [Tooltip("Blink rate (Hz) once <= 1 second is left.")]
        [SerializeField] private float blinkHzAt1s = 20f;

        [Header("Colors (RGB pickers, like StuyPulseLEDController - real code's raw 0-255 values as Color)")]
        [Tooltip("Used for both the flat \"<=20s left\" solid state and the \"<=10s left\" blink's on-phase - matches the real code using (0,255,0) green for both.")]
        [SerializeField] private Color matchGreenColor = Color.green;
        [Tooltip("Real code: setAllLEDs(255, 0, 3).")]
        [SerializeField] private Color redAllianceColor = new Color(1f, 0f, 3f / 255f);
        [Tooltip("Real code: setAllLEDs(0, 0, 255).")]
        [SerializeField] private Color blueAllianceColor = Color.blue;

        [Header("Match Timer")]
        [Tooltip("The scene's active match timer (ReefscapeTimerManager). Left empty, this auto-finds one via FindObjectOfType at Start - only assign manually if there's more than one in the scene.")]
        [SerializeField] private BaseTimerManager timerManager;

        private ReefscapeRobotBase _base;
        private Material _material;

        private readonly Dictionary<Color, Texture2D> _solidTextures = new();

        private void Start()
        {
            _base = GetComponent<ReefscapeRobotBase>();
            _material = BuildSharedMaterial(leds);

            if (timerManager == null)
            {
                timerManager = FindObjectOfType<BaseTimerManager>();
            }
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
            if (_material == null) return;

            // Disabled (including after a match): rainbow, same as the real code's else-branch.
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                RunRainbow();
                return;
            }

            // --- Enabled (teleop or auto) ---

            if (timerManager == null)
            {
                // No timer manager found in the scene - fall back to alliance color rather than guessing.
                SetAll(AllianceColor(), onIntensity);
                return;
            }

            var matchTime = timerManager.Timer;

            if (matchTime <= 0f)
            {
                RunRainbow();
                return;
            }
            else if (matchTime <= 1f)
            {
                SetAll(matchGreenColor, Blink(blinkHzAt1s));
            }
            else if (matchTime <= 2f)
            {
                SetAll(matchGreenColor, Blink(blinkHzAt2s));
            }
            else if (matchTime <= 3f)
            {
                SetAll(matchGreenColor, Blink(blinkHzAt3s));
            }
            else if (matchTime <= 5f)
            {
                SetAll(matchGreenColor, Blink(blinkHzAt5s));
            }
            else if (matchTime <= 10f)
            {
                SetAll(matchGreenColor, Blink(blinkHzAt10s));
            }
            else if (matchTime <= 20f)
            {
                SetAll(matchGreenColor, onIntensity);
            }
            else
            {
                SetAll(AllianceColor(), onIntensity);
            }
        }

        private Color AllianceColor()
        {
            return _base != null && _base.Alliance == Alliance.Red ? redAllianceColor : blueAllianceColor;
        }

        // Same idiom as RoboGymLights.Blink(hz): a plain Time.time modulo period, on for the first half and
        // off (dim) for the second. Empirical and easy to tune - swap onIntensity/dimIntensity or the hz
        // ramp above if it needs adjusting.
        private static float Blink(float hz)
        {
            return Time.time % (1f / hz) > 1f / (hz * 2f) ? 20f : 0f;
        }
        // Exactly RoboGymLights' idiom for its disabled state: assign a texture, scroll it via _X over time.
        // No generation/baking here - rainbowTexture is whatever asset was dragged into the inspector.
        private void RunRainbow()
        {
            _material.SetTexture("_Texture2D", rainbowTexture);
            _material.SetFloat("_Y", 0f);
            _material.SetFloat("_X", Time.time * rainbowScrollSpeed);
            _material.SetFloat("_intensity", onIntensity);
        }

        // Same idea as GRRLights/StuyPulseLEDController's Set(): _X/_Y stay at 0 for solid colors (no
        // scroll), _intensity drives brightness/blink, _Texture2D gets the baked solid-color texture.
        private void SetAll(Color color, float intensity)
        {
            var texture = GetSolidTexture(color);
            _material.SetFloat("_X", 0f);
            _material.SetFloat("_Y", 0f);
            _material.SetFloat("_intensity", intensity);
            _material.SetTexture("_Texture2D", texture);
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