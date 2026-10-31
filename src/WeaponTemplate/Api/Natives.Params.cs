using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

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
