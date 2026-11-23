using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Shows one ammo bar per equipped weapon, built from each weapon's own ammoBarPrefab.
public class AmmoUI : MonoBehaviour
{
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Transform weaponsUIParent; // Assign Canvas → Weapons
    [SerializeField] private float inactiveScale = 0.85f;
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private float transitionSpeed = 6f;

    private readonly Dictionary<Weapon, WeaponUI> weaponUIs = new Dictionary<Weapon, WeaponUI>();

    private class WeaponUI
    {
        public Slider ammoSlider;
        public Text ammoText;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;
    }

    void Start()
    {
        if (weaponManager == null)
            weaponManager = GameObject.FindWithTag("Player").GetComponent<WeaponManager>();

        foreach (Weapon weapon in weaponManager.EquippedWeapons)
            CreateWeaponUI(weapon);
    }

    void Update()
    {
        Weapon currentWeapon = weaponManager.CurrentWeapon;

        foreach (var entry in weaponUIs)
        {
            bool isActive = entry.Key == currentWeapon;
            if (isActive)
                RefreshWeaponUI(entry.Key, entry.Value);
            AnimateUIState(entry.Value, isActive);
        }
    }

    private void CreateWeaponUI(Weapon weapon)
    {
        if (weapon.ammoBarPrefab == null)
        {
            Debug.LogWarning($"{weapon.weaponName} has no ammo bar prefab.", weapon);
            return;
        }

        // Instantiate under layout group parent — keep prefab size
        GameObject uiObj = Instantiate(weapon.ammoBarPrefab, weaponsUIParent, false);
        uiObj.transform.localScale = Vector3.one;

        CanvasGroup cg = uiObj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = uiObj.AddComponent<CanvasGroup>();

        WeaponUI ui = new WeaponUI
        {
            ammoSlider = uiObj.GetComponentInChildren<Slider>(),
            ammoText = uiObj.GetComponentInChildren<Text>(),
            canvasGroup = cg,
            rectTransform = uiObj.GetComponent<RectTransform>()
        };

        ui.ammoSlider.maxValue = weapon.MaxAmmo;
        RefreshWeaponUI(weapon, ui);
        weaponUIs[weapon] = ui;
    }

    private void RefreshWeaponUI(Weapon weapon, WeaponUI ui)
    {
        ui.ammoSlider.value = weapon.IsReloading
            ? Mathf.Lerp(weapon.CurrentAmmo, weapon.MaxAmmo, weapon.CurrentReloadProgress())
            : weapon.CurrentAmmo;

        if (ui.ammoText != null)
            ui.ammoText.text = $"{weapon.CurrentAmmo}/{weapon.MaxAmmo}";
    }

    private void AnimateUIState(WeaponUI ui, bool isActive)
    {
        Vector3 targetScale = isActive ? Vector3.one : Vector3.one * inactiveScale;
        ui.rectTransform.localScale = Vector3.Lerp(ui.rectTransform.localScale, targetScale, Time.deltaTime * transitionSpeed);

        float targetAlpha = isActive ? 1f : inactiveAlpha;
        ui.canvasGroup.alpha = Mathf.Lerp(ui.canvasGroup.alpha, targetAlpha, Time.deltaTime * transitionSpeed);
    }
}
