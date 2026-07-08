// using UnityEditor;
// using UnityEngine;
//
// namespace Editor
// {
//     public class CenterOfMassVisualizer : UnityEditor.Editor
//     {
//         [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
//         public static void DrawGizmos(Rigidbody rb, GizmoType gizmoType)
//         {
//             if (rb != null)
//             {
//                 Handles.color = Color.yellow;
//                 Handles.SphereHandleCap(0, rb.worldCenterOfMass, Quaternion.identity, 0.05f, EventType.Repaint);
//
//                 Handles.color = Color.red;
//                 Handles.DrawLine(rb.position, rb.worldCenterOfMass);
//             }
//         }
//     }
// }