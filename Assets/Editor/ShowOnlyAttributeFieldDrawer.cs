using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowOnlyAttribute))]
public class ShowOnlyAttributeFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var previousGUIState = GUI.enabled;

        GUI.enabled = false;

        EditorGUI.PropertyField(position, property, label);

        GUI.enabled = previousGUIState;
    }
}
