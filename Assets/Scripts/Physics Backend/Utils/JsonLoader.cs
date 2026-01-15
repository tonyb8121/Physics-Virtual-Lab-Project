using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class JsonLoader
{
    /// <summary>
    /// Loads questions from a local JSON file in Resources.
    /// Path example: "Campaign/Level1_Questions" (no .json extension)
    /// </summary>
    public static List<Question> LoadQuestions(string resourcesPath)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(resourcesPath);
        
        if (jsonFile != null)
        {
            // Deserialize into our QuizData wrapper
            try {
                QuizData data = JsonConvert.DeserializeObject<QuizData>(jsonFile.text);
                return data.questions;
            }
            catch (System.Exception e) {
                Logger.LogError($"JSON Parse Error in {resourcesPath}: {e.Message}");
                return new List<Question>();
            }
        }
        else
        {
            Logger.LogError($"JSON file not found at: {resourcesPath}");
            return new List<Question>();
        }
    }
}