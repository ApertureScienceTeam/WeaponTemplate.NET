using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// RegisterHam 注册的武器/玩家回调:部署、收起、攻击、装填、待机、伤害、手雷思考等。
// 返回 HAM_IGNORED(1)/HAM_SUPERCEDE(4);注册列表见 Lifecycle/PluginLifecycle.cs。
public static unsafe partial class Callbacks
{
    [AmxxCallback("wt_ItemAddToPlayerPost")]
    public static void OnItemAddToPlayerPost(int entityIndex, int id)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        if (Eng.GetPDataInt(entity, 43) == EngineConst.CswC4) return;
        WeaponGive.ChangeWeaponList(id, entity);
    }

    [AmxxCallback("wt_ItemDeploy")]
    public static int OnItemDeploy(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        // AWP无法设置切枪时间的bug
        WeaponStore.Player(id).UserDeploy[0] = Eng.GetPDataFloat(entity, 46);
        WeaponStore.Player(id).UserDeploy[1] = Eng.GetPDataFloat(entity, 47);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_ItemDeployPost")]
    public static void OnItemDeployPost(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);

        if (i == 0)
        {
            // 记录原版武器备弹
            int weapon = Eng.GetPDataInt(entity, 43);
            const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            if (((1 << weapon) & specialBitSum) == 0) PlayerStocks.SetUserBpAmmo(id, weapon, WeaponStore.Player(id).GameBpAmmo[weapon]);
            return;
        }

        // 记录新武器备弹
        if (WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && WeaponStore.Weapon(id, i).Id != EngineConst.CswKnife)
            PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, WeaponStore.Player(id).BPAmmo[i]);
        WeaponGive.ChangeWeaponList(id, entity);
        Eng.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
        Eng.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw]);
            Eng.EmitSound(pPlayer, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
        }
        else if (WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade)
        {
            if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDrawSilenced]);
            else
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDraw]);
        }
        Eng.SetPDataFloat(pPlayer, 83, WeaponStore.Weapon(id, i).DrawTime);
        Eng.SetPDataFloat(entity, 46, WeaponStore.Player(id).UserDeploy[0]);
        Eng.SetPDataFloat(entity, 47, WeaponStore.Player(id).UserDeploy[1]);
        if (WeaponStore.Weapon(id, i).Clip == 0 && WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && WeaponStore.Weapon(id, i).Id != EngineConst.CswKnife)
        {
            int clip = Eng.GetPDataInt(entity, 51);
            PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, clip);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("CurWeapon"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.WriteByte(WeaponStore.Weapon(id, i).Id);
            Eng.WriteByte(0xFF); // -1
            Eng.MessageEnd();
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoX"), null, pPlayer);
            Eng.WriteByte(GameTables.GameWeaponAmmoId[WeaponStore.Weapon(id, i).Id]);
            Eng.WriteByte(clip);
            Eng.MessageEnd();
        }
        else
        {
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("CurWeapon"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.WriteByte(WeaponStore.Weapon(id, i).Id);
            Eng.WriteByte(Eng.GetPDataInt(entity, 51));
            Eng.MessageEnd();
        }
        if (WeaponStore.Weapon(id, i).Sight != 0)
        {
            Eng.SetPDataInt(pPlayer, 363, 90);
            WeaponStore.Player(id).SightMode = WpnConst.Closed;
            Eng.SetPDataInt(pPlayer, 361, Eng.GetPDataInt(pPlayer, 361) & ~(1 << 6));
            WeaponStore.Player(id).Reloading = false;
        }
        NativeFunctionRegister.Execute_WpnWeaponDraw(entityIndex, id, i);
    }

    [AmxxCallback("wt_ItemHolsterPost")]
    public static void OnItemHolsterPost(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i != 0 && WeaponStore.Weapon(id, i).Sight != 0)
        {
            Eng.SetPDataInt(pPlayer, 363, 90);
            WeaponStore.Player(id).SightMode = WpnConst.Closed;
            Eng.SetPDataInt(pPlayer, 361, Eng.GetPDataInt(pPlayer, 361) & ~(1 << 6));
            WeaponStore.Player(id).Reloading = false;
        }

        // 收起武器时记录备用子弹
        int weapon = Eng.GetPDataInt(entity, 43);
        const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
        if (((1 << weapon) & specialBitSum) == 0)
        {
            if (i == 0) WeaponStore.Player(id).GameBpAmmo[weapon] = PlayerStocks.GetUserBpAmmo(id, weapon);
            else WeaponStore.Player(id).BPAmmo[i] = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
        }
    }

    [AmxxCallback("wt_WeaponPrimaryAttack")]
    public static int OnWeaponPrimaryAttack(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
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

        if (Eng.GetPDataInt(entity, 55) != 0) Eng.SetPDataInt(entity, 55, 0);
        if (result == WpnConst.WPN_HANDLED)
        {
            // WPN_HANDLED:打断原版攻击并执行一次假攻击
            if (Eng.GetPDataInt(entity, 51) <= 0) return EngineConst.HamIgnored;

            bool silenced = (Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0;

            var push = new float[] { 0.0f, 0.0f, 0.0f };
            if (silenced)
            {
                if (Eng.RandomNum(0, 1) != 0)
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
                else
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
            }
            else
            {
                if (Eng.RandomNum(0, 1) != 0)
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
                else
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
            }
            if (PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open)
            {
                push[0] = -1.0f * WeaponStore.Weapon(id, i).AimRecoil;
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).AimSpeed);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).AimSpeed);
            }
            else if (PlayerStocks.GetUserZoom(id) == 1 && WeaponStore.Player(id).SightMode != WpnConst.Open)
            {
                push[0] = -1.0f * WeaponStore.Weapon(id, i).Recoil;
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).Speed);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).Speed);
            }
            else if (silenced)
            {
                push[0] = -1.0f * WeaponStore.Weapon(id, i).SilencedRecoil;
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).SilencedSpeed);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).SilencedSpeed);
            }
            pPlayer->v.punchangle = new vec3_t { x = push[0], y = push[1], z = push[2] };
            if (silenced) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1Silenced, WpnConst.AnimShoot3Silenced)]);
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
            else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1, WpnConst.AnimShoot3)]);
            if (Eng.GetPDataInt(entity, 51) > 0)
            {
                int postResult = NativeFunctionRegister.Execute_WpnPrimaryPostAttack(entityIndex, id, i);
                if (postResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;
            }
            Eng.SetPDataInt(entity, 51, Eng.GetPDataInt(entity, 51) - 1);
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
        WeaponStore.Player(id).ClipAmmo = Eng.GetPDataInt(entity, 51);
        NativeFunctionRegister.Execute_WpnPrimaryPreAttackPost(entityIndex, id, i);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponPrimaryAttackPost")]
    public static void OnWeaponPrimaryAttackPost(int entityIndex)
    {
        bool wasInPrimaryAttack = WeaponStore.IsInPrimaryAttack;
        WeaponStore.IsInPrimaryAttack = false;
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (WeaponStore.Weapon(id, i).KnifeSlashTime == 0.0f)
            {
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
            }
            return;
        }
        if (!wasInPrimaryAttack) return;

        if (Eng.GetPDataInt(entity, 51) > 0)
        {
            int preResult = NativeFunctionRegister.Execute_WpnPrimaryPostAttack(entityIndex, id, i);
            if (preResult == WpnConst.WPN_SUPERCEDE) return;
        }
        if (WeaponStore.Player(id).ClipAmmo == 0 || WeaponStore.Player(id).ClipAmmo == Eng.GetPDataInt(entity, 51)) return;

        bool silenced = (Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0;

        var push = new float[] { pPlayer->v.punchangle.x, pPlayer->v.punchangle.y, pPlayer->v.punchangle.z };
        push[0] -= WeaponStore.Player(id).PushAngle[0];
        push[1] -= WeaponStore.Player(id).PushAngle[1];
        push[2] -= WeaponStore.Player(id).PushAngle[2];
        if (silenced)
        {
            if (Eng.RandomNum(0, 1) != 0)
                Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
            else
                Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
        }
        else
        {
            if (Eng.RandomNum(0, 1) != 0)
                Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
            else
                Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
        }
        if ((PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open) && !silenced)
        {
            push[0] *= WeaponStore.Weapon(id, i).AimRecoil;
            push[1] *= WeaponStore.Weapon(id, i).AimRecoil;
            push[2] *= WeaponStore.Weapon(id, i).AimRecoil;
            Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).AimSpeed);
            Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).AimSpeed);
        }
        else if (PlayerStocks.GetUserZoom(id) == 1 && WeaponStore.Player(id).SightMode != WpnConst.Open && !silenced)
        {
            push[0] *= WeaponStore.Weapon(id, i).Recoil;
            push[1] *= WeaponStore.Weapon(id, i).Recoil;
            push[2] *= WeaponStore.Weapon(id, i).Recoil;
            Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).Speed);
            Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).Speed);
        }
        else if (silenced)
        {
            push[0] *= WeaponStore.Weapon(id, i).SilencedRecoil;
            push[1] *= WeaponStore.Weapon(id, i).SilencedRecoil;
            push[2] *= WeaponStore.Weapon(id, i).SilencedRecoil;
            Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).SilencedSpeed);
            Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).SilencedSpeed);
        }
        push[0] += WeaponStore.Player(id).PushAngle[0];
        push[1] += WeaponStore.Player(id).PushAngle[1];
        push[2] += WeaponStore.Player(id).PushAngle[2];
        pPlayer->v.punchangle = new vec3_t { x = push[0], y = push[1], z = push[2] };
        if (silenced) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1Silenced, WpnConst.AnimShoot3Silenced)]);
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
        else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1, WpnConst.AnimShoot3)]);
        NativeFunctionRegister.Execute_WpnPrimaryPostAttackPost(entityIndex, id, i);
    }

    [AmxxCallback("wt_WeaponSecondaryAttack")]
    public static int OnWeaponSecondaryAttack(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
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
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return;

        int result = NativeFunctionRegister.Execute_WpnSecondaryPostAttack(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (WeaponStore.Weapon(id, i).KnifeStabTime == 0.0f)
            {
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeStabResetTime);
                Eng.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).KnifeStabResetTime);
            }
            return;
        }
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswHegrenade) return;

        if (WeaponStore.Weapon(id, i).Sight != 0) return;

        if (PlayerStocks.GetUserZoom(id) == 2 || PlayerStocks.GetUserZoom(id) == 3) Eng.SetViewModel(pPlayer, "");
        else
        {
            Eng.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdle]);
        }
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM4a1 || WeaponStore.Weapon(id, i).Id == EngineConst.CswUsp)
        {
            if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
            {
                if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM4a1) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) & ~(1 << 2));
                else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswUsp) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) & ~(1 << 0));
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDetachSilencer]);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).SilencedTime[1]);
                Eng.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).SilencedTime[1]);
                Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).SilencedTime[1]);
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).SilencedTime[1]);
            }
            else
            {
                if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM4a1) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) | (1 << 2));
                else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswUsp) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) | (1 << 0));
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAttachSilencer]);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).SilencedTime[0]);
                Eng.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).SilencedTime[0]);
                Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).SilencedTime[0]);
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).SilencedTime[0]);
            }
        }
        NativeFunctionRegister.Execute_WpnSecondaryPostAttackPost(entityIndex, id, i);
    }

    [AmxxCallback("wt_ItemPostFrame")]
    public static int OnItemPostFrame(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return EngineConst.HamIgnored;

        if (i == 0)
        {
            // 记录原版武器的换弹
            int weapon = Eng.GetPDataInt(entity, 43);
            const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            if (((1 << weapon) & specialBitSum) == 0)
            {
                if (weapon != EngineConst.CswM3 && weapon != EngineConst.CswXm1014)
                {
                    if (Eng.GetPDataInt(entity, 54) != 0 && Eng.GetPDataFloat(id, 83) <= 0.0f)
                        WeaponStore.Player(id).GameBpAmmo[weapon] = PlayerStocks.GetUserBpAmmo(id, weapon);
                }
                else if (Eng.GetPDataInt(entity, 55) == 1 && Eng.GetPDataFloat(entity, 48) <= 0.0f)
                    WeaponStore.Player(id).GameBpAmmo[weapon] = PlayerStocks.GetUserBpAmmo(id, weapon);
            }
            return EngineConst.HamIgnored;
        }

        int forwardResult = NativeFunctionRegister.Execute_WpnWeaponPostFrame(entityIndex, id, i);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return EngineConst.HamIgnored;
        if (WeaponStore.Weapon(id, i).Clip == 0) return EngineConst.HamIgnored;

        if (!WeaponStore.Player(id).Reloading)
        {
            int clipExtra = WeaponStore.Weapon(id, i).Clip;
            float flNextAttack = Eng.GetPDataFloat(id, 83);
            int bpAmmo = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
            int clip = Eng.GetPDataInt(entity, 51);
            int inReload = Eng.GetPDataInt(entity, 54);
            if (inReload != 0 && flNextAttack <= 0.0f && WeaponStore.Weapon(id, i).ReloadMode == 0)
            {
                int j = Math.Min(clipExtra - clip, bpAmmo);
                Eng.SetPDataInt(entity, 51, clip + j);
                PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, bpAmmo - j);
                WeaponStore.Player(id).BPAmmo[i] = bpAmmo - j;
                Eng.SetPDataInt(entity, 54, 0);
                inReload = 0;
            }
            if (WeaponStore.Weapon(id, i).ReloadMode != 0)
            {
                int button = pPlayer->v.button;
                if ((button & EngineConst.InAttack) != 0 && Eng.GetPDataFloat(entity, 46) <= 0.0f && Eng.GetPDataInt(entity, 51) != 0)
                    return EngineConst.HamIgnored;

                if (Eng.GetPDataInt(entity, 51) == 0 && PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id) != 0)
                {
                    pPlayer->v.button = button & ~EngineConst.InAttack;
                    if (Eng.GetPDataInt(entity, 55) == 0 && Eng.GetPDataInt(entity, 54) == 0)
                    {
                        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                        {
                            if (WeaponStore.Player(id).SightMode != WpnConst.Open) return EngineConst.HamIgnored;

                            WeaponStore.Player(id).Reloading = true;
                            Attack.BeginClosing(entity);
                            return EngineConst.HamIgnored;
                        }
                    }
                }
                int tempClip = 0;
                if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM3) tempClip = 8;
                else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswXm1014) tempClip = 7;
                if ((button & EngineConst.InReload) != 0 && inReload == 0 && tempClip != 0)
                {
                    if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                    {
                        if (WeaponStore.Player(id).SightMode != WpnConst.Open) return EngineConst.HamIgnored;

                        WeaponStore.Player(id).Reloading = true;
                        Attack.BeginClosing(entity);
                        return EngineConst.HamIgnored;
                    }
                    if (clip >= clipExtra) pPlayer->v.button = button & ~EngineConst.InReload;
                    else if (clip == tempClip)
                    {
                        if (bpAmmo == 0) return EngineConst.HamIgnored;

                        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed) return EngineConst.HamIgnored;

                        // 子弹数和原版一样时，按R键装一发子弹，然后触发idle的装弹
                        WeaponAmmo.ShotgunReload(entity, clipExtra, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);
                    }
                }
            }
        }
        if (WeaponStore.Weapon(id, i).Sight != 0)
        {
            if (Eng.GetPDataInt(entity, 54) != 0 || Eng.GetPDataInt(entity, 55) != 0) return EngineConst.HamIgnored;

            if (Eng.GetPDataFloat(entity, 47) > 0.0f)
            {
                float curTime = Eng.GP->time;
                if (curTime <= WeaponStore.Player(id).NextThink) return EngineConst.HamIgnored;

                if (WeaponStore.Weapon(id, i).SightFrameRate <= 0.0f || WeaponStore.Weapon(id, i).SightDistance <= 0.0f) return EngineConst.HamIgnored;

                float valve = WeaponStore.Weapon(id, i).SightFrameRate;
                if (valve < 1.0f) valve = 1.0f;
                if (WeaponStore.Player(id).SightMode == WpnConst.AtoB)
                {
                    Eng.SetPDataInt(pPlayer, 363, Math.Max(Eng.GetPDataInt(pPlayer, 363) - PawnMath.Round(valve), PawnMath.Round(WeaponStore.Weapon(id, i).SightDistance)));
                    WeaponStore.Player(id).NextThink = curTime + WeaponStore.Weapon(id, i).SightOpenTime / ((90.0f - WeaponStore.Weapon(id, i).SightDistance) / WeaponStore.Weapon(id, i).SightFrameRate);
                }
                else if (WeaponStore.Player(id).SightMode == WpnConst.BtoA)
                {
                    Eng.SetPDataInt(pPlayer, 363, Math.Min(Eng.GetPDataInt(pPlayer, 363) + PawnMath.Round(valve), 90));
                    WeaponStore.Player(id).NextThink = curTime + WeaponStore.Weapon(id, i).SightCloseTime / ((90.0f - WeaponStore.Weapon(id, i).SightDistance) / WeaponStore.Weapon(id, i).SightFrameRate);
                }
                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Player(id).SightMode == WpnConst.AtoB)
            {
                Attack.OpeningEnd(entity);
                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Player(id).SightMode == WpnConst.BtoA)
            {
                Attack.ClosingEnd(entity);
                return EngineConst.HamIgnored;
            }
            int button = pPlayer->v.button;
            if ((button & EngineConst.InAttack2) == 0)
            {
                WeaponStore.Player(id).OldButton = false;
                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Player(id).OldButton) return EngineConst.HamSupercede;

            WeaponStore.Player(id).OldButton = true;
            if ((button & EngineConst.InAttack) != 0) return EngineConst.HamSupercede;

            if (Eng.GetPDataFloat(entity, 46) > 0.0f) return EngineConst.HamSupercede;

            if (WeaponStore.Player(id).SightMode == WpnConst.Closed)
            {
                Attack.BeginOpening(entity);
                return EngineConst.HamSupercede;
            }
            Attack.BeginClosing(entity);
            return EngineConst.HamSupercede;
        }
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponReload")]
    public static int OnWeaponReload(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return EngineConst.HamIgnored;
        if (i == 0) return EngineConst.HamIgnored;
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return EngineConst.HamIgnored;
        if (WeaponStore.Weapon(id, i).Clip == 0) return EngineConst.HamSupercede;

        int result = NativeFunctionRegister.Execute_WpnWeaponReload(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed)
        {
            if (WeaponStore.Player(id).SightMode != WpnConst.Open) return EngineConst.HamSupercede;

            WeaponStore.Player(id).Reloading = true;
            Attack.BeginClosing(entity);
            return EngineConst.HamSupercede;
        }
        int bpAmmo = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
        int clip = Eng.GetPDataInt(entity, 51);
        int inSpecialReload = Eng.GetPDataInt(entity, 55);

        // 在ShotgunReload里判断并设置换弹状态，fInSpecReload为2时忽略，使用原版的装填子弹
        if (WeaponStore.Weapon(id, i).ReloadMode != 0)
        {
            if (inSpecialReload < 2 && (WeaponStore.Weapon(id, i).Id == EngineConst.CswM3 || WeaponStore.Weapon(id, i).Id == EngineConst.CswXm1014))
                WeaponAmmo.ShotgunReload(entity, WeaponStore.Weapon(id, i).Clip, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);

            if (WeaponStore.Weapon(id, i).Id != EngineConst.CswM3 && WeaponStore.Weapon(id, i).Id != EngineConst.CswXm1014)
            {
                WeaponAmmo.ShotgunReload(entity, WeaponStore.Weapon(id, i).Clip, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);
                return EngineConst.HamSupercede;
            }
        }
        int clipExtra = WeaponStore.Weapon(id, i).Clip;
        WeaponStore.Player(id).TmpClip[i] = -1;
        if (bpAmmo <= 0) return EngineConst.HamSupercede;

        if (clip >= clipExtra) return EngineConst.HamSupercede;

        WeaponStore.Player(id).TmpClip[i] = clip;
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponReloadPost")]
    public static void OnWeaponReloadPost(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return;
        if (i == 0) return;
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return;

        int result = NativeFunctionRegister.Execute_WpnWeaponReloadPost(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).Reloading) return;
        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed) return;

        int inSpecialReload = Eng.GetPDataInt(entity, 55);
        if (WeaponStore.Weapon(id, i).ReloadMode != 0)
        {
            if (inSpecialReload == 1) WeaponStore.Player(id).BPAmmo[i] = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
            return;
        }
        if (WeaponStore.Weapon(id, i).Clip == 0) return;

        int tmpClip = WeaponStore.Player(id).TmpClip[i];
        if (tmpClip == -1) return;

        Eng.SetPDataInt(entity, 51, tmpClip);
        Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).ReloadTime[0]);
        Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).ReloadTime[0]);
        Eng.SetPDataInt(entity, 54, 1);
        if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimReloadSilenced]);
        else
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimReload]);
    }

    [AmxxCallback("wt_WeaponWeaponIdle")]
    public static int OnWeaponWeaponIdle(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return EngineConst.HamIgnored;
        if (i == 0) return EngineConst.HamIgnored;

        int result = NativeFunctionRegister.Execute_WpnWeaponIdle(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).ReloadMode != 0)
        {
            if (Eng.GetPDataFloat(entity, 48) > 0.0f) return EngineConst.HamIgnored;

            if (WeaponStore.Player(id).Reloading) return EngineConst.HamIgnored;

            int bpAmmo = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
            int clip = Eng.GetPDataInt(entity, 51);
            int inSpecialReload = Eng.GetPDataInt(entity, 55);
            if (clip == 0 && inSpecialReload == 0)
            {
                if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Open && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                    return EngineConst.HamSupercede;

                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Open && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                return EngineConst.HamSupercede;

            int tempClip = 0;
            if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM3) tempClip = 8;
            else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswXm1014) tempClip = 7;
            if (inSpecialReload != 0 && tempClip != 0)
            {
                // 弹夹数和原版一样时继续换弹
                if (clip < WeaponStore.Weapon(id, i).Clip && clip == tempClip && bpAmmo != 0)
                    WeaponAmmo.ShotgunReload(entity, WeaponStore.Weapon(id, i).Clip, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);

                // 换弹结束
                else if (clip == WeaponStore.Weapon(id, i).Clip)
                {
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAfterReload]);
                    Eng.SetPDataInt(entity, 55, 0);
                    Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).ReloadTime[2]);
                }
            }
            else if (inSpecialReload != 0 && tempClip == 0)
            {
                if (clip < WeaponStore.Weapon(id, i).Clip && bpAmmo != 0)
                    HamSandwich.ExecuteHamB(EngineConst.HamWeaponReload, entityIndex);

                else if (clip == WeaponStore.Weapon(id, i).Clip || bpAmmo == 0)
                {
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAfterReload]);
                    Eng.SetPDataInt(entity, 55, 0);
                    Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).ReloadTime[2]);
                }
                return EngineConst.HamSupercede;
            }
        }
        if (Eng.GetPDataFloat(entity, 48) <= 0.0f && WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && Eng.GetPDataInt(entity, 55) == 0 && Eng.GetPDataInt(entity, 54) == 0)
        {
            if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimIdle]);
            else
            {
                if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdleSilenced]);
                else
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdle]);
            }
            Eng.SetPDataFloat(entity, 48, 20.0f);
            return EngineConst.HamSupercede;
        }
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_UseStationaryPost")]
    public static void OnUseStationaryPost(int entityIndex, int id, int activator, int useType)
    {
        if (useType == 0 && Eng.IsConnected(id)) Callbacks.EventCurWeapon(id);
    }

    [AmxxCallback("wt_TakeDamage")]
    public static int OnTakeDamage(int victim, int inflictor, int attacker, float damage, int damageType)
    {
        if (!Eng.IsConnected(attacker) || !Eng.IsConnected(victim)) return EngineConst.HamIgnored;

        int forwardResult = NativeFunctionRegister.Execute_WpnTakeDamage(victim, inflictor, attacker, damage, damageType);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        var pVictim = Eng.EntOfIndex(victim);
        var pAttacker = Eng.EntOfIndex(attacker);
        if (pVictim == null || pAttacker == null) return EngineConst.HamIgnored;

        damage *= WeaponStore.Player(attacker).DamagePoint * WeaponStore.Player(victim).DamageVictimPoint;
        int i = WeaponGive.WeaponTypeByIndex(inflictor);
        if ((damageType & (1 << 24)) != 0)
        {
            if (i == 0)
            {
                HamSandwich.SetHamParamFloat(4, damage);
                return EngineConst.HamIgnored;
            }
            damage *= WeaponStore.Weapon(attacker, i).Damage;
            var pInflictor = Eng.EntOfIndex(inflictor);
            var victimOrigin = new float[] { pVictim->v.origin.x, pVictim->v.origin.y, pVictim->v.origin.z };
            var inflictorOrigin = pInflictor != null
                ? new float[] { pInflictor->v.origin.x, pInflictor->v.origin.y, pInflictor->v.origin.z }
                : new float[] { 0f, 0f, 0f };
            float distance = VectorMath.Distance(victimOrigin, inflictorOrigin);
            damage -= damage * distance / WeaponStore.Weapon(attacker, i).GrenadeData[2];
            if (damage <= 0.0f) damage = 0.0f;
            HamSandwich.SetHamParamFloat(4, damage);
            return EngineConst.HamIgnored;
        }
        if (attacker != inflictor)
        {
            if (i != 0) WeaponStore.Player(attacker).AttackerEntity = inflictor;
            else
            {
                HamSandwich.SetHamParamFloat(4, damage);
                return EngineConst.HamIgnored;
            }
        }
        else
        {
            var activeItem = Eng.GetPDataCbase(pAttacker, 373);
            if (activeItem == null) return EngineConst.HamIgnored;

            i = WeaponStore.GetWeaponType(activeItem);
            if (i == 0)
            {
                HamSandwich.SetHamParamFloat(4, damage);
                return EngineConst.HamIgnored;
            }
            WeaponStore.Player(attacker).AttackerEntity = Eng.IndexOf(activeItem);
        }
        if (WeaponStore.Weapon(attacker, i).Id == EngineConst.CswKnife)
        {
            if (WeaponStore.Player(attacker).KnifeTempDamage != 0.0f)
            {
                HamSandwich.SetHamParamFloat(4, damage * WeaponStore.Player(attacker).KnifeTempDamage);
                WeaponStore.Player(attacker).KnifeTempDamage = 0.0f;
            }
            return EngineConst.HamIgnored;
        }
        HamSandwich.SetHamParamFloat(4, damage * WeaponStore.Weapon(attacker, i).Damage);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_TakeDamagePost")]
    public static void OnTakeDamagePost(int victim, int inflictor, int attacker, float damage, int damageType)
    {
        if (!Eng.IsConnected(attacker) || !Eng.IsConnected(victim)) return;

        var pAttacker = Eng.EntOfIndex(attacker);
        var pVictim = Eng.EntOfIndex(victim);
        if (pAttacker == null || pVictim == null) return;

        if (Eng.GetPDataInt(pAttacker, 114) == Eng.GetPDataInt(pVictim, 114)) return;

        if (WeaponStore.Player(attacker).AttackerEntity != 0)
        {
            float health = pVictim->v.health;
            if (health - damage > 0.0f) WeaponStore.Player(attacker).AttackerEntity = 0;
        }
        var activeItem = Eng.GetPDataCbase(pAttacker, 373);
        if (activeItem == null) return;

        int i = WeaponStore.GetWeaponType(activeItem);
        int forwardResult = NativeFunctionRegister.Execute_WpnWeaponKnockBack(attacker, victim, inflictor, i, damage);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return;

        if (i == 0) return;

        // 原版 pev_flags & (FL_DUCKING|FL_ONGROUND) == (FL_DUCKING|FL_ONGROUND) 受 Pawn 运算符优先级影响
        // 实际退化为 flags & 1;这里按注释的本意(蹲下且在地面)实现。
        bool ducking = ((pVictim->v.flags & (EngineConst.FlDucking | EngineConst.FlOnground)) == (EngineConst.FlDucking | EngineConst.FlOnground));

        var victimOrigin = new float[] { pVictim->v.origin.x, pVictim->v.origin.y, pVictim->v.origin.z };
        var attackerOrigin = new float[] { pAttacker->v.origin.x, pAttacker->v.origin.y, pAttacker->v.origin.z };
        var velocity = new float[3];
        float victimVelocityZ = pVictim->v.velocity.z;
        float knockBack = ducking
            ? damage * WeaponStore.Weapon(attacker, i).KnockBack * 0.25f * WeaponStore.Player(victim).KnockBackPoint
            : damage * WeaponStore.Weapon(attacker, i).KnockBack * WeaponStore.Player(victim).KnockBackPoint;
        VectorMath.GetVelocityFromOrigin(victimOrigin, attackerOrigin, knockBack, velocity);
        velocity[2] = victimVelocityZ;
        pVictim->v.velocity = new vec3_t { x = velocity[0], y = velocity[1], z = velocity[2] };
    }

    [AmxxCallback("wt_KilledPost")]
    public static void OnKilledPost(int id, int attacker)
    {
        if (!Eng.IsConnected(id)) return;

        // 死亡时剥夺所有子弹
        const int primaryBitSum =
            (1 << EngineConst.CswScout) | (1 << EngineConst.CswXm1014) | (1 << EngineConst.CswMac10) | (1 << EngineConst.CswAug) |
            (1 << EngineConst.CswUmp45) | (1 << EngineConst.CswSg550) | (1 << EngineConst.CswGalil) | (1 << EngineConst.CswFamas) |
            (1 << EngineConst.CswAwp) | (1 << EngineConst.CswMp5Navy) | (1 << EngineConst.CswM249) | (1 << EngineConst.CswM3) |
            (1 << EngineConst.CswM4a1) | (1 << EngineConst.CswTmp) | (1 << EngineConst.CswG3sg1) | (1 << EngineConst.CswSg552) |
            (1 << EngineConst.CswAk47) | (1 << EngineConst.CswP90);
        const int secondaryBitSum =
            (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) |
            (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);
        Array.Clear(WeaponStore.Player(id).BPAmmo);
        for (int i = 0; i < 32; i++) WeaponStore.Player(id).GameBpAmmo[i] = 0;
        for (int i = 1; i < 31; i++)
            if (((1 << i) & (primaryBitSum | secondaryBitSum)) != 0) PlayerStocks.SetUserBpAmmo(id, i, 0);
    }

    [AmxxCallback("wt_ThinkGrenade")]
    public static int OnThinkGrenade(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;

        float dmgTime = entity->v.dmgtime;
        float curTime = Eng.GameTime;
        if (dmgTime > curTime) return EngineConst.HamIgnored;

        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        int ownerId = Eng.IndexOf(entity->v.owner);
        int forwardResult = NativeFunctionRegister.Execute_WpnHookGrenadeExplode(entityIndex, ownerId, i);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        var origin = stackalloc float[3];
        origin[0] = entity->v.origin.x;
        origin[1] = entity->v.origin.y;
        origin[2] = entity->v.origin.z;

        Eng.MessageBegin(EngineConst.MsgAll, EngineConst.SvcTempEntity, origin, null);
        Eng.WriteByte(EngineConst.TeExplosion);
        Eng.WriteCoord(origin[0]);
        Eng.WriteCoord(origin[1]);
        Eng.WriteCoord(origin[2] + 45.0f);
        Eng.WriteShort(WeaponStore.Weapon(ownerId, i).GrenadeSprId);
        Eng.WriteByte(25);
        Eng.WriteByte(30);
        Eng.WriteByte(EngineConst.TeExplFlagNoSound);
        Eng.MessageEnd();

        Eng.MessageBegin(EngineConst.MsgPas, EngineConst.SvcTempEntity, origin, null);
        Eng.WriteByte(EngineConst.TeWorldDecal);
        Eng.WriteCoord(origin[0]);
        Eng.WriteCoord(origin[1]);
        Eng.WriteCoord(origin[2]);
        Eng.WriteByte(Eng.DecalIndex("{scorch3"));
        Eng.MessageEnd();

        Eng.EmitSound(entity, EngineConst.ChanStatic, WeaponStore.Weapon(ownerId, i).Sound1, EngineConst.VolNorm, WeaponStore.Weapon(ownerId, i).FireSoundVolume, 0, EngineConst.PitchNorm);

        // 记录手雷的序号，给deathmsg用
        WeaponStore.Player(ownerId).UserGrenadeId = i;

        // 原版从实体 0(worldspawn)开始检索
        var found = Eng.EntOfIndex(0);
        while (found != null && (found = Eng.FindEntityInSphere(found, origin, WeaponStore.Weapon(ownerId, i).GrenadeData[2])) != null)
        {
            int victim = Eng.IndexOf(found);
            if (!Eng.IsValidIndex(victim)) continue;
            if (found->v.solid <= EngineConst.SolidNot) continue;
            if (found->v.takedamage <= 0.0f) continue;
            var pOwner = Eng.EntOfIndex(ownerId);
            if (victim != ownerId && Eng.IsConnected(victim) && Eng.IsValid(pOwner) && Eng.GetPDataInt(pOwner, 114) == Eng.GetPDataInt(found, 114) && Eng.CvarNum("mp_friendlyfire") == 0)
                continue;

            Eng.TakeDamage(victim, entityIndex, ownerId, 100.0f, 1 << 24);
            NativeFunctionRegister.Execute_WpnHookGrenadeExplodePost(ownerId, entityIndex, victim, i);
        }
        WeaponStore.Player(ownerId).UserGrenadeId = 0;
        Eng.RemoveEntity(entity);
        return EngineConst.HamSupercede;
    }

    // 其他方式获取的子弹
    [AmxxCallback("wt_GiveAmmo")]
    public static int OnGiveAmmo(int id, int amount, string ammoName, int max)
    {
        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return EngineConst.HamIgnored;

        if (WeaponStore.Player(id).BuyingAmmo) return EngineConst.HamIgnored;

        ammoName = ammoName.ToLowerInvariant();
        if (ammoName is "hegrenade" or "flashbang" or "smokegrenade" or "c4") return EngineConst.HamIgnored;

        int i = WeaponStore.GetWeaponType(activeItem);
        int weapon = Eng.GetPDataInt(activeItem, 43);
        const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
        const int secWeaponBitSum = (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) | (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);

        if (GameTables.GameWeaponAmmoType[weapon] == ammoName)
        {
            if (((1 << weapon) & secWeaponBitSum) == 0 && ((1 << weapon) & specialBitSum) == 0)
                WeaponAmmo.GivePriAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
            else if (((1 << weapon) & secWeaponBitSum) != 0)
                WeaponAmmo.GiveSecAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
        }
        else
        {
            if (Eng.GetPDataCbase(pPlayer, 368) == activeItem) activeItem = Eng.GetPDataCbase(pPlayer, 369);
            else if (Eng.GetPDataCbase(pPlayer, 369) == activeItem) activeItem = Eng.GetPDataCbase(pPlayer, 368);
            else if (Eng.GetPDataCbase(pPlayer, 368) != activeItem && Eng.GetPDataCbase(pPlayer, 369) != activeItem)
            {
                var item1 = Eng.GetPDataCbase(pPlayer, 369);
                var item2 = Eng.GetPDataCbase(pPlayer, 368);
                if (Eng.IsValid(item1) && GameTables.GameWeaponAmmoType[Eng.GetPDataInt(item1, 43)] == ammoName) activeItem = item1;
                if (Eng.IsValid(item2) && GameTables.GameWeaponAmmoType[Eng.GetPDataInt(item2, 43)] == ammoName) activeItem = item2;
            }
            if (!Eng.IsValid(activeItem)) return EngineConst.HamSupercede;

            i = WeaponStore.GetWeaponType(activeItem);
            weapon = Eng.GetPDataInt(activeItem, 43);
            if (GameTables.GameWeaponAmmoType[weapon] == ammoName)
            {
                if (((1 << weapon) & secWeaponBitSum) == 0)
                    WeaponAmmo.GivePriAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
                else
                    WeaponAmmo.GiveSecAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
            }
        }
        return EngineConst.HamSupercede;
    }

    [AmxxCallback("wt_PlayerSpawnPost")]
    public static void OnPlayerSpawnPost(int id)
    {
        if (!Eng.IsAlive(id)) return;

        if (!Eng.IsBot(id))
        {
            var pPlayer = Eng.EntOfIndex(id);
            var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
            if (activeItem == null) return;

            int i = WeaponStore.GetWeaponType(activeItem);
            if (i == 0) return;

            if (WeaponStore.Weapon(id, i).Sight == 0) return;

            if (WeaponStore.Player(id).SightMode == WpnConst.Closed) return;

            Attack.ClosingEnd(activeItem);
            return;
        }
        if (WeaponStore.Round == 0) return;

        WeaponStore.Player(id).BotBuyWeapon = true;
        if (WeaponStore.Player(id).BlockBuy == WpnConst.BuyNormally) WeaponStore.Player(id).BotNextThink = 0.0f;
        else if (WeaponStore.Player(id).BlockBuy == WpnConst.BuyEverywhere) WeaponStore.Player(id).BotNextThink = Eng.GameTime + 0.1f;
        else WeaponStore.Player(id).BotBuyWeapon = false;
    }

    [AmxxCallback("wt_TraceAttackPost")]
    public static void OnTraceAttackPost(int entityIndex, int attacker, float damage, [AmxxBuffer(3)] Span<float> direction, int traceHandle, int damageType)
    {
        if (!Eng.IsAlive(attacker)) return;

        var pAttacker = Eng.EntOfIndex(attacker);
        var activeItem = pAttacker == null ? null : Eng.GetPDataCbase(pAttacker, 373);
        int i = Eng.IsValid(activeItem) ? WeaponStore.GetWeaponType(activeItem) : 0;
        if (i == 0) return;

        if (WeaponStore.Weapon(attacker, i).Id == EngineConst.CswKnife) return;

        // ham 传入的是 fakemeta trace 句柄
        var endPos = new float[3];
        FakeMeta.GetTr2(traceHandle, 5, endPos); // TR_vecEndPos = 5

        if (entityIndex != 0)
        {
            Eng.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
            Eng.WriteByte(EngineConst.TeDecal);
            Eng.WriteCoord(endPos[0]);
            Eng.WriteCoord(endPos[1]);
            Eng.WriteCoord(endPos[2]);
            Eng.WriteByte(GameTables.GunshotDecals[Eng.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
            Eng.WriteShort(entityIndex);
            Eng.MessageEnd();
        }
        else
        {
            Eng.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
            Eng.WriteByte(EngineConst.TeWorldDecal);
            Eng.WriteCoord(endPos[0]);
            Eng.WriteCoord(endPos[1]);
            Eng.WriteCoord(endPos[2]);
            Eng.WriteByte(GameTables.GunshotDecals[Eng.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
            Eng.MessageEnd();
        }
        Eng.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
        Eng.WriteByte(EngineConst.TeGunshotDecal);
        Eng.WriteCoord(endPos[0]);
        Eng.WriteCoord(endPos[1]);
        Eng.WriteCoord(endPos[2]);
        Eng.WriteShort(attacker);
        Eng.WriteByte(GameTables.GunshotDecals[Eng.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
        Eng.MessageEnd();
        endPos[2] += 5.0f;
        Effects.FakeSmoke(endPos);
    }
}
