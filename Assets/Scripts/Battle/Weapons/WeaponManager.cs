using System.Collections.Generic;
using UnityEngine;

// Spawns the selected weapon prefabs under the player and handles switching, shooting and reloading.
public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Sprite unarmedSprite;

    private readonly List<Weapon> equippedWeapons = new List<Weapon>();
    private SpriteRenderer playerSprite;
    private PlayerHealth health;
    private PlayerInputReader input;

    public IReadOnlyList<Weapon> EquippedWeapons => equippedWeapons;
    public Weapon CurrentWeapon { get; private set; }
    public int CurrentWeaponIndex { get; private set; }

    void Awake()
    {
        playerSprite = GetComponent<SpriteRenderer>();

        foreach (Weapon prefab in WeaponSelectionData.SelectedWeapons)
        {
            // worldPositionStays = false keeps the prefab's root position as the offset from the player.
            Weapon weapon = Instantiate(prefab, transform, false);
            weapon.name = prefab.name;
            weapon.gameObject.SetActive(false);
            equippedWeapons.Add(weapon);
        }

        if (equippedWeapons.Count == 0)
            Debug.LogWarning("No weapons found. Put weapon prefabs in Assets/Resources/Weapons.", this);

        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();
        if (health != null) health.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    // A dead player can't switch, shoot or reload.
    private void HandleDeath()
    {
        enabled = false;
    }

    void OnEnable()
    {
        if (input == null) return;
        input.SwitchWeapon += CycleWeapon;
        input.Slot1Pressed += EquipSlot1;
        input.Slot2Pressed += EquipSlot2;
        input.UnequipPressed += UnequipWeapon;
        input.ReloadPressed += Reload;
    }

    void OnDisable()
    {
        if (input == null) return;
        input.SwitchWeapon -= CycleWeapon;
        input.Slot1Pressed -= EquipSlot1;
        input.Slot2Pressed -= EquipSlot2;
        input.UnequipPressed -= UnequipWeapon;
        input.ReloadPressed -= Reload;
    }

    void Start()
    {
        EquipWeapon(0);
    }

    void Update()
    {
        if (CurrentWeapon != null && input != null && input.FireHeld)
            CurrentWeapon.Shoot();
    }

    private void CycleWeapon(int step)
    {
        if (equippedWeapons.Count == 0) return;
        int count = equippedWeapons.Count;
        EquipWeapon(((CurrentWeaponIndex + step) % count + count) % count);
    }

    private void EquipSlot1() => EquipWeapon(0);
    private void EquipSlot2() => EquipWeapon(1);

    private void Reload()
    {
        if (CurrentWeapon != null)
            CurrentWeapon.StartReload();
    }

    public void EquipWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedWeapons.Count) return;

        if (CurrentWeapon != null)
            CurrentWeapon.gameObject.SetActive(false);

        CurrentWeapon = equippedWeapons[slotIndex];
        CurrentWeaponIndex = slotIndex;
        CurrentWeapon.gameObject.SetActive(true);
        CurrentWeapon.ResetFireDelay();

        if (CurrentWeapon.playerSprite != null)
            playerSprite.sprite = CurrentWeapon.playerSprite;
    }

    public void UnequipWeapon()
    {
        if (CurrentWeapon != null)
            CurrentWeapon.gameObject.SetActive(false);

        CurrentWeapon = null;

        if (unarmedSprite != null)
            playerSprite.sprite = unarmedSprite;
    }
}
