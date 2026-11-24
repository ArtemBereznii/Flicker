using Flicker.Generation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Shows the current block's seed and lets you regenerate (N) or type a seed (a number or any word).
// Gameplay input is paused while the seed field has focus, so typing "1", "2" or "n" does not
// switch weapons or regenerate.
public class BlockDebugOverlay : MonoBehaviour
{
    [SerializeField] private BlockBuilder builder;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_InputField seedField;

    void OnEnable()
    {
        builder.Generated += ShowSeed;
        if (input != null) input.RegeneratePressed += builder.GenerateRandom;
        seedField.onSubmit.AddListener(SubmitSeed);
        seedField.onSelect.AddListener(PauseGameplayInput);
        seedField.onDeselect.AddListener(ResumeGameplayInput);
        seedField.onEndEdit.AddListener(EndEdit);
        if (builder.Current != null) ShowSeed(builder.Current);
    }

    void OnDisable()
    {
        builder.Generated -= ShowSeed;
        if (input != null) input.RegeneratePressed -= builder.GenerateRandom;
        seedField.onSubmit.RemoveListener(SubmitSeed);
        seedField.onSelect.RemoveListener(PauseGameplayInput);
        seedField.onDeselect.RemoveListener(ResumeGameplayInput);
        seedField.onEndEdit.RemoveListener(EndEdit);
    }

    private void ShowSeed(BlockLayout layout)
    {
        label.text = layout.Attempt > 0
            ? $"Block — Seed {layout.Seed}  (try {layout.Attempt + 1})"
            : $"Block — Seed {layout.Seed}";
    }

    private void SubmitSeed(string text)
    {
        if (SeedText.TryParse(text, out int seed)) builder.Generate(seed);
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
        if (input != null) input.enabled = true;
    }
}
