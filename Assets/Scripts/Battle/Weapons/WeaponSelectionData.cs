using System.Collections.Generic;
using System.Linq;

// Carries the menu's weapon choice (prefab references) into the arena scene.
public static class WeaponSelectionData
{
    public const int LoadoutSize = 2;

    private static List<Weapon> selectedWeapons = new List<Weapon>();

    public static IReadOnlyList<Weapon> SelectedWeapons =>
        selectedWeapons.Count > 0 ? selectedWeapons : WeaponCatalog.All.Take(LoadoutSize).ToList();

    public static void SetSelection(IEnumerable<Weapon> weapons)
    {
        selectedWeapons = weapons.Where(w => w != null).ToList();
    }
}
