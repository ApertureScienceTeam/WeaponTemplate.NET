namespace Module;

// WeaponTemplate.sma 顶部的 CS 游戏武器静态表(下标 = CSW id),只读、不随地图重置。
public static class GameTables
{
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
}
