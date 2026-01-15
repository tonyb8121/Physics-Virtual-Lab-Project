using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class AIQuestionGenerator : MonoBehaviour
{
    private static AIQuestionGenerator _instance;
    public static AIQuestionGenerator Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AIQuestionGenerator");
                _instance = go.AddComponent<AIQuestionGenerator>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ============================================
    // API CONFIGURATION
    // ============================================
    private const string GEMINI_API_KEY = "AIzaSyDd68iFN-Zq2jyA_dRfBqtZlLb5vtiDmFg";
    private const string GEMINI_ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + GEMINI_API_KEY;

    private const string OPENROUTER_API_KEY = "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";
    private const string OPENROUTER_ENDPOINT = "https://openrouter.ai/api/v1/chat/completions";
    private const string OPENROUTER_MODEL = "qwen/qwen-2.5-7b-instruct";

    private const string GROQ_API_KEY = "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"; // Replace if needed
    private const string GROQ_ENDPOINT = "https://api.groq.com/openai/v1/chat/completions";
    private const string GROQ_MODEL = "llama-3.3-70b-versatile";

    private enum AIProvider { Gemini, OpenRouter, Groq }
    private AIProvider _lastSuccessfulProvider = AIProvider.Gemini;

    // ============================================
    // MAIN GENERATION
    // ============================================
    public IEnumerator GenerateQuestions(string topic, string level, int count, System.Action<List<Question>> callback)
    {
        bool isLoggedIn = AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null;
        AIProvider start = isLoggedIn ? AIProvider.Gemini : AIProvider.Groq; // Groq is now default backup

        Logger.Log($"🤖 Generating {count} questions ({topic}, {level}) - Starting with {start}");

        List<Question> questions = null;
        var providers = GetProviderSequence(start);

        foreach (var provider in providers)
        {
            Logger.Log($"⏳ Trying {provider}...");
            yield return TryProvider(provider, topic, level, count, result => questions = result);

            if (questions != null && questions.Count > 0)
            {
                RandomizeCorrectAnswers(questions);
                FinalizeQuestions(questions, topic, level);
                Logger.Log($"✅ {provider} succeeded: {questions.Count} questions", Color.green);
                callback?.Invoke(questions);
                _lastSuccessfulProvider = provider;
                yield break;
            }
        }

        Logger.LogError("❌ All AI providers failed.");
        callback?.Invoke(null);
    }

    private List<AIProvider> GetProviderSequence(AIProvider start)
    {
        // CHANGED: Order is now Gemini -> Groq -> OpenRouter
        var seq = new List<AIProvider> { AIProvider.Gemini, AIProvider.Groq, AIProvider.OpenRouter };
        int idx = seq.IndexOf(start);
        if (idx > 0)
        {
            var front = seq.GetRange(0, idx);
            seq.RemoveRange(0, idx);
            seq.AddRange(front);
        }
        return seq;
    }

    private IEnumerator TryProvider(AIProvider provider, string topic, string level, int count, System.Action<List<Question>> callback)
    {
        switch (provider)
        {
            case AIProvider.Gemini:     yield return GenerateWithGemini(topic, level, count, callback); break;
            case AIProvider.OpenRouter: yield return GenerateWithOpenRouter(topic, level, count, callback); break;
            case AIProvider.Groq:       yield return GenerateWithGroq(topic, level, count, callback); break;
        }
    }

    // ============================================
    // GEMINI
    // ============================================
    private IEnumerator GenerateWithGemini(string topic, string level, int count, System.Action<List<Question>> callback)
    {
        string prompt = BuildPrompt(topic, level, count);

        var body = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.8f, maxOutputTokens = 4000 }
        };

        string json = JsonConvert.SerializeObject(body);
        byte[] data = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(GEMINI_ENDPOINT, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(data);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 40;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var root = JsonConvert.DeserializeObject<GeminiResponseRoot>(req.downloadHandler.text);
                string text = root?.candidates?[0]?.content?.parts?[0]?.text;
                callback?.Invoke(ParseQuestionJSON(text));
            }
            else
            {
                Logger.LogError($"Gemini failed: {req.error}");
                callback?.Invoke(null);
            }
        }
    }

    // ============================================
    // OPENROUTER
    // ============================================
    private IEnumerator GenerateWithOpenRouter(string topic, string level, int count, System.Action<List<Question>> callback)
    {
        string prompt = BuildPrompt(topic, level, count);

        var body = new
        {
            model = OPENROUTER_MODEL,
            messages = new[]
            {
                new { role = "system", content = "Output ONLY a valid JSON array of question objects. No extra text or markdown." },
                new { role = "user", content = prompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.8f,
            max_tokens = 4000
        };

        string json = JsonConvert.SerializeObject(body);
        byte[] data = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(OPENROUTER_ENDPOINT, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(data);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {OPENROUTER_API_KEY}");
            req.SetRequestHeader("HTTP-Referer", "https://arphysicslab.com");
            req.SetRequestHeader("X-Title", "AR Physics Lab");
            req.timeout = 40;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var root = JsonConvert.DeserializeObject<OpenAIStyleResponse>(req.downloadHandler.text);
                string text = root?.choices?[0]?.message?.content;
                callback?.Invoke(ParseQuestionJSON(text));
            }
            else
            {
                Logger.LogError($"OpenRouter failed: {req.error}");
                callback?.Invoke(null);
            }
        }
    }

    // ============================================
    // GROQ
    // ============================================
    private IEnumerator GenerateWithGroq(string topic, string level, int count, System.Action<List<Question>> callback)
    {
        string prompt = BuildPrompt(topic, level, count);

        var body = new
        {
            model = GROQ_MODEL,
            messages = new[]
            {
                new { role = "system", content = "Output ONLY a valid JSON array of question objects. No extra text or markdown." },
                new { role = "user", content = prompt }
            },
            temperature = 0.8f,
            max_tokens = 4000
        };

        string json = JsonConvert.SerializeObject(body);
        byte[] data = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(GROQ_ENDPOINT, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(data);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {GROQ_API_KEY}");
            req.timeout = 40;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var root = JsonConvert.DeserializeObject<OpenAIStyleResponse>(req.downloadHandler.text);
                string text = root?.choices?[0]?.message?.content;
                callback?.Invoke(ParseQuestionJSON(text));
            }
            else
            {
                Logger.LogError($"Groq failed: {req.error}");
                callback?.Invoke(null);
            }
        }
    }

    // ============================================
    // GET EXPLANATION (FIXED CLEANUP)
    // ============================================
  public IEnumerator GetExplanation(string questionText, string correctAnswer, System.Action<string> callback)
{
    string prompt = $@"
You are a KCSE Physics tutor. 
Question: '{questionText}'
Correct Answer: '{correctAnswer}'

Explain why the answer is correct.
Keep it under 100 words.
IMPORTANT:
- Output PLAIN TEXT ONLY.
- DO NOT use any tags (no HTML, no Markdown).
- DO NOT add a header like 'AI Explanation:'. Start directly with the explanation.
";

    string result = null;
    // CHANGED: Provider order to prefer Groq over OpenRouter here too
    var providers = new List<AIProvider> { _lastSuccessfulProvider, AIProvider.Gemini, AIProvider.Groq, AIProvider.OpenRouter };
    providers = providers.Distinct().ToList();

    foreach (var p in providers)
    {
        if (p == AIProvider.Gemini)
            yield return GetExplanationGemini(prompt, r => result = r);
        else
            yield return GetExplanationOpenAIStyle(
                p == AIProvider.OpenRouter ? OPENROUTER_ENDPOINT : GROQ_ENDPOINT,
                p == AIProvider.OpenRouter ? OPENROUTER_API_KEY : GROQ_API_KEY,
                p == AIProvider.OpenRouter ? OPENROUTER_MODEL : GROQ_MODEL,
                prompt,
                r => result = r);

        if (!string.IsNullOrEmpty(result))
        {
            _lastSuccessfulProvider = p;
            break;
        }
    }

    if (!string.IsNullOrEmpty(result))
    {
        // --- NUCLEAR CLEANUP - removes ALL tags and labels first ---
        
        // Remove ALL angle bracket tags (including <color=cyan>, etc.)
        result = System.Text.RegularExpressions.Regex.Replace(result, "<[^>]*>", string.Empty);
        
        // Remove markdown
        result = result.Replace("**", "").Replace("*", "");
        
        // Split into lines and filter out label-only lines
        var lines = result.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        var cleanLines = new List<string>();
        
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            // Skip lines that are ONLY labels
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^(AI\s*)?(Explanation|Answer):?\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                continue;
            
            // Remove inline labels at start of line
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^(AI\s*)?(Explanation|Answer):?\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (!string.IsNullOrWhiteSpace(trimmed))
                cleanLines.Add(trimmed);
        }
        
        result = string.Join("\n", cleanLines).Trim();
        
        // Add our single clean header
        result = $"<color=#00FFFF><b>AI Explanation:</b></color>\n{result}";
        
        // --- CLEANUP LOGIC END ---
    }
    else
    {
        result = "Sorry, explanation unavailable.";
    }

    callback?.Invoke(result);
}
    private IEnumerator GetExplanationGemini(string prompt, System.Action<string> callback)
    {
        var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        string json = JsonConvert.SerializeObject(body);
        byte[] data = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(GEMINI_ENDPOINT, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(data);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 20;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var root = JsonConvert.DeserializeObject<GeminiResponseRoot>(req.downloadHandler.text);
                callback?.Invoke(root?.candidates?[0]?.content?.parts?[0]?.text);
            }
            else callback?.Invoke(null);
        }
    }

    private IEnumerator GetExplanationOpenAIStyle(string endpoint, string key, string model, string prompt, System.Action<string> callback)
    {
        var body = new { model = model, messages = new[] { new { role = "user", content = prompt } }, max_tokens = 300 };
        string json = JsonConvert.SerializeObject(body);
        byte[] data = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(data);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {key}");
            if (endpoint.Contains("openrouter"))
            {
                req.SetRequestHeader("HTTP-Referer", "https://arphysicslab.com");
                req.SetRequestHeader("X-Title", "AR Physics Lab");
            }
            req.timeout = 20;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var root = JsonConvert.DeserializeObject<OpenAIStyleResponse>(req.downloadHandler.text);
                callback?.Invoke(root?.choices?[0]?.message?.content);
            }
            else callback?.Invoke(null);
        }
    }

    private string BuildPrompt(string topic, string level, int count)
    {
        return $@"
You are an expert Physics teacher creating questions for the Kenyan KCSE exam.

Generate EXACTLY {count} multiple-choice questions on the following:

Topic: {topic}
Difficulty Level: {level}

YOUR RESPONSE MUST BE ONLY A VALID JSON ARRAY OF COMPLETE QUESTION OBJECTS.

Correct example (you must follow this structure exactly):

[
  {{
    ""questionText"": ""What is the main characteristic of transverse waves?"",
    ""options"": [
      ""A: Particles vibrate parallel to the wave direction"",
      ""B: Particles vibrate perpendicular to the wave direction"",
      ""C: They cannot travel through a vacuum"",
      ""D: They require a medium only""
    ],
    ""correctAnswerIndex"": 1,
    ""explanation"": ""In transverse waves, particle vibration is perpendicular to the direction of wave propagation."",
    ""hint1"": ""Think about a rope wave."",
    ""hint2"": ""Compare with sound waves which are longitudinal.""
  }}
]

MANDATORY RULES — DO NOT BREAK ANY:
- Output ONLY the JSON array above. Nothing else.
- NO explanatory text, NO introductions, NO numbering.
- NO markdown code blocks (no ```json or ```).
- NEVER output just a list of options like [""A: ..."", ""B: ...""]
- Every object MUST have these exact fields: questionText, options (4 strings starting with A:, B:, C:, D:), correctAnswerIndex (0–3, randomized), explanation, hint1, hint2.
- The entire response must be directly parsable as a JSON array of objects.

Generate the {count} questions now in this exact format only.";
    }


    // ============================================
    // FIX: RANDOMIZATION & ORDERING
    // ============================================
    private void RandomizeCorrectAnswers(List<Question> questions)
    {
        foreach (var q in questions)
        {
            // FIX: Using .Length for array
            if (q.options == null || q.options.Length == 0) continue;

            // FIX: Using .Length
            int validIndex = Mathf.Clamp(q.correctAnswerIndex, 0, q.options.Length - 1);
            
            string originalCorrectText = q.options[validIndex];
            
            // Strip prefix from the correct answer so we can match it later
            originalCorrectText = System.Text.RegularExpressions.Regex.Replace(originalCorrectText, "^[A-D][.:)]\\s*", "").Trim();

            // 2. Clean ALL options (Strip A:, B:, etc.)
            List<string> cleanOptions = new List<string>();
            foreach (var opt in q.options)
            {
                string clean = System.Text.RegularExpressions.Regex.Replace(opt, "^[A-D][.:)]\\s*", "").Trim();
                cleanOptions.Add(clean);
            }

            // 3. Shuffle the clean options
            for (int i = 0; i < cleanOptions.Count; i++)
            {
                string temp = cleanOptions[i];
                int randomIndex = UnityEngine.Random.Range(i, cleanOptions.Count);
                cleanOptions[i] = cleanOptions[randomIndex];
                cleanOptions[randomIndex] = temp;
            }

            // 4. Find where the correct answer moved to
            int newCorrectIndex = -1;
            for(int i = 0; i < cleanOptions.Count; i++)
            {
                if(cleanOptions[i] == originalCorrectText) 
                {
                    newCorrectIndex = i;
                    break;
                }
            }

            // Fallback
            if (newCorrectIndex == -1) newCorrectIndex = 0; 

            q.correctAnswerIndex = newCorrectIndex;

            // 5. Re-add prefixes (A, B, C, D)
            string[] prefixes = { "A: ", "B: ", "C: ", "D: " };
            for (int i = 0; i < cleanOptions.Count; i++)
            {
                if (i < prefixes.Length)
                {
                    cleanOptions[i] = prefixes[i] + cleanOptions[i];
                }
            }

            // FIX: Convert back to Array
            q.options = cleanOptions.ToArray();
        }
    }

    private void FinalizeQuestions(List<Question> questions, string topic, string level)
    {
        string teacherId = (AuthManager.Instance?.CurrentUser != null) ? AuthManager.Instance.CurrentUser.UserId : "AI";
        foreach (var q in questions)
        {
            q.moduleId = topic;
            q.difficulty = level;
            q.teacherId = teacherId;
            q.points = 10;
            q.timeLimit = 60;
            q.hint1 = string.IsNullOrEmpty(q.hint1) ? "" : q.hint1;
            q.hint2 = string.IsNullOrEmpty(q.hint2) ? "" : q.hint2;
        }
    }

    private List<Question> ParseQuestionJSON(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return null;

        try
        {
            rawText = rawText.Trim().Replace("```json", "").Replace("```", "").Trim();

            int start = rawText.IndexOf('[');
            int end = rawText.LastIndexOf(']');
            if (start >= 0 && end > start)
                rawText = rawText.Substring(start, end - start + 1);

            var list = JsonConvert.DeserializeObject<List<Question>>(rawText);
            return list?.Count > 0 ? list : null;
        }
        catch (System.Exception e)
        {
            Logger.LogError($"JSON parse failed: {e.Message}\nPreview: {rawText.Substring(0, Mathf.Min(300, rawText.Length))}");
            return null;
        }
    }

    // ============================================
    // RESPONSE MODELS
    // ============================================
    [System.Serializable] private class GeminiResponseRoot { public GeminiCandidate[] candidates; }
    [System.Serializable] private class GeminiCandidate { public GeminiContent content; }
    [System.Serializable] private class GeminiContent { public GeminiPart[] parts; }
    [System.Serializable] private class GeminiPart { public string text; }

    [System.Serializable] private class OpenAIStyleResponse { public Choice[] choices; }
    [System.Serializable] private class Choice { public Message message; }
    [System.Serializable] private class Message { public string content; }
}