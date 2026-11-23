using System.Collections;
using UnityEngine;

// Owns the zombie's alpha: invisible unless a VisibilityLight covers it with no wall in between.
public class ZombieVisibility : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private LayerMask wallLayer; // Assign in Inspector: "Wall" layer only
    [Tooltip("Seconds between visibility checks.")]
    [SerializeField] private float checkInterval = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Coroutine fadeCoroutine;
    private bool isVisible;
    private float nextCheckTime;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetAlpha(0f);
    }

    void Update()
    {
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        bool lit = IsLit();
        if (lit != isVisible)
            SetVisibility(lit);
    }

    private bool IsLit()
    {
        Vector2 position = transform.position;
        foreach (VisibilityLight light in VisibilityLight.All)
        {
            if (light.Covers(position) && !Physics2D.Linecast(position, light.Position, wallLayer))
                return true;
        }
        return false;
    }

    private void SetVisibility(bool visible)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(visible ? 1f : 0f, fadeDuration));
        isVisible = visible;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = spriteRenderer.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration));
            yield return null;
        }
        SetAlpha(targetAlpha);
        fadeCoroutine = null;
    }

    // Changes alpha only, so a hit tint from ZombieHealth is kept.
    private void SetAlpha(float alpha)
    {
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }
}
