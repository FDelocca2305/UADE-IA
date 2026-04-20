using Attributes;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
    public class ReadOnlyInspectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect p_position, SerializedProperty p_property, GUIContent p_label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(p_position, p_property, p_label);
            GUI.enabled = true;
        }
        
        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
