using System;
using UnityEngine;
using UnityEngine.UI;

// One entry in the weapon selection menu. Spawned per weapon by WeaponSelectionManager.
[RequireComponent(typeof(Button))]
public class WeaponButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;

    private Button button;
    private Image background;

    public Weapon Weapon { get; private set; }

    public void Setup(Weapon weapon, Action<WeaponButton> onClick)
    {
        button = GetComponent<Button>();
        background = GetComponent<Image>();

        Weapon = weapon;
        name = weapon.weaponName;

        if (icon != null)
        {
            icon.sprite = weapon.menuIcon;
            icon.enabled = weapon.menuIcon != null;
        }

        button.onClick.AddListener(() => onClick(this));
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}
