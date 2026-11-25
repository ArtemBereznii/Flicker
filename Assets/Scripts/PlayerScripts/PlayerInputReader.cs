using System;
using UnityEngine;
using UnityEngine.InputSystem;

// The only place gameplay reads input. Other scripts take values and events from here,
// so swapping bindings or adding a gamepad scheme only touches FlickerControls.inputactions.
[DefaultExecutionOrder(-100)] // Read input before anything uses it this frame.
public class PlayerInputReader : MonoBehaviour
{
    private FlickerControls controls;

    public Vector2 Move { get; private set; }
    public Vector2 AimScreenPosition { get; private set; }
    public bool FireHeld { get; private set; }

    public event Action ReloadPressed;
    public event Action Slot1Pressed;
    public event Action Slot2Pressed;
    public event Action UnequipPressed;
    // Debug: generate a new block with a random seed.
    public event Action RegeneratePressed;
    // Use the thing in reach (the exit car).
    public event Action InteractPressed;
    /// <summary>+1 for next weapon, -1 for previous.</summary>
    public event Action<int> SwitchWeapon;

    void Awake()
    {
        controls = new FlickerControls();
        var gameplay = controls.Gameplay;
        gameplay.Reload.performed += _ => ReloadPressed?.Invoke();
        gameplay.Slot1.performed += _ => Slot1Pressed?.Invoke();
        gameplay.Slot2.performed += _ => Slot2Pressed?.Invoke();
        gameplay.Unequip.performed += _ => UnequipPressed?.Invoke();
        gameplay.Regenerate.performed += _ => RegeneratePressed?.Invoke();
        gameplay.Interact.performed += _ => InteractPressed?.Invoke();
        gameplay.SwitchWeapon.performed += OnSwitchWeapon;
    }

    void OnEnable() => controls.Gameplay.Enable();

    void OnDisable()
    {
        controls.Gameplay.Disable();
        Move = Vector2.zero;
        FireHeld = false;
    }

    void OnDestroy() => controls.Dispose();

    void Update()
    {
        var gameplay = controls.Gameplay;
        Move = Vector2.ClampMagnitude(gameplay.Move.ReadValue<Vector2>(), 1f);
        AimScreenPosition = gameplay.Aim.ReadValue<Vector2>();
        FireHeld = gameplay.Fire.IsPressed();
    }

    private void OnSwitchWeapon(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<float>();
        if (scroll > 0f) SwitchWeapon?.Invoke(1);
        else if (scroll < 0f) SwitchWeapon?.Invoke(-1);
    }
}
