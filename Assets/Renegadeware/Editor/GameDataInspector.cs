using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Renegadeware.LL_LS1A1 {
    [CustomEditor(typeof(GameData))]
    public class GameDataInspector : Editor {

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            M8.EditorExt.Utility.DrawSeparator();

            var dat = target as GameData;

            if(GUILayout.Button("Refresh Organism Components")) {
                GameDataEdit.RefreshOrganismComponents(dat);
            }
        }
    }
}