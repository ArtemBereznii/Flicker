using Flicker.Generation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Shows the current level and block seed and lets you restart the run (N: random seed) or type a seed
// (a number or any word). Restarts are ignored while the level cutscene plays.
// Gameplay input is paused while the seed field has focus, so typing "1", "2" or "n" does not
// switch weapons or regenerate.
public class BlockDebugOverlay : MonoBehaviour
{
    [SerializeField] private BlockBuilder builder;
    [SerializeField] private LevelRun run;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField seedField;

    void OnEnable()
    {
        builder.Generated += ShowSeed;
        if (input != null) input.RegeneratePressed += RestartRandom;
        seedField.onSubmit.AddListener(SubmitSeed);
        seedField.onSelect.AddListener(PauseGameplayInput);
        seedField.onDeselect.AddListener(ResumeGameplayInput);
        seedField.onEndEdit.AddListener(EndEdit);
        if (builder.Current != null) ShowSeed(builder.Current);
    }

    void OnDisable()
    {
        builder.Generated -= ShowSeed;
        if (input != null) input.RegeneratePressed -= RestartRandom;
        seedField.onSubmit.RemoveListener(SubmitSeed);
        seedField.onSelect.RemoveListener(PauseGameplayInput);
        seedField.onDeselect.RemoveListener(ResumeGameplayInput);
        seedField.onEndEdit.RemoveListener(EndEdit);
    }

    private void ShowSeed(BlockLayout layout)
    {
        string level = run != null ? $"Level {run.Level}" : "Block";
        label.text = layout.Attempt > 0
            ? $"{level} — Seed {layout.Seed}  (try {layout.Attempt + 1})"
            : $"{level} — Seed {layout.Seed}";
    }

    private void RestartRandom()
    {
        if (run == null) builder.GenerateRandom();
        else if (!run.Busy) run.StartRandomRun();
    }

    private void Restart(int seed)
    {
        if (run == null) builder.Generate(seed);
        else if (!run.Busy) run.StartRun(seed);
    }

    private void SubmitSeed(string text)
    {
        if (SeedText.TryParse(text, out int seed)) Restart(seed);
        seedField.text = string.Empty;
        seedField.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        ResumeGameplayInput(string.Empty);
    }

    // Escape cancels editing without changing the EventSystem selection, so onDeselect never fires.
    // Any end of editing (Enter, Escape, clicking away) drops focus and gives control back.
    private void EndEdit(string _)
    {
        var events = EventSystem.current;
        // During a normal deselect the EventSystem is already mid-selection; changing it again would error.
        if (events != null && !events.alreadySelecting && events.currentSelectedGameObject == seedField.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
        ResumeGameplayInput(string.Empty);
    }

    private void PauseGameplayInput(string _)
    {
        if (input != null) input.enabled = false;
    }

    private void ResumeGameplayInput(string _)
    {
        // The cutscene owns input while it plays; don't hand control back mid-drive.
        if (input != null && (run == null || !run.Busy)) input.enabled = true;
    }
}
