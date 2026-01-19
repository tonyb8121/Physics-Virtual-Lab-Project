using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TrailRenderer))]
public class ProjectileBehavior : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip launchSound;
    public AudioClip landSound;
    public bool generateSoundsIfMissing = true; // Option to auto-generate sounds
    private AudioSource audioSource;
    
    [Header("Particle Effects")]
    public ParticleSystem launchParticles;
    public ParticleSystem trailParticles;
    
    [Header("Physics Data")]
    public float launchHeight = 0f;
    public float timeOfFlight = 0f;
    public float maxHeight = 0f;
    public float range = 0f;
    
    private Rigidbody rb;
    private TrailRenderer trail;
    private Vector3 launchPosition;
    private Vector3 initialVelocity;
    private float startTime;
    private bool hasLanded = false;
    private float highestY = 0f;

    public UnityEvent onLanded = new UnityEvent();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
        
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 0.6f;
        
        // Configure trail
        trail.time = 10f;
        trail.startWidth = 0.1f;
        trail.endWidth = 0.05f;
        
        // Create gradient for trail (yellow to red)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.yellow, 0.0f), 
                new GradientColorKey(Color.red, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        trail.colorGradient = gradient;
    }

    void Start()
    {
        // Generate sounds if missing and generation is enabled
        if (generateSoundsIfMissing)
        {
            if (launchSound == null)
            {
                launchSound = GenerateLaunchSound();
                Debug.Log("✓ Generated launch sound for projectile");
            }
            
            if (landSound == null)
            {
                landSound = GenerateLandSound();
                Debug.Log("✓ Generated land sound for projectile");
            }
        }
    }

    public void Launch(Vector3 velocity)
    {
        initialVelocity = velocity;
        launchPosition = transform.position;
        launchHeight = launchPosition.y;
        startTime = Time.time;
        highestY = launchPosition.y;
        hasLanded = false;

        rb.linearVelocity = velocity;
        rb.useGravity = true;
        
        // Play launch sound with slight volume variation
        if (launchSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f); // Slight pitch variation
            audioSource.PlayOneShot(launchSound, 0.8f);
            audioSource.pitch = 1f; // Reset pitch
        }
        
        // Play launch particles
        if (launchParticles != null)
            launchParticles.Play();
        
        // Start trail particles if attached
        if (trailParticles != null)
            trailParticles.Play();
    }

    void FixedUpdate()
    {
        if (hasLanded) return;

        // Track maximum height
        if (transform.position.y > highestY)
            highestY = transform.position.y;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        hasLanded = true;
        timeOfFlight = Time.time - startTime;
        
        // Calculate max height RELATIVE to launch height
        maxHeight = highestY - launchHeight;
        
        // Calculate horizontal range
        Vector3 landingPos = transform.position;
        Vector3 horizontalLaunch = new Vector3(launchPosition.x, 0, launchPosition.z);
        Vector3 horizontalLanding = new Vector3(landingPos.x, 0, landingPos.z);
        range = Vector3.Distance(horizontalLaunch, horizontalLanding);

        // Stop movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        
        // Play land sound with impact velocity-based volume
        if (landSound != null && audioSource != null)
        {
            float impactVelocity = collision.relativeVelocity.magnitude;
            float volume = Mathf.Clamp(impactVelocity / 10f, 0.3f, 1f);
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(landSound, volume);
            audioSource.pitch = 1f;
        }
        
        // Stop particles
        if (trailParticles != null)
            trailParticles.Stop();

        // Invoke event
        onLanded.Invoke();
        
        Debug.Log($"=== PROJECTILE LANDED ===");
        Debug.Log($"Launch Height: {launchHeight:F2} m");
        Debug.Log($"Time of Flight: {timeOfFlight:F2} s");
        Debug.Log($"Max Height Above Launch: {maxHeight:F2} m");
        Debug.Log($"Horizontal Range: {range:F2} m");
    }

    // ==========================================
    // AUDIO GENERATION METHODS
    // ==========================================
    
    /// <summary>
    /// Generates a cannon/explosion-like launch sound
    /// </summary>
    AudioClip GenerateLaunchSound()
    {
        int sampleRate = 44100;
        float duration = 0.4f;
        int samples = (int)(sampleRate * duration);
        
        AudioClip clip = AudioClip.Create("GeneratedLaunchSound", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            
            // Explosive sound - combination of frequencies
            float freq1 = Mathf.Lerp(1200f, 80f, t / duration); // High to low sweep
            float freq2 = Mathf.Lerp(600f, 40f, t / duration);  // Lower harmonic
            float freq3 = 150f; // Bass component
            
            // Mix frequencies
            float wave1 = Mathf.Sin(2 * Mathf.PI * freq1 * t);
            float wave2 = Mathf.Sin(2 * Mathf.PI * freq2 * t);
            float wave3 = Mathf.Sin(2 * Mathf.PI * freq3 * t);
            
            // Add some noise for texture
            float noise = Random.Range(-0.3f, 0.3f);
            
            // Combine with exponential decay
            float envelope = Mathf.Exp(-6f * t);
            data[i] = (wave1 * 0.3f + wave2 * 0.3f + wave3 * 0.2f + noise * 0.2f) * envelope;
            
            // Clamp to prevent distortion
            data[i] = Mathf.Clamp(data[i], -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    /// <summary>
    /// Generates a thud/impact landing sound
    /// </summary>
    AudioClip GenerateLandSound()
    {
        int sampleRate = 44100;
        float duration = 0.2f;
        int samples = (int)(sampleRate * duration);
        
        AudioClip clip = AudioClip.Create("GeneratedLandSound", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            
            // Thud sound - low frequency with very quick decay
            float freq1 = 120f; // Deep thud
            float freq2 = 80f;  // Even deeper rumble
            
            float wave1 = Mathf.Sin(2 * Mathf.PI * freq1 * t);
            float wave2 = Mathf.Sin(2 * Mathf.PI * freq2 * t);
            
            // Add impact click at the start
            float click = 0f;
            if (t < 0.01f)
            {
                click = Mathf.Sin(2 * Mathf.PI * 2000f * t) * (1f - t / 0.01f);
            }
            
            // Very fast decay for impact
            float envelope = Mathf.Exp(-25f * t);
            
            // Add some noise for realism
            float noise = Random.Range(-0.2f, 0.2f) * envelope;
            
            data[i] = (wave1 * 0.4f + wave2 * 0.3f + click * 0.2f + noise * 0.1f) * envelope;
            
            // Clamp
            data[i] = Mathf.Clamp(data[i], -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
}