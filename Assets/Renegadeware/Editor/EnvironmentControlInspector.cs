using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Renegadeware.LL_LS1A1 {
    [CustomEditor(typeof(EnvironmentControl))]
    public class EnvironmentControlInspector : Editor {
        BoxBoundsHandle mBoxHandle = new BoxBoundsHandle();

        void OnSceneGUI() {
            var dat = target as EnvironmentControl;
            if(dat == null)
                return;

            using(new Handles.DrawingScope(dat.editBoundsColor)) {
                mBoxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;

                Bounds b = new Bounds(dat.bounds.center, dat.bounds.size);

                mBoxHandle.center = new Vector3(b.center.x, b.center.y, 0f);
                mBoxHandle.size = new Vector3(b.size.x, b.size.y, 0f);

                EditorGUI.BeginChangeCheck();
                mBoxHandle.DrawHandle();
                if(EditorGUI.EndChangeCheck()) {
                    Vector2 min = mBoxHandle.center - mBoxHandle.size * 0.5f;

                    float _minX = Mathf.Round(min.x / dat.editBoundsSteps);
                    float _minY = Mathf.Round(min.y / dat.editBoundsSteps);

                    min.x = _minX * dat.editBoundsSteps;
                    min.y = _minY * dat.editBoundsSteps;

                    Vector2 max = mBoxHandle.center + mBoxHandle.size * 0.5f;

                    float _maxX = Mathf.Round(max.x / dat.editBoundsSteps);
                    float _maxY = Mathf.Round(max.y / dat.editBoundsSteps);

                    max.x = _maxX * dat.editBoundsSteps;
                    max.y = _maxY * dat.editBoundsSteps;

                    b.center = Vector2.Lerp(min, max, 0.5f);
                    b.size = max - min;

                    Undo.RecordObject(dat, "Change Environment Control Bounds");
                    dat.bounds = new Rect(b.min, b.size);

                    if(dat.editSyncBoxCollider) {
                        BoxCollider2D boxColl = dat.GetComponent<BoxCollider2D>();
                        if(boxColl) {
                            Undo.RecordObject(boxColl, "Change Environment Control BoxCollider2D");
                            boxColl.offset = dat.transform.worldToLocalMatrix.MultiplyPoint3x4(dat.bounds.center);
                            boxColl.size = dat.bounds.size;
                        }
                    }
                }
            }
        }
    }
}