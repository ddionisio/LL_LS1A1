using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Renegadeware.LL_LS1A1 {
    [CustomEditor(typeof(CameraControl))]
    public class CameraControlInspector : Editor {

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            M8.EditorExt.Utility.DrawSeparator();

            var dat = target as CameraControl;

            if(dat.zoomLevels.Length > 0) {
                var labels = new string[dat.zoomLevels.Length];
                for(int i = 0; i < labels.Length; i++)
                    labels[i] = string.Format("{0} : {1}", i, dat.zoomLevels[i].label);

                GUILayout.BeginHorizontal();

                var cam = dat.GetComponentInChildren<Camera>();

                var curZoomIndex = 0;

                //grab closest zoom index
                if(cam) {
                    var camPos = cam.transform.localPosition;
                    var diff = Mathf.Abs(camPos.z - dat.zoomLevels[0].level);
                    for(int i = 1; i < dat.zoomLevels.Length; i++) {
                        var _diff = Mathf.Abs(camPos.z - dat.zoomLevels[i].level);
                        if(_diff < diff)
                            curZoomIndex = i;
                    }
                }

                var zoomIndex = EditorGUILayout.Popup("Zoom Level", curZoomIndex, labels);

                if(zoomIndex != curZoomIndex && cam) {
                    var camPos = cam.transform.localPosition;
                    camPos.z = dat.zoomLevels[zoomIndex].level;
                    cam.transform.localPosition = camPos;
                }

                GUILayout.EndHorizontal();
            }
        }
    }
}