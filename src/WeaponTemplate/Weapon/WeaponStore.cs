using GoldSrc.HLSDK.Native;

namespace Module;

// 模块状态的唯一入口,数据本身分三层:
//   GameTables  - CS 静态常量表(只读)
//   Config      - 所有武器模板配置(注册/INI 写入,每张地图重建)
//   Players     - 按玩家 id 懒创建的运行时数据
// 原版 g_xxx[MAXPLAYERS+1][MAXWEAPONS] 在加载时复制 32 份;
// 这里模板只存一份,玩家只有在 WPN_SetParam* 改过某模板时才有自己的副本。
public static unsafe class WeaponStore
{
    public static readonly WeaponConfig Config = new();
    public static readonly Dictionary<int, PlayerData> Players = new();

    public static int WeaponAmount => Config.WeaponAmount;
    public static bool IsPlayerIndex(int id) => id >= 1 && id <= 32;
    public static bool IsTemplate(int index) => Config.IsTemplate(index);

    public static PlayerData Player(int id)
    {
        if (!Players.TryGetValue(id, out var data))
        {
            data = new PlayerData(id, Config);
            Players[id] = data;
        }
        return data;
    }

    public static WeaponData Weapon(int id, int index) => Player(id).Weapon(index);
    public static WeaponData WriteWeapon(int id, int index) => Player(id).WeaponForWrite(index);

    public static int CreateTemplate()
    {
        int index = Config.Create();
        // 运行时注册模板也要让已有玩家跟上新序号
        foreach (var player in Players.Values) player.EnsureTemplateCapacity(index);
        return index;
    }

    // ---- 全局状态(不属于任何玩家) ----

    public static float DisableBuyTime;
    public static bool IsInPrimaryAttack;
    public static bool BotForwardRegister = true;                  // g_fwBotForwardRegister
    public static int BloodSpray;                                   // spr_blood_spray
    public static int BloodDrop;                                    // spr_blood_drop
    public static int SmokeId;                                      // g_iSmokeId
    public static int BloodColor;
    public static int AllTeamBuy;
    public static bool RoundStart;                                  // g_iRoundStart
    public static bool RoundRestart;                                // g_iRoundRestart
    public static int Round;
    public static readonly int[] GameWeaponMaxBpammo = new int[32];
    public static int MaxMoney = WpnConst.MaxMoney;

    public static bool IsGun(int csw) => csw > 0 && csw < GameTables.GameWeaponClassName.Length &&
        GameTables.GameWeaponClassName[csw].Length > 0 && csw is not (EngineConst.CswKnife or EngineConst.CswHegrenade
            or EngineConst.CswFlashbang or EngineConst.CswSmokegrenade or EngineConst.CswC4);

    // GetWeaponType(iEntity) 的 C# 对应:读武器实体 11 号单元的模板序号。
    public static int GetWeaponType(edict_t* ent)
    {
        if (!Engine.IsValid(ent)) return 0;
        int serial = Engine.GetPDataInt(ent, CsOffsets.ItemTemplateSerial);
        return serial >= 1 && serial <= WeaponAmount ? serial : 0;
    }

    public static void ApplyDefaultSoundVolumes()
    {
        for (int i = 1; i <= Config.WeaponAmount; i++) Config[i].ApplyDefaultSoundVolumes();
        // 注册发生在玩家入场前,这里顺带兜底玩家已有的副本
        foreach (var player in Players.Values)
            foreach (var data in player.Overrides) data.ApplyDefaultSoundVolumes();
    }

    // Pawn 插件每张地图重新加载,全局量归零;C# 模块常驻,在 plugin_precache 里显式重置。
    public static void ResetAll()
    {
        Config.Reset();
        Players.Clear();

        DisableBuyTime = 0;
        IsInPrimaryAttack = false;
        BloodSpray = BloodDrop = SmokeId = BloodColor = 0;
        AllTeamBuy = Round = 0;
        RoundStart = RoundRestart = false;
        Array.Clear(GameWeaponMaxBpammo);
        Array.Copy(GameTables.GameWeaponMaxBpammoBase, GameWeaponMaxBpammo, GameTables.GameWeaponMaxBpammoBase.Length);
        // 常驻默认值
        MaxMoney = WpnConst.MaxMoney;
        BotForwardRegister = true;
    }
}
