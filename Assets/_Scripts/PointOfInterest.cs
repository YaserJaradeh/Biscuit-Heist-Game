using UnityEngine;

namespace _Scripts
{
    public class PointOfInterest : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = Color.yellow;
        [SerializeField] private float gizmoRadius = 0.5f;
        [SerializeField] private Vector3 labelOffset = new Vector3(-0.12f, 0.73f, 0);

        private void OnDrawGizmos()
        {
            // Set the gizmo color
            Gizmos.color = gizmoColor;

            // Draw a wireframe sphere at the transform position
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);

            // Draw a small solid sphere at the center
            Gizmos.DrawSphere(transform.position, gizmoRadius * 0.1f);

            // Draw the object name as a label above the sphere
            UnityEditor.Handles.Label(transform.position + labelOffset, gameObject.name);
        }
    }
}