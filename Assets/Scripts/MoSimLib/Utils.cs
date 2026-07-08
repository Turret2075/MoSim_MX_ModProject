using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoSimLib
{
    public static class Utils
    {
        /// <summary>
        /// Finds a child with a given name by only searching the children instead of everything.
        /// breadth only search!!
        /// </summary>
        /// <param name="childName"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static GameObject FindChild(string childName, GameObject parent)
        {
            if (parent == null) return null;
            
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                if (parent.transform.GetChild(i).name == childName)
                {
                    return parent.transform.GetChild(i).gameObject;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Checks for and adds if missing components and scripts
        /// </summary>
        /// <param name="parent">The parent game object</param>
        /// <param name="componentType">The component type to add</param>
        /// <returns></returns>
        public static T TryGetAddComponent<T>(GameObject parent) where T: Component
        {
            // 1. Try to get the component.
            if (parent.TryGetComponent(typeof(T), out var existingComponent))
            {
                // Found it, return the existing component.
                return (T)existingComponent;
            }

            // 2. Not found, add the component.
            Component newComponent = parent.AddComponent(typeof(T));

            // 3. Return the newly created component.
            return (T)newComponent;
        }

        public static bool InRange(float input, float target, float range)
        {
            return input > target - range && input < target + range;
        }
        
        public static T TryGetAddComponent<T>(GameObject parent, out bool spawned) where T: Component
        {
            // 1. Try to get the component.
            if (parent.TryGetComponent(typeof(T), out var existingComponent))
            {
                // Found it, return the existing component.
                spawned = false;
                return (T)existingComponent;
            }

            // 2. Not found, add the component.
            Component newComponent = parent.AddComponent(typeof(T));
            spawned = true;

            // 3. Return the newly created component.
            return (T)newComponent;
        }
        
        /// <summary>
        /// Flips the angle 180
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float FlipAngle(float angle)
        {
            angle = -angle;
            angle = Mathf.Repeat(angle, 360); 
            if (angle < 0)
            {
                angle += 360;
            }
            return angle;
        }
        
        /// <summary>
        /// Wraps the angle into -180 180 from 360
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float WrapAngle180(float angle)
        {
            angle = Mathf.Repeat(angle, 360);
            if (angle > 180)
            {
                angle -= 360; // Convert to -180 to 180 range
            }
            return angle;
        }
        
        /// <summary>
        /// Determines if a value (A) is within a specified unit distance (C) of a center value (B).
        /// </summary>
        /// <param name="a">The value to check.</param>
        /// <param name="b">The center value.</param>
        /// <param name="c">The maximum allowed difference (tolerance).</param>
        /// <returns>True if A is in the range [B - C, B + C], False otherwise.</returns>
        public static bool InRange(double a, double b, double c)
        {
            return Math.Abs(a - b) <= c;
        }

        /// <summary>
        /// wraps angle to 0 to 360
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float WrapAngle360(float angle)
        {
            angle = Mathf.Repeat(angle, 360);
            return angle;
        }
        
        public static Vector3 MultiplyVectors(Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(vector1.x * vector2.x, vector1.y * vector2.y, vector1.z * vector2.z);
        }
        
        /// <summary>
        /// returns the difference between two angles
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float AngleDifference(float a, float b) {
            return (a - b + 540) % 360 - 180;
        }
        
        /// <summary>
        /// Determines if angle A is within the angular range defined by the shortest arc between B and C.
        /// </summary>
        /// <param name="a">The angle to check.</param>
        /// <param name="b">The first boundary angle.</param>
        /// <param name="c">The second boundary angle.</param>
        /// <returns>True if A is within the shortest arc segment [B, C], False otherwise.</returns>
        public static bool WithinAngularRange(float a, float b, float c)
        {
            float diffBC = AngleDifference(c, b);

            float diffBa = AngleDifference(a, b);
            
            bool sameDirection = (diffBa * diffBC >= 0);

            bool smallerMagnitude = (Math.Abs(diffBa) <= Math.Abs(diffBC));

            return sameDirection && smallerMagnitude;
        }
        
        /// <summary>
        /// returns if A is within C degrees of B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool InAngularRange(float a, float b, float c)
        {
            return Mathf.Abs(AngleDifference(a, b)) <= c;
        }
        
        /// <summary>
        /// Determines if a value (A) lies within the inclusive range defined by B and C.
        /// The order of B and C does not matter.
        /// </summary>
        /// <param name="a">The value to check.</param>
        /// <param name="b">One boundary of the range.</param>
        /// <param name="c">The other boundary of the range.</param>
        /// <returns>True if A is within the closed interval [min(B, C), max(B, C)], False otherwise.</returns>
        public static bool WithinRange(double a, double b, double c)
        {
            double minBound = Math.Min(b, c);
            double maxBound = Math.Max(b, c);
        
            // A is within the range if it is >= the minimum bound AND <= the maximum bound.
            return a >= minBound && a <= maxBound;
        }
        
        public static void AddParentLookUpFast(GameObject child, Component parent)
        {
            ParentComponentCache.TryAdd(child, parent);
        }
    
        private static readonly Dictionary<GameObject, Component> ParentComponentCache = new Dictionary<GameObject, Component>();

        /// <summary>
        /// Finds the first Parent objcet which contains a object T
        /// </summary>
        /// <param name="value"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        public static T FindParentObjectComponent<T>(GameObject child) where T : Component
        {
            // 1. Check the cache first
            if (ParentComponentCache.TryGetValue(child, out Component cachedComponent) && cachedComponent is T resultT)
            {
                return resultT;
            }

            // 2. Perform the expensive traversal
            T foundComponent = FindParentObjectComponentActual<T>(child);

            // 3. Cache the result (even if null)
            if (foundComponent != null)
            {
                ParentComponentCache[child] = foundComponent;
            }
            else
            {
                // Cache a marker for "not found" to prevent future null searches
                // A common technique is to cache a known dummy component or null, 
                // but for simplicity here we'll just skip caching null for now
                // as it would require more complex cache management.
            }

            return foundComponent;
        }
        
        private static T FindParentObjectComponentActual<T>(GameObject child) where T : Component
        {
            if (child == null)
            {
                return null;
            }

            Transform currentTransform = child.transform.parent;

            while (currentTransform != null)
            {
                T component = currentTransform.GetComponent<T>();
                if (component)
                {
                    return component;
                }
                currentTransform = currentTransform.parent;
            }
            return null;
        }
    
        /// <summary>
        /// smooth damp. behaves the same as Vector3.smooth damp.
        /// Full credit to MaxAttack
        /// </summary>
        /// <param name="rot"></param>
        /// <param name="target"></param>
        /// <param name="deriv"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        //https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
        public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time) {
            if (Time.fixedTime < Mathf.Epsilon) return rot;
            // account for double-cover
            var dot = Quaternion.Dot(rot, target);
            var multi = dot > 0f ? 1f : -1f;
            target.x *= multi;
            target.y *= multi;
            target.z *= multi;
            target.w *= multi;
            // smooth damp (nlerp approx)
            var result = new Vector4(
                Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
                Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
                Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
                Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
            ).normalized;
		
            // ensure deriv is tangent
            var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), result);
            deriv.x -= derivError.x;
            deriv.y -= derivError.y;
            deriv.z -= derivError.z;
            deriv.w -= derivError.w;		
		
            return new Quaternion(result.x, result.y, result.z, result.w);
        }
    }
}
