using System.Runtime.CompilerServices;
#pragma warning disable CS8981
using cell = int;

namespace Module;
// wpn_const.inc 原样翻译(中文注释保留),另附引擎/AMXX 侧用到的数值常量。
public static class WpnConst
{
    /* 模板数量软上限:只做注册防呆,不参与任何数组尺寸 */
    public const int MaxTemplateCount = 512;

    /* 事件返回值 */
    public const int WPN_IGNORED = 0;
    public const int WPN_SUPERCEDE = 1;
    public const int WPN_HANDLED = 2;

    /* 获取武器序号的模板序号类型 */
    public const int SlotWeapon = 0;
    public const int SlotKnife = 1;
    public const int SlotGrenade = 2;

    /* 原版刀子攻击距离 */
    public const int DefaultDistanceSwing = 48;
    public const int DefaultDistanceStab = 32;

    /* 机瞄状态 */
    public const int Closed = 0;
    public const int Open = 1;
    public const int AtoB = 2;
    public const int BtoA = 3;

    /* 最大金钱量 */
    public const int MaxMoney = 16000;

    /* 购买限制设置 */
    public const int BuyNormally = 0;
    public const int BuyCannot = 1;
    public const int BuyEverywhere = 2;

    /* BOT购买的Special Weapon */
    public const int SwVesthelm = 1;
    public const int SwDefuser = 2;
    public const int SwHegrenade = 3;
    public const int SwFlashbang = 4;
    public const int SwSmokegrenade = 5;

    /* 最大主武器id数 */
    public const int MaxPrim = 18;

    /* 最大副武器id数 */
    public const int MaxSec = 6;

    /* 伤害时的特殊标记 */
    public const int SpdmgNormal = 0;
    public const int SpdmgCantHeadshot = 1 << 0;
    public const int SpdmgTeamattack = 1 << 1;

    /* CheckAttack的返回值 */
    public const int HitEmpty = 0;
    public const int HitPlayer = 1;
    public const int HitEntity = 2;
    public const int HitWall = 3;

    /* 伤害类型 */
    public const int DmgTypeSector = 0;
    public const int DmgTypeLine = 1;
    public const int DmgTypeExplosion = 2;
    public const int DmgTypeTouch = 3;

    /* 动作序号(ANIM_*) */
    public const int AnimIdle = 0;
    public const int AnimReload = 1;
    public const int AnimDraw = 2;
    public const int AnimShoot1 = 3;
    public const int AnimShoot2 = 4;
    public const int AnimShoot3 = 5;
    public const int AnimStartReload = 6;
    public const int AnimInsert = 7;
    public const int AnimAfterReload = 8;
    public const int AnimSightOpen = 9;
    public const int AnimSightClose = 10;
    public const int AnimDetachSilencer = 11;
    public const int AnimAttachSilencer = 12;
    public const int AnimIdleSilenced = 13;
    public const int AnimReloadSilenced = 14;
    public const int AnimDrawSilenced = 15;
    public const int AnimShoot1Silenced = 16;
    public const int AnimShoot2Silenced = 17;
    public const int AnimShoot3Silenced = 18;
    public const int MaxAnimation = 19;

    /* 刀子动作序号(KNIFE_ANIM_*) */
    public const int KnifeAnimIdle = 0;
    public const int KnifeAnimSlash1 = 1;
    public const int KnifeAnimSlash2 = 2;
    public const int KnifeAnimDraw = 3;
    public const int KnifeAnimStab = 4;
    public const int KnifeAnimStabMiss = 5;
    public const int KnifeAnimMidSlash = 6;
    public const int KnifeAnimMidSlashMiss = 7;
    public const int KnifeAnimSlashBegin = 8;
    public const int KnifeAnimStabBegin = 9;
    public const int KnifeMaxAnimation = 10;

    /* 武器整数型数据下标(e_i*) */
    public const int EiReloadMode = 0;
    public const int EiWeaponSight = 1;
    public const int EiWeaponClip = 2;
    public const int EiWeaponAmmo = 3;
    public const int EiWeaponCost = 4;
    public const int EiWeaponAmmoCost = 5;
    public const int EiWeaponTeam = 6;
    public const int EiWeaponWModelBody = 7;
    public const int MaxWeaponIntData = 8;

    /* 武器浮点型数据下标(e_f*) */
    public const int EfWeaponDamage = 0;
    public const int EfWeaponSpeed = 1;
    public const int EfWeaponAimSpeed = 2;
    public const int EfWeaponSilencedSpeed = 3;
    public const int EfWeaponRecoil = 4;
    public const int EfWeaponAimRecoil = 5;
    public const int EfWeaponSilencedRecoil = 6;
    public const int EfWeaponAccuracy = 7;
    public const int EfWeaponAimAccuracy = 8;
    public const int EfWeaponSilencedAccuracy = 9;
    public const int EfWeaponReloadTime = 10;
    public const int EfWeaponDrawTime = 11;
    public const int EfWeaponMaxSpeed = 12;
    public const int EfWeaponAimMaxSpeed = 13;
    public const int EfWeaponKnockBack = 14;
    public const int EfSightDistance = 15;
    public const int EfSightFrameRate = 16;
    public const int EfSightOpenTime = 17;
    public const int EfSightCloseTime = 18;
    public const int EfWeaponSilencedTime1 = 19;
    public const int EfWeaponSilencedTime2 = 20;
    public const int EfWeaponFireSoundVolume = 21;
    public const int EfWeaponSilenceFireSoundVolume = 22;
    public const int EfShotgunStartReloadTime = 23;
    public const int EfShotgunAfterReloadTime = 24;
    public const int MaxWeaponFloatData = 25;

    /* 刀子浮点型数据下标(e_fKnife*) */
    public const int EfKnifeSlashResetTime = 0;
    public const int EfKnifeSlashTime = 1;
    public const int EfKnifeSlashRange = 2;
    public const int EfKnifeSlashAngle = 3;
    public const int EfKnifeStabResetTime = 4;
    public const int EfKnifeStabTime = 5;
    public const int EfKnifeStabRange = 6;
    public const int EfKnifeStabAngle = 7;
    public const int EfKnifeDrawTime = 8;
    public const int EfKnifeDamage = 9;
    public const int EfKnifeDamage2 = 10;
    public const int EfKnifeMaxSpeed = 11;
    public const int EfKnifeKnockBack = 12;
    public const int EfKnifeSoundVolume1 = 13;
    public const int EfKnifeSoundVolume2 = 14;
    public const int EfKnifeSoundVolume3 = 15;
    public const int EfKnifeSoundVolume4 = 16;
    public const int EfKnifeSoundVolume5 = 17;
    public const int EfKnifeSlashAngleOffset = 18;
    public const int EfKnifeStabAngleOffset = 19;
    public const int EfKnifeSlashHeight = 20;
    public const int EfKnifeStabHeight = 21;
    public const int EfKnifeSlashHeightOffset = 22;
    public const int EfKnifeStabHeightOffset = 23;
    public const int MaxKnifeFloatData = 24;

    /* WPN_Get/SetParam 的参数类型(wpn_*) */
    public const int WpnIWeaponId = 0;
    public const int WpnSzWeaponList = 1;
    public const int WpnSzKillSpr = 2;
    public const int WpnIExplodeSprId = 3;
    public const int WpnSzPModel = 4;
    public const int WpnSzVModel = 5;
    public const int WpnSzWModel = 6;
    public const int WpnSzSightModel = 7;
    public const int WpnSzSound1 = 8;
    public const int WpnSzSound2 = 9;
    public const int WpnSzSound3 = 10;
    public const int WpnSzSound4 = 11;
    public const int WpnSzSound5 = 12;
    public const int WpnSzSound6 = 13;
    public const int WpnIReloadMode = 14;
    public const int WpnISightMode = 15;
    public const int WpnIClip = 16;
    public const int WpnIBPAmmo = 17;
    public const int WpnFDamage = 18;
    public const int WpnFStabDamage = 19;
    public const int WpnFFireSpeed = 20;
    public const int WpnFAimFireSpeed = 21;
    public const int WpnFSilencedFireSpeed = 22;
    public const int WpnFRecoil = 23;
    public const int WpnFAimRecoil = 24;
    public const int WpnFSilencedRecoil = 25;
    public const int WpnFAccuracy = 26;
    public const int WpnFAimAccuracy = 27;
    public const int WpnFSilencedAccuracy = 28;
    public const int WpnFSightDistance = 29;
    public const int WpnFSightFramerate = 30;
    public const int WpnFSightOpenTime = 31;
    public const int WpnFSightCloseTime = 32;
    public const int WpnFReloadTime = 33;
    public const int WpnFDrawTime = 34;
    public const int WpnFDetachSilencerTime = 35;
    public const int WpnFAttachSilencerTime = 36;
    public const int WpnFSlashResetTime = 37;
    public const int WpnFSlashTime = 38;
    public const int WpnFSlashRange = 39;
    public const int WpnFSlashAngle = 40;
    public const int WpnFStabResetTime = 41;
    public const int WpnFStabTime = 42;
    public const int WpnFStabRange = 43;
    public const int WpnFStabAngle = 44;
    public const int WpnFExpTime = 45;
    public const int WpnFExpRange = 46;
    public const int WpnFMaxSpeed = 47;
    public const int WpnFAimMaxSpeed = 48;
    public const int WpnFKnockBack = 49;
    public const int WpnSzCommand = 50;
    public const int WpnICost = 51;
    public const int WpnIAmmoCost = 52;
    public const int WpnITeam = 53;
    public const int WpnIWModelBody = 54;
    public const int WpnIAnimIdle = 55;
    public const int WpnIAnimReload = 56;
    public const int WpnIAnimDraw = 57;
    public const int WpnIAnimShoot1 = 58;
    public const int WpnIAnimShoot2 = 59;
    public const int WpnIAnimShoot3 = 60;
    public const int WpnIAnimStartReload = 61;
    public const int WpnIAnimInsert = 62;
    public const int WpnIAnimAfterReload = 63;
    public const int WpnIAnimSightOpen = 64;
    public const int WpnIAnimSightClose = 65;
    public const int WpnIAnimSilencedIdle = 66;
    public const int WpnIAnimSilencedReload = 67;
    public const int WpnIAnimSilencedDraw = 68;
    public const int WpnIAnimSilencedShoot1 = 69;
    public const int WpnIAnimSilencedShoot2 = 70;
    public const int WpnIAnimSilencedShoot3 = 71;
    public const int WpnIAnimDetachSilencer = 72;
    public const int WpnIAnimAttachSilencer = 73;
    public const int WpnIKnifeAnimIdle = 74;
    public const int WpnIKnifeAnimSlash1 = 75;
    public const int WpnIKnifeAnimSlash2 = 76;
    public const int WpnIKnifeAnimDraw = 77;
    public const int WpnIKnifeAnimStab = 78;
    public const int WpnIKnifeAnimStabMiss = 79;
    public const int WpnIKnifeAnimMidSlash = 80;
    public const int WpnIKnifeAnimMidSlashMiss = 81;
    public const int WpnIKnifeAnimSlashBegin = 82;
    public const int WpnIKnifeAnimStabBegin = 83;
    public const int WpnIUser1 = 84;
    public const int WpnIUser2 = 85;
    public const int WpnIUser3 = 86;
    public const int WpnIUser4 = 87;
    public const int WpnFUser1 = 88;
    public const int WpnFUser2 = 89;
    public const int WpnFUser3 = 90;
    public const int WpnFUser4 = 91;
    public const int WpnSzUser1 = 92;
    public const int WpnSzUser2 = 93;
    public const int WpnSzUser3 = 94;
    public const int WpnSzUser4 = 95;
    public const int WpnIAmount = 96;
    public const int WpnID = 97;
    public const int WpnIBloodColor = 98;
    public const int WpnAllTeamBuy = 99;
    public const int WpnSetBuy = 100;
    public const int WpnIUserSight = 101;
    public const int WpnIBotMoney = 102;
    public const int WpnIGameWeaponBPAmmo = 103;
    public const int WpnFDamagePoint = 104;
    public const int WpnFKnockBackPoint = 105;
    public const int WpnICurWeaponBPAmmo = 106;
    public const int WpnIGameCurWeaponBPAmmo = 107;
    public const int WpnIUserMoney = 108;
    public const int WpnIMaxMoney = 109;
    public const int WpnFDamageVictimPoint = 110;
    public const int WpnIAttackEntity = 111;
    public const int WpnIHitGroup = 112;
    public const int WpnFFireSoundVolume = 113;
    public const int WpnFSilenceFireSoundVolume = 114;
    public const int WpnFKnifeSoundVolume1 = 115;
    public const int WpnFKnifeSoundVolume2 = 116;
    public const int WpnFKnifeSoundVolume3 = 117;
    public const int WpnFKnifeSoundVolume4 = 118;
    public const int WpnFKnifeSoundVolume5 = 119;
    public const int WpnFShotgunStartReloadTime = 120;
    public const int WpnFShotgunAfterReloadTime = 121;
    public const int WpnFKnifeSlashAngleOffset = 122;
    public const int WpnFKnifeStabAngleOffset = 123;
    public const int WpnFKnifeSlashHeight = 124;
    public const int WpnFKnifeStabHeight = 125;
    public const int WpnFKnifeSlashHeightOffset = 126;
    public const int WpnFKnifeStabHeightOffset = 127;
}
