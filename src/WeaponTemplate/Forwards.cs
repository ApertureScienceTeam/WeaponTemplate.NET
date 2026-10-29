using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// 原版 CreateMultiForward 注册的 WPN_* 事件,WPN_LoadFiles 除外
// (它需要在 precache 阶段触发,由 ConfigLoad.OnPrecache 手动注册执行)。
// Pawn 插件声明同名 public 即可接收;C# 代码经 NativeFunctionRegister.Execute_Xxx 触发。
public static class Forwards
{
    [AmxxForward("WPN_PrimaryPreAttack")]
    public static void WpnPrimaryPreAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryPreAttack_Post")]
    public static void WpnPrimaryPreAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryPostAttack")]
    public static void WpnPrimaryPostAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryPostAttack_Post")]
    public static void WpnPrimaryPostAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPreAttack")]
    public static void WpnSecondaryPreAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPreAttack_Post")]
    public static void WpnSecondaryPreAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPostAttack")]
    public static void WpnSecondaryPostAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPostAttack_Post")]
    public static void WpnSecondaryPostAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryAttackEnd")]
    public static void WpnPrimaryAttackEnd(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryAttackEnd_Post")]
    public static void WpnPrimaryAttackEndPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryAttackEnd")]
    public static void WpnSecondaryAttackEnd(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryAttackEnd_Post")]
    public static void WpnSecondaryAttackEndPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponIdle")]
    public static void WpnWeaponIdle(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponReload")]
    public static void WpnWeaponReload(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponReload_Post")]
    public static void WpnWeaponReloadPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponDraw")]
    public static void WpnWeaponDraw(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_BuyWeapon")]
    public static void WpnBuyWeapon(int id, string command) { }

    [AmxxForward("WPN_BuyWeapon_Post")]
    public static void WpnBuyWeaponPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponMaxSpeed")]
    public static void WpnWeaponMaxSpeed(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponKnockBack")]
    public static void WpnWeaponKnockBack(int attacker, int victim, int inflictor, int weaponIndex, float damage) { }

    [AmxxForward("WPN_HookSpawnBlood")]
    public static void WpnHookSpawnBlood(int color) { }

    [AmxxForward("WPN_HookGrenadeExplode")]
    public static void WpnHookGrenadeExplode(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_HookGrenadeExplode_Post")]
    public static void WpnHookGrenadeExplodePost(int id, int entity, int victim, int weaponIndex) { }

    [AmxxForward("WPN_BotBuyWeapon")]
    public static void WpnBotBuyWeapon(int id) { }

    [AmxxForward("WPN_BotBuyWeapon_Post")]
    public static void WpnBotBuyWeaponPost(int id, int weaponIndex, int specialWeapon) { }

    [AmxxForward("WPN_WeaponPostFrame")]
    public static void WpnWeaponPostFrame(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_TakeDamage")]
    public static void WpnTakeDamage(int victim, int inflictor, int attacker, float damage, int damageType) { }

    [AmxxForward("WPN_DeathMsg")]
    public static void WpnDeathMsg(int attacker, int victim, int headshot, string weapon) { }
}
