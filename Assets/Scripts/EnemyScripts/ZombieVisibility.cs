using UnityEngine;

public class ZombieVisibility : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Coroutine fadeCoroutine;
    private bool isVisible;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private LayerMask wallLayer; // Assign in Inspector: "Wall" layer only

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Flashlight") && !isVisible)
        {
            CheckVisibility(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Flashlight") && !isVisible)
        {
            CheckVisibility(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Flashlight") && isVisible)
        {
            SetVisibility(false);
        }
    }

    private void CheckVisibility(Collider2D flashlightCollider)
    {
        if (!WallBlockingLight(flashlightCollider))
        {
            SetVisibility(true);
            Debug.DrawLine(transform.position, flashlightCollider.transform.position, Color.green, 1f);
        }
    }

    private bool WallBlockingLight(Collider2D flashlightCollider)
    {
        Vector2 origin = transform.position;
        Vector2 direction = (flashlightCollider.transform.position - transform.position).normalized;
        float distance = Vector2.Distance(origin, flashlightCollider.transform.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, wallLayer);
        return hit.collider != null;
    }

    private void SetVisibility(bool visible)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(visible ? 1f : 0f, fadeDuration));
        isVisible = visible;
    }

    private System.Collections.IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = spriteRenderer.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            spriteRenderer.color = new Color(1f, 1f, 1f, newAlpha);
            yield return null;
        }
        spriteRenderer.color = new Color(1f, 1f, 1f, targetAlpha);
    }
}