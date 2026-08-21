using System;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace RobotFramework.Components
{
    /// <summary>
    /// Variant of GenericElevator for continuous elevators where the stage extension order
    /// matches real-world telescoping behavior: stage[0] (the outermost/largest stage) is
    /// the directly-driven "primary" stage and extends first, with each subsequent stage
    /// (stage[1], stage[2], ... up to stage[^1], the carriage) engaging in order as the
    /// stage before it approaches its travel limit.
    ///
    /// This is NOT a subclass of GenericElevator: GenericElevator's fields and lifecycle
    /// methods (Start/LateUpdate/FixedUpdate) are private and non-virtual, so Unity's
    /// message dispatch would call both classes' copies independently if this inherited
    /// from it, causing duplicated/conflicting behavior. Instead this is a self-contained
    /// component that mirrors GenericElevator field-for-field and behavior-for-behavior,
    /// with only the continuous stage-ordering math changed.
    ///
    /// Everything else (cascade behavior, audio, click sounds, PID handling, axis overrides,
    /// reported elevator height) is identical to GenericElevator.
    /// </summary>
    public class ContinuousRealisticElevator : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Array of GenericJoint stages composing this elevator. Order must go from the outermost/largest stage (index 0, directly driven) to the innermost/topmost carriage stage (last index).")]
        private GenericJoint[] stages;

        [SerializeField]
        [Tooltip("Elevator configuration type (cascade or continuous).")]
        private ElevatorType elevatorType;

        [FormerlySerializedAs("pidmi")]
        [SerializeField]
        [Tooltip("PID constants for primary elevator stage control.")]
        private PidConstants pidConstants;

        [SerializeField]
        [Tooltip("PID constants for individual stages (continuous elevators only).")]
        private PidConstants stagesPidConstants;

        [SerializeField]
        [Tooltip("Which axis the elevator moves along.")]
        private JointAxis elevatorAxis;

        [SerializeField]
        [Tooltip("Whether to invert the elevator direction.")]
        private bool flipped;

        [SerializeField]
        [Tooltip("Per-stage PID overrides.")]
        private PerStageOverrides[] setStageOverrides;

        [SerializeField]
        [Tooltip("Height of each stage in inches.")]
        private float stageHeight;

        [SerializeField]
        [Tooltip("Overlap between stages in inches.")]
        private float stageOverlap;

        [SerializeField]
        [Tooltip("Height of the carriage (0 for open-top, or stage overlap value).")]
        private float carriageHeight;

        private float _previousStageHeight;
        private float _previousStageOverlap;
        private float _previousCarriageHeight;

        private Vector3 _carriageStartPosition;

        private JointAxis _lastAxis;
        private bool _lastOffset;

        [Header("Audio Settings")] [SerializeField]
        private AudioSource elevatorSound;

        [SerializeField] private AudioClip elevatorClip;
        [SerializeField] private float minSpeed = 1f;
        [SerializeField] private float maxSpeed = 60f;
        [SerializeField] private Vector2 soundPitchRange = new Vector2(0.8f, 1.2f);
        [SerializeField] private Vector2 soundVolumeRange = new Vector2(0.3f, 1.0f);

        [Header("Continuous Elevator Audio Settings")] [SerializeField]
        private AudioClip stageClickClip;

        [SerializeField] private float clickVolume = 0.1f;
        [SerializeField] private float clickSoundCooldown = 0.2f;
        [SerializeField] private float topClickOffset = 0.5f;
        [SerializeField] private float bottomClickOffset = 0.5f;

        [SerializeField] private Transform referenceTransform;

        private AudioSource[] _stageClickSources;

        private float[] _lastStagePositions;
        private float[] _lastClickTimes;
        private bool[] _wasAtTop;

        private bool[] _wasAtBottom;

        private Vector3 _lastStagePosition;
        private float _continuousRealTargetHeight;

        [Serializable]
        public struct PerStageOverrides
        {
            public int stageNum;
            public JointAxis overrideAxis;
            public bool useStartingOffset;
        }

        private void Start()
        {
            for (var i = 0; i < stages.Length; i++)
            {
                // CHANGED: the primary/directly-driven stage is now index 0 (the outermost
                // stage) instead of the last index, so it gets the "primary" PID constants.
                if (i == 0 || elevatorType == ElevatorType.Cascade)
                {
                    stages[i].SetPid(pidConstants);
                }
                else
                {
                    stages[i].SetPid(stagesPidConstants);
                }
            }

            _carriageStartPosition = stages[0].transform.parent.InverseTransformPoint(stages[^1].transform.position);
            _lastAxis = elevatorAxis;
            _lastOffset = flipped;

            _lastStagePosition = stages[^1].transform.localPosition;

            if (elevatorSound != null && elevatorClip != null)
            {
                elevatorSound.clip = elevatorClip;
                elevatorSound.loop = true;
                elevatorSound.playOnAwake = false;
            }
            else
            {
                Debug.LogWarning("Elevator sound or clip not set.");
            }

            _stageClickSources = new AudioSource[stages.Length];
            _lastClickTimes = new float[stages.Length];
            _lastStagePositions = new float[stages.Length];
            _wasAtTop = new bool[stages.Length];
            _wasAtBottom = new bool[stages.Length];
            for (var i = 0; i < stages.Length; i++)
            {
                _stageClickSources[i] = stages[i].gameObject.AddComponent<AudioSource>();
                _stageClickSources[i].clip = stageClickClip;
                _stageClickSources[i].volume = clickVolume;
                _stageClickSources[i].playOnAwake = false;
                _stageClickSources[i].loop = false;

                _lastStagePositions[i] = stages[i].GetAxisLocation(elevatorAxis) * 39.3701f; // Convert to inches
                _lastClickTimes[i] = -clickSoundCooldown;
                _wasAtTop[i] = false;
                _wasAtBottom[i] = false;
            }

            InitClickPositions();
            PrecacheCombinedHeights();
        }

        private void LateUpdate()
        {
            for (var i = 0; i < stages.Length; i++)
            {
                // CHANGED: same primary-index swap as in Start().
                if (i == 0 || elevatorType == ElevatorType.Cascade)
                {
                    stages[i].UpdatePid(pidConstants);
                }
                else
                {
                    stages[i].UpdatePid(stagesPidConstants);
                }
            }

            if (!Mathf.Approximately(stageHeight, _previousStageHeight) ||
                !Mathf.Approximately(stageOverlap, _previousStageOverlap) ||
                !Mathf.Approximately(carriageHeight, _previousCarriageHeight))
            {
                PrecacheCombinedHeights();
                _previousStageHeight = stageHeight;
                _previousStageOverlap = stageOverlap;
                _previousCarriageHeight = carriageHeight;
            }
        }

        private void FixedUpdate()
        {
            UpdateElevatorAudio();
            CheckContinuousElevatorClicks();
        }

        public void SetTarget(float target)
        {
            switch (elevatorType)
            {
                case ElevatorType.Cascade:
                    RunCascade(target);
                    break;
                case ElevatorType.Continuous:
                    RunContinuous(target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RunCascade(float target)
        {
            for (var i = stages.Length - 1; i >= 0; i--)
            {
                var useStartingOffset = false;
                var axis = elevatorAxis;
                for (var j = 0; j < setStageOverrides.Length; j++)
                {
                    if (setStageOverrides[j].stageNum - 1 != i) continue;
                    axis = setStageOverrides[j].overrideAxis;
                    useStartingOffset = setStageOverrides[j].useStartingOffset;
                }

                if (flipped)
                {
                    if (useStartingOffset)
                    {
                        stages[i].SetLinearTarget(target / stages.Length).withAxis(elevatorAxis)
                            .useDifferentEncoderAxis(axis).flipDirection().useAutomaticStartingOffset();
                    }
                    else
                    {
                        stages[i].SetLinearTarget(target / stages.Length).withAxis(elevatorAxis)
                            .useDifferentEncoderAxis(axis).flipDirection();
                    }
                }
                else
                {
                    if (useStartingOffset)
                    {
                        stages[i].SetLinearTarget(target / stages.Length).withAxis(elevatorAxis)
                            .useDifferentEncoderAxis(axis).useAutomaticStartingOffset();
                    }
                    else
                    {
                        stages[i].SetLinearTarget(target / stages.Length).withAxis(elevatorAxis)
                            .useDifferentEncoderAxis(axis);
                    }
                }
            }
        }

        public static float GetLocationOnAxis(JointAxis jointAxis, Vector3 setTransform)
        {
            return jointAxis switch
            {
                JointAxis.X => setTransform.x,
                JointAxis.Y => setTransform.y,
                JointAxis.Z => setTransform.z,
                _ => 0
            };
        }

        /// <summary>
        /// PrecacheCombinedHeights MUST be called before running this function.
        /// CHANGED from GenericElevator: stage 0 is now the directly-driven primary stage
        /// and extends first; stages 1..(Length-1) are followers that engage in order,
        /// closest-to-primary first, carriage (last index) last.
        /// </summary>
        private void RunContinuous(float target)
        {
            // minStagePosition still reads the position of stages[^1] (the carriage). This is
            // the physical top of the transform hierarchy, so it always reflects the TRUE total
            // extension of the whole assembly regardless of which stage is being driven directly -
            // this must stay pointed at stages[^1], not stages[0].
            float minStagePosition = transform.InverseTransformPoint(stages[^1].transform.position).y;

            for (int i = 0; i < stages.Length; i++)
            {
                float setPoint = 0;
                if (i == 0)
                {
                    setPoint = target;
                    setTarget(setPoint, i);
                    continue; // Skip follower calculations
                }

                float combinedHeight = _cachedCombinedHeights[i];

                if (combinedHeight < minStagePosition)
                {
                    setPoint = minStagePosition - combinedHeight;
                }

                setTarget(setPoint * 39.3701f, i);
            }
        }

        private float[] _cachedCombinedHeights;

        /// <summary>
        /// CHANGED from GenericElevator: thresholds are now computed by "distance from the
        /// primary stage" measured from index 0 instead of index Length-1. We reuse the exact
        /// same threshold formula as GenericElevator, just evaluated at the mirrored index
        /// k = Length - 1 - i, so that the follower closest to the new primary (index 1) gets
        /// the smallest threshold (engages first) and the carriage (last index) gets the
        /// largest threshold (engages last).
        /// </summary>
        private void PrecacheCombinedHeights()
        {
            _cachedCombinedHeights = new float[stages.Length];

            for (int i = 1; i < stages.Length; i++)
            {
                int k = stages.Length - 1 - i; // mirrored index, preserves distance-from-primary math

                float combinedHeight = (-carriageHeight) * 0.0254f;

                for (int j = k; j < stages.Length - 1; j++)
                {
                    float retractionOffset = 0;

                    if (j < stages.Length - 2)
                    {
                        retractionOffset = (stageOverlap + ((stages.Length - j) * 2f)) * 0.0254f;
                    }

                    combinedHeight += (stageHeight * 0.0254f) - retractionOffset;
                }

                _cachedCombinedHeights[i] = combinedHeight;
            }
        }

        private void setTarget(float target, int i)
        {
            var useStartingOffset = false;
            var axis = elevatorAxis;
            for (var j = 0; j < setStageOverrides.Length; j++)
            {
                if (setStageOverrides[j].stageNum - 1 != i) continue;
                axis = setStageOverrides[j].overrideAxis;
                useStartingOffset = setStageOverrides[j].useStartingOffset;
            }

            if (flipped)
            {
                if (useStartingOffset)
                {
                    stages[i].SetLinearTarget(target).withAxis(elevatorAxis).useDifferentEncoderAxis(axis)
                        .flipDirection().useAutomaticStartingOffset();
                }
                else
                {
                    stages[i].SetLinearTarget(target).withAxis(elevatorAxis).useDifferentEncoderAxis(axis)
                        .flipDirection();
                }
            }
            else
            {
                if (useStartingOffset)
                {
                    stages[i].SetLinearTarget(target).withAxis(elevatorAxis).useDifferentEncoderAxis(axis)
                        .useAutomaticStartingOffset();
                }
                else
                {
                    stages[i].SetLinearTarget(target).withAxis(elevatorAxis).useDifferentEncoderAxis(axis);
                }
            }
        }

        public float GetElevatorHeight()
        {
            if (elevatorType == ElevatorType.Cascade)
            {
                if (stages.Length == 0) return 0f;

                var elevatorHeight = 0f;
                foreach (var stage in stages)
                {
                    elevatorHeight += stage.GetAxisLocation(elevatorAxis) * 39.3701f;
                    elevatorHeight -= stage.useStartingOffset ? stage._startingPosition.y * 39.3701f : 0f;
                }

                return elevatorHeight;
            }
            else
            {
                // Unchanged: stages[^1] (the carriage) is still the physical top of the stack,
                // so its position is still the correct total-height reading regardless of which
                // stage is the directly-driven primary.
                return stages[^1].GetAxisLocation(elevatorAxis) * 39.3701f -
                       (stages[^1].useStartingOffset ? stages[^1]._startingPosition.y * 39.3701f : 0f);
            }
        }

        /// <summary>
        /// NOTE: kept identical to GenericElevator. This utility predicts the real achievable
        /// height given the OLD (stages[^1]-primary) engagement order's reachability math. It is
        /// not called by SetTarget/RunContinuous, so it doesn't affect actual elevator movement,
        /// but if you rely on its exact output for a continuous elevator using this script, it may
        /// not perfectly reflect the new first-to-last engagement order. Let me know if you need
        /// this re-derived for the new order too.
        /// </summary>
        public float GetContinuousTargetHeight(float targetHeight = 0f)
        {
            if (elevatorType != ElevatorType.Continuous)
            {
                throw new InvalidOperationException("This method is only valid for continuous elevators.");
            }

            if (targetHeight == 0f) return _continuousRealTargetHeight;

            _continuousRealTargetHeight = 0f;

            for (var i = stages.Length - 1; i >= 0; i--)
            {
                var axis = elevatorAxis;
                for (var j = 0; j < setStageOverrides.Length; j++)
                {
                    if (setStageOverrides[j].stageNum - 1 != i) continue;
                    axis = setStageOverrides[j].overrideAxis;
                }

                var altAxis = axis;
                if (i == stages.Length - 1)
                {
                    foreach (var stageOverride in setStageOverrides)
                    {
                        if (stageOverride.stageNum - 1 == 0)
                        {
                            altAxis = stageOverride.overrideAxis;
                        }
                    }
                }

                float realTarget;
                if (i == stages.Length - 1)
                {
                    var axisPose = GetLocationOnAxis(altAxis, _carriageStartPosition) * 39.3701f;
                    var axisCPose = GetLocationOnAxis(altAxis,
                        stages[0].transform.parent.InverseTransformPoint(stages[^1].transform.position)) * 39.3701f;
                    if (axisCPose > targetHeight - axisPose - (stageHeight - stageOverlap) * (stages.Length - 1) ||
                        targetHeight < axisPose + (stageHeight - carriageHeight))
                    {
                        realTarget = Mathf.Min(targetHeight, stageHeight - carriageHeight);
                    }
                    else
                    {
                        realTarget = Mathf.Infinity - 1;
                    }
                }
                else
                {
                    var maxExtension = i + 2 == stages.Length
                        ? stageHeight - carriageHeight
                        : stageHeight - stageOverlap;
                    var adjustedTarget = targetHeight - ((maxExtension) * ((stages.Length - 1) - i));
                    var offset = _lastOffset
                        ? (GetLocationOnAxis(_lastAxis, stages[i + 1]._startingPosition) * 39.3701f)
                        : 0;
                    if (adjustedTarget > 0 &&
                        maxExtension - ((stages[i + 1].GetAxisLocation(_lastAxis) * 39.3701) - offset) < 2f)
                    {
                        realTarget = Mathf.Min(Mathf.Max(adjustedTarget, 0), stageHeight - stageOverlap);
                    }
                    else
                    {
                        realTarget = 0;
                    }
                }

                realTarget = Mathf.Max(realTarget, 0);

                _continuousRealTargetHeight += realTarget;

                _lastOffset = flipped;
                _lastAxis = axis;
            }

            return _continuousRealTargetHeight;
        }

        private void UpdateElevatorAudio()
        {
            if (elevatorSound == null || stages.Length == 0) return;

            var currentPosition = stages[^1].transform.localPosition;
            var distanceMoved = Vector3.Distance(_lastStagePosition, currentPosition);
            var speed = distanceMoved / Time.fixedDeltaTime * 39.3701f;
            _lastStagePosition = currentPosition;

            if (speed > minSpeed)
            {
                if (!elevatorSound.isPlaying)
                {
                    elevatorSound.Play();
                }

                var t = Mathf.Clamp01(speed / maxSpeed);
                elevatorSound.pitch = Mathf.Lerp(soundPitchRange.x, soundPitchRange.y, t);
                elevatorSound.volume = Mathf.Lerp(soundVolumeRange.x, soundVolumeRange.y, t);
            }
            else
            {
                if (elevatorSound.isPlaying)
                {
                    elevatorSound.Stop();
                }
            }
        }

        private void CheckContinuousElevatorClicks()
        {
            if (elevatorType != ElevatorType.Continuous || stageClickClip == null)
                return;

            for (var i = 0; i < stages.Length; i++)
            {
                var localStagePosition = stages[i].transform.localPosition.y * 39.3701f;

                var currentPos = localStagePosition;

                var movement = currentPos - _lastStagePositions[i];

                var movingUp = movement > 0.001f;
                var movingDown = movement < -0.001f;

                // Unchanged: this is each stage's own physical travel range, which is a
                // geometric fact (only the last/carriage stage has no top overlap) and doesn't
                // depend on which stage is driven directly.
                var travelDist = i == stages.Length - 1 ? stageHeight - carriageHeight : stageHeight - stageOverlap;

                var topTriggerPos = travelDist - topClickOffset;
                var bottomTriggerPos = bottomClickOffset;

                var hitTopTrigger = currentPos >= topTriggerPos;
                var hitBottomTrigger = currentPos <= bottomTriggerPos;

                var playClick = (movingUp && hitTopTrigger && !_wasAtTop[i]) ||
                                (movingDown && hitBottomTrigger && !_wasAtBottom[i]);

                if (playClick)
                {
                    if (Time.time - _lastClickTimes[i] > clickSoundCooldown)
                    {
                        _stageClickSources[i].Play();
                        _lastClickTimes[i] = Time.time;
                    }
                }

                _wasAtTop[i] = hitTopTrigger;
                _wasAtBottom[i] = hitBottomTrigger;
                _lastStagePositions[i] = currentPos;
            }
        }

        private void InitClickPositions()
        {
            if (referenceTransform == null && stages.Length > 0)
            {
                referenceTransform = stages[0].transform.parent;
            }

            _lastStagePositions = new float[stages.Length];
            _lastClickTimes = new float[stages.Length];

            for (var i = 0; i < stages.Length; i++)
            {
                var localPos = referenceTransform.InverseTransformPoint(stages[i].transform.position);
                var posInches = GetLocationOnAxis(elevatorAxis, localPos) * 39.3701f;

                if (flipped) posInches = -posInches;

                _lastStagePositions[i] = posInches;
                _lastClickTimes[i] = -clickSoundCooldown;
                _wasAtTop[i] = false;
                _wasAtBottom[i] = false;
            }
        }
    }
}