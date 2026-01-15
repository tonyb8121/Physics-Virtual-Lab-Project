using UnityEngine;

[CreateAssetMenu(fileName = "UITheme", menuName = "ARPhysicsLab/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Colors")]
    public Color primaryColor = new Color(0.05f, 0.1f, 0.16f, 1f); // Dark Blue #0D1B2A
    public Color accentColor = new Color(0.12f, 0.56f, 1f, 1f); // Cyan #1F8FFF
    public Color successColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    public Color warningColor = new Color(1f, 0.7f, 0.2f, 1f); // Orange
    public Color errorColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
    
    [Header("Text")]
    public Color textPrimary = Color.white;
    public Color textSecondary = new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
}