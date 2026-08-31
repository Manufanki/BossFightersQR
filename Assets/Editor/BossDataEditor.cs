using UnityEditor;
using UnityEngine;

// Custom inspector for BossData: default fields plus a managed editor for the modular
// trigger list. Each trigger type shows only its own fields, mirroring the CardData editor.
[CustomEditor(typeof(BossData))]
public class BossDataEditor : Editor
{
    private static readonly string[] TriggerTypeNames =
    {
        "Round (every/even/odd/specific)",
        "Health Threshold",
        "Shield Broken",
        "Hit Reaction"
    };

    private static readonly string[] EffectTypeNames =
    {
        "Attack Up",
        "Damage Players",
        "Status Players",
        "Shield Up",
        "Poison",
        "Heal Boss"
    };

    private SerializedProperty _modularTriggersProperty;
    private int _selectedTriggerType;
    private int _selectedEffectType;

    private void OnEnable()
    {
        _modularTriggersProperty = serializedObject.FindProperty("modularTriggers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "modularTriggers");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Modular Triggers", EditorStyles.boldLabel);

        for (int i = 0; i < _modularTriggersProperty.arraySize; i++)
        {
            SerializedProperty triggerProperty = _modularTriggersProperty.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            object trigger = triggerProperty.managedReferenceValue;
            string typeName = trigger == null ? "Missing Trigger" : trigger.GetType().Name;
            string triggerName = triggerProperty.FindPropertyRelative("triggerName").stringValue;
            EditorGUILayout.LabelField($"{i + 1}. {triggerName} ({typeName})", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                _modularTriggersProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            // Shared fields.
            EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("triggerName"));

            // Only the fields relevant to this trigger type.
            switch (trigger)
            {
                case BossRoundTrigger _:
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("timing"));
                    if (((BossRoundTrigger)trigger).timing == BossTriggerTiming.SpecificRound)
                        EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("specificRound"));
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("fromRound"));
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("phase"));
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("oneShot"));
                    break;

                case BossHealthTrigger _:
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("hpAtOrBelow"));
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("phase"));
                    EditorGUILayout.HelpBox("Fires once when boss HP reaches the threshold.", MessageType.None);
                    break;

                case BossShieldBrokenTrigger _:
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("shieldType"));
                    EditorGUILayout.HelpBox("Fires the moment this shield is destroyed (once per refill). A suppressed Shield Strike does not fire it.", MessageType.None);
                    break;

                case BossHitReactionTrigger _:
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("hitThreshold"));
                    EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("oneShot"));
                    EditorGUILayout.HelpBox("Hits only happen in the Action phase; the reaction fires at the end of a round in which a single hit met the threshold.", MessageType.None);
                    break;
            }

            EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("popupText"));
            DrawEffectsList(triggerProperty.FindPropertyRelative("effects"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        _selectedTriggerType = EditorGUILayout.Popup(_selectedTriggerType, TriggerTypeNames);
        if (GUILayout.Button("Add Trigger"))
        {
            int newIndex = _modularTriggersProperty.arraySize;
            _modularTriggersProperty.arraySize++;
            _modularTriggersProperty.GetArrayElementAtIndex(newIndex).managedReferenceValue = CreateTrigger(_selectedTriggerType);
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEffectsList(SerializedProperty effectsProperty)
    {
        EditorGUILayout.LabelField("Effects", EditorStyles.miniBoldLabel);

        for (int i = 0; i < effectsProperty.arraySize; i++)
        {
            SerializedProperty effectProperty = effectsProperty.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            string effectName = effectProperty.managedReferenceValue == null
                ? "Missing Effect"
                : effectProperty.managedReferenceValue.GetType().Name;
            EditorGUILayout.LabelField($"{i + 1}. {effectName}", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                effectsProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(effectProperty, GUIContent.none, true);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        _selectedEffectType = EditorGUILayout.Popup(_selectedEffectType, EffectTypeNames);
        if (GUILayout.Button("Add Effect"))
        {
            int newIndex = effectsProperty.arraySize;
            effectsProperty.arraySize++;
            effectsProperty.GetArrayElementAtIndex(newIndex).managedReferenceValue = CreateEffect(_selectedEffectType);
        }
        EditorGUILayout.EndHorizontal();
    }

    private static BossTrigger CreateTrigger(int triggerTypeIndex)
    {
        switch (triggerTypeIndex)
        {
            case 0: return new BossRoundTrigger();
            case 1: return new BossHealthTrigger();
            case 2: return new BossShieldBrokenTrigger();
            case 3: return new BossHitReactionTrigger();
            default: throw new System.ArgumentOutOfRangeException(nameof(triggerTypeIndex), triggerTypeIndex, null);
        }
    }

    private static BossEffect CreateEffect(int effectTypeIndex)
    {
        switch (effectTypeIndex)
        {
            case 0: return new BossAttackUpEffect();
            case 1: return new BossDamagePlayersEffect();
            case 2: return new BossStatusPlayersEffect();
            case 3: return new BossShieldUpEffect();
            case 4: return new BossPoisonEffect();
            case 5: return new BossHealEffect();
            default: throw new System.ArgumentOutOfRangeException(nameof(effectTypeIndex), effectTypeIndex, null);
        }
    }
}
