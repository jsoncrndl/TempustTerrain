#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace TempustTerrain
{
    public class TerrainUtility
    {
        public static List<Vector3> MakeEdgeBevel(Vector3 corner, float cornerScale, Vector3 cornerNormal, Vector3 startDir, float radius, float angle, int subdivisions)
        {
            if (subdivisions <= 0) return new List<Vector3> { corner };

            Vector3 yVec = Vector3.Cross(startDir, Vector3.down);

            Vector3 bevelCenter = corner - cornerNormal * radius * cornerScale;

            List<Vector3> bevelPoints = new List<Vector3>();

            for (int i = 0; i <= subdivisions; i++)
            {

                float curAngle = i * angle / subdivisions * Mathf.Deg2Rad;

                float dX = Mathf.Cos(curAngle) * radius;
                float dY = Mathf.Sin(curAngle) * radius;

                bevelPoints.Add(bevelCenter + startDir * dX + dY * yVec);
            }

            return bevelPoints;
        }

        public static List<Vector3> MakeTopBevel(Vector3 corner, Vector3 edgeNormal, float xRadius, float yRadius, int subdivisions)
        {
            if (subdivisions <= 0) return new List<Vector3> { corner };

            Vector3 bevelCenter = corner - edgeNormal * xRadius - Vector3.up * yRadius;

            List<Vector3> bevelPoints = new List<Vector3>();

            for (int i = 0; i < subdivisions; i++)
            {
                float curAngle = i * 90 / subdivisions * Mathf.Deg2Rad;

                float dX = Mathf.Cos(curAngle) * xRadius;
                float dY = Mathf.Sin(curAngle) * yRadius;

                bevelPoints.Add(bevelCenter + edgeNormal * dX + dY * Vector3.up);
            }

            bevelPoints.Add(bevelCenter + yRadius * Vector3.up);

            return bevelPoints;
        }

        public static Quaternion EdgeTypeToRotation(int edgeType)
        {
            return Quaternion.Euler(new Vector3(0, 45 * edgeType, 0));
        }

        public static int RotationToEdgeType(Quaternion rotation)
        {
            return Mathf.RoundToInt(rotation.eulerAngles.y) / 45;
        }

        public static Mesh MakeMesh(List<TTMesh.TTEdge> edges, bool closed, int topUVChannel)
        {
            int numEdgeVerts = edges[0].vertices.Count;
            Vector3[] vertices = new Vector3[numEdgeVerts * edges.Count];
            List<int> bevelTriangles = new List<int>();
            List<int> flatTriangles = new List<int>();
            Vector3[] uvs = new Vector3[vertices.Length];

            //Debug.Log("Making mesh with " + vertices.Length + " vertices");

            float horizontalPos = 0;

            for (int i = 0; i < edges.Count; i++)
            {
                int prevEdgeType = edges[(int)Mathf.Repeat(i - 1, edges.Count)].nextEdgeType;

                //if (edges[i].nextEdgeType == TTMesh.GroundEdge.NONE && prevEdgeType == TTMesh.GroundEdge.NONE)
                //    continue;
                edges[i].vertices.CopyTo(vertices, i * numEdgeVerts);
                
                
                float verticalDistance = 0;

                if (i > 0)
                    horizontalPos += Vector3.Distance(vertices[(i - 1) * numEdgeVerts], vertices[i * numEdgeVerts]);

                List<int> triangleList = edges[i].nextEdgeType == TTMesh.GroundEdge.STANDARD ? bevelTriangles : flatTriangles;

                for (int j = 0; j < numEdgeVerts - 1; j++)
                {
                    int curVert = i * numEdgeVerts + j;

                    if (edges[i].nextEdgeType != TTMesh.GroundEdge.NONE && (i < edges.Count - 1 || closed))
                    {
                        triangleList.AddRange(new List<int>()
                        {
                            (curVert + numEdgeVerts) % vertices.Length,
                            (curVert + numEdgeVerts + 1) % vertices.Length,
                            (curVert) % vertices.Length,
                            (curVert + numEdgeVerts + 1) % vertices.Length,
                            (curVert + 1) % vertices.Length,
                            (curVert) % vertices.Length
                        });
                    }
                    if (j > 0)
                    {
                        float dist = Vector3.Distance(vertices[curVert], vertices[curVert - 1]);
                        verticalDistance += dist;
                    }

                    uvs[i * numEdgeVerts + j] = new Vector2(horizontalPos, verticalDistance);
                }

                int topVert = i * numEdgeVerts + (numEdgeVerts - 1);

                verticalDistance += Vector3.Distance(vertices[topVert], vertices[topVert - 1]);
                uvs[topVert] = new Vector2(horizontalPos, verticalDistance);
            }


            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.subMeshCount = 3;
            mesh.SetTriangles(bevelTriangles, 0);
            mesh.SetTriangles(flatTriangles, 1);

            //Make the top face
            List<Vector2> topVerts = new List<Vector2>();
            edges.ForEach(edge => topVerts.Add(new Vector2(edge.vertices[numEdgeVerts - 1].x, edge.vertices[numEdgeVerts - 1].z)));

            List<int> topEdges = EarClipping(topVerts);
            for (int i = 0; i < topEdges.Count; i++)
            {
                topEdges[i] = topEdges[i] * numEdgeVerts + numEdgeVerts - 1;
            }


            mesh.SetTriangles(topEdges, 2);
            Vector2[] topUVS = new Vector2[vertices.Length];

            for (int i = 0; i < topVerts.Count; i++)
            {
                topUVS[i * numEdgeVerts + numEdgeVerts - 1] = topVerts[i];
            }
            mesh.SetUVs(topUVChannel, topUVS);

            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }

        //private static bool IsSimplePolygon(List<Vector2> points)
        //{
        //    for (int i = 0; i < points.Count - 1; i++)
        //    {
        //        for (int j = i + 1; j < points.Count - 1; j++)
        //        {
        //            int l1End = i + 1;
        //            int l2Start = j;
        //            int l2End = j + 1;

        //            if (LinesIntersect(points[i], points[l1End], points[l2Start], points[l2End]))
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //    return true;
        //}

        //private static bool LinesIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        //{
        //    //four direction for two lines and points of other line
        //    int dir1 = TriangleDirection(p1, p2, p3);
        //    int dir2 = TriangleDirection(p1, p2, p4);
        //    int dir3 = TriangleDirection(p3, p4, p1);
        //    int dir4 = TriangleDirection(p3, p4, p2);


        //    if (!(p2 == p3 || p4 == p1))
        //    {
        //        if (dir1 != dir2 && dir3 != dir4)
        //            return true; //they are intersecting
        //    }
        //    if (p2 != p3)
        //    {
        //        if (dir1 == 0 && PointOnSegment(p1, p2, p3))
        //            return true;
        //        if (dir4 == 0 && PointOnSegment(p3, p4, p2))
        //            return true;
        //    }
        //    if (p4 != p1)
        //    {
        //        if (dir3 == 0 && PointOnSegment(p3, p4, p1))
        //            return true;
        //        if (dir2 == 0 && PointOnSegment(p1, p2, p4))
        //            return true;
        //    }

        //    return false;
        //}

        private static List<int> EarClipping(List<Vector2> inputCoords)
        {
            List<int> triangles = new List<int>();
            DoubleLinkedList<Vector2> vertices = new DoubleLinkedList<Vector2>();
            inputCoords.ForEach(v => vertices.AddLast(v));

            DoubleLinkedList<Vector2>.DoubleLinkedListNode curNode = vertices.First;
            int numTries = 0;
            //Ear Clipping
            while (vertices.Count > 2 && numTries < 1000)
            {
                Vector2 i = curNode.Previous.Value;
                Vector2 j = curNode.Value;
                Vector2 k = curNode.Next.Value;

                i = i == null ? vertices.Last.Value : i;
                k = k == null ? vertices.First.Value : k;

                bool isConvex = IsConvex(i, j, k);
                bool isEar = true;
                if (isConvex)
                {
                    DoubleLinkedList<Vector2>.DoubleLinkedListNode testNode = curNode.Next.Next;

                    while (testNode != curNode.Previous && isEar)
                    {
                        isEar = !VertexInTriangle(i, j, k, testNode.Value);
                        testNode = testNode.Next;
                    }
                }
                else
                    isEar = false;

                if (isEar)
                {
                    triangles.AddRange(new int[]{
                        inputCoords.IndexOf(i),
                        inputCoords.IndexOf(j),
                        inputCoords.IndexOf(k)
                    });
                    vertices.Remove(curNode);
                }
                curNode = curNode.Next;
                numTries++;
            }

            return triangles;
        }

        //private static bool PointOnSegment(Vector2 p1, Vector2 p2, Vector2 p3)
        //{
        //    return p3.x < Mathf.Max(p1.x, p2.x) && p3.x < Mathf.Min(p1.x, p2.x) && p3.y < Mathf.Max(p1.y, p2.y) && p3.y < Mathf.Min(p1.y, p2.y);
        //}

        //private static int TriangleDirection(Vector2 a, Vector2 b, Vector2 c)
        //{
        //    float val = (b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y);
        //    if (val == 0)
        //        return 0;     //colinear
        //    else if (val < 0)
        //        return 2;    //anti-clockwise direction
        //    return 1;    //clockwise direction
        //}

        private static bool IsConvex(Vector2 i, Vector2 j, Vector2 k)
        {
            Vector2 a = i - j;
            Vector2 b = k - j;

            float dot = a.x * b.x + a.y * b.y;
            float det = a.x * b.y - a.y * b.x;
            float angle = Mathf.Atan2(det, dot);
            angle = Mathf.Repeat(angle, 2.0f * Mathf.PI);

            return angle < Mathf.PI;
        }

        private static bool VertexInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            Vector2 v0 = c - a;
            Vector2 v1 = b - a;
            Vector2 v2 = p - a;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float denom = dot00 * dot11 - dot01 * dot01;
            if (Mathf.Abs(denom) < 1e-20)
                return true;
            float invDenom = 1.0f / denom;
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v < 1);
        }
    }
}
#endif