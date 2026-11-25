using System.Collections;
using TMPro;
using UnityEngine;

// Full-screen black overlay with a title card ("LEVEL 2"), drawn over the HUD.
// Blocks clicks only while it's visible.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private TMP_Text card;

    private CanvasGroup group;

    public float Alpha => Group.alpha;

    private CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

    public void SetAlpha(float alpha)
    {
        Group.alpha = alpha;
        Group.blocksRaycasts = alpha > 0.001f;
        Group.interactable = false;
    }

    public IEnumerator FadeTo(float alpha, float duration)
    {
        float start = Group.alpha;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetAlpha(Mathf.Lerp(start, alpha, t / duration));
            yield return null;
        }
        SetAlpha(alpha);
    }

    // Empty or null hides the card.
    public void ShowCard(string text)
    {
        if (card == null) return;
        card.text = text ?? string.Empty;
        card.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }
}
