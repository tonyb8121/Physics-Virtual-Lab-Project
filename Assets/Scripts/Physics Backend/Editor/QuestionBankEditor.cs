#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(QuestionBank))]
public class QuestionBankEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        QuestionBank bank = (QuestionBank)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Question Management", EditorStyles.boldLabel);

        // Show current question count
        EditorGUILayout.LabelField($"Current Questions: {bank.questions.Count}");

        EditorGUILayout.Space();

        // Button to load from JSON
        if (GUILayout.Button("Load Questions from JSON", GUILayout.Height(30)))
        {
            LoadQuestionsFromJSON(bank);
        }

        // Button to clear questions
        if (GUILayout.Button("Clear All Questions", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Questions", 
                "Are you sure you want to clear all questions?", 
                "Yes", "Cancel"))
            {
                bank.questions.Clear();
                EditorUtility.SetDirty(bank);
                AssetDatabase.SaveAssets();
                Debug.Log($"Cleared all questions from {bank.levelName}");
            }
        }
    }

    private void LoadQuestionsFromJSON(QuestionBank bank)
    {
        // Try multiple possible paths
        string[] possiblePaths = new string[]
        {
            $"Campaign/Level{bank.levelIndex}_Questions",
            $"Questions/Level{bank.levelIndex}_Questions",
            $"Campaign/Level{bank.levelIndex}Questions",
        };

        List<Question> loadedQuestions = null;

        foreach (string path in possiblePaths)
        {
            loadedQuestions = JsonLoader.LoadQuestions(path);
            
            if (loadedQuestions != null && loadedQuestions.Count > 0)
            {
                Debug.Log($"Found JSON at: {path}");
                break;
            }
        }

        if (loadedQuestions == null || loadedQuestions.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Could not find JSON file for Level {bank.levelIndex}.\n\n" +
                $"Tried paths:\n{string.Join("\n", possiblePaths)}\n\n" +
                $"Make sure the file exists in Resources folder.", 
                "OK");
            return;
        }

        // Ask user if they want to replace or append
        bool replace = EditorUtility.DisplayDialog("Load Questions",
            $"Found {loadedQuestions.Count} questions.\n\n" +
            $"Current count: {bank.questions.Count}\n\n" +
            $"Replace existing questions or append?",
            "Replace", "Append");

        if (replace)
        {
            bank.questions.Clear();
        }

        // Add the questions
        bank.questions.AddRange(loadedQuestions);

        // Mark as dirty and save
        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ Loaded {loadedQuestions.Count} questions into {bank.levelName}. Total: {bank.questions.Count}");
        
        EditorUtility.DisplayDialog("Success", 
            $"Loaded {loadedQuestions.Count} questions!\n\n" +
            $"Total questions in {bank.levelName}: {bank.questions.Count}", 
            "OK");
    }
}
#endif