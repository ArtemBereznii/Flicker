using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Every weapon prefab in Assets/Resources/Weapons, ordered by Weapon.sortOrder.
public static class WeaponCatalog
{
    private const string ResourcesPath = "Weapons";
    private static List<Weapon> weapons;

    public static IReadOnlyList<Weapon> All
    {
        get
        {
            if (weapons == null)
            {
                weapons = Resources.LoadAll<Weapon>(ResourcesPath)
                    .OrderBy(w => w.sortOrder)
                    .ThenBy(w => w.weaponName)
                    .ToList();
            }
            return weapons;
        }
    }
}
