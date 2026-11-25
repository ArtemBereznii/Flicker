using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shown a moment after the player dies: how far the run got, then Retry (a fresh run with the same
// loadout, by reloading the scene) or back to the main menu.
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private LevelRun run;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text summary;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private float delay = 1f;

    void Awake()
    {
        panel.SetActive(false);
        retryButton.onClick.AddListener(Retry);
        menuButton.onClick.AddListener(BackToMenu);
    }

    void OnEnable()
    {
        playerHealth.OnDied += HandleDied;
        playerHealth.OnRevived += Hide;
    }

    void OnDisable()
    {
        playerHealth.OnDied -= HandleDied;
        playerHealth.OnRevived -= Hide;
    }

    private void HandleDied() => StartCoroutine(ShowAfterDelay());

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        if (!playerHealth.IsDead) yield break; // revived in the meantime (debug restart)

        // No gameplay input under the screen: N would revive the player behind it.
        if (input != null) input.enabled = false;
        summary.text = run != null
            ? $"Reached level {run.Level}  ·  Fuel {run.Fuel}/{run.FuelNeeded}"
            : string.Empty;
        panel.SetActive(true);
    }

    private void Hide()
    {
        StopAllCoroutines();
        panel.SetActive(false);
    }

    private void Retry() => SceneManager.LoadScene(SceneNames.Block);

    private void BackToMenu() => SceneManager.LoadScene(SceneNames.MainMenu);
}
