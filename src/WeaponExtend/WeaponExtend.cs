using System.Runtime.InteropServices;
using System.Globalization;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// WeaponExtend.sma v2.2 的 C# 移植:抛射物 / 范围攻击扩展。
// 依赖 WeaponTemplate 核心模块提供的 WPN_* native 与 WPN_* forward(plugins.ini 中须先于本模块加载,
// 使本模块能在其 plugin_precache 阶段收到 WPN_LoadFiles)。
//
// 武器伤害值 / 攻击方式:
//  1.飞行道具: 抛射物模型、播放速率、缩放、亮度、重力、飞行速度、高度修正、伤害类型
//    (1碰撞爆炸/2延迟爆炸/3碰撞伤害/4范围碰撞伤害)、爆炸SPR/速率/缩放/高度、爆炸声音、爆炸特效、
//    伤害半径、存在时间、延迟时间、拖尾(颜色/透明度/间隔SPR/间隔时间/速率/缩放/高度)、
//    追踪模式(0关/1准星/2自动)、追踪检测距离、附带SPR(速率/缩放)、发光(半径/颜色RGB)
//  2.范围攻击: 伤害半径、伤害距离
//
// 抛射物实体 iuser1=被伤害过的玩家位图 iuser2=抛射物类型 iuser3=总帧数 iuser4=附带SPR实体;
// 附带SPR实体 iuser1=总帧数。
public static unsafe partial class WeaponExtend
{
    private const string PluginName = "Weapon Extend Template";


    private const int WpnTypeProjectile = 1;
    private const int WpnTypeAoeDamage = 2;

    private const int ProjectileModel = 1;
    private const int ProjectileSprite = 2;

    private const int DamageTypeTouchExp = 1;
    private const int DamageTypeDelayExp = 2;
    private const int DamageTypeTouch = 3;
    private const int DamageTypeAoeTouch = 4;

    private const int DmgSoundNone = 0;
    private const int DmgSoundCustom = 1;
    private const int DmgSoundDefault = 2;

    private const int TraceModeNone = 0;
    private const int TraceModeAim = 1;
    private const int TraceModeAuto = 2;

    private const string WpnProjectileName = "wpn_projectile";
    private const string WpnProjectileExtra = "wpn_projectile_extra";

    private const int MaxPlayers = 33;
    // 容量以核心模块为准(2026-10 决策):模板序号 1..MaxTemplateCount(512),下标 0 占位。
    // 原版 MAXWEAPON=128 与核心上限一致;核心放宽到 512 后这里必须跟上,否则第 129 个模板会让 OnLoadFiles 越界。
    private const int MaxWeapon = WpnConst.MaxTemplateCount + 1;

    // ---- 生命周期 ----

    [AmxxCallback("plugin_init")]
    public static void OnPluginInit()
    {
        InfoTarget = (int)Engine.AllocStr("info_target").data;
        EnvSprite = (int)Engine.AllocStr("env_sprite").data;
        Wpn.SetParamInt(0, 1, WpnConst.WpnSetBuy, WpnConst.BuyEverywhere);
        MF.RegisterEvent("HLTV", "wt_ExtendRoundStart", "a", "1=0", "2=0");
    }

    [AmxxHook("pfnThink", Post = true)]
    public static void ThinkPost(edict_t* pEntity)
    {
        HookResult.Ignore();
        ThinkLogic(pEntity);
    }

    [AmxxHook("pfnTouch", Post = true)]
    public static void TouchPost(edict_t* pEntity, edict_t* pOther)
    {
        HookResult.Ignore();
        TouchLogic(pEntity, pOther);
    }

    // ---- WPN forward 回调 ----

    [AmxxCallback("WPN_LoadFiles")]
    public static void OnLoadFiles(int slot, int weaponIndex, string key, string value)
    {
        SprBeamFollow = Engine.PrecacheModel("sprites/laserbeam.spr");

        // 核心模块读 INI 时已剥掉行内注释;这里做严格解析,单个坏键只丢弃该键并记 [error],不影响其余配置
        try { ApplyLoadFilesValue(weaponIndex, key, value); }
        catch (Exception e) { MF.Log($"[error] WPN_LoadFiles 配置值解析失败 key={key} value='{value}': {e.Message}"); }
    }

    private static void ApplyLoadFilesValue(int weaponIndex, string key, string value)
    {
        int i = weaponIndex;
        if (key == "攻击方式") for (int id = 1; id < MaxPlayers; id++) WeaponAttackMode[id, i] = int.Parse(value, CultureInfo.InvariantCulture);
        else if (key == "武器伤害值") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) WeaponDamage[id, i] = v; }
        else if (key == "抛射物模型")
        {
            for (int id = 1; id < MaxPlayers; id++) ProjectileModelPath[id, i] = value;
            Engine.PrecacheModel(value);
        }
        else if (key == "抛射物播放速率") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileFramerate[id, i] = v; }
        else if (key == "抛射物缩放") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileScale[id, i] = v; }
        else if (key == "抛射物亮度") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileBrightness[id, i] = v; }
        else if (key == "抛射物重力") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileGravity[id, i] = v; }
        else if (key == "飞行速度") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileFlySpeed[id, i] = v; }
        else if (key == "飞行高度修正") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileHeightOfs[id, i] = v; }
        else if (key == "伤害类型") for (int id = 1; id < MaxPlayers; id++) WeaponDamageType[id, i] = int.Parse(value, CultureInfo.InvariantCulture);
        else if (key == "爆炸SPR")
        {
            int sprite = Engine.PrecacheModel(value);
            for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeSprite[id, i] = sprite;
        }
        else if (key == "爆炸SPR播放速率") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileExpFramerate[id, i] = v; }
        else if (key == "爆炸SPR缩放") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileExpScale[id, i] = v; }
        else if (key == "爆炸SPR高度修正") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileExpHeightOfs[id, i] = v; }
        else if (key == "爆炸声音") for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeSound[id, i] = int.Parse(value, CultureInfo.InvariantCulture);
        else if (key == "爆炸声音路径")
        {
            for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeSoundPath[id, i] = value;
            Engine.PrecacheSound(value);
        }
        else if (key == "爆炸特效") for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeEffect[id, i] = int.Parse(value, CultureInfo.InvariantCulture);
        else if (key == "伤害半径") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileDamageRadius[id, i] = v; }
        else if (key == "存在时间") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileDuration[id, i] = v; }
        else if (key == "延迟时间") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileDelay[id, i] = v; }
        else if (key == "抛射物拖尾") for (int id = 1; id < MaxPlayers; id++) ProjectileHasTail[id, i] = int.Parse(value, CultureInfo.InvariantCulture);
        else if (key == "抛射物拖尾颜色R") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 0] = v; }
        else if (key == "抛射物拖尾颜色G") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 1] = v; }
        else if (key == "抛射物拖尾颜色B") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 2] = v; }
        else if (key == "抛射物拖尾透明度") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 3] = v; }
        else if (key == "拖尾间隔SPR")
        {
            int sprite = Engine.PrecacheModel(value);
            for (int id = 1; id < MaxPlayers; id++) ProjectileTailSprite[id, i] = sprite;
        }
        else if (key == "拖尾间隔时间") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailTime[id, i] = v; }
        else if (key == "拖尾间隔SPR播放速率") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailFramerate[id, i] = v; }
        else if (key == "拖尾间隔SPR缩放") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailScale[id, i] = v; }
        else if (key == "拖尾间隔SPR高度修正") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTailHeightOfs[id, i] = v; }
        else if (key == "追踪模式") for (int id = 1; id < MaxPlayers; id++) ProjectileTraceMode[id, i] = int.Parse(value, CultureInfo.InvariantCulture);
        else if (key == "追踪检测距离") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileTraceRadius[id, i] = v; }
        else if (key == "抛射物附带SPR")
        {
            for (int id = 1; id < MaxPlayers; id++) ProjectileExtraSprite[id, i] = value;
            Engine.PrecacheModel(value);
        }
        else if (key == "抛射物附带SPR播放速率") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ExtraSpriteFramerate[id, i] = v; }
        else if (key == "抛射物附带SPR缩放") { float v = float.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ExtraSpriteScale[id, i] = v; }
        else if (key == "抛射物发光") for (int id = 1; id < MaxPlayers; id++) ProjectileGlow[id, i] = int.Parse(value, CultureInfo.InvariantCulture);
        else if (key == "抛射物发光半径") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowRadius[id, i] = v; }
        else if (key == "抛射物发光颜色R") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowColor[id, i, 0] = v; }
        else if (key == "抛射物发光颜色G") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowColor[id, i, 1] = v; }
        else if (key == "抛射物发光颜色B") { int v = int.Parse(value, CultureInfo.InvariantCulture); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowColor[id, i, 2] = v; }
    }

    [AmxxCallback("WPN_PrimaryPreAttack")]
    public static int OnPrimaryPreAttack(int entityIndex, int id, int weaponIndex)
    {
        if (WeaponAttackMode[id, weaponIndex] == WpnTypeProjectile)
            return WpnConst.WPN_HANDLED;

        return WpnConst.WPN_IGNORED;
    }

    [AmxxCallback("WPN_PrimaryPostAttack_Post")]
    public static void OnPrimaryPostAttackPost(int entityIndex, int id, int weaponIndex)
    {
        if (WeaponAttackMode[id, weaponIndex] != WpnTypeProjectile)
            return;

        Wpn.GetParamString(weaponIndex, id, WpnConst.WpnSzKillSpr, out var weaponName, 63);

        Span<float> startOrigin = stackalloc float[3];
        Span<float> targetOrigin = stackalloc float[3];
        VectorMath.GetPosition(id, 40.0f, 0.0f, 0.0f, startOrigin);
        VectorMath.GetAimOrigin(id, targetOrigin);
        CreateProjectile(id, weaponIndex, weaponName, startOrigin, targetOrigin);
    }

    [AmxxCallback("WPN_LoadFiles_Post")]
    public static void OnLoadFilesPost(int slot, int weaponIndex, string key, string value)
    {
    }

    // ---- 回合开始清理 ----

    [AmxxCallback("wt_ExtendRoundStart")]
    public static void OnRoundStart(int id)
    {
        var entity = Engine.EntOfIndex(0);
        while (entity != null && (entity = Engine.FindEntityByString(entity, "globalname", WpnProjectileName)) != null)
            Engine.RemoveEntity(entity);
        entity = Engine.EntOfIndex(0);
        while (entity != null && (entity = Engine.FindEntityByString(entity, "globalname", WpnProjectileExtra)) != null)
            Engine.RemoveEntity(entity);
    }

}
