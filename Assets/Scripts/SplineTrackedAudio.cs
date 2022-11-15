using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TempustTerrain
{
    public class SplineTrackedAudio : MonoBehaviour
    {
        public SplineFunctions spline;
        public Transform target;
        public Transform center;
        public float searchDistance;

        private void Update()
        {
            //if ((audioListener.transform.position - center.position).sqrMagnitude < searchDistance * SearchDistance)
            {
                UpdatePosition();
            }
        }

        void UpdatePosition()
        {
            transform.position = spline.ClosestInterpolatedPoint(target.position);
        }
    }
}