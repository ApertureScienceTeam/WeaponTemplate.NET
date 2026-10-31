using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Native;
using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// 原版 CreateMultiForward 注册的 WPN_* 事件,WPN_LoadFiles 除外
// (它需要在 precache 阶段触发,由 ConfigLoad.OnPrecache 手动注册执行)。
// Pawn 插件声明同名 public 即可接收;C# 代码经 NativeFunctionRegister.Execute_Xxx 触发。
public static class Forwards
{
    [AmxxForward("WPN_PrimaryPreAttack", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnPrimaryPreAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryPreAttack_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnPrimaryPreAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryPostAttack", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnPrimaryPostAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryPostAttack_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnPrimaryPostAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPreAttack", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnSecondaryPreAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPreAttack_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnSecondaryPreAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPostAttack", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnSecondaryPostAttack(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryPostAttack_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnSecondaryPostAttackPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryAttackEnd", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnPrimaryAttackEnd(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_PrimaryAttackEnd_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnPrimaryAttackEndPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryAttackEnd", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnSecondaryAttackEnd(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_SecondaryAttackEnd_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnSecondaryAttackEndPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponIdle", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnWeaponIdle(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponReload", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnWeaponReload(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponReload_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnWeaponReloadPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponDraw", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnWeaponDraw(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_BuyWeapon", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnBuyWeapon(int id, string command) { }

    [AmxxForward("WPN_BuyWeapon_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnBuyWeaponPost(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponMaxSpeed", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnWeaponMaxSpeed(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_WeaponKnockBack", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnWeaponKnockBack(int attacker, int victim, int inflictor, int weaponIndex, float damage) { }

    [AmxxForward("WPN_HookSpawnBlood", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnHookSpawnBlood(int color) { }

    [AmxxForward("WPN_HookGrenadeExplode", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnHookGrenadeExplode(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_HookGrenadeExplode_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnHookGrenadeExplodePost(int id, int entity, int victim, int weaponIndex) { }

    [AmxxForward("WPN_BotBuyWeapon", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnBotBuyWeapon(int id) { }

    [AmxxForward("WPN_BotBuyWeapon_Post", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnBotBuyWeaponPost(int id, int weaponIndex, int specialWeapon) { }

    [AmxxForward("WPN_WeaponPostFrame", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnWeaponPostFrame(int entity, int id, int weaponIndex) { }

    [AmxxForward("WPN_TakeDamage", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnTakeDamage(int victim, int inflictor, int attacker, float damage, int damageType) { }

    [AmxxForward("WPN_DeathMsg", ExecType = ForwardExecType.ET_CONTINUE)]
    public static void WpnDeathMsg(int attacker, int victim, int headshot, string weapon) { }
}
