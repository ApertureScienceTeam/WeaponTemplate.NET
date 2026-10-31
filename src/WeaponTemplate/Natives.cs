using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// 原版 plugin_natives() 注册的 WPN_* native。
// 固定数组使用 AmxxBuffer 描述长度,保持原版 Pawn 的参数数量和顺序。
public static unsafe class Natives
{
    private static bool HasLength(int actual, int required) => actual >= required;

    private static bool PrecacheResources(params (string Path, bool Sound)[] resources)
    {
        foreach (var (path, sound) in resources)
        {
            if (string.IsNullOrEmpty(path)) continue;
            if ((sound ? Eng.PrecacheSound(path) : Eng.PrecacheModel(path)) <= 0) return false;
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
        if (!Store.IsGun(weaponId) || !Store.CanRegister ||
            !HasLength(weaponData.Length, WpnConst.MaxWeaponIntData) ||
            !HasLength(weaponFloatData.Length, WpnConst.MaxWeaponFloatData)) return 0;
        if (!PrecacheResources((pModel, false), (vModel, false), (wModel, false), (sightModel, false),
            (fireSound1, true), (fireSound2, true), (silencedFireSound1, true), (silencedFireSound2, true))) return 0;
        if (!Store.TryCreateTemplate(out int i)) return 0;

        var weapon = Store.Config[i];
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

        if (pModel.Length > 0) Eng.PrecacheModel(pModel);
        if (vModel.Length > 0) Eng.PrecacheModel(vModel);
        if (wModel.Length > 0) Eng.PrecacheModel(wModel);
        if (sightModel.Length > 0) Eng.PrecacheModel(sightModel);
        if (fireSound1.Length > 0) Eng.PrecacheSound(fireSound1);
        if (fireSound2.Length > 0) Eng.PrecacheSound(fireSound2);
        if (silencedFireSound1.Length > 0) Eng.PrecacheSound(silencedFireSound1);
        if (silencedFireSound2.Length > 0) Eng.PrecacheSound(silencedFireSound2);

        Core.ApplyDefaultSoundVolumes();
        return i;
    }

    [AmxxNative("WPN_SetWeaponAnim")]
    public static void SetWeaponAnim(int weaponIndex, int idle, int reload, int draw,
        [AmxxBuffer(3)] Span<int> shoot, [AmxxBuffer(3)] Span<int> shotgunReload,
        int sightOpen, int sightClose, int detachSilencer, int attachSilencer, int idleSilenced, int reloadSilenced, int drawSilenced,
        [AmxxBuffer(3)] Span<int> silencedShoot)
    {
        if (!Store.IsTemplate(weaponIndex) || !HasLength(shoot.Length, 3) ||
            !HasLength(shotgunReload.Length, 3) || !HasLength(silencedShoot.Length, 3)) return;
        var weapon = Store.Config[weaponIndex];
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
        if (!Store.CanRegister || !HasLength(knifeData.Length, WpnConst.MaxKnifeFloatData)) return 0;
        if (!PrecacheResources((pModel, false), (vModel, false), (knifeSound1, true), (knifeSound2, true),
            (knifeSound3, true), (knifeSound4, true), (knifeSound5, true), (knifeSound6, true))) return 0;
        if (!Store.TryCreateTemplate(out int i)) return 0;

        var weapon = Store.Config[i];
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

        if (pModel.Length > 0) Eng.PrecacheModel(pModel);
        if (vModel.Length > 0) Eng.PrecacheModel(vModel);
        if (knifeSound1.Length > 0) Eng.PrecacheSound(knifeSound1);
        if (knifeSound2.Length > 0) Eng.PrecacheSound(knifeSound2);
        if (knifeSound3.Length > 0) Eng.PrecacheSound(knifeSound3);
        if (knifeSound4.Length > 0) Eng.PrecacheSound(knifeSound4);
        if (knifeSound5.Length > 0) Eng.PrecacheSound(knifeSound5);
        if (knifeSound6.Length > 0) Eng.PrecacheSound(knifeSound6);
        return i;
    }

    [AmxxNative("WPN_SetKnifeAnim")]
    public static void SetKnifeAnim(int weaponIndex, int idle, int slash1, int slash2, int draw, int stab, int stabMiss, int midSlash, int midSlashMiss,
        int slashBegin, int stabBegin)
    {
        if (!Store.IsTemplate(weaponIndex)) return;
        var weapon = Store.Config[weaponIndex];
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
        if (!Store.CanRegister || !PrecacheResources((explodeSpr, false), (pModel, false),
            (vModel, false), (wModel, false), (explodeSound, true))) return 0;
        if (!Store.TryCreateTemplate(out int i)) return 0;
        int grenadeSprId = 0;
        if (explodeSpr.Length > 0) grenadeSprId = Eng.PrecacheModel(explodeSpr);

        var weapon = Store.Config[i];
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

        if (pModel.Length > 0) Eng.PrecacheModel(pModel);
        if (vModel.Length > 0) Eng.PrecacheModel(vModel);
        if (wModel.Length > 0) Eng.PrecacheModel(wModel);
        if (explodeSound.Length > 0) Eng.PrecacheSound(explodeSound);
        return i;
    }

    // ---- 发放 / 攻击 / 特效 ----

    [AmxxNative("WPN_GiveWeapon")]
    public static int GiveWeapon(int id, int weaponIndex) => Core.GiveWeaponByTemplate(id, weaponIndex);

    [AmxxNative("WPN_ExecuteAttack")]
    public static int ExecuteAttack(int attacker, int victim, int entity,
        [AmxxBuffer(6)] Span<float> param, [AmxxBuffer(3)] Span<float> origin,
        int damageType, int special)
    {
        if (!HasLength(param.Length, 6) || !HasLength(origin.Length, 3)) return -1;
        // 原版 get_array_f 复制到局部数组;CheckAttack 添加视角高度时不能写回调用方。
        Span<float> attackOrigin = stackalloc float[3];
        origin[..3].CopyTo(attackOrigin);
        return Attack.CheckAttack(attacker, victim, Eng.EntOfIndex(entity), param[..6], attackOrigin, damageType, special);
    }

    [AmxxNative("WPN_SpawnBlood")]
    public static void SpawnBlood([AmxxBuffer(3)] Span<float> origin, int color, int amount)
    {
        if (HasLength(origin.Length, 3)) Core.SpawnBlood(origin[..3], color, amount);
    }

    [AmxxNative("WPN_FakeSmoke")]
    public static void FakeSmoke([AmxxBuffer(3)] Span<float> origin)
    {
        if (HasLength(origin.Length, 3)) Core.FakeSmoke(origin[..3]);
    }

    [AmxxNative("WPN_GiveAmmo")]
    public static void GiveAmmo(int id, int entity, int weaponIndex, int full)
    {
        if (Store.IsPlayerIndex(id) && (weaponIndex == 0 || Store.IsTemplate(weaponIndex)))
            Core.GiveAmmoForWeapon(id, entity, weaponIndex, full);
    }

    // ---- 参数读取 ----

    [AmxxNative("WPN_GetParamInt")]
    public static int GetParamInt(int weaponIndex, int id, int type)
    {
        int i = weaponIndex;
        switch (type)
        {
            case WpnConst.WpnID:
                return id is >= 0 and < 3 ? Store.Config.GetTemplateBySerial(id, i)
                    : Core.WeaponTypeByIndex(id);
            case WpnConst.WpnIAmount: return Store.WeaponAmount;
            case WpnConst.WpnIBloodColor: return Store.BloodColor;
            case WpnConst.WpnAllTeamBuy: return Store.AllTeamBuy;
            case WpnConst.WpnIMaxMoney: return Store.MaxMoney;
            case WpnConst.WpnIGameWeaponBPAmmo:
                return i >= 0 && i < Store.GameWeaponMaxBpammo.Length ? Store.GameWeaponMaxBpammo[i] : -1;
        }
        if (!Store.IsPlayerIndex(id)) return -1;
        switch (type)
        {
            case WpnConst.WpnSetBuy: return Store.Player(id).BlockBuy;
            case WpnConst.WpnIUserSight: return Store.Player(id).SightMode;
            case WpnConst.WpnIBotMoney: return Store.Player(id).BotMoney;
            case WpnConst.WpnIUserMoney: return Core.GetUserMoney(id);
            case WpnConst.WpnIAttackEntity: return Store.Player(id).AttackerEntity;
            case WpnConst.WpnIGameCurWeaponBPAmmo:
                return i >= 0 && i < Store.Player(id).GameBpAmmo.Length ? Store.Player(id).GameBpAmmo[i] : -1;
        }
        if (!Store.IsTemplate(i)) return -1;
        switch (type)
        {
            case WpnConst.WpnIWeaponId: return Store.Weapon(id, i).Id;
            case WpnConst.WpnIExplodeSprId: return Store.Weapon(id, i).GrenadeSprId;
            case WpnConst.WpnIReloadMode: return Store.Weapon(id, i).ReloadMode;
            case WpnConst.WpnISightMode: return Store.Weapon(id, i).Sight;
            case WpnConst.WpnIClip: return Store.Weapon(id, i).Clip;
            case WpnConst.WpnIBPAmmo: return Store.Weapon(id, i).Ammo;
            case WpnConst.WpnICost: return Store.Weapon(id, i).Cost;
            case WpnConst.WpnIAmmoCost: return Store.Weapon(id, i).AmmoCost;
            case WpnConst.WpnITeam: return Store.Weapon(id, i).Team;
            case WpnConst.WpnIWModelBody: return Store.Weapon(id, i).WModelBody;
            case WpnConst.WpnIAnimIdle: return Store.Weapon(id, i).Anim[WpnConst.AnimIdle];
            case WpnConst.WpnIAnimReload: return Store.Weapon(id, i).Anim[WpnConst.AnimReload];
            case WpnConst.WpnIAnimDraw: return Store.Weapon(id, i).Anim[WpnConst.AnimDraw];
            case WpnConst.WpnIAnimShoot1: return Store.Weapon(id, i).Anim[WpnConst.AnimShoot1];
            case WpnConst.WpnIAnimShoot2: return Store.Weapon(id, i).Anim[WpnConst.AnimShoot2];
            case WpnConst.WpnIAnimShoot3: return Store.Weapon(id, i).Anim[WpnConst.AnimShoot3];
            case WpnConst.WpnIAnimStartReload: return Store.Weapon(id, i).Anim[WpnConst.AnimStartReload];
            case WpnConst.WpnIAnimInsert: return Store.Weapon(id, i).Anim[WpnConst.AnimInsert];
            case WpnConst.WpnIAnimAfterReload: return Store.Weapon(id, i).Anim[WpnConst.AnimAfterReload];
            case WpnConst.WpnIAnimSightOpen: return Store.Weapon(id, i).Anim[WpnConst.AnimSightOpen];
            case WpnConst.WpnIAnimSightClose: return Store.Weapon(id, i).Anim[WpnConst.AnimSightClose];
            case WpnConst.WpnIAnimSilencedIdle: return Store.Weapon(id, i).Anim[WpnConst.AnimIdleSilenced];
            case WpnConst.WpnIAnimSilencedReload: return Store.Weapon(id, i).Anim[WpnConst.AnimReloadSilenced];
            case WpnConst.WpnIAnimSilencedDraw: return Store.Weapon(id, i).Anim[WpnConst.AnimDrawSilenced];
            case WpnConst.WpnIAnimSilencedShoot1: return Store.Weapon(id, i).Anim[WpnConst.AnimShoot1Silenced];
            case WpnConst.WpnIAnimSilencedShoot2: return Store.Weapon(id, i).Anim[WpnConst.AnimShoot2Silenced];
            case WpnConst.WpnIAnimSilencedShoot3: return Store.Weapon(id, i).Anim[WpnConst.AnimShoot3Silenced];
            case WpnConst.WpnIAnimDetachSilencer: return Store.Weapon(id, i).Anim[WpnConst.AnimDetachSilencer];
            case WpnConst.WpnIAnimAttachSilencer: return Store.Weapon(id, i).Anim[WpnConst.AnimAttachSilencer];
            case WpnConst.WpnIKnifeAnimIdle: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimIdle];
            case WpnConst.WpnIKnifeAnimSlash1: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash1];
            case WpnConst.WpnIKnifeAnimSlash2: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash2];
            case WpnConst.WpnIKnifeAnimDraw: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw];
            case WpnConst.WpnIKnifeAnimStab: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab];
            case WpnConst.WpnIKnifeAnimStabMiss: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabMiss];
            case WpnConst.WpnIKnifeAnimMidSlash: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlash];
            case WpnConst.WpnIKnifeAnimMidSlashMiss: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlashMiss];
            case WpnConst.WpnIKnifeAnimSlashBegin: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin];
            case WpnConst.WpnIKnifeAnimStabBegin: return Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin];
            case WpnConst.WpnIUser1: return Store.Player(id).User1[i];
            case WpnConst.WpnIUser2: return Store.Player(id).User2[i];
            case WpnConst.WpnIUser3: return Store.Player(id).User3[i];
            case WpnConst.WpnIUser4: return Store.Player(id).User4[i];
            case WpnConst.WpnIAmount: return Store.WeaponAmount;
            case WpnConst.WpnID:
                if (id < 3) return Store.Config.GetTemplateBySerial(id, i);
                return Core.WeaponTypeByIndex(id);
            case WpnConst.WpnIBloodColor: return Store.BloodColor;
            case WpnConst.WpnAllTeamBuy: return Store.AllTeamBuy;
            case WpnConst.WpnSetBuy: return Store.Player(id).BlockBuy;
            case WpnConst.WpnIUserSight: return Store.Player(id).SightMode;
            case WpnConst.WpnIBotMoney: return Store.Player(id).BotMoney;
            case WpnConst.WpnIGameWeaponBPAmmo: return Store.GameWeaponMaxBpammo[i];
            case WpnConst.WpnIGameCurWeaponBPAmmo: return Store.Player(id).GameBpAmmo[i];
            case WpnConst.WpnICurWeaponBPAmmo: return Store.Player(id).BPAmmo[i];
            case WpnConst.WpnIMaxMoney: return Store.MaxMoney;
            case WpnConst.WpnIUserMoney: return Core.GetUserMoney(id);
            case WpnConst.WpnIAttackEntity: return Store.Player(id).AttackerEntity;
        }
        return -1;
    }

    [AmxxNative("WPN_GetParamFloat")]
    public static float GetParamFloat(int weaponIndex, int id, int type)
    {
        if (!Store.IsPlayerIndex(id)) return -1.0f;
        switch (type)
        {
            case WpnConst.WpnFDamagePoint: return Store.Player(id).DamagePoint;
            case WpnConst.WpnFKnockBackPoint: return Store.Player(id).KnockBackPoint;
            case WpnConst.WpnFDamageVictimPoint: return Store.Player(id).DamageVictimPoint;
        }
        int i = weaponIndex;
        if (!Store.IsTemplate(i)) return -1.0f;

        switch (type)
        {
            case WpnConst.WpnFDamage: return Store.Weapon(id, i).Damage;
            case WpnConst.WpnFStabDamage: return Store.Weapon(id, i).Damage2;
            case WpnConst.WpnFFireSpeed: return Store.Weapon(id, i).Speed;
            case WpnConst.WpnFAimFireSpeed: return Store.Weapon(id, i).AimSpeed;
            case WpnConst.WpnFSilencedFireSpeed: return Store.Weapon(id, i).SilencedSpeed;
            case WpnConst.WpnFRecoil: return Store.Weapon(id, i).Recoil;
            case WpnConst.WpnFAimRecoil: return Store.Weapon(id, i).AimRecoil;
            case WpnConst.WpnFSilencedRecoil: return Store.Weapon(id, i).SilencedRecoil;
            case WpnConst.WpnFAccuracy: return Store.Weapon(id, i).Accuracy;
            case WpnConst.WpnFAimAccuracy: return Store.Weapon(id, i).AimAccuracy;
            case WpnConst.WpnFSilencedAccuracy: return Store.Weapon(id, i).SilencedAccuracy;
            case WpnConst.WpnFSightDistance: return Store.Weapon(id, i).SightDistance;
            case WpnConst.WpnFSightFramerate: return Store.Weapon(id, i).SightFrameRate;
            case WpnConst.WpnFSightOpenTime: return Store.Weapon(id, i).SightOpenTime;
            case WpnConst.WpnFSightCloseTime: return Store.Weapon(id, i).SightCloseTime;
            case WpnConst.WpnFReloadTime: return Store.Weapon(id, i).ReloadTime[0];
            case WpnConst.WpnFShotgunStartReloadTime: return Store.Weapon(id, i).ReloadTime[1];
            case WpnConst.WpnFShotgunAfterReloadTime: return Store.Weapon(id, i).ReloadTime[2];
            case WpnConst.WpnFDrawTime: return Store.Weapon(id, i).DrawTime;
            case WpnConst.WpnFDetachSilencerTime: return Store.Weapon(id, i).SilencedTime[1];
            case WpnConst.WpnFAttachSilencerTime: return Store.Weapon(id, i).SilencedTime[0];
            case WpnConst.WpnFSlashResetTime: return Store.Weapon(id, i).KnifeSlashResetTime;
            case WpnConst.WpnFSlashTime: return Store.Weapon(id, i).KnifeSlashTime;
            case WpnConst.WpnFSlashRange: return Store.Weapon(id, i).KnifeSlashRange;
            case WpnConst.WpnFSlashAngle: return Store.Weapon(id, i).KnifeSlashAngle;
            case WpnConst.WpnFStabResetTime: return Store.Weapon(id, i).KnifeStabResetTime;
            case WpnConst.WpnFStabTime: return Store.Weapon(id, i).KnifeStabTime;
            case WpnConst.WpnFStabRange: return Store.Weapon(id, i).KnifeStabRange;
            case WpnConst.WpnFStabAngle: return Store.Weapon(id, i).KnifeStabAngle;
            case WpnConst.WpnFExpTime: return Store.Weapon(id, i).GrenadeData[1];
            case WpnConst.WpnFExpRange: return Store.Weapon(id, i).GrenadeData[2];
            case WpnConst.WpnFMaxSpeed: return Store.Weapon(id, i).MaxSpeed;
            case WpnConst.WpnFAimMaxSpeed: return Store.Weapon(id, i).AimMaxSpeed;
            case WpnConst.WpnFKnockBack: return Store.Weapon(id, i).KnockBack;
            case WpnConst.WpnFUser1: return Store.Player(id).FUser1[i];
            case WpnConst.WpnFUser2: return Store.Player(id).FUser2[i];
            case WpnConst.WpnFUser3: return Store.Player(id).FUser3[i];
            case WpnConst.WpnFUser4: return Store.Player(id).FUser4[i];
            case WpnConst.WpnFDamagePoint: return Store.Player(id).DamagePoint;
            case WpnConst.WpnFKnockBackPoint: return Store.Player(id).KnockBackPoint;
            case WpnConst.WpnFDamageVictimPoint: return Store.Player(id).DamageVictimPoint;
            case WpnConst.WpnFFireSoundVolume: return Store.Weapon(id, i).FireSoundVolume;
            case WpnConst.WpnFSilenceFireSoundVolume: return Store.Weapon(id, i).SilenceFireSoundVolume;
            case WpnConst.WpnFKnifeSoundVolume1: return Store.Weapon(id, i).KnifeSoundVolume[0];
            case WpnConst.WpnFKnifeSoundVolume2: return Store.Weapon(id, i).KnifeSoundVolume[1];
            case WpnConst.WpnFKnifeSoundVolume3: return Store.Weapon(id, i).KnifeSoundVolume[2];
            case WpnConst.WpnFKnifeSoundVolume4: return Store.Weapon(id, i).KnifeSoundVolume[3];
            case WpnConst.WpnFKnifeSoundVolume5: return Store.Weapon(id, i).KnifeSoundVolume[4];
            case WpnConst.WpnFKnifeSlashAngleOffset: return Store.Weapon(id, i).KnifeSlashAngleOffset;
            case WpnConst.WpnFKnifeStabAngleOffset: return Store.Weapon(id, i).KnifeStabAngleOffset;
            case WpnConst.WpnFKnifeSlashHeight: return Store.Weapon(id, i).KnifeSlashHeight;
            case WpnConst.WpnFKnifeStabHeight: return Store.Weapon(id, i).KnifeStabHeight;
            case WpnConst.WpnFKnifeSlashHeightOffset: return Store.Weapon(id, i).KnifeSlashHeightOffset;
            case WpnConst.WpnFKnifeStabHeightOffset: return Store.Weapon(id, i).KnifeStabHeightOffset;
        }
        return -1.0f;
    }

    [AmxxNative("WPN_GetParamString")]
    public static void GetParamString(int weaponIndex, int id, int type, Span<int> output, int len)
    {
        if (len < 0) return;
        GetParamStringValue(weaponIndex, id, type, out string value, len);
        using var encoded = value.GetNativeString();
        sbyte* source = encoded;
        // Pawn passes charsmax: len writable bytes plus one NUL cell. The generated Span
        // covers len cells; keep its original address even when len == 0.
        ref int first = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(output);
        fixed (int* target = &first)
        {
            if (target == null) return;
            int count = 0;
            int limit = Math.Min(len, output.Length);
            while (count < limit && source[count] != 0) { target[count] = (byte)source[count]; count++; }
            target[count] = 0;
        }
    }

    public static void GetParamStringValue(int weaponIndex, int id, int type, out string value, int len)
    {
        value = "";
        int i = weaponIndex;
        if (!Store.IsPlayerIndex(id) || !Store.IsTemplate(i)) return;

        switch (type)
        {
            case WpnConst.WpnSzWeaponList: value = Store.Weapon(id, i).List; break;
            case WpnConst.WpnSzKillSpr: value = Store.Weapon(id, i).Spr; break;
            case WpnConst.WpnSzPModel: value = Store.Weapon(id, i).PModel; break;
            case WpnConst.WpnSzVModel: value = Store.Weapon(id, i).VModel; break;
            case WpnConst.WpnSzWModel: value = Store.Weapon(id, i).WModel; break;
            case WpnConst.WpnSzSightModel: value = Store.Weapon(id, i).SightModel; break;
            case WpnConst.WpnSzSound1: value = Store.Weapon(id, i).Sound1; break;
            case WpnConst.WpnSzSound2: value = Store.Weapon(id, i).Sound2; break;
            case WpnConst.WpnSzSound3: value = Store.Weapon(id, i).Sound3; break;
            case WpnConst.WpnSzSound4: value = Store.Weapon(id, i).Sound4; break;
            case WpnConst.WpnSzSound5: value = Store.Weapon(id, i).Sound5; break;
            case WpnConst.WpnSzSound6: value = Store.Weapon(id, i).Sound6; break;
            case WpnConst.WpnSzCommand: value = Store.Weapon(id, i).BuyCommand; break;
            case WpnConst.WpnSzUser1: value = Store.Player(id).SzUser1[i]; break;
            case WpnConst.WpnSzUser2: value = Store.Player(id).SzUser2[i]; break;
            case WpnConst.WpnSzUser3: value = Store.Player(id).SzUser3[i]; break;
            case WpnConst.WpnSzUser4: value = Store.Player(id).SzUser4[i]; break;
        }
    }

    // ---- 参数写入 ----

    [AmxxNative("WPN_SetParamInt")]
    public static void SetParamInt(int weaponIndex, int id, int type, int value)
    {
        int i = weaponIndex;
        switch (type)
        {
            case WpnConst.WpnIBloodColor: Store.BloodColor = value; return;
            case WpnConst.WpnAllTeamBuy: Store.AllTeamBuy = value; return;
            case WpnConst.WpnIMaxMoney: Store.MaxMoney = Math.Max(0, value); return;
            case WpnConst.WpnIGameWeaponBPAmmo:
                if (i >= 0 && i < Store.GameWeaponMaxBpammo.Length) Store.GameWeaponMaxBpammo[i] = value;
                return;
        }
        if (!Store.IsPlayerIndex(id)) return;
        switch (type)
        {
            case WpnConst.WpnSetBuy: Store.Player(id).BlockBuy = value; return;
            case WpnConst.WpnIBotMoney: Store.Player(id).BotMoney = value; return;
            case WpnConst.WpnIAttackEntity: Store.Player(id).AttackerEntity = value; return;
            case WpnConst.WpnIUserMoney: Core.SetUserMoney(id, value, weaponIndex); return;
            case WpnConst.WpnIHitGroup: Store.Player(id).AttackHitGroup = value; return;
            case WpnConst.WpnIGameCurWeaponBPAmmo:
                if (i >= 0 && i < Store.Player(id).GameBpAmmo.Length) Store.Player(id).GameBpAmmo[i] = value;
                return;
        }
        if (!Store.IsTemplate(i)) return;

        switch (type)
        {
            case WpnConst.WpnIWeaponId: Store.WriteWeapon(id, i).Id = value; break;
            case WpnConst.WpnIExplodeSprId: Store.WriteWeapon(id, i).GrenadeSprId = value; break;
            case WpnConst.WpnIReloadMode: Store.WriteWeapon(id, i).ReloadMode = value; break;
            case WpnConst.WpnISightMode: Store.WriteWeapon(id, i).Sight = value; break;
            case WpnConst.WpnIClip: Store.WriteWeapon(id, i).Clip = value; break;
            case WpnConst.WpnIBPAmmo: Store.WriteWeapon(id, i).Ammo = value; break;
            case WpnConst.WpnICost: Store.WriteWeapon(id, i).Cost = value; break;
            case WpnConst.WpnIAmmoCost: Store.WriteWeapon(id, i).AmmoCost = value; break;
            case WpnConst.WpnITeam: Store.WriteWeapon(id, i).Team = value; break;
            case WpnConst.WpnIWModelBody: Store.WriteWeapon(id, i).WModelBody = value; break;
            case WpnConst.WpnIAnimIdle: Store.WriteWeapon(id, i).Anim[WpnConst.AnimIdle] = value; break;
            case WpnConst.WpnIAnimReload: Store.WriteWeapon(id, i).Anim[WpnConst.AnimReload] = value; break;
            case WpnConst.WpnIAnimDraw: Store.WriteWeapon(id, i).Anim[WpnConst.AnimDraw] = value; break;
            case WpnConst.WpnIAnimShoot1: Store.WriteWeapon(id, i).Anim[WpnConst.AnimShoot1] = value; break;
            case WpnConst.WpnIAnimShoot2: Store.WriteWeapon(id, i).Anim[WpnConst.AnimShoot2] = value; break;
            case WpnConst.WpnIAnimShoot3: Store.WriteWeapon(id, i).Anim[WpnConst.AnimShoot3] = value; break;
            case WpnConst.WpnIAnimStartReload: Store.WriteWeapon(id, i).Anim[WpnConst.AnimStartReload] = value; break;
            case WpnConst.WpnIAnimInsert: Store.WriteWeapon(id, i).Anim[WpnConst.AnimInsert] = value; break;
            case WpnConst.WpnIAnimAfterReload: Store.WriteWeapon(id, i).Anim[WpnConst.AnimAfterReload] = value; break;
            case WpnConst.WpnIAnimSightOpen: Store.WriteWeapon(id, i).Anim[WpnConst.AnimSightOpen] = value; break;
            case WpnConst.WpnIAnimSightClose: Store.WriteWeapon(id, i).Anim[WpnConst.AnimSightClose] = value; break;
            case WpnConst.WpnIAnimSilencedIdle: Store.WriteWeapon(id, i).Anim[WpnConst.AnimIdleSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedReload: Store.WriteWeapon(id, i).Anim[WpnConst.AnimReloadSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedDraw: Store.WriteWeapon(id, i).Anim[WpnConst.AnimDrawSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot1: Store.WriteWeapon(id, i).Anim[WpnConst.AnimShoot1Silenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot2: Store.WriteWeapon(id, i).Anim[WpnConst.AnimShoot2Silenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot3: Store.WriteWeapon(id, i).Anim[WpnConst.AnimShoot3Silenced] = value; break;
            case WpnConst.WpnIAnimDetachSilencer: Store.WriteWeapon(id, i).Anim[WpnConst.AnimDetachSilencer] = value; break;
            case WpnConst.WpnIAnimAttachSilencer: Store.WriteWeapon(id, i).Anim[WpnConst.AnimAttachSilencer] = value; break;
            case WpnConst.WpnIKnifeAnimIdle: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimIdle] = value; break;
            case WpnConst.WpnIKnifeAnimSlash1: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash1] = value; break;
            case WpnConst.WpnIKnifeAnimSlash2: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash2] = value; break;
            case WpnConst.WpnIKnifeAnimDraw: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw] = value; break;
            case WpnConst.WpnIKnifeAnimStab: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab] = value; break;
            case WpnConst.WpnIKnifeAnimStabMiss: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabMiss] = value; break;
            case WpnConst.WpnIKnifeAnimMidSlash: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlash] = value; break;
            case WpnConst.WpnIKnifeAnimMidSlashMiss: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlashMiss] = value; break;
            case WpnConst.WpnIKnifeAnimSlashBegin: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] = value; break;
            case WpnConst.WpnIKnifeAnimStabBegin: Store.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin] = value; break;
            case WpnConst.WpnIUser1: Store.Player(id).User1[i] = value; break;
            case WpnConst.WpnIUser2: Store.Player(id).User2[i] = value; break;
            case WpnConst.WpnIUser3: Store.Player(id).User3[i] = value; break;
            case WpnConst.WpnIUser4: Store.Player(id).User4[i] = value; break;
            case WpnConst.WpnICurWeaponBPAmmo: Store.Player(id).BPAmmo[i] = value; break;
        }
    }

    [AmxxNative("WPN_SetParamFloat")]
    public static void SetParamFloat(int weaponIndex, int id, int type, float value)
    {
        if (!Store.IsPlayerIndex(id) || !float.IsFinite(value)) return;
        switch (type)
        {
            case WpnConst.WpnFDamagePoint: Store.Player(id).DamagePoint = value; return;
            case WpnConst.WpnFKnockBackPoint: Store.Player(id).KnockBackPoint = value; return;
            case WpnConst.WpnFDamageVictimPoint: Store.Player(id).DamageVictimPoint = value; return;
        }
        int i = weaponIndex;
        if (!Store.IsTemplate(i) || !float.IsFinite(value)) return;

        switch (type)
        {
            case WpnConst.WpnFDamage: Store.WriteWeapon(id, i).Damage = value; break;
            case WpnConst.WpnFStabDamage: Store.WriteWeapon(id, i).Damage2 = value; break;
            case WpnConst.WpnFFireSpeed: Store.WriteWeapon(id, i).Speed = value; break;
            case WpnConst.WpnFAimFireSpeed: Store.WriteWeapon(id, i).AimSpeed = value; break;
            case WpnConst.WpnFSilencedFireSpeed: Store.WriteWeapon(id, i).SilencedSpeed = value; break;
            case WpnConst.WpnFRecoil: Store.WriteWeapon(id, i).Recoil = value; break;
            case WpnConst.WpnFAimRecoil: Store.WriteWeapon(id, i).AimRecoil = value; break;
            case WpnConst.WpnFSilencedRecoil: Store.WriteWeapon(id, i).SilencedRecoil = value; break;
            case WpnConst.WpnFAccuracy: Store.WriteWeapon(id, i).Accuracy = value; break;
            case WpnConst.WpnFAimAccuracy: Store.WriteWeapon(id, i).AimAccuracy = value; break;
            case WpnConst.WpnFSilencedAccuracy: Store.WriteWeapon(id, i).SilencedAccuracy = value; break;
            case WpnConst.WpnFSightDistance: Store.WriteWeapon(id, i).SightDistance = value; break;
            case WpnConst.WpnFSightFramerate: Store.WriteWeapon(id, i).SightFrameRate = value; break;
            case WpnConst.WpnFSightOpenTime: Store.WriteWeapon(id, i).SightOpenTime = value; break;
            case WpnConst.WpnFSightCloseTime: Store.WriteWeapon(id, i).SightCloseTime = value; break;
            case WpnConst.WpnFReloadTime: Store.WriteWeapon(id, i).ReloadTime[0] = value; break;
            case WpnConst.WpnFShotgunStartReloadTime: Store.WriteWeapon(id, i).ReloadTime[1] = value; break;
            case WpnConst.WpnFShotgunAfterReloadTime: Store.WriteWeapon(id, i).ReloadTime[2] = value; break;
            case WpnConst.WpnFDrawTime: Store.WriteWeapon(id, i).DrawTime = value; break;
            case WpnConst.WpnFDetachSilencerTime: Store.WriteWeapon(id, i).SilencedTime[1] = value; break;
            case WpnConst.WpnFAttachSilencerTime: Store.WriteWeapon(id, i).SilencedTime[0] = value; break;
            case WpnConst.WpnFSlashResetTime: Store.WriteWeapon(id, i).KnifeSlashResetTime = value; break;
            case WpnConst.WpnFSlashTime: Store.WriteWeapon(id, i).KnifeSlashTime = value; break;
            case WpnConst.WpnFSlashRange: Store.WriteWeapon(id, i).KnifeSlashRange = value; break;
            case WpnConst.WpnFSlashAngle: Store.WriteWeapon(id, i).KnifeSlashAngle = value; break;
            case WpnConst.WpnFStabResetTime: Store.WriteWeapon(id, i).KnifeStabResetTime = value; break;
            case WpnConst.WpnFStabTime: Store.WriteWeapon(id, i).KnifeStabTime = value; break;
            case WpnConst.WpnFStabRange: Store.WriteWeapon(id, i).KnifeStabRange = value; break;
            case WpnConst.WpnFStabAngle: Store.WriteWeapon(id, i).KnifeStabAngle = value; break;
            case WpnConst.WpnFExpTime: Store.WriteWeapon(id, i).GrenadeData[1] = value; break;
            case WpnConst.WpnFExpRange: Store.WriteWeapon(id, i).GrenadeData[2] = value; break;
            case WpnConst.WpnFMaxSpeed: Store.WriteWeapon(id, i).MaxSpeed = value; break;
            case WpnConst.WpnFAimMaxSpeed: Store.WriteWeapon(id, i).AimMaxSpeed = value; break;
            case WpnConst.WpnFKnockBack: Store.WriteWeapon(id, i).KnockBack = value; break;
            case WpnConst.WpnFUser1: Store.Player(id).FUser1[i] = value; break;
            case WpnConst.WpnFUser2: Store.Player(id).FUser2[i] = value; break;
            case WpnConst.WpnFUser3: Store.Player(id).FUser3[i] = value; break;
            case WpnConst.WpnFUser4: Store.Player(id).FUser4[i] = value; break;
            case WpnConst.WpnFFireSoundVolume: Store.WriteWeapon(id, i).FireSoundVolume = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : 0.35f; break;
            case WpnConst.WpnFSilenceFireSoundVolume: Store.WriteWeapon(id, i).SilenceFireSoundVolume = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume1: Store.WriteWeapon(id, i).KnifeSoundVolume[0] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume2: Store.WriteWeapon(id, i).KnifeSoundVolume[1] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume3: Store.WriteWeapon(id, i).KnifeSoundVolume[2] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume4: Store.WriteWeapon(id, i).KnifeSoundVolume[3] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume5: Store.WriteWeapon(id, i).KnifeSoundVolume[4] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSlashAngleOffset: Store.WriteWeapon(id, i).KnifeSlashAngleOffset = value; break;
            case WpnConst.WpnFKnifeStabAngleOffset: Store.WriteWeapon(id, i).KnifeStabAngleOffset = value; break;
            case WpnConst.WpnFKnifeSlashHeight: Store.WriteWeapon(id, i).KnifeSlashHeight = value; break;
            case WpnConst.WpnFKnifeStabHeight: Store.WriteWeapon(id, i).KnifeStabHeight = value; break;
            case WpnConst.WpnFKnifeSlashHeightOffset: Store.WriteWeapon(id, i).KnifeSlashHeightOffset = value; break;
            case WpnConst.WpnFKnifeStabHeightOffset: Store.WriteWeapon(id, i).KnifeStabHeightOffset = value; break;
        }
    }

    [AmxxNative("WPN_SetParamString")]
    public static void SetParamString(int weaponIndex, int id, int type, string value)
    {
        int i = weaponIndex;
        if (!Store.IsPlayerIndex(id) || !Store.IsTemplate(i)) return;
        value ??= "";
        if (value.Length > 0 && type is >= WpnConst.WpnSzPModel and <= WpnConst.WpnSzSound6 &&
            !EngineResources.Exists(value, type >= WpnConst.WpnSzSound1)) return;

        switch (type)
        {
            case WpnConst.WpnSzWeaponList: Store.WriteWeapon(id, i).List = value; break;
            case WpnConst.WpnSzKillSpr: Store.WriteWeapon(id, i).Spr = value; break;
            case WpnConst.WpnSzPModel: Store.WriteWeapon(id, i).PModel = value; break;
            case WpnConst.WpnSzVModel: Store.WriteWeapon(id, i).VModel = value; break;
            case WpnConst.WpnSzWModel: Store.WriteWeapon(id, i).WModel = value; break;
            case WpnConst.WpnSzSightModel: Store.WriteWeapon(id, i).SightModel = value; break;
            case WpnConst.WpnSzSound1: Store.WriteWeapon(id, i).Sound1 = value; break;
            case WpnConst.WpnSzSound2: Store.WriteWeapon(id, i).Sound2 = value; break;
            case WpnConst.WpnSzSound3: Store.WriteWeapon(id, i).Sound3 = value; break;
            case WpnConst.WpnSzSound4: Store.WriteWeapon(id, i).Sound4 = value; break;
            case WpnConst.WpnSzSound5: Store.WriteWeapon(id, i).Sound5 = value; break;
            case WpnConst.WpnSzSound6: Store.WriteWeapon(id, i).Sound6 = value; break;
            case WpnConst.WpnSzCommand: Store.WriteWeapon(id, i).BuyCommand = value; break;
            case WpnConst.WpnSzUser1: Store.Player(id).SzUser1[i] = value; break;
            case WpnConst.WpnSzUser2: Store.Player(id).SzUser2[i] = value; break;
            case WpnConst.WpnSzUser3: Store.Player(id).SzUser3[i] = value; break;
            case WpnConst.WpnSzUser4: Store.Player(id).SzUser4[i] = value; break;
        }
    }
}
