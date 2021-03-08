using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Renegadeware.LL_LS1A1 {
    [CustomPropertyDrawer(typeof(OrganismComponentIDAttribute))]
    public class OrganismComponentIDPropertyDrawer : PropertyDrawer {
        private int[] mKeyIDs;
        private string[] mKeys;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var gameDat = GameDataEdit.gameData;
            if(!gameDat) {
                GUI.Label(position, "GameData not found.");
            }
            else {
                if(!gameDat.VerifyOrganismComponents())
                    Refresh();
                else {
                    var comps = gameDat.organismComponents;
                    if(mKeyIDs == null || mKeyIDs.Length != comps.Length + 1)
                        GenerateKeys();
                }

                const float editSize = 20f;
                const float editSpace = 4f;

                var popUpPos = new Rect(position.x, position.y, position.width - editSize - editSpace, position.height);
                var editPos = new Rect(position.x + position.width - editSize, position.y, editSize, position.height);

                property.intValue = EditorGUI.IntPopup(popUpPos, property.intValue, mKeys, mKeyIDs);

                if(GUI.Button(editPos, new GUIContent("R", "Refresh Component List"))) {
                    Refresh();
                }
            }

            EditorGUI.EndProperty();
        }

        private void Refresh() {
            var gameDat = GameDataEdit.gameData;

            GameDataEdit.RefreshOrganismComponents(gameDat);

            GenerateKeys();
        }

        private void GenerateKeys() {
            var gameDat = GameDataEdit.gameData;
            var comps = gameDat.organismComponents;

            mKeyIDs = new int[comps.Length + 1];
            mKeys = new string[comps.Length + 1];

            mKeyIDs[0] = GameData.invalidID;
            mKeys[0] = "- Invalid -";

            for(int i = 0; i < comps.Length; i++) {
                mKeyIDs[i + 1] = comps[i].ID;
                mKeys[i + 1] = comps[i].name;
            }
        }
    }
}