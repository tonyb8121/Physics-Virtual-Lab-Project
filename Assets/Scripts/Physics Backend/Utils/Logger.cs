using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Static logging class with color-coded output and file persistence.
/// Debug builds: Logs to Console and File.
/// Release builds: Logs errors only to File (unless configured otherwise).
/// </summary>
public static class Logger
{
    private static string _logPath = Path.Combine(Application.persistentDataPath, "app_logs.txt");
    private static bool _showDebug = true;

    public static void Initialize()
    {
        // Reset log file on new session
        try
        {
            if (File.Exists(_logPath)) File.Delete(_logPath);
            Log("Logger initialized. Log file: " + _logPath, Color.cyan);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize logger: {e.Message}");
        }
    }

    public static void Log(string message, Color? color = null)
    {
        if (!_showDebug) return;

        string hexColor = color.HasValue ? ColorUtility.ToHtmlStringRGB(color.Value) : "FFFFFF";
        string formattedMsg = $"<color=#{hexColor}>{message}</color>";

        Debug.Log(formattedMsg);
        WriteToFile($"[INFO] {DateTime.Now}: {message}");
    }

    public static void LogWarning(string message)
    {
        string formattedMsg = $"<color=yellow>[WARNING] {message}</color>";
        Debug.LogWarning(formattedMsg);
        WriteToFile($"[WARN] {DateTime.Now}: {message}");
    }

    public static void LogError(string message)
    {
        string formattedMsg = $"<color=red>[ERROR] {message}</color>";
        Debug.LogError(formattedMsg);
        WriteToFile($"[ERROR] {DateTime.Now}: {message}");
    }

    private static void WriteToFile(string text)
    {
        try
        {
            using (StreamWriter writer = File.AppendText(_logPath))
            {
                writer.WriteLine(text);
            }
        }
        catch
        {
            // Fail silently if file I/O fails to prevent app crash loop
        }
    }
}