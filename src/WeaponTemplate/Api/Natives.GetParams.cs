using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// WPN_GetParam* :Pawn 插件读取模板/玩家参数(与 wpn_const.inc 的参数类型一一对应)。
public static unsafe partial class Natives
{
    // ---- 参数读取 ----

    [AmxxNative("WPN_GetParamInt")]
    public static int GetParamInt(int weaponIndex, int id, int type)
    {
        int i = weaponIndex;
        switch (type)
        {
            case WpnConst.WpnID:
                return id is >= 0 and < 3 ? WeaponStore.Config.GetTemplateBySerial(id, i)
                    : WeaponGive.WeaponTypeByIndex(id);
            case WpnConst.WpnIAmount: return WeaponStore.WeaponAmount;
            case WpnConst.WpnIBloodColor: return WeaponStore.BloodColor;
            case WpnConst.WpnAllTeamBuy: return WeaponStore.AllTeamBuy;
            case WpnConst.WpnIMaxMoney: return WeaponStore.MaxMoney;
            case WpnConst.WpnIGameWeaponBPAmmo:
                return i >= 0 && i < WeaponStore.GameWeaponMaxBpammo.Length ? WeaponStore.GameWeaponMaxBpammo[i] : -1;
        }
        if (!WeaponStore.IsPlayerIndex(id)) return -1;
        switch (type)
        {
            case WpnConst.WpnSetBuy: return WeaponStore.Player(id).BlockBuy;
            case WpnConst.WpnIUserSight: return WeaponStore.Player(id).SightMode;
            case WpnConst.WpnIBotMoney: return WeaponStore.Player(id).BotMoney;
            case WpnConst.WpnIUserMoney: return PlayerStocks.GetUserMoney(id);
            case WpnConst.WpnIAttackEntity: return WeaponStore.Player(id).AttackerEntity;
            case WpnConst.WpnIGameCurWeaponBPAmmo:
                return i >= 0 && i < WeaponStore.Player(id).GameBpAmmo.Length ? WeaponStore.Player(id).GameBpAmmo[i] : -1;
        }
        if (!WeaponStore.IsTemplate(i)) return -1;
        switch (type)
        {
            case WpnConst.WpnIWeaponId: return WeaponStore.Weapon(id, i).Id;
            case WpnConst.WpnIExplodeSprId: return WeaponStore.Weapon(id, i).GrenadeSprId;
            case WpnConst.WpnIReloadMode: return WeaponStore.Weapon(id, i).ReloadMode;
            case WpnConst.WpnISightMode: return WeaponStore.Weapon(id, i).Sight;
            case WpnConst.WpnIClip: return WeaponStore.Weapon(id, i).Clip;
            case WpnConst.WpnIBPAmmo: return WeaponStore.Weapon(id, i).Ammo;
            case WpnConst.WpnICost: return WeaponStore.Weapon(id, i).Cost;
            case WpnConst.WpnIAmmoCost: return WeaponStore.Weapon(id, i).AmmoCost;
            case WpnConst.WpnITeam: return WeaponStore.Weapon(id, i).Team;
            case WpnConst.WpnIWModelBody: return WeaponStore.Weapon(id, i).WModelBody;
            case WpnConst.WpnIAnimIdle: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdle];
            case WpnConst.WpnIAnimReload: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimReload];
            case WpnConst.WpnIAnimDraw: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDraw];
            case WpnConst.WpnIAnimShoot1: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot1];
            case WpnConst.WpnIAnimShoot2: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot2];
            case WpnConst.WpnIAnimShoot3: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot3];
            case WpnConst.WpnIAnimStartReload: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimStartReload];
            case WpnConst.WpnIAnimInsert: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimInsert];
            case WpnConst.WpnIAnimAfterReload: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAfterReload];
            case WpnConst.WpnIAnimSightOpen: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimSightOpen];
            case WpnConst.WpnIAnimSightClose: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimSightClose];
            case WpnConst.WpnIAnimSilencedIdle: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdleSilenced];
            case WpnConst.WpnIAnimSilencedReload: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimReloadSilenced];
            case WpnConst.WpnIAnimSilencedDraw: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDrawSilenced];
            case WpnConst.WpnIAnimSilencedShoot1: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot1Silenced];
            case WpnConst.WpnIAnimSilencedShoot2: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot2Silenced];
            case WpnConst.WpnIAnimSilencedShoot3: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot3Silenced];
            case WpnConst.WpnIAnimDetachSilencer: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDetachSilencer];
            case WpnConst.WpnIAnimAttachSilencer: return WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAttachSilencer];
            case WpnConst.WpnIKnifeAnimIdle: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimIdle];
            case WpnConst.WpnIKnifeAnimSlash1: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash1];
            case WpnConst.WpnIKnifeAnimSlash2: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlash2];
            case WpnConst.WpnIKnifeAnimDraw: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw];
            case WpnConst.WpnIKnifeAnimStab: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab];
            case WpnConst.WpnIKnifeAnimStabMiss: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabMiss];
            case WpnConst.WpnIKnifeAnimMidSlash: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlash];
            case WpnConst.WpnIKnifeAnimMidSlashMiss: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlashMiss];
            case WpnConst.WpnIKnifeAnimSlashBegin: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin];
            case WpnConst.WpnIKnifeAnimStabBegin: return WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin];
            case WpnConst.WpnIUser1: return WeaponStore.Player(id).User1[i];
            case WpnConst.WpnIUser2: return WeaponStore.Player(id).User2[i];
            case WpnConst.WpnIUser3: return WeaponStore.Player(id).User3[i];
            case WpnConst.WpnIUser4: return WeaponStore.Player(id).User4[i];
            case WpnConst.WpnIAmount: return WeaponStore.WeaponAmount;
            case WpnConst.WpnID:
                if (id < 3) return WeaponStore.Config.GetTemplateBySerial(id, i);
                return WeaponGive.WeaponTypeByIndex(id);
            case WpnConst.WpnIBloodColor: return WeaponStore.BloodColor;
            case WpnConst.WpnAllTeamBuy: return WeaponStore.AllTeamBuy;
            case WpnConst.WpnSetBuy: return WeaponStore.Player(id).BlockBuy;
            case WpnConst.WpnIUserSight: return WeaponStore.Player(id).SightMode;
            case WpnConst.WpnIBotMoney: return WeaponStore.Player(id).BotMoney;
            case WpnConst.WpnIGameWeaponBPAmmo: return WeaponStore.GameWeaponMaxBpammo[i];
            case WpnConst.WpnIGameCurWeaponBPAmmo: return WeaponStore.Player(id).GameBpAmmo[i];
            case WpnConst.WpnICurWeaponBPAmmo: return WeaponStore.Player(id).BPAmmo[i];
            case WpnConst.WpnIMaxMoney: return WeaponStore.MaxMoney;
            case WpnConst.WpnIUserMoney: return PlayerStocks.GetUserMoney(id);
            case WpnConst.WpnIAttackEntity: return WeaponStore.Player(id).AttackerEntity;
        }
        return -1;
    }

    [AmxxNative("WPN_GetParamFloat")]
    public static float GetParamFloat(int weaponIndex, int id, int type)
    {
        if (!WeaponStore.IsPlayerIndex(id)) return -1.0f;
        switch (type)
        {
            case WpnConst.WpnFDamagePoint: return WeaponStore.Player(id).DamagePoint;
            case WpnConst.WpnFKnockBackPoint: return WeaponStore.Player(id).KnockBackPoint;
            case WpnConst.WpnFDamageVictimPoint: return WeaponStore.Player(id).DamageVictimPoint;
        }
        int i = weaponIndex;
        if (!WeaponStore.IsTemplate(i)) return -1.0f;

        switch (type)
        {
            case WpnConst.WpnFDamage: return WeaponStore.Weapon(id, i).Damage;
            case WpnConst.WpnFStabDamage: return WeaponStore.Weapon(id, i).Damage2;
            case WpnConst.WpnFFireSpeed: return WeaponStore.Weapon(id, i).Speed;
            case WpnConst.WpnFAimFireSpeed: return WeaponStore.Weapon(id, i).AimSpeed;
            case WpnConst.WpnFSilencedFireSpeed: return WeaponStore.Weapon(id, i).SilencedSpeed;
            case WpnConst.WpnFRecoil: return WeaponStore.Weapon(id, i).Recoil;
            case WpnConst.WpnFAimRecoil: return WeaponStore.Weapon(id, i).AimRecoil;
            case WpnConst.WpnFSilencedRecoil: return WeaponStore.Weapon(id, i).SilencedRecoil;
            case WpnConst.WpnFAccuracy: return WeaponStore.Weapon(id, i).Accuracy;
            case WpnConst.WpnFAimAccuracy: return WeaponStore.Weapon(id, i).AimAccuracy;
            case WpnConst.WpnFSilencedAccuracy: return WeaponStore.Weapon(id, i).SilencedAccuracy;
            case WpnConst.WpnFSightDistance: return WeaponStore.Weapon(id, i).SightDistance;
            case WpnConst.WpnFSightFramerate: return WeaponStore.Weapon(id, i).SightFrameRate;
            case WpnConst.WpnFSightOpenTime: return WeaponStore.Weapon(id, i).SightOpenTime;
            case WpnConst.WpnFSightCloseTime: return WeaponStore.Weapon(id, i).SightCloseTime;
            case WpnConst.WpnFReloadTime: return WeaponStore.Weapon(id, i).ReloadTime[0];
            case WpnConst.WpnFShotgunStartReloadTime: return WeaponStore.Weapon(id, i).ReloadTime[1];
            case WpnConst.WpnFShotgunAfterReloadTime: return WeaponStore.Weapon(id, i).ReloadTime[2];
            case WpnConst.WpnFDrawTime: return WeaponStore.Weapon(id, i).DrawTime;
            case WpnConst.WpnFDetachSilencerTime: return WeaponStore.Weapon(id, i).SilencedTime[1];
            case WpnConst.WpnFAttachSilencerTime: return WeaponStore.Weapon(id, i).SilencedTime[0];
            case WpnConst.WpnFSlashResetTime: return WeaponStore.Weapon(id, i).KnifeSlashResetTime;
            case WpnConst.WpnFSlashTime: return WeaponStore.Weapon(id, i).KnifeSlashTime;
            case WpnConst.WpnFSlashRange: return WeaponStore.Weapon(id, i).KnifeSlashRange;
            case WpnConst.WpnFSlashAngle: return WeaponStore.Weapon(id, i).KnifeSlashAngle;
            case WpnConst.WpnFStabResetTime: return WeaponStore.Weapon(id, i).KnifeStabResetTime;
            case WpnConst.WpnFStabTime: return WeaponStore.Weapon(id, i).KnifeStabTime;
            case WpnConst.WpnFStabRange: return WeaponStore.Weapon(id, i).KnifeStabRange;
            case WpnConst.WpnFStabAngle: return WeaponStore.Weapon(id, i).KnifeStabAngle;
            case WpnConst.WpnFExpTime: return WeaponStore.Weapon(id, i).GrenadeData[1];
            case WpnConst.WpnFExpRange: return WeaponStore.Weapon(id, i).GrenadeData[2];
            case WpnConst.WpnFMaxSpeed: return WeaponStore.Weapon(id, i).MaxSpeed;
            case WpnConst.WpnFAimMaxSpeed: return WeaponStore.Weapon(id, i).AimMaxSpeed;
            case WpnConst.WpnFKnockBack: return WeaponStore.Weapon(id, i).KnockBack;
            case WpnConst.WpnFUser1: return WeaponStore.Player(id).FUser1[i];
            case WpnConst.WpnFUser2: return WeaponStore.Player(id).FUser2[i];
            case WpnConst.WpnFUser3: return WeaponStore.Player(id).FUser3[i];
            case WpnConst.WpnFUser4: return WeaponStore.Player(id).FUser4[i];
            case WpnConst.WpnFDamagePoint: return WeaponStore.Player(id).DamagePoint;
            case WpnConst.WpnFKnockBackPoint: return WeaponStore.Player(id).KnockBackPoint;
            case WpnConst.WpnFDamageVictimPoint: return WeaponStore.Player(id).DamageVictimPoint;
            case WpnConst.WpnFFireSoundVolume: return WeaponStore.Weapon(id, i).FireSoundVolume;
            case WpnConst.WpnFSilenceFireSoundVolume: return WeaponStore.Weapon(id, i).SilenceFireSoundVolume;
            case WpnConst.WpnFKnifeSoundVolume1: return WeaponStore.Weapon(id, i).KnifeSoundVolume[0];
            case WpnConst.WpnFKnifeSoundVolume2: return WeaponStore.Weapon(id, i).KnifeSoundVolume[1];
            case WpnConst.WpnFKnifeSoundVolume3: return WeaponStore.Weapon(id, i).KnifeSoundVolume[2];
            case WpnConst.WpnFKnifeSoundVolume4: return WeaponStore.Weapon(id, i).KnifeSoundVolume[3];
            case WpnConst.WpnFKnifeSoundVolume5: return WeaponStore.Weapon(id, i).KnifeSoundVolume[4];
            case WpnConst.WpnFKnifeSlashAngleOffset: return WeaponStore.Weapon(id, i).KnifeSlashAngleOffset;
            case WpnConst.WpnFKnifeStabAngleOffset: return WeaponStore.Weapon(id, i).KnifeStabAngleOffset;
            case WpnConst.WpnFKnifeSlashHeight: return WeaponStore.Weapon(id, i).KnifeSlashHeight;
            case WpnConst.WpnFKnifeStabHeight: return WeaponStore.Weapon(id, i).KnifeStabHeight;
            case WpnConst.WpnFKnifeSlashHeightOffset: return WeaponStore.Weapon(id, i).KnifeSlashHeightOffset;
            case WpnConst.WpnFKnifeStabHeightOffset: return WeaponStore.Weapon(id, i).KnifeStabHeightOffset;
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
        if (!WeaponStore.IsPlayerIndex(id) || !WeaponStore.IsTemplate(i)) return;

        switch (type)
        {
            case WpnConst.WpnSzWeaponList: value = WeaponStore.Weapon(id, i).List; break;
            case WpnConst.WpnSzKillSpr: value = WeaponStore.Weapon(id, i).Spr; break;
            case WpnConst.WpnSzPModel: value = WeaponStore.Weapon(id, i).PModel; break;
            case WpnConst.WpnSzVModel: value = WeaponStore.Weapon(id, i).VModel; break;
            case WpnConst.WpnSzWModel: value = WeaponStore.Weapon(id, i).WModel; break;
            case WpnConst.WpnSzSightModel: value = WeaponStore.Weapon(id, i).SightModel; break;
            case WpnConst.WpnSzSound1: value = WeaponStore.Weapon(id, i).Sound1; break;
            case WpnConst.WpnSzSound2: value = WeaponStore.Weapon(id, i).Sound2; break;
            case WpnConst.WpnSzSound3: value = WeaponStore.Weapon(id, i).Sound3; break;
            case WpnConst.WpnSzSound4: value = WeaponStore.Weapon(id, i).Sound4; break;
            case WpnConst.WpnSzSound5: value = WeaponStore.Weapon(id, i).Sound5; break;
            case WpnConst.WpnSzSound6: value = WeaponStore.Weapon(id, i).Sound6; break;
            case WpnConst.WpnSzCommand: value = WeaponStore.Weapon(id, i).BuyCommand; break;
            case WpnConst.WpnSzUser1: value = WeaponStore.Player(id).SzUser1[i]; break;
            case WpnConst.WpnSzUser2: value = WeaponStore.Player(id).SzUser2[i]; break;
            case WpnConst.WpnSzUser3: value = WeaponStore.Player(id).SzUser3[i]; break;
            case WpnConst.WpnSzUser4: value = WeaponStore.Player(id).SzUser4[i]; break;
        }
    }

}
