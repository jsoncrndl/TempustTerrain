using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace TempustTerrain
{
    [CustomEditor(typeof(TTMesh))]
    public class TTMeshEditor : Editor
    {
        SerializedProperty splineProp;
        Spline spline;

        private void OnEnable()
        {
            splineProp = serializedObject.FindProperty("spline");
            spline = (splineProp.objectReferenceValue as SplineContainer).Spline;
        }

        private void OnSceneGUI()
        {
            for (int i = 0; i < spline.Count; i++)
            {
                Vector3 relativePos = spline[i].Position + (spline[(i+1) % spline.Count].Position - spline[i].Position) / 2;
                Vector3 faceDir = Vector3.Cross(relativePos - (Vector3)spline[i].Position, Vector3.up);

                if (Handles.Button((serializedObject.targetObject as TTMesh).transform.position + relativePos, Quaternion.LookRotation(faceDir, Vector3.up), .05f, .1f, Handles.CubeHandleCap))
                {
                    ToggleEdge(i);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void ToggleEdge(int index)
        {
            int currentEdge = TerrainUtility.RotationToEdgeType(spline[index].Rotation);
            SerializedProperty knotsProp = new SerializedObject(splineProp.objectReferenceValue).FindProperty("m_Spline").FindPropertyRelative("m_Knots").GetArrayElementAtIndex(index).FindPropertyRelative("Rotation").FindPropertyRelative("value");
            Quaternion rotation = TerrainUtility.EdgeTypeToRotation((int)Mathf.Repeat(currentEdge + 1, TTMesh.GroundEdge.edgeTypeCount));
            knotsProp.FindPropertyRelative("w").floatValue = rotation.w;
            knotsProp.FindPropertyRelative("y").floatValue = rotation.y;
            if (knotsProp.serializedObject.ApplyModifiedProperties())
            {
                (serializedObject.targetObject as TTMesh).RecalculateMesh();
            }
            Debug.Log("Toggled edge " + index + " to type " + TerrainUtility.RotationToEdgeType(rotation));
        }
    }
}