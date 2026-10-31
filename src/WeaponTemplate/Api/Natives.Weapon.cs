using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class Natives
{
    // ---- 发放 / 攻击 / 特效 ----

    [AmxxNative("WPN_GiveWeapon")]
    public static int GiveWeapon(int id, int weaponIndex) => WeaponGive.GiveWeaponByTemplate(id, weaponIndex);

    [AmxxNative("WPN_ExecuteAttack")]
    public static int ExecuteAttack(int attacker, int victim, int entity,
        [AmxxBuffer(6)] Span<float> param, [AmxxBuffer(3)] Span<float> origin,
        int damageType, int special)
    {
        if (!HasLength(param.Length, 6) || !HasLength(origin.Length, 3)) return -1;
        // 原版 get_array_f 复制到局部数组;CheckAttack 添加视角高度时不能写回调用方。
        Span<float> attackOrigin = stackalloc float[3];
        origin[..3].CopyTo(attackOrigin);
        return Attack.CheckAttack(attacker, victim, Eng.EntOfIndex(entity), param[..6], attackOrigin, damageType, special);
    }

    [AmxxNative("WPN_SpawnBlood")]
    public static void SpawnBlood([AmxxBuffer(3)] Span<float> origin, int color, int amount)
    {
        if (HasLength(origin.Length, 3)) Effects.SpawnBlood(origin[..3], color, amount);
    }

    [AmxxNative("WPN_FakeSmoke")]
    public static void FakeSmoke([AmxxBuffer(3)] Span<float> origin)
    {
        if (HasLength(origin.Length, 3)) Effects.FakeSmoke(origin[..3]);
    }

    [AmxxNative("WPN_GiveAmmo")]
    public static void GiveAmmo(int id, int entity, int weaponIndex, int full)
    {
        if (WeaponStore.IsPlayerIndex(id) && (weaponIndex == 0 || WeaponStore.IsTemplate(weaponIndex)))
            WeaponAmmo.GiveAmmoForWeapon(id, entity, weaponIndex, full);
    }
}
