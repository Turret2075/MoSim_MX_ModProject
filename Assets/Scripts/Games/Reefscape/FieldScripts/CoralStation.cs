using System;
using System.Collections.Generic;
using System.Linq;
using Games.Reefscape.Robots;
using MoSimCore.Enums;
using RobotFramework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Reefscape.FieldScripts
{
    public class CoralStation : MonoBehaviour
    {
        [field: SerializeField] public Alliance Alliance { get; private set; }
        [field: SerializeField] public List<ReefscapeRobotBase> Robots { get; set; }

        [SerializeField] private GameObject coralPrefab;
        [SerializeField] private Transform coralSpawnpoint;
        [SerializeField] private float maxXOffset = 0.8f;
        private Vector3 _initialSpawnpoint;
        private Quaternion _initialSpawnpointRotation;

        private BoxCollider _stationTrigger;

        [SerializeField] private float minCoralSpawnForce = 4f;
        [SerializeField] private float maxCoralSpawnForce = 6f;

        [SerializeField] private float coralSpawnTorque = 0.1f;

        [SerializeField] private bool spawnCoral;

        private float _coralSpawnTimer = 1.5f;

        [SerializeField] private float coralSpawnInterval = 3f;

        [SerializeField] private Transform coralParent;
        private Rigidbody _coralRb;

        private static Queue<GameObject> _corals = new();

        private static bool _executedThisFrame;

        // Per-robot cooldown tracker for station drops. Each robot can independently request station drops without being blocked.
        private readonly Dictionary<RobotBase, float> _lastStationSpawnTime = new();

        private void Awake()
        {
            _stationTrigger = GetComponent<BoxCollider>();

            coralParent = GameObject.FindGameObjectWithTag("GamePieceWorld").transform;

            _initialSpawnpoint = coralSpawnpoint.position;
            _initialSpawnpointRotation = coralSpawnpoint.rotation;

            PoolObjects(true);
        }

        private void Start()
        {
            PoolObjects(true);
            InitializePoolForScene();
        }

        private void InitializePoolForScene()
        {
            // Clear existing objects if any (from previous scene if persisting manager)
            foreach (var obj in _corals)
            {
                Destroy(obj);
            }

            _corals.Clear();

            // Then populate for the new scene
            PoolObjects(true);
        }

        private void PoolObjects(bool start = false)
        {
            if ((_executedThisFrame && !start) || (_corals.Count > 6 && !start))
            {
                return;
            }

            if (start)
            {
                while (_corals.Count < 12 + 6)
                {
                    var tmp = Instantiate(coralPrefab);
                    tmp.SetActive(false);
                    _corals.Enqueue(tmp);
                    var currentCoralRb = tmp.GetComponent<Rigidbody>();
                    currentCoralRb.useGravity = false;
                    currentCoralRb.interpolation = RigidbodyInterpolation.None;
                    currentCoralRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    currentCoralRb.detectCollisions = false;
                }
            }
            else
            {
                var count = 0;
                while (_corals.Count < 12 && count < 1)
                {
                    count++;
                    var tmp = Instantiate(coralPrefab);
                    tmp.SetActive(false);
                    _corals.Enqueue(tmp);
                    var currentCoralRb = tmp.GetComponent<Rigidbody>();
                    currentCoralRb.interpolation = RigidbodyInterpolation.None;
                    currentCoralRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    currentCoralRb.useGravity = false;
                    currentCoralRb.detectCollisions = false;
                }
            }

            _executedThisFrame = true;
        }

        private void LateUpdate()
        {
            _executedThisFrame = false;
        }

        private void Update()
        {
            PoolObjects();

            _coralSpawnTimer += Time.deltaTime;

            foreach (var robot in Robots)
            {
                var coralStationMode = robot.CurrentCoralStationMode;
                if (coralStationMode == null)
                {
                    continue;
                }
            
                if (coralStationMode.DropType == DropType.Station)
                {
                    var target = coralStationMode.TargetTransform != null ? coralStationMode.TargetTransform : robot.transform;
                    AlignCoralSpawnPoint(target);

                    var distanceToTarget = GetDistanceToRobot(robot.transform);

                    _lastStationSpawnTime.TryGetValue(robot, out var lastSpawnTime);
                    var stationInterval = Mathf.Max(0.0f, coralSpawnInterval - 1f);

                    if (distanceToTarget < coralStationMode.DropDistance && Time.time - lastSpawnTime >= stationInterval)
                    {
                        if (coralStationMode.RequireIntaking && !robot.IsIntaking)
                        {
                            continue;
                        }

                        if (_corals.Count == 0)
                        {
                            PoolObjects();
                            if (_corals.Count == 0) continue;
                        }

                        _lastStationSpawnTime[robot] = Time.time;

                        var rb = AddCoralToWorld(coralStationMode.DropOrientation == DropOrientation.Vertical ? Vector3.zero : new Vector3(0f, 90f, 0f));

                        rb.AddForce(
                            coralSpawnpoint.forward *
                            (coralStationMode.DropStrength +
                             Random.Range(
                                 -coralStationMode.DropForceVariance,
                                 coralStationMode.DropForceVariance)),
                            ForceMode.Impulse);

                        rb.AddRelativeTorque(
                            new Vector3(0,
                                Random.Range(
                                    -coralStationMode.RotationVariance,
                                    coralStationMode.RotationVariance),
                                0),
                            ForceMode.Impulse);
                    }
                }
                else
                {
                    coralSpawnpoint.position = _initialSpawnpoint;
                    coralSpawnpoint.rotation = _initialSpawnpointRotation;
            
                    var results = Physics.OverlapBox(_stationTrigger.bounds.center, _stationTrigger.bounds.extents, Quaternion.identity);
                    var numCoral = results.Count(result => result.CompareTag("Coral"));
            
                    if ((numCoral < 3 && _coralSpawnTimer >= coralSpawnInterval) || spawnCoral)
                    {
                        _coralSpawnTimer = 0f;
                        spawnCoral = false;
                        if (_corals.Count > 0)
                        {
                            var rb = AddCoralToWorld(new Vector3(0f, 90f, 0f));
                            rb.AddForce(
                                coralSpawnpoint.transform.forward *
                                Random.Range(minCoralSpawnForce, maxCoralSpawnForce), ForceMode.Impulse);
                            rb.AddRelativeTorque(new Vector3(0, Random.Range(-coralSpawnTorque, coralSpawnTorque), 0),
                                ForceMode.Impulse);
                        }
                    }
                }
            }
        }

        private Rigidbody AddCoralToWorld(Vector3 spawnRotationOffset = new Vector3())
        {
            var coralToSpawn = _corals.Dequeue();
            var currentCoralRb = coralToSpawn.GetComponent<Rigidbody>();

            var tempSpawnPoint = new GameObject("CoralSpawnPoint").transform;
            tempSpawnPoint.position = coralSpawnpoint.position;
            tempSpawnPoint.rotation = coralSpawnpoint.rotation;
            tempSpawnPoint.localScale = coralSpawnpoint.localScale;
            tempSpawnPoint.SetParent(coralSpawnpoint.transform);
            tempSpawnPoint.localEulerAngles += spawnRotationOffset;

            if (!coralParent)
            {
                coralParent = GameObject.FindGameObjectWithTag("GamePieceWorld").transform;
            }

            coralToSpawn.transform.parent = coralParent;
            currentCoralRb.gameObject.SetActive(true);
            currentCoralRb.detectCollisions = false;
            currentCoralRb.Move(coralSpawnpoint.position, tempSpawnPoint.rotation);
            currentCoralRb.detectCollisions = true;
            currentCoralRb.interpolation = RigidbodyInterpolation.Interpolate;
            currentCoralRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            currentCoralRb.useGravity = true;

            Destroy(tempSpawnPoint.gameObject);

            return currentCoralRb;
        }

        public void ResetCoralStation(Transform newCoralParent)
        {
            coralParent = newCoralParent;
            PoolObjects(true);
        }

        private void AlignCoralSpawnPoint(Transform robotTransform)
        {
            var localXRotation = _initialSpawnpointRotation * Vector3.right;

            var toRobot = robotTransform.position - _initialSpawnpoint;

            var localXOffset = Vector3.Dot(toRobot, localXRotation);
            localXOffset = Mathf.Clamp(localXOffset, -maxXOffset, maxXOffset);

            var newLocalPosition = _initialSpawnpoint + localXRotation * localXOffset;

            coralSpawnpoint.position = newLocalPosition;
        }

        private float GetDistanceToRobot(Transform robotTransform)
        {
            var localZ = _initialSpawnpointRotation * Vector3.forward;
            var toRobot = robotTransform.position - _initialSpawnpoint;
            return Vector3.Dot(toRobot, localZ);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            foreach (var robot in Robots.Where(r => r.CurrentCoralStationMode.DrawDebugLines))
            {
                var mode = robot.CurrentCoralStationMode;
                var target = mode.TargetTransform != null ? mode.TargetTransform : robot.transform;

                Gizmos.DrawLine(target.position, _initialSpawnpoint);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    (target.position + _initialSpawnpoint) / 2 + Vector3.up * 0.2f,
                    $"Distance: {GetDistanceToRobot(target):F2}m");
#endif
            }
        }
    }

    [Serializable]
    public class CoralStationMode
    {
        [field: SerializeField] public DropType DropType { get; set; }

        [field: SerializeField] public float DropDistance { get; set; }

        [field: SerializeField] public float DropStrength { get; set; } = 5f;

        [field: SerializeField] public float DropForceVariance { get; set; } = 1f;

        [field: SerializeField] public float RotationVariance { get; set; } = 0.1f;

        [Header("Station Intake Settings")]
        [field: SerializeField] public DropOrientation DropOrientation { get; set; }

        [field: SerializeField] public bool RequireIntaking { get; set; } = true;

        [Header("Targeting")]
        [field: SerializeField] public Transform TargetTransform { get; set; }

        [Header("Debug Settings")]
        [field: SerializeField] public bool DrawDebugLines { get; private set; }
    }

    public enum DropType
    {
        Ground,
        Station
    }

    public enum DropOrientation
    {
        Horizontal,
        Vertical
    }
}