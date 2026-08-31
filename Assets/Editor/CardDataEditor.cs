using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardData))]
public class CardDataEditor : Editor
{
    private static readonly string[] EffectTypeNames =
    {
        "Attack",
        "Support",
        "Protection",
        "Lightning",
        "Heal",
        "Draw",
        "Remove Status",
        "Extra Turn",
        "Cleanse Attack",
        "Special",
        "Attack Boost",
        "Shield Strike"
    };

    private QRCodeReader _qrCodeReader;
    private bool _isWaitingForScan;
    private int _selectedEffectType;
    private SerializedProperty _effectsProperty;

    private void OnEnable()
    {
        _effectsProperty = serializedObject.FindProperty("effects");
    }

    private void OnDisable()
    {
        StopListening();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "effects");

        EditorGUILayout.Space();
        DrawEffectsList();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Rename Asset From Card"))
            RenameAssetFromCard();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            string buttonText = _isWaitingForScan ? "Waiting For QR Scan..." : "Scan QR Code Into ID";
            if (GUILayout.Button(buttonText))
                StartListening();
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play Mode, then click Scan QR Code Into ID to capture the next code from a scene QRCodeReader.", MessageType.Info);
        else if (_isWaitingForScan)
            EditorGUILayout.HelpBox("Point the webcam at a QR code. The next detected value will replace QR ID.", MessageType.Info);
    }

    private void DrawEffectsList()
    {
        EditorGUILayout.LabelField("Card Effects", EditorStyles.boldLabel);

        for (int i = 0; i < _effectsProperty.arraySize; i++)
        {
            SerializedProperty effectProperty = _effectsProperty.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string effectName = effectProperty.managedReferenceValue == null
                ? "Missing Effect"
                : effectProperty.managedReferenceValue.GetType().Name;
            EditorGUILayout.LabelField($"{i + 1}. {effectName}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                _effectsProperty.DeleteArrayElementAtIndex(i);
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
            AddEffect();
        EditorGUILayout.EndHorizontal();
    }

    private void AddEffect()
    {
        Undo.RecordObject(target, "Add Card Effect");
        int newIndex = _effectsProperty.arraySize;
        _effectsProperty.arraySize++;
        _effectsProperty.GetArrayElementAtIndex(newIndex).managedReferenceValue = CreateEffect(_selectedEffectType);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static CardEffect CreateEffect(int effectTypeIndex)
    {
        switch (effectTypeIndex)
        {
            case 0: return new AttackCardEffect();
            case 1: return new SupportCardEffect();
            case 2: return new ProtectionCardEffect();
            case 3: return new LightningCardEffect();
            case 4: return new HealCardEffect();
            case 5: return new DrawCardEffect();
            case 6: return new RemoveStatusCardEffect();
            case 7: return new ExtraTurnCardEffect();
            case 8: return new CleanseAttackCardEffect();
            case 9: return new SpecialCardEffect();
            case 10: return new AttackBoostCardEffect();
            case 11: return new ShieldStrikeCardEffect();
            default: throw new System.ArgumentOutOfRangeException(nameof(effectTypeIndex), effectTypeIndex, null);
        }
    }

    private void RenameAssetFromCard()
    {
        CardData card = (CardData)target;
        string assetPath = AssetDatabase.GetAssetPath(card);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("[CardDataEditor] Card is not a saved asset; nothing to rename.");
            return;
        }

        string baseName = string.IsNullOrWhiteSpace(card.cardName) ? "Card" : card.cardName;
        string hero = card.heroType == HeroType.All ? null : card.heroType.ToString();
        string className = card.classType == ClassType.All ? null : card.classType.ToString();

        string combined = baseName;
        if (hero != null) combined += " " + hero;
        if (className != null) combined += " " + className;

        string error = AssetDatabase.RenameAsset(assetPath, combined);
        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning($"[CardDataEditor] Rename failed: {error}");
    }

    private void StartListening()
    {
        if (_isWaitingForScan)
            return;

        _qrCodeReader = FindAnyObjectByType<QRCodeReader>();
        if (_qrCodeReader == null)
        {
            Debug.LogError("[CardDataEditor] No QRCodeReader exists in the active scene.");
            return;
        }

        _qrCodeReader.OnQRCodeScanned += AssignQrId;
        _isWaitingForScan = true;
    }

    private void StopListening()
    {
        if (_qrCodeReader != null)
            _qrCodeReader.OnQRCodeScanned -= AssignQrId;

        _qrCodeReader = null;
        _isWaitingForScan = false;
    }

    private void AssignQrId(string qrId)
    {
        CardData card = (CardData)target;
        Undo.RecordObject(card, "Assign Card QR ID");
        card.qrId = qrId;
        EditorUtility.SetDirty(card);
        AssetDatabase.SaveAssets();
        StopListening();
        Repaint();
    }
}