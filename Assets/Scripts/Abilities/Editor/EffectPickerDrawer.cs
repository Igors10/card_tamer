using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws a dropdown with all AbilityEffect classes next to each effect in the inspector
/// </summary>
[CustomPropertyDrawer(typeof(EffectPickerAttribute))]
public class EffectPickerDrawer : PropertyDrawer
{
    static Type[] effectTypes;
    static Type[] EffectTypes => effectTypes ??= TypeCache.GetTypesDerivedFrom<AbilityEffect>()
        .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
        .OrderBy(t => t.Name)
        .ToArray();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        Type currentType = property.managedReferenceValue?.GetType();
        string typeName = (currentType == null) ? "(none)" : ObjectNames.NicifyVariableName(currentType.Name);

        // dropdown button on the header line (drawn first so it gets the click before the foldout)
        Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2f, position.y,
            position.width - EditorGUIUtility.labelWidth - 2f, EditorGUIUtility.singleLineHeight);
        if (EditorGUI.DropdownButton(buttonRect, new GUIContent(typeName), FocusType.Keyboard))
            ShowMenu(property, currentType, buttonRect);

        // effect's own fields
        EditorGUI.PropertyField(position, property, new GUIContent(typeName), true);

        EditorGUI.EndProperty();
    }

    static void ShowMenu(SerializedProperty property, Type currentType, Rect rect)
    {
        SerializedObject serializedObject = property.serializedObject;
        string path = property.propertyPath;

        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("(none)"), currentType == null, () => SetValue(serializedObject, path, null));

        foreach (Type type in EffectTypes)
        {
            Type t = type;
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(t.Name)), t == currentType,
                () => SetValue(serializedObject, path, Activator.CreateInstance(t)));
        }

        menu.DropDown(rect);
    }

    static void SetValue(SerializedObject serializedObject, string path, object value)
    {
        serializedObject.Update();
        serializedObject.FindProperty(path).managedReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
    }
}
