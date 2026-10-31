using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// RegisterHam 武器持有回调:入包、部署、收起与固定武器(func_tank)使用。
public static unsafe partial class WeaponDeploy
{
    [AmxxCallback("wt_ItemAddToPlayerPost")]
    public static void OnItemAddToPlayerPost(int entityIndex, int id)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return;
        if (Engine.GetPDataInt(entity, CsOffsets.ItemId) == EngineConst.CswC4) return;
        WeaponGive.ChangeWeaponList(id, entity);
    }

    [AmxxCallback("wt_ItemDeploy")]
    public static int OnItemDeploy(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        // AWP无法设置切枪时间的bug
        WeaponStore.Player(id).UserDeploy[0] = Engine.GetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack);
        WeaponStore.Player(id).UserDeploy[1] = Engine.GetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_ItemDeployPost")]
    public static void OnItemDeployPost(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);

        if (i == 0)
        {
            // 记录原版武器备弹
            int weapon = Engine.GetPDataInt(entity, CsOffsets.ItemId);
            const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            if (((1 << weapon) & specialBitSum) == 0) PlayerStocks.SetUserBpAmmo(id, weapon, WeaponStore.Player(id).GameBpAmmo[weapon]);
            return;
        }

        // 记录新武器备弹
        if (WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && WeaponStore.Weapon(id, i).Id != EngineConst.CswKnife)
            PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, WeaponStore.Player(id).BPAmmo[i]);
        WeaponGive.ChangeWeaponList(id, entity);
        Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
        Engine.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw]);
            Engine.EmitSound(pPlayer, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
        }
        else if (WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade)
        {
            if ((Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 2)) != 0 || (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 0)) != 0)
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDrawSilenced]);
            else
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimDraw]);
        }
        Engine.SetPDataFloat(pPlayer, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).DrawTime);
        Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Player(id).UserDeploy[0]);
        Engine.SetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack, WeaponStore.Player(id).UserDeploy[1]);
        if (WeaponStore.Weapon(id, i).Clip == 0 && WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && WeaponStore.Weapon(id, i).Id != EngineConst.CswKnife)
        {
            int clip = Engine.GetPDataInt(entity, CsOffsets.ItemClip);
            PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, clip);
            Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("CurWeapon"), null, pPlayer);
            Engine.WriteByte(1);
            Engine.WriteByte(WeaponStore.Weapon(id, i).Id);
            Engine.WriteByte(0xFF); // -1
            Engine.MessageEnd();
            Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("AmmoX"), null, pPlayer);
            Engine.WriteByte(GameTables.GameWeaponAmmoId[WeaponStore.Weapon(id, i).Id]);
            Engine.WriteByte(clip);
            Engine.MessageEnd();
        }
        else
        {
            Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("CurWeapon"), null, pPlayer);
            Engine.WriteByte(1);
            Engine.WriteByte(WeaponStore.Weapon(id, i).Id);
            Engine.WriteByte(Engine.GetPDataInt(entity, CsOffsets.ItemClip));
            Engine.MessageEnd();
        }
        if (WeaponStore.Weapon(id, i).Sight != 0)
        {
            Engine.SetPDataInt(pPlayer, CsOffsets.Zoom, 90);
            WeaponStore.Player(id).SightMode = WpnConst.Closed;
            Engine.SetPDataInt(pPlayer, CsOffsets.ZoomSignal, Engine.GetPDataInt(pPlayer, CsOffsets.ZoomSignal) & ~(1 << 6));
            WeaponStore.Player(id).Reloading = false;
        }
        NativeFunctionRegister.Execute_WpnWeaponDraw(entityIndex, id, i);
    }

    [AmxxCallback("wt_ItemHolsterPost")]
    public static void OnItemHolsterPost(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (i != 0 && WeaponStore.Weapon(id, i).Sight != 0)
        {
            Engine.SetPDataInt(pPlayer, CsOffsets.Zoom, 90);
            WeaponStore.Player(id).SightMode = WpnConst.Closed;
            Engine.SetPDataInt(pPlayer, CsOffsets.ZoomSignal, Engine.GetPDataInt(pPlayer, CsOffsets.ZoomSignal) & ~(1 << 6));
            WeaponStore.Player(id).Reloading = false;
        }

        // 收起武器时记录备用子弹
        int weapon = Engine.GetPDataInt(entity, CsOffsets.ItemId);
        const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
        if (((1 << weapon) & specialBitSum) == 0)
        {
            if (i == 0) WeaponStore.Player(id).GameBpAmmo[weapon] = PlayerStocks.GetUserBpAmmo(id, weapon);
            else WeaponStore.Player(id).BPAmmo[i] = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
        }
    }

    [AmxxCallback("wt_UseStationaryPost")]
    public static void OnUseStationaryPost(int entityIndex, int id, int activator, int useType)
    {
        if (useType == 0 && Engine.IsConnected(id)) WeaponView.EventCurWeapon(id);
    }


    // ---- fm_SetModel(EngFunc_SetModel hook:手雷与武器箱世界模型替换) ----

    [AmxxHook("pfnSetModel", Table = HookTable.Engine)]
    public static void SetModel(edict_t* pEntity, sbyte* model)
    {
        HookResult.Ignore();
        if (SetModelLogic(pEntity)) HookResult.Supercede();
    }

    // 与原版 fw_SetModel 的分歧(2026-10 决策,按安全版本保留):原版两个分支都无条件 FMRES_SUPERCEDE,
    // WModel 为空时也会拦掉引擎换模(实体可能丢模型);这里仅在确实替换了模型时才 SUPERCEDE,
    // WModel 为空时放行引擎默认行为。
    private static bool SetModelLogic(edict_t* entity)
    {
        if (!Engine.IsValid(entity)) return false;

        string className = Engine.ClassName(entity);
        if (className == "grenade")
        {
            int id = Engine.IndexOf(entity->v.owner);
            if (id == 0) return false;

            var pPlayer = Engine.EntOfIndex(id);
            var activeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
            if (activeItem == null) return false;

            int i = WeaponStore.GetWeaponType(activeItem);
            if (i == 0) return false;

            Engine.SetPDataInt(entity, CsOffsets.ItemTemplateSerial, i);
            entity->v.dmgtime = Engine.GameTime + WeaponStore.Weapon(id, i).GrenadeData[1];
            Engine.SetModel(entity, WeaponStore.Weapon(id, i).WModel);
            if (WeaponStore.Weapon(id, i).WModelBody != 0) entity->v.body = WeaponStore.Weapon(id, i).WModelBody;
            return !string.IsNullOrEmpty(WeaponStore.Weapon(id, i).WModel);
        }
        if (className != "weaponbox") return false;

        // 通过weaponbox实体获取武器实体
        edict_t* weapon = null;
        for (int i = 0; i < 6; i++)
        {
            weapon = Engine.GetPDataCbase(entity, CsOffsets.BoxItem(i));
            if (weapon != null) break;
        }
        if (weapon == null) return false;

        int type = WeaponStore.GetWeaponType(weapon);
        if (type == 0) return false;

        int ownerId = Engine.IndexOf(entity->v.owner);
        if (WeaponStore.Weapon(ownerId, type).Id == EngineConst.CswHegrenade)
        {
            entity->v.nextthink = Engine.GameTime + 0.1f;
            return true;
        }
        Engine.SetModel(entity, WeaponStore.Weapon(ownerId, type).WModel);
        if (WeaponStore.Weapon(ownerId, type).WModelBody != 0) entity->v.body = WeaponStore.Weapon(ownerId, type).WModelBody;
        return !string.IsNullOrEmpty(WeaponStore.Weapon(ownerId, type).WModel);
    }
}
