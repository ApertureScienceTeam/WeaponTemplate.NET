using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// WPN_SetParam* :Pawn 插件覆写模板/玩家参数(写入玩家副本,见 PlayerData.WeaponForWrite)。
public static unsafe partial class Natives
{
    // ---- 参数写入 ----

    [AmxxNative("WPN_SetParamInt")]
    public static void SetParamInt(int weaponIndex, int id, int type, int value)
    {
        int i = weaponIndex;
        switch (type)
        {
            case WpnConst.WpnIBloodColor: WeaponStore.BloodColor = value; return;
            case WpnConst.WpnAllTeamBuy: WeaponStore.AllTeamBuy = value; return;
            case WpnConst.WpnIMaxMoney: WeaponStore.MaxMoney = Math.Max(0, value); return;
            case WpnConst.WpnIGameWeaponBPAmmo:
                if (i >= 0 && i < WeaponStore.GameWeaponMaxBpammo.Length) WeaponStore.GameWeaponMaxBpammo[i] = value;
                return;
        }
        if (!WeaponStore.IsPlayerIndex(id)) return;
        switch (type)
        {
            case WpnConst.WpnSetBuy: WeaponStore.Player(id).BlockBuy = value; return;
            case WpnConst.WpnIBotMoney: WeaponStore.Player(id).BotMoney = value; return;
            case WpnConst.WpnIAttackEntity: WeaponStore.Player(id).AttackerEntity = value; return;
            case WpnConst.WpnIUserMoney: PlayerStocks.SetUserMoney(id, value, weaponIndex); return;
            case WpnConst.WpnIHitGroup: WeaponStore.Player(id).AttackHitGroup = value; return;
            case WpnConst.WpnIGameCurWeaponBPAmmo:
                if (i >= 0 && i < WeaponStore.Player(id).GameBpAmmo.Length) WeaponStore.Player(id).GameBpAmmo[i] = value;
                return;
        }
        if (!WeaponStore.IsTemplate(i)) return;

        switch (type)
        {
            case WpnConst.WpnIWeaponId: WeaponStore.WriteWeapon(id, i).Id = value; break;
            case WpnConst.WpnIExplodeSprId: WeaponStore.WriteWeapon(id, i).GrenadeSprId = value; break;
            case WpnConst.WpnIReloadMode: WeaponStore.WriteWeapon(id, i).ReloadMode = value; break;
            case WpnConst.WpnISightMode: WeaponStore.WriteWeapon(id, i).Sight = value; break;
            case WpnConst.WpnIClip: WeaponStore.WriteWeapon(id, i).Clip = value; break;
            case WpnConst.WpnIBPAmmo: WeaponStore.WriteWeapon(id, i).Ammo = value; break;
            case WpnConst.WpnICost: WeaponStore.WriteWeapon(id, i).Cost = value; break;
            case WpnConst.WpnIAmmoCost: WeaponStore.WriteWeapon(id, i).AmmoCost = value; break;
            case WpnConst.WpnITeam: WeaponStore.WriteWeapon(id, i).Team = value; break;
            case WpnConst.WpnIWModelBody: WeaponStore.WriteWeapon(id, i).WModelBody = value; break;
            case WpnConst.WpnIAnimIdle: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimIdle] = value; break;
            case WpnConst.WpnIAnimReload: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimReload] = value; break;
            case WpnConst.WpnIAnimDraw: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimDraw] = value; break;
            case WpnConst.WpnIAnimShoot1: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimShoot1] = value; break;
            case WpnConst.WpnIAnimShoot2: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimShoot2] = value; break;
            case WpnConst.WpnIAnimShoot3: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimShoot3] = value; break;
            case WpnConst.WpnIAnimStartReload: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimStartReload] = value; break;
            case WpnConst.WpnIAnimInsert: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimInsert] = value; break;
            case WpnConst.WpnIAnimAfterReload: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimAfterReload] = value; break;
            case WpnConst.WpnIAnimSightOpen: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimSightOpen] = value; break;
            case WpnConst.WpnIAnimSightClose: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimSightClose] = value; break;
            case WpnConst.WpnIAnimSilencedIdle: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimIdleSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedReload: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimReloadSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedDraw: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimDrawSilenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot1: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimShoot1Silenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot2: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimShoot2Silenced] = value; break;
            case WpnConst.WpnIAnimSilencedShoot3: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimShoot3Silenced] = value; break;
            case WpnConst.WpnIAnimDetachSilencer: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimDetachSilencer] = value; break;
            case WpnConst.WpnIAnimAttachSilencer: WeaponStore.WriteWeapon(id, i).Anim[WpnConst.AnimAttachSilencer] = value; break;
            case WpnConst.WpnIKnifeAnimIdle: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimIdle] = value; break;
            case WpnConst.WpnIKnifeAnimSlash1: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash1] = value; break;
            case WpnConst.WpnIKnifeAnimSlash2: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash2] = value; break;
            case WpnConst.WpnIKnifeAnimDraw: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw] = value; break;
            case WpnConst.WpnIKnifeAnimStab: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab] = value; break;
            case WpnConst.WpnIKnifeAnimStabMiss: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabMiss] = value; break;
            case WpnConst.WpnIKnifeAnimMidSlash: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlash] = value; break;
            case WpnConst.WpnIKnifeAnimMidSlashMiss: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlashMiss] = value; break;
            case WpnConst.WpnIKnifeAnimSlashBegin: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] = value; break;
            case WpnConst.WpnIKnifeAnimStabBegin: WeaponStore.WriteWeapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin] = value; break;
            case WpnConst.WpnIUser1: WeaponStore.Player(id).User1[i] = value; break;
            case WpnConst.WpnIUser2: WeaponStore.Player(id).User2[i] = value; break;
            case WpnConst.WpnIUser3: WeaponStore.Player(id).User3[i] = value; break;
            case WpnConst.WpnIUser4: WeaponStore.Player(id).User4[i] = value; break;
            case WpnConst.WpnICurWeaponBPAmmo: WeaponStore.Player(id).BPAmmo[i] = value; break;
        }
    }

    [AmxxNative("WPN_SetParamFloat")]
    public static void SetParamFloat(int weaponIndex, int id, int type, float value)
    {
        if (!WeaponStore.IsPlayerIndex(id) || !float.IsFinite(value)) return;
        switch (type)
        {
            case WpnConst.WpnFDamagePoint: WeaponStore.Player(id).DamagePoint = value; return;
            case WpnConst.WpnFKnockBackPoint: WeaponStore.Player(id).KnockBackPoint = value; return;
            case WpnConst.WpnFDamageVictimPoint: WeaponStore.Player(id).DamageVictimPoint = value; return;
        }
        int i = weaponIndex;
        if (!WeaponStore.IsTemplate(i) || !float.IsFinite(value)) return;

        switch (type)
        {
            case WpnConst.WpnFDamage: WeaponStore.WriteWeapon(id, i).Damage = value; break;
            case WpnConst.WpnFStabDamage: WeaponStore.WriteWeapon(id, i).Damage2 = value; break;
            case WpnConst.WpnFFireSpeed: WeaponStore.WriteWeapon(id, i).Speed = value; break;
            case WpnConst.WpnFAimFireSpeed: WeaponStore.WriteWeapon(id, i).AimSpeed = value; break;
            case WpnConst.WpnFSilencedFireSpeed: WeaponStore.WriteWeapon(id, i).SilencedSpeed = value; break;
            case WpnConst.WpnFRecoil: WeaponStore.WriteWeapon(id, i).Recoil = value; break;
            case WpnConst.WpnFAimRecoil: WeaponStore.WriteWeapon(id, i).AimRecoil = value; break;
            case WpnConst.WpnFSilencedRecoil: WeaponStore.WriteWeapon(id, i).SilencedRecoil = value; break;
            case WpnConst.WpnFAccuracy: WeaponStore.WriteWeapon(id, i).Accuracy = value; break;
            case WpnConst.WpnFAimAccuracy: WeaponStore.WriteWeapon(id, i).AimAccuracy = value; break;
            case WpnConst.WpnFSilencedAccuracy: WeaponStore.WriteWeapon(id, i).SilencedAccuracy = value; break;
            case WpnConst.WpnFSightDistance: WeaponStore.WriteWeapon(id, i).SightDistance = value; break;
            case WpnConst.WpnFSightFramerate: WeaponStore.WriteWeapon(id, i).SightFrameRate = value; break;
            case WpnConst.WpnFSightOpenTime: WeaponStore.WriteWeapon(id, i).SightOpenTime = value; break;
            case WpnConst.WpnFSightCloseTime: WeaponStore.WriteWeapon(id, i).SightCloseTime = value; break;
            case WpnConst.WpnFReloadTime: WeaponStore.WriteWeapon(id, i).ReloadTime[0] = value; break;
            case WpnConst.WpnFShotgunStartReloadTime: WeaponStore.WriteWeapon(id, i).ReloadTime[1] = value; break;
            case WpnConst.WpnFShotgunAfterReloadTime: WeaponStore.WriteWeapon(id, i).ReloadTime[2] = value; break;
            case WpnConst.WpnFDrawTime: WeaponStore.WriteWeapon(id, i).DrawTime = value; break;
            case WpnConst.WpnFDetachSilencerTime: WeaponStore.WriteWeapon(id, i).SilencedTime[1] = value; break;
            case WpnConst.WpnFAttachSilencerTime: WeaponStore.WriteWeapon(id, i).SilencedTime[0] = value; break;
            case WpnConst.WpnFSlashResetTime: WeaponStore.WriteWeapon(id, i).KnifeSlashResetTime = value; break;
            case WpnConst.WpnFSlashTime: WeaponStore.WriteWeapon(id, i).KnifeSlashTime = value; break;
            case WpnConst.WpnFSlashRange: WeaponStore.WriteWeapon(id, i).KnifeSlashRange = value; break;
            case WpnConst.WpnFSlashAngle: WeaponStore.WriteWeapon(id, i).KnifeSlashAngle = value; break;
            case WpnConst.WpnFStabResetTime: WeaponStore.WriteWeapon(id, i).KnifeStabResetTime = value; break;
            case WpnConst.WpnFStabTime: WeaponStore.WriteWeapon(id, i).KnifeStabTime = value; break;
            case WpnConst.WpnFStabRange: WeaponStore.WriteWeapon(id, i).KnifeStabRange = value; break;
            case WpnConst.WpnFStabAngle: WeaponStore.WriteWeapon(id, i).KnifeStabAngle = value; break;
            case WpnConst.WpnFExpTime: WeaponStore.WriteWeapon(id, i).GrenadeData[1] = value; break;
            case WpnConst.WpnFExpRange: WeaponStore.WriteWeapon(id, i).GrenadeData[2] = value; break;
            case WpnConst.WpnFMaxSpeed: WeaponStore.WriteWeapon(id, i).MaxSpeed = value; break;
            case WpnConst.WpnFAimMaxSpeed: WeaponStore.WriteWeapon(id, i).AimMaxSpeed = value; break;
            case WpnConst.WpnFKnockBack: WeaponStore.WriteWeapon(id, i).KnockBack = value; break;
            case WpnConst.WpnFUser1: WeaponStore.Player(id).FUser1[i] = value; break;
            case WpnConst.WpnFUser2: WeaponStore.Player(id).FUser2[i] = value; break;
            case WpnConst.WpnFUser3: WeaponStore.Player(id).FUser3[i] = value; break;
            case WpnConst.WpnFUser4: WeaponStore.Player(id).FUser4[i] = value; break;
            case WpnConst.WpnFFireSoundVolume: WeaponStore.WriteWeapon(id, i).FireSoundVolume = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : 0.35f; break;
            case WpnConst.WpnFSilenceFireSoundVolume: WeaponStore.WriteWeapon(id, i).SilenceFireSoundVolume = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume1: WeaponStore.WriteWeapon(id, i).KnifeSoundVolume[0] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume2: WeaponStore.WriteWeapon(id, i).KnifeSoundVolume[1] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume3: WeaponStore.WriteWeapon(id, i).KnifeSoundVolume[2] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume4: WeaponStore.WriteWeapon(id, i).KnifeSoundVolume[3] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSoundVolume5: WeaponStore.WriteWeapon(id, i).KnifeSoundVolume[4] = value > 0.0f ? ((1.0f - value) > 0.0f ? 1.0f - value : 0.0f) : EngineConst.AttnNorm; break;
            case WpnConst.WpnFKnifeSlashAngleOffset: WeaponStore.WriteWeapon(id, i).KnifeSlashAngleOffset = value; break;
            case WpnConst.WpnFKnifeStabAngleOffset: WeaponStore.WriteWeapon(id, i).KnifeStabAngleOffset = value; break;
            case WpnConst.WpnFKnifeSlashHeight: WeaponStore.WriteWeapon(id, i).KnifeSlashHeight = value; break;
            case WpnConst.WpnFKnifeStabHeight: WeaponStore.WriteWeapon(id, i).KnifeStabHeight = value; break;
            case WpnConst.WpnFKnifeSlashHeightOffset: WeaponStore.WriteWeapon(id, i).KnifeSlashHeightOffset = value; break;
            case WpnConst.WpnFKnifeStabHeightOffset: WeaponStore.WriteWeapon(id, i).KnifeStabHeightOffset = value; break;
        }
    }

    [AmxxNative("WPN_SetParamString")]
    public static void SetParamString(int weaponIndex, int id, int type, string value)
    {
        int i = weaponIndex;
        if (!WeaponStore.IsPlayerIndex(id) || !WeaponStore.IsTemplate(i)) return;
        value ??= "";
        if (value.Length > 0 && type is >= WpnConst.WpnSzPModel and <= WpnConst.WpnSzSound6 &&
            !EngineResources.Exists(value, type >= WpnConst.WpnSzSound1)) return;

        switch (type)
        {
            case WpnConst.WpnSzWeaponList: WeaponStore.WriteWeapon(id, i).List = value; break;
            case WpnConst.WpnSzKillSpr: WeaponStore.WriteWeapon(id, i).Spr = value; break;
            case WpnConst.WpnSzPModel: WeaponStore.WriteWeapon(id, i).PModel = value; break;
            case WpnConst.WpnSzVModel: WeaponStore.WriteWeapon(id, i).VModel = value; break;
            case WpnConst.WpnSzWModel: WeaponStore.WriteWeapon(id, i).WModel = value; break;
            case WpnConst.WpnSzSightModel: WeaponStore.WriteWeapon(id, i).SightModel = value; break;
            case WpnConst.WpnSzSound1: WeaponStore.WriteWeapon(id, i).Sound1 = value; break;
            case WpnConst.WpnSzSound2: WeaponStore.WriteWeapon(id, i).Sound2 = value; break;
            case WpnConst.WpnSzSound3: WeaponStore.WriteWeapon(id, i).Sound3 = value; break;
            case WpnConst.WpnSzSound4: WeaponStore.WriteWeapon(id, i).Sound4 = value; break;
            case WpnConst.WpnSzSound5: WeaponStore.WriteWeapon(id, i).Sound5 = value; break;
            case WpnConst.WpnSzSound6: WeaponStore.WriteWeapon(id, i).Sound6 = value; break;
            case WpnConst.WpnSzCommand: WeaponStore.WriteWeapon(id, i).BuyCommand = value; break;
            case WpnConst.WpnSzUser1: WeaponStore.Player(id).SzUser1[i] = value; break;
            case WpnConst.WpnSzUser2: WeaponStore.Player(id).SzUser2[i] = value; break;
            case WpnConst.WpnSzUser3: WeaponStore.Player(id).SzUser3[i] = value; break;
            case WpnConst.WpnSzUser4: WeaponStore.Player(id).SzUser4[i] = value; break;
        }
    }
}
