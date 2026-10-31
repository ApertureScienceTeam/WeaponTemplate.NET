using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// RegisterHam 开火回调:主/副武器攻击及其 Post。
public static unsafe partial class Attack
{
    [AmxxCallback("wt_WeaponPrimaryAttack")]
    public static int OnWeaponPrimaryAttack(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswHegrenade) return EngineConst.HamIgnored;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            int preResult = NativeFunctionRegister.Execute_WpnPrimaryPreAttack(entityIndex, id, i);
            if (preResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

            if (WeaponStore.Weapon(id, i).KnifeSlashTime == 0.0f)
            {
                WeaponStore.Player(id).KnifeSlashMode = WeaponStore.Player(id).KnifeSlashMode != 0 ? 0 : 1;
                WeaponStore.Player(id).KnifeTempDamage = WeaponStore.Weapon(id, i).Damage;
                if (WeaponStore.Player(id).UserKnifeAttack == 0) WeaponStore.Player(id).UserKnifeAttack = 1;
                NativeFunctionRegister.Execute_WpnPrimaryPreAttackPost(entityIndex, id, i);
                return EngineConst.HamIgnored;
            }
            else
            {
                if (WeaponStore.Player(id).KnifeAttack != 0) return EngineConst.HamSupercede;

                WeaponStore.Player(id).KnifeSlashMode = WeaponStore.Player(id).KnifeSlashMode != 0 ? 0 : 1;
                WeaponStore.Player(id).KnifeAttack = 1;
                Attack.KnifeAttack(id, i, WeaponStore.Player(id).KnifeAttack, entity);
            }
            NativeFunctionRegister.Execute_WpnPrimaryPreAttackPost(entityIndex, id, i);
            return EngineConst.HamSupercede;
        }

        int result = NativeFunctionRegister.Execute_WpnPrimaryPreAttack(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload) != 0) Engine.SetPDataInt(entity, CsOffsets.ItemInSpecialReload, 0);
        if (result == WpnConst.WPN_HANDLED)
        {
            // WPN_HANDLED:打断原版攻击并执行一次假攻击
            if (Engine.GetPDataInt(entity, CsOffsets.ItemClip) <= 0) return EngineConst.HamIgnored;

            bool silenced = (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 2)) != 0 || (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 0)) != 0;

            var push = new float[] { 0.0f, 0.0f, 0.0f };
            if (silenced)
            {
                if (Engine.RandomNum(0, 1) != 0)
                    Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
                else
                    Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
            }
            else
            {
                if (Engine.RandomNum(0, 1) != 0)
                    Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
                else
                    Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
            }
            if (PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open)
            {
                push[0] = -1.0f * WeaponStore.Weapon(id, i).AimRecoil;
                Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).AimSpeed);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).AimSpeed);
            }
            else if (PlayerStocks.GetUserZoom(id) == 1 && WeaponStore.Player(id).SightMode != WpnConst.Open)
            {
                push[0] = -1.0f * WeaponStore.Weapon(id, i).Recoil;
                Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).Speed);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).Speed);
            }
            else if (silenced)
            {
                push[0] = -1.0f * WeaponStore.Weapon(id, i).SilencedRecoil;
                Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).SilencedSpeed);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).SilencedSpeed);
            }
            pPlayer->v.punchangle = new vec3_t { x = push[0], y = push[1], z = push[2] };
            if (silenced) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Engine.RandomNum(WpnConst.AnimShoot1Silenced, WpnConst.AnimShoot3Silenced)]);
            else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswElite)
            {
                if (WeaponStore.Player(id).EliteShotNumber == 0)
                {
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot1]);
                    WeaponStore.Player(id).EliteShotNumber = 1;
                }
                else
                {
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot2]);
                    WeaponStore.Player(id).EliteShotNumber = 0;
                }
            }
            else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Engine.RandomNum(WpnConst.AnimShoot1, WpnConst.AnimShoot3)]);
            if (Engine.GetPDataInt(entity, CsOffsets.ItemClip) > 0)
            {
                int postResult = NativeFunctionRegister.Execute_WpnPrimaryPostAttack(entityIndex, id, i);
                if (postResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;
            }
            Engine.SetPDataInt(entity, CsOffsets.ItemClip, Engine.GetPDataInt(entity, CsOffsets.ItemClip) - 1);
            NativeFunctionRegister.Execute_WpnPrimaryPostAttackPost(entityIndex, id, i);
            return EngineConst.HamSupercede;
        }
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM3 || WeaponStore.Weapon(id, i).Id == EngineConst.CswXm1014)
        {
            if (WeaponStore.Player(id).SightMode == WpnConst.AtoB || WeaponStore.Player(id).SightMode == WpnConst.BtoA)
                return EngineConst.HamSupercede;
        }
        WeaponStore.IsInPrimaryAttack = true;
        WeaponStore.Player(id).PushAngle[0] = pPlayer->v.punchangle.x;
        WeaponStore.Player(id).PushAngle[1] = pPlayer->v.punchangle.y;
        WeaponStore.Player(id).PushAngle[2] = pPlayer->v.punchangle.z;
        WeaponStore.Player(id).ClipAmmo = Engine.GetPDataInt(entity, CsOffsets.ItemClip);
        NativeFunctionRegister.Execute_WpnPrimaryPreAttackPost(entityIndex, id, i);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponPrimaryAttackPost")]
    public static void OnWeaponPrimaryAttackPost(int entityIndex)
    {
        bool wasInPrimaryAttack = WeaponStore.IsInPrimaryAttack;
        WeaponStore.IsInPrimaryAttack = false;
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (WeaponStore.Weapon(id, i).KnifeSlashTime == 0.0f)
            {
                Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
            }
            return;
        }
        if (!wasInPrimaryAttack) return;

        if (Engine.GetPDataInt(entity, CsOffsets.ItemClip) > 0)
        {
            int preResult = NativeFunctionRegister.Execute_WpnPrimaryPostAttack(entityIndex, id, i);
            if (preResult == WpnConst.WPN_SUPERCEDE) return;
        }
        if (WeaponStore.Player(id).ClipAmmo == 0 || WeaponStore.Player(id).ClipAmmo == Engine.GetPDataInt(entity, CsOffsets.ItemClip)) return;

        bool silenced = (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 2)) != 0 || (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 0)) != 0;

        var push = new float[] { pPlayer->v.punchangle.x, pPlayer->v.punchangle.y, pPlayer->v.punchangle.z };
        push[0] -= WeaponStore.Player(id).PushAngle[0];
        push[1] -= WeaponStore.Player(id).PushAngle[1];
        push[2] -= WeaponStore.Player(id).PushAngle[2];
        if (silenced)
        {
            if (Engine.RandomNum(0, 1) != 0)
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
            else
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
        }
        else
        {
            if (Engine.RandomNum(0, 1) != 0)
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
            else
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
        }
        if ((PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open) && !silenced)
        {
            push[0] *= WeaponStore.Weapon(id, i).AimRecoil;
            push[1] *= WeaponStore.Weapon(id, i).AimRecoil;
            push[2] *= WeaponStore.Weapon(id, i).AimRecoil;
            Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).AimSpeed);
            Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).AimSpeed);
        }
        else if (PlayerStocks.GetUserZoom(id) == 1 && WeaponStore.Player(id).SightMode != WpnConst.Open && !silenced)
        {
            push[0] *= WeaponStore.Weapon(id, i).Recoil;
            push[1] *= WeaponStore.Weapon(id, i).Recoil;
            push[2] *= WeaponStore.Weapon(id, i).Recoil;
            Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).Speed);
            Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).Speed);
        }
        else if (silenced)
        {
            push[0] *= WeaponStore.Weapon(id, i).SilencedRecoil;
            push[1] *= WeaponStore.Weapon(id, i).SilencedRecoil;
            push[2] *= WeaponStore.Weapon(id, i).SilencedRecoil;
            Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).SilencedSpeed);
            Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).SilencedSpeed);
        }
        push[0] += WeaponStore.Player(id).PushAngle[0];
        push[1] += WeaponStore.Player(id).PushAngle[1];
        push[2] += WeaponStore.Player(id).PushAngle[2];
        pPlayer->v.punchangle = new vec3_t { x = push[0], y = push[1], z = push[2] };
        if (silenced) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Engine.RandomNum(WpnConst.AnimShoot1Silenced, WpnConst.AnimShoot3Silenced)]);
        else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswElite)
        {
            if (WeaponStore.Player(id).EliteShotNumber == 0)
            {
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot1]);
                WeaponStore.Player(id).EliteShotNumber = 1;
            }
            else
            {
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimShoot2]);
                WeaponStore.Player(id).EliteShotNumber = 0;
            }
        }
        else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Engine.RandomNum(WpnConst.AnimShoot1, WpnConst.AnimShoot3)]);
        NativeFunctionRegister.Execute_WpnPrimaryPostAttackPost(entityIndex, id, i);
    }

    [AmxxCallback("wt_WeaponSecondaryAttack")]
    public static int OnWeaponSecondaryAttack(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswHegrenade) return EngineConst.HamIgnored;

        int result = NativeFunctionRegister.Execute_WpnSecondaryPreAttack(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (WeaponStore.Weapon(id, i).KnifeStabTime == 0.0f)
            {
                WeaponStore.Player(id).KnifeTempDamage = WeaponStore.Weapon(id, i).Damage2;
                if (WeaponStore.Player(id).UserKnifeAttack == 0) WeaponStore.Player(id).UserKnifeAttack = 2;
                NativeFunctionRegister.Execute_WpnSecondaryPreAttackPost(entityIndex, id, i);
                return EngineConst.HamIgnored;
            }
            else
            {
                if (WeaponStore.Player(id).KnifeAttack != 0) return EngineConst.HamSupercede;

                WeaponStore.Player(id).KnifeAttack = 3;
                Attack.KnifeAttack(id, i, WeaponStore.Player(id).KnifeAttack, entity);
            }
            NativeFunctionRegister.Execute_WpnSecondaryPreAttackPost(entityIndex, id, i);
            return EngineConst.HamSupercede;
        }
        if (WeaponStore.Weapon(id, i).Sight != 0) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).Id is EngineConst.CswM4a1 or EngineConst.CswUsp or EngineConst.CswFamas or EngineConst.CswGlock18)
            return EngineConst.HamSupercede;

        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponSecondaryAttackPost")]
    public static void OnWeaponSecondaryAttackPost(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return;

        int result = NativeFunctionRegister.Execute_WpnSecondaryPostAttack(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (WeaponStore.Weapon(id, i).KnifeStabTime == 0.0f)
            {
                Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).KnifeStabResetTime);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack, WeaponStore.Weapon(id, i).KnifeStabResetTime);
            }
            return;
        }
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswHegrenade) return;

        if (WeaponStore.Weapon(id, i).Sight != 0) return;

        if (PlayerStocks.GetUserZoom(id) == 2 || PlayerStocks.GetUserZoom(id) == 3) Engine.SetViewModel(pPlayer, "");
        else
        {
            Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdle]);
        }
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM4a1 || WeaponStore.Weapon(id, i).Id == EngineConst.CswUsp)
        {
            if ((Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 2)) != 0 || (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 0)) != 0)
            {
                if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM4a1) Engine.SetPDataInt(entity, CsOffsets.ItemWeaponState, Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & ~(1 << 2));
                else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswUsp) Engine.SetPDataInt(entity, CsOffsets.ItemWeaponState, Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & ~(1 << 0));
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDetachSilencer]);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).SilencedTime[1]);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack, WeaponStore.Weapon(id, i).SilencedTime[1]);
                Engine.SetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle, WeaponStore.Weapon(id, i).SilencedTime[1]);
                Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).SilencedTime[1]);
            }
            else
            {
                if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM4a1) Engine.SetPDataInt(entity, CsOffsets.ItemWeaponState, Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) | (1 << 2));
                else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswUsp) Engine.SetPDataInt(entity, CsOffsets.ItemWeaponState, Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) | (1 << 0));
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAttachSilencer]);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).SilencedTime[0]);
                Engine.SetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack, WeaponStore.Weapon(id, i).SilencedTime[0]);
                Engine.SetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle, WeaponStore.Weapon(id, i).SilencedTime[0]);
                Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).SilencedTime[0]);
            }
        }
        NativeFunctionRegister.Execute_WpnSecondaryPostAttackPost(entityIndex, id, i);
    }

}
