using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// A fuel canister on the ground. Hidden (alpha 0) until one of the player's lights reveals it, by the same
// rule as zombies (VisibilityLight.IsLit). Once found it stays visible and glows: unlit sprite, a pulsing
// warm halo light and a sparkle burst. LevelRun's backup timer can turn the glow on early (SetBeacon) for
// canisters the player hasn't found. Walking over it picks it up; LevelRun counts the pickups.
// Spawned by BlockBuilder under the block's Generated root, so a new block clears leftovers.
[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class FuelCanister : MonoBehaviour
{
    private static readonly List<FuelCanister> canisters = new List<FuelCanister>();
    public static IReadOnlyList<FuelCanister> All => canisters;
    public static event Action PickedUp;
    // While set, lights don't reveal canisters: LevelRun sets it during the car cutscene, so the
    // headlights can't find fuel before the player has control.
    public static bool RevealPaused { get; set; }

    [Header("Reveal")]
    [SerializeField] private LayerMask wallLayer;
    [Tooltip("Seconds between reveal checks.")]
    [SerializeField] private float checkInterval = 0.05f;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Glow")]
    [Tooltip("Swapped in on reveal so the canister shows at full color in the dark.")]
    [SerializeField] private Material unlitMaterial;
    [SerializeField] private Light2D halo;
    [SerializeField] private float haloIntensity = 0.9f;
    [SerializeField] private float pulseHz = 1.5f;
    [Range(0f, 1f)] [SerializeField] private float pulseAmount = 0.3f;
    [Tooltip("Sprite brightness swing of the shimmer, on top of its base color.")]
    [Range(0f, 1f)] [SerializeField] private float shimmerAmount = 0.25f;
    [SerializeField] private ParticleSystem sparkleFx;

    private SpriteRenderer sprite;
    private Color baseColor;
    private float alpha;
    private bool glowing;
    private bool taken;
    private float nextCheckTime;

    // Revealed by the player's light (a backup beacon alone doesn't count).
    public bool Found { get; private set; }

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        baseColor = sprite.color;
        if (halo != null) halo.enabled = false;
        ApplyColor(1f);
    }

    void OnEnable() => canisters.Add(this);
    void OnDisable() => canisters.Remove(this);

    void Update()
    {
        if (!Found && !RevealPaused && Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            if (VisibilityLight.IsLit(transform.position, wallLayer))
            {
                Found = true;
                // A beacon is already glowing; finding it still earns the sparkle.
                if (glowing) PlaySparkle();
                else GlowOn(sparkle: true);
            }
        }

        if (!glowing) return;

        if (alpha < 1f)
            alpha = fadeDuration > 0f ? Mathf.Min(1f, alpha + Time.deltaTime / fadeDuration) : 1f;

        // Shimmer: halo and sprite pulse together.
        float wave = Mathf.Sin(Time.time * 2f * Mathf.PI * pulseHz);
        if (halo != null) halo.intensity = haloIntensity * (1f + pulseAmount * wave) * alpha;
        ApplyColor(1f + shimmerAmount * wave);
    }

    // Backup: glow through the dark without counting as found. One-way: the canister keeps glowing
    // until it is picked up or its block is cleared.
    public void MakeBeacon()
    {
        if (!glowing) GlowOn(sparkle: false);
    }

    private void GlowOn(bool sparkle)
    {
        if (glowing) return;
        glowing = true;
        if (unlitMaterial != null) sprite.sharedMaterial = unlitMaterial;
        if (halo != null) halo.enabled = true;
        if (sparkle) PlaySparkle();
    }

    private void PlaySparkle()
    {
        if (sparkleFx != null) sparkleFx.Play();
    }

    private void ApplyColor(float brightness)
    {
        var c = baseColor * brightness;
        c.a = alpha;
        sprite.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || !other.CompareTag(GameTags.Player)) return;
        taken = true;
        PickedUp?.Invoke();
        Destroy(gameObject);
    }
}
