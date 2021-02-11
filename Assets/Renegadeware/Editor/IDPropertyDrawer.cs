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
                if(property.intValue == attrib.invalidID) {
                    var key = "ID." + attrib.group;

                    int id = EditorPrefs.GetInt(key, attrib.startID);

                    property.intValue = id;

                    EditorPrefs.SetInt(key, id + 1);
                }

                EditorGUI.LabelField(position, label, new GUIContent(property.intValue.ToString()));

                EditorGUI.EndProperty();
            }
            else
                EditorGUI.PropertyField(position, property, label);
        }
    }
}