using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Renegadeware.LL_LS1A1 {
    [CustomPropertyDrawer(typeof(IDAttribute))]
    public class IDPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if(property.propertyType == SerializedPropertyType.Integer) {
                EditorGUI.BeginProperty(position, label, property);

                var attrib = this.attribute as IDAttribute;

                //apply ID if invalid
                if(property.intValue == attrib.invalidID)
                    ApplyID(attrib.group, attrib.startID, property);

                const float labelSize = 20f;
                const float editSpace = 4f;

                var labelPos = new Rect(position.x, position.y, position.width - labelSize - editSpace, position.height);
                var editPos = new Rect(position.x + position.width - labelSize, position.y, labelSize, position.height);

                EditorGUI.LabelField(labelPos, label, new GUIContent(property.intValue.ToString()));

                if(GUI.Button(editPos, new GUIContent("R", "Apply a new ID.")))
                    ApplyID(attrib.group, attrib.startID, property);

                EditorGUI.EndProperty();
            }
            else
                EditorGUI.PropertyField(position, property, label);
        }

        private void ApplyID(string grp, int defaultID, SerializedProperty property) {
            var key = "ID." + grp;

            int id = EditorPrefs.GetInt(key, defaultID);

            property.intValue = id;

            EditorPrefs.SetInt(key, id + 1);
        }
    }
}