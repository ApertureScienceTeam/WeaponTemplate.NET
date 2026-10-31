using System.Runtime.InteropServices;
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

    private const int WeaponIdPData = 11; // 原版 WEAPON_ID = ammo_buckshot = 11

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
    private const int MaxWeapon = 128; // 与 wpn_const.inc 的 MAXWEAPON 一致

    // ---- 引擎封装(本模块局部使用) ----

    private static enginefuncs_t EF => ModuleRuntime.g_engfuncs;

    private static float GameTime => ModuleRuntime.gpGlobals->time;

    private static edict_t* EntOfIndex(int index)
    {
        if (index < 0) return null;
        var p = EF.pfnPEntityOfEntIndex(index);
        return p == null || p->free != 0 ? null : p;
    }

    private static int IndexOf(edict_t* ent) => ent == null ? 0 : EF.pfnIndexOfEdict(ent);

    private static bool IsValid(edict_t* ent) => ent != null && ent->free == 0 && ent->pvPrivateData != null;

    private static string Str(string_t s)
    {
        if (s.data == 0 || ModuleRuntime.gpGlobals->pStringBase == null) return "";
        return Marshal.PtrToStringAnsi((nint)(ModuleRuntime.gpGlobals->pStringBase + s.data)) ?? "";
    }

    private static string GlobalName(edict_t* ent) => Str(ent->v.globalname);

    private static string_t AllocStr(string value)
    {
        using var native = value.GetNativeString();
        return new string_t { data = unchecked((uint)EF.pfnAllocString(native)) };
    }

    private static int PrecacheModel(string path)
    {
        return EngineResources.PrecacheModel(path);
    }

    private static int PrecacheSound(string path)
    {
        return EngineResources.PrecacheSound(path);
    }

    private static void SetModel(edict_t* ent, string model)
    {
        EF.pfnSetModel(ent, EngineStrings.Get(model));
    }

    private static void EmitSound(edict_t* ent, int channel, string sample, float volume, float attn, int flags, int pitch)
    {
        using var s = sample.GetNativeString();
        EF.pfnEmitSound(ent, channel, s, volume, attn, flags, pitch);
    }

    private static void RemoveEntity(edict_t* ent) => EF.pfnRemoveEntity(ent);

    private static edict_t* FindEntityByString(edict_t* start, string field, string value)
    {
        using var f = field.GetNativeString();
        using var v = value.GetNativeString();
        var found = EF.pfnFindEntityByString(start, f, v);
        return found == null || IndexOf(found) == 0 ? null : found;
    }

    private static edict_t* CreateNamedEntity(string className)
    {
        var alloc = AllocStr(className);
        return EF.pfnCreateNamedEntity((int)alloc.data);
    }

    private static bool IsConnected(int id)
    {
        if (id < 1 || id > 32) return false;
        var p = EF.pfnPEntityOfEntIndex(id);
        return p != null && p->free == 0 && p->v.netname.data != 0;
    }

    private static bool IsAlive(int id)
    {
        if (id < 1 || id > 32) return false;
        var p = EF.pfnPEntityOfEntIndex(id);
        return p != null && p->free == 0 && p->v.deadflag == 0 && p->v.health > 0.0f;
    }

    private static int GetPDataInt(edict_t* ent, int offset) => ((int*)ent->pvPrivateData)[offset];
    private static void SetPDataInt(edict_t* ent, int offset, int value) => ((int*)ent->pvPrivateData)[offset] = value;

    private static int GetPlayerTeam(int id)
    {
        var p = EntOfIndex(id);
        return p == null ? 0 : GetPDataInt(p, 114); // m_iTeam
    }

    // ---- 模板数据(原版 [33][MAXWEAPON] 数组) ----

    private static readonly int[,] WeaponAttackMode = new int[MaxPlayers, MaxWeapon];      // 攻击方式
    private static readonly int[,] WeaponDamageType = new int[MaxPlayers, MaxWeapon];      // 伤害类型
    private static readonly int[,] ProjectileExplodeSprite = new int[MaxPlayers, MaxWeapon];   // 爆炸SPR
    private static readonly int[,] ProjectileExplodeSound = new int[MaxPlayers, MaxWeapon];    // 爆炸声音
    private static readonly int[,] ProjectileExplodeEffect = new int[MaxPlayers, MaxWeapon];   // 爆炸特效
    private static readonly int[,] ProjectileHasTail = new int[MaxPlayers, MaxWeapon];         // 抛射物拖尾
    private static readonly int[,,] ProjectileTailColor = new int[MaxPlayers, MaxWeapon, 4];    // 抛射物拖尾颜色
    private static readonly int[,] ProjectileTailSprite = new int[MaxPlayers, MaxWeapon];      // 拖尾间隔SPR
    private static readonly int[,] ProjectileTraceMode = new int[MaxPlayers, MaxWeapon];       // 追踪模式
    private static readonly int[,] ProjectileGlow = new int[MaxPlayers, MaxWeapon];            // 抛射物发光
    private static readonly int[,,] ProjectileGlowColor = new int[MaxPlayers, MaxWeapon, 3];    // 抛射物发光颜色
    private static readonly int[,] ProjectileGlowRadius = new int[MaxPlayers, MaxWeapon];      // 抛射物发光半径
    private static readonly float[,] WeaponDamage = new float[MaxPlayers, MaxWeapon];          // 武器伤害值
    private static readonly float[,] ProjectileFramerate = new float[MaxPlayers, MaxWeapon];   // 抛射物播放速率
    private static readonly float[,] ProjectileScale = new float[MaxPlayers, MaxWeapon];       // 抛射物缩放
    private static readonly float[,] ProjectileBrightness = new float[MaxPlayers, MaxWeapon];  // 抛射物亮度
    private static readonly float[,] ProjectileGravity = new float[MaxPlayers, MaxWeapon];     // 抛射物重力
    private static readonly float[,] ProjectileFlySpeed = new float[MaxPlayers, MaxWeapon];    // 飞行速度
    private static readonly float[,] ProjectileHeightOfs = new float[MaxPlayers, MaxWeapon];   // 飞行高度修正
    private static readonly float[,] ProjectileExpFramerate = new float[MaxPlayers, MaxWeapon];// 爆炸SPR播放速率
    private static readonly float[,] ProjectileExpScale = new float[MaxPlayers, MaxWeapon];    // 爆炸SPR缩放
    private static readonly float[,] ProjectileExpHeightOfs = new float[MaxPlayers, MaxWeapon];// 爆炸SPR高度修正
    private static readonly float[,] ProjectileDamageRadius = new float[MaxPlayers, MaxWeapon];// 伤害半径
    private static readonly float[,] ProjectileDuration = new float[MaxPlayers, MaxWeapon];    // 存在时间
    private static readonly float[,] ProjectileDelay = new float[MaxPlayers, MaxWeapon];       // 延迟时间
    private static readonly float[,] ProjectileTailTime = new float[MaxPlayers, MaxWeapon];    // 拖尾间隔时间
    private static readonly float[,] ProjectileTailFramerate = new float[MaxPlayers, MaxWeapon];
    private static readonly float[,] ProjectileTailScale = new float[MaxPlayers, MaxWeapon];
    private static readonly float[,] ProjectileTailHeightOfs = new float[MaxPlayers, MaxWeapon];
    private static readonly float[,] ProjectileTraceRadius = new float[MaxPlayers, MaxWeapon]; // 追踪检测距离
    private static readonly float[,] ExtraSpriteFramerate = new float[MaxPlayers, MaxWeapon];  // 抛射物附带SPR播放速率
    private static readonly float[,] ExtraSpriteScale = new float[MaxPlayers, MaxWeapon];      // 抛射物附带SPR缩放
    private static readonly string[,] ProjectileModelPath = new string[MaxPlayers, MaxWeapon]; // 抛射物模型
    private static readonly string[,] ProjectileExplodeSoundPath = new string[MaxPlayers, MaxWeapon];
    private static readonly string[,] ProjectileExtraSprite = new string[MaxPlayers, MaxWeapon];// 抛射物附带SPR

    private static int InfoTarget;     // g_iInfoTarget(AllocString 句柄)
    private static int EnvSprite;      // g_iEnvSprite
    private static int SprBeamFollow;  // g_sprBeamFollow

    static WeaponExtend() => ResetAll();

    public static void ResetAll()
    {
        EngineResources.Reset();
        foreach (var field in typeof(WeaponExtend).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
        {
            if (field.GetValue(null) is string[,] strings)
            {
                for (int row = 0; row < strings.GetLength(0); row++)
                    for (int col = 0; col < strings.GetLength(1); col++) strings[row, col] = "";
            }
            else if (field.GetValue(null) is Array array) Array.Clear(array);
        }
        InfoTarget = EnvSprite = SprBeamFollow = 0;
    }

    // ---- 生命周期 ----

    [AmxxCallback("plugin_init")]
    public static void OnPluginInit()
    {
        InfoTarget = (int)AllocStr("info_target").data;
        EnvSprite = (int)AllocStr("env_sprite").data;
        Wpn.SetParamInt(0, 1, WpnConst.SetBuy, WpnConst.BuyEverywhere);
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
        SprBeamFollow = PrecacheModel("sprites/laserbeam.spr");

        int i = weaponIndex;
        if (key == "攻击方式") for (int id = 1; id < MaxPlayers; id++) WeaponAttackMode[id, i] = ParseInt(value);
        else if (key == "武器伤害值") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) WeaponDamage[id, i] = v; }
        else if (key == "抛射物模型")
        {
            for (int id = 1; id < MaxPlayers; id++) ProjectileModelPath[id, i] = value;
            PrecacheModel(value);
        }
        else if (key == "抛射物播放速率") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileFramerate[id, i] = v; }
        else if (key == "抛射物缩放") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileScale[id, i] = v; }
        else if (key == "抛射物亮度") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileBrightness[id, i] = v; }
        else if (key == "抛射物重力") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileGravity[id, i] = v; }
        else if (key == "飞行速度") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileFlySpeed[id, i] = v; }
        else if (key == "飞行高度修正") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileHeightOfs[id, i] = v; }
        else if (key == "伤害类型") for (int id = 1; id < MaxPlayers; id++) WeaponDamageType[id, i] = ParseInt(value);
        else if (key == "爆炸SPR")
        {
            int sprite = PrecacheModel(value);
            for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeSprite[id, i] = sprite;
        }
        else if (key == "爆炸SPR播放速率") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileExpFramerate[id, i] = v; }
        else if (key == "爆炸SPR缩放") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileExpScale[id, i] = v; }
        else if (key == "爆炸SPR高度修正") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileExpHeightOfs[id, i] = v; }
        else if (key == "爆炸声音") for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeSound[id, i] = ParseInt(value);
        else if (key == "爆炸声音路径")
        {
            for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeSoundPath[id, i] = value;
            PrecacheSound(value);
        }
        else if (key == "爆炸特效") for (int id = 1; id < MaxPlayers; id++) ProjectileExplodeEffect[id, i] = ParseInt(value);
        else if (key == "伤害半径") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileDamageRadius[id, i] = v; }
        else if (key == "存在时间") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileDuration[id, i] = v; }
        else if (key == "延迟时间") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileDelay[id, i] = v; }
        else if (key == "抛射物拖尾") for (int id = 1; id < MaxPlayers; id++) ProjectileHasTail[id, i] = ParseInt(value);
        else if (key == "抛射物拖尾颜色R") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 0] = v; }
        else if (key == "抛射物拖尾颜色G") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 1] = v; }
        else if (key == "抛射物拖尾颜色B") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 2] = v; }
        else if (key == "抛射物拖尾透明度") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailColor[id, i, 3] = v; }
        else if (key == "拖尾间隔SPR")
        {
            int sprite = PrecacheModel(value);
            for (int id = 1; id < MaxPlayers; id++) ProjectileTailSprite[id, i] = sprite;
        }
        else if (key == "拖尾间隔时间") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailTime[id, i] = v; }
        else if (key == "拖尾间隔SPR播放速率") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailFramerate[id, i] = v; }
        else if (key == "拖尾间隔SPR缩放") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailScale[id, i] = v; }
        else if (key == "拖尾间隔SPR高度修正") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTailHeightOfs[id, i] = v; }
        else if (key == "追踪模式") for (int id = 1; id < MaxPlayers; id++) ProjectileTraceMode[id, i] = ParseInt(value);
        else if (key == "追踪检测距离") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ProjectileTraceRadius[id, i] = v; }
        else if (key == "抛射物附带SPR")
        {
            for (int id = 1; id < MaxPlayers; id++) ProjectileExtraSprite[id, i] = value;
            PrecacheModel(value);
        }
        else if (key == "抛射物附带SPR播放速率") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ExtraSpriteFramerate[id, i] = v; }
        else if (key == "抛射物附带SPR缩放") { float v = ParseFloat(value); for (int id = 1; id < MaxPlayers; id++) ExtraSpriteScale[id, i] = v; }
        else if (key == "抛射物发光") for (int id = 1; id < MaxPlayers; id++) ProjectileGlow[id, i] = ParseInt(value);
        else if (key == "抛射物发光半径") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowRadius[id, i] = v; }
        else if (key == "抛射物发光颜色R") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowColor[id, i, 0] = v; }
        else if (key == "抛射物发光颜色G") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowColor[id, i, 1] = v; }
        else if (key == "抛射物发光颜色B") { int v = ParseInt(value); for (int id = 1; id < MaxPlayers; id++) ProjectileGlowColor[id, i, 2] = v; }
    }

    [AmxxCallback("WPN_PrimaryPreAttack")]
    public static int OnPrimaryPreAttack(int entityIndex, int id, int weaponIndex)
    {
        if (WeaponAttackMode[id, weaponIndex] == WpnTypeProjectile)
            return WpnConst.WpnHandled;

        return WpnConst.WpnIgnored;
    }

    [AmxxCallback("WPN_PrimaryPostAttack_Post")]
    public static void OnPrimaryPostAttackPost(int entityIndex, int id, int weaponIndex)
    {
        if (WeaponAttackMode[id, weaponIndex] != WpnTypeProjectile)
            return;

        Wpn.GetParamString(weaponIndex, id, WpnConst.KillSpr, out var weaponName, 63);

        Span<float> startOrigin = stackalloc float[3];
        Span<float> targetOrigin = stackalloc float[3];
        GetPosition(id, 40.0f, 0.0f, 0.0f, startOrigin);
        GetAimOrigin(id, targetOrigin);
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
        var entity = EntOfIndex(0);
        while (entity != null && (entity = FindEntityByString(entity, "globalname", WpnProjectileName)) != null)
            RemoveEntity(entity);
        entity = EntOfIndex(0);
        while (entity != null && (entity = FindEntityByString(entity, "globalname", WpnProjectileExtra)) != null)
            RemoveEntity(entity);
    }

    // ---- 抛射物思考 ----

    private static void ThinkLogic(edict_t* entity)
    {
        if (!IsValid(entity)) return;

        string globalName = GlobalName(entity);
        if (globalName != WpnProjectileName) return;

        var pev = &entity->v;
        Span<float> origin = stackalloc float[3] { pev->origin.x, pev->origin.y, pev->origin.z };
        Span<float> targetOrigin = stackalloc float[3];
        Span<float> velocity = stackalloc float[3];
        int owner = IndexOf(pev->owner);
        int i = GetPDataInt(entity, WeaponIdPData);
        pev->nextthink = GameTime + 0.01f;

        int sprEnt = pev->iuser4;
        if (IsValid(EntOfIndex(sprEnt)))
        {
            var sprPev = &EntOfIndex(sprEnt)->v;
            sprPev->frame += 0.2f * sprPev->framerate;
            if ((int)sprPev->frame >= sprPev->iuser1) sprPev->frame = 0.0f;
        }

        // SPR播放
        if (pev->iuser2 == ProjectileSprite)
        {
            pev->frame += 0.2f * pev->framerate;
            if ((int)pev->frame >= pev->iuser3) pev->frame = 0.0f;
        }

        // 延迟时间
        float dmgTime = pev->dmgtime;
        if (WeaponDamageType[owner, i] == DamageTypeDelayExp && GameTime > dmgTime)
        {
            int explodeFlag = 0;
            if (ProjectileExplodeEffect[owner, i] == 0)
                explodeFlag = WpnConst.TeExplFlagNoDlights | WpnConst.TeExplFlagNoParticles;
            else
            {
                MessageBeginGlobal(0, 23);
                WriteByte(WpnConst.TeWorldDecal);
                WriteCoord(origin[0]);
                WriteCoord(origin[1]);
                WriteCoord(origin[2]);
                WriteByte(RandomNum(46, 48));
                ModuleRuntime.g_engfuncs.pfnMessageEnd();
            }
            if (ProjectileExplodeSound[owner, i] != DmgSoundDefault)
            {
                explodeFlag |= WpnConst.TeExplFlagNoSound;
                if (ProjectileExplodeSound[owner, i] == DmgSoundCustom)
                    EmitSound(entity, 1, ProjectileExplodeSoundPath[owner, i], 1.0f, 0.8f, 0, 100);
            }
            MessageBeginGlobal(0, 23);
            WriteByte(WpnConst.TeExplosion);
            WriteCoord(origin[0]);
            WriteCoord(origin[1]);
            WriteCoord(origin[2] + ProjectileExpHeightOfs[owner, i]);
            WriteShort(ProjectileExplodeSprite[owner, i]);
            WriteByte(PawnMath.Round(ProjectileExpScale[owner, i] * 10.0f));
            WriteByte(PawnMath.Round(ProjectileExpFramerate[owner, i] * 10.0f));
            WriteByte(explodeFlag);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();

            Span<float> param = stackalloc float[6];
            param[0] = ProjectileDamageRadius[owner, i];
            param[5] = WeaponDamage[owner, i];
            Wpn.ExecuteAttack(owner, 0, IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeExplosion, WpnConst.SpdmgCantHeadshot);
            if (IsValid(EntOfIndex(sprEnt))) RemoveEntity(EntOfIndex(sprEnt));
            RemoveEntity(entity);
            return;
        }

        // 范围碰撞伤害
        if (WeaponDamageType[owner, i] == DamageTypeAoeTouch)
        {
            Span<float> param = stackalloc float[6];
            for (int id = 1; id < MaxPlayers; id++)
            {
                if (!IsAlive(id)) continue;

                var pTarget = EntOfIndex(id);
                targetOrigin[0] = pTarget->v.origin.x;
                targetOrigin[1] = pTarget->v.origin.y;
                targetOrigin[2] = pTarget->v.origin.z;
                if (Distance(origin, targetOrigin) > ProjectileDamageRadius[owner, i]) continue;

                if ((pev->iuser1 & (1 << id)) != 0) continue;

                if (GetPlayerTeam(id) == GetPlayerTeam(owner)) continue;

                pev->iuser1 |= 1 << id;
                param.Clear();
                param[5] = WeaponDamage[owner, i];
                Wpn.ExecuteAttack(owner, id, IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeTouch, WpnConst.SpdmgCantHeadshot);
            }
        }

        // 存在时间
        float user4Time = pev->fuser4;
        if (GameTime > user4Time && user4Time > 0.0f)
        {
            if (IsValid(EntOfIndex(sprEnt))) RemoveEntity(EntOfIndex(sprEnt));
            RemoveEntity(entity);
            return;
        }

        // 拖尾间隔SPR
        float user1Time = pev->fuser1;
        if (GameTime > user1Time && user1Time != 0.0f)
        {
            MessageBeginGlobal(0, 23);
            WriteByte(WpnConst.TeExplosion);
            WriteCoord(origin[0]);
            WriteCoord(origin[1]);
            WriteCoord(origin[2] + ProjectileTailHeightOfs[owner, i]);
            WriteShort(ProjectileTailSprite[owner, i]);
            WriteByte(PawnMath.Round(ProjectileTailScale[owner, i] * 10.0f));
            WriteByte(PawnMath.Round(ProjectileTailFramerate[owner, i] * 10.0f));
            WriteByte(WpnConst.TeExplFlagNoDlights | WpnConst.TeExplFlagNoSound | WpnConst.TeExplFlagNoParticles);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();
            pev->fuser1 = GameTime + ProjectileTailTime[owner, i];
        }

        // 抛射物发光
        float user2Time = pev->fuser2;
        if (GameTime > user2Time && user2Time != 0.0f)
        {
            MessageBeginPvs(3, 23, origin);
            WriteByte(WpnConst.TeDlight);
            WriteCoord(origin[0]);
            WriteCoord(origin[1]);
            WriteCoord(origin[2]);
            WriteByte(ProjectileGlowRadius[owner, i] / 10);
            WriteByte(ProjectileGlowColor[owner, i, 0]);
            WriteByte(ProjectileGlowColor[owner, i, 1]);
            WriteByte(ProjectileGlowColor[owner, i, 2]);
            WriteByte(1);
            WriteByte(0);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();

            MessageBeginPvs(3, 23, origin);
            WriteByte(WpnConst.TeElight);
            WriteShort(IndexOf(entity));
            WriteCoord(origin[0]);
            WriteCoord(origin[1]);
            WriteCoord(origin[2]);
            WriteCoord(10.0f);
            WriteByte(ProjectileGlowColor[owner, i, 0]);
            WriteByte(ProjectileGlowColor[owner, i, 1]);
            WriteByte(ProjectileGlowColor[owner, i, 2]);
            WriteByte(2);
            WriteCoord(0.0f);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();

            pev->fuser2 = GameTime + 0.01f;
        }

        int target = pev->euser1 != null ? IndexOf(pev->euser1) : 0;
        if (ProjectileTraceMode[owner, i] == TraceModeAuto)
        {
            float maxDis = 99999.0f;
            int tempTarget = 0;
            if (!IsAlive(target))
            {
                for (int player = 1; player < MaxPlayers; player++)
                {
                    if (!IsAlive(player)) continue;

                    if (GetPlayerTeam(owner) == GetPlayerTeam(player)) continue;

                    var pPlayer = EntOfIndex(player);
                    targetOrigin[0] = pPlayer->v.origin.x;
                    targetOrigin[1] = pPlayer->v.origin.y;
                    targetOrigin[2] = pPlayer->v.origin.z;
                    if (IsWallBetweenPoints(origin, targetOrigin, entity)) continue;

                    float distance = Distance(origin, targetOrigin);
                    if (distance < maxDis)
                    {
                        maxDis = distance;
                        tempTarget = player;
                    }
                }
                if (maxDis <= ProjectileTraceRadius[owner, i])
                {
                    var pTemp = EntOfIndex(tempTarget);
                    pev->euser1 = pTemp;
                }
            }
            else
            {
                var pTarget = EntOfIndex(target);
                targetOrigin[0] = pTarget->v.origin.x;
                targetOrigin[1] = pTarget->v.origin.y;
                targetOrigin[2] = pTarget->v.origin.z;
                GetSpeedVector(origin, targetOrigin, ProjectileFlySpeed[owner, i], velocity);
                EntityTurn(entity, targetOrigin);
                pev->velocity.x = velocity[0];
                pev->velocity.y = velocity[1];
                pev->velocity.z = velocity[2];
            }
        }
        else if (ProjectileTraceMode[owner, i] == TraceModeAim)
        {
            GetAimOrigin(owner, targetOrigin);
            GetSpeedVector(origin, targetOrigin, ProjectileFlySpeed[owner, i], velocity);
            EntityTurn(entity, targetOrigin);
            pev->velocity.x = velocity[0];
            pev->velocity.y = velocity[1];
            pev->velocity.z = velocity[2];
        }
    }

    // ---- 碰撞 ----

    private static void TouchLogic(edict_t* entity, edict_t* other)
    {
        if (!IsValid(entity)) return;

        string globalName = GlobalName(entity);
        if (globalName != WpnProjectileName) return;

        if (IsValid(other))
        {
            string otherName = GlobalName(other);
            if (otherName == globalName) return;
        }

        var pev = &entity->v;
        Span<float> origin = stackalloc float[3] { pev->origin.x, pev->origin.y, pev->origin.z };
        int owner = IndexOf(pev->owner);
        int i = GetPDataInt(entity, WeaponIdPData);
        int sprEnt = pev->iuser4;

        // 碰撞爆炸
        if (WeaponDamageType[owner, i] == DamageTypeTouchExp)
        {
            int explodeFlag = 0;
            if (ProjectileExplodeEffect[owner, i] == 0)
                explodeFlag = WpnConst.TeExplFlagNoDlights | WpnConst.TeExplFlagNoParticles;
            else
            {
                MessageBeginGlobal(0, 23);
                WriteByte(WpnConst.TeWorldDecal);
                WriteCoord(origin[0]);
                WriteCoord(origin[1]);
                WriteCoord(origin[2]);
                WriteByte(RandomNum(46, 48));
                ModuleRuntime.g_engfuncs.pfnMessageEnd();
            }
            if (ProjectileExplodeSound[owner, i] != DmgSoundDefault)
            {
                explodeFlag |= WpnConst.TeExplFlagNoSound;
                if (ProjectileExplodeSound[owner, i] == DmgSoundCustom)
                    EmitSound(entity, 1, ProjectileExplodeSoundPath[owner, i], 1.0f, 0.8f, 0, 100);
            }
            MessageBeginGlobal(0, 23);
            WriteByte(WpnConst.TeExplosion);
            WriteCoord(origin[0]);
            WriteCoord(origin[1]);
            WriteCoord(origin[2] + ProjectileExpHeightOfs[owner, i]);
            WriteShort(ProjectileExplodeSprite[owner, i]);
            WriteByte(PawnMath.Round(ProjectileExpScale[owner, i] * 10.0f));
            WriteByte(PawnMath.Round(ProjectileExpFramerate[owner, i] * 10.0f));
            WriteByte(explodeFlag);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();

            Span<float> param = stackalloc float[6];
            param[0] = ProjectileDamageRadius[owner, i];
            param[5] = WeaponDamage[owner, i];
            Wpn.ExecuteAttack(owner, 0, IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeExplosion, WpnConst.SpdmgCantHeadshot);
            if (IsValid(EntOfIndex(sprEnt))) RemoveEntity(EntOfIndex(sprEnt));
        }
        else if (WeaponDamageType[owner, i] == DamageTypeTouch)
        {
            Span<float> param = stackalloc float[6];
            param[5] = WeaponDamage[owner, i];
            Wpn.ExecuteAttack(owner, IndexOf(other), IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeTouch, WpnConst.SpdmgCantHeadshot);
        }
        if (IsValid(EntOfIndex(sprEnt))) RemoveEntity(EntOfIndex(sprEnt));
        RemoveEntity(entity);
    }

    // ---- 创建抛射物 ----

    private static edict_t* CreateProjectile(int id, int i, string className, Span<float> origin, Span<float> targetOrigin)
    {
        int entityType = 0;
        edict_t* entity;

        // 抛射物模型
        if (CheckSuffix(ProjectileModelPath[id, i], "mdl") || CheckSuffix(ProjectileModelPath[id, i], "MDL"))
        {
            entityType = ProjectileModel;
            entity = CreateNamedEntityByIndex(InfoTarget);
        }
        else if (CheckSuffix(ProjectileModelPath[id, i], "spr") || CheckSuffix(ProjectileModelPath[id, i], "SPR"))
        {
            entityType = ProjectileSprite;
            entity = CreateNamedEntityByIndex(EnvSprite);
        }
        else return null;

        if (!IsValid(entity)) return null;

        SetModel(entity, ProjectileModelPath[id, i]);
        var pev = &entity->v;

        // 抛射物重力
        if (ProjectileGravity[id, i] == 0.0f) pev->movetype = 5;  // MOVETYPE_FLY
        else pev->movetype = 6;                                    // MOVETYPE_TOSS
        pev->gravity = ProjectileGravity[id, i];

        // 飞行速度 & 飞行高度修正
        Span<float> velocity = stackalloc float[3];
        GetSpeedVector(origin, targetOrigin, ProjectileFlySpeed[id, i], velocity);
        if (ProjectileGravity[id, i] > 0.0f) velocity[2] += ProjectileHeightOfs[id, i];
        pev->velocity.x = velocity[0];
        pev->velocity.y = velocity[1];
        pev->velocity.z = velocity[2];

        // 延迟时间
        if (WeaponDamageType[id, i] == DamageTypeDelayExp)
            pev->dmgtime = GameTime + ProjectileDelay[id, i];

        // 存在时间
        if (ProjectileDuration[id, i] > 0.0f)
            pev->fuser4 = GameTime + ProjectileDuration[id, i];

        // 其他设置
        var pPlayer = EntOfIndex(id);
        float angleX = pPlayer != null ? -pPlayer->v.v_angle.x : 0.0f;
        float angleY = pPlayer != null ? pPlayer->v.v_angle.y : 0.0f;
        float angleZ = pPlayer != null ? pPlayer->v.v_angle.z : 0.0f;
        SetPDataInt(entity, WeaponIdPData, i);
        pev->solid = 1; // SOLID_TRIGGER
        entity->v.classname = AllocStr(className);
        entity->v.globalname = AllocStr(WpnProjectileName);
        pev->origin.x = origin[0];
        pev->origin.y = origin[1];
        pev->origin.z = origin[2];
        pev->angles.x = angleX;
        pev->angles.y = angleY;
        pev->angles.z = angleZ;
        pev->owner = pPlayer;
        pev->mins = new vec3_t { x = -1, y = -1, z = -1 };
        pev->maxs = new vec3_t { x = 1, y = 1, z = 1 };
        pev->nextthink = GameTime + 0.01f;
        pev->iuser2 = entityType;
        pev->framerate = ProjectileFramerate[id, i];
        if (entityType == ProjectileSprite)
        {
            pev->rendermode = 5; // kRenderTransAdd
            pev->renderamt = ProjectileBrightness[id, i];
            pev->rendercolor = new vec3_t { x = 255, y = 255, z = 255 };
            pev->scale = ProjectileScale[id, i];
            int modelIndex = ModelIndex(ProjectileModelPath[id, i]);
            int frames = EF.pfnModelFrames(modelIndex);
            pev->iuser3 = frames;
        }
        else pev->animtime = GameTime;

        // 抛射物拖尾
        if (ProjectileHasTail[id, i] != 0)
        {
            MessageBeginGlobal(0, 23);
            WriteByte(WpnConst.TeBeamFollow);
            WriteShort(IndexOf(entity));
            WriteShort(SprBeamFollow);
            WriteByte(10);
            WriteByte(3);
            WriteByte(ProjectileTailColor[id, i, 0]);
            WriteByte(ProjectileTailColor[id, i, 1]);
            WriteByte(ProjectileTailColor[id, i, 2]);
            WriteByte(ProjectileTailColor[id, i, 3]);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();
        }

        // 拖尾间隔SPR
        if (ProjectileTailSprite[id, i] > 0)
            pev->fuser1 = GameTime + ProjectileTailTime[id, i];

        // 抛射物附带SPR
        if (ProjectileExtraSprite[id, i] is { Length: > 0 })
        {
            var sprEnt = CreateNamedEntityByIndex(EnvSprite);
            if (IsValid(sprEnt))
            {
                var sprPev = &sprEnt->v;
                sprPev->globalname = AllocStr(WpnProjectileExtra);
                sprPev->movetype = 12; // MOVETYPE_FOLLOW
                sprPev->rendermode = 5; // kRenderTransAdd
                sprPev->renderamt = 255.0f;
                sprPev->rendercolor = new vec3_t { x = 255, y = 255, z = 255 };
                sprPev->aiment = entity;
                sprPev->framerate = ExtraSpriteFramerate[id, i];
                sprPev->scale = ExtraSpriteScale[id, i];
                SetModel(sprEnt, ProjectileExtraSprite[id, i]);
                int modelIndex = ModelIndex(ProjectileExtraSprite[id, i]);
                int frames = EF.pfnModelFrames(modelIndex);
                sprPev->iuser1 = frames;
                pev->iuser4 = IndexOf(sprEnt);
            }
        }

        // 抛射物发光
        if (ProjectileGlow[id, i] != 0)
        {
            MessageBeginPvs(3, 23, origin);
            WriteByte(WpnConst.TeDlight);
            WriteCoord(origin[0]);
            WriteCoord(origin[1]);
            WriteCoord(origin[2]);
            WriteByte(ProjectileGlowRadius[id, i] / 10);
            WriteByte(ProjectileGlowColor[id, i, 0]);
            WriteByte(ProjectileGlowColor[id, i, 1]);
            WriteByte(ProjectileGlowColor[id, i, 2]);
            WriteByte(1);
            WriteByte(0);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();

            MessageBeginPvs(3, 23, origin);
            WriteByte(WpnConst.TeElight);
            WriteShort(IndexOf(entity));
            WriteCoord(origin[0]);
            WriteCoord(origin[1]);
            WriteCoord(origin[2]);
            WriteCoord(10.0f);
            WriteByte(ProjectileGlowColor[id, i, 0]);
            WriteByte(ProjectileGlowColor[id, i, 1]);
            WriteByte(ProjectileGlowColor[id, i, 2]);
            WriteByte(2);
            WriteCoord(0.0f);
            ModuleRuntime.g_engfuncs.pfnMessageEnd();

            pev->fuser2 = GameTime + 0.01f;
        }

        return entity;
    }

    private static edict_t* CreateNamedEntityByIndex(int stringIndex) => EF.pfnCreateNamedEntity(stringIndex);

    private static int ModelIndex(string model)
    {
        using var m = model.GetNativeString();
        return EF.pfnModelIndex(m);
    }

    // ---- 消息写入小工具 ----

    private static void MessageBeginGlobal(int dest, int type) => EF.pfnMessageBegin(dest, type, null, null);

    private static void MessageBeginPvs(int dest, int type, Span<float> origin)
    {
        fixed (float* p = origin) EF.pfnMessageBegin(dest, type, p, null);
    }

    private static void WriteByte(int v) => EF.pfnWriteByte(v);
    private static void WriteShort(int v) => EF.pfnWriteShort(v);
    private static void WriteCoord(float v) => EF.pfnWriteCoord(v);

    private static int RandomNum(int lo, int hi) => EF.pfnRandomLong(lo, hi);

    // ---- 工具函数 ----

    private static int ParseInt(string value) => PawnMath.ParseInt(value);

    private static float ParseFloat(string value) => PawnMath.ParseFloat(value);

    private static float Distance(Span<float> a, Span<float> b)
    {
        float dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // stock get_speed_vector
    private static void GetSpeedVector(Span<float> origin1, Span<float> origin2, float speed, Span<float> newVelocity)
    {
        newVelocity[0] = origin2[0] - origin1[0];
        newVelocity[1] = origin2[1] - origin1[1];
        newVelocity[2] = origin2[2] - origin1[2];
        float len = MathF.Sqrt(newVelocity[0] * newVelocity[0] + newVelocity[1] * newVelocity[1] + newVelocity[2] * newVelocity[2]);
        float num = MathF.Sqrt(speed * speed / (len * len));
        newVelocity[0] *= num;
        newVelocity[1] *= num;
        newVelocity[2] *= num;
    }

    // HLSDK AngleVectors(mode: 1=forward 2=right 3=up)
    private static void AngleVector(Span<float> angles, int mode, Span<float> dest)
    {
        const float Deg2Rad = MathF.PI / 180.0f;
        float sp = MathF.Sin(angles[0] * Deg2Rad), cp = MathF.Cos(angles[0] * Deg2Rad);
        float sy = MathF.Sin(angles[1] * Deg2Rad), cy = MathF.Cos(angles[1] * Deg2Rad);
        float sr = MathF.Sin(angles[2] * Deg2Rad), cr = MathF.Cos(angles[2] * Deg2Rad);
        switch (mode)
        {
            case 1:
                dest[0] = cp * cy; dest[1] = cp * sy; dest[2] = -sp;
                break;
            case 2:
                dest[0] = -sr * sp * cy + cr * sy;
                dest[1] = -sr * sp * sy - cr * cy;
                dest[2] = -sr * cp;
                break;
            case 3:
                dest[0] = cr * sp * cy + sy * sr;
                dest[1] = cr * sp * sy - cy * sr;
                dest[2] = cr * cp;
                break;
        }
    }

    // stock get_position
    private static void GetPosition(int id, float forward, float right, float up, Span<float> start)
    {
        var pPlayer = EntOfIndex(id);
        if (pPlayer == null) return;
        var pev = pPlayer->v;

        float ox = pev.origin.x + pev.view_ofsr[0];
        float oy = pev.origin.y + pev.view_ofsr[1];
        float oz = pev.origin.z + pev.view_ofsr[2];
        Span<float> angles = stackalloc float[3] { pev.v_angle.x, pev.v_angle.y, pev.v_angle.z };
        Span<float> vForward = stackalloc float[3];
        Span<float> vRight = stackalloc float[3];
        Span<float> vUp = stackalloc float[3];
        AngleVector(angles, 1, vForward);
        AngleVector(angles, 2, vRight);
        AngleVector(angles, 3, vUp);
        start[0] = ox + vForward[0] * forward + vRight[0] * right + vUp[0] * up;
        start[1] = oy + vForward[1] * forward + vRight[1] * right + vUp[1] * up;
        start[2] = oz + vForward[2] * forward + vRight[2] * right + vUp[2] * up;
    }

    // stock fm_get_aim_origin
    private static void GetAimOrigin(int id, Span<float> origin)
    {
        var pPlayer = EntOfIndex(id);
        if (pPlayer == null) return;
        var pev = pPlayer->v;

        Span<float> start = stackalloc float[3] { pev.origin.x + pev.view_ofsr[0], pev.origin.y + pev.view_ofsr[1], pev.origin.z + pev.view_ofsr[2] };
        Span<float> angles = stackalloc float[3] { pev.v_angle.x, pev.v_angle.y, pev.v_angle.z };
        fixed (float* pAngles = angles)
        {
            EF.pfnMakeVectors(pAngles);
        }
        Span<float> dest = stackalloc float[3] { ModuleRuntime.gpGlobals->v_forward.x, ModuleRuntime.gpGlobals->v_forward.y, ModuleRuntime.gpGlobals->v_forward.z };
        dest[0] *= 9999.0f; dest[1] *= 9999.0f; dest[2] *= 9999.0f;
        dest[0] += start[0]; dest[1] += start[1]; dest[2] += start[2];

        TraceResult trace = default;
        fixed (float* pStart = start, pDest = dest)
        {
            EF.pfnTraceLine(pStart, pDest, 0, pPlayer, &trace);
        }
        origin[0] = trace.vecEndPos.x;
        origin[1] = trace.vecEndPos.y;
        origin[2] = trace.vecEndPos.z;
    }

    // stock CheckSuffix
    private static bool CheckSuffix(string fileName, string suffix)
    {
        int dot = fileName.LastIndexOf('.');
        if (dot < 0) return false;
        return string.Equals(fileName[(dot + 1)..], suffix, StringComparison.Ordinal);
    }

    // stock EntityTurn
    private static void EntityTurn(edict_t* entity, Span<float> target)
    {
        var pev = &entity->v;
        float dx = target[0] - pev->origin.x;
        float dy = target[1] - pev->origin.y;
        float dz = target[2] - pev->origin.z;
        float length = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        float nx = dx / length;
        float ny = dy / length;
        float nz = dz / length;
        Span<float> direction = stackalloc float[3] { nx, ny, nz };
        Span<float> angles = stackalloc float[3];
        fixed (float* pDir = direction, pAngles = angles)
        {
            EF.pfnVecToAngles(pDir, pAngles);
        }
        if (angles[1] > 180.0f) angles[1] -= 360.0f;
        if (angles[1] < -180.0f) angles[1] += 360.0f;
        if (angles[1] == 180.0f || angles[1] == -180.0f) angles[1] = -179.999999f;
        pev->angles.x = angles[0];
        pev->angles.y = angles[1];
        pev->angles.z = angles[2];
    }

    // stock is_wall_between_points
    private static bool IsWallBetweenPoints(Span<float> start, Span<float> end, edict_t* ignoreEntity)
    {
        TraceResult trace = default;
        fixed (float* pStart = start, pEnd = end)
        {
            EF.pfnTraceLine(pStart, pEnd, 1 /* IGNORE_MONSTERS */, ignoreEntity, &trace);
        }
        float dx = end[0] - trace.vecEndPos.x;
        float dy = end[1] - trace.vecEndPos.y;
        float dz = end[2] - trace.vecEndPos.z;
        return (int)MathF.Sqrt(dx * dx + dy * dy + dz * dz) != 0;
    }

    // ---- 常量子集(与核心模块 WpnConst 对应,Pawn 版 wpn_const.inc) ----
    private static class WpnConst
    {
        public const int WpnIgnored = 0;
        public const int WpnSupercede = 1;
        public const int WpnHandled = 2;

        public const int BuyEverywhere = 2;

        public const int SetBuy = 100;
        public const int KillSpr = 2;

        public const int DmgTypeExplosion = 2;
        public const int DmgTypeTouch = 3;

        public const int SpdmgCantHeadshot = 1 << 0;

        public const int TeExplosion = 3;
        public const int TeBeamFollow = 22;
        public const int TeDlight = 27;
        public const int TeElight = 28;
        public const int TeWorldDecal = 116;
        public const int TeExplFlagNoDlights = 2;
        public const int TeExplFlagNoSound = 4;
        public const int TeExplFlagNoParticles = 8;
    }
}
