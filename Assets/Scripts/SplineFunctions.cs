using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace TempustTerrain
{
    public class SplineFunctions : MonoBehaviour
    {
        private SplineContainer spline;
        private List<Vector3> dividedPoints;

        [Range(1, 50)]
        public int subdivisions = 5;

        private void Start()
        {
            spline = GetComponent<SplineContainer>();
            SubdivideSpline();
        }

        private void SubdivideSpline()
        {
            dividedPoints = new List<Vector3>();

            for (int i = 0; i <= subdivisions + 1; i++)
            {
                float t = ((float)i) / subdivisions;
                dividedPoints.Add(spline.EvaluatePosition(t));
            }
        }

        public Vector3 ClosestInterpolatedPoint(Vector3 pos)
        {
            int[] indexes = new int[2];
            float[] mins = new float[] { (pos - dividedPoints[0]).sqrMagnitude, (pos - dividedPoints[0]).sqrMagnitude };



            for (int i = 1; i < dividedPoints.Count; i++)
            {
                float check = (pos - dividedPoints[i]).sqrMagnitude;
                if (check < mins[0])
                {
                    float temp = mins[0];
                    int tempI = indexes[0];

                    mins[0] = check;
                    indexes[0] = i;

                    if (temp < mins[1])
                    {
                        mins[1] = temp;
                        indexes[1] = tempI;
                    }
                }
                else if (check < mins[1])
                {
                    mins[1] = check;
                    indexes[1] = i;
                }
            }

            Vector3 close = dividedPoints[indexes[0]];  //The closest point to the player
            Vector3 far = dividedPoints[indexes[1]];    //The second closest point to the player

            Debug.DrawRay(close, Vector3.up, Color.green);
            Debug.DrawRay(far, Vector3.up, Color.red);
            float projection = Vector3.Dot((far - close).normalized, (pos - close).normalized);
            //Mathf.Cos(Vector3.SignedAngle(far - close, pos - close, Vector3.up));
            Debug.Log(projection);
            return Vector3.Lerp(close, far, projection);
        }

        public int ClosestDividedIndex(Vector3 pos)
        {
            float min = (pos - dividedPoints[0]).sqrMagnitude;
            int minIndex = 0;

            for (int i = 1; i < dividedPoints.Count; i++)
            {
                float check = (pos - dividedPoints[i]).sqrMagnitude;
                if (check < min)
                {
                    min = check;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        public Vector3 ClosestDirection(Vector3 pos)
        {
            int index = ClosestDividedIndex(pos);
            if (index == dividedPoints.Count - 1)
            {
                return dividedPoints[index] - dividedPoints[index - 1];
            }
            else
            {
                return dividedPoints[index + 1] - dividedPoints[index];
            }
        }

        public Vector3 ClosestPoint(Vector3 pos)
        {
            return dividedPoints[ClosestDividedIndex(pos)];
        }
    }
}
