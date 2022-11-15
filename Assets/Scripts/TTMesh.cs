using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor.Splines;
#endif

namespace TempustTerrain
{

    [RequireComponent(typeof(SplineContainer))]
    [ExecuteInEditMode]
    public class TTMesh : MonoBehaviour
    {
        public float height = 1;
        public float topBevelRadius = .07f;
        public int topBevelSubdivisions = 3;
        public float edgeBevelRadius = .07f;
        public int edgeBevelSubdivisions = 3;
        public int edgeBevelStartAngle = 89;
        public float wallThickness = .1f;
        public bool closed;
        public int topUVChannel = 4;

        //Compiled mesh
        [SerializeField] protected Mesh mesh;

        [SerializeField] private List<TTEdge> edges;
        private List<VertexInfo> vertexList;
        [SerializeField] private SplineContainer spline;

        private MeshFilter meshFilter;
        MeshCollider meshCollider;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            edges = new List<TTEdge>();
            vertexList = new List<VertexInfo>();
            EditorSplineUtility.afterSplineWasModified += (s) =>
            {
                if (s.Equals(spline.Spline))
                {
                    RecalculateMesh();
                }
            };

            RecalculateMesh();
        }


        private void OnDisable()
        {
        }

        private void OnValidate()
        {
            RecalculateMesh();
        }

        public void RecalculateMesh()
        {
            //Debug.Log("Updating Mesh");
            if (edges == null)
                edges = new List<TTEdge>();
            edges.Clear();

            if (vertexList == null)
                vertexList = new List<VertexInfo>();
            else
                vertexList.Clear();

            BezierKnot[] points = spline.Spline.ToArray();

            for (int index = 0; index < points.Length; index++)
            {

                int index1 = (int)Mathf.Repeat(index - 1, points.Length);
                int index2 = (int)Mathf.Repeat(index + 1, points.Length);

                int prevEdgeType = TerrainUtility.RotationToEdgeType(points[index1].Rotation);
                int nextEdgeType = TerrainUtility.RotationToEdgeType(points[index].Rotation);

                if ((index1 == points.Length - 1 && !closed) || prevEdgeType != GroundEdge.STANDARD)
                {
                    index1 = -1;
                }
                if ((index2 == 0 && !closed) || nextEdgeType != GroundEdge.STANDARD)
                {
                    index2 = -1;
                }

                Vector3 norm1 = index1 == -1 ? Vector3.zero : Vector3.Cross(points[index].Position - points[index1].Position, Vector3.up).normalized;
                Vector3 norm2 = index2 == -1 ? Vector3.zero : Vector3.Cross(points[index2].Position - points[index].Position, Vector3.up).normalized;

                Vector3 direction = (norm1 + norm2).normalized;

                float bisectedAngle = Vector3.Angle(norm1, direction);
                float radiusScale = 1 / Mathf.Cos(bisectedAngle * Mathf.Deg2Rad);

                if (index1 == -1 || index2 == -1)
                {
                    vertexList.Add(new VertexInfo(points[index].Position, prevEdgeType, nextEdgeType));
                    continue;
                }

                float angle = Vector3.Angle(points[index1].Position - points[index].Position, points[index2].Position - points[index].Position);

                if (angle <= edgeBevelStartAngle)
                {
                    List<Vector3> bevelPoints = TerrainUtility.MakeEdgeBevel(points[index].Position, radiusScale, direction, norm1, edgeBevelRadius, Vector3.Angle(norm1, norm2), edgeBevelSubdivisions);
                    for (int i = 0; i < bevelPoints.Count; i++)
                    {
                        if (i == bevelPoints.Count - 1)
                        {
                            vertexList.Add(new VertexInfo(bevelPoints[i], prevEdgeType, nextEdgeType));
                        }
                        else
                        {
                            vertexList.Add(new VertexInfo(bevelPoints[i], prevEdgeType, prevEdgeType));
                        }
                    }
                }
                else
                {
                    vertexList.Add(new VertexInfo(points[index].Position, prevEdgeType, nextEdgeType));
                }
            }

            for (int i = 0; i < vertexList.Count; i++)
            {
                edges.Add(new TTEdge(this, i));
            }
            mesh = TerrainUtility.MakeMesh(edges, closed, topUVChannel);
            
            mesh.name = "Generated TTMesh";
            if (meshFilter != null)
                meshFilter.mesh = mesh;
            if (meshCollider != null)
                meshCollider.sharedMesh = mesh;
        }
        public void ToggleEdge(int index)
        {
            int currentEdge = TerrainUtility.RotationToEdgeType(spline.Spline[index].Rotation);
            BezierKnot knot = new BezierKnot(spline.Spline[index].Position, Vector3.zero, Vector3.zero, TerrainUtility.EdgeTypeToRotation((int)Mathf.Repeat(currentEdge + 1, GroundEdge.edgeTypeCount)));
            spline.Spline[index] = knot;
            //Debug.Log("Toggled edge " + index + " to type " + TerrainUtility.RotationToEdgeType(knot.Rotation));
        }

#endif
        private void OnDrawGizmos()
        {
            /*Gizmos.color = Color.red;
            if (edges == null)
            {
                edges = new List<TTEdge>();
            }

            foreach (TTEdge edge in edges)
            {
                for (int i = 0; i < edge.vertices.Count - 1; i++)
                    Gizmos.DrawLine(edge.vertices[i], edge.vertices[i + 1]);
            }*/

            //vertexList.ForEach((vert) => Debug.DrawRay(vert.pos, Vector3.up, Color.cyan));

        }
        public class TTEdge
        {
            public Vector3 direction;
            public List<Vector3> vertices;
            public int index;
            public int nextEdgeType;

            public TTEdge(TTMesh mesh, int index)
            {
#if UNITY_EDITOR
                int index1 = (int)Mathf.Repeat(index - 1, mesh.vertexList.Count);
                int index2 = (int)Mathf.Repeat(index + 1, mesh.vertexList.Count);

                int prevEdgeType = mesh.vertexList[index].prevEdgeType;
                int nextEdgeType = mesh.vertexList[index].nextEdgeType;

                this.nextEdgeType = nextEdgeType;

                if ((index1 == mesh.vertexList.Count - 1 && !mesh.closed) || prevEdgeType != GroundEdge.STANDARD)
                {
                    index1 = -1;
                }
                if ((index2 == 0 && !mesh.closed) || nextEdgeType != GroundEdge.STANDARD)
                {
                    index2 = -1;
                }

                Vector3 norm1 = index1 == -1 ? Vector3.zero : Vector3.Cross(mesh.vertexList[index].pos - mesh.vertexList[index1].pos, Vector3.up).normalized;
                Vector3 norm2 = index2 == -1 ? Vector3.zero : Vector3.Cross(mesh.vertexList[index2].pos - mesh.vertexList[index].pos, Vector3.up).normalized;

                direction = (norm1 + norm2).normalized;

                float bisectedAngle = Vector3.Angle(norm1, direction);
                float radiusScale = 1 / Mathf.Cos(bisectedAngle * Mathf.Deg2Rad);

                vertices = new List<Vector3>();
                vertices.Add(mesh.vertexList[index].pos + direction * mesh.wallThickness * radiusScale);

                Vector3 bevelStartPos = vertices[0] + mesh.height * Vector3.up;

                vertices.AddRange(TerrainUtility.MakeTopBevel(bevelStartPos, direction, radiusScale * mesh.topBevelRadius, mesh.topBevelRadius, mesh.topBevelSubdivisions));
                if (mesh.wallThickness > mesh.topBevelRadius)
                {
                    vertices.Add(mesh.vertexList[index].pos + mesh.height * Vector3.up);
                }
#endif
            }
        }
        public class VertexInfo
        {
            public Vector3 pos;
            public int nextEdgeType;
            public int prevEdgeType;

            public VertexInfo(Vector3 position, int prevEdge, int nextEdge)
            {
                pos = position;
                nextEdgeType = nextEdge;
                prevEdgeType = prevEdge;
            }
        }

        public class GroundEdge
        {
            public const int STANDARD = 0;
            public const int FLAT = 1;
            public const int NONE = 2;

            public const int edgeTypeCount = 3; 
        }
    }
}