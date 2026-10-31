namespace Module;

// CS1.6 CBasePlayer / CBasePlayerItem 的 pvPrivateData 单元偏移。
// Windows 值为基准;Linux 值 = Windows + 差值,差值按成员族划分(与原版 .sma 的显式 linuxdiff 完全吻合,
// 并以社区公认偏移校准:m_rgpPlayerItems Linux=408、m_pActiveItem=414,即玩家 cbase 族 +41):
//   玩家 int/float(偏移 < 366)    +5
//   玩家 cbase 指针(365..373)     +41
//   玩家 m_rgAmmo(377..390)       +41
//   武器实体 CBasePlayerItem 全部  +4
// 注意:原版 .sma 的玩家 cbase 读取未带 linuxdiff(依赖 AMXX 默认 +4),在 Linux 下实为错误值,
// 本表以正确偏移为准;387(闪光弹备弹)原版写了 +4,同属 m_rgAmmo 族,按 +41 修正。
// 当前构建目标仍是 win-x86;Linux 列为代码层预备,构建与部署另行处理。
internal static class CsOffsets
{
    private static readonly bool Linux = OperatingSystem.IsLinux();

    private static int Sel(int win, int linux) => Linux ? linux : win;

    public const int VipBit = 0x100; // m_bIsVIP 在其所在单元内的位

    // ---- CBasePlayer(玩家) ----

    public static readonly int NextAttack = Sel(83, 88);    // m_flNextAttack(浮点)
    public static readonly int ArmorType = Sel(112, 117);   // 防弹衣类型(1=护甲 2=护甲+头盔)
    public static readonly int Team = Sel(114, 119);        // m_iTeam
    public static readonly int Money = Sel(115, 120);       // m_iAccount
    public static readonly int NVGs = Sel(129, 134);        // 夜视仪拥有位(1<<0)
    public static readonly int DefuseKit = Sel(193, 198);   // 拆弹器拥有位(1<<16)
    public static readonly int Vip = Sel(209, 214);         // VIP 标记(与 VipBit 配用;原版未显式给 linuxdiff,按玩家 int 族推定)
    public static readonly int CanBuy = Sel(235, 240);      // 可购买判定位(1<<0)
    public static readonly int HitGroup = Sel(75, 80);      // 最近命中部位标记(本插件写入)
    public static readonly int ZoomSignal = Sel(361, 366);  // 机瞄信号位(1<<6)
    public static readonly int Zoom = Sel(363, 368);        // 镜头焦距(90=默认)

    // 玩家武器槽 cbase 指针
    public static readonly int PrimaryWeapon = Sel(368, 409);   // m_rgpPlayerItems[1]
    public static readonly int SecondaryWeapon = Sel(369, 410); // m_rgpPlayerItems[2]
    public static readonly int KnifeSlot = Sel(370, 411);       // m_rgpPlayerItems[3]
    public static readonly int ActiveItem = Sel(373, 414);      // m_pActiveItem

    /// <summary>玩家武器槽位指针:slot 0..5 → 367..372 / 408..413</summary>
    public static int PlayerSlot(int slot) => Sel(367 + slot, 408 + slot);

    /// <summary>玩家后备弹药单元(m_rgAmmo 族,Linux 整族 +41):CSW id → 偏移,未知武器返回 -1</summary>
    public static int BpAmmoOffset(int weaponId) => weaponId switch
    {
        EngineConst.CswAwp => Sel(377, 418),
        EngineConst.CswScout or EngineConst.CswAk47 or EngineConst.CswG3sg1 => Sel(378, 419),
        EngineConst.CswM249 => Sel(379, 420),
        EngineConst.CswM4a1 or EngineConst.CswFamas or EngineConst.CswAug or EngineConst.CswSg550 or EngineConst.CswGali or EngineConst.CswSg552 => Sel(380, 421),
        EngineConst.CswM3 or EngineConst.CswXm1014 => Sel(381, 422),
        EngineConst.CswUsp or EngineConst.CswUmp45 or EngineConst.CswMac10 => Sel(382, 423),
        EngineConst.CswFiveseven or EngineConst.CswP90 => Sel(383, 424),
        EngineConst.CswDeagle => Sel(384, 425),
        EngineConst.CswP228 => Sel(385, 426),
        EngineConst.CswGlock18 or EngineConst.CswMp5Navy or EngineConst.CswTmp or EngineConst.CswElite => Sel(386, 427),
        EngineConst.CswFlashbang => Sel(387, 428),
        EngineConst.CswHegrenade => Sel(388, 429),
        EngineConst.CswSmokegrenade => Sel(389, 430),
        EngineConst.CswC4 => Sel(390, 431),
        _ => -1,
    };

    // ---- CBasePlayerItem / CBasePlayerWeapon(武器实体,族内统一 +4) ----

    public static readonly int ItemTemplateSerial = Sel(11, 15);      // 模板序号标记(借用单元;WE 的 WEAPON_ID 同源)
    public static readonly int ItemId = Sel(43, 47);                  // m_iId(CSW)
    public static readonly int ItemClip = Sel(51, 55);                // m_iClip
    public static readonly int ItemInReload = Sel(54, 58);            // 装填进行中标记
    public static readonly int ItemInSpecialReload = Sel(55, 59);     // m_fInSpecialReload
    public static readonly int ItemWeaponState = Sel(74, 78);         // 消音位:(1<<0) USP / (1<<2) M4A1
    public static readonly int ItemNextPrimaryAttack = Sel(46, 50);   // m_flNextPrimaryAttack
    public static readonly int ItemNextSecondaryAttack = Sel(47, 51); // m_flNextSecondaryAttack
    public static readonly int ItemTimeWeaponIdle = Sel(48, 52);      // m_flTimeWeaponIdle(代码中亦存掏出/换弹计时)
    public static readonly int ItemOwner = Sel(41, 45);               // m_pPlayer(cbase)

    /// <summary>weaponbox 内武器槽 cbase 指针:34+i / 38+i</summary>
    public static int BoxItem(int index) => Sel(34 + index, 38 + index);

    // ---- 通用实体 ----

    public static readonly int HitCooldown = Sel(15, 19); // 命中冷却标记(借用浮点单元,防同帧重复伤害)
}
