using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Renegadeware.LL_LS1A1 {
    [CustomEditor(typeof(OrganismEntity))]
    public class OrganismEntityInspector : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            M8.EditorExt.Utility.DrawSeparator();

            var dat = target as OrganismEntity;

            GUI.enabled = !Application.isPlaying;

            if(GUILayout.Button("Initialize Data")) {
                Undo.RecordObject(dat, "Initialize Data");

                dat.GenerateSize();
                dat.GenerateStats();
                dat.ComponentSetupTemplate();
            }
        }
    }
}