using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// 原版 plugin_natives() 注册的 WPN_* native。
// 固定数组使用 AmxxBuffer 描述长度,保持原版 Pawn 的参数数量和顺序。
public static unsafe partial class Natives
{
    private static bool HasLength(int actual, int required) => actual >= required;

    private static bool PrecacheResources(params (string Path, bool Sound)[] resources)
    {
        foreach (var (path, sound) in resources)
        {
            if (string.IsNullOrEmpty(path)) continue;
            if ((sound ? Engine.PrecacheSound(path) : Engine.PrecacheModel(path)) <= 0) return false;
        }
        return true;
    }

    // ---- 注册 ----

    [AmxxNative("WPN_RegisterWeapon")]
    public static int RegisterWeapon(int weaponId, [AmxxBuffer(WpnConst.MaxWeaponIntData)] Span<int> weaponData,
        [AmxxBuffer(WpnConst.MaxWeaponFloatData)] Span<float> weaponFloatData,
        string weaponList, string killSpr, string pModel, string vModel, string wModel, string sightModel,
        string fireSound1, string fireSound2, string silencedFireSound1, string silencedFireSound2, string command)
    {
        if (!WeaponStore.IsGun(weaponId) || !WeaponStore.CanRegister ||
            !HasLength(weaponData.Length, WpnConst.MaxWeaponIntData) ||
            !HasLength(weaponFloatData.Length, WpnConst.MaxWeaponFloatData)) return 0;
        if (!PrecacheResources((pModel, false), (vModel, false), (wModel, false), (sightModel, false),
            (fireSound1, true), (fireSound2, true), (silencedFireSound1, true), (silencedFireSound2, true))) return 0;
        if (!WeaponStore.TryCreateTemplate(out int i)) return 0;

        var weapon = WeaponStore.Config[i];
        weapon.Id = weaponId;
        weapon.ReloadMode = weaponData[WpnConst.EiReloadMode];
        if (weaponId != EngineConst.CswG3sg1 && weaponId != EngineConst.CswSg550 && weaponId != EngineConst.CswAwp && weaponId != EngineConst.CswScout)
            weapon.Sight = weaponData[WpnConst.EiWeaponSight];
        weapon.Clip = weaponData[WpnConst.EiWeaponClip];
        weapon.Ammo = weaponData[WpnConst.EiWeaponAmmo];
        weapon.Cost = weaponData[WpnConst.EiWeaponCost];
        weapon.AmmoCost = weaponData[WpnConst.EiWeaponAmmoCost];
        weapon.Team = weaponData[WpnConst.EiWeaponTeam];
        weapon.WModelBody = weaponData[WpnConst.EiWeaponWModelBody];
        weapon.Damage = weaponFloatData[WpnConst.EfWeaponDamage];
        weapon.Speed = weaponFloatData[WpnConst.EfWeaponSpeed];
        weapon.AimSpeed = weaponFloatData[WpnConst.EfWeaponAimSpeed];
        weapon.SilencedSpeed = weaponFloatData[WpnConst.EfWeaponSilencedSpeed];
        weapon.Recoil = weaponFloatData[WpnConst.EfWeaponRecoil];
        weapon.AimRecoil = weaponFloatData[WpnConst.EfWeaponAimRecoil];
        weapon.SilencedRecoil = weaponFloatData[WpnConst.EfWeaponSilencedRecoil];
        weapon.Accuracy = weaponFloatData[WpnConst.EfWeaponAccuracy];
        weapon.AimAccuracy = weaponFloatData[WpnConst.EfWeaponAimAccuracy];
        weapon.SilencedAccuracy = weaponFloatData[WpnConst.EfWeaponSilencedAccuracy];
        weapon.ReloadTime[0] = weaponFloatData[WpnConst.EfWeaponReloadTime];
        weapon.ReloadTime[1] = weaponFloatData[WpnConst.EfShotgunStartReloadTime];
        weapon.ReloadTime[2] = weaponFloatData[WpnConst.EfShotgunAfterReloadTime];
        weapon.DrawTime = weaponFloatData[WpnConst.EfWeaponDrawTime];
        weapon.MaxSpeed = weaponFloatData[WpnConst.EfWeaponMaxSpeed];
        weapon.AimMaxSpeed = weaponFloatData[WpnConst.EfWeaponAimMaxSpeed];
        weapon.KnockBack = weaponFloatData[WpnConst.EfWeaponKnockBack];
        weapon.SightDistance = weaponFloatData[WpnConst.EfSightDistance];
        weapon.SightFrameRate = weaponFloatData[WpnConst.EfSightFrameRate];
        weapon.SightOpenTime = weaponFloatData[WpnConst.EfSightOpenTime];
        weapon.SightCloseTime = weaponFloatData[WpnConst.EfSightCloseTime];
        weapon.SilencedTime[0] = weaponFloatData[WpnConst.EfWeaponSilencedTime1];
        weapon.SilencedTime[1] = weaponFloatData[WpnConst.EfWeaponSilencedTime2];
        weapon.FireSoundVolume = weaponFloatData[WpnConst.EfWeaponFireSoundVolume];
        weapon.SilenceFireSoundVolume = weaponFloatData[WpnConst.EfWeaponSilenceFireSoundVolume];
        weapon.List = weaponList;
        weapon.Spr = killSpr;
        weapon.PModel = pModel;
        weapon.VModel = vModel;
        weapon.WModel = wModel;
        weapon.SightModel = sightModel;
        weapon.Sound1 = fireSound1;
        weapon.Sound2 = fireSound2;
        weapon.Sound3 = silencedFireSound1;
        weapon.Sound4 = silencedFireSound2;
        weapon.BuyCommand = command;

        if (pModel.Length > 0) Engine.PrecacheModel(pModel);
        if (vModel.Length > 0) Engine.PrecacheModel(vModel);
        if (wModel.Length > 0) Engine.PrecacheModel(wModel);
        if (sightModel.Length > 0) Engine.PrecacheModel(sightModel);
        if (fireSound1.Length > 0) Engine.PrecacheSound(fireSound1);
        if (fireSound2.Length > 0) Engine.PrecacheSound(fireSound2);
        if (silencedFireSound1.Length > 0) Engine.PrecacheSound(silencedFireSound1);
        if (silencedFireSound2.Length > 0) Engine.PrecacheSound(silencedFireSound2);

        WeaponStore.ApplyDefaultSoundVolumes();
        return i;
    }

    [AmxxNative("WPN_SetWeaponAnim")]
    public static void SetWeaponAnim(int weaponIndex, int idle, int reload, int draw,
        [AmxxBuffer(3)] Span<int> shoot, [AmxxBuffer(3)] Span<int> shotgunReload,
        int sightOpen, int sightClose, int detachSilencer, int attachSilencer, int idleSilenced, int reloadSilenced, int drawSilenced,
        [AmxxBuffer(3)] Span<int> silencedShoot)
    {
        if (!WeaponStore.IsTemplate(weaponIndex) || !HasLength(shoot.Length, 3) ||
            !HasLength(shotgunReload.Length, 3) || !HasLength(silencedShoot.Length, 3)) return;
        var weapon = WeaponStore.Config[weaponIndex];
        weapon.Anim[WpnConst.AnimIdle] = idle;
        weapon.Anim[WpnConst.AnimReload] = reload;
        weapon.Anim[WpnConst.AnimDraw] = draw;
        weapon.Anim[WpnConst.AnimShoot1] = shoot[0];
        weapon.Anim[WpnConst.AnimShoot2] = shoot[1];
        weapon.Anim[WpnConst.AnimShoot3] = shoot[2];
        weapon.Anim[WpnConst.AnimStartReload] = shotgunReload[0];
        weapon.Anim[WpnConst.AnimInsert] = shotgunReload[1];
        weapon.Anim[WpnConst.AnimAfterReload] = shotgunReload[2];
        weapon.Anim[WpnConst.AnimSightOpen] = sightOpen;
        weapon.Anim[WpnConst.AnimSightClose] = sightClose;
        weapon.Anim[WpnConst.AnimDetachSilencer] = detachSilencer;
        weapon.Anim[WpnConst.AnimAttachSilencer] = attachSilencer;
        weapon.Anim[WpnConst.AnimIdleSilenced] = idleSilenced;
        weapon.Anim[WpnConst.AnimReloadSilenced] = reloadSilenced;
        weapon.Anim[WpnConst.AnimDrawSilenced] = drawSilenced;
        weapon.Anim[WpnConst.AnimShoot1Silenced] = silencedShoot[0];
        weapon.Anim[WpnConst.AnimShoot2Silenced] = silencedShoot[1];
        weapon.Anim[WpnConst.AnimShoot3Silenced] = silencedShoot[2];
    }

    [AmxxNative("WPN_RegisterKnife")]
    public static int RegisterKnife(string weaponList, string killSpr, string pModel, string vModel,
        string knifeSound1, string knifeSound2, string knifeSound3, string knifeSound4, string knifeSound5, string knifeSound6,
        [AmxxBuffer(WpnConst.MaxKnifeFloatData)] Span<float> knifeData, string command, int cost, int team)
    {
        if (!WeaponStore.CanRegister || !HasLength(knifeData.Length, WpnConst.MaxKnifeFloatData)) return 0;
        if (!PrecacheResources((pModel, false), (vModel, false), (knifeSound1, true), (knifeSound2, true),
            (knifeSound3, true), (knifeSound4, true), (knifeSound5, true), (knifeSound6, true))) return 0;
        if (!WeaponStore.TryCreateTemplate(out int i)) return 0;

        var weapon = WeaponStore.Config[i];
        weapon.Id = EngineConst.CswKnife;
        weapon.KnifeSlashResetTime = knifeData[WpnConst.EfKnifeSlashResetTime];
        weapon.KnifeSlashTime = knifeData[WpnConst.EfKnifeSlashTime];
        weapon.KnifeSlashRange = knifeData[WpnConst.EfKnifeSlashRange];
        weapon.KnifeSlashAngle = knifeData[WpnConst.EfKnifeSlashAngle];
        weapon.KnifeSlashAngleOffset = knifeData[WpnConst.EfKnifeSlashAngleOffset];
        weapon.KnifeSlashHeight = knifeData[WpnConst.EfKnifeSlashHeight];
        weapon.KnifeSlashHeightOffset = knifeData[WpnConst.EfKnifeSlashHeightOffset];
        weapon.KnifeStabResetTime = knifeData[WpnConst.EfKnifeStabResetTime];
        weapon.KnifeStabTime = knifeData[WpnConst.EfKnifeStabTime];
        weapon.KnifeStabRange = knifeData[WpnConst.EfKnifeStabRange];
        weapon.KnifeStabAngle = knifeData[WpnConst.EfKnifeStabAngle];
        weapon.KnifeStabAngleOffset = knifeData[WpnConst.EfKnifeStabAngleOffset];
        weapon.KnifeStabHeight = knifeData[WpnConst.EfKnifeStabHeight];
        weapon.KnifeStabHeightOffset = knifeData[WpnConst.EfKnifeStabHeightOffset];
        weapon.DrawTime = knifeData[WpnConst.EfKnifeDrawTime];
        weapon.Damage = knifeData[WpnConst.EfKnifeDamage];
        weapon.Damage2 = knifeData[WpnConst.EfKnifeDamage2];
        weapon.MaxSpeed = knifeData[WpnConst.EfKnifeMaxSpeed];
        weapon.KnockBack = knifeData[WpnConst.EfKnifeKnockBack];
        weapon.KnifeSoundVolume[0] = knifeData[WpnConst.EfKnifeSoundVolume1];
        weapon.KnifeSoundVolume[1] = knifeData[WpnConst.EfKnifeSoundVolume2];
        weapon.KnifeSoundVolume[2] = knifeData[WpnConst.EfKnifeSoundVolume3];
        weapon.KnifeSoundVolume[3] = knifeData[WpnConst.EfKnifeSoundVolume4];
        weapon.KnifeSoundVolume[4] = knifeData[WpnConst.EfKnifeSoundVolume5];
        weapon.Cost = cost;
        weapon.Team = team;
        weapon.List = weaponList;
        weapon.Spr = killSpr;
        weapon.PModel = pModel;
        weapon.VModel = vModel;
        weapon.Sound1 = knifeSound1;
        weapon.Sound2 = knifeSound2;
        weapon.Sound3 = knifeSound3;
        weapon.Sound4 = knifeSound4;
        weapon.Sound5 = knifeSound5;
        weapon.Sound6 = knifeSound6;
        weapon.BuyCommand = command;

        if (pModel.Length > 0) Engine.PrecacheModel(pModel);
        if (vModel.Length > 0) Engine.PrecacheModel(vModel);
        if (knifeSound1.Length > 0) Engine.PrecacheSound(knifeSound1);
        if (knifeSound2.Length > 0) Engine.PrecacheSound(knifeSound2);
        if (knifeSound3.Length > 0) Engine.PrecacheSound(knifeSound3);
        if (knifeSound4.Length > 0) Engine.PrecacheSound(knifeSound4);
        if (knifeSound5.Length > 0) Engine.PrecacheSound(knifeSound5);
        if (knifeSound6.Length > 0) Engine.PrecacheSound(knifeSound6);
        return i;
    }

    [AmxxNative("WPN_SetKnifeAnim")]
    public static void SetKnifeAnim(int weaponIndex, int idle, int slash1, int slash2, int draw, int stab, int stabMiss, int midSlash, int midSlashMiss,
        int slashBegin, int stabBegin)
    {
        if (!WeaponStore.IsTemplate(weaponIndex)) return;
        var weapon = WeaponStore.Config[weaponIndex];
        weapon.KnifeAnim[WpnConst.KnifeAnimIdle] = idle;
        weapon.KnifeAnim[WpnConst.KnifeAnimSlash1] = slash1;
        weapon.KnifeAnim[WpnConst.KnifeAnimSlash2] = slash2;
        weapon.KnifeAnim[WpnConst.KnifeAnimDraw] = draw;
        weapon.KnifeAnim[WpnConst.KnifeAnimStab] = stab;
        weapon.KnifeAnim[WpnConst.KnifeAnimStabMiss] = stabMiss;
        weapon.KnifeAnim[WpnConst.KnifeAnimMidSlash] = midSlash;
        weapon.KnifeAnim[WpnConst.KnifeAnimMidSlashMiss] = midSlashMiss;
        weapon.KnifeAnim[WpnConst.KnifeAnimSlashBegin] = slashBegin;
        weapon.KnifeAnim[WpnConst.KnifeAnimStabBegin] = stabBegin;
    }

    [AmxxNative("WPN_RegisterGrenade")]
    public static int RegisterGrenade(string weaponList, string killSpr, string explodeSpr, string pModel, string vModel, string wModel,
        string explodeSound, float drawTime, float explodeTime, float range, float maxSpeed, float damage, float explodeSoundVolume,
        string command, int cost, int team, int wModelBody)
    {
        if (!WeaponStore.CanRegister || !PrecacheResources((explodeSpr, false), (pModel, false),
            (vModel, false), (wModel, false), (explodeSound, true))) return 0;
        if (!WeaponStore.TryCreateTemplate(out int i)) return 0;
        int grenadeSprId = 0;
        if (explodeSpr.Length > 0) grenadeSprId = Engine.PrecacheModel(explodeSpr);

        var weapon = WeaponStore.Config[i];
        weapon.Id = EngineConst.CswHegrenade;
        weapon.DrawTime = drawTime;
        weapon.GrenadeData[1] = explodeTime;
        weapon.GrenadeData[2] = range;
        weapon.MaxSpeed = maxSpeed;
        weapon.Damage = damage;
        weapon.FireSoundVolume = explodeSoundVolume;
        weapon.Cost = cost;
        weapon.Team = team;
        weapon.WModelBody = wModelBody;
        weapon.GrenadeSprId = grenadeSprId;
        weapon.List = weaponList;
        weapon.Spr = killSpr;
        weapon.PModel = pModel;
        weapon.VModel = vModel;
        weapon.WModel = wModel;
        weapon.Sound1 = explodeSound;
        weapon.BuyCommand = command;

        if (pModel.Length > 0) Engine.PrecacheModel(pModel);
        if (vModel.Length > 0) Engine.PrecacheModel(vModel);
        if (wModel.Length > 0) Engine.PrecacheModel(wModel);
        if (explodeSound.Length > 0) Engine.PrecacheSound(explodeSound);
        return i;
    }
}
