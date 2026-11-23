using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Builds one button per weapon in WeaponCatalog and stores the player's choice.
public class WeaponSelectionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private WeaponButton buttonPrefab;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMPro.TMP_Text selectedWeaponsText;

    private readonly List<WeaponButton> buttons = new List<WeaponButton>();
    private readonly List<Weapon> selectedWeapons = new List<Weapon>();

    void Start()
    {
        foreach (Weapon weapon in WeaponCatalog.All)
        {
            WeaponButton button = Instantiate(buttonPrefab, buttonParent);
            button.Setup(weapon, ToggleWeapon);
            buttons.Add(button);
        }

        confirmButton.onClick.AddListener(ConfirmSelection);
        UpdateUI();
    }

    private void ToggleWeapon(WeaponButton button)
    {
        if (selectedWeapons.Contains(button.Weapon))
            selectedWeapons.Remove(button.Weapon);
        else if (selectedWeapons.Count < WeaponSelectionData.LoadoutSize)
            selectedWeapons.Add(button.Weapon);

        UpdateUI();
    }

    private void UpdateUI()
    {
        foreach (WeaponButton button in buttons)
            button.SetSelected(selectedWeapons.Contains(button.Weapon));

        selectedWeaponsText.text = "Selected: ";
        foreach (Weapon weapon in selectedWeapons)
            selectedWeaponsText.text += weapon.weaponName + " ";

        confirmButton.interactable = selectedWeapons.Count == WeaponSelectionData.LoadoutSize;
    }

    private void ConfirmSelection()
    {
        WeaponSelectionData.SetSelection(selectedWeapons);
        SceneManager.LoadScene(SceneNames.Arena1);
    }
}
