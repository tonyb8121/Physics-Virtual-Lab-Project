using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform pivot;
    public Transform barrel;
    public Transform barrelTip;
    public GameObject projectilePrefab;

    [Header("UI Controls")]
    public Slider angleSlider;
    public Slider velocitySlider;
    public TMP_Dropdown gravityDropdown;
    public Button launchButton;

    [Header("UI Display References")]
    public TextMeshProUGUI angleLabel;         
    public TextMeshProUGUI velocityLabel;      
    public TextMeshProUGUI gravityLabel;        // Optional - displays the current gravity name

    [Header("Launch Settings")]
    public bool useInstantiate = true;
    public float projectileAutoDestroy = 10f;

    private readonly float[] gravityValues = { 9.81f, 1.62f, 3.71f, 24.79f, 0f };

    private readonly string[] gravityNames = 
        { "Earth (9.81)", "Moon (1.62)", "Mars (3.71)", "Jupiter (24.79)", "Zero (0.00)" };

    void Start()
    {
        // Safety checks
        if (pivot == null || barrel == null || barrelTip == null)
            Debug.LogWarning("Basic references missing!");

        if (angleSlider == null || velocitySlider == null || gravityDropdown == null || launchButton == null)
            Debug.LogWarning("UI references missing!");

        // Hook UI
        angleSlider.onValueChanged.AddListener(OnAngleChanged);
        velocitySlider.onValueChanged.AddListener(OnVelocityChanged);
        gravityDropdown.onValueChanged.AddListener(OnGravityChanged);
        launchButton.onClick.AddListener(OnLaunchClicked);

        OnAngleChanged(angleSlider.value);
        OnVelocityChanged(velocitySlider.value);
        OnGravityChanged(gravityDropdown.value);
    }

    void OnDestroy()
    {
        angleSlider.onValueChanged.RemoveAllListeners();
        velocitySlider.onValueChanged.RemoveAllListeners();
        gravityDropdown.onValueChanged.RemoveAllListeners();
        launchButton.onClick.RemoveAllListeners();
    }

    void OnAngleChanged(float val)
    {
        pivot.localEulerAngles = new Vector3(0f, 0f, -val);

        if (angleLabel != null)
            angleLabel.text = $"{val:F1}°";
    }

    void OnVelocityChanged(float val)
    {
        if (velocityLabel != null)
            velocityLabel.text = $"{val:F1} m/s";
    }

    void OnGravityChanged(int index)
    {
        if (index < 0 || index >= gravityValues.Length) return;

        float g = gravityValues[index];
        Physics.gravity = new Vector3(0f, -g, 0f);

        if (gravityLabel != null)
            gravityLabel.text = "Gravity: " + gravityNames[index];
    }

    void OnLaunchClicked()
    {
        // launchButton.interactable = false;
        FireProjectile();
    }

    void FireProjectile()
    {
        Vector3 dir = barrel.transform.up.normalized;
        float speed = velocitySlider.value;

        GameObject projGO = Instantiate(projectilePrefab, barrelTip.position, Quaternion.identity);

        Rigidbody rb = projGO.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Projectile needs a Rigidbody!");
            return;
        }

        rb.linearVelocity = dir * speed;
        rb.angularVelocity = Vector3.zero;

        ProjectileBehavior pb = projGO.GetComponent<ProjectileBehavior>();
        if (pb != null)
        {
            pb.onLanded.RemoveAllListeners();
            pb.onLanded.AddListener(() =>
            {
                launchButton.interactable = true;
            });
        }
        else
        {
            StartCoroutine(ReenableLaunchAfter(projectileAutoDestroy + 0.1f));
        }

        Destroy(projGO, projectileAutoDestroy);
    }

    System.Collections.IEnumerator ReenableLaunchAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        launchButton.interactable = true;
    }
}
