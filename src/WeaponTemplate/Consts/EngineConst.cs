namespace Module;

// 引擎与 AMXX 侧常量(in_buttons.h / damage.h / const.h / message_const.inc / amxconst.inc / ham_const.inc)。
public static class EngineConst
{
    // CSW_* 武器id(cs weapons.h)
    public const int CswP228 = 1;
    public const int CswScout = 3;
    public const int CswHegrenade = 4;
    public const int CswXm1014 = 5;
    public const int CswC4 = 6;
    public const int CswMac10 = 7;
    public const int CswAug = 8;
    public const int CswSmokegrenade = 9;
    public const int CswElite = 10;
    public const int CswFiveseven = 11;
    public const int CswUmp45 = 12;
    public const int CswSg550 = 13;
    public const int CswGalil = 14;
    public const int CswGali = 14;
    public const int CswFamas = 15;
    public const int CswUsp = 16;
    public const int CswGlock18 = 17;
    public const int CswAwp = 18;
    public const int CswMp5Navy = 19;
    public const int CswM249 = 20;
    public const int CswM3 = 21;
    public const int CswM4a1 = 22;
    public const int CswTmp = 23;
    public const int CswG3sg1 = 24;
    public const int CswFlashbang = 25;
    public const int CswDeagle = 26;
    public const int CswSg552 = 27;
    public const int CswAk47 = 28;
    public const int CswKnife = 29;
    public const int CswP90 = 30;
    public const int CswVest = 31;
    public const int CswVesthelm = 32;

    // in_buttons.h
    public const int InAttack = 1 << 0;
    public const int InAttack2 = 1 << 11;
    public const int InReload = 1 << 13;

    // damage.h
    public const int DmgNeverGib = 1 << 12;
    public const int DmgBullet = 1 << 1;

    // flags.h / movetype.h / solid.h / render.h
    public const int FlOnground = 1 << 9;
    public const int FlDucking = 1 << 14;
    public const int FlFakeclient = 1 << 13;
    public const int MoveTypeFly = 5;
    public const int MoveTypeToss = 6;
    public const int MoveTypeFollow = 12;
    public const int SolidNot = 0;
    public const int SolidTrigger = 1;
    public const int KRenderTransAdd = 5;
    public const int SfNoRespawn = 1 << 30;

    // sound constants
    public const float VolNorm = 1.0f;
    public const float AttnNorm = 0.8f;
    public const int PitchNorm = 100;
    public const int ChanAuto = 0;
    public const int ChanWeapon = 1;
    public const int ChanItem = 3;
    public const int ChanStatic = 6;

    // messagelist (user message dest) & SVC
    public const int MsgBroadcast = 0;
    public const int MsgOne = 1;
    public const int MsgAll = 2;
    public const int MsgPvs = 3;
    public const int MsgPas = 4;
    public const int SvcTempEntity = 23;
    public const int SvcWeaponAnim = 35;

    // message_const.inc (temp entity)
    public const int TeExplosion = 3;
    public const int TeBeamFollow = 22;
    public const int TeDlight = 27;
    public const int TeElight = 28;
    public const int TeBloodstream = 101;
    public const int TeBlood = 103;
    public const int TeDecal = 104;
    public const int TeGunshotDecal = 109;
    public const int TeBloodSprite = 115;
    public const int TeWorldDecal = 116;
    public const int TeExplFlagNoDlights = 2;
    public const int TeExplFlagNoSound = 4;
    public const int TeExplFlagNoParticles = 8;

    // set_msg_arg_int 的类型(message_const.inc, ARG_BYTE=1)
    public const int ArgByte = 1;

    // amxconst.inc
    public const int PluginContinue = 0;
    public const int PluginHandled = 1;

    // amxconst.inc client_print 输出类型: print_console=0, print_chat=1, print_center=2
    public const int PrintCenter = 2;

    // hitgroup (hulls.h / util.h)
    public const int HitGeneric = 0;
    public const int HitHead = 1;
    public const int HitChest = 2;
    public const int HitStomach = 3;
    public const int HitLeftArm = 4;
    public const int HitRightArm = 5;
    public const int HitLeftLeg = 6;
    public const int HitRightLeg = 7;

    // angle_vector 模式
    public const int AngleVectorForward = 1;
    public const int AngleVectorRight = 2;
    public const int AngleVectorUp = 3;

    // fakemeta FMRES_* 仅作注释语义参考;metamod 函数表 hook 中
    // "SUPERCEDE" 由"不调用原版函数"表达。

    // hamsandwich 返回值(ham_const.inc, 与 FMRES 对应)
    public const int HamIgnored = 1;
    public const int HamHandled = 2;
    public const int HamOverride = 3;
    public const int HamSupercede = 4;

    // Ham 虚函数枚举值(ham_const.inc 的 enum Ham 序号)
    public const int HamSpawn = 0;
    public const int HamTraceAttack = 8;
    public const int HamTakeDamage = 9;
    public const int HamKilled = 11;
    public const int HamGiveAmmo = 22;
    public const int HamThink = 41;
    public const int HamUse = 43;
    public const int HamItemAddToPlayer = 63;
    public const int HamItemDeploy = 66;
    public const int HamItemHolster = 68;
    public const int HamItemPostFrame = 71;
    public const int HamItemItemSlot = 79;
    public const int HamWeaponPrimaryAttack = 87;
    public const int HamWeaponSecondaryAttack = 88;
    public const int HamWeaponReload = 89;
    public const int HamWeaponWeaponIdle = 90;
    public const int HamCsItemGetMaxSpeed = 100;
}
