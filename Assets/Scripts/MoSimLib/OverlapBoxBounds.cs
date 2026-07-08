using System;
using UnityEngine;

namespace MoSimLib
{
    /// <summary>
    /// Utility struct for efficient physics overlap queries using a box collider as a template.
    /// Caches collider bounds and optionally tracks movement with a transform.
    /// </summary>
    public struct OverlapBoxBounds : IEquatable<OverlapBoxBounds>
    {
        /// <summary>Gets the transform this box is bound to (if movesWithTransform is true).</summary>
        public readonly Transform Transform;

        /// <summary>Gets or sets the half-extents of the box in world space.</summary>
        public Vector3 Bounds;

        /// <summary>Gets or sets the center position of the box in world space.</summary>
        public Vector3 Center;

        /// <summary>Gets or sets the rotation of the box in world space.</summary>
        public Quaternion Rotation;

        /// <summary>Gets whether this box updates its position and rotation with the transform.</summary>
        private readonly bool _movesWithTransform;

        /// <summary>
        /// Creates an OverlapBoxBounds from a BoxCollider, extracting its bounds and transform info.
        /// </summary>
        /// <param name="collider">The BoxCollider to extract bounds from.</param>
        /// <param name="movesWithTransform">If true, Center and Rotation update each frame to match the transform.</param>
        public OverlapBoxBounds(BoxCollider collider, bool movesWithTransform = true)
        {
            Transform = collider.transform;
            Bounds = Utils.MultiplyVectors(collider.size, collider.transform.lossyScale) / 2;
            Center = collider.bounds.center;
            Rotation = collider.transform.rotation;
            _movesWithTransform = movesWithTransform;
        }

        /// <summary>
        /// Performs a physics overlap query and returns all colliders touching this box.
        /// </summary>
        /// <param name="mask">Optional layer mask to filter results. If null, uses default mask.</param>
        /// <returns>Array of colliders overlapping this box.</returns>
        public Collider[] OverlapBox(LayerMask? mask = null)
        {
            if (_movesWithTransform && Transform is not null)
            {
                Center = Transform.position;
                Rotation = Transform.rotation;
            }

            return mask != null
                ? Physics.OverlapBox(Center, Bounds, Rotation, mask.Value)
                : Physics.OverlapBox(Center, Bounds, Rotation);
        }

        /// <summary>
        /// Performs a physics overlap query and stores results in the provided array (non-allocating).
        /// More efficient than OverlapBox for repeated queries.
        /// </summary>
        /// <param name="colliders">Array to store results (will be filled up to its length).</param>
        /// <param name="mask">Optional layer mask to filter results. If null, uses default mask.</param>
        /// <returns>The number of colliders that overlapped this box.</returns>
        public int OverlapBoxNonAlloc(ref Collider[] colliders, LayerMask? mask = null)
        {
            if (_movesWithTransform && Transform is not null)
            {
                Center = Transform.position;
                Rotation = Transform.rotation;
            }

            return mask != null
                ? Physics.OverlapBoxNonAlloc(Center, Bounds, colliders, Rotation, mask.Value)
                : Physics.OverlapBoxNonAlloc(Center, Bounds, colliders, Rotation);
        }

        /// <summary>
        /// Compares two OverlapBoxBounds for equality based on their transform reference.
        /// </summary>
        public static bool operator ==(OverlapBoxBounds a, OverlapBoxBounds b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Compares two OverlapBoxBounds for inequality based on their transform reference.
        /// </summary>
        public static bool operator !=(OverlapBoxBounds a, OverlapBoxBounds b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Checks equality with another OverlapBoxBounds based on transform reference.
        /// </summary>
        public bool Equals(OverlapBoxBounds other)
        {
            return Equals(Transform, other.Transform);
        }

        /// <summary>
        /// Checks equality with an object, converting if possible.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is OverlapBoxBounds other && Equals(other);
        }

        /// <summary>
        /// Generates a hash code based on transform, bounds, center, and rotation.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Transform, Bounds, Center, Rotation);
        }
    }
}