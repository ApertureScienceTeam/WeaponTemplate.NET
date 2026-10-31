using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// WeaponTemplate 核心模块的 WPN_* native 导入
internal static partial class Wpn
{
    [AmxxImportNative("WPN_SetParamInt")]
    public static partial void SetParamInt(int weaponIndex, int id, int type, int value);

    [AmxxImportNative("WPN_GetParamInt")]
    public static partial int GetParamInt(int weaponIndex, int id, int type);

    [AmxxImportNative("WPN_GetParamString")]
    public static partial void GetParamString(int weaponIndex, int id, int type, out string value, int len);

    [AmxxImportNative("WPN_SetParamString")]
    public static partial void SetParamString(int weaponIndex, int id, int type, string value);

    [AmxxImportNative("WPN_GiveWeapon")]
    public static partial int GiveWeapon(int id, int weaponIndex);

    [AmxxImportNative("WPN_GiveAmmo")]
    public static partial void GiveAmmo(int id, int entity, int weaponIndex, int full);
}

// Zombie Plague 4.3 Pawn 插件提供的 native 导入。
