using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MyUI
{
    [CustomEditor(typeof(FlexibleScollBar))]
    public class FlexibleScrollBarInspector : Editor
    {
        private FlexibleScollBar t;

        private SerializedProperty m_PointList;


        void OnEnable()
        {
            t = (FlexibleScollBar)target;
        }

        void Update()
        {

        }

        void OnSceneGUI()
        {
            if (t.points.Count >= 2)
            {
                Handles.color = Color.cyan;
                for (int i = 1; i < t.points.Count; ++i)
                {
                    DrawHermiteCurve(t.points[i - 1], t.points[i]);
                }
                Handles.color = Color.red;
                for (int i = 0; i < t.points.Count; ++i)
                {
                    DrawHandles(t.points[i]);
                }
            }
        }

        private void DrawHandles(FlexibleScollBar.Point point)
        {
            Handles.DrawLine(point.position - point.tanget, point.position + point.tanget);
        }

        private void DrawHermiteCurve(FlexibleScollBar.Point startPoint, FlexibleScollBar.Point endPoint)
        {
            FlexibleScollBar.Point lastPoint = startPoint;
            for (float i = 0; i < 1f; i += 0.01f)
            {
                FlexibleScollBar.Point nextPoint = FlexibleScollBar.GetHermiteCurvePoint(startPoint, endPoint, i);
                Handles.DrawLine(lastPoint.position, nextPoint.position);
                lastPoint = nextPoint;
            }
        }

    }
}