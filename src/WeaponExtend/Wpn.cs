using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// WeaponTemplate 核心模块提供的 WPN_* native 声明式导入。
// 要求 modules.ini 中已启用 WeaponTemplate 核心模块。
internal static partial class Wpn
{
    [AmxxImportNative("WPN_SetParamInt")]
    public static partial void SetParamInt(int weaponIndex, int id, int type, int value);

    [AmxxImportNative("WPN_GetParamString")]
    public static partial void GetParamString(int weaponIndex, int id, int type, out string value, int len);

    [AmxxImportNative("WPN_ExecuteAttack")]
    public static partial int ExecuteAttack(int attacker, int victim, int entity, float[] param, float[] origin, int damageType, int special);

    [AmxxImportNative("WPN_GiveAmmo")]
    public static partial void GiveAmmo(int id, int entity, int weaponIndex, int full);
}
