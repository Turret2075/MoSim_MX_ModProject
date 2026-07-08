using System.Collections.Generic;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.Robots;
using RobotFramework.Controllers.Drivetrain;
using UnityEngine;

public class ReefscapeAutoAlign : AutoAlign
{
    [Header("Offsets")]
    public Vector3 offset;
    private Vector3 realOffset;
    public float rotation;
    [Header("Auto Align Direction")]
    [Tooltip("Enable forward auto align (when facing the reef)")]
    public bool enableForwardAlign = true;
    [Tooltip("Enable backwards auto align (when facing away from the reef)")]
    public bool enableBackwardsAlign = true;
    [Header("Auto Align Settings")]
    [Tooltip("Maximum distance from alignment node for auto align to activate (in feet)")]
    [SerializeField] private float maxAlignDistanceFeet = 15f;
    
    private const float FEET_TO_METERS = 0.3048f;
    
    private List<AlignNode> targetNodes = new List<AlignNode>();
    private Dictionary<Transform, AlignNode> parentLookup = new Dictionary<Transform, AlignNode>();

    private AlignNode closest;
    private AlignNode secondClosest;
    
    private ReefscapeRobotBase _base;

    private (Transform, float)[] candidates;

    private bool startup;

    private Transform closests;
    private Transform secondCloses;

    private void Awake()
    {
        startup = true;
        _base = GetComponent<ReefscapeRobotBase>();
    }

    private void Update()
    {
        if (_base == null) return;
        
        if (_base.AutoAlignLeftAction.triggered || _base.AutoAlignRightAction.triggered)
        {
            ClosestFaces();
            (Transform closest, Transform secondClosest) = ClosestPoints();
            closests = closest;
            secondCloses = secondClosest;
        }

        realOffset = offset * 0.0254f;
    }

    private void FixedUpdate()
    {
        if (startup)
        {
            var nodes = GameObject.FindGameObjectsWithTag("ReefFace");
        
            foreach (var node in nodes)
            {
                node.TryGetComponent<AlignNode>(out var tar);
                if (tar != null)
                {
                    targetNodes.Add(tar);
                }
            }

            foreach (var node in targetNodes)
            {
                parentLookup.TryAdd(node.LeftNode.transform, node);
                parentLookup.TryAdd(node.RightNode.transform, node);
            }
        
            candidates = new (Transform, float)[4];
            
            startup = false;
        }

        if (PlayerPrefs.GetInt("PerspectiveAutoAlign", 1) == 1)
        {
            perspectiveRelativeAlign();
        }
        else
        {
            ReefRelativeAlign();
        }
        
    }

    private bool cameraFacesNode(AlignNode node)
    {
        GameObject activeCamera = _base.GetActiveCamera();

        var nodeTransform = node.transform;
        
        Vector3 nodeForward = nodeTransform.forward;
    
        Vector3 cameraForward = activeCamera.transform.forward;
    
        float dotProduct = Vector3.Dot(cameraForward, nodeForward);
    
        return dotProduct > 0;
    }

    private void perspectiveRelativeAlign()
    {
        if (_base == null) return;
        
        if (_base.AutoAlignLeftAction.IsPressed())
        {
            parentLookup.TryGetValue(closests, out var cl);
            if (TryAlignToNode(closests, !cameraFacesNode(cl))) return;
            parentLookup.TryGetValue(secondCloses, out var sc);
            if (TryAlignToNode(secondCloses, !cameraFacesNode(sc))) return;
            if (TryAlignToNode(cl.LeftNode.transform, !cameraFacesNode(cl))) return;
            if (TryAlignToNode(cl.RightNode.transform, !cameraFacesNode(cl))) return;
        }
        
        if (_base.AutoAlignRightAction.IsPressed())
        {
            parentLookup.TryGetValue(closests, out var cl);
            if (TryAlignToNode(closests, cameraFacesNode(cl))) return;
            parentLookup.TryGetValue(secondCloses, out var sc);
            if (TryAlignToNode(secondCloses, cameraFacesNode(sc))) return;
            if (TryAlignToNode(cl.LeftNode.transform, cameraFacesNode(cl))) return;
            if (TryAlignToNode(cl.RightNode.transform, cameraFacesNode(cl))) return;
        }
    }

    private void ReefRelativeAlign()
    {
        if (_base == null) return;
        
        if (_base.AutoAlignLeftAction.IsPressed())
        {
            TryAlignToNode(closests, true);
            TryAlignToNode(secondCloses, true);
        }
        
        if (_base.AutoAlignRightAction.IsPressed())
        {
            TryAlignToNode(closests, false);
            TryAlignToNode(secondCloses, false);
        }
    }
    
    private bool TryAlignToNode(Transform targetNode, bool isLeftSide)
    {
        if (targetNode == null) return false;
        
        if (!parentLookup.TryGetValue(targetNode, out var parentNode)) return false;
        
        // Check if this is the correct side node
        bool isCorrectNode = isLeftSide 
            ? parentNode.LeftNode.transform == targetNode 
            : parentNode.RightNode == targetNode.gameObject;
            
        if (!isCorrectNode) return false;
        
        bool isFacingReef = _base.GetFacingReef();
        
        // Check if within max distance (convert feet to meters)
        float maxDistanceMeters = maxAlignDistanceFeet * FEET_TO_METERS;
        if (Vector3.Distance(transform.position, targetNode.position) > maxDistanceMeters) return false;
        
        var target = targetNode.transform;
        Quaternion targetRotation = target.rotation;
        Vector3 finalTarget = target.position;
        
        if ((!isFacingReef && enableBackwardsAlign) || !enableForwardAlign)
        {
            targetRotation *= Quaternion.Euler(0, 180, 0);
        }

        finalTarget += target.rotation * realOffset;
        
        targetRotation *= Quaternion.Euler(0, rotation, 0);
        
        AlignPosition(finalTarget, targetRotation);
        
        return true;
    }


    private (Transform close, Transform sec) ClosestPoints()
    {
        if (closest == null || secondClosest == null)
        {
            return (null, null);
        }
        
        var pointA = closest.LeftNode.transform;
        var pointB = closest.RightNode.transform;
        var pointC = secondClosest.LeftNode.transform;
        var pointD = secondClosest.RightNode.transform;

        var origin = transform.position;
    
        var distA = Vector3.Distance(pointA.position, origin);
        var distB = Vector3.Distance(pointB.position, origin);
        var distC = Vector3.Distance(pointC.position, origin);
        var distD = Vector3.Distance(pointD.position, origin);

        Transform finalClosest = null;
        var finalCloseDist = float.MaxValue;
        Transform finalSecondClosest = null;
        var finalSecondCloseDist = float.MaxValue;
    
        candidates[0] = (pointA, distA);
        candidates[1] = (pointB, distB);
        candidates[2] = (pointC, distC);
        candidates[3] = (pointD, distD);
        foreach (var (currentPoint, currentDistance) in candidates)
        {
            if (currentDistance < finalCloseDist)
            {
                finalSecondClosest = finalClosest;
                finalSecondCloseDist = finalCloseDist;
            
                finalClosest = currentPoint;
                finalCloseDist = currentDistance;
            }
            else if (currentDistance < finalSecondCloseDist)
            {
                // Node is not the closest, but is the new second closest
                finalSecondClosest = currentPoint;
                finalSecondCloseDist = currentDistance;
            }
        }

        return (finalClosest, finalSecondClosest);
    }
    
    private void ClosestFaces()
    {
        float closestDist = float.MaxValue;
        float secondClosestDist = float.MaxValue;
    
        closest = null;
        secondClosest = null;

        foreach (var node in targetNodes)
        {
            if (node == null || node.transform == null)
            {
                continue;
            }
        
            float currentDistance = Vector3.Distance(transform.position, node.transform.position);

            if (currentDistance < closestDist)
            {
                secondClosestDist = closestDist;
                secondClosest = closest;

                closestDist = currentDistance;
                closest = node;
            }
        
            else if (currentDistance < secondClosestDist)
            {
                secondClosestDist = currentDistance;
                secondClosest = node;
            }
        }
    }
}
