using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// After LevelRun's backup timer fires, points at every canister the player hasn't found: one arrow per
// off-screen canister, pinned to the screen edge. Arrows hide while their canister is on screen.
public class CanisterCompass : MonoBehaviour
{
    [SerializeField] private LevelRun run;
    [Tooltip("Full-screen container the arrows live in; keep it below Game Over and the fader.")]
    [SerializeField] private RectTransform arrowParent;
    [Tooltip("Distance (canvas px) between an arrow and the screen edge.")]
    [SerializeField] private float edgePadding = 48f;
    [SerializeField] private float arrowSize = 36f;
    [SerializeField] private Color arrowColor = new Color(1f, 0.72f, 0.1f, 0.95f);
    [Tooltip("Viewport margin inside which a canister counts as on screen.")]
    [SerializeField] private float onScreenMargin = 0.02f;

    private readonly List<Image> arrows = new List<Image>();
    private Sprite arrowSprite;
    private Camera cam;

    void Awake()
    {
        arrowSprite = MakeTriangle(32);
    }

    // The arrow texture and sprite are runtime objects: free them, or every scene reload leaks a pair.
    void OnDestroy()
    {
        if (arrowSprite == null) return;
        Destroy(arrowSprite.texture);
        Destroy(arrowSprite);
    }

    void LateUpdate()
    {
        int used = 0;
        if (cam == null) cam = Camera.main;
        if (run != null && run.BackupActive && cam != null)
        {
            Rect area = arrowParent.rect;
            Vector2 half = new Vector2(area.width * 0.5f - edgePadding, area.height * 0.5f - edgePadding);

            foreach (var canister in FuelCanister.All)
            {
                if (canister == null || canister.Found) continue;

                Vector3 v = cam.WorldToViewportPoint(canister.transform.position);
                if (v.x > onScreenMargin && v.x < 1f - onScreenMargin && v.y > onScreenMargin && v.y < 1f - onScreenMargin)
                    continue;

                // Direction from the screen center, in canvas units; push it out to the padded edge.
                Vector2 dir = new Vector2((v.x - 0.5f) * area.width, (v.y - 0.5f) * area.height);
                if (dir.sqrMagnitude < 0.0001f) continue;
                float scale = Mathf.Min(half.x / (Mathf.Abs(dir.x) + 1e-5f), half.y / (Mathf.Abs(dir.y) + 1e-5f));

                var arrow = ArrowAt(used++);
                arrow.rectTransform.anchoredPosition = dir * scale;
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                arrow.enabled = true;
            }
        }

        for (int i = used; i < arrows.Count; i++) arrows[i].enabled = false;
    }

    private Image ArrowAt(int index)
    {
        while (arrows.Count <= index)
        {
            var go = new GameObject("Canister Arrow", typeof(RectTransform), typeof(Image));
            go.layer = arrowParent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(arrowParent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(arrowSize, arrowSize);
            var image = go.GetComponent<Image>();
            image.sprite = arrowSprite;
            image.color = arrowColor;
            image.raycastTarget = false;
            image.enabled = false;
            arrows.Add(image);
        }
        return arrows[index];
    }

    // A triangle pointing along +X, so rotating by the direction angle aims it.
    private static Sprite MakeTriangle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Inside when |y - mid| <= half-height shrinking linearly toward the tip at x = size.
                float fx = (x + 0.5f) / size, fy = (y + 0.5f) / size;
                bool inside = Mathf.Abs(fy - 0.5f) <= 0.5f * (1f - fx);
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
