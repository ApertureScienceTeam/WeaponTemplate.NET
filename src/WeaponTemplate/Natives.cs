using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// 原版 plugin_natives() 注册的 WPN_* native。
// 与 Pawn 版的 ABI 差异:固定数组参数改为 (arr[], len) 约定(生成器要求 Span 后紧跟 int 长度)。
public static unsafe class Natives
{
    // ---- 注册 ----

    [AmxxNative]
    public static int RegisterWeapon(int weaponId, Span<int> weaponData, int weaponDataLen, Span<float> weaponFloatData, int weaponFloatDataLen,
        string weaponList, string killSpr, string pModel, string vModel, string wModel, string sightModel,
        string fireSound1, string fireSound2, string silencedFireSound1, string silencedFireSound2, string command)
    {
        if (weaponId is EngineConst.CswKnife or EngineConst.CswHegrenade or EngineConst.CswFlashbang or EngineConst.CswSmokegrenade
            or EngineConst.CswC4 or EngineConst.CswVest or EngineConst.CswVesthelm)
            return 0;

        Store.WeaponAmount++;
        int i = Store.WeaponAmount;

        for (int id = 1; id < 33; id++)
        {
            Store.WeaponId[id, i] = weaponId;
            Store.ReloadMode[id, i] = weaponData[WpnConst.EiReloadMode];
            if (weaponId != EngineConst.CswG3sg1 && weaponId != EngineConst.CswSg550 && weaponId != EngineConst.CswAwp && weaponId != EngineConst.CswScout)
                Store.WeaponSight[id, i] = weaponData[WpnConst.EiWeaponSight];
            Store.WeaponClip[id, i] = weaponData[WpnConst.EiWeaponClip];
            Store.WeaponAmmo[id, i] = weaponData[WpnConst.EiWeaponAmmo];
            Store.WeaponCost[id, i] = weaponData[WpnConst.EiWeaponCost];
            Store.WeaponAmmoCost[id, i] = weaponData[WpnConst.EiWeaponAmmoCost];
            Store.WeaponTeam[id, i] = weaponData[WpnConst.EiWeaponTeam];
            Store.WeaponWModelBody[id, i] = weaponData[WpnConst.EiWeaponWModelBody];
            Store.WeaponDamage[id, i] = weaponFloatData[WpnConst.EfWeaponDamage];
            Store.WeaponSpeed[id, i] = weaponFloatData[WpnConst.EfWeaponSpeed];
            Store.WeaponAimSpeed[id, i] = weaponFloatData[WpnConst.EfWeaponAimSpeed];
            Store.WeaponSilencedSpeed[id, i] = weaponFloatData[WpnConst.EfWeaponSilencedSpeed];
            Store.WeaponRecoil[id, i] = weaponFloatData[WpnConst.EfWeaponRecoil];
            Store.WeaponAimRecoil[id, i] = weaponFloatData[WpnConst.EfWeaponAimRecoil];
            Store.WeaponSilencedRecoil[id, i] = weaponFloatData[WpnConst.EfWeaponSilencedRecoil];
            Store.WeaponAccuracy[id, i] = weaponFloatData[WpnConst.EfWeaponAccuracy];
            Store.WeaponAimAccuracy[id, i] = weaponFloatData[WpnConst.EfWeaponAimAccuracy];
            Store.WeaponSilencedAccuracy[id, i] = weaponFloatData[WpnConst.EfWeaponSilencedAccuracy];
            Store.WeaponReloadTime[id, i, 0] = weaponFloatData[WpnConst.EfWeaponReloadTime];
            Store.WeaponReloadTime[id, i, 1] = weaponFloatData[WpnConst.EfShotgunStartReloadTime];
            Store.WeaponReloadTime[id, i, 2] = weaponFloatData[WpnConst.EfShotgunAfterReloadTime];
            Store.WeaponDrawTime[id, i] = weaponFloatData[WpnConst.EfWeaponDrawTime];
            Store.WeaponMaxSpeed[id, i] = weaponFloatData[WpnConst.EfWeaponMaxSpeed];
            Store.WeaponAimMaxSpeed[id, i] = weaponFloatData[WpnConst.EfWeaponAimMaxSpeed];
            Store.WeaponKnockBack[id, i] = weaponFloatData[WpnConst.EfWeaponKnockBack];
            Store.SightDistance[id, i] = weaponFloatData[WpnConst.EfSightDistance];
            Store.SightFrameRate[id, i] = weaponFloatData[WpnConst.EfSightFrameRate];
            Store.SightOpenTime[id, i] = weaponFloatData[WpnConst.EfSightOpenTime];
            Store.SightCloseTime[id, i] = weaponFloatData[WpnConst.EfSightCloseTime];
            Store.WeaponSilencedTime[id, i, 0] = weaponFloatData[WpnConst.EfWeaponSilencedTime1];
            Store.WeaponSilencedTime[id, i, 1] = weaponFloatData[WpnConst.EfWeaponSilencedTime2];
            Store.WeaponFireSoundVolume[id, i] = weaponFloatData[WpnConst.EfWeaponFireSoundVolume];
            Store.WeaponSilenceFireSoundVolume[id, i] = weaponFloatData[WpnConst.EfWeaponSilenceFireSoundVolume];
            Store.WeaponList[id, i] = weaponList;
            Store.WeaponSpr[id, i] = killSpr;
            Store.WeaponPModel[id, i] = pModel;
            Store.WeaponVModel[id, i] = vModel;
            Store.WeaponWModel[id, i] = wModel;
            Store.WeaponVModelS[id, i] = sightModel;
            Store.WeaponSound1[id, i] = fireSound1;
            Store.WeaponSound2[id, i] = fireSound2;
            Store.WeaponSound3[id, i] = silencedFireSound1;
            Store.WeaponSound4[id, i] = silencedFireSound2;
            Store.WeaponBuyCommand[id, i] = command;
        }

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

    [AmxxNative]
    public static void SetWeaponAnim(int weaponIndex, int idle, int reload, int draw, Span<int> shoot, int shootLen, Span<int> shotgunReload, int shotgunReloadLen,
        int sightOpen, int sightClose, int detachSilencer, int attachSilencer, int idleSilenced, int reloadSilenced, int drawSilenced,
        Span<int> silencedShoot, int silencedShootLen)
    {
        int i = weaponIndex;
        for (int id = 1; id < 33; id++)
        {
            Store.WeaponAnim[id, i, WpnConst.AnimIdle] = idle;
            Store.WeaponAnim[id, i, WpnConst.AnimReload] = reload;
            Store.WeaponAnim[id, i, WpnConst.AnimDraw] = draw;
            Store.WeaponAnim[id, i, WpnConst.AnimShoot1] = shoot[0];
            Store.WeaponAnim[id, i, WpnConst.AnimShoot2] = shoot[1];
            Store.WeaponAnim[id, i, WpnConst.AnimShoot3] = shoot[2];
            Store.WeaponAnim[id, i, WpnConst.AnimStartReload] = shotgunReload[0];
            Store.WeaponAnim[id, i, WpnConst.AnimInsert] = shotgunReload[1];
            Store.WeaponAnim[id, i, WpnConst.AnimAfterReload] = shotgunReload[2];
            Store.WeaponAnim[id, i, WpnConst.AnimSightOpen] = sightOpen;
            Store.WeaponAnim[id, i, WpnConst.AnimSightClose] = sightClose;
            Store.WeaponAnim[id, i, WpnConst.AnimDetachSilencer] = detachSilencer;
            Store.WeaponAnim[id, i, WpnConst.AnimAttachSilencer] = attachSilencer;
            Store.WeaponAnim[id, i, WpnConst.AnimIdleSilenced] = idleSilenced;
            Store.WeaponAnim[id, i, WpnConst.AnimReloadSilenced] = reloadSilenced;
            Store.WeaponAnim[id, i, WpnConst.AnimDrawSilenced] = drawSilenced;
            Store.WeaponAnim[id, i, WpnConst.AnimShoot1Silenced] = silencedShoot[0];
            Store.WeaponAnim[id, i, WpnConst.AnimShoot2Silenced] = silencedShoot[1];
            Store.WeaponAnim[id, i, WpnConst.AnimShoot3Silenced] = silencedShoot[2];
        }
    }

    [AmxxNative]
    public static int RegisterKnife(string weaponList, string killSpr, string pModel, string vModel,
        string knifeSound1, string knifeSound2, string knifeSound3, string knifeSound4, string knifeSound5, string knifeSound6,
        Span<float> knifeData, int knifeDataLen, string command, int cost, int team)
    {
        Store.WeaponAmount++;
        int i = Store.WeaponAmount;

        for (int id = 1; id < 33; id++)
        {
            Store.WeaponId[id, i] = EngineConst.CswKnife;
            Store.KnifeSlashResetTime[id, i] = knifeData[WpnConst.EfKnifeSlashResetTime];
            Store.KnifeSlashTime[id, i] = knifeData[WpnConst.EfKnifeSlashTime];
            Store.KnifeSlashRange[id, i] = knifeData[WpnConst.EfKnifeSlashRange];
            Store.KnifeSlashAngle[id, i] = knifeData[WpnConst.EfKnifeSlashAngle];
            Store.KnifeSlashAngleOffset[id, i] = knifeData[WpnConst.EfKnifeSlashAngleOffset];
            Store.KnifeSlashHeight[id, i] = knifeData[WpnConst.EfKnifeSlashHeight];
            Store.KnifeSlashHeightOffset[id, i] = knifeData[WpnConst.EfKnifeSlashHeightOffset];
            Store.KnifeStabResetTime[id, i] = knifeData[WpnConst.EfKnifeStabResetTime];
            Store.KnifeStabTime[id, i] = knifeData[WpnConst.EfKnifeStabTime];
            Store.KnifeStabRange[id, i] = knifeData[WpnConst.EfKnifeStabRange];
            Store.KnifeStabAngle[id, i] = knifeData[WpnConst.EfKnifeStabAngle];
            Store.KnifeStabAngleOffset[id, i] = knifeData[WpnConst.EfKnifeStabAngleOffset];
            Store.KnifeStabHeight[id, i] = knifeData[WpnConst.EfKnifeStabHeight];
            Store.KnifeStabHeightOffset[id, i] = knifeData[WpnConst.EfKnifeStabHeightOffset];
            Store.WeaponDrawTime[id, i] = knifeData[WpnConst.EfKnifeDrawTime];
            Store.WeaponDamage[id, i] = knifeData[WpnConst.EfKnifeDamage];
            Store.WeaponDamage2[id, i] = knifeData[WpnConst.EfKnifeDamage2];
            Store.WeaponMaxSpeed[id, i] = knifeData[WpnConst.EfKnifeMaxSpeed];
            Store.WeaponKnockBack[id, i] = knifeData[WpnConst.EfKnifeKnockBack];
            Store.KnifeSoundVolume[id, i, 0] = knifeData[WpnConst.EfKnifeSoundVolume1];
            Store.KnifeSoundVolume[id, i, 1] = knifeData[WpnConst.EfKnifeSoundVolume2];
            Store.KnifeSoundVolume[id, i, 2] = knifeData[WpnConst.EfKnifeSoundVolume3];
            Store.KnifeSoundVolume[id, i, 3] = knifeData[WpnConst.EfKnifeSoundVolume4];
            Store.KnifeSoundVolume[id, i, 4] = knifeData[WpnConst.EfKnifeSoundVolume5];
            Store.WeaponCost[id, i] = cost;
            Store.WeaponTeam[id, i] = team;
            Store.WeaponList[id, i] = weaponList;
            Store.WeaponSpr[id, i] = killSpr;
            Store.WeaponPModel[id, i] = pModel;
            Store.WeaponVModel[id, i] = vModel;
            Store.WeaponSound1[id, i] = knifeSound1;
            Store.WeaponSound2[id, i] = knifeSound2;
            Store.WeaponSound3[id, i] = knifeSound3;
            Store.WeaponSound4[id, i] = knifeSound4;
            Store.WeaponSound5[id, i] = knifeSound5;
            Store.WeaponSound6[id, i] = knifeSound6;
            Store.WeaponBuyCommand[id, i] = command;
        }

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

    [AmxxNative]
    public static void SetKnifeAnim(int weaponIndex, int idle, int slash1, int slash2, int draw, int stab, int stabMiss, int midSlash, int midSlashMiss,
        int slashBegin, int stabBegin)
    {
        int i = weaponIndex;
        for (int id = 1; id < 33; id++)
        {
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimIdle] = idle;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlash1] = slash1;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlash2] = slash2;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimDraw] = draw;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab] = stab;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabMiss] = stabMiss;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimMidSlash] = midSlash;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimMidSlashMiss] = midSlashMiss;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin] = slashBegin;
            Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin] = stabBegin;
        }
    }

    [AmxxNative]
    public static int RegisterGrenade(string weaponList, string killSpr, string explodeSpr, string pModel, string vModel, string wModel,
        string explodeSound, float drawTime, float explodeTime, float range, float maxSpeed, float damage, float explodeSoundVolume,
        string command, int cost, int team, int wModelBody)
    {
        Store.WeaponAmount++;
        int i = Store.WeaponAmount;
        int grenadeSprId = 0;
        if (explodeSpr.Length > 0) grenadeSprId = Eng.PrecacheModel(explodeSpr);

        for (int id = 1; id < 33; id++)
        {
            Store.WeaponId[id, i] = EngineConst.CswHegrenade;
            Store.WeaponDrawTime[id, i] = drawTime;
            Store.GrenadeData[id, i, 1] = explodeTime;
            Store.GrenadeData[id, i, 2] = range;
            Store.WeaponMaxSpeed[id, i] = maxSpeed;
            Store.WeaponDamage[id, i] = damage;
            Store.WeaponFireSoundVolume[id, i] = explodeSoundVolume;
            Store.WeaponCost[id, i] = cost;
            Store.WeaponTeam[id, i] = team;
            Store.WeaponWModelBody[id, i] = wModelBody;
            Store.GrenadeSprId[id, i] = grenadeSprId;
            Store.WeaponList[id, i] = weaponList;
            Store.WeaponSpr[id, i] = killSpr;
            Store.WeaponPModel[id, i] = pModel;
            Store.WeaponVModel[id, i] = vModel;
            Store.WeaponWModel[id, i] = wModel;
            Store.WeaponSound1[id, i] = explodeSound;
            Store.WeaponBuyCommand[id, i] = command;
        }

        if (pModel.Length > 0) Eng.PrecacheModel(pModel);
        if (vModel.Length > 0) Eng.PrecacheModel(vModel);
        if (wModel.Length > 0) Eng.PrecacheModel(wModel);
        if (explodeSound.Length > 0) Eng.PrecacheSound(explodeSound);
        return i;
    }

    // ---- 发放 / 攻击 / 特效 ----

    [AmxxNative]
    public static int GiveWeapon(int id, int weaponIndex) => Core.GiveWeaponByTemplate(id, weaponIndex);

    [AmxxNative]
    public static int ExecuteAttack(int attacker, int victim, int entity, Span<float> param, int paramLen, Span<float> origin, int originLen,
        int damageType, int special)
        => Attack.CheckAttack(attacker, victim, Eng.EntOfIndex(entity), param, origin, damageType, special);

    [AmxxNative]
    public static void SpawnBlood(Span<float> origin, int originLen, int color, int amount)
        => Core.SpawnBlood(origin, color, amount);

    [AmxxNative]
    public static void FakeSmoke(Span<float> origin, int originLen)
        => Core.FakeSmoke(origin);

    [AmxxNative]
    public static void GiveAmmo(int id, int entity, int weaponIndex, int full)
        => Core.GiveAmmoForWeapon(id, entity, weaponIndex, full);

    // ---- 参数读取 ----

    [AmxxNative]
    public static int GetParamInt(int weaponIndex, int id, int type)
    {
        int i = weaponIndex;
        switch (type)
        {
            case WpnConst.WpnIWeaponId: return Store.WeaponId[id, i];
            case WpnConst.WpnIExplodeSprId: return Store.GrenadeSprId[id, i];
            case WpnConst.WpnIReloadMode: return Store.ReloadMode[id, i];
            case WpnConst.WpnISightMode: return Store.WeaponSight[id, i];
            case WpnConst.WpnIClip: return Store.WeaponClip[id, i];
            case WpnConst.WpnIBPAmmo: return Store.WeaponAmmo[id, i];
            case WpnConst.WpnICost: return Store.WeaponCost[id, i];
            case WpnConst.WpnIAmmoCost: return Store.WeaponAmmoCost[id, i];
            case WpnConst.WpnITeam: return Store.WeaponTeam[id, i];
            case WpnConst.WpnIWModelBody: return Store.WeaponWModelBody[id, i];
            case WpnConst.WpnIAnimIdle: return Store.WeaponAnim[id, i, WpnConst.AnimIdle];
            case WpnConst.WpnIAnimReload: return Store.WeaponAnim[id, i, WpnConst.AnimReload];
            case WpnConst.WpnIAnimDraw: return Store.WeaponAnim[id, i, WpnConst.AnimDraw];
            case WpnConst.WpnIAnimShoot1: return Store.WeaponAnim[id, i, WpnConst.AnimShoot1];
            case WpnConst.WpnIAnimShoot2: return Store.WeaponAnim[id, i, WpnConst.AnimShoot2];
            case WpnConst.WpnIAnimShoot3: return Store.WeaponAnim[id, i, WpnConst.AnimShoot3];
            case WpnConst.WpnIAnimStartReload: return Store.WeaponAnim[id, i, WpnConst.AnimStartReload];
            case WpnConst.WpnIAnimInsert: return Store.WeaponAnim[id, i, WpnConst.AnimInsert];
            case WpnConst.WpnIAnimAfterReload: return Store.WeaponAnim[id, i, WpnConst.AnimAfterReload];
            case WpnConst.WpnIAnimSightOpen: return Store.WeaponAnim[id, i, WpnConst.AnimSightOpen];
            case WpnConst.WpnIAnimSightClose: return Store.WeaponAnim[id, i, WpnConst.AnimSightClose];
            case WpnConst.WpnIAnimSilencedIdle: return Store.WeaponAnim[id, i, WpnConst.AnimIdleSilenced];
            case WpnConst.WpnIAnimSilencedReload: return Store.WeaponAnim[id, i, WpnConst.AnimReloadSilenced];
            case WpnConst.WpnIAnimSilencedDraw: return Store.WeaponAnim[id, i, WpnConst.AnimDrawSilenced];
            case WpnConst.WpnIAnimSilencedShoot1: return Store.WeaponAnim[id, i, WpnConst.AnimShoot1Silenced];
            case WpnConst.WpnIAnimSilencedShoot2: return Store.WeaponAnim[id, i, WpnConst.AnimShoot2Silenced];
            case WpnConst.WpnIAnimSilencedShoot3: return Store.WeaponAnim[id, i, WpnConst.AnimShoot3Silenced];
            case WpnConst.WpnIAnimDetachSilencer: return Store.WeaponAnim[id, i, WpnConst.AnimDetachSilencer];
            case WpnConst.WpnIAnimAttachSilencer: return Store.WeaponAnim[id, i, WpnConst.AnimAttachSilencer];
            case WpnConst.WpnIKnifeAnimIdle: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimIdle];
            case WpnConst.WpnIKnifeAnimSlash1: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlash1];
            case WpnConst.WpnIKnifeAnimSlash2: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlash2];
            case WpnConst.WpnIKnifeAnimDraw: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimDraw];
            case WpnConst.WpnIKnifeAnimStab: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab];
            case WpnConst.WpnIKnifeAnimStabMiss: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabMiss];
            case WpnConst.WpnIKnifeAnimMidSlash: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimMidSlash];
            case WpnConst.WpnIKnifeAnimMidSlashMiss: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimMidSlashMiss];
            case WpnConst.WpnIKnifeAnimSlashBegin: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin];
            case WpnConst.WpnIKnifeAnimStabBegin: return Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin];
            case WpnConst.WpnIUser1: return Store.User1[id, i];
            case WpnConst.WpnIUser2: return Store.User2[id, i];
            case WpnConst.WpnIUser3: return Store.User3[id, i];
            case WpnConst.WpnIUser4: return Store.User4[id, i];
            case WpnConst.WpnIAmount: return Store.WeaponAmount;
            case WpnConst.WpnID:
                if (id < 3) return Store.WeaponTemplateSerialNumber[id, i];
                return Core.WeaponTypeByIndex(id);
            case WpnConst.WpnIBloodColor: return Store.BloodColor;
            case WpnConst.WpnAllTeamBuy: return Store.AllTeamBuy;
            case WpnConst.WpnSetBuy: return Store.BlockBuy[id];
            case WpnConst.WpnIUserSight: return Store.SightMode[id];
            case WpnConst.WpnIBotMoney: return Store.BotMoney[id];
            case WpnConst.WpnIGameWeaponBPAmmo: return Store.GameWeaponMaxBpammo[i];
            case WpnConst.WpnIGameCurWeaponBPAmmo: return Store.GameCurWeaponBPAmmo[id, i];
            case WpnConst.WpnICurWeaponBPAmmo: return Store.CurWeaponBPAmmo[id, i];
            case WpnConst.WpnIMaxMoney: return Store.MaxMoney;
            case WpnConst.WpnIUserMoney: return Core.GetUserMoney(id);
            case WpnConst.WpnIAttackEntity: return Store.AttackerEntity[id];
        }
        return -1;
    }

    [AmxxNative]
    public static float GetParamFloat(int weaponIndex, int id, int type)
    {
        int i = weaponIndex;
        if (i == 0) return -1.0f;

        switch (type)
        {
            case WpnConst.WpnFDamage: return Store.WeaponDamage[id, i];
            case WpnConst.WpnFStabDamage: return Store.WeaponDamage2[id, i];
            case WpnConst.WpnFFireSpeed: return Store.WeaponSpeed[id, i];
            case WpnConst.WpnFAimFireSpeed: return Store.WeaponAimSpeed[id, i];
            case WpnConst.WpnFSilencedFireSpeed: return Store.WeaponSilencedSpeed[id, i];
            case WpnConst.WpnFRecoil: return Store.WeaponRecoil[id, i];
            case WpnConst.WpnFAimRecoil: return Store.WeaponAimRecoil[id, i];
            case WpnConst.WpnFSilencedRecoil: return Store.WeaponSilencedRecoil[id, i];
            case WpnConst.WpnFAccuracy: return Store.WeaponAccuracy[id, i];
            case WpnConst.WpnFAimAccuracy: return Store.WeaponAimAccuracy[id, i];
            case WpnConst.WpnFSilencedAccuracy: return Store.WeaponSilencedAccuracy[id, i];
            case WpnConst.WpnFSightDistance: return Store.SightDistance[id, i];
            case WpnConst.WpnFSightFramerate: return Store.SightFrameRate[id, i];
            case WpnConst.WpnFSightOpenTime: return Store.SightOpenTime[id, i];
            case WpnConst.WpnFSightCloseTime: return Store.SightCloseTime[id, i];
            case WpnConst.WpnFReloadTime: return Store.WeaponReloadTime[id, i, 0];
            case WpnConst.WpnFShotgunStartReloadTime: return Store.WeaponReloadTime[id, i, 1];
            case WpnConst.WpnFShotgunAfterReloadTime: return Store.WeaponReloadTime[id, i, 2];
            case WpnConst.WpnFDrawTime: return Store.WeaponDrawTime[id, i];
            case WpnConst.WpnFDetachSilencerTime: return Store.WeaponSilencedTime[id, i, 1];
            case WpnConst.WpnFAttachSilencerTime: return Store.WeaponSilencedTime[id, i, 0];
            case WpnConst.WpnFSlashResetTime: return Store.KnifeSlashResetTime[id, i];
            case WpnConst.WpnFSlashTime: return Store.KnifeSlashTime[id, i];
            case WpnConst.WpnFSlashRange: return Store.KnifeSlashRange[id, i];
            case WpnConst.WpnFSlashAngle: return Store.KnifeSlashAngle[id, i];
            case WpnConst.WpnFStabResetTime: return Store.KnifeStabResetTime[id, i];
            case WpnConst.WpnFStabTime: return Store.KnifeStabTime[id, i];
            case WpnConst.WpnFStabRange: return Store.KnifeStabRange[id, i];
            case WpnConst.WpnFStabAngle: return Store.KnifeStabAngle[id, i];
            case WpnConst.WpnFExpTime: return Store.GrenadeData[id, i, 1];
            case WpnConst.WpnFExpRange: return Store.GrenadeData[id, i, 2];
            case WpnConst.WpnFMaxSpeed: return Store.WeaponMaxSpeed[id, i];
            case WpnConst.WpnFAimMaxSpeed: return Store.WeaponAimMaxSpeed[id, i];
            case WpnConst.WpnFKnockBack: return Store.WeaponKnockBack[id, i];
            case WpnConst.WpnFUser1: return Store.FUser1[id, i];
            case WpnConst.WpnFUser2: return Store.FUser2[id, i];
            case WpnConst.WpnFUser3: return Store.FUser3[id, i];
            case WpnConst.WpnFUser4: return Store.FUser4[id, i];
            case WpnConst.WpnFDamagePoint: return Store.DamagePoint[id];
            case WpnConst.WpnFKnockBackPoint: return Store.KnockBackPoint[id];
            case WpnConst.WpnFDamageVictimPoint: return Store.DamageVictimPoint[id];
            case WpnConst.WpnFFireSoundVolume: return Store.WeaponFireSoundVolume[id, i];
            case WpnConst.WpnFSilenceFireSoundVolume: return Store.WeaponSilenceFireSoundVolume[id, i];
            case WpnConst.WpnFKnifeSoundVolume1: return Store.KnifeSoundVolume[id, i, 0];
            case WpnConst.WpnFKnifeSoundVolume2: return Store.KnifeSoundVolume[id, i, 1];
            case WpnConst.WpnFKnifeSoundVolume3: return Store.KnifeSoundVolume[id, i, 2];
            case WpnConst.WpnFKnifeSoundVolume4: return Store.KnifeSoundVolume[id, i, 3];
            case WpnConst.WpnFKnifeSoundVolume5: return Store.KnifeSoundVolume[id, i, 4];
            case WpnConst.WpnFKnifeSlashAngleOffset: return Store.KnifeSlashAngleOffset[id, i];
            case WpnConst.WpnFKnifeStabAngleOffset: return Store.KnifeStabAngleOffset[id, i];
            case WpnConst.WpnFKnifeSlashHeight: return Store.KnifeSlashHeight[id, i];
            case WpnConst.WpnFKnifeStabHeight: return Store.KnifeStabHeight[id, i];
            case WpnConst.WpnFKnifeSlashHeightOffset: return Store.KnifeSlashHeightOffset[id, i];
            case WpnConst.WpnFKnifeStabHeightOffset: return Store.KnifeStabHeightOffset[id, i];
        }
        return -1.0f;
    }

    [AmxxNative]
    public static void GetParamString(int weaponIndex, int id, int type, out string value, int len)
    {
        value = "";
        int i = weaponIndex;
        if (i == 0) return;

        switch (type)
        {
            case WpnConst.WpnSzWeaponList: value = Store.WeaponList[id, i]; break;
            case WpnConst.WpnSzKillSpr: value = Store.WeaponSpr[id, i]; break;
            case WpnConst.WpnSzPModel: value = Store.WeaponPModel[id, i]; break;
            case WpnConst.WpnSzVModel: value = Store.WeaponVModel[id, i]; break;
            case WpnConst.WpnSzWModel: value = Store.WeaponWModel[id, i]; break;
            case WpnConst.WpnSzSightModel: value = Store.WeaponVModelS[id, i]; break;
            case WpnConst.WpnSzSound1: value = Store.WeaponSound1[id, i]; break;
            case WpnConst.WpnSzSound2: value = Store.WeaponSound2[id, i]; break;
            case WpnConst.WpnSzSound3: value = Store.WeaponSound3[id, i]; break;
            case WpnConst.WpnSzSound4: value = Store.WeaponSound4[id, i]; break;
            case WpnConst.WpnSzSound5: value = Store.WeaponSound5[id, i]; break;
            case WpnConst.WpnSzSound6: value = Store.WeaponSound6[id, i]; break;
            case WpnConst.WpnSzCommand: value = Store.WeaponBuyCommand[id, i]; break;
            case WpnConst.WpnSzUser1: value = Store.SzUser1[id, i]; break;
            case WpnConst.WpnSzUser2: value = Store.SzUser2[id, i]; break;
            case WpnConst.WpnSzUser3: value = Store.SzUser3[id, i]; break;
            case WpnConst.WpnSzUser4: value = Store.SzUser4[id, i]; break;
        }
    }

    // ---- 参数写入 ----

    [AmxxNative]
    public static void SetParamInt(int weaponIndex, int id, int type, int value)
    {
        if (type == WpnConst.WpnIBloodColor) Store.BloodColor = value;
        if (type == WpnConst.WpnAllTeamBuy) Store.AllTeamBuy = value;
        if (type == WpnConst.WpnSetBuy) Store.BlockBuy[id] = value;
        if (type == WpnConst.WpnIBotMoney) Store.BotMoney[id] = value;
        if (type == WpnConst.WpnIGameWeaponBPAmmo) Store.GameWeaponMaxBpammo[weaponIndex] = value;
        if (type == WpnConst.WpnIMaxMoney) Store.MaxMoney = value;
        if (type == WpnConst.WpnIAttackEntity) Store.AttackerEntity[id] = value;
        if (type == WpnConst.WpnIUserMoney) Core.SetUserMoney(id, value, weaponIndex);
        if (type == WpnConst.WpnIHitGroup) Store.AttackHitGroup[id] = value;

        int i = weaponIndex;
        if (i == 0) return;

        switch (type)
        {
            case WpnConst.WpnIWeaponId: Store.WeaponId[id, i] = value; break;
            case WpnConst.WpnIExplodeSprId: Store.GrenadeSprId[id, i] = value; break;
            case WpnConst.WpnIReloadMode: Store.ReloadMode[id, i] = value; break;
            case WpnConst.WpnISightMode: Store.WeaponSight[id, i] = value; break;
            case WpnConst.WpnIClip: Store.WeaponClip[id, i] = value; break;
            case WpnConst.WpnIBPAmmo: Store.WeaponAmmo[id, i] = value; break;
            case WpnConst.WpnICost: Store.WeaponCost[id, i] = value; break;
            case WpnConst.WpnIAmmoCost: Store.WeaponAmmoCost[id, i] = value; break;
            case WpnConst.WpnITeam: Store.WeaponTeam[id, i] = value; break;
            case WpnConst.WpnIWModelBody: Store.WeaponWModelBody[id, i] = value; break;
            case WpnConst.WpnIAnimIdle: Store.WeaponAnim[id, i, WpnConst.AnimIdle] = value; break;
            case WpnConst.WpnIAnimReload: Store.WeaponAnim[id, i, WpnConst.AnimReload] = value; break;
            case WpnConst.WpnIAnimDraw: Store.WeaponAnim[id, i, WpnConst.AnimDraw] = value; break;
            case WpnConst.WpnIAnimShoot1: Store.WeaponAnim[id, i, WpnConst.AnimShoot1] = value; break;
            case WpnConst.WpnIAnimShoot2: Store.WeaponAnim[id, i, WpnConst.AnimShoot2] = value; break;
            case WpnConst.WpnIAnimShoot3: Store.WeaponAnim[id, i, WpnConst.AnimShoot3] = value; break;
            case WpnConst.WpnIAnimStartReload: Store.WeaponAnim[id, i, WpnConst.AnimStartReload] = value; break;
            case WpnConst.WpnIAnimInsert: Store.WeaponAnim[id, i, WpnConst.AnimInsert] = value; break;
            case WpnConst.WpnIAnimAfterReload: Store.WeaponAnim[id, i, WpnConst.AnimAfterReload] = value; break;
            case WpnConst.WpnIAnimSightOpen: Store.WeaponAnim[id, i, WpnConst.AnimSightOpen] = value; break;
            case WpnConst.WpnIAnimSightClose: Store.WeaponAnim[id, i, WpnConst.AnimSightClose] = value; break;
            case WpnConst.WpnIAnimSilencedIdle: Store.WeaponAnim[id, i, WpnConst.AnimIdleSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedReload: Store.WeaponAnim[id, i, WpnConst.AnimReloadSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedDraw: Store.WeaponAnim[id, i, WpnConst.AnimDrawSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot1: Store.WeaponAnim[id, i, WpnConst.AnimShoot1Silenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot2: Store.WeaponAnim[id, i, WpnConst.AnimShoot2Silenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot3: Store.WeaponAnim[id, i, WpnConst.AnimShoot3Silenced] = value; break;
            case WpnConst.WpnIAnimDetachSilencer: Store.WeaponAnim[id, i, WpnConst.AnimDetachSilencer] = value; break;
            case WpnConst.WpnIAnimAttachSilencer: Store.WeaponAnim[id, i, WpnConst.AnimAttachSilencer] = value; break;
            case WpnConst.WpnIKnifeAnimIdle: Store.KnifeAnim[id, i, WpnConst.KnifeAnimIdle] = value; break;
            case WpnConst.WpnIKnifeAnimSlash1: Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlash1] = value; break;
            case WpnConst.WpnIKnifeAnimSlash2: Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlash2] = value; break;
            case WpnConst.WpnIKnifeAnimDraw: Store.KnifeAnim[id, i, WpnConst.KnifeAnimDraw] = value; break;
            case WpnConst.WpnIKnifeAnimStab: Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab] = value; break;
            case WpnConst.WpnIKnifeAnimStabMiss: Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabMiss] = value; break;
            case WpnConst.WpnIKnifeAnimMidSlash: Store.KnifeAnim[id, i, WpnConst.KnifeAnimMidSlash] = value; break;
            case WpnConst.WpnIKnifeAnimMidSlashMiss: Store.KnifeAnim[id, i, WpnConst.KnifeAnimMidSlashMiss] = value; break;
            case WpnConst.WpnIKnifeAnimSlashBegin: Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin] = value; break;
            case WpnConst.WpnIKnifeAnimStabBegin: Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin] = value; break;
            case WpnConst.WpnIUser1: Store.User1[id, i] = value; break;
            case WpnConst.WpnIUser2: Store.User2[id, i] = value; break;
            case WpnConst.WpnIUser3: Store.User3[id, i] = value; break;
            case WpnConst.WpnIUser4: Store.User4[id, i] = value; break;
            case WpnConst.WpnIGameCurWeaponBPAmmo: Store.GameCurWeaponBPAmmo[id, i] = value; break;
            case WpnConst.WpnICurWeaponBPAmmo: Store.CurWeaponBPAmmo[id, i] = value; break;
        }
    }

    [AmxxNative]
    public static void SetParamFloat(int weaponIndex, int id, int type, float value)
    {
        if (type == WpnConst.WpnFDamagePoint) Store.DamagePoint[id] = value;
        if (type == WpnConst.WpnFKnockBackPoint) Store.KnockBackPoint[id] = value;
        if (type == WpnConst.WpnFDamageVictimPoint) Store.DamageVictimPoint[id] = value;

        int i = weaponIndex;
        if (i == 0) return;

        switch (type)
        {
            case WpnConst.WpnFDamage: Store.WeaponDamage[id, i] = value; break;
            case WpnConst.WpnFStabDamage: Store.WeaponDamage2[id, i] = value; break;
            case WpnConst.WpnFFireSpeed: Store.WeaponSpeed[id, i] = value; break;
            case WpnConst.WpnFAimFireSpeed: Store.WeaponAimSpeed[id, i] = value; break;
            case WpnConst.WpnFSilencedFireSpeed: Store.WeaponSilencedSpeed[id, i] = value; break;
            case WpnConst.WpnFRecoil: Store.WeaponRecoil[id, i] = value; break;
            case WpnConst.WpnFAimRecoil: Store.WeaponAimRecoil[id, i] = value; break;
            case WpnConst.WpnFSilencedRecoil: Store.WeaponSilencedRecoil[id, i] = value; break;
            case WpnConst.WpnFAccuracy: Store.WeaponAccuracy[id, i] = value; break;
            case WpnConst.WpnFAimAccuracy: Store.WeaponAimAccuracy[id, i] = value; break;
            case WpnConst.WpnFSilencedAccuracy: Store.WeaponSilencedAccuracy[id, i] = value; break;
            case WpnConst.WpnFSightDistance: Store.SightDistance[id, i] = value; break;
            case WpnConst.WpnFSightFramerate: Store.SightFrameRate[id, i] = value; break;
            case WpnConst.WpnFSightOpenTime: Store.SightOpenTime[id, i] = value; break;
            case WpnConst.WpnFSightCloseTime: Store.SightCloseTime[id, i] = value; break;
            case WpnConst.WpnFReloadTime: Store.WeaponReloadTime[id, i, 0] = value; break;
            case WpnConst.WpnFShotgunStartReloadTime: Store.WeaponReloadTime[id, i, 1] = value; break;
            case WpnConst.WpnFShotgunAfterReloadTime: Store.WeaponReloadTime[id, i, 2] = value; break;
            case WpnConst.WpnFDrawTime: Store.WeaponDrawTime[id, i] = value; break;
            case WpnConst.WpnFDetachSilencerTime: Store.WeaponSilencedTime[id, i, 1] = value; break;
            case WpnConst.WpnFAttachSilencerTime: Store.WeaponSilencedTime[id, i, 0] = value; break;
            case WpnConst.WpnFSlashResetTime: Store.KnifeSlashResetTime[id, i] = value; break;
            case WpnConst.WpnFSlashTime: Store.KnifeSlashTime[id, i] = value; break;
            case WpnConst.WpnFSlashRange: Store.KnifeSlashRange[id, i] = value; break;
            case WpnConst.WpnFSlashAngle: Store.KnifeSlashAngle[id, i] = value; break;
            case WpnConst.WpnFStabResetTime: Store.KnifeStabResetTime[id, i] = value; break;
            case WpnConst.WpnFStabTime: Store.KnifeStabTime[id, i] = value; break;
            case WpnConst.WpnFStabRange: Store.KnifeStabRange[id, i] = value; break;
            case WpnConst.WpnFStabAngle: Store.KnifeStabAngle[id, i] = value; break;
            case WpnConst.WpnFExpTime: Store.GrenadeData[id, i, 1] = value; break;
            case WpnConst.WpnFExpRange: Store.GrenadeData[id, i, 2] = value; break;
            case WpnConst.WpnFMaxSpeed: Store.WeaponMaxSpeed[id, i] = value; break;
            case WpnConst.WpnFAimMaxSpeed: Store.WeaponAimMaxSpeed[id, i] = value; break;
            case WpnConst.WpnFKnockBack: Store.WeaponKnockBack[id, i] = value; break;
            case WpnConst.WpnFUser1: Store.FUser1[id, i] = value; break;
            case WpnConst.WpnFUser2: Store.FUser2[id, i] = value; break;
            case WpnConst.WpnFUser3: Store.FUser3[id, i] = value; break;
            case WpnConst.WpnFUser4: Store.FUser4[id, i] = value; break;
            case WpnConst.WpnFFireSoundVolume: Store.WeaponFireSoundVolume[id, i] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : 0.35f; break;
            case WpnConst.WpnFSilenceFireSoundVolume: Store.WeaponSilenceFireSoundVolume[id, i] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume1: Store.KnifeSoundVolume[id, i, 0] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume2: Store.KnifeSoundVolume[id, i, 1] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume3: Store.KnifeSoundVolume[id, i, 2] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume4: Store.KnifeSoundVolume[id, i, 3] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume5: Store.KnifeSoundVolume[id, i, 4] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSlashAngleOffset: Store.KnifeSlashAngleOffset[id, i] = value; break;
            case WpnConst.WpnFKnifeStabAngleOffset: Store.KnifeStabAngleOffset[id, i] = value; break;
            case WpnConst.WpnFKnifeSlashHeight: Store.KnifeSlashHeight[id, i] = value; break;
            case WpnConst.WpnFKnifeStabHeight: Store.KnifeStabHeight[id, i] = value; break;
            case WpnConst.WpnFKnifeSlashHeightOffset: Store.KnifeSlashHeightOffset[id, i] = value; break;
            case WpnConst.WpnFKnifeStabHeightOffset: Store.KnifeStabHeightOffset[id, i] = value; break;
        }
    }

    [AmxxNative]
    public static void SetParamString(int weaponIndex, int id, int type, string value)
    {
        int i = weaponIndex;
        if (i == 0) return;

        switch (type)
        {
            case WpnConst.WpnSzWeaponList: Store.WeaponList[id, i] = value; break;
            case WpnConst.WpnSzKillSpr: Store.WeaponSpr[id, i] = value; break;
            case WpnConst.WpnSzPModel: Store.WeaponPModel[id, i] = value; break;
            case WpnConst.WpnSzVModel: Store.WeaponVModel[id, i] = value; break;
            case WpnConst.WpnSzWModel: Store.WeaponWModel[id, i] = value; break;
            case WpnConst.WpnSzSightModel: Store.WeaponVModelS[id, i] = value; break;
            case WpnConst.WpnSzSound1: Store.WeaponSound1[id, i] = value; break;
            case WpnConst.WpnSzSound2: Store.WeaponSound2[id, i] = value; break;
            case WpnConst.WpnSzSound3: Store.WeaponSound3[id, i] = value; break;
            case WpnConst.WpnSzSound4: Store.WeaponSound4[id, i] = value; break;
            case WpnConst.WpnSzSound5: Store.WeaponSound5[id, i] = value; break;
            case WpnConst.WpnSzSound6: Store.WeaponSound6[id, i] = value; break;
            case WpnConst.WpnSzCommand: Store.WeaponBuyCommand[id, i] = value; break;
            case WpnConst.WpnSzUser1: Store.SzUser1[id, i] = value; break;
            case WpnConst.WpnSzUser2: Store.SzUser2[id, i] = value; break;
            case WpnConst.WpnSzUser3: Store.SzUser3[id, i] = value; break;
            case WpnConst.WpnSzUser4: Store.SzUser4[id, i] = value; break;
        }
    }
}
