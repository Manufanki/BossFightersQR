using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardDatabase))]
public class CardDatabaseEditor : Editor
{
    private SerializedProperty _cardsProperty;

    private void OnEnable()
    {
        _cardsProperty = serializedObject.FindProperty("cards");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");

        EditorGUILayout.Space();
        if (GUILayout.Button("Import Cards From Folder"))
            ImportCardsFromFolder();

        serializedObject.ApplyModifiedProperties();
    }

    private void ImportCardsFromFolder()
    {
        CardDatabase database = (CardDatabase)target;
        string assetPath = AssetDatabase.GetAssetPath(database);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("[CardDatabaseEditor] Database is not a saved asset; save it first.");
            return;
        }

        string folder = Path.GetDirectoryName(assetPath);
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { folder });

        var found = new List<CardData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null && card != database)
                found.Add(card);
        }

        Undo.RecordObject(database, "Import Cards From Folder");

        _cardsProperty.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++)
            _cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue = found[i];

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CardDatabaseEditor] Imported {found.Count} card(s) from '{folder}'.");
    }
}
