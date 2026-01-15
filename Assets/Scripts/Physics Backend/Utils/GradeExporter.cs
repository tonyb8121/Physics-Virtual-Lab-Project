using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Exports student grades to CSV format for Excel/Google Sheets.
/// </summary>
public static class GradeExporter
{
    /// <summary>
    /// Generates a CSV format string from submissions, including Admission Number.
    /// </summary>
    public static string GenerateCSV(List<StudentSubmission> submissions, string assignmentTitle)
    {
        StringBuilder sb = new StringBuilder();
        
        // --- HEADER ---
        sb.AppendLine($"Report for: {assignmentTitle}");
        sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine("");
        
        // FIX: Include Admission No in the column header
        sb.AppendLine("Admission No,Student Name,Score,Total,Percentage,Submitted Date");

        // --- DATA ROWS ---
        foreach (var sub in submissions)
        {
            string date = new System.DateTime(sub.submittedAt).ToString("yyyy-MM-dd HH:mm");
            
            // FIX: Include sub.admissionNumber at the start of the data row
            // Assuming sub.admissionNumber is a string property on StudentSubmission
            sb.AppendLine($"{sub.admissionNumber},{sub.studentName},{sub.score},{sub.totalQuestions},{sub.percentage:F1}%,{date}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Saves CSV to device storage and copies to clipboard.
    /// </summary>
    public static void SaveToFile(string content, string fileName)
    {
        // Use Application.persistentDataPath for platform-independent storage
        string path = Path.Combine(Application.persistentDataPath, fileName);
        
        try
        {
            // Write the content to the file
            File.WriteAllText(path, content);
            
            // Copy to clipboard for easy Excel paste
            GUIUtility.systemCopyBuffer = content;
            
            // Assuming Logger and UIManager.Instance.ShowToast exist
            Logger.Log($"Exported to: {path}", Color.green);
            UIManager.Instance.ShowToast($"Saved & Copied to Clipboard!");
            
            // Optional: Open file location on PC
            #if UNITY_EDITOR || UNITY_STANDALONE
            // Check if the directory exists before trying to open it
            if (Directory.Exists(Application.persistentDataPath))
            {
                System.Diagnostics.Process.Start(Application.persistentDataPath);
            }
            #endif
        }
        catch (System.Exception e)
        {
            Logger.LogError("Export failed: " + e.Message);
            UIManager.Instance.ShowToast("Export Failed.");
        }
    }
}