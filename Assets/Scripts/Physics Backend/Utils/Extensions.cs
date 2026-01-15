using UnityEngine;
using System.Text.RegularExpressions;

/// <summary>
/// Extension methods for Transform, Color, String, and GameObject.
/// </summary>
public static class Extensions
{
    #region Transform Extensions
    /// <summary>
    /// Resets position, rotation, and scale to default.
    /// </summary>
    public static void ResetTransform(this Transform trans)
    {
        trans.position = Vector3.zero;
        trans.localRotation = Quaternion.identity;
        trans.localScale = Vector3.one;
    }
    #endregion

    #region Color Extensions
    /// <summary>
    /// Returns a new color with modified alpha.
    /// </summary>
    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
    #endregion

    #region String Extensions
    /// <summary>
    /// Validates email format using Regex.
    /// </summary>
    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    /// <summary>
    /// Converts PascalCase to snake_case.
    /// </summary>
    public static string ToSnakeCase(this string text)
    {
        if (text == null) return string.Empty;
        if (text.Length < 2) return text.ToLowerInvariant();

        var sb = new System.Text.StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        for (int i = 1; i < text.Length; ++i)
        {
            char c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    #endregion

    #region GameObject Extensions
    /// <summary>
    /// Gets component if it exists, otherwise adds it.
    /// </summary>
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
    #endregion
}