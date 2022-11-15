using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class TTRiver : MonoBehaviour
{
    [SerializeField] private SplineContainer spline;
    private MeshCollider meshCollider;
    private MeshFilter filter;
    private Mesh mesh;
    private Mesh collision;

    public List<Vector3> divPoints;

    public int subdivisions;
    public float width;

    private void Awake()
    {
        meshCollider = GetComponent<MeshCollider>();
        filter = GetComponent<MeshFilter>();
    }

    private void OnEnable()
    {
        meshCollider = GetComponent<MeshCollider>();
        filter = GetComponent<MeshFilter>();
        spline.Spline.changed += GenerateMesh;
    }

    private void OnValidate()
    {

    }

    public void GenerateMesh()
    {
        Vector3[] points = new Vector3[(subdivisions + 1) * 2];
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        float curY = 0;

        divPoints = new List<Vector3>();

        uvs.Add(new Vector2(-width, 0));
        uvs.Add(new Vector2(width, 0));

        for (int i = 0; i <= subdivisions + 1; i++)
        {
            float t = ((float)i) / subdivisions;
            Vector3 tangent = ((Vector3)spline.EvaluateTangent(t)).normalized;
            Vector3 up = ((Vector3)spline.EvaluateUpVector(t)).normalized;
            Vector3 normal = Vector3.Cross(up, tangent);
            points[i * 2] = transform.InverseTransformPoint((Vector3)spline.EvaluatePosition(t) + (normal * width));
            points[i * 2 + 1] = transform.InverseTransformPoint((Vector3)spline.EvaluatePosition(t) - (normal * width));
            divPoints.Add(spline.EvaluatePosition(t));
        }


        for (int i = 0; i < subdivisions * 2; i+=2)
        {
            curY += Vector3.Distance(points[i], points[i + 2]) + Vector3.Distance(points[i + 1], points[i + 3]) / 2;
            triangles.AddRange(new int[] { i, i+1, i+2, i+1, i+3, i+2 });
            uvs.Add(new Vector2(-width, curY));
            uvs.Add(new Vector2(width, curY));
        }

        mesh = new Mesh();
        mesh.name = name + " Generated Mesh";
        mesh.vertices = points;
        mesh.triangles = triangles.ToArray();
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        filter.mesh = mesh;

        //meshCollider.sharedMesh = mesh;
    }
}
