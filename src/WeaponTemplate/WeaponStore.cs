namespace Module;

// WeaponTemplate.sma 顶部的静态数据表与 33(MAXWEAPON)二维武器模板数组。
// [id, i] 维度与原版 g_xxx[id][i] 一致(id=1..32 玩家, i=1..127 武器序号)。
public static unsafe class Store
{
    // ---- 原版 CS 游戏武器静态表(下标 = CSW id) ----

    public static readonly string[] GameWeaponClassName =
    {
        "", "weapon_p228", "", "weapon_scout", "weapon_hegrenade", "weapon_xm1014", "weapon_c4", "weapon_mac10", "weapon_aug", "weapon_smokegrenade",
        "weapon_elite", "weapon_fiveseven", "weapon_ump45", "weapon_sg550", "weapon_galil", "weapon_famas", "weapon_usp", "weapon_glock18", "weapon_awp", "weapon_mp5navy", "weapon_m249",
        "weapon_m3", "weapon_m4a1", "weapon_tmp", "weapon_g3sg1", "weapon_flashbang", "weapon_deagle", "weapon_sg552", "weapon_ak47", "weapon_knife", "weapon_p90",
    };

    public static readonly string[] GameWeaponBuyCommand =
    {
        "", "p228", "", "scout", "hegren", "xm1014", "c4", "mac10", "aug", "sgren", "elites", "fn57", "ump45", "sg550", "galil", "famas", "usp",
        "glock", "awp", "mp5", "m249", "m3", "m4a1", "tmp", "g3sg1", "flash", "deagle", "sg552", "ak47", "knife", "p90",
    };

    public static readonly string[] GameWeaponKillSpr =
    {
        "", "p228", "", "scout", "grenade", "xm1014", "", "mac10", "aug", "", "elite", "fiveseven", "ump45", "sg550", "galil", "famas", "usp",
        "glock18", "awp", "mp5navy", "m249", "m3", "m4a1", "tmp", "g3sg1", "flashbang", "deagle", "sg552", "ak47", "knife", "p90",
    };

    public static readonly string[] GameWeaponAmmoType =
    {
        "", "357sig", "", "762nato", "", "buckshot", "", "45acp", "556nato", "", "9mm", "57mm", "45acp", "556nato", "556nato", "556nato", "45acp", "9mm", "338magnum",
        "9mm", "556natobox", "buckshot", "556nato", "9mm", "762nato", "", "50ae", "556nato", "762nato", "", "57mm",
    };

    public static readonly int[] GameWeaponDefaultTeam = { 0, 0, 0, 0, 0, 0, 0, 1, 2, 0, 1, 2, 0, 2, 1, 2, 0, 0, 0, 0, 0, 0, 2, 2, 1, 0, 0, 1, 1, 0, 0 };

    public static readonly int[] GameWeaponDefaultCost = { -1, 600, -1, 2750, 300, 3000, 0, 1400, 3500, 300, 800, 750, 1700, 4200, 2000, 2250, 500, 400, 4750, 1500, 5750, 1700, 3100, 1250, 5000, 200, 650,
        3500, 2500, 0, 2350 };

    public static readonly int[] GameWeaponAmmoId = { -1, 9, -1, 2, 12, 5, 14, 6, 4, 13, 10, 7, 6, 4, 4, 4, 6, 10, 1, 10, 3, 5, 4, 10, 2, 11, 8, 4, 2, 0, 7 };

    public static readonly int[] GameWeaponPosition = { -1, 1, -1, 0, 3, 0, 4, 0, 0, 3, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 3, 1, 0, 0, 2, 0 };

    public static readonly int[] GameWeaponSlot = { -1, 3, -1, 9, 1, 12, 3, 13, 14, 3, 5, 6, 15, 16, 17, 18, 4, 2, 2, 7, 4, 5, 6, 11, 3, 2, 1, 10, 1, 1, 8 };

    public static readonly int[] GameWeaponMaxBpammoBase = { -1, 52, -1, 90, 1, 32, 1, 100, 90, 1, 120, 100, 100, 90, 90, 90, 100, 120, 30, 120, 200, 32, 90, 120, 90, 2, 35, 90, 90, -1, 100 };

    public static readonly int[] PrimId =
    {
        EngineConst.CswScout, EngineConst.CswXm1014, EngineConst.CswMac10, EngineConst.CswAug, EngineConst.CswUmp45, EngineConst.CswSg550, EngineConst.CswGalil,
        EngineConst.CswFamas, EngineConst.CswAwp, EngineConst.CswMp5Navy, EngineConst.CswM249, EngineConst.CswM3, EngineConst.CswM4a1, EngineConst.CswTmp,
        EngineConst.CswG3sg1, EngineConst.CswSg552, EngineConst.CswAk47, EngineConst.CswP90,
    };

    public static readonly int[] PrimCost = { 80, 65, 25, 60, 25, 60, 60, 60, 125, 20, 60, 65, 60, 20, 80, 60, 80, 50 };
    public static readonly int[] PrimAmount = { 30, 8, 12, 30, 12, 30, 30, 30, 10, 30, 30, 8, 30, 30, 30, 30, 30, 50 };

    public static readonly int[] SecondId =
    {
        EngineConst.CswP228, EngineConst.CswElite, EngineConst.CswFiveseven, EngineConst.CswUsp, EngineConst.CswGlock18, EngineConst.CswDeagle,
    };

    public static readonly int[] SecondCost = { 50, 20, 50, 25, 20, 40 };
    public static readonly int[] SecAmount = { 13, 30, 50, 12, 30, 7 };

    public static readonly int[] GunshotDecals = { 41, 42, 43, 44, 45 };

    // ---- 玩家/武器二维模板数据 ----

    public static int WeaponAmount;

    public static readonly int[,] WeaponId = new int[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponList = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponPModel = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponVModel = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponWModel = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponSpr = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponSound1 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponSound2 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponSound3 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponSound4 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponSound5 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponSound6 = new string[33, WpnConst.MaxWeapon];
    public static readonly int[,] ReloadMode = new int[33, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponClip = new int[33, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponAmmo = new int[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponDamage = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponSpeed = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponAimSpeed = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponSilencedSpeed = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponRecoil = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponAimRecoil = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,,] WeaponReloadTime = new float[33, WpnConst.MaxWeapon, 3];
    public static readonly string[,] WeaponBuyCommand = new string[33, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponCost = new int[33, WpnConst.MaxWeapon];
    public static readonly int[,,] WeaponAnim = new int[33, WpnConst.MaxWeapon, WpnConst.MaxAnimation];
    public static readonly float[,] WeaponDrawTime = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,,] GrenadeData = new float[33, WpnConst.MaxWeapon, 3];
    public static readonly int[,] GrenadeSprId = new int[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponAimAccuracy = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponAccuracy = new float[33, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponTeam = new int[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponMaxSpeed = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponKnockBack = new float[33, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponTemplateSerialNumber = new int[3, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponSight = new int[33, WpnConst.MaxWeapon];
    public static readonly string[,] WeaponVModelS = new string[33, WpnConst.MaxWeapon];
    public static readonly float[,] SightDistance = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] SightFrameRate = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] SightOpenTime = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] SightCloseTime = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,,] WeaponSilencedTime = new float[33, WpnConst.MaxWeapon, 2];
    public static readonly float[,] WeaponSilencedRecoil = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponSilencedAccuracy = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponAimMaxSpeed = new float[33, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponWModelBody = new int[33, WpnConst.MaxWeapon];
    public static readonly int[,] WeaponAmmoCost = new int[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponDamage2 = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeSlashResetTime = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeSlashTime = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeSlashRange = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeStabResetTime = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeStabTime = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeStabRange = new float[33, WpnConst.MaxWeapon];
    public static readonly int[,,] KnifeAnim = new int[33, WpnConst.MaxWeapon, WpnConst.KnifeMaxAnimation];
    public static readonly float[,] KnifeSlashAngle = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeStabAngle = new float[33, WpnConst.MaxWeapon];
    public static readonly int[,] CurWeaponBPAmmo = new int[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponFireSoundVolume = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] WeaponSilenceFireSoundVolume = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,,] KnifeSoundVolume = new float[33, WpnConst.MaxWeapon, 5];
    public static readonly float[,] KnifeSlashAngleOffset = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeStabAngleOffset = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeSlashHeight = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeStabHeight = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeSlashHeightOffset = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] KnifeStabHeightOffset = new float[33, WpnConst.MaxWeapon];

    public static readonly int[,] User1 = new int[33, WpnConst.MaxWeapon];
    public static readonly int[,] User2 = new int[33, WpnConst.MaxWeapon];
    public static readonly int[,] User3 = new int[33, WpnConst.MaxWeapon];
    public static readonly int[,] User4 = new int[33, WpnConst.MaxWeapon];
    public static readonly float[,] FUser1 = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] FUser2 = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] FUser3 = new float[33, WpnConst.MaxWeapon];
    public static readonly float[,] FUser4 = new float[33, WpnConst.MaxWeapon];
    public static readonly string[,] SzUser1 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] SzUser2 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] SzUser3 = new string[33, WpnConst.MaxWeapon];
    public static readonly string[,] SzUser4 = new string[33, WpnConst.MaxWeapon];

    // ---- 玩家一维状态 ----

    public static float DisableBuyTime;
    public static bool IsInPrimaryAttack;
    public static readonly float[,] PushAngle = new float[33, 3];   // cl_fPushAngle[id][3]
    public static readonly int[] ClipAmmo = new int[33];
    public static readonly int[] TmpClip = new int[33 * WpnConst.MaxWeapon];
    public static bool BotForwardRegister = true;                  // g_fwBotForwardRegister
    public static readonly int[] KnifeAttack = new int[33];
    public static readonly int[] KnifeSlashMode = new int[33];
    public static readonly float[] PlayerThink = new float[33];
    public static int BloodSpray;                                   // spr_blood_spray
    public static int BloodDrop;                                    // spr_blood_drop
    public static readonly int[] UserGrenadeId = new int[33];
    public static int SmokeId;                                      // g_iSmokeId
    public static int BloodColor;
    public static readonly int[] BlockBuy = new int[33];
    public static int AllTeamBuy;
    public static readonly int[] SightMode = new int[33];
    public static readonly bool[] Reloading = new bool[33];        // Reload[id]
    public static readonly float[] NextThink = new float[33];
    public static readonly bool[] OldButton = new bool[33];
    public static bool RoundStart;                                  // g_iRoundStart
    public static readonly int[] BotMoney = new int[33];
    public static readonly bool[] BotBuyWeapon = new bool[33];
    public static readonly float[] BotNextThink = new float[33];
    public static int Round;
    public static readonly int[] AttackerEntity = new int[33];
    public static readonly float[] KnifeTempDamage = new float[33];
    public static readonly int[] GameWeaponMaxBpammo = new int[32];
    public static readonly int[,] GameCurWeaponBPAmmo = new int[33, 32];
    public static readonly float[] DamagePoint = new float[33];
    public static readonly float[] DamageVictimPoint = new float[33];
    public static readonly float[] KnockBackPoint = new float[33];
    public static int MaxMoney = WpnConst.MaxMoney;
    public static readonly int[] AttackHitGroup = new int[33];
    public static bool RoundRestart;                                // g_iRoundRestart
    public static readonly int[] EliteShotNumber = new int[33];
    public static readonly int[] UserKnifeAttack = new int[33];
    public static readonly float[,] UserDeploy = new float[33, 2];
    public static readonly bool[] BuyingAmmo = new bool[33];

    // GetWeaponType(iEntity) 的 C# 对应:读武器实体 11 号单元的模板序号。
    public static int GetWeaponType(GoldSrc.HLSDK.Native.edict_t* ent)
    {
        if (!Eng.IsValid(ent)) return 0;
        int serial = Eng.GetPDataInt(ent, 11);
        return serial >= 1 && serial <= WeaponAmount ? serial : 0;
    }

    // Pawn 插件每张地图重新加载,全局量归零;C# 模块常驻,在 plugin_precache 里显式重置。
    public static void ResetAll()
    {
        WeaponAmount = 0;
        foreach (System.Reflection.FieldInfo field in typeof(Store).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            object? value = field.GetValue(null);
            if (value is Array array)
                Array.Clear(array);
        }

        // 常驻默认值
        MaxMoney = WpnConst.MaxMoney;
        BotForwardRegister = true;
    }
}
