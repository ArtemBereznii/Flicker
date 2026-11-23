using System.Collections.Generic;
using UnityEngine;

// Spawns the selected weapon prefabs under the player and handles switching, shooting and reloading.
public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Sprite unarmedSprite;

    private readonly List<Weapon> equippedWeapons = new List<Weapon>();
    private SpriteRenderer playerSprite;

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
    }

    void Start()
    {
        EquipWeapon(0);
    }

    void Update()
    {
        HandleWeaponSwitching();
        HandleShooting();
        HandleReloading();
    }

    private void HandleWeaponSwitching()
    {
        if (equippedWeapons.Count == 0) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) EquipWeapon((CurrentWeaponIndex + 1) % equippedWeapons.Count);
        if (scroll < 0) EquipWeapon((CurrentWeaponIndex - 1 + equippedWeapons.Count) % equippedWeapons.Count);

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeapon(1);
        if (Input.GetKeyDown(KeyCode.X)) UnequipWeapon();
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

    private void HandleShooting()
    {
        if (CurrentWeapon != null && Input.GetButton("Fire1"))
            CurrentWeapon.Shoot();
    }

    private void HandleReloading()
    {
        if (CurrentWeapon != null && Input.GetKeyDown(KeyCode.R))
            CurrentWeapon.StartReload();
    }
}
