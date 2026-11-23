using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class Menu : MonoBehaviour
{
    [Header("Panels")]
    public RectTransform menuPanel;
    public RectTransform weaponPanel;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public float slideDistance = 1000f;

    private bool showingWeapons = false;
    private bool isSliding = false;

    void Start()
    {
        // Initial positions
        menuPanel.anchoredPosition = Vector2.zero;
        weaponPanel.anchoredPosition = new Vector2(-slideDistance, 0);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(SceneNames.Arena1);
    }

    public void OpenWeapons()
    {
        if (isSliding) return;

        if (!showingWeapons)
        {
            StartCoroutine(Slide(menuPanel, menuPanel.anchoredPosition, new Vector2(slideDistance, 0)));
            StartCoroutine(Slide(weaponPanel, weaponPanel.anchoredPosition, Vector2.zero));
        }
        else
        {
            StartCoroutine(Slide(menuPanel, menuPanel.anchoredPosition, Vector2.zero));
            StartCoroutine(Slide(weaponPanel, weaponPanel.anchoredPosition, new Vector2(-slideDistance, 0)));
        }

        showingWeapons = !showingWeapons;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    IEnumerator Slide(RectTransform panel, Vector2 from, Vector2 to)
    {
        isSliding = true;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            float t = elapsed / slideDuration;
            t = EaseOutQuint(t); // Easing applied here

            panel.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        panel.anchoredPosition = to;
        isSliding = false;
    }

    float EaseOutQuint(float t)
    {
        return 1f - Mathf.Pow(1f - t, 5f);
    }
}
