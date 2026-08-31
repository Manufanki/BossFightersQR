using UnityEditor;
using UnityEngine;

// Draws an EffectValue as a mode dropdown with the constant field only when Constant is picked.
[CustomPropertyDrawer(typeof(EffectValue))]
public class EffectValueDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty mode = property.FindPropertyRelative("mode");
        SerializedProperty constant = property.FindPropertyRelative("constant");
        SerializedProperty min = property.FindPropertyRelative("min");
        SerializedProperty max = property.FindPropertyRelative("max");

        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        float half = row.width * 0.45f;
        Rect modeRect = new Rect(row.x, row.y, half, row.height);
        Rect valueRect = new Rect(row.x + half + 6, row.y, row.width - half - 6, row.height);

        EditorGUI.PropertyField(modeRect, mode, label);

        switch ((EffectValue.Mode)mode.enumValueIndex)
        {
            case EffectValue.Mode.Constant:
                EditorGUI.PropertyField(valueRect, constant, GUIContent.none);
                break;
            case EffectValue.Mode.CurrentRound:
                EditorGUI.LabelField(valueRect, "= current round");
                break;
            case EffectValue.Mode.BossAttackAgainstPlayer:
                EditorGUI.LabelField(valueRect, "= boss attack vs player");
                break;
            case EffectValue.Mode.RandomRange:
                float halfValue = (valueRect.width - 4) * 0.5f;
                Rect minRect = new Rect(valueRect.x, valueRect.y, halfValue, valueRect.height);
                Rect maxRect = new Rect(valueRect.x + halfValue + 4, valueRect.y, halfValue, valueRect.height);
                EditorGUI.PropertyField(minRect, min, GUIContent.none);
                EditorGUI.PropertyField(maxRect, max, GUIContent.none);
                break;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
